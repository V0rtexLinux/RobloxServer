using System;
using System.IO;
using System.Text;
using RobloxServer.Data;
using RobloxServer.Web;

namespace RobloxServer.Handlers.Game
{
    /// <summary>
    /// Recebe a foto de perfil do personagem. O link assinado vem no Join.lua (so vale para aquele jogador, por 6 h).
    /// POST: corpo = PNG em base64 (o que o ThumbnailGenerator devolve).  GET ...&amp;status=motivo: o cliente avisa que nao conseguiu.
    /// </summary>
    public class AvatarPhoto : HandlerBase
    {
        protected override void Handle()
        {
            User user = Db.FindUser(QueryLong("userId"));
            if (user == null || !AvatarPhotos.ValidSignature(user, QueryLong("expires"), Request.QueryString["sig"]))
            {
                WriteStatus(403, "Invalid or expired link.");
                return;
            }

            string status = Request.QueryString["status"];
            if (!string.IsNullOrEmpty(status))
            {
                Logging.Log(LogType.Backend, "Avatar photo for " + user.Name + " could not be taken: " + (status.Length > 20 ? status.Substring(0, 20) : status));
                WriteText("ok");
                return;
            }

            if (Request.HttpMethod != "POST" || Request.ContentLength <= 0 || Request.ContentLength > 900000)
            {
                WriteStatus(400, "Bad request.");
                return;
            }
            string text;
            using (var reader = new StreamReader(Request.InputStream, Encoding.ASCII))
            {
                text = reader.ReadToEnd();
            }
            int comma = text.IndexOf(',');
            if (text.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma > 0)
            {
                text = text.Substring(comma + 1);
            }
            text = text.Trim().Replace("\r", "").Replace("\n", "").Replace(" ", "");

            byte[] png;
            try
            {
                png = Convert.FromBase64String(text);
            }
            catch (FormatException)
            {
                WriteStatus(400, "Bad data.");
                return;
            }
            string error = AvatarPhotos.Save(user, png);
            if (error != null)
            {
                Logging.Log(LogType.Backend, "Avatar photo for " + user.Name + " refused: " + error);
                WriteStatus(400, error);
                return;
            }
            Logging.Log(LogType.Success, "Avatar photo saved for " + user.Name);
            WriteText("ok");
        }
    }
}
