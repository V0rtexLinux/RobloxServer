using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using RobloxServer.Data;
using RobloxServer.Security;
using RobloxServer.Web;

namespace RobloxServer.Handlers.Login
{
    /// <summary>
    /// "Login with Facebook" (OAuth 2, fluxo de codigo no servidor).
    /// /Login/Facebook.ashx           -> manda para o Facebook
    /// /Login/Facebook.ashx?code=...  -> volta do Facebook: entra, liga a conta logada, ou cria uma conta nova.
    /// Precisa de FacebookAppId (Web.config) e do segredo na variavel de ambiente ROBLOXSERVER_FACEBOOK_SECRET.
    /// </summary>
    public class Facebook : HandlerBase
    {
        const string StateCookie = "RBXFbState";
        const string Api = "https://graph.facebook.com/v19.0/";

        static Facebook()
        {
            ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072; // TLS 1.2
        }

        protected override void Handle()
        {
            string appId = Config.FacebookAppId;
            string secret = Config.FacebookAppSecret;
            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(secret))
            {
                WriteStatus(503, "Facebook login is not configured on this server.");
                return;
            }
            string code = Request.QueryString["code"];
            if (string.IsNullOrEmpty(code))
            {
                if (!string.IsNullOrEmpty(Request.QueryString["error"]))
                {
                    Fail("Facebook login was cancelled.");
                    return;
                }
                Start(appId);
                return;
            }
            Finish(appId, secret, code);
        }

        string RedirectUri()
        {
            string root = ConfigurationManager.AppSettings["BaseUrl"];
            if (string.IsNullOrEmpty(root))
            {
                root = Request.Url.GetLeftPart(UriPartial.Authority);
            }
            return root.TrimEnd('/') + "/Login/Facebook.ashx";
        }

        static string Hex(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }

        static string RandomHex(int bytes)
        {
            var data = new byte[bytes];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(data);
            }
            return Hex(data);
        }

        void Start(string appId)
        {
            string state = RandomHex(16);
            Response.Cookies.Add(new HttpCookie(StateCookie, state)
            {
                HttpOnly = true,
                Secure = Request.IsSecureConnection,
                Expires = DateTime.UtcNow.AddMinutes(10)
            });
            Response.Redirect("https://www.facebook.com/v19.0/dialog/oauth?client_id=" + Uri.EscapeDataString(appId)
                + "&redirect_uri=" + Uri.EscapeDataString(RedirectUri()) + "&state=" + state + "&scope=public_profile", false);
        }

        void Fail(string text)
        {
            Response.Cookies.Add(new HttpCookie("RBXLoginError", HttpUtility.UrlEncode(text))
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddMinutes(2)
            });
            Response.Redirect("~/Login.aspx", false);
        }

        static string Get(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Timeout = 15000;
            request.ReadWriteTimeout = 15000;
            using (var response = request.GetResponse())
            using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        void Finish(string appId, string secret, string code)
        {
            HttpCookie cookie = Request.Cookies[StateCookie];
            string state = Request.QueryString["state"] ?? "";
            if (cookie == null || string.IsNullOrEmpty(cookie.Value) || !PasswordHasher.ConstantTimeEquals(cookie.Value, state))
            {
                Fail("Facebook login expired. Please try again.");
                return;
            }
            Response.Cookies.Add(new HttpCookie(StateCookie, "") { Expires = DateTime.UtcNow.AddDays(-1) });

            string fbId;
            string fbName;
            try
            {
                string tokenJson = Get(Api + "oauth/access_token?client_id=" + Uri.EscapeDataString(appId)
                    + "&redirect_uri=" + Uri.EscapeDataString(RedirectUri())
                    + "&client_secret=" + Uri.EscapeDataString(secret) + "&code=" + Uri.EscapeDataString(code));
                string token = Regex.Match(tokenJson, "\"access_token\"\s*:\s*\"([^\"]+)\"").Groups[1].Value;
                if (token.Length == 0)
                {
                    Fail("Facebook did not accept the login. Please try again.");
                    return;
                }
                string proof;
                using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
                {
                    proof = Hex(hmac.ComputeHash(Encoding.UTF8.GetBytes(token)));
                }
                string me = Get(Api + "me?fields=id,name&access_token=" + Uri.EscapeDataString(token) + "&appsecret_proof=" + proof);
                fbId = Regex.Match(me, "\"id\"\s*:\s*\"(\d+)\"").Groups[1].Value;
                fbName = Regex.Match(me, "\"name\"\s*:\s*\"((?:[^\"\\\\]|\\\\.)*)\"").Groups[1].Value;
                try { fbName = Regex.Unescape(fbName); } catch (ArgumentException) { }
            }
            catch (Exception ex)
            {
                Logging.Log(LogType.Security, "Facebook login failed: " + ex.Message);
                Fail("Could not reach Facebook. Please try again.");
                return;
            }
            if (fbId.Length == 0)
            {
                Fail("Facebook did not return your profile.");
                return;
            }

            User user = Db.Users.Find(u => u.FacebookId == fbId);
            if (user == null)
            {
                User current = Auth.CurrentUser;
                if (current != null)
                {
                    // Ja esta logado: so liga o Facebook a esta conta.
                    Db.Users.Update(u => u.Id == current.Id, u => u.FacebookId = fbId);
                    Response.Redirect("~/My/Home.aspx", false);
                    return;
                }
                user = CreateAccount(fbId, fbName);
                if (user == null)
                {
                    return;
                }
            }
            Auth.SignIn(user, Context);
            Response.Redirect(Accounts.AfterLogin(user, null), false);
        }

        User CreateAccount(string fbId, string fbName)
        {
            if (!Config.RegistrationOpen)
            {
                Fail("Registration is closed on this server.");
                return null;
            }
            if (Db.Users.All().Count == 0)
            {
                Fail("Create the first account with a username and password before using Facebook.");
                return null;
            }
            string ip = ClientIp.Get(Context);
            if (FloodChecker.Hit("register:" + ip, 3, TimeSpan.FromHours(1)))
            {
                Fail("Too many accounts have been created from your IP. Try again later.");
                return null;
            }

            string baseName = Regex.Replace(fbName ?? "", "[^A-Za-z0-9]", "");
            if (baseName.Length < 3)
            {
                baseName = "Player";
            }
            if (baseName.Length > 12)
            {
                baseName = baseName.Substring(0, 12);
            }
            string chosen = baseName;
            var random = new Random();
            int tries = 0;
            while (WordFilter.ValidateUserName(chosen) != null || Db.FindUser(chosen) != null)
            {
                if (++tries > 50)
                {
                    Fail("Could not pick a username for you. Sign up with a username instead.");
                    return null;
                }
                chosen = baseName + random.Next(100, 99999);
            }

            string name = chosen;
            DateTime now = DateTime.UtcNow;
            string hash = PasswordHasher.Hash(RandomHex(32)); // senha aleatoria: a conta entra pelo Facebook
            User created = Db.Users.InsertWithId(u => u.Id, 1, id => new User
            {
                Id = id,
                Name = name,
                PasswordHash = hash,
                Created = now,
                LastOnline = now,
                LastIp = ip,
                FacebookId = fbId
            });
            Logging.Log(LogType.Success, "New account " + created.Name + " (" + created.Id + ") via Facebook from " + ip);
            return created;
        }
    }
}
