using System;
using System.Linq;
using System.Text;
using RobloxServer.Data;
using RobloxServer.Web;

namespace RobloxServer.Pages
{
    /// <summary>Trade 2013 (1 item por 1 item): Trade.aspx, ?with=nome para montar uma oferta.</summary>
    public class Trade : SocialPage
    {
        protected override string Handle(string verb, long id, User user)
        {
            if (verb == "offer")
            {
                User target = Db.FindUser((Request.Form["who"] ?? "").Trim());
                long give = ToLong(Request.Form["give"]);
                long want = ToLong(Request.Form["want"]);
                if (target == null || target.Id == user.Id) return "Pick another user to trade with.";
                if (!SocialStore.Owns(user.Id, give)) return "You do not own the item you are offering.";
                if (!SocialStore.Owns(target.Id, want)) return target.Name + " does not own that item.";
                if (SocialStore.Owns(user.Id, want) || SocialStore.Owns(target.Id, give)) return "One of you already owns the item you would receive.";
                SocialStore.Trades.InsertWithId(t => t.Id, 1, newId => new TradeOffer
                {
                    Id = newId, FromId = user.Id, FromName = user.Name, ToId = target.Id, ToName = target.Name,
                    OfferItemId = give, WantItemId = want, Status = "Open", Created = DateTime.UtcNow
                });
                NextUrl = Url("Trade.aspx");
                return null;
            }
            if (verb == "accept") return SocialStore.AcceptTrade(id, user.Id);
            if (verb == "decline")
            {
                SocialStore.Trades.Update(t => t.Id == id && t.ToId == user.Id && t.Status == "Open", t => t.Status = "Declined");
                return null;
            }
            if (verb == "cancel")
            {
                SocialStore.Trades.Update(t => t.Id == id && t.FromId == user.Id && t.Status == "Open", t => t.Status = "Cancelled");
                return null;
            }
            return "Unknown action.";
        }

        string ItemName(long id)
        {
            CatalogItem item = CatalogService.Items.Find(i => i.Id == id);
            return item == null ? "Item " + id : "<a href=\"" + Url("Catalog.aspx?id=" + id) + "\">" + E(item.Name) + "</a>";
        }

        string Options(long userId)
        {
            var sb = new StringBuilder();
            foreach (InventoryEntry e in SocialStore.Inventory.Where(i => i.UserId == userId))
            {
                CatalogItem item = CatalogService.Items.Find(i => i.Id == e.ItemId);
                if (item != null) sb.Append("<option value=\"" + item.Id + "\">" + E(item.Name) + "</option>");
            }
            return sb.ToString();
        }

        protected override string Render(User user)
        {
            var sb = new StringBuilder("<div id=\"TradeContainer\" style=\"width:880px;margin:10px auto;\">" + Hidden(user));

            string with = (Request.QueryString["with"] ?? "").Trim();
            User other = with.Length > 0 ? Db.FindUser(with) : null;
            sb.Append("<div class=\"StandardBoxHeader\"><span>New Trade</span></div><div class=\"StandardBox\" style=\"padding:8px;\">");
            if (other == null || other.Id == user.Id)
            {
                sb.Append("Trade with: <input type=\"text\" id=\"tw\" value=\"" + E(with) + "\" /> <input type=\"button\" value=\"Next\" onclick=\"location.href='" + Url("Trade.aspx") + "?with='+encodeURIComponent(document.getElementById('tw').value);\" />");
                if (with.Length > 0) sb.Append("<p>No such user.</p>");
            }
            else
            {
                sb.Append("<input type=\"hidden\" name=\"who\" value=\"" + E(other.Name) + "\" />"
                    + "<p>You give: <select name=\"give\">" + Options(user.Id) + "</select></p>"
                    + "<p>You want from " + E(other.Name) + ": <select name=\"want\">" + Options(other.Id) + "</select></p>"
                    + Btn("Send trade", "offer"));
            }
            sb.Append("</div><br />");

            var trades = SocialStore.Trades.Where(t => t.FromId == user.Id || t.ToId == user.Id).OrderByDescending(t => t.Created).Take(40).ToList();
            sb.Append("<div class=\"StandardBoxHeader\"><span>My Trades</span></div><div class=\"StandardBox\" style=\"padding:8px;\">");
            if (trades.Count == 0) sb.Append("<p>No trades yet.</p>");
            foreach (TradeOffer t in trades)
            {
                bool incoming = t.ToId == user.Id;
                sb.Append("<div style=\"margin:6px 0;border-bottom:1px solid #ddd;padding-bottom:6px;\">"
                    + (incoming ? UserLink(t.FromId, t.FromName) + " offers you " + ItemName(t.OfferItemId) + " for your " + ItemName(t.WantItemId)
                                : "You offered " + ItemName(t.OfferItemId) + " to " + UserLink(t.ToId, t.ToName) + " for " + ItemName(t.WantItemId))
                    + " &middot; <b>" + t.Status + "</b> &middot; " + Stamp(t.Created));
                if (t.Status == "Open")
                {
                    sb.Append("<br />" + (incoming ? Btn("Accept", "accept:" + t.Id) + " " + Btn("Decline", "decline:" + t.Id) : Btn("Cancel", "cancel:" + t.Id)));
                }
                sb.Append("</div>");
            }
            return sb.Append("</div></div>").ToString();
        }
    }
}
