#!/usr/bin/env python3
"""Aplica as mudancas dos clients 2007-2013 direto nos arquivos do repositorio RobloxServer.

Funciona com quebras de linha do Windows (CRLF) ou Linux (LF), e pode rodar mais de uma vez.
Uso (dentro da pasta do repositorio):   python aplicar_mudancas.py
"""
import os, sys

E = []  # (arquivo, trecho antigo, trecho novo)  -- escritos com \n; o script converte

E.append(('RobloxServer.Web/Code/Config.cs',
'''        /// <summary>The only clients RobloxPlayerLauncher knows how to install and start.</summary>
        public static readonly string[] SupportedClients = { "2012M", "2013M" };
''',
'''        /// <summary>
        /// Clients this site can host and list games for: 2007-2013, in the Novetus naming (year + E early, M mid, L late).
        /// Only 2012M and 2013M have join/host scripts (App_Data/Templates); the others are hosted and labelled only.
        /// </summary>
        public static readonly string[] SupportedClients =
        {
            "2007E", "2007M", "2007L", "2008E", "2008M", "2008L", "2009E", "2009M", "2009L",
            "2010E", "2010M", "2010L", "2011E", "2011M", "2011L", "2012E", "2012M", "2012L",
            "2013E", "2013M", "2013L"
        };
'''))
E.append(('RobloxServer.Web/Code/Config.cs',
'''        public static string DataPath
''',
'''        /// <summary>Where a client download goes when its zip is not in App_Data/Clients (ROBLOX 2007-2013 collection on archive.org).</summary>
        public static string ClientsArchiveUrl
        {
            get
            {
                string url = Get("ClientsArchiveUrl", "");
                return string.IsNullOrWhiteSpace(url) ? "https://archive.org/download/ROBLOX20072013/ROBLOX%202007-2013.zip" : url.Trim();
            }
        }

        public static string DataPath
'''))
E.append(('RobloxServer.Web/Code/Data/ClientPackages.cs',
'''    ///   App_Data/Clients/2012M.zip, App_Data/Clients/2013M.zip  (one zip per client)
''',
'''    ///   App_Data/Clients/2012M.zip, App_Data/Clients/2013M.zip, 2009E.zip...  (one zip per client, 2007-2013)
'''))
E.append(('RobloxServer.Web/Code/Data/PlaceService.cs',
'''(2006S...2011M from the Novetus days) are played''',
'''(2006S and other names outside 2007-2013) are played'''))
E.append(('RobloxServer.Web/Install/Download.ashx.cs',
'''    /// launcher in App_Data/Launcher the launcher download goes to Config.LauncherDownloadUrl.
''',
'''    /// launcher in App_Data/Launcher the launcher download goes to Config.LauncherDownloadUrl; a client
    /// without a zip in App_Data/Clients goes to Config.ClientsArchiveUrl.
'''))
E.append(('RobloxServer.Web/Install/Download.ashx.cs',
'''                WriteStatus(404, "Not found");
''',
'''                if (name != null)
                {
                    // A client this site does not host itself: send the person to the archive.org collection.
                    Response.Redirect(Config.ClientsArchiveUrl, false);
                    return;
                }
                WriteStatus(404, "Not found");
'''))
E.append(('RobloxServer.Web/Landing.aspx',
'''<a href="<%: ResolveUrl("~/Install/Download.ashx?client=Launcher") %>">Download</a> &nbsp;|&nbsp; <a href="https://github.com/V0rtexLinux/RobloxServer">Source Code</a>''',
'''<a href="<%: ResolveUrl("~/Install/Download.ashx?client=Launcher") %>">Download</a> &nbsp;|&nbsp;
                        <a href="<%: RobloxServer.Config.ClientsArchiveUrl %>" target="_blank" rel="noopener">Clients 2007-2013</a> &nbsp;|&nbsp; <a href="https://github.com/V0rtexLinux/RobloxServer">Source Code</a>'''))
E.append(('RobloxServer.Web/Web.config',
'''    <!-- Only 2012M and 2013M are supported by RobloxPlayerLauncher; other names are ignored. -->
    <add key="Clients" value="2012M,2013M" />
''',
'''    <!-- Clients games can be published for (2007-2013, year + E/M/L). Only 2012M and 2013M have join/host scripts. Remove the ones you do not install. -->
    <add key="Clients" value="2007E,2007M,2007L,2008E,2008M,2008L,2009E,2009M,2009L,2010E,2010M,2010L,2011E,2011M,2011L,2012E,2012M,2012L,2013E,2013M,2013L" />
'''))
E.append(('RobloxServer.Web/Web.config',
'''    <add key="LauncherDownloadUrl" value="" />
''',
'''    <add key="LauncherDownloadUrl" value="" />
    <!-- Where /Install/Download.ashx?client=2012M|2013M goes when that zip is not in App_Data/Clients (empty: ROBLOX 2007-2013 on archive.org). -->
    <add key="ClientsArchiveUrl" value="" />
'''))
E.append(('docs/playing.md',
'''Só os clientes **2012M** e **2013M** são suportados.''',
'''O site hospeda e lista jogos dos clientes **2007 a 2013** (2007E, 2007M, 2007L ... 2013L). Só o **2012M** e o **2013M** têm script de entrar/hospedar; os outros são hospedados e rotulados, mas o launcher ainda não os inicia.'''))
E.append(('tests/smoke_test.py', 'name=Old&client=2009E&', 'name=Old&client=2006S&'))
E.append(('tests/smoke_test.py', '"unsupported clients (2009E) become 2012M"', '"unsupported clients (2006S) become 2012M"'))
E.append(('tests/smoke_test.py', 'anon.get("/install/version.ashx?client=2009E")', 'anon.get("/install/version.ashx?client=2006S")'))

root = sys.argv[1] if len(sys.argv) > 1 else '.'
cache, problemas, feitos = {}, [], 0
for path, old, new in E:
    full = os.path.join(root, path)
    if not os.path.isfile(full):
        problemas.append('arquivo nao encontrado: ' + path + ' (rode dentro da pasta do repositorio)')
        continue
    if full not in cache:
        raw = open(full, 'rb').read().decode('utf-8')
        cache[full] = [raw, '\r\n' if '\r\n' in raw else '\n', False]
    raw, eol, _ = cache[full]
    o, n = old.replace('\n', eol), new.replace('\n', eol)
    if n in raw:
        continue                      # ja aplicado
    if raw.count(o) != 1:
        problemas.append(f'nao achei o trecho em {path}: {old.strip().splitlines()[0][:70]}')
        continue
    cache[full][0] = raw.replace(o, n, 1)
    cache[full][2] = True
    feitos += 1
for full, (raw, eol, changed) in cache.items():
    if changed:
        open(full, 'wb').write(raw.encode('utf-8'))
print(f'{feitos} alteracoes aplicadas.')
if problemas:
    print('PROBLEMAS:')
    for p in problemas:
        print('  -', p)
    sys.exit(1)
print('Tudo certo.')
