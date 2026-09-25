using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace RobloxServer.Starter
{
    /// <summary>
    /// The IniciarServidor window: site status, Open / Stop buttons, "keep this computer on",
    /// "start with Windows" and the IIS Express log. Minimizes to the notification area.
    /// </summary>
    public class StarterForm : Form
    {
        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string RunValue = "RobloxServer";

        readonly SiteRunner runner;
        readonly bool startMinimized;
        readonly Label status;
        readonly Button startStop;
        readonly Button open;
        readonly CheckBox keepAwake;
        readonly CheckBox startWithWindows;
        readonly TextBox log;
        readonly NotifyIcon tray;
        bool exiting;

        public StarterForm(string sitePath, int port, bool startMinimized)
        {
            this.startMinimized = startMinimized;
            runner = new SiteRunner(sitePath, port);

            Text = "RobloxServer";
            Icon = LoadIcon();
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(560, 380);
            MinimumSize = new Size(480, 320);
            Font = new Font("Segoe UI", 9f);
            BackColor = Color.White;

            var header = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.FromArgb(0x11, 0x40, 0x81) };
            header.Controls.Add(new Label
            {
                Text = "RobloxServer",
                ForeColor = Color.White,
                Font = new Font("Arial", 15f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(12, 9)
            });

            status = new Label
            {
                Text = "Iniciando...",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Location = new Point(12, 54),
                Size = new Size(536, 22),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            open = new Button { Text = "Abrir o site", Location = new Point(12, 82), Size = new Size(110, 28), Enabled = false };
            open.Click += (s, e) => OpenSite();

            startStop = new Button { Text = "Parar", Location = new Point(130, 82), Size = new Size(110, 28), Enabled = false };
            startStop.Click += (s, e) => ToggleSite();

            keepAwake = new CheckBox
            {
                Text = "Manter o computador ligado (não entrar em suspensão)",
                Checked = true,
                AutoSize = true,
                Location = new Point(12, 120),
                Enabled = KeepAwake.IsSupported
            };
            keepAwake.CheckedChanged += (s, e) => ApplyKeepAwake();

            startWithWindows = new CheckBox
            {
                Text = "Iniciar o site junto com o Windows",
                Checked = IsStartingWithWindows(),
                AutoSize = true,
                Location = new Point(12, 144)
            };
            startWithWindows.CheckedChanged += (s, e) => SetStartWithWindows(startWithWindows.Checked);

            log = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(0xf1, 0xf1, 0xf1),
                Font = new Font("Consolas", 8.5f),
                Location = new Point(12, 174),
                Size = new Size(536, 194),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            Controls.Add(log);
            Controls.Add(startWithWindows);
            Controls.Add(keepAwake);
            Controls.Add(startStop);
            Controls.Add(open);
            Controls.Add(status);
            Controls.Add(header);

            var menu = new ContextMenuStrip();
            menu.Items.Add("Mostrar", null, (s, e) => ShowWindow());
            menu.Items.Add("Abrir o site", null, (s, e) => OpenSite());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Parar e sair", null, (s, e) => ExitApplication());
            tray = new NotifyIcon { Icon = Icon, Text = "RobloxServer", ContextMenuStrip = menu, Visible = true };
            tray.DoubleClick += (s, e) => ShowWindow();

            runner.Output += line => OnUi(() => AppendLog(line));
            runner.RunningChanged += running => OnUi(UpdateStatus);
        }

        static Icon LoadIcon()
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("RobloxServer.Starter.RobloxServer.ico"))
            {
                return stream != null ? new Icon(stream) : SystemIcons.Application;
            }
        }

        void OnUi(Action action)
        {
            if (IsDisposed || !IsHandleCreated)
            {
                return;
            }
            try
            {
                BeginInvoke(action);
            }
            catch (InvalidOperationException)
            {
                // Closing.
            }
        }

        void AppendLog(string line)
        {
            if (log.TextLength > 200000)
            {
                log.Text = log.Text.Substring(100000);
            }
            log.AppendText(line + Environment.NewLine);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ApplyKeepAwake();
            AppendLog("Site: " + runner.SitePath);
            AppendLog("O IIS Express só atende este PC. Para outros PCs da rede use o IIS completo (docs\\setting-up.md).");
            if (startMinimized)
            {
                Hide();
            }
            StartSite();
        }

        void StartSite()
        {
            startStop.Enabled = false;
            status.Text = "Iniciando...";
            Task.Run(() =>
            {
                bool started = runner.Start();
                OnUi(() =>
                {
                    UpdateStatus();
                    if (started && !startMinimized)
                    {
                        OpenSite();
                    }
                });
            });
        }

        void ToggleSite()
        {
            if (runner.IsRunning)
            {
                startStop.Enabled = false;
                Task.Run(() => runner.Stop());
            }
            else
            {
                StartSite();
            }
        }

        void UpdateStatus()
        {
            bool running = runner.IsRunning;
            status.Text = running ? "Rodando em " + runner.Url : "Parado";
            status.ForeColor = running ? Color.FromArgb(0x36, 0xa4, 0x00) : Color.FromArgb(0xc0, 0x00, 0x00);
            startStop.Text = running ? "Parar" : "Iniciar";
            startStop.Enabled = true;
            open.Enabled = running;
            tray.Text = running ? "RobloxServer - rodando na porta " + runner.Port : "RobloxServer - parado";
        }

        void ApplyKeepAwake()
        {
            bool ok = KeepAwake.Set(keepAwake.Checked);
            if (keepAwake.Checked)
            {
                AppendLog(ok ? "O computador não vai entrar em suspensão enquanto este programa estiver aberto."
                             : "Não foi possível impedir a suspensão neste sistema.");
            }
        }

        void OpenSite()
        {
            try
            {
                Process.Start(runner.Url);
            }
            catch (Exception ex)
            {
                AppendLog("Não foi possível abrir o navegador: " + ex.Message);
            }
        }

        void ShowWindow()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        static bool IsStartingWithWindows()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey))
                {
                    return key != null && key.GetValue(RunValue) != null;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        void SetStartWithWindows(bool enabled)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKey))
                {
                    if (enabled)
                    {
                        key.SetValue(RunValue, "\"" + Application.ExecutablePath + "\" --port " + runner.Port + " --minimized");
                    }
                    else
                    {
                        key.DeleteValue(RunValue, false);
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog("Não foi possível mudar a inicialização com o Windows: " + ex.Message);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
                tray.ShowBalloonTip(3000, "RobloxServer", "O site continua rodando. Clique duas vezes aqui para abrir.", ToolTipIcon.Info);
            }
        }

        void ExitApplication()
        {
            exiting = true;
            Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!exiting && e.CloseReason == CloseReason.UserClosing && runner.IsRunning)
            {
                DialogResult answer = MessageBox.Show(this, "Parar o RobloxServer e fechar?\r\n\r\nClique em Não para deixar o site rodando na área de notificação.",
                    "RobloxServer", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (answer == DialogResult.No)
                {
                    e.Cancel = true;
                    Hide();
                    return;
                }
                if (answer == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
            }

            runner.Stop();
            KeepAwake.Set(false);
            tray.Visible = false;
            tray.Dispose();
            base.OnFormClosing(e);
        }
    }
}
