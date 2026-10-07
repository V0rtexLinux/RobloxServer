using System;
using System.Linq;
using System.Text;
using System.Web;
using RobloxServer.Data;
using RobloxServer.Web;

namespace RobloxServer.Pages
{
    /// <summary>
    /// Home page of visitors (2012 look): Member Login, the Play Now buttons, the featured game and the news.
    /// Members are sent to My/Home. No server controls: the login box posts to Login/HeaderLogin.ashx.
    /// </summary>
    public class Welcome : BasePage
    {
        protected bool HasFeatured;
        protected string FeaturedName = "";
        protected string FeaturedBy = "";
        protected string FeaturedUrl = "";
        protected string FeaturedByUrl = "";
        protected string FeaturedThumb = "";
        protected string FeaturedUpdated = "";
        protected string FeaturedVisits = "";
        protected string PlayUrl = "";
        protected string ForgotUrl = "";
        protected string NewsHtml = "";

        protected override bool AllowBanned
        {
            get { return true; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/My/Home.aspx", true);
                return;
            }

            PlayUrl = ResolveUrl("~/Games");
            ForgotUrl = ResolveUrl("~/Login.aspx");

            var servers = GameServers.All();
            var featured = Db.Places.Where(p => p.IsPublic)
                .Select(p => new { Place = p, Playing = servers.Where(s => s.PlaceId == p.Id).Sum(s => s.PlayerCount) })
                .OrderByDescending(x => x.Playing).ThenByDescending(x => x.Place.Visits).ThenByDescending(x => x.Place.Updated)
                .FirstOrDefault();
            if (featured != null)
            {
                Place place = featured.Place;
                HasFeatured = true;
                FeaturedName = place.Name;
                FeaturedBy = place.CreatorName;
                FeaturedUrl = ResolveUrl("~/PlaceItem.aspx?id=" + place.Id);
                FeaturedByUrl = ResolveUrl("~/User.aspx?id=" + place.CreatorId);
                FeaturedThumb = PlaceThumb(place.Id, "420x230");
                FeaturedUpdated = Ago(place.Updated);
                FeaturedVisits = place.Visits.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
                PlayUrl = FeaturedUrl;
            }

            NewsHtml = News();
        }

        string News()
        {
            ForumStore.EnsureSeed();
            ForumBoard board = ForumStore.Boards.All().OrderBy(b => b.CategoryId).ThenBy(b => b.Order).FirstOrDefault();
            var sb = new StringBuilder();
            if (board != null)
            {
                foreach (ForumThread t in ForumStore.Threads.Where(x => x.BoardId == board.Id).OrderByDescending(x => x.Pinned).ThenByDescending(x => x.Created).Take(3))
                {
                    sb.Append("<div class=\"wl-news-item\"><a href=\"").Append(HttpUtility.HtmlAttributeEncode(ResolveUrl("~/Forum.aspx?t=" + t.Id))).Append("\">")
                      .Append(HttpUtility.HtmlEncode(t.Title)).Append("</a></div>");
                }
            }
            if (sb.Length == 0)
            {
                sb.Append("<div class=\"wl-news-item\"><a href=\"").Append(HttpUtility.HtmlAttributeEncode(ResolveUrl("~/Forum.aspx"))).Append("\">No news yet: visit the Forum</a></div>");
            }
            return sb.ToString();
        }

        /// <summary>"4 months ago", like the featured game box.</summary>
        static string Ago(DateTime utc)
        {
            TimeSpan span = DateTime.UtcNow - DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            if (span.TotalMinutes < 1) { return "just now"; }
            if (span.TotalHours < 1) { return Plural((int)span.TotalMinutes, "minute"); }
            if (span.TotalDays < 1) { return Plural((int)span.TotalHours, "hour"); }
            if (span.TotalDays < 30) { return Plural((int)span.TotalDays, "day"); }
            if (span.TotalDays < 365) { return Plural((int)(span.TotalDays / 30), "month"); }
            return Plural((int)(span.TotalDays / 365), "year");
        }

        static string Plural(int n, string unit)
        {
            return n + " " + unit + (n == 1 ? "" : "s") + " ago";
        }
    }
}
