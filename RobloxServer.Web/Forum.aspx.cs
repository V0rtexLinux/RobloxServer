using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using RobloxServer.Data;
using RobloxServer.Web;

namespace RobloxServer.Pages
{
    /// <summary>
    /// Forum: Forum.aspx (index), ?f=ID (forum), ?t=ID&amp;p=N (thread), ?q=text (search).
    /// Sem controles de servidor: todo HTML sai de Html e os formularios usam campos simples dentro do form da master.
    /// </summary>
    public class Forum : BasePage
    {
        const int PageSize = 15;
        const string Style = "<style>.fm{font-family:Arial,Helvetica,sans-serif;font-size:13px;color:#000;max-width:980px;margin:10px auto}.fm a{color:#0066cc;text-decoration:none}.fm a:hover{text-decoration:underline}.fm-side{float:left;width:190px;padding-right:20px}.fm-main{float:left;width:760px}.fm-bar{text-align:right;margin-bottom:8px}.fm-bar a{margin-left:14px}.fm-t{width:100%;border-collapse:collapse}.fm-t th{font-weight:bold;padding:4px 6px;text-align:center}.fm-t td{padding:5px 6px;vertical-align:top}.fm-t td.num{text-align:center;width:90px}.fm-t td.last{text-align:center;width:160px}.fm-cat{font-weight:bold;padding:12px 6px 2px 6px}.fm-err{background:#fde8e8;border:1px solid #d99;padding:6px 8px;margin-bottom:8px}.fm-post{border:1px solid #ccc;margin-bottom:10px;overflow:hidden}.fm-who{float:left;width:150px;padding:8px;background:#f3f3f3;min-height:60px}.fm-what{margin-left:166px;padding:8px;word-wrap:break-word}.fm textarea,.fm input.fm-in{width:100%;box-sizing:border-box;margin-bottom:6px}.fm h2{font-size:16px;margin:8px 0}.fm-crumb{margin-bottom:8px}.fm-pager{margin:8px 0}.fm-adm{margin:6px 0}</style>";

        protected string Html = "";

        protected void Page_Load(object sender, EventArgs e)
        {
            ForumStore.EnsureSeed();
            string error = null;
            if (IsPostBack)
            {
                bool redirected;
                error = HandlePost(out redirected);
                if (redirected)
                {
                    return;
                }
            }
            Html = RenderPage(error);
        }

        // ---------- helpers
        static string E(string s)
        {
            return HttpUtility.HtmlEncode(s ?? "");
        }

        static string BodyHtml(string s)
        {
            return E(s).Replace("\r\n", "\n").Replace("\n", "<br />");
        }

        static long ToLong(string s)
        {
            long v;
            long.TryParse(s ?? "", out v);
            return v;
        }

        string Url(string query)
        {
            return ResolveUrl("~/Forum.aspx") + query;
        }

        static string Stamp(DateTime utc)
        {
            DateTime local = DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime();
            string time = local.ToString("hh:mm tt", CultureInfo.InvariantCulture);
            return local.Date == DateTime.Now.Date
                ? "Today @ " + time
                : local.ToString("MMM d, yyyy", CultureInfo.InvariantCulture) + " @ " + time;
        }

        string Hidden(User user)
        {
            return user == null ? "" : "<input type=\"hidden\" name=\"ftok\" value=\"" + ForumStore.Token(user) + "\" />";
        }

        // ---------- actions (POST)
        string HandlePost(out bool redirected)
        {
            redirected = false;
            User user = RequireLogin();
            if (user == null)
            {
                return "Please log in.";
            }
            if (Request.Form["ftok"] != ForumStore.Token(user))
            {
                return "Your session expired. Reload the page and try again.";
            }

            string act = Request.Form["act"] ?? "";
            int colon = act.IndexOf(':');
            string verb = colon < 0 ? act : act.Substring(0, colon);
            long id = colon < 0 ? 0 : ToLong(act.Substring(colon + 1));
            string next = null;
            string error = null;

            switch (verb)
            {
                case "newthread":
                    error = NewThread(user, id, out next);
                    break;
                case "reply":
                    error = Reply(user, id, out next);
                    break;
                case "delthread":
                case "delpost":
                case "lock":
                case "pin":
                    error = IsAdmin ? Moderate(verb, id, out next) : "Not allowed.";
                    break;
                default:
                    error = "Unknown action.";
                    break;
            }

            if (error == null && next != null)
            {
                Response.Redirect(next, false);
                Context.ApplicationInstance.CompleteRequest();
                redirected = true;
            }
            return error;
        }

        string TooFast(User user)
        {
            DateTime last = DateTime.MinValue;
            foreach (ForumPost p in ForumStore.Posts.Where(x => x.AuthorId == user.Id))
            {
                if (p.Created > last)
                {
                    last = p.Created;
                }
            }
            return (DateTime.UtcNow - last).TotalSeconds < 15 ? "Slow down: wait a few seconds between posts." : null;
        }

        string NewThread(User user, long boardId, out string next)
        {
            next = null;
            ForumBoard board = ForumStore.Boards.Find(b => b.Id == boardId);
            string title = (Request.Form["title"] ?? "").Trim();
            string body = (Request.Form["body"] ?? "").Trim();
            if (board == null) { return "Forum not found."; }
            if (title.Length < 3 || title.Length > 120) { return "The title must have 3 to 120 characters."; }
            if (body.Length < 1 || body.Length > 5000) { return "The message must have 1 to 5000 characters."; }
            string wait = TooFast(user);
            if (wait != null) { return wait; }

            DateTime now = DateTime.UtcNow;
            ForumThread thread = ForumStore.Threads.InsertWithId(x => x.Id, 1, id => new ForumThread
            {
                Id = id, BoardId = board.Id, AuthorId = user.Id, AuthorName = user.Name, Title = title,
                Created = now, LastPostAt = now, LastPostBy = user.Name
            });
            ForumStore.Posts.InsertWithId(x => x.Id, 1, id => new ForumPost
            {
                Id = id, ThreadId = thread.Id, AuthorId = user.Id, AuthorName = user.Name, Body = body, Created = now
            });
            next = Url("?t=" + thread.Id);
            return null;
        }

        string Reply(User user, long threadId, out string next)
        {
            next = null;
            ForumThread thread = ForumStore.Threads.Find(t => t.Id == threadId);
            string body = (Request.Form["body"] ?? "").Trim();
            if (thread == null) { return "Thread not found."; }
            if (thread.Locked && !IsAdmin) { return "This thread is locked."; }
            if (body.Length < 1 || body.Length > 5000) { return "The message must have 1 to 5000 characters."; }
            string wait = TooFast(user);
            if (wait != null) { return wait; }

            DateTime now = DateTime.UtcNow;
            ForumStore.Posts.InsertWithId(x => x.Id, 1, id => new ForumPost
            {
                Id = id, ThreadId = thread.Id, AuthorId = user.Id, AuthorName = user.Name, Body = body, Created = now
            });
            ForumStore.Threads.Update(t => t.Id == thread.Id, t =>
            {
                t.Replies++;
                t.LastPostAt = now;
                t.LastPostBy = user.Name;
            });
            int pages = (thread.Replies + 1 + PageSize) / PageSize;
            next = Url("?t=" + thread.Id + "&p=" + pages);
            return null;
        }

        string Moderate(string verb, long id, out string next)
        {
            next = null;
            if (verb == "delthread")
            {
                ForumThread thread = ForumStore.Threads.Find(t => t.Id == id);
                if (thread == null) { return "Thread not found."; }
                ForumStore.Posts.Delete(p => p.ThreadId == id);
                ForumStore.Threads.Delete(t => t.Id == id);
                next = Url("?f=" + thread.BoardId);
            }
            else if (verb == "delpost")
            {
                ForumPost post = ForumStore.Posts.Find(p => p.Id == id);
                if (post == null) { return "Post not found."; }
                List<ForumPost> all = ForumStore.Posts.Where(p => p.ThreadId == post.ThreadId).OrderBy(p => p.Created).ThenBy(p => p.Id).ToList();
                if (all[0].Id == post.Id) { return "That is the first post: delete the whole thread instead."; }
                ForumStore.Posts.Delete(p => p.Id == id);
                all.RemoveAll(p => p.Id == id);
                ForumPost last = all[all.Count - 1];
                ForumStore.Threads.Update(t => t.Id == post.ThreadId, t =>
                {
                    t.Replies = all.Count - 1;
                    t.LastPostAt = last.Created;
                    t.LastPostBy = last.AuthorName;
                });
                next = Url("?t=" + post.ThreadId);
            }
            else
            {
                ForumThread thread = ForumStore.Threads.Find(t => t.Id == id);
                if (thread == null) { return "Thread not found."; }
                ForumStore.Threads.Update(t => t.Id == id, t =>
                {
                    if (verb == "lock") { t.Locked = !t.Locked; } else { t.Pinned = !t.Pinned; }
                });
                next = Url("?t=" + id);
            }
            return null;
        }

        // ---------- views (GET)
        string RenderPage(string error)
        {
            var sb = new StringBuilder(Style);
            sb.Append("<div class=\"fm\">");
            sb.Append(TopBar());
            sb.Append("<div class=\"fm-side\">").Append(SearchBox()).Append("</div><div class=\"fm-main\">");
            if (error != null)
            {
                sb.Append("<div class=\"fm-err\">").Append(E(error)).Append("</div>");
            }
            long t = ToLong(Request.QueryString["t"]);
            long f = ToLong(Request.QueryString["f"]);
            string q = (Request.QueryString["q"] ?? "").Trim();
            if (t > 0) { ThreadView(sb, t); }
            else if (f > 0) { BoardView(sb, f); }
            else if (q.Length > 0) { SearchView(sb, q); }
            else { IndexView(sb); }
            sb.Append("</div><div style=\"clear:both\"></div></div>");
            return sb.ToString();
        }

        string TopBar()
        {
            var bar = new StringBuilder("<div class=\"fm-bar\"><a href=\"" + Url("") + "\">Home</a>");
            bar.Append("<a href=\"#\" onclick=\"document.getElementById('fmq').focus();return false;\">Search</a>");
            if (CurrentUser == null)
            {
                bar.Append("<a href=\"" + ResolveUrl("~/Register.aspx") + "\">Register</a><a href=\"" + ResolveUrl("~/Login.aspx") + "\">Login</a>");
            }
            bar.Append("</div><div style=\"text-align:center;margin-bottom:8px\">Current time: ")
               .Append(DateTime.Now.ToString("MMM d, hh:mm tt", CultureInfo.InvariantCulture)).Append("</div>");
            return bar.ToString();
        }

        string SearchBox()
        {
            return "<div style=\"font-weight:bold;margin:8px 0\">Search Forums</div>"
                + "<input id=\"fmq\" type=\"text\" class=\"fm-in\" maxlength=\"60\" onkeydown=\"if(event.keyCode==13){fmGo();return false;}\" />"
                + "<button type=\"button\" onclick=\"fmGo()\">Search</button>"
                + "<script>function fmGo(){var v=document.getElementById('fmq').value;if(v){location.href='" + Url("?q=") + "'+encodeURIComponent(v);}}</script>";
        }

        void IndexView(StringBuilder sb)
        {
            List<ForumThread> threads = ForumStore.Threads.All();
            List<ForumBoard> boards = ForumStore.Boards.All();
            sb.Append("<table class=\"fm-t\"><tr><th style=\"text-align:left\">Forum</th><th>Threads</th><th>Posts</th><th>Last Post</th></tr>");
            foreach (ForumCategory c in ForumStore.Categories.All().OrderBy(x => x.Order))
            {
                sb.Append("<tr><td colspan=\"4\" class=\"fm-cat\">").Append(E(c.Name)).Append("</td></tr>");
                foreach (ForumBoard b in boards.Where(x => x.CategoryId == c.Id).OrderBy(x => x.Order))
                {
                    List<ForumThread> mine = threads.Where(x => x.BoardId == b.Id).ToList();
                    int posts = mine.Count + mine.Sum(x => x.Replies);
                    ForumThread last = mine.OrderByDescending(x => x.LastPostAt).FirstOrDefault();
                    sb.Append("<tr><td><a href=\"").Append(Url("?f=" + b.Id)).Append("\">").Append(E(b.Name)).Append("</a><br />")
                      .Append(E(b.Description)).Append("</td><td class=\"num\">").Append(mine.Count.ToString("N0", CultureInfo.InvariantCulture))
                      .Append("</td><td class=\"num\">").Append(posts.ToString("N0", CultureInfo.InvariantCulture)).Append("</td><td class=\"last\">");
                    if (last != null)
                    {
                        sb.Append("<b>").Append(Stamp(last.LastPostAt)).Append("</b><br />by ").Append(E(last.LastPostBy));
                    }
                    sb.Append("</td></tr>");
                }
            }
            sb.Append("</table>");
        }

        void Pager(StringBuilder sb, string baseQuery, int page, int total)
        {
            int pages = Math.Max(1, (total + PageSize - 1) / PageSize);
            if (pages == 1) { return; }
            sb.Append("<div class=\"fm-pager\">Pages: ");
            for (int i = 1; i <= pages; i++)
            {
                if (i == page) { sb.Append("<b>").Append(i).Append("</b> "); }
                else { sb.Append("<a href=\"").Append(Url(baseQuery + "&p=" + i)).Append("\">").Append(i).Append("</a> "); }
            }
            sb.Append("</div>");
        }

        int PageParam()
        {
            return (int)Math.Max(1, Math.Min(100000, ToLong(Request.QueryString["p"])));
        }

        void BoardView(StringBuilder sb, long boardId)
        {
            ForumBoard board = ForumStore.Boards.Find(b => b.Id == boardId);
            if (board == null) { sb.Append("<p>Forum not found.</p>"); return; }
            List<ForumThread> threads = ForumStore.Threads.Where(t => t.BoardId == boardId)
                .OrderByDescending(t => t.Pinned).ThenByDescending(t => t.LastPostAt).ToList();
            int page = PageParam();

            sb.Append("<div class=\"fm-crumb\"><a href=\"").Append(Url("")).Append("\">Forum</a> &raquo; <b>").Append(E(board.Name)).Append("</b></div>");
            sb.Append("<table class=\"fm-t\"><tr><th style=\"text-align:left\">Thread</th><th>Author</th><th>Replies</th><th>Last Post</th></tr>");
            foreach (ForumThread t in threads.Skip((page - 1) * PageSize).Take(PageSize))
            {
                sb.Append("<tr><td>").Append(t.Pinned ? "[Pinned] " : "").Append(t.Locked ? "[Locked] " : "")
                  .Append("<a href=\"").Append(Url("?t=" + t.Id)).Append("\">").Append(E(t.Title)).Append("</a></td><td class=\"num\">")
                  .Append(E(t.AuthorName)).Append("</td><td class=\"num\">").Append(t.Replies)
                  .Append("</td><td class=\"last\"><b>").Append(Stamp(t.LastPostAt)).Append("</b><br />by ").Append(E(t.LastPostBy)).Append("</td></tr>");
            }
            if (threads.Count == 0) { sb.Append("<tr><td colspan=\"4\">No threads yet. Be the first!</td></tr>"); }
            sb.Append("</table>");
            Pager(sb, "?f=" + boardId, page, threads.Count);

            User user = CurrentUser;
            if (user == null)
            {
                sb.Append("<p><a href=\"" + ResolveUrl("~/Login.aspx") + "\">Log in</a> to start a thread.</p>");
                return;
            }
            sb.Append("<h2>New thread</h2>").Append(Hidden(user))
              .Append("<input type=\"text\" name=\"title\" class=\"fm-in\" maxlength=\"120\" placeholder=\"Title\" />")
              .Append("<textarea name=\"body\" rows=\"7\" maxlength=\"5000\" placeholder=\"Message\"></textarea>")
              .Append("<button type=\"submit\" name=\"act\" value=\"newthread:").Append(boardId).Append("\">Post thread</button>");
        }

        void ThreadView(StringBuilder sb, long threadId)
        {
            ForumThread thread = ForumStore.Threads.Find(t => t.Id == threadId);
            if (thread == null) { sb.Append("<p>Thread not found.</p>"); return; }
            ForumBoard board = ForumStore.Boards.Find(b => b.Id == thread.BoardId);
            List<ForumPost> posts = ForumStore.Posts.Where(p => p.ThreadId == threadId).OrderBy(p => p.Created).ThenBy(p => p.Id).ToList();
            int page = PageParam();
            User user = CurrentUser;

            sb.Append("<div class=\"fm-crumb\"><a href=\"").Append(Url("")).Append("\">Forum</a>");
            if (board != null)
            {
                sb.Append(" &raquo; <a href=\"").Append(Url("?f=" + board.Id)).Append("\">").Append(E(board.Name)).Append("</a>");
            }
            sb.Append("</div><h2>").Append(thread.Pinned ? "[Pinned] " : "").Append(thread.Locked ? "[Locked] " : "").Append(E(thread.Title)).Append("</h2>");
            if (user != null) { sb.Append(Hidden(user)); }
            if (IsAdmin)
            {
                sb.Append("<div class=\"fm-adm\"><button type=\"submit\" name=\"act\" value=\"lock:").Append(threadId).Append("\">").Append(thread.Locked ? "Unlock" : "Lock")
                  .Append("</button> <button type=\"submit\" name=\"act\" value=\"pin:").Append(threadId).Append("\">").Append(thread.Pinned ? "Unpin" : "Pin")
                  .Append("</button> <button type=\"submit\" name=\"act\" value=\"delthread:").Append(threadId)
                  .Append("\" onclick=\"return confirm('Delete this whole thread?');\">Delete thread</button></div>");
            }
            Pager(sb, "?t=" + threadId, page, posts.Count);
            int first = (page - 1) * PageSize;
            foreach (ForumPost p in posts.Skip(first).Take(PageSize))
            {
                sb.Append("<div class=\"fm-post\"><div class=\"fm-who\"><b>").Append(E(p.AuthorName)).Append("</b><br />").Append(Stamp(p.Created)).Append("</div><div class=\"fm-what\">")
                  .Append(BodyHtml(p.Body));
                if (IsAdmin && posts[0].Id != p.Id)
                {
                    sb.Append("<div class=\"fm-adm\"><button type=\"submit\" name=\"act\" value=\"delpost:").Append(p.Id)
                      .Append("\" onclick=\"return confirm('Delete this post?');\">Delete post</button></div>");
                }
                sb.Append("</div></div>");
            }
            Pager(sb, "?t=" + threadId, page, posts.Count);

            if (user == null)
            {
                sb.Append("<p><a href=\"" + ResolveUrl("~/Login.aspx") + "\">Log in</a> to reply.</p>");
            }
            else if (thread.Locked && !IsAdmin)
            {
                sb.Append("<p>This thread is locked.</p>");
            }
            else
            {
                sb.Append("<h2>Reply</h2><textarea name=\"body\" rows=\"6\" maxlength=\"5000\"></textarea>")
                  .Append("<button type=\"submit\" name=\"act\" value=\"reply:").Append(threadId).Append("\">Post reply</button>");
            }
        }

        void SearchView(StringBuilder sb, string q)
        {
            if (q.Length > 60) { q = q.Substring(0, 60); }
            HashSet<long> ids = new HashSet<long>(ForumStore.Posts.Where(p => p.Body != null && p.Body.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0).Select(p => p.ThreadId));
            List<ForumThread> found = ForumStore.Threads
                .Where(t => ids.Contains(t.Id) || (t.Title != null && t.Title.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0))
                .OrderByDescending(t => t.LastPostAt).Take(50).ToList();
            sb.Append("<h2>Search: ").Append(E(q)).Append("</h2><table class=\"fm-t\"><tr><th style=\"text-align:left\">Thread</th><th>Author</th><th>Replies</th><th>Last Post</th></tr>");
            foreach (ForumThread t in found)
            {
                sb.Append("<tr><td><a href=\"").Append(Url("?t=" + t.Id)).Append("\">").Append(E(t.Title)).Append("</a></td><td class=\"num\">")
                  .Append(E(t.AuthorName)).Append("</td><td class=\"num\">").Append(t.Replies)
                  .Append("</td><td class=\"last\"><b>").Append(Stamp(t.LastPostAt)).Append("</b></td></tr>");
            }
            if (found.Count == 0) { sb.Append("<tr><td colspan=\"4\">No results.</td></tr>"); }
            sb.Append("</table>");
        }
    }
}
