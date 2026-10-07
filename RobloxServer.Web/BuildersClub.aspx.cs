using System;
using System.Text;
using RobloxServer.Data;
using RobloxServer.Web;

namespace RobloxServer.Pages
{
    /// <summary>
    /// Builders Club 2013 (BC / Turbo / Outrageous). Servidor privado, sem pagamento: quem concede e o admin.
    /// O nivel muda o limite de grupos (5 / 10 / 20 / 100).
    /// </summary>
    public class BuildersClub : SocialPage
    {
        protected override string Handle(string verb, long id, User user)
        {
            if (verb == "grant")
            {
                if (!Db.IsAdmin(user)) return "Only admins can change memberships.";
                User target = Db.FindUser((Request.Form["who"] ?? "").Trim());
                string tier = Request.Form["tier"] ?? "";
                if (target == null) return "That user does not exist.";
                if (tier != "BC" && tier != "TBC" && tier != "OBC" && tier != "None") return "Unknown membership.";
                SocialStore.BuildersClub.Delete(m => m.UserId == target.Id);
                if (tier != "None") SocialStore.BuildersClub.Insert(new BuildersClubMember { UserId = target.Id, Tier = tier, Since = DateTime.UtcNow });
                return null;
            }
            return "Unknown action.";
        }

        protected override string Render(User user)
        {
            string tier = SocialStore.Tier(user.Id);
            var sb = new StringBuilder("<div id=\"BuildersClubContainer\" style=\"width:880px;margin:10px auto;\">" + Hidden(user));
            sb.Append("<div class=\"StandardBoxHeader\"><span>Builders Club</span></div><div class=\"StandardBox\" style=\"padding:12px;\">");
            sb.Append("<p>Your membership: <b>" + E(SocialStore.TierName(tier)) + "</b>.</p>");
            sb.Append("<table cellspacing=\"0\" cellpadding=\"6\" style=\"width:100%;text-align:center;border-collapse:collapse;\"><tr><th></th><th>Free</th><th>Builders Club</th><th>Turbo</th><th>Outrageous</th></tr>"
                + "<tr><td align=\"left\">Groups you can join</td><td>5</td><td>10</td><td>20</td><td>100</td></tr>"
                + "<tr><td align=\"left\">Badge next to your name</td><td>-</td><td>Yes</td><td>Yes</td><td>Yes</td></tr></table>");
            sb.Append("<p style=\"color:#666;\">There are no payments on this server. Ask an admin if you want a membership.</p></div>");

            if (Db.IsAdmin(user))
            {
                sb.Append("<br /><div class=\"StandardBoxHeader\"><span>Admin: set membership</span></div><div class=\"StandardBox\" style=\"padding:8px;\">"
                    + "Username: <input type=\"text\" name=\"who\" maxlength=\"40\" /> "
                    + "<select name=\"tier\"><option value=\"BC\">Builders Club</option><option value=\"TBC\">Turbo Builders Club</option><option value=\"OBC\">Outrageous Builders Club</option><option value=\"None\">Remove</option></select> "
                    + Btn("Set", "grant") + "</div>");
            }
            return sb.Append("</div>").ToString();
        }
    }
}
