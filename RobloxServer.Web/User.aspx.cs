using System;
using System.Linq;
using RobloxServer.Data;
using RobloxServer.Web;

namespace RobloxServer.Pages
{
    /// <summary>2013 style profile: User.aspx?id= (or ?username=).</summary>
    public partial class UserProfile : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            long id;
            long.TryParse(Request.QueryString["id"], out id);
            User user = id != 0 ? Db.FindUser(id) : Db.FindUser(Request.QueryString["username"]);

            if (Noli.Is(user))
            {
                // The myth: Noli's profile has no data and opening it sends you back to the home page.
                // The only thing left behind is the first clue of the ARG.
                // (Response.Redirect would drop the X-Void header on Mono.)
                Response.Clear();
                Response.StatusCode = 302;
                Response.RedirectLocation = ResolveUrl("~/Default.aspx");
                Response.AppendHeader("X-Void", Noli.Clue);
                Response.End();
                return;
            }

            if (user == null)
            {
                Response.StatusCode = 404;
                NotFoundPanel.Visible = true;
                ProfilePanel.Visible = false;
                return;
            }

            string name = Server.HtmlEncode(user.Name);
            Title = user.Name;
            HeaderLiteral.Text = name;
            NoPlacesLiteral.Text = name;
            bool online = IsOnline(user);
            StatusLiteral.Text = online ? "<span class=\"UserOnline\">[ Online: Website ]</span>" : "<span class=\"UserOffline\">[ Offline ]</span>";
            UrlLiteral.Text = Server.HtmlEncode(Config.BaseUrl + "User.aspx?id=" + user.Id);
            AvatarImage.Src = ResolveUrl("~/Asset/Avatar.ashx?userId=" + user.Id);
            AvatarImage.Alt = user.Name;
            JoinedLiteral.Text = user.Created.ToString("M/d/yyyy");
            LastOnlineLiteral.Text = online ? "Now" : Server.HtmlEncode(Ago(user.LastOnline));
            AdminBadge.Visible = user.IsAdmin;

            User viewer = CurrentUser;
            var places = Db.Places.Where(p => p.CreatorId == user.Id)
                .Where(p => p.IsPublic || PlaceService.CanEdit(viewer, p))
                .OrderByDescending(p => p.Visits)
                .ToList();
            VisitsLiteral.Text = places.Sum(p => p.Visits).ToString("N0");
            PlacesRepeater.DataSource = places;
            PlacesRepeater.DataBind();
            NoPlaces.Visible = places.Count == 0;
        }
    }
}
