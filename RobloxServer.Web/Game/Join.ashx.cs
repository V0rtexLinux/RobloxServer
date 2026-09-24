using System;
using RobloxServer.Data;
using RobloxServer.Security;
using RobloxServer.Web;

namespace RobloxServer.Handlers.Game
{
    /// <summary>
    /// Signed JSON join script (2015 format) for a running job. The Novetus launcher reads
    /// MachineAddress / ServerPort / NovetusClient / ClientTicket from it.
    /// </summary>
    public class Join : HandlerBase
    {
        protected override void Handle()
        {
            User user = RequireUser();
            if (user == null)
            {
                return;
            }

            GameServer server = GameServers.Find(Request.QueryString["jobId"]);
            if (server == null)
            {
                WriteStatus(404, "Game not found");
                return;
            }

            Place place = Db.FindPlace(server.PlaceId);
            if (server.PlaceId != 0 && !CanSee(user, place))
            {
                WriteStatus(403, "Place is private");
                return;
            }

            if (place != null)
            {
                Db.Places.Update(p => p.Id == place.Id, p => p.Visits++);
            }

            AuthTicket ticket = AuthTickets.Issue(user.Id, user.Name, server.PlaceId, server.JobId);
            string address = GameServers.AddressFor(server, Ip);

            var join = new
            {
                ClientPort = 0,
                MachineAddress = address,
                ServerPort = server.Port,
                PingUrl = Config.BaseUrl + "Game/ClientPresence.ashx?version=old&PlaceID=" + server.PlaceId,
                PingInterval = 120,
                UserName = user.Name,
                SeleniumTestMode = false,
                UserId = user.Id,
                SuperSafeChat = user.SuperSafeChat,
                CharacterAppearance = Config.BaseUrl + "Asset/CharacterFetch.ashx?userId=" + user.Id + "&placeId=" + server.PlaceId,
                ClientTicket = ticket.Value,
                GameId = server.JobId,
                PlaceId = server.PlaceId,
                MeasurementUrl = "",
                WaitingForCharacterGuid = Guid.NewGuid().ToString(),
                BaseUrl = Config.BaseUrl,
                ChatStyle = "ClassicAndBubble",
                VendorId = 0,
                ScreenShotInfo = "",
                VideoInfo = "",
                CreatorId = place != null ? place.CreatorId : 0,
                CreatorTypeEnum = "User",
                MembershipType = "None",
                AccountAge = user.AccountAgeDays,
                CookieStoreFirstTimePlayKey = "rbx_evt_ftp",
                CookieStoreFiveMinutePlayKey = "rbx_evt_fmp",
                CookieStoreEnabled = true,
                IsRobloxPlace = false,
                GenerateTeleportJoin = false,
                IsUnknownOrUnder13 = user.SuperSafeChat,
                SessionId = Guid.NewGuid().ToString(),
                DataCenterId = 0,
                UniverseId = 0,
                BrowserTrackerId = 0,
                UsePortraitMode = false,
                FollowUserId = 0,
                characterAppearanceId = user.Id,
                NovetusClient = server.Client,
                FilteringEnabled = place != null && place.FilteringEnabled
            };

            WriteSignedScript(Serialize(join));
        }
    }
}
