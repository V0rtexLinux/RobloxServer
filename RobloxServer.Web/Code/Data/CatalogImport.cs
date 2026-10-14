using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RobloxServer.Data
{
    /// <summary>
    /// Importa itens PROPRIOS para o catalogo a partir de App_Data/CatalogImport/:
    ///   items.csv (separado por ponto e virgula, # = comentario):  arquivo;nome;categoria;preco;descricao;criador;data;thumb
    ///   e os arquivos .rbxm/.rbxmx citados na coluna "arquivo" (na mesma pasta).
    /// Use so arquivos que voce criou ou tem direito de distribuir. Os ids ficam a partir de 8.000.000.000.000.
    /// </summary>
    public static class CatalogImport
    {
        public const long FirstId = 8000000000000L;
        const long MaxFileBytes = 5 * 1024 * 1024;

        public static string Folder
        {
            get { return Path.Combine(Config.DataPath, "CatalogImport"); }
        }

        static bool LooksLikeModel(byte[] data)
        {
            string head = Encoding.ASCII.GetString(data, 0, Math.Min(data.Length, 64));
            return head.Contains("<roblox");
        }

        public static string Run()
        {
            string csv = Path.Combine(Folder, "items.csv");
            if (!File.Exists(csv))
            {
                return "Crie " + csv + " (colunas: arquivo;nome;categoria;preco;descricao;criador;data;thumb).";
            }

            int added = 0, skipped = 0;
            var problems = new List<string>();
            int lineNumber = 0;
            foreach (string raw in File.ReadAllLines(csv, Encoding.UTF8))
            {
                lineNumber++;
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#") || (lineNumber == 1 && line.StartsWith("arquivo;", StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }
                string[] p = line.Split(';');
                string error = ImportLine(p);
                if (error == null) { added++; }
                else if (error == "duplicate") { skipped++; }
                else { problems.Add("linha " + lineNumber + ": " + error); }
            }

            string result = added + " item(ns) importado(s), " + skipped + " ja existiam";
            if (problems.Count > 0)
            {
                result += ", " + problems.Count + " com problema (" + string.Join("; ", problems.Take(3).ToArray()) + ")";
            }
            return result + ".";
        }

        static string ImportLine(string[] p)
        {
            if (p.Length < 4) { return "faltam colunas (arquivo;nome;categoria;preco)"; }
            string fileName = Path.GetFileName(p[0].Trim());
            string name = p[1].Trim();
            CatalogCategory category = CatalogService.FindCategory(p[2].Trim());
            int price;
            if (fileName.Length == 0 || !fileName.EndsWith(".rbxm", StringComparison.OrdinalIgnoreCase) && !fileName.EndsWith(".rbxmx", StringComparison.OrdinalIgnoreCase)) { return "arquivo deve ser .rbxm ou .rbxmx"; }
            if (name.Length < 2 || name.Length > 60) { return "nome deve ter 2 a 60 caracteres"; }
            if (category == null) { return "categoria desconhecida: " + p[2].Trim(); }
            if (!int.TryParse(p[3].Trim(), out price) || price < 0 || price > 1000000) { return "preco invalido"; }

            string path = Path.Combine(Folder, fileName);
            if (!File.Exists(path)) { return "arquivo nao encontrado: " + fileName; }
            if (new FileInfo(path).Length > MaxFileBytes) { return "arquivo maior que 5 MB"; }
            byte[] data = File.ReadAllBytes(path);
            if (!LooksLikeModel(data)) { return "nao parece um modelo do Roblox: " + fileName; }

            string description = p.Length > 4 ? p[4].Trim() : "";
            string creator = p.Length > 5 && p[5].Trim().Length > 0 ? p[5].Trim() : "Imported";
            DateTime created;
            if (p.Length < 7 || !DateTime.TryParse(p[6].Trim(), out created)) { created = new DateTime(2013, 6, 1, 0, 0, 0, DateTimeKind.Utc); }
            created = DateTime.SpecifyKind(created, DateTimeKind.Utc);
            if (created < CatalogService.From) { created = CatalogService.From; }
            if (created > CatalogService.To) { created = CatalogService.To; }
            string thumb = p.Length > 7 ? p[7].Trim() : "";
            if (thumb.Length > 0 && !thumb.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) { thumb = ""; }

            bool exists = CatalogService.Items.All().Any(i => i.Id >= FirstId && i.Creator == creator
                && string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase) && i.Category == category.Name);
            if (exists) { return "duplicate"; }

            CatalogItem item = CatalogService.Items.InsertWithId(i => i.Id, FirstId, id => new CatalogItem
            {
                Id = id, Name = name, Description = description, Category = category.Name, Price = price,
                Creator = creator, Created = created, Thumb = thumb, Sales = 0
            });
            Directory.CreateDirectory(CatalogService.CacheDir);
            File.WriteAllBytes(Path.Combine(CatalogService.CacheDir, item.Id + ".rbxm"), data);
            return null;
        }
    }
}
