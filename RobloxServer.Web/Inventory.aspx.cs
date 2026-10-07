using System;
using System.Linq;
using System.Text;
using RobloxServer.Data;
using RobloxServer.Web;

namespace RobloxServer.Pages
{
    /// <summary>My Stuff 2013: Inventory.aspx?c=Categoria, ?id=ID (ver o de outra pessoa).</summary>
    public class Inventory : SocialPage
    {
        const int PageSize = 20;

        protected override string Handle(string verb, long id, User user)
        {
            if (verb == "remove")
            {
                SocialStore.RemoveItem(user.Id, id);
                return null;
            }
            return "Unknown action.";
        }

        protected override string Render(User user)
        {
            long otherId = ToLong(Request.QueryString["id"]);
            User owner = otherId > 0 ? Db.FindUser(otherId) : user;
            if (owner == null) owner = user;
            bool mine = owner.Id == user.Id;
            string cat = Request.QueryString["c"] ?? "";

            var entries = SocialStore.Inventory.Where(i => i.UserId == owner.Id).OrderByDescending(i => i.Added).ToList();
            var rows = entries.Select(e => new { Entry = e, Item = CatalogService.Items.Find(c => c.Id == e.ItemId) })
                .Where(r => r.Item != null && (cat.Length == 0 || string.Equals(r.Item.Category, cat, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            string own = owner.Id == user.Id ? "" : "&id=" + owner.Id;
            int page = PageNo("p");

            var sb = new StringBuilder("<div id=\"UserContainer\" style=\"width:880px;margin:10px auto;\">" + Hidden(user));
            sb.Append("<div id=\"UserAssetsPane\"><div class=\"StandardBoxHeader\"><span>" + (mine ? "My Stuff" : E(owner.Name) + "'s Stuff") + "</span></div>");
            sb.Append("<div id=\"UserAssets\" class=\"StandardBox\" style=\"overflow:hidden;padding:8px;\">");
            sb.Append("<div id=\"AssetsMenu\" style=\"float:left;width:170px;\"><ul style=\"list-style:none;margin:0;padding:0;line-height:22px;\">");
            sb.Append("<li><a href=\"" + Url("Inventory.aspx" + (own.Length > 0 ? "?" + own.Substring(1) : "")) + "\">" + (cat.Length == 0 ? "<b>Everything</b>" : "Everything") + "</a></li>");
            foreach (CatalogCategory c in CatalogService.Categories)
            {
                bool on = string.Equals(c.Name, cat, StringComparison.OrdinalIgnoreCase);
                sb.Append("<li><a href=\"" + Url("Inventory.aspx?c=" + Uri.EscapeDataString(c.Name) + own) + "\">" + (on ? "<b>" : "") + E(c.Name) + (on ? "</b>" : "") + "</a></li>");
            }
            sb.Append("</ul></div><div id=\"AssetsContent\" style=\"margin-left:186px;\">");

            if (rows.Count == 0)
            {
                sb.Append("<p>" + (mine ? "You don't have anything here yet. Get items from the <a href=\"" + Url("Catalog.aspx") + "\">Catalog</a>." : "Nothing to show.") + "</p>");
            }
            sb.Append("<table cellspacing=\"0\" style=\"width:100%;\"><tr>");
            int col = 0;
            foreach (var r in rows.Skip((page - 1) * PageSize).Take(PageSize))
            {
                if (col > 0 && col % 4 == 0) sb.Append("</tr><tr>");
                sb.Append("<td style=\"width:25%;text-align:center;vertical-align:top;padding:6px;\">"
                    + "<a href=\"" + Url("Catalog.aspx?id=" + r.Item.Id) + "\"><img src=\"" + E(ItemThumb(r.Item)) + "\" width=\"110\" height=\"110\" alt=\"\" style=\"border:0;\" /></a><br />"
                    + "<a href=\"" + Url("Catalog.aspx?id=" + r.Item.Id) + "\">" + E(r.Item.Name) + "</a><br />"
                    + "<a href=\"" + Url("Asset/Rbxm.ashx?id=" + r.Item.Id) + "\">Download .rbxm</a>"
                    + (mine ? "<br />" + Btn("Delete", "remove:" + r.Item.Id) : "") + "</td>");
                col++;
            }
            sb.Append("</tr></table>");
            sb.Append(Pager(Url("Inventory.aspx?c=" + Uri.EscapeDataString(cat) + own), page, rows.Count, PageSize));
            sb.Append("</div></div></div></div>");
            return sb.ToString();
        }
    }
}
