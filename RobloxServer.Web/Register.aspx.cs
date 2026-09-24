using System;
using RobloxServer.Data;
using RobloxServer.Security;
using RobloxServer.Web;

namespace RobloxServer.Pages
{
    public partial class Register : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            RegisterPanel.Visible = Config.RegistrationOpen;
            ClosedMessage.Visible = !Config.RegistrationOpen;
        }

        protected void RegisterButton_Click(object sender, EventArgs e)
        {
            if (!Config.RegistrationOpen)
            {
                return;
            }

            string name = (UserNameBox.Text ?? "").Trim();
            string error = WordFilter.ValidateUserName(name);
            if (error == null && (PasswordBox.Text ?? "").Length < 6)
            {
                error = "Passwords must be at least 6 characters.";
            }
            if (error == null && PasswordBox.Text != ConfirmBox.Text)
            {
                error = "Passwords do not match.";
            }
            if (error == null && string.Equals(PasswordBox.Text, name, StringComparison.OrdinalIgnoreCase))
            {
                error = "Your password cannot be your username.";
            }
            if (error == null && Db.FindUser(name) != null)
            {
                error = "This username is already in use.";
            }

            // 2015 style signup flood check: 3 accounts per IP per hour.
            string ip = ClientIp.Get(Context);
            if (error == null && FloodChecker.Hit("register:" + ip, 3, TimeSpan.FromHours(1)))
            {
                error = "Too many accounts have been created from your IP. Try again later.";
            }

            if (error != null)
            {
                ErrorLabel.Text = Server.HtmlEncode(error);
                return;
            }

            bool firstUser = Db.Users.All().Count == 0;
            DateTime now = DateTime.UtcNow;
            string hash = PasswordHasher.Hash(PasswordBox.Text);
            User user = Db.Users.InsertWithId(u => u.Id, 1, id => new User
            {
                Id = id,
                Name = name,
                PasswordHash = hash,
                Created = now,
                LastOnline = now,
                LastIp = ip,
                IsAdmin = firstUser,
                SuperSafeChat = Under13Box.Checked
            });

            Logging.Log(LogType.Success, "New account " + user.Name + " (" + user.Id + ") from " + ip + (firstUser ? " [admin]" : ""));
            Auth.SignIn(user, Context);
            Response.Redirect("~/Default.aspx", false);
        }
    }
}
