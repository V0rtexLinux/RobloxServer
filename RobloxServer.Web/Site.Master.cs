using System;
using System.Web.UI;
using RobloxServer.Data;
using RobloxServer.Security;

namespace RobloxServer
{
    public partial class SiteMaster : MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            User user = Auth.CurrentUser;
            LoggedIn.Visible = user != null;
            LoggedOut.Visible = user == null;
            AdminLink.Visible = Db.IsAdmin(user);
            if (user != null)
            {
                UserNameLiteral.Text = Server.HtmlEncode(user.Name);
            }
        }

        protected void LogoutButton_Click(object sender, EventArgs e)
        {
            Auth.SignOut(Context);
            Response.Redirect("~/Default.aspx", false);
        }
    }
}
