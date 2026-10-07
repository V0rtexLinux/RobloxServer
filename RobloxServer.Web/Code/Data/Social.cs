using System;
using System.Collections.Generic;
using System.Linq;

namespace RobloxServer.Data
{
    /// <summary>Pedido de amizade (Accepted=false) ou amizade (Accepted=true). Uma linha por par.</summary>
    public class Friendship
    {
        public long Id { get; set; }
        public long FromId { get; set; }
        public string FromName { get; set; }
        public long ToId { get; set; }
        public string ToName { get; set; }
        public bool Accepted { get; set; }
        public DateTime Created { get; set; }
    }

    public class PrivateMessage
    {
        public long Id { get; set; }
        public long FromId { get; set; }
        public string FromName { get; set; }
        public long ToId { get; set; }
        public string ToName { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public DateTime Created { get; set; }
        public bool Read { get; set; }
        public bool DeletedBySender { get; set; }
        public bool DeletedByRecipient { get; set; }
    }

    public class Group
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public long OwnerId { get; set; }
        public string OwnerName { get; set; }
        public DateTime Created { get; set; }
    }

    public class GroupMember
    {
        public long GroupId { get; set; }
        public long UserId { get; set; }
        public string UserName { get; set; }
        public DateTime Joined { get; set; }
    }

    /// <summary>Um item do catalogo (CatalogService) que o usuario tem no "My Stuff".</summary>
    public class InventoryEntry
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public long ItemId { get; set; }
        public DateTime Added { get; set; }
    }

    public class TradeOffer
    {
        public long Id { get; set; }
        public long FromId { get; set; }
        public string FromName { get; set; }
        public long ToId { get; set; }
        public string ToName { get; set; }
        public long OfferItemId { get; set; }
        public long WantItemId { get; set; }
        public string Status { get; set; } // Open, Accepted, Declined, Cancelled
        public DateTime Created { get; set; }
    }

    public class BuildersClubMember
    {
        public long UserId { get; set; }
        public string Tier { get; set; } // BC, TBC, OBC
        public DateTime Since { get; set; }
    }

    public static class SocialStore
    {
        public static readonly XmlTable<Friendship> Friendships = new XmlTable<Friendship>("Friendships.xml");
        public static readonly XmlTable<PrivateMessage> Messages = new XmlTable<PrivateMessage>("Messages.xml");
        public static readonly XmlTable<Group> Groups = new XmlTable<Group>("Groups.xml");
        public static readonly XmlTable<GroupMember> GroupMembers = new XmlTable<GroupMember>("GroupMembers.xml");
        public static readonly XmlTable<InventoryEntry> Inventory = new XmlTable<InventoryEntry>("Inventory.xml");
        public static readonly XmlTable<TradeOffer> Trades = new XmlTable<TradeOffer>("Trades.xml");
        public static readonly XmlTable<BuildersClubMember> BuildersClub = new XmlTable<BuildersClubMember>("BuildersClub.xml");

        static readonly object InventoryLock = new object();

        // ---- friends
        public static Friendship FriendLink(long a, long b)
        {
            return Friendships.Find(f => (f.FromId == a && f.ToId == b) || (f.FromId == b && f.ToId == a));
        }

        public static List<long> FriendIds(long userId)
        {
            return Friendships.Where(f => f.Accepted && (f.FromId == userId || f.ToId == userId))
                .Select(f => f.FromId == userId ? f.ToId : f.FromId).ToList();
        }

        // ---- messages
        public static int Unread(long userId)
        {
            return Messages.Where(m => m.ToId == userId && !m.Read && !m.DeletedByRecipient).Count;
        }

        // ---- inventory
        public static bool Owns(long userId, long itemId)
        {
            return Inventory.Find(i => i.UserId == userId && i.ItemId == itemId) != null;
        }

        public static bool AddItem(long userId, long itemId)
        {
            lock (InventoryLock)
            {
                if (Owns(userId, itemId))
                {
                    return false;
                }
                Inventory.InsertWithId(i => i.Id, 1, id => new InventoryEntry { Id = id, UserId = userId, ItemId = itemId, Added = DateTime.UtcNow });
                return true;
            }
        }

        public static void RemoveItem(long userId, long itemId)
        {
            lock (InventoryLock)
            {
                Inventory.Delete(i => i.UserId == userId && i.ItemId == itemId);
            }
        }

        // ---- trade (troca 1 por 1, atomica)
        public static string AcceptTrade(long tradeId, long userId)
        {
            lock (InventoryLock)
            {
                TradeOffer t = Trades.Find(x => x.Id == tradeId);
                if (t == null || t.Status != "Open") { return "This trade is no longer open."; }
                if (t.ToId != userId) { return "This trade was not sent to you."; }
                if (!Owns(t.FromId, t.OfferItemId) || !Owns(t.ToId, t.WantItemId)) { return "One of the items is no longer available."; }
                if (Owns(t.ToId, t.OfferItemId) || Owns(t.FromId, t.WantItemId)) { return "Someone already owns the item they would receive."; }

                Inventory.Update(i => i.UserId == t.FromId && i.ItemId == t.OfferItemId, i => i.UserId = t.ToId);
                Inventory.Update(i => i.UserId == t.ToId && i.ItemId == t.WantItemId, i => i.UserId = t.FromId);
                Trades.Update(x => x.Id == tradeId, x => x.Status = "Accepted");
                return null;
            }
        }

        // ---- Builders Club
        public static string Tier(long userId)
        {
            BuildersClubMember m = BuildersClub.Find(x => x.UserId == userId);
            return m == null ? "None" : m.Tier;
        }

        public static string TierName(string tier)
        {
            switch (tier)
            {
                case "BC": return "Builders Club";
                case "TBC": return "Turbo Builders Club";
                case "OBC": return "Outrageous Builders Club";
                default: return "Free";
            }
        }

        /// <summary>Quantos grupos a pessoa pode participar (limite do servidor, por categoria de membro).</summary>
        public static int GroupLimit(long userId)
        {
            switch (Tier(userId))
            {
                case "BC": return 10;
                case "TBC": return 20;
                case "OBC": return 100;
                default: return 5;
            }
        }
    }
}
