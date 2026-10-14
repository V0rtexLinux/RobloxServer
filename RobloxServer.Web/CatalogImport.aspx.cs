using System.Text;
using RobloxServer.Data;
using RobloxServer.Web;

namespace RobloxServer.Pages
{
    /// <summary>Admin: importa para o catalogo os itens proprios da pasta App_Data/CatalogImport.</summary>
    public class CatalogImport : SocialPage
    {
        protected override string Handle(string verb, long id, User user)
        {
            if (verb != "import")
            {
                return "Unknown action.";
            }
            if (!Db.IsAdmin(user))
            {
                return "Only admins can import catalog items.";
            }
            string result = RobloxServer.Data.CatalogImport.Run();
            Logging.Log(LogType.Success, "Catalog import by " + user.Name + ": " + result);
            if (result.Length > 300)
            {
                result = result.Substring(0, 300);
            }
            NextUrl = Url("CatalogImport.aspx?r=" + System.Uri.EscapeDataString(result));
            return null;
        }

        protected override string Render(User user)
        {
            var sb = new StringBuilder("<h2>Import catalog items</h2>");
            if (!Db.IsAdmin(user))
            {
                return sb.Append("<p>Only admins can use this page.</p>").ToString();
            }
            string r = Request.QueryString["r"];
            if (!string.IsNullOrEmpty(r))
            {
                sb.Append("<p style=\"color:#060\">" + E(r) + "</p>");
            }
            sb.Append("<p>Put your own <b>.rbxm</b> / <b>.rbxmx</b> files and an <b>items.csv</b> in <code>"
                + E(RobloxServer.Data.CatalogImport.Folder) + "</code>, then click Import. Only use files you made or may distribute.</p>");
            sb.Append("<p><code>arquivo;nome;categoria;preco;descricao;criador;data;thumb</code><br />Categories: ");
            for (int i = 0; i < CatalogService.Categories.Length; i++)
            {
                sb.Append((i > 0 ? ", " : "") + E(CatalogService.Categories[i].Name));
            }
            sb.Append(".</p><div>" + Hidden(user) + Btn("Import now", "import:0") + "</div>");
            return sb.ToString();
        }
    }
}
