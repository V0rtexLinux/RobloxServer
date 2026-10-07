using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RobloxServer.Data;
using RobloxServer.Web;

namespace RobloxServer.Pages
{
    /// <summary>
    /// Catalog no estilo de 2013: Catalog.aspx (grade), ?c=Categoria, ?q=busca, ?sort=, ?id=ITEM (detalhe).
    /// Cada item tem o botao "Download .rbxm" (Asset/Rbxm.ashx). Admin: botao "Sync catalog".
    /// </summary>
    public class Catalog : SocialPage
    {
        const int PageSize = 20;

        protected override bool NeedsLogin { get { return false; } }

        protected override string Handle(string verb, long id, User user)
        {
            if (verb == "buy")
            {
                CatalogItem item = CatalogService.Items.Find(i => i.Id == id);
                if (item == null) return "That item does not exist.";
                if (!SocialStore.AddItem(user.Id, item.Id)) return "You already own this item.";
                CatalogService.Items.Update(i => i.Id == id, i => i.Sales++);
                NextUrl = Url("Inventory.aspx");
                return null;
            }
            if (verb == "sync")
            {
                if (!Db.IsAdmin(user)) return "Only admins can sync the catalog.";
                try
                {
                    Server.ScriptTimeout = 900;
                    NextUrl = Url("Catalog.aspx?synced=" + Uri.EscapeDataString(CatalogService.Sync(3)));
                }
                catch (Exception ex)
                {
                    Logging.Log(LogType.Error, "Catalog sync: " + ex.Message);
                    return "Sync failed: " + ex.Message;
                }
                return null;
            }
            return "Unknown action.";
        }

        protected override string Render(User user)
        {
            var sb = new StringBuilder();
            sb.Append("<div id=\"CatalogContainer\" style=\"width:882px;margin:10px auto;font-family:Verdana,Arial,sans-serif;font-size:12px;\">");

            string synced = Request.QueryString["synced"];
            if (!string.IsNullOrEmpty(synced))
            {
                sb.Append("<div class=\"SystemAlert\" style=\"margin-bottom:8px;\">" + E(synced) + "</div>");
            }

            long itemId = ToLong(Request.QueryString["id"]);
            if (itemId > 0)
            {
                CatalogItem item = CatalogService.Items.Find(i => i.Id == itemId);
                sb.Append(item == null ? "<p>This item is not in the catalog.</p>" : RenderItem(item, user));
                return sb.Append("</div>").ToString();
            }

            string cat = Request.QueryString["c"] ?? "";
            string q = (Request.QueryString["q"] ?? "").Trim();
            string sort = Request.QueryString["sort"] ?? "new";

            IEnumerable<CatalogItem> items = CatalogService.Items.All();
            if (cat.Length > 0) items = items.Where(i => string.Equals(i.Category, cat, StringComparison.OrdinalIgnoreCase));
            if (q.Length > 0) items = items.Where(i => (i.Name ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);
            switch (sort)
            {
                case "sales": items = items.OrderByDescending(i => i.Sales); break;
                case "old": items = items.OrderBy(i => i.Created); break;
                case "price": items = items.OrderBy(i => i.Price); break;
                default: items = items.OrderByDescending(i => i.Created); break;
            }
            List<CatalogItem> list = items.ToList();
            int page = PageNo("p");
            int total = CatalogService.Items.All().Count;

            // barra lateral de categorias (como na pagina de 2013)
            sb.Append("<div style=\"float:left;width:180px;\"><div class=\"StandardBoxHeader\"><span>Categories</span></div><div class=\"StandardBox\" style=\"padding:8px;\"><ul style=\"list-style:none;margin:0;padding:0;line-height:20px;\">");
            sb.Append("<li><a href=\"" + Url("Catalog.aspx") + "\">" + (cat.Length == 0 ? "<b>All Items</b>" : "All Items") + "</a></li>");
            foreach (CatalogCategory c in CatalogService.Categories)
            {
                bool on = string.Equals(c.Name, cat, StringComparison.OrdinalIgnoreCase);
                sb.Append("<li><a href=\"" + Url("Catalog.aspx?c=" + Uri.EscapeDataString(c.Name)) + "\">" + (on ? "<b>" : "") + E(c.Name) + (on ? "</b>" : "") + "</a></li>");
            }
            sb.Append("</ul></div></div>");

            sb.Append("<div style=\"margin-left:196px;\"><div class=\"StandardBoxHeader\"><span>Catalog 2007 - 2013</span></div><div class=\"StandardBox\" style=\"padding:8px;\">");
            sb.Append("<div style=\"margin-bottom:8px;\"><input type=\"text\" id=\"cq\" value=\"" + E(q) + "\" /> ");
            sb.Append("<select id=\"cs\"><option value=\"new\"" + Sel(sort, "new") + ">Newest</option><option value=\"old\"" + Sel(sort, "old") + ">Oldest</option><option value=\"sales\"" + Sel(sort, "sales") + ">Best selling</option><option value=\"price\"" + Sel(sort, "price") + ">Price</option></select> ");
            sb.Append("<input type=\"button\" value=\"Search\" onclick=\"location.href='" + Url("Catalog.aspx") + "?c=" + Uri.EscapeDataString(cat) + "&amp;sort='+document.getElementById('cs').value+'&amp;q='+encodeURIComponent(document.getElementById('cq').value);\" /></div>");

            if (total == 0)
            {
                sb.Append("<p>The catalog is empty. " + (user != null && Db.IsAdmin(user)
                    ? "Click <b>Sync catalog</b> below to load every ROBLOX item made between 2007 and 2013."
                    : "An admin needs to sync it first.") + "</p>");
            }
            else if (list.Count == 0)
            {
                sb.Append("<p>No items found.</p>");
            }

            sb.Append("<table cellspacing=\"0\" cellpadding=\"4\" style=\"width:100%;\"><tr>");
            int col = 0;
            foreach (CatalogItem it in list.Skip((page - 1) * PageSize).Take(PageSize))
            {
                if (col > 0 && col % 5 == 0) sb.Append("</tr><tr>");
                sb.Append("<td style=\"width:20%;text-align:center;vertical-align:top;padding:6px;\">");
                sb.Append("<a href=\"" + Url("Catalog.aspx?id=" + it.Id) + "\"><img src=\"" + E(ItemThumb(it)) + "\" width=\"110\" height=\"110\" alt=\"" + E(it.Name) + "\" style=\"border:0;\" /></a><br />");
                sb.Append("<a href=\"" + Url("Catalog.aspx?id=" + it.Id) + "\">" + E(it.Name) + "</a><br />");
                sb.Append("<span style=\"color:#666;\">" + it.Year + " &middot; " + (it.Price > 0 ? "R$ " + it.Price : "Free") + "</span>");
                sb.Append("</td>");
                col++;
            }
            sb.Append("</tr></table>");
            sb.Append(Pager(Url("Catalog.aspx?c=" + Uri.EscapeDataString(cat) + "&q=" + Uri.EscapeDataString(q) + "&sort=" + Uri.EscapeDataString(sort)), page, list.Count, PageSize));
            sb.Append("</div>");

            if (user != null && Db.IsAdmin(user))
            {
                sb.Append("<div style=\"margin-top:10px;\">" + Hidden(user) + Btn("Sync catalog", "sync")
                    + " <span style=\"color:#666;\">Loads ROBLOX items made 2007-2013 from the Roblox public API (can take a few minutes).</span></div>");
            }
            sb.Append("</div><div style=\"clear:both;\"></div>");
            return sb.Append("</div>").ToString();
        }

        static string Sel(string current, string value)
        {
            return current == value ? " selected=\"selected\"" : "";
        }

        string RenderItem(CatalogItem item, User user)
        {
            var sb = new StringBuilder();
            sb.Append("<div class=\"StandardBoxHeader\"><span>" + E(item.Name) + "</span></div><div class=\"StandardBox\" style=\"padding:12px;overflow:hidden;\">");
            sb.Append("<div style=\"float:left;width:200px;text-align:center;\"><img src=\"" + E(ItemThumb(item)) + "\" width=\"150\" height=\"150\" alt=\"\" /></div>");
            sb.Append("<div style=\"margin-left:216px;\">");
            sb.Append("<div><b>Creator:</b> " + E(item.Creator) + "</div>");
            sb.Append("<div><b>Category:</b> <a href=\"" + Url("Catalog.aspx?c=" + Uri.EscapeDataString(item.Category ?? "")) + "\">" + E(item.Category) + "</a></div>");
            sb.Append("<div><b>Created:</b> " + item.Created.ToString("MMM d, yyyy", System.Globalization.CultureInfo.InvariantCulture) + "</div>");
            sb.Append("<div><b>Sales:</b> " + item.Sales + "</div>");
            sb.Append("<div><b>Price:</b> " + (item.Price > 0 ? "R$ " + item.Price + " (free on this server)" : "Free") + "</div>");
            sb.Append("<p>" + E(item.Description) + "</p>");
            sb.Append("<div>" + Hidden(user));
            if (user == null)
            {
                sb.Append("<a class=\"Button\" href=\"" + Url("Login.aspx?ReturnUrl=" + Uri.EscapeDataString(Request.RawUrl)) + "\">Log in to get this item</a> ");
            }
            else if (SocialStore.Owns(user.Id, item.Id))
            {
                sb.Append("<b>You own this item.</b> ");
            }
            else
            {
                sb.Append(Btn("Get this item", "buy:" + item.Id) + " ");
            }
            sb.Append("<a class=\"Button\" href=\"" + Url("Asset/Rbxm.ashx?id=" + item.Id) + "\">Download .rbxm</a>");
            sb.Append("</div></div></div>");
            return sb.ToString();
        }
    }
}
