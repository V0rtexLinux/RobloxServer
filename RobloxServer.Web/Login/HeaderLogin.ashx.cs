using System;
using System.Web;
using RobloxServer.Data;
using RobloxServer.Security;
using RobloxServer.Web;

namespace RobloxServer.Handlers.Login
{
    /// <summary>
    /// Login/HeaderLogin.ashx: target of the "Login" dropdown in the header and of the Member Login box of the home page.
    /// A good login goes back to the page the visitor was on; a bad one goes to Login.aspx with the message.
    /// </summary>
    public class HeaderLogin : HandlerBase
    {
        protected override void Handle()
        {
            if (Request.HttpMethod != "POST")
            {
                Response.Redirect("~/Login.aspx", false);
                return;
            }

            string error;
            User user = Auth.Login(Request.Form["hl_user"], Request.Form["hl_pass"], Context, out error);
            if (user == null)
            {
                string text = error ?? "Incorrect username or password.";
                Response.Cookies.Add(new HttpCookie("RBXLoginError", HttpUtility.UrlEncode(text))
                {
                    HttpOnly = true,
                    Expires = DateTime.UtcNow.AddMinutes(2)
                });
                Response.Redirect("~/Login.aspx", false);
                return;
            }

            Auth.SignIn(user, Context);
            Response.Redirect(Accounts.AfterLogin(user, Request.Form["hl_return"]), false);
        }
    }
}
