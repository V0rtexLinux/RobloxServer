using System;
using System.Collections.Generic;
using RobloxServer.Data;
using RobloxServer.Web;

namespace RobloxServer.Handlers.Asset
{
    public class BodyColors : HandlerBase
    {
        /// <summary>The Noli myth: a Robloxian with completely black skin.</summary>
        public const string NoliName = "Noli";

        /// <summary>BrickColor "Really black".</summary>
        const string ReallyBlack = "1003";

        public static bool IsNoli(User user)
        {
            return user != null && string.Equals(user.Name, NoliName, StringComparison.OrdinalIgnoreCase);
        }

        protected override void Handle()
        {
            User user = Db.FindUser(QueryLong("userId"));
            Response.ContentType = "text/xml";

            if (user != null && !string.IsNullOrEmpty(user.BodyColors))
            {
                Response.Write(user.BodyColors);
                return;
            }

            string xml = Templates.Render("BodyColors.xml", new Dictionary<string, string>());
            if (IsNoli(user))
            {
                xml = System.Text.RegularExpressions.Regex.Replace(xml, @"(<int name=""\w+Color"">)\d+(</int>)", "${1}" + ReallyBlack + "${2}");
            }
            Response.Write(xml);
        }
    }
}
