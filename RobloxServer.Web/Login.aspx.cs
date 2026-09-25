using System;
using RobloxServer.Data;
using RobloxServer.Security;
using RobloxServer.Web;

namespace RobloxServer.Pages
{
    public partial class Login : BasePage
    {
        protected override bool AllowBanned
        {
            get { return true; }
        }

        protected void LoginButton_Click(object sender, EventArgs e)
        {
            string error;
            User user = Auth.Login(UserNameBox.Text, PasswordBox.Text, Context, out error);
            if (user == null)
            {
                ErrorLabel.Text = Server.HtmlEncode(error);
                return;
            }

            Auth.SignIn(user, Context);

            string returnUrl = Request.QueryString["ReturnUrl"];
            if (user.IsCurrentlyBanned)
            {
                returnUrl = "~/NotApproved.aspx";
            }
            else if (string.IsNullOrEmpty(returnUrl) || !returnUrl.StartsWith("/") || returnUrl.StartsWith("//") || returnUrl.StartsWith("/\\"))
            {
                returnUrl = "~/Default.aspx";
            }
            Response.Redirect(returnUrl, false);
        }
    }
}
