using System.Text;
using RobloxServer.Data;
using RobloxServer.Web;

namespace RobloxServer.Pages
{
    /// <summary>My Money: saldo de Robux/Tix, como ganhar, e (admin) dar ou tirar saldo.</summary>
    public class Money : SocialPage
    {
        static int ToInt(string s)
        {
            int v;
            int.TryParse((s ?? "").Trim(), out v);
            return v;
        }

        protected override string Handle(string verb, long id, User user)
        {
            if (verb != "grant")
            {
                return "Unknown action.";
            }
            if (!Db.IsAdmin(user))
            {
                return "Only admins can change balances.";
            }
            User target = Db.FindUser((Request.Form["gname"] ?? "").Trim());
            if (target == null)
            {
                return "User not found.";
            }
            int robux = ToInt(Request.Form["grobux"]);
            int tix = ToInt(Request.Form["gtix"]);
            if (robux < -1000000 || robux > 1000000 || tix < -1000000 || tix > 1000000)
            {
                return "Use values between -1,000,000 and 1,000,000.";
            }
            Economy.Grant(target.Id, robux, tix);
            Logging.Log(LogType.Success, "Admin " + user.Name + " granted R$ " + robux + " / T$ " + tix + " to " + target.Name);
            NextUrl = Url("Money.aspx?ok=" + System.Uri.EscapeDataString(target.Name));
            return null;
        }

        protected override string Render(User user)
        {
            Economy.ClaimDaily(user);
            User me = Db.Users.Find(u => u.Id == user.Id) ?? user;
            string tier = SocialStore.Tier(user.Id);
            int stipend = Economy.Stipend(tier);

            var sb = new StringBuilder();
            sb.Append("<h2>My Money</h2>");
            string ok = Request.QueryString["ok"];
            if (!string.IsNullOrEmpty(ok))
            {
                sb.Append("<p style=\"color:#060\">Balance updated for " + E(ok) + ".</p>");
            }
            sb.Append("<p><b>Robux:</b> R$ " + me.Robux.ToString("N0") + " &nbsp;&nbsp; <b>Tix:</b> T$ " + me.Tix.ToString("N0") + "</p>");
            sb.Append("<p>Every day you log in you get T$ " + Economy.DailyTix);
            if (stipend > 0)
            {
                sb.Append(" and a stipend of R$ " + stipend + " (" + E(SocialStore.TierName(tier)) + ")");
            }
            sb.Append(". Catalog items cost Robux, or " + Economy.TixPerRobux + " Tix for each R$. ");
            sb.Append(stipend > 0 ? "" : "Builders Club members also get a daily Robux stipend. ");
            sb.Append("<a href=\"" + Url("Catalog.aspx") + "\">Go to the Catalog</a>.</p>");
            sb.Append("<p style=\"color:#666\">Robux and Tix are virtual and cannot be bought with real money on this server.</p>");

            if (Db.IsAdmin(user))
            {
                sb.Append("<h2>Admin: change a balance</h2><div>" + Hidden(user)
                    + "User name <input type=\"text\" name=\"gname\" maxlength=\"30\" /> "
                    + "R$ <input type=\"text\" name=\"grobux\" value=\"0\" size=\"8\" /> "
                    + "T$ <input type=\"text\" name=\"gtix\" value=\"0\" size=\"8\" /> "
                    + Btn("Apply", "grant:0") + "</div>");
            }
            return sb.ToString();
        }
    }
}
