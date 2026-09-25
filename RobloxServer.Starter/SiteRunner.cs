using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace RobloxServer.Starter
{
    /// <summary>
    /// Runs RobloxServer.Web with IIS Express, like iniciar-servidor.bat: compiles the site first when
    /// bin\RobloxServer.dll is missing, and starts IIS Express again when it stops unexpectedly.
    /// </summary>
    public class SiteRunner : IDisposable
    {
        readonly object sync = new object();
        Process process;
        bool wantRunning;
        DateTime lastStart;
        int quickRestarts;

        public string SitePath { get; private set; }
        public int Port { get; private set; }

        /// <summary>Log lines (IIS Express output and our own messages), from any thread.</summary>
        public event Action<string> Output;

        /// <summary>true when IIS Express is running, from any thread.</summary>
        public event Action<bool> RunningChanged;

        public SiteRunner(string sitePath, int port)
        {
            SitePath = sitePath;
            Port = port;
        }

        public bool IsRunning
        {
            get
            {
                lock (sync)
                {
                    return process != null && !process.HasExited;
                }
            }
        }

        public string Url
        {
            get { return "http://localhost:" + Port + "/"; }
        }

        void Log(string line)
        {
            var handler = Output;
            if (handler != null)
            {
                handler(DateTime.Now.ToString("HH:mm:ss") + "  " + line);
            }
        }

        void SetRunning(bool running)
        {
            var handler = RunningChanged;
            if (handler != null)
            {
                handler(running);
            }
        }

        /// <summary>RobloxServer.Web next to the exe, or up to four folders above it (bin\Release while developing).</summary>
        public static string FindSite()
        {
            string dir = AppDomain.CurrentDomain.BaseDirectory;
            for (int i = 0; i < 5 && dir != null; i++)
            {
                string site = Path.Combine(dir, "RobloxServer.Web");
                if (File.Exists(Path.Combine(site, "Web.config")))
                {
                    return site;
                }
                dir = Path.GetDirectoryName(dir.TrimEnd('\\', '/'));
            }
            return null;
        }

        public static string FindIisExpress()
        {
            return new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetEnvironmentVariable("ProgramFiles(x86)"),
                Environment.GetEnvironmentVariable("ProgramW6432")
            }
            .Where(p => !string.IsNullOrEmpty(p))
            .Select(p => Path.Combine(p, "IIS Express", "iisexpress.exe"))
            .FirstOrDefault(File.Exists);
        }

        static string FindMsBuild()
        {
            string programFilesX86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)") ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string vswhere = Path.Combine(programFilesX86, "Microsoft Visual Studio", "Installer", "vswhere.exe");
            if (!File.Exists(vswhere))
            {
                return null;
            }
            string output = RunAndRead(vswhere, "-latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\\**\\Bin\\MSBuild.exe");
            return (output ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()).FirstOrDefault(File.Exists);
        }

        static string RunAndRead(string exe, string arguments)
        {
            var info = new ProcessStartInfo(exe, arguments)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };
            using (Process p = Process.Start(info))
            {
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                return output;
            }
        }

        /// <summary>Compiles the solution when the site was never built (a fresh clone). Returns false on failure.</summary>
        bool EnsureBuilt()
        {
            if (File.Exists(Path.Combine(SitePath, "bin", "RobloxServer.dll")))
            {
                return true;
            }

            string solution = Path.Combine(Path.GetDirectoryName(SitePath), "RobloxServer.sln");
            string msbuild = FindMsBuild();
            if (msbuild == null || !File.Exists(solution))
            {
                Log("O site ainda não foi compilado e o MSBuild não foi encontrado.");
                Log("Baixe o site já compilado em Actions > última execução > Artifacts > RobloxServer-site,");
                Log("ou instale o Visual Studio (ou Build Tools) com \"ASP.NET and web development\".");
                return false;
            }

            foreach (string framework in new[] { null, "v4.8" })
            {
                Log(framework == null ? "Compilando o RobloxServer..." : "Tentando compilar para o .NET Framework 4.8...");
                string args = "\"" + solution + "\" /nologo /v:minimal /p:Configuration=Release" + (framework != null ? " /p:TargetFrameworkVersion=" + framework : "");
                if (RunLogged(msbuild, args) == 0 && File.Exists(Path.Combine(SitePath, "bin", "RobloxServer.dll")))
                {
                    return true;
                }
            }
            Log("A compilação falhou. Veja as mensagens acima.");
            return false;
        }

        int RunLogged(string exe, string arguments)
        {
            var info = new ProcessStartInfo(exe, arguments)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (Process p = new Process { StartInfo = info })
            {
                p.OutputDataReceived += (s, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) Log(e.Data); };
                p.ErrorDataReceived += (s, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) Log(e.Data); };
                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                p.WaitForExit();
                return p.ExitCode;
            }
        }

        /// <summary>Builds (if needed) and starts IIS Express. Blocking: call from a worker thread.</summary>
        public bool Start()
        {
            lock (sync)
            {
                if (process != null && !process.HasExited)
                {
                    return true;
                }
                wantRunning = true;
                quickRestarts = 0;
            }

            if (!EnsureBuilt())
            {
                lock (sync)
                {
                    wantRunning = false;
                }
                return false;
            }
            return Launch();
        }

        bool Launch()
        {
            string iisExpress = FindIisExpress();
            if (iisExpress == null)
            {
                Log("IIS Express não encontrado. Baixe em https://www.microsoft.com/download/details.aspx?id=48264");
                Log("(ou use o IIS completo, veja docs\\setting-up.md).");
                lock (sync)
                {
                    wantRunning = false;
                }
                return false;
            }

            var info = new ProcessStartInfo(iisExpress, "/path:\"" + SitePath.TrimEnd('\\') + "\" /port:" + Port + " /clr:v4.0")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true
            };

            var p = new Process { StartInfo = info, EnableRaisingEvents = true };
            p.OutputDataReceived += (s, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) Log(e.Data); };
            p.ErrorDataReceived += (s, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) Log(e.Data); };
            p.Exited += OnExited;

            lock (sync)
            {
                if (!wantRunning)
                {
                    return false;
                }
                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                process = p;
                lastStart = DateTime.UtcNow;
            }

            Log("RobloxServer rodando em " + Url);
            SetRunning(true);
            return true;
        }

        void OnExited(object sender, EventArgs e)
        {
            bool restart;
            int code = 0;
            try
            {
                code = ((Process)sender).ExitCode;
            }
            catch (InvalidOperationException)
            {
            }

            lock (sync)
            {
                if (sender != process)
                {
                    return;
                }
                process = null;
                restart = wantRunning;
                if (restart)
                {
                    // Crashing right after starting over and over (port in use...): give up after 5 tries.
                    quickRestarts = DateTime.UtcNow - lastStart < TimeSpan.FromMinutes(1) ? quickRestarts + 1 : 0;
                    if (quickRestarts >= 5)
                    {
                        restart = false;
                        wantRunning = false;
                    }
                }
            }

            SetRunning(false);
            if (!restart)
            {
                Log(code == 0 ? "O site foi parado." : "O IIS Express fechou (código " + code + ") e não foi reiniciado. A porta " + Port + " está livre?");
                return;
            }

            Log("O IIS Express fechou (código " + code + "). Reiniciando em 5 segundos...");
            ThreadPool.QueueUserWorkItem(_ =>
            {
                Thread.Sleep(5000);
                Launch();
            });
        }

        /// <summary>Stops IIS Express ("Q" on its console, like closing iniciar-servidor.bat).</summary>
        public void Stop()
        {
            Process p;
            lock (sync)
            {
                wantRunning = false;
                p = process;
            }
            if (p == null)
            {
                return;
            }

            try
            {
                if (!p.HasExited)
                {
                    p.StandardInput.WriteLine("Q");
                    if (!p.WaitForExit(5000))
                    {
                        p.Kill();
                        p.WaitForExit(5000);
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Não foi possível parar o IIS Express: " + ex.Message);
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
