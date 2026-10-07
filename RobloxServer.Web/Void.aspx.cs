using System;
using System.Security.Cryptography;
using System.Text;
using RobloxServer.Web;

namespace RobloxServer.Pages
{
    /// <summary>ARG do Noli (/Void): quatro etapas baseadas nos fatos do mito original, depois a explicacao final.</summary>
    public partial class VoidPage : BasePage
    {
        int Stage
        {
            get { object o = ViewState["stage"]; return o == null ? 1 : (int)o; }
            set { ViewState["stage"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Render();
        }

        void Render()
        {
            RiddleLiteral.Text = Server.HtmlEncode(NoliArg.Riddles[Stage - 1]);
            HintLiteral.Text = Server.HtmlEncode(NoliArg.Hints[Stage - 1]);
        }

        protected void SpeakButton_Click(object sender, EventArgs e)
        {
            string answer = AnswerBox.Text;
            AnswerBox.Text = "";
            if (!NoliArg.Check(Stage, answer)) { AngeredLabel.Visible = true; return; }

            if (Stage < NoliArg.Riddles.Length)
            {
                Stage = Stage + 1;
                Render();
                return;
            }
            AskPanel.Visible = false;
            RevealPanel.Visible = true;
            WhisperLiteral.Text = Server.HtmlEncode(NoliArg.Ending);
        }
    }

    static class NoliArg
    {
        public const string Ending = "There was never a hacker. noli was a username with a badly wiped database row: no avatar, no last seen, a profile that sends you home. Someone fixed the query years later and the question mark stopped appearing. Nobody banned anybody. Thank you for waiting.";

        // SHA-256 de "noli|<estagio>|<resposta normalizada>" (minusculas, sem espacos)
        static readonly string[][] Accepted =
        {
            new[] { "55d34105a81cd85dca8f2fa832f8fbcc8ccd7d04643abb2b7dbe2961a9448f9d", "6df291653432b474ac9d1d216aefc4e10c0ca2ac1fec1ab12dcbe285754e2ca2" },
            new[] { "4995bbe80df02e1effbf540bbaf7887f9ec892942b50b07d268583d5ffde1f9f" },
            new[] { "5f1577cebebd1fc6007b42c72149adc9fefddb6e77de52e7030e3bc64fd29d8c" },
            new[] { "6da3a446d22896365c757278566e31258d144e11272abea3736db710e6a8cd94" }
        };

        public static readonly string[] Riddles =
        {
            "A stranger searched a name and found a profile that sent him home. He wrote about it on the forum. When?",
            "He was not looking for me. He was typing another name and slipped. Which name?",
            "They say I wore the first star. They are wrong. Who wore it first?",
            "Someone pushed me to touch it. My friend, my partner in crime. Who?"
        };

        public static readonly string[] Hints = { "dd/mm/yyyy", "a name", "a username", "a username" };

        public static bool Check(int stage, string answer)
        {
            string text = "noli|" + stage + "|" + (answer ?? "").Trim().ToLowerInvariant().Replace(" ", "");
            string hash;
            using (SHA256 h = SHA256.Create())
            {
                hash = BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "").ToLowerInvariant();
            }
            foreach (string ok in Accepted[stage - 1])
            {
                if (ok == hash) { return true; }
            }
            return false;
        }
    }
}
