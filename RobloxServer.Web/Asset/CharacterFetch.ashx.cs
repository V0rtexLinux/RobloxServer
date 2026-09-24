using RobloxServer.Web;

namespace RobloxServer.Handlers.Asset
{
    /// <summary>Player.CharacterAppearance: semicolon separated list of appearance assets.</summary>
    public class CharacterFetch : HandlerBase
    {
        protected override void Handle()
        {
            long userId = QueryLong("userId");
            WriteText(Config.BaseUrl + "Asset/BodyColors.ashx?userId=" + userId + ";" + Config.BaseUrl + "Asset/?versionid=21351761");
        }
    }
}
