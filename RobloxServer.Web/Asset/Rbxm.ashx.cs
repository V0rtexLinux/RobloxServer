using System;
using RobloxServer.Data;
using RobloxServer.Web;

namespace RobloxServer.Handlers.Asset
{
    /// <summary>/Asset/Rbxm.ashx?id= : baixa o .rbxm de um item do catalogo (2007-2013) como arquivo.</summary>
    public class Rbxm : HandlerBase
    {
        protected override void Handle()
        {
            long id = QueryLong("id");
            CatalogItem item = id > 0 ? CatalogService.Items.Find(i => i.Id == id) : null;
            if (item == null)
            {
                WriteStatus(404, "This item is not in the catalog.");
                return;
            }

            string error;
            byte[] data = CatalogService.GetRbxm(item, out error);
            if (data == null)
            {
                WriteStatus(502, error);
                return;
            }

            Response.ContentType = "application/octet-stream";
            Response.AddHeader("Content-Disposition", "attachment; filename=\"" + CatalogService.SafeFileName(item) + "\"");
            Response.AddHeader("Content-Length", data.Length.ToString());
            Response.BinaryWrite(data);
        }
    }
}
