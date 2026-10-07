using System;
using System.Linq;
using System.Text;
using RobloxServer.Data;
using RobloxServer.Web;

namespace RobloxServer.Pages
{
    /// <summary>Friends 2013: Friends.aspx (meus amigos + pedidos), ?id=ID (amigos de outra pessoa).</summary>
    public class Friends : SocialPage
    {
        protected override string Handle(string verb, long id, User user)
        {
            if (verb == "add")
            {
                User target = id > 0 ? Db.FindUser(id) : Db.FindUser((Request.Form["name"] ?? "").Trim());
                if (target == null) return "That user does not exist.";
                if (target.Id == user.Id) return "You cannot be your own friend.";
                Friendship link = SocialStore.FriendLink(user.Id, target.Id);
                if (link != null && link.Accepted) return "You are already friends.";
                if (link != null && link.FromId == target.Id)
                {
                    SocialStore.Friendships.Update(f => f.Id == link.Id, f => f.Accepted = true);
                    return null;
                }
                if (link != null) return "You already sent a friend request to " + target.Name + ".";
                SocialStore.Friendships.InsertWithId(f => f.Id, 1, newId => new Friendship
                {
                    Id = newId, FromId = user.Id, FromName = user.Name, ToId = target.Id, ToName = target.Name, Created = DateTime.UtcNow
                });
                return null;
            }
            if (verb == "accept")
            {
                SocialStore.Friendships.Update(f => f.Id == id && f.ToId == user.Id && !f.Accepted, f => f.Accepted = true);
                return null;
            }
            if (verb == "decline" || verb == "remove")
            {
                SocialStore.Friendships.Delete(f => f.Id == id && (f.ToId == user.Id || f.FromId == user.Id));
                return null;
            }
            return "Unknown action.";
        }

        protected override string Render(User user)
        {
            long otherId = ToLong(Request.QueryString["id"]);
            User owner = otherId > 0 && otherId != user.Id ? Db.FindUser(otherId) : user;
            if (owner == null) owner = user;
            bool mine = owner.Id == user.Id;

            var sb = new StringBuilder("<div id=\"FriendsContainer\" style=\"width:880px;margin:10px auto;\">" + Hidden(user));

            if (mine)
            {
                var requests = SocialStore.Friendships.Where(f => !f.Accepted && f.ToId == user.Id);
                if (requests.Count > 0)
                {
                    sb.Append("<div class=\"StandardBoxHeader\"><span>Friend Requests (" + requests.Count + ")</span></div><div class=\"StandardBox\" style=\"padding:8px;\">");
                    foreach (Friendship f in requests)
                    {
                        sb.Append("<div style=\"margin:4px 0;\"><img src=\"" + AvatarUrl(f.FromId) + "\" width=\"48\" height=\"48\" alt=\"\" style=\"vertical-align:middle;\" /> "
                            + UserLink(f.FromId, f.FromName) + " &nbsp; " + Btn("Accept", "accept:" + f.Id) + " " + Btn("Decline", "decline:" + f.Id) + "</div>");
                    }
                    sb.Append("</div><br />");
                }
            }

            var ids = SocialStore.FriendIds(owner.Id);
            sb.Append("<div class=\"StandardBoxHeader\"><span>" + (mine ? "My Friends" : E(owner.Name) + "'s Friends") + " (" + ids.Count + ")</span></div><div class=\"StandardBox\" style=\"padding:8px;overflow:hidden;\">");
            if (ids.Count == 0) sb.Append("<p>No friends yet.</p>");
            foreach (long fid in ids)
            {
                User f = Db.FindUser(fid);
                if (f == null) continue;
                Friendship link = SocialStore.FriendLink(owner.Id, fid);
                sb.Append("<div style=\"float:left;width:140px;text-align:center;margin:6px;\"><a href=\"" + Url("User.aspx?id=" + f.Id) + "\"><img src=\"" + AvatarUrl(f.Id) + "\" width=\"100\" height=\"100\" alt=\"\" style=\"border:0;\" /></a><br />"
                    + UserLink(f.Id, f.Name) + "<br /><span style=\"color:" + (IsOnline(f) ? "#2a2" : "#888") + ";\">" + (IsOnline(f) ? "Online: Website" : "Offline") + "</span>"
                    + (mine && link != null ? "<br />" + Btn("Remove", "remove:" + link.Id) : "") + "</div>");
            }
            sb.Append("</div>");

            if (mine)
            {
                sb.Append("<br /><div class=\"StandardBoxHeader\"><span>Add a Friend</span></div><div class=\"StandardBox\" style=\"padding:8px;\">"
                    + "Username: <input type=\"text\" name=\"name\" maxlength=\"40\" /> " + Btn("Send request", "add") + "</div>");
            }
            return sb.Append("</div>").ToString();
        }
    }
}
