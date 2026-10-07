using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace RobloxServer.Data
{
    /// <summary>Um item do catalogo (roupa, acessorio ou gear) feito pela conta "ROBLOX" entre 2007 e 2013.</summary>
    public class CatalogItem
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        /// <summary>Hats, Hair, Face Accessories, Neck, Shoulder, Front, Back, Waist, Shirts, Pants, T-Shirts, Gear, Faces</summary>
        public string Category { get; set; }
        public int Price { get; set; }
        public string Creator { get; set; }
        public DateTime Created { get; set; }
        public string Thumb { get; set; }
        public int Sales { get; set; }

        public int Year
        {
            get { return Created.Year; }
        }
    }

    public class CatalogCategory
    {
        public string Name;
        public int AssetType;
        public bool Clothing;
    }

    /// <summary>
    /// Catalogo no estilo de 2013. Os dados vem da API publica do Roblox (catalog/economy/thumbnails)
    /// quando um admin clica em "Sync catalog": so entram itens da conta ROBLOX criados entre 2007 e 2013.
    /// O arquivo .rbxm de cada item e baixado do asset delivery do Roblox na primeira vez que alguem pede
    /// e fica guardado em App_Data/Assets/Catalog.
    /// </summary>
    public static class CatalogService
    {
        public static readonly XmlTable<CatalogItem> Items = new XmlTable<CatalogItem>("Catalog.xml");

        public static readonly DateTime From = new DateTime(2007, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public static readonly DateTime To = new DateTime(2013, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        public static readonly CatalogCategory[] Categories =
        {
            new CatalogCategory { Name = "Hats", AssetType = 8 },
            new CatalogCategory { Name = "Hair", AssetType = 41 },
            new CatalogCategory { Name = "Face Accessories", AssetType = 42 },
            new CatalogCategory { Name = "Neck", AssetType = 43 },
            new CatalogCategory { Name = "Shoulder", AssetType = 44 },
            new CatalogCategory { Name = "Front", AssetType = 45 },
            new CatalogCategory { Name = "Back", AssetType = 46 },
            new CatalogCategory { Name = "Waist", AssetType = 47 },
            new CatalogCategory { Name = "Shirts", AssetType = 11, Clothing = true },
            new CatalogCategory { Name = "Pants", AssetType = 12, Clothing = true },
            new CatalogCategory { Name = "T-Shirts", AssetType = 2, Clothing = true },
            new CatalogCategory { Name = "Faces", AssetType = 18 },
            new CatalogCategory { Name = "Gear", AssetType = 19 }
        };

        public static string CacheDir
        {
            get { return Path.Combine(Path.Combine(Config.DataPath, "Assets"), "Catalog"); }
        }

        public static CatalogCategory FindCategory(string name)
        {
            return Categories.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        static string CachePath(long id)
        {
            return Path.Combine(CacheDir, id + ".rbxm");
        }

        public static bool IsCached(long id)
        {
            return File.Exists(CachePath(id));
        }

        public static string SafeFileName(CatalogItem item)
        {
            var sb = new StringBuilder();
            foreach (char c in item.Name ?? "item")
            {
                sb.Append(char.IsLetterOrDigit(c) ? c : '-');
            }
            return sb.ToString().Trim('-') + "-" + item.Id + ".rbxm";
        }

        /// <summary>Devolve o .rbxm do item (baixando do Roblox se ainda nao estiver guardado). Null se nao deu.</summary>
        public static byte[] GetRbxm(CatalogItem item, out string error)
        {
            error = null;
            string path = CachePath(item.Id);
            if (File.Exists(path))
            {
                return File.ReadAllBytes(path);
            }

            try
            {
                byte[] data = Download("https://assetdelivery.roblox.com/v1/asset/?id=" + item.Id, 20000);
                if (data == null || data.Length < 16)
                {
                    error = "Roblox returned an empty file for this item.";
                    return null;
                }
                Directory.CreateDirectory(CacheDir);
                File.WriteAllBytes(path, data);
                return data;
            }
            catch (Exception ex)
            {
                Logging.Log(LogType.Error, "Catalog rbxm " + item.Id + ": " + ex.Message);
                error = "Could not download this item from Roblox right now.";
                return null;
            }
        }

        // ------------------------------------------------------------------ sync

        /// <summary>Atualiza o catalogo local. Devolve uma linha de resumo. Chamado so por admin.</summary>
        public static string Sync(int maxPages)
        {
            var found = new List<CatalogItem>();
            int scanned = 0;

            foreach (CatalogCategory category in Categories)
            {
                string cursor = "";
                for (int page = 0; page < maxPages; page++)
                {
                    string url = "https://catalog.roblox.com/v1/search/items/details?CreatorTargetId=1&CreatorType=User"
                        + "&AssetTypes=" + category.AssetType + "&SortType=3&Limit=30"
                        + (cursor.Length > 0 ? "&Cursor=" + Uri.EscapeDataString(cursor) : "");

                    Dictionary<string, object> json;
                    try
                    {
                        json = ParseObject(Encoding.UTF8.GetString(Download(url, 15000)));
                    }
                    catch (Exception ex)
                    {
                        Logging.Log(LogType.Error, "Catalog sync " + category.Name + ": " + ex.Message);
                        break;
                    }

                    object[] data = AsArray(json, "data");
                    foreach (object o in data)
                    {
                        var row = o as Dictionary<string, object>;
                        if (row == null) continue;
                        long id = AsLong(row, "id");
                        if (id == 0) continue;
                        scanned++;
                        found.Add(new CatalogItem
                        {
                            Id = id,
                            Name = AsString(row, "name"),
                            Description = AsString(row, "description"),
                            Category = category.Name,
                            Price = (int)AsLong(row, "price"),
                            Creator = "ROBLOX",
                            Sales = (int)AsLong(row, "purchaseCount")
                        });
                    }

                    cursor = AsString(json, "nextPageCursor");
                    if (data.Length == 0 || string.IsNullOrEmpty(cursor)) break;
                }
            }

            // a data de criacao so aparece na API de economia, um item por vez
            var kept = new List<CatalogItem>();
            foreach (CatalogItem item in found)
            {
                try
                {
                    Dictionary<string, object> d = ParseObject(Encoding.UTF8.GetString(
                        Download("https://economy.roblox.com/v2/assets/" + item.Id + "/details", 10000)));
                    DateTime created;
                    if (!DateTime.TryParse(AsString(d, "Created"), null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out created))
                    {
                        continue;
                    }
                    if (created < From || created > To) continue;
                    item.Created = created;
                    if (!string.IsNullOrEmpty(AsString(d, "Description"))) item.Description = AsString(d, "Description");
                    kept.Add(item);
                }
                catch (Exception ex)
                {
                    Logging.Log(LogType.Error, "Catalog date " + item.Id + ": " + ex.Message);
                }
            }

            // miniaturas em lotes de 100
            for (int i = 0; i < kept.Count; i += 100)
            {
                List<CatalogItem> batch = kept.Skip(i).Take(100).ToList();
                try
                {
                    string url = "https://thumbnails.roblox.com/v1/assets?size=150x150&format=Png&assetIds="
                        + string.Join(",", batch.Select(b => b.Id.ToString()).ToArray());
                    Dictionary<string, object> t = ParseObject(Encoding.UTF8.GetString(Download(url, 15000)));
                    foreach (object o in AsArray(t, "data"))
                    {
                        var row = o as Dictionary<string, object>;
                        if (row == null) continue;
                        long tid = AsLong(row, "targetId");
                        CatalogItem it = batch.FirstOrDefault(b => b.Id == tid);
                        if (it != null) it.Thumb = AsString(row, "imageUrl");
                    }
                }
                catch (Exception ex)
                {
                    Logging.Log(LogType.Error, "Catalog thumbs: " + ex.Message);
                }
            }

            int added = 0;
            foreach (CatalogItem item in kept)
            {
                CatalogItem existing = Items.Find(x => x.Id == item.Id);
                if (existing == null)
                {
                    Items.Insert(item);
                    added++;
                }
                else
                {
                    Items.Update(x => x.Id == item.Id, x =>
                    {
                        x.Name = item.Name; x.Description = item.Description; x.Category = item.Category;
                        x.Price = item.Price; x.Created = item.Created; x.Sales = item.Sales;
                        if (!string.IsNullOrEmpty(item.Thumb)) x.Thumb = item.Thumb;
                    });
                }
            }

            return "Scanned " + scanned + " ROBLOX items, kept " + kept.Count + " made 2007-2013, " + added + " new.";
        }

        // ------------------------------------------------------------------ helpers

        static byte[] Download(string url, int timeout)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Timeout = timeout;
            request.UserAgent = "RobloxServer-Catalog/1.0";
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }

        static Dictionary<string, object> ParseObject(string json)
        {
            var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            return serializer.DeserializeObject(json) as Dictionary<string, object> ?? new Dictionary<string, object>();
        }

        static object[] AsArray(Dictionary<string, object> d, string key)
        {
            object v;
            if (d.TryGetValue(key, out v) && v is object[]) return (object[])v;
            return new object[0];
        }

        static string AsString(Dictionary<string, object> d, string key)
        {
            object v;
            return d.TryGetValue(key, out v) && v != null ? Convert.ToString(v) : "";
        }

        static long AsLong(Dictionary<string, object> d, string key)
        {
            object v;
            if (d.TryGetValue(key, out v) && v != null)
            {
                long r;
                if (long.TryParse(Convert.ToString(v), out r)) return r;
            }
            return 0;
        }
    }
}
