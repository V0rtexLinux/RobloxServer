using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace RobloxServer.Data
{
    public class ForumCategory
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public int Order { get; set; }
    }

    public class ForumBoard
    {
        public long Id { get; set; }
        public long CategoryId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Order { get; set; }
    }

    public class ForumThread
    {
        public long Id { get; set; }
        public long BoardId { get; set; }
        public long AuthorId { get; set; }
        public string AuthorName { get; set; }
        public string Title { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastPostAt { get; set; }
        public string LastPostBy { get; set; }
        public int Replies { get; set; }
        public bool Locked { get; set; }
        public bool Pinned { get; set; }
    }

    public class ForumPost
    {
        public long Id { get; set; }
        public long ThreadId { get; set; }
        public long AuthorId { get; set; }
        public string AuthorName { get; set; }
        public string Body { get; set; }
        public DateTime Created { get; set; }
    }

    /// <summary>Forum storage (XML files in App_Data, like the rest of the site).</summary>
    public static class ForumStore
    {
        public static readonly XmlTable<ForumCategory> Categories = new XmlTable<ForumCategory>("ForumCategories.xml");
        public static readonly XmlTable<ForumBoard> Boards = new XmlTable<ForumBoard>("ForumBoards.xml");
        public static readonly XmlTable<ForumThread> Threads = new XmlTable<ForumThread>("ForumThreads.xml");
        public static readonly XmlTable<ForumPost> Posts = new XmlTable<ForumPost>("ForumPosts.xml");

        static readonly object SeedLock = new object();

        /// <summary>Anti-CSRF token for forum forms (only the server and this user can compute it).</summary>
        public static string Token(User user)
        {
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes("forum|" + user.Id + "|" + user.PasswordHash));
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant().Substring(0, 32);
            }
        }

        // Forum index text of the 2012 site. Category rows: {name, old name or null}. Board rows: {name, description, old name or null}.
        static readonly string[][] Layout =
        {
            new[] { "ROBLOX", "RobloxServer" },
            new[] { "All Things ROBLOX", "The area for discussions purely about ROBLOX \u2013 the features, the games, and company news.", "All Things RobloxServer" },
            new[] { "Help (Technical Support and Account Issues)", "Seeking account or technical help? Post your questions here.", null },
            new[] { "I Made That", "Calling all creative ROBLOXians! Movie makers, model builders, decal artists and re-texturers - this is your forum.", null },
            new[] { "ROBLOX Contests", "Get involved with ROBLOX Contests! We're discussing ongoing and future contests in this forum.", "Contests" },
            new[] { "ROBLOX Global", "This forum is the place to find other players from your country, find online pen pals, and post discussions in foreign languages.", "Global" },
            new[] { "Suggestions & Ideas", "Do you have a suggestion and ideas for ROBLOX? Share your feedback here.", null },
            new[] { "Club Houses", null },
            new[] { "ROBLOX Talk", "A popular hangout where ROBLOXians talk about various topics.", "Talk" },
            new[] { "Off Topic", "When no other forum makes sense for your post, Off Topic will help it make even less sense.", null },
            new[] { "Clans & Guilds", "Talk about what's going on in your Clans, Companies, and Guilds, and about the Groups feature in general.", null },
            new[] { "Let's Make a Deal", "A fast paced community of wheelers and dealers. Post what you have to offer or see if anyone is willing to part with that special thing you really want.", null },
            new[] { "Game Creation and Development", null },
            new[] { "Building Helpers", "Learn the ins and outs of building structures in ROBLOX. Share your techniques with other builders, discuss designs, and draft plans. Help others!", null }
        };

        /// <summary>Creates the forum index on first run; on older data renames the boards the first seed called differently.</summary>
        public static void EnsureSeed()
        {
            lock (SeedLock)
            {
                if (Boards.All().Count > 0)
                {
                    Rename();
                    return;
                }
                ForumCategory category = null;
                int order = 0;
                foreach (string[] row in Layout)
                {
                    if (row.Length == 2)
                    {
                        string categoryName = row[0];
                        int categoryOrder = Categories.All().Count;
                        category = Categories.InsertWithId(x => x.Id, 1, id => new ForumCategory { Id = id, Name = categoryName, Order = categoryOrder });
                        order = 0;
                        continue;
                    }
                    string name = row[0];
                    string description = row[1];
                    long categoryId = category.Id;
                    int boardOrder = order++;
                    Boards.InsertWithId(x => x.Id, 1, id => new ForumBoard
                    {
                        Id = id, CategoryId = categoryId, Name = name, Description = description, Order = boardOrder
                    });
                }
            }
        }

        static bool renamed;

        /// <summary>Once per run: boards and categories that still have the names of the first seed get the 2012 text.</summary>
        static void Rename()
        {
            if (renamed)
            {
                return;
            }
            renamed = true;
            foreach (string[] row in Layout)
            {
                if (row.Length == 2)
                {
                    string categoryName = row[0];
                    string oldCategory = row[1];
                    if (oldCategory != null && Categories.Find(c => c.Name == categoryName) == null)
                    {
                        Categories.Update(c => c.Name == oldCategory, c => c.Name = categoryName);
                    }
                    continue;
                }
                string name = row[0];
                string description = row[1];
                string old = row[2];
                ForumBoard existing = Boards.Find(x => x.Name == name || (old != null && x.Name == old));
                if (existing != null && (existing.Name != name || existing.Description != description))
                {
                    Boards.Update(x => x.Id == existing.Id, x => { x.Name = name; x.Description = description; });
                }
            }
        }
    }
}
