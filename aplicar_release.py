#!/usr/bin/env python3
"""Faz o site baixar os clients 2007-2013 do GitHub Release (v0.1.0-alpha) quando o zip nao esta no site.

Funciona com quebras de linha do Windows (CRLF) ou Linux (LF), e pode rodar mais de uma vez.
Uso (dentro da pasta do repositorio):   python aplicar_release.py
"""
import os, sys

E = []

E.append(('RobloxServer.Web/Code/Config.cs',
'''        public static string DataPath
''',
'''        /// <summary>Folder (GitHub Release) with one zip per client, e.g. .../releases/download/v0.1.0-alpha/2009E.zip.</summary>
        public static string ClientsReleaseUrl
        {
            get
            {
                string url = Get("ClientsReleaseUrl", "");
                if (string.IsNullOrWhiteSpace(url)) url = "https://github.com/V0rtexLinux/RobloxServer/releases/download/v0.1.0-alpha/";
                url = url.Trim();
                return url.EndsWith("/") ? url : url + "/";
            }
        }

        public static string DataPath
'''))
E.append(('RobloxServer.Web/Install/Download.ashx.cs',
'''                    // A client this site does not host itself: send the person to the archive.org collection.
                    Response.Redirect(Config.ClientsArchiveUrl, false);
''',
'''                    // A client this site does not host itself: send the person to the zip in the GitHub Release.
                    Response.Redirect(Config.ClientsReleaseUrl + name + ".zip", false);
'''))
E.append(('RobloxServer.Web/Install/Download.ashx.cs',
'''a client
    /// without a zip in App_Data/Clients goes to Config.ClientsArchiveUrl.
''',
'''a client
    /// without a zip in App_Data/Clients goes to its zip in the GitHub Release (Config.ClientsReleaseUrl).
'''))
E.append(('RobloxServer.Web/Web.config',
'''    <add key="ClientsArchiveUrl" value="" />
''',
'''    <add key="ClientsArchiveUrl" value="" />
    <!-- Folder of the GitHub Release with the client zips; /Install/Download.ashx?client=2009E goes to <this>2009E.zip when that zip is not in App_Data/Clients (empty: the v0.1.0-alpha release of this repository). -->
    <add key="ClientsReleaseUrl" value="" />
'''))

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
