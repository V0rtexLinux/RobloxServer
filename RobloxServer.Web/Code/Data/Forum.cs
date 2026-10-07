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

        public static void EnsureSeed()
        {
            lock (SeedLock)
            {
                if (Boards.All().Count > 0)
                {
                    return;
                }
                Seed("RobloxServer", new[]
                {
                    new[] { "All Things RobloxServer", "Everything about the site: features, games and news." },
                    new[] { "Help (Technical Support and Account Issues)", "Need help with your account or something is broken? Ask here." },
                    new[] { "I Made That", "Show off your builds, models, decals and videos." },
                    new[] { "Contests", "Take part in community contests and see what is coming up." },
                    new[] { "Global", "Meet players from your country and talk in other languages." },
                    new[] { "Suggestions & Ideas", "Have an idea for the site? Share your feedback." }
                });
                Seed("Club Houses", new[]
                {
                    new[] { "Talk", "A popular hangout where members talk about anything." },
                    new[] { "Off Topic", "When no other forum fits your post, it goes here." },
                    new[] { "Clans & Guilds", "Talk about your clans, companies and groups." },
                    new[] { "Let's Make a Deal", "Trade and swap: say what you offer and what you want." }
                });
                Seed("Game Creation and Development", new[]
                {
                    new[] { "Building Helpers", "Learn how to build, share techniques and plan your projects." }
                });
            }
        }

        static void Seed(string category, string[][] boards)
        {
            int order = Categories.All().Count;
            ForumCategory c = Categories.InsertWithId(x => x.Id, 1, id => new ForumCategory { Id = id, Name = category, Order = order });
            int index = 0;
            foreach (string[] b in boards)
            {
                string name = b[0];
                string description = b[1];
                int boardOrder = index++;
                Boards.InsertWithId(x => x.Id, 1, id => new ForumBoard
                {
                    Id = id, CategoryId = c.Id, Name = name, Description = description, Order = boardOrder
                });
            }
        }
    }
}
