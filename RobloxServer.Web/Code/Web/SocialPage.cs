using System;
using System.Globalization;
using System.Web;
using RobloxServer.Data;

namespace RobloxServer.Web
{
    /// <summary>
    /// Base das paginas sociais (Messages, Friends, Inventory, Groups, Trade, Builders Club, Catalog).
    /// Sem controles de servidor: Render() devolve o HTML e os formularios sao campos simples com o
    /// mesmo token anti-CSRF do forum. Acoes chegam em Request.Form["act"] = "verbo" ou "verbo:id".
    /// </summary>
    public abstract class SocialPage : BasePage
    {
        protected string Html = "";
        protected string NextUrl;

        protected virtual bool NeedsLogin { get { return true; } }
        protected abstract string Render(User user);

        /// <summary>Devolve uma mensagem de erro, ou null se deu certo (a pagina recarrega em NextUrl).</summary>
        protected virtual string Handle(string verb, long id, User user)
        {
            return "Unknown action.";
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            User user = NeedsLogin ? RequireLogin() : CurrentUser;
            string error = null;

            if (IsPostBack)
            {
                if (user == null)
                {
                    Response.Redirect("~/Login.aspx?ReturnUrl=" + HttpUtility.UrlEncode(Request.RawUrl), true);
                    return;
                }
                if (Request.Form["ftok"] != ForumStore.Token(user))
                {
                    error = "Your session expired. Reload the page and try again.";
                }
                else
                {
                    string act = Request.Form["act"] ?? "";
                    int colon = act.IndexOf(':');
                    string verb = colon < 0 ? act : act.Substring(0, colon);
                    long id = colon < 0 ? 0 : ToLong(act.Substring(colon + 1));
                    error = Handle(verb, id, user);
                    if (error == null)
                    {
                        Response.Redirect(NextUrl ?? Request.RawUrl, false);
                        Context.ApplicationInstance.CompleteRequest();
                        return;
                    }
                }
            }

            Html = (error == null ? "" : "<div class=\"SystemAlert\" style=\"margin:10px auto;width:700px;\">" + E(error) + "</div>")
                + Render(user);
        }

        protected static string E(string s)
        {
            return HttpUtility.HtmlEncode(s ?? "");
        }

        protected static long ToLong(string s)
        {
            long v;
            long.TryParse(s ?? "", out v);
            return v;
        }

        protected string Hidden(User user)
        {
            return user == null ? "" : "<input type=\"hidden\" name=\"ftok\" value=\"" + ForumStore.Token(user) + "\" />";
        }

        protected static string Btn(string label, string act)
        {
            return "<button type=\"submit\" name=\"act\" value=\"" + E(act) + "\" class=\"Button\">" + E(label) + "</button>";
        }

        protected string Url(string path)
        {
            return ResolveUrl("~/" + path);
        }

        protected string UserLink(long id, string name)
        {
            return "<a href=\"" + Url("User.aspx?id=" + id) + "\">" + E(name) + "</a>";
        }

        protected string AvatarUrl(long userId)
        {
            return Url("Asset/Avatar.ashx?userId=" + userId);
        }

        protected string ItemThumb(CatalogItem item)
        {
            return item != null && !string.IsNullOrEmpty(item.Thumb) ? item.Thumb : Url("Images/Icons/BulletPointArrow.png");
        }

        protected static string Stamp(DateTime utc)
        {
            DateTime local = DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime();
            return local.ToString("MMM d, yyyy | h:mm tt", CultureInfo.InvariantCulture);
        }

        protected int PageNo(string key)
        {
            int p;
            return int.TryParse(Request.QueryString[key], out p) && p > 0 ? p : 1;
        }

        protected static string Pager(string baseUrl, int page, int total, int size)
        {
            int pages = Math.Max(1, (total + size - 1) / size);
            if (pages == 1) return "";
            var sb = new System.Text.StringBuilder("<div class=\"FooterPager\" style=\"text-align:center;margin:8px 0;\">");
            string sep = baseUrl.Contains("?") ? "&" : "?";
            for (int i = 1; i <= pages; i++)
            {
                if (i == page) sb.Append("<b>" + i + "</b> ");
                else sb.Append("<a href=\"" + E(baseUrl + sep + "p=" + i) + "\">" + i + "</a> ");
            }
            return sb.Append("</div>").ToString();
        }
    }
}
