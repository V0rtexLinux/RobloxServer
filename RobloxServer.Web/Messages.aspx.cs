using System;
using System.Linq;
using System.Text;
using RobloxServer.Data;
using RobloxServer.Security;
using RobloxServer.Web;

namespace RobloxServer.Pages
{
    /// <summary>Inbox 2013: Messages.aspx (caixa de entrada), ?tab=sent, ?id=N (ler), ?compose=1&amp;to=nome.</summary>
    public class Messages : SocialPage
    {
        const int PageSize = 20;

        protected override string Handle(string verb, long id, User user)
        {
            if (verb == "send")
            {
                string to = (Request.Form["to"] ?? "").Trim();
                string subject = (Request.Form["subject"] ?? "").Trim();
                string body = (Request.Form["body"] ?? "").Trim();
                User target = Db.FindUser(to);
                if (target == null) return "There is no user called \"" + to + "\".";
                if (target.Id == user.Id) return "You cannot message yourself.";
                if (body.Length == 0) return "Write a message first.";
                if (subject.Length == 0) subject = "(no subject)";
                if (subject.Length > 100) subject = subject.Substring(0, 100);
                if (body.Length > 2000) body = body.Substring(0, 2000);
                if (FloodChecker.Hit("pm:" + user.Id, 10, TimeSpan.FromMinutes(1))) return "You are sending messages too fast.";

                SocialStore.Messages.InsertWithId(m => m.Id, 1, newId => new PrivateMessage
                {
                    Id = newId, FromId = user.Id, FromName = user.Name, ToId = target.Id, ToName = target.Name,
                    Subject = WordFilter.Filter(subject), Body = WordFilter.Filter(body), Created = DateTime.UtcNow
                });
                NextUrl = Url("Messages.aspx?tab=sent");
                return null;
            }
            if (verb == "delete")
            {
                bool sent = Request.QueryString["tab"] == "sent";
                foreach (string raw in Request.Form.GetValues("sel") ?? new string[0])
                {
                    long mid = ToLong(raw);
                    if (sent) SocialStore.Messages.Update(m => m.Id == mid && m.FromId == user.Id, m => m.DeletedBySender = true);
                    else SocialStore.Messages.Update(m => m.Id == mid && m.ToId == user.Id, m => m.DeletedByRecipient = true);
                }
                return null;
            }
            return "Unknown action.";
        }

        protected override string Render(User user)
        {
            var sb = new StringBuilder("<div id=\"InboxContainer\" style=\"width:700px;margin:10px auto;\">");
            string tab = Request.QueryString["tab"] == "sent" ? "sent" : "inbox";
            sb.Append("<div id=\"Tabs\"><a class=\"" + (tab == "inbox" && Request.QueryString["compose"] == null ? "contesttabselected" : "contesttab") + "\" href=\"" + Url("Messages.aspx") + "\">Inbox (" + SocialStore.Unread(user.Id) + ")</a>&nbsp;"
                + "<a class=\"" + (tab == "sent" ? "contesttabselected" : "contesttab") + "\" href=\"" + Url("Messages.aspx?tab=sent") + "\">Sent</a>&nbsp;"
                + "<a class=\"contesttab\" href=\"" + Url("Messages.aspx?compose=1") + "\">Compose</a></div>");
            sb.Append("<div class=\"StandardBox\" style=\"width:700px;padding:6px;\">");

            long readId = ToLong(Request.QueryString["id"]);
            if (Request.QueryString["compose"] != null)
            {
                sb.Append(Compose(user));
            }
            else if (readId > 0)
            {
                sb.Append(Read(user, readId));
            }
            else
            {
                sb.Append(List(user, tab));
            }
            return sb.Append("</div></div>").ToString();
        }

        string Compose(User user)
        {
            return "<h3>New message</h3>" + Hidden(user)
                + "<p>To: <input type=\"text\" name=\"to\" value=\"" + E(Request.QueryString["to"]) + "\" maxlength=\"40\" /></p>"
                + "<p>Subject: <input type=\"text\" name=\"subject\" value=\"" + E(Request.QueryString["subject"]) + "\" maxlength=\"100\" style=\"width:400px;\" /></p>"
                + "<p><textarea name=\"body\" rows=\"8\" style=\"width:660px;\"></textarea></p>"
                + "<div class=\"Buttons\">" + Btn("Send", "send") + " <a class=\"Button\" href=\"" + Url("Messages.aspx") + "\">Cancel</a></div>";
        }

        string Read(User user, long id)
        {
            PrivateMessage m = SocialStore.Messages.Find(x => x.Id == id && (x.ToId == user.Id || x.FromId == user.Id));
            if (m == null) return "<p>That message does not exist.</p>";
            if (m.ToId == user.Id && !m.Read) SocialStore.Messages.Update(x => x.Id == id, x => x.Read = true);

            bool incoming = m.ToId == user.Id;
            return "<table style=\"width:100%;\"><tr><td style=\"width:110px;vertical-align:top;text-align:center;\">"
                + "<img src=\"" + AvatarUrl(m.FromId) + "\" width=\"100\" height=\"100\" alt=\"\" /><br />" + UserLink(m.FromId, m.FromName) + "</td>"
                + "<td style=\"vertical-align:top;\"><h3 style=\"margin:0 0 4px 0;\">" + E(m.Subject) + "</h3>"
                + "<div style=\"color:#666;margin-bottom:8px;\">To: " + E(m.ToName) + " &middot; " + Stamp(m.Created) + "</div>"
                + "<div style=\"white-space:pre-wrap;word-wrap:break-word;\">" + E(m.Body) + "</div></td></tr></table>"
                + "<div class=\"Buttons\" style=\"margin-top:10px;\">"
                + (incoming ? "<a class=\"Button\" href=\"" + Url("Messages.aspx?compose=1&to=" + Uri.EscapeDataString(m.FromName) + "&subject=" + Uri.EscapeDataString("RE: " + m.Subject)) + "\">Reply</a> " : "")
                + "<a class=\"Button\" href=\"" + Url("Messages.aspx" + (incoming ? "" : "?tab=sent")) + "\">Back</a></div>";
        }

        string List(User user, string tab)
        {
            bool sent = tab == "sent";
            var all = (sent
                ? SocialStore.Messages.Where(m => m.FromId == user.Id && !m.DeletedBySender)
                : SocialStore.Messages.Where(m => m.ToId == user.Id && !m.DeletedByRecipient))
                .OrderByDescending(m => m.Created).ToList();
            int page = PageNo("p");

            var sb = new StringBuilder(Hidden(user));
            if (all.Count == 0)
            {
                return sb.Append("<div class=\"EmptyInbox\" style=\"text-align:center;padding:20px;\">You have no messages in your " + (sent ? "Sent folder" : "Inbox") + ".</div>").ToString();
            }
            sb.Append("<table cellspacing=\"0\" cellpadding=\"3\" style=\"width:100%;border-collapse:collapse;\"><tr class=\"InboxHeader\"><th style=\"width:28px;\"></th><th align=\"left\">" + (sent ? "To" : "From") + "</th><th align=\"left\">Subject</th><th align=\"left\">Date</th></tr>");
            foreach (PrivateMessage m in all.Skip((page - 1) * PageSize).Take(PageSize))
            {
                bool unread = !sent && !m.Read;
                sb.Append("<tr style=\"border-top:1px solid #ddd;" + (unread ? "font-weight:bold;background:#f3f8fd;" : "") + "\">"
                    + "<td><input type=\"checkbox\" name=\"sel\" value=\"" + m.Id + "\" /></td>"
                    + "<td>" + UserLink(sent ? m.ToId : m.FromId, sent ? m.ToName : m.FromName) + "</td>"
                    + "<td><a href=\"" + Url("Messages.aspx?id=" + m.Id) + "\">" + E(m.Subject) + "</a></td>"
                    + "<td>" + Stamp(m.Created) + "</td></tr>");
            }
            sb.Append("</table>");
            sb.Append(Pager(Url("Messages.aspx" + (sent ? "?tab=sent" : "")), page, all.Count, PageSize));
            sb.Append("<div class=\"Buttons\" style=\"text-align:center;\">" + Btn("Delete selected", "delete") + "</div>");
            return sb.ToString();
        }
    }
}
