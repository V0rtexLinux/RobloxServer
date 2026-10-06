using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RobloxServer.Security;

namespace RobloxServer.Data
{
    public static class PlaceService
    {
        /// <summary>Accepts binary (&lt;roblox!) and XML (&lt;roblox ...) place files.</summary>
        public static string Validate(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return "The place file is empty.";
            }
            if (data.LongLength > Config.MaxPlaceSizeMegabytes * 1024L * 1024L)
            {
                return "The place file is larger than " + Config.MaxPlaceSizeMegabytes + " MB.";
            }

            string head = Encoding.UTF8.GetString(data, 0, Math.Min(data.Length, 1024));
            if (!head.Contains("<roblox"))
            {
                return "This is not a ROBLOX place file (.rbxl).";
            }
            return null;
        }

        public static string CleanName(string name)
        {
            name = (name ?? "").Trim();
            if (name.Length == 0)
            {
                name = "Place";
            }
            if (name.Length > 50)
            {
                name = name.Substring(0, 50);
            }
            return WordFilter.Filter(name);
        }

        public static string CleanDescription(string description)
        {
            description = (description ?? "").Trim();
            if (description.Length > 1000)
            {
                description = description.Substring(0, 1000);
            }
            return WordFilter.Filter(description);
        }

        public static string CleanClient(string client)
        {
            string match = Config.Clients.FirstOrDefault(c => string.Equals(c, (client ?? "").Trim(), StringComparison.OrdinalIgnoreCase));
            return match ?? Config.DefaultClient;
        }

        /// <summary>
        /// Client a place is played with. Places published for clients that are no longer supported
        /// (2006S and other names outside 2007-2013) are played with the default client.
        /// </summary>
        public static string ClientFor(Place place)
        {
            return CleanClient(place != null ? place.Client : null);
        }

        public static Place Create(User creator, string name, string description, string client, bool isPublic, bool filteringEnabled, int maxPlayers, byte[] data, string genre = null)
        {
            DateTime now = DateTime.UtcNow;
            Place place = Db.Places.InsertWithId(p => p.Id, Config.FirstLocalAssetId, id => new Place
            {
                Id = id,
                Name = CleanName(name),
                Description = CleanDescription(description),
                CreatorId = creator.Id,
                CreatorName = creator.Name,
                Client = CleanClient(client),
                Genre = Genres.Clean(genre),
                Created = now,
                Updated = now,
                IsPublic = isPublic,
                FilteringEnabled = filteringEnabled,
                MaxPlayers = Math.Max(1, Math.Min(maxPlayers <= 0 ? 12 : maxPlayers, 100)),
                FileExtension = "rbxl"
            });

            Db.SavePlaceFile(place.Id, data, "rbxl");
            Logging.Log(LogType.Success, creator.Name + " published place " + place.Id + " (" + place.Name + ")");
            return Db.FindPlace(place.Id);
        }

        /// <summary>Who may start a game server for a place (the HostPolicy setting).</summary>
        public static bool CanHost(User user, Place place)
        {
            // Ninguem hospeda manualmente: so o bot HOST.
            return user != null && place != null && !user.IsCurrentlyBanned && BotAccount.Is(user);
        }

        public static bool CanEdit(User user, Place place)
        {
            return user != null && place != null && (place.CreatorId == user.Id || Db.IsAdmin(user));
        }
    }

    public static class BotAccount
    {
        public const string Name = "HOST";
        const string Locked = "!"; // hash invalido: ninguem consegue logar com senha
        static readonly object Sync = new object();

        public static bool Is(User u)
        {
            return u != null && string.Equals(u.Name, Name, StringComparison.OrdinalIgnoreCase) && u.PasswordHash == Locked;
        }

        /// null se ainda nao ha nenhuma conta (a 1a conta vira admin) ou se o nome HOST ja e de uma conta comum.
        public static User GetOrCreate()
        {
            lock (Sync)
            {
                User bot = Db.FindUser(Name);
                if (bot != null) { return Is(bot) ? bot : null; }
                if (Db.Users.All().Count == 0) { return null; }
                return Db.Users.InsertWithId(u => u.Id, 1, id => new User
                {
                    Id = id, Name = Name, PasswordHash = Locked,
                    Created = DateTime.UtcNow, LastOnline = DateTime.UtcNow
                });
            }
        }
    }

    /// <summary>
    /// Todo jogo (existente ou novo) ganha um bot HOST automaticamente. As portas ficam fixas por jogo em
    /// App_Data/BotPorts.txt (placeId;porta). App_Data/BotServers.txt e opcional, so para ajustes:
    ///   placeId;porta;endereco   define porta/endereco     !placeId   desliga o bot desse jogo     # comentario
    /// </summary>
    public static class BotServers
    {
        public const int FirstPort = 53641;
        static readonly object Sync = new object();

        public static string ToText()
        {
            lock (Sync)
            {
                var over = new Dictionary<long, string[]>();
                var off = new HashSet<long>();
                string cfg = Path.Combine(Config.DataPath, "BotServers.txt");
                if (File.Exists(cfg))
                {
                    foreach (string raw in File.ReadAllLines(cfg))
                    {
                        string line = raw.Trim();
                        long id;
                        if (line.Length == 0 || line.StartsWith("#")) { continue; }
                        if (line.StartsWith("!"))
                        {
                            if (long.TryParse(line.Substring(1).Trim(), out id)) { off.Add(id); }
                            continue;
                        }
                        string[] p = line.Split(';');
                        if (long.TryParse(p[0].Trim(), out id)) { over[id] = p; }
                    }
                }

                string mapPath = Path.Combine(Config.DataPath, "BotPorts.txt");
                var map = new Dictionary<long, int>();
                if (File.Exists(mapPath))
                {
                    foreach (string raw in File.ReadAllLines(mapPath))
                    {
                        string[] p = raw.Trim().Split(';');
                        long id; int port;
                        if (p.Length == 2 && long.TryParse(p[0], out id) && int.TryParse(p[1], out port)) { map[id] = port; }
                    }
                }

                var blocked = new HashSet<int>(map.Values);
                foreach (var kv in over)
                {
                    int port;
                    if (kv.Value.Length > 1 && int.TryParse(kv.Value[1].Trim(), out port)) { blocked.Add(port); }
                }

                var assigned = new HashSet<int>();
                var result = new Dictionary<long, int>();
                var text = new StringBuilder();
                foreach (Place place in Db.Places.All().OrderBy(x => x.Id))
                {
                    if (off.Contains(place.Id)) { continue; }
                    string[] o;
                    over.TryGetValue(place.Id, out o);
                    int port = 0;
                    if (o != null && o.Length > 1 && int.TryParse(o[1].Trim(), out port) && port > 0 && port <= 65535 && !assigned.Contains(port))
                    {
                    }
                    else if (map.TryGetValue(place.Id, out port) && !assigned.Contains(port) && !IsOverridePortOfOther(over, place.Id, port))
                    {
                    }
                    else
                    {
                        port = FirstPort;
                        while (blocked.Contains(port) || assigned.Contains(port)) { port++; }
                        blocked.Add(port);
                    }
                    assigned.Add(port);
                    result[place.Id] = port;
                    string address = o != null && o.Length > 2 ? o[2].Trim() : "";
                    text.Append(place.Id).Append(';').Append(port).Append(';').Append(address).Append('\n');
                }

                bool changed = result.Count != map.Count || result.Any(kv => !map.ContainsKey(kv.Key) || map[kv.Key] != kv.Value);
                if (changed)
                {
                    Directory.CreateDirectory(Config.DataPath);
                    File.WriteAllLines(mapPath, result.Select(kv => kv.Key + ";" + kv.Value).ToArray());
                }
                return text.ToString();
            }
        }

        static bool IsOverridePortOfOther(Dictionary<long, string[]> over, long placeId, int port)
        {
            foreach (var kv in over)
            {
                int p;
                if (kv.Key != placeId && kv.Value.Length > 1 && int.TryParse(kv.Value[1].Trim(), out p) && p == port) { return true; }
            }
            return false;
        }
    }

    /// <summary>
    /// Chave dos bots: App_Data/bot.key (aleatoria, criada sozinha na primeira vez; apague o arquivo para trocar).
    /// O launcher que roda na mesma maquina le o mesmo arquivo (--site-dir). Nada secreto vai para o Git.
    /// </summary>
    public static class BotKeyFile
    {
        static readonly object Sync = new object();

        public static string GetOrCreate()
        {
            lock (Sync)
            {
                string path = Path.Combine(Config.DataPath, "bot.key");
                if (File.Exists(path))
                {
                    string existing = File.ReadAllText(path).Trim();
                    if (existing.Length >= 32)
                    {
                        return existing;
                    }
                }
                byte[] bytes = new byte[32];
                using (var rng = new System.Security.Cryptography.RNGCryptoServiceProvider())
                {
                    rng.GetBytes(bytes);
                }
                string key = BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
                Directory.CreateDirectory(Config.DataPath);
                File.WriteAllText(path, key);
                return key;
            }
        }
    }

    /// <summary>
    /// Pareamento de launchers de bot em OUTRA maquina: o admin gera um codigo (10 min, uso unico) logado no site,
    /// o launcher troca o codigo por uma chave propria, guardada aqui so como hash (App_Data/bot-keys.txt).
    /// Para revogar uma maquina, apague a linha dela (ou o arquivo).
    /// </summary>
    public static class BotPairing
    {
        static readonly object Sync = new object();
        static string code;
        static DateTime expires;
        static int wrong;

        static string KeysPath
        {
            get { return Path.Combine(Config.DataPath, "bot-keys.txt"); }
        }

        static string Sha256(string text)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "").ToLowerInvariant();
            }
        }

        public static string NewCode()
        {
            lock (Sync)
            {
                const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
                var bytes = new byte[10];
                using (var rng = new System.Security.Cryptography.RNGCryptoServiceProvider())
                {
                    rng.GetBytes(bytes);
                }
                var text = new StringBuilder();
                foreach (byte b in bytes)
                {
                    text.Append(alphabet[b % alphabet.Length]);
                }
                code = text.ToString();
                expires = DateTime.UtcNow.AddMinutes(10);
                wrong = 0;
                return code;
            }
        }

        /// <summary>Troca o codigo por uma chave nova. Null se o codigo estiver errado, vencido ou ja usado.</summary>
        public static string Redeem(string sent)
        {
            lock (Sync)
            {
                if (code == null || DateTime.UtcNow > expires)
                {
                    return null;
                }
                if (!PasswordHasher.ConstantTimeEquals(code, (sent ?? "").Trim().ToUpperInvariant()))
                {
                    if (++wrong >= 10)
                    {
                        code = null;
                    }
                    return null;
                }
                code = null;
                var bytes = new byte[32];
                using (var rng = new System.Security.Cryptography.RNGCryptoServiceProvider())
                {
                    rng.GetBytes(bytes);
                }
                string key = BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
                Directory.CreateDirectory(Config.DataPath);
                File.AppendAllText(KeysPath, Sha256(key) + "\n");
                return key;
            }
        }

        public static bool IsPairedKey(string sent)
        {
            if (string.IsNullOrEmpty(sent) || !File.Exists(KeysPath))
            {
                return false;
            }
            string hash = Sha256(sent);
            foreach (string line in File.ReadAllLines(KeysPath))
            {
                if (PasswordHasher.ConstantTimeEquals(line.Trim(), hash))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
