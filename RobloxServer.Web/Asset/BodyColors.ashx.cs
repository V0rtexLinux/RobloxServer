using System;
using System.Collections.Generic;
using RobloxServer.Data;
using RobloxServer.Web;

namespace RobloxServer.Handlers.Asset
{
    public class BodyColors : HandlerBase
    {
        protected override void Handle()
        {
            Response.ContentType = "text/xml";
            Response.Write(XmlFor(Db.FindUser(QueryLong("userId"))));
        }

        /// <summary>The user's BodyColors, or the default ones (all "Really black" for Noli).</summary>
        public static string XmlFor(User user)
        {
            if (user != null && !string.IsNullOrEmpty(user.BodyColors))
            {
                return user.BodyColors;
            }

            string xml = Templates.Render("BodyColors.xml", new Dictionary<string, string>());
            if (Noli.Is(user))
            {
                xml = System.Text.RegularExpressions.Regex.Replace(xml, @"(<int name=""\w+Color"">)\d+(</int>)", "${1}" + Noli.ReallyBlack + "${2}");
            }
            return xml;
        }
    }
}
