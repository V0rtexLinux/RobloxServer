using System;

namespace RobloxServer.Data
{
    /// <summary>
    /// Robux (R$) e Tix (T$) virtuais: nenhum dinheiro de verdade. Tix diario para todo mundo e salario diario de Robux
    /// para o Builders Club (BC 15, TBC 35, OBC 60). Catalogo: item custa R$ preco ou T$ preco x 10.
    /// </summary>
    public static class Economy
    {
        public const int DailyTix = 10;
        public const int TixPerRobux = 10;
        const int MaxBalance = 1000000000;
        static readonly object Sync = new object();

        public static int Stipend(string tier)
        {
            switch (tier)
            {
                case "BC": return 15;
                case "TBC": return 35;
                case "OBC": return 60;
                default: return 0;
            }
        }

        /// <summary>Paga o Tix diario e o salario do Builders Club uma vez por dia (UTC).</summary>
        public static void ClaimDaily(User user)
        {
            if (user == null)
            {
                return;
            }
            DateTime today = DateTime.UtcNow.Date;
            lock (Sync)
            {
                User current = Db.Users.Find(u => u.Id == user.Id);
                if (current == null || current.LastDaily >= today)
                {
                    return;
                }
                int robux = Stipend(SocialStore.Tier(user.Id));
                Db.Users.Update(u => u.Id == user.Id, u =>
                {
                    u.LastDaily = today;
                    u.Tix = Clamp(u.Tix + DailyTix);
                    u.Robux = Clamp(u.Robux + robux);
                });
            }
        }

        static int Clamp(long value)
        {
            return (int)Math.Max(0, Math.Min(MaxBalance, value));
        }

        /// <summary>Cobra de uma vez so (sem saldo negativo). False se nao tiver saldo.</summary>
        public static bool TryCharge(long userId, int robux, int tix)
        {
            lock (Sync)
            {
                User u = Db.Users.Find(x => x.Id == userId);
                if (u == null || robux < 0 || tix < 0 || u.Robux < robux || u.Tix < tix)
                {
                    return false;
                }
                Db.Users.Update(x => x.Id == userId, x =>
                {
                    x.Robux -= robux;
                    x.Tix -= tix;
                });
                return true;
            }
        }

        /// <summary>Soma (ou tira, com valores negativos) saldo. Usado pelo admin e para estornos.</summary>
        public static void Grant(long userId, int robux, int tix)
        {
            lock (Sync)
            {
                Db.Users.Update(x => x.Id == userId, x =>
                {
                    x.Robux = Clamp((long)x.Robux + robux);
                    x.Tix = Clamp((long)x.Tix + tix);
                });
            }
        }
    }
}
