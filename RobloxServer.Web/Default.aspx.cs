using System;
using System.Linq;
using RobloxServer.Data;
using RobloxServer.Web;

namespace RobloxServer.Pages
{
    public partial class Default : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            User user = CurrentUser;
            var servers = GameServers.All();
            var games = Db.Places.All()
                .Where(p => p.IsPublic || (user != null && (p.CreatorId == user.Id || IsAdmin)))
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.CreatorName,
                    p.Client,
                    p.Visits,
                    Playing = servers.Where(s => s.PlaceId == p.Id).Sum(s => s.PlayerCount)
                })
                .OrderByDescending(g => g.Playing)
                .ThenByDescending(g => g.Visits)
                .ToList();

            GamesRepeater.DataSource = games;
            GamesRepeater.DataBind();
            EmptyMessage.Visible = games.Count == 0;
        }
    }
}
