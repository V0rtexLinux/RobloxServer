using System.Collections.Generic;
using RobloxServer.Data;
using RobloxServer.Web;

namespace RobloxServer.Handlers.Asset
{
    public class BodyColors : HandlerBase
    {
        protected override void Handle()
        {
            User user = Db.FindUser(QueryLong("userId"));
            Response.ContentType = "text/xml";

            if (user != null && !string.IsNullOrEmpty(user.BodyColors))
            {
                Response.Write(user.BodyColors);
                return;
            }

            Response.Write(Templates.Render("BodyColors.xml", new Dictionary<string, string>()));
        }
    }
}
