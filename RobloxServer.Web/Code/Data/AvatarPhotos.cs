using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using RobloxServer.Security;
using RobloxServer.Handlers.Asset;

namespace RobloxServer.Data
{
    /// <summary>
    /// Foto de perfil real (PNG) do personagem do jogador, tirada pelo proprio cliente dentro do jogo
    /// (Join.lua: camera de frente + ThumbnailGenerator) e enviada para Game/AvatarPhoto.ashx.
    /// Arquivos em App_Data/Photos/{userId}.png. O cliente manda uma foto nova assim que a aparencia muda.
    /// </summary>
    public static class AvatarPhotos
    {
        const int MaxBytes = 600000;
        static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(1);
        static readonly object Sync = new object();

        static string Dir
        {
            get { return Path.Combine(Config.DataPath, "Photos"); }
        }

        static string PngPath(long userId)
        {
            return Path.Combine(Dir, userId + ".png");
        }

        static string HashPath(long userId)
        {
            return Path.Combine(Dir, userId + ".hash");
        }

        static string Sha(string text)
        {
            using (var sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "").ToLowerInvariant();
            }
        }

        static long NowSeconds()
        {
            return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }

        static string Fingerprint(User user)
        {
            return Sha(BodyColors.XmlFor(user));
        }

        static string Sig(User user, long expires)
        {
            return Sha("photo|" + user.Id + "|" + expires + "|" + user.PasswordHash).Substring(0, 32);
        }

        public static bool ValidSignature(User user, long expires, string sig)
        {
            return user != null && expires > NowSeconds() && PasswordHasher.ConstantTimeEquals(Sig(user, expires), sig ?? "");
        }

        public static bool NeedsPhoto(User user)
        {
            // O cliente confere a aparencia a cada segundo e so manda foto quando ela muda: o site sempre aceita.
            return user != null && !Noli.Is(user);
        }

        /// <summary>Link assinado (24 h) que vai dentro do Join.lua; vazio quando nao precisa de foto nova.</summary>
        public static string UrlFor(User user)
        {
            if (!NeedsPhoto(user))
            {
                return "";
            }
            long expires = NowSeconds() + 24 * 3600;
            return Config.BaseUrl + "Game/AvatarPhoto.ashx?userId=" + user.Id + "&expires=" + expires + "&sig=" + Sig(user, expires);
        }

        /// <summary>Guarda a foto. Devolve null se deu certo, ou o motivo da recusa.</summary>
        public static string Save(User user, byte[] png)
        {
            if (png == null || png.Length < 100 || png.Length > MaxBytes)
            {
                return "bad size";
            }
            if (png[0] != 0x89 || png[1] != 0x50 || png[2] != 0x4E || png[3] != 0x47)
            {
                return "not a png";
            }
            int width = (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];
            int height = (png[20] << 24) | (png[21] << 16) | (png[22] << 8) | png[23];
            if (width < 64 || width > 1024 || height < 64 || height > 1024)
            {
                return "bad dimensions";
            }
            lock (Sync)
            {
                Directory.CreateDirectory(Dir);
                string path = PngPath(user.Id);
                if (File.Exists(path) && DateTime.UtcNow - File.GetLastWriteTimeUtc(path) < MinInterval)
                {
                    return "too fast";
                }
                File.WriteAllBytes(path, png);
                File.WriteAllText(HashPath(user.Id), Fingerprint(user));
            }
            return null;
        }

        public static byte[] Load(User user)
        {
            if (user == null)
            {
                return null;
            }
            string path = PngPath(user.Id);
            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        }
    }
}
