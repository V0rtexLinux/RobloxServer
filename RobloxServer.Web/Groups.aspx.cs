using System;
using System.Linq;
using System.Text;
using RobloxServer.Data;
using RobloxServer.Security;
using RobloxServer.Web;

namespace RobloxServer.Pages
{
    /// <summary>Groups 2013: Groups.aspx (meus grupos + busca), ?id=ID (grupo), ?q=texto.</summary>
    public class Groups : SocialPage
    {
        protected override string Handle(string verb, long id, User user)
        {
            if (verb == "create")
            {
                string name = (Request.Form["gname"] ?? "").Trim();
                string desc = (Request.Form["gdesc"] ?? "").Trim();
                if (name.Length < 3 || name.Length > 50) return "A group name needs 3 to 50 characters.";
                if (desc.Length > 1000) desc = desc.Substring(0, 1000);
                if (!WordFilter.IsClean(name) || !WordFilter.IsClean(desc)) return "Please keep the group name and description clean.";
                if (SocialStore.Groups.Find(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)) != null) return "A group with that name already exists.";
                if (SocialStore.GroupMembers.Where(m => m.UserId == user.Id).Count >= SocialStore.GroupLimit(user.Id))
                    return "You are in too many groups. Leave one or upgrade your Builders Club.";

                Group g = SocialStore.Groups.InsertWithId(x => x.Id, 1, newId => new Group
                {
                    Id = newId, Name = name, Description = desc, OwnerId = user.Id, OwnerName = user.Name, Created = DateTime.UtcNow
                });
                SocialStore.GroupMembers.Insert(new GroupMember { GroupId = g.Id, UserId = user.Id, UserName = user.Name, Joined = DateTime.UtcNow });
                NextUrl = Url("Groups.aspx?id=" + g.Id);
                return null;
            }
            if (verb == "join")
            {
                if (SocialStore.Groups.Find(x => x.Id == id) == null) return "That group does not exist.";
                if (SocialStore.GroupMembers.Find(m => m.GroupId == id && m.UserId == user.Id) != null) return "You are already a member.";
                if (SocialStore.GroupMembers.Where(m => m.UserId == user.Id).Count >= SocialStore.GroupLimit(user.Id))
                    return "You are in too many groups. Leave one or upgrade your Builders Club.";
                SocialStore.GroupMembers.Insert(new GroupMember { GroupId = id, UserId = user.Id, UserName = user.Name, Joined = DateTime.UtcNow });
                return null;
            }
            if (verb == "leave")
            {
                Group g = SocialStore.Groups.Find(x => x.Id == id);
                if (g != null && g.OwnerId == user.Id) return "The owner cannot leave. Delete the group instead.";
                SocialStore.GroupMembers.Delete(m => m.GroupId == id && m.UserId == user.Id);
                return null;
            }
            if (verb == "delete")
            {
                Group g = SocialStore.Groups.Find(x => x.Id == id);
                if (g == null || (g.OwnerId != user.Id && !Db.IsAdmin(user))) return "You cannot delete this group.";
                SocialStore.Groups.Delete(x => x.Id == id);
                SocialStore.GroupMembers.Delete(m => m.GroupId == id);
                NextUrl = Url("Groups.aspx");
                return null;
            }
            return "Unknown action.";
        }

        protected override string Render(User user)
        {
            var sb = new StringBuilder("<div id=\"GroupsContainer\" style=\"width:880px;margin:10px auto;\">" + Hidden(user));
            long gid = ToLong(Request.QueryString["id"]);
            if (gid > 0)
            {
                Group g = SocialStore.Groups.Find(x => x.Id == gid);
                if (g == null) return sb.Append("<p>That group does not exist.</p></div>").ToString();
                var members = SocialStore.GroupMembers.Where(m => m.GroupId == gid).OrderBy(m => m.Joined).ToList();
                bool isMember = members.Any(m => m.UserId == user.Id);

                sb.Append("<div class=\"StandardBoxHeader\"><span>" + E(g.Name) + "</span></div><div class=\"StandardBox\" style=\"padding:10px;overflow:hidden;\">");
                sb.Append("<div style=\"color:#666;\">Owned by " + UserLink(g.OwnerId, g.OwnerName) + " &middot; " + members.Count + " members &middot; created " + g.Created.ToString("MMM d, yyyy", System.Globalization.CultureInfo.InvariantCulture) + "</div>");
                sb.Append("<p style=\"white-space:pre-wrap;\">" + E(g.Description) + "</p>");
                sb.Append(isMember ? Btn("Leave group", "leave:" + g.Id) : Btn("Join group", "join:" + g.Id));
                if (g.OwnerId == user.Id || Db.IsAdmin(user)) sb.Append(" " + Btn("Delete group", "delete:" + g.Id));
                sb.Append("<h3>Members</h3>");
                foreach (GroupMember m in members.Take(60))
                {
                    sb.Append("<div style=\"float:left;width:100px;text-align:center;margin:4px;\"><a href=\"" + Url("User.aspx?id=" + m.UserId) + "\"><img src=\"" + AvatarUrl(m.UserId) + "\" width=\"80\" height=\"80\" alt=\"\" style=\"border:0;\" /></a><br />" + UserLink(m.UserId, m.UserName) + "</div>");
                }
                sb.Append("</div><div style=\"margin-top:8px;\"><a class=\"Button\" href=\"" + Url("Groups.aspx") + "\">Back to Groups</a></div>");
                return sb.Append("</div>").ToString();
            }

            string q = (Request.QueryString["q"] ?? "").Trim();
            var mine = SocialStore.GroupMembers.Where(m => m.UserId == user.Id).Select(m => m.GroupId).ToList();

            sb.Append("<div class=\"StandardBoxHeader\"><span>My Groups (" + mine.Count + " / " + SocialStore.GroupLimit(user.Id) + ")</span></div><div class=\"StandardBox\" style=\"padding:8px;\">");
            foreach (long id in mine)
            {
                Group g = SocialStore.Groups.Find(x => x.Id == id);
                if (g != null) sb.Append("<div style=\"margin:3px 0;\"><a href=\"" + Url("Groups.aspx?id=" + g.Id) + "\">" + E(g.Name) + "</a></div>");
            }
            if (mine.Count == 0) sb.Append("<p>You are not in any groups yet.</p>");
            sb.Append("</div><br />");

            sb.Append("<div class=\"StandardBoxHeader\"><span>Find Groups</span></div><div class=\"StandardBox\" style=\"padding:8px;\">");
            sb.Append("<input type=\"text\" id=\"gq\" value=\"" + E(q) + "\" /> <input type=\"button\" value=\"Search\" onclick=\"location.href='" + Url("Groups.aspx") + "?q='+encodeURIComponent(document.getElementById('gq').value);\" />");
            var found = SocialStore.Groups.All().Where(g => q.Length == 0 || g.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0).OrderBy(g => g.Name).Take(30).ToList();
            foreach (Group g in found)
            {
                sb.Append("<div style=\"margin:4px 0;\"><a href=\"" + Url("Groups.aspx?id=" + g.Id) + "\"><b>" + E(g.Name) + "</b></a> <span style=\"color:#666;\">by " + E(g.OwnerName) + "</span></div>");
            }
            if (found.Count == 0) sb.Append("<p>No groups found.</p>");
            sb.Append("</div><br />");

            sb.Append("<div class=\"StandardBoxHeader\"><span>Create a Group</span></div><div class=\"StandardBox\" style=\"padding:8px;\">"
                + "<p>Name: <input type=\"text\" name=\"gname\" maxlength=\"50\" style=\"width:300px;\" /></p>"
                + "<p>Description:<br /><textarea name=\"gdesc\" rows=\"4\" style=\"width:500px;\"></textarea></p>"
                + Btn("Create", "create") + "</div>");
            return sb.Append("</div>").ToString();
        }
    }
}
