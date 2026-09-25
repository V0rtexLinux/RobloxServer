<div align=center>
  <img src="https://cdn.discordapp.com/attachments/1168613040040710144/1168613089315389571/Roblox-Logo-2015-500x281.png?ex=655266c0&is=653ff1c0&hm=69be43efadd5290b3533620cd5d7ae9a88a7f07fa536d7a7a37b93fc0cfcd93e&" alt="RS Logo">

  ### RobloxServer é um servidor privado open-source do Roblox 2015!

</div>
<br>

## Procurando a documentação? Clique [aqui](docs/welcome.md).

### Rodando no seu PC (Windows) em 1 minuto

> O `127.0.0.1:8081` que aparece nos logs do GitHub Actions é uma máquina temporária do GitHub, usada só
> para testar e apagada logo depois. O site precisa rodar no **seu** computador.

1. Baixe o site já compilado: aba **Actions** → última execução verde → **Artifacts** → `RobloxServer-site`
   (ou clone o repositório e tenha o Visual Studio / Build Tools instalado, o script compila sozinho).
2. Instale o [IIS Express](https://www.microsoft.com/download/details.aspx?id=48264) se ainda não tiver.
3. Dê dois cliques em **`iniciar-servidor.bat`**. O navegador abre `http://localhost:8080/`.
4. Crie a primeira conta em *Sign Up* (ela vira administradora) e publique um jogo em *Develop*.
5. No launcher: *Server Browser* → *ROBLOXSERVER GAMES...* → endereço `localhost:8080`.

O IIS Express só atende este PC. Para outros PCs da rede, a Internet e o Raspberry Pi, use o IIS completo:
[docs/setting-up.md](docs/setting-up.md).

RobloxServer agora é um site **ASP.NET clássico (Web Forms, .NET Framework 4.6, C# 6 / Visual Studio 2015)**,
com handlers `.ashx` e páginas `.aspx` como o roblox.com de 2015. Todo o JavaScript (Node/Express) foi removido.

Os jogos publicados aqui são jogados pelo **[RobloxServerLauncher](https://github.com/V0rtexLinux/RobloxServerLauncher)**
(Server Browser → *RobloxServer Games*), e um **Raspberry Pi Zero 2W** faz o port forwarding, o UPnP e o DNS da rede.

### Estrutura

| Pasta | O que tem |
| --- | --- |
| `RobloxServer.Web/` | O site ASP.NET (abra `RobloxServer.sln` no Visual Studio 2015 ou mais novo) |
| `RobloxServer.Web/Game`, `Asset`, `Login`, ... | Handlers `.ashx` que os clientes 2015 chamam (`/Game/Visit.ashx`, `/asset/?id=`, ...) |
| `RobloxServer.Web/Api` | JSON usado pelo launcher (login, jogos, servidores) |
| `RobloxServer.Web/App_Data` | Templates dos scripts; em execução guarda usuários, places, chaves e logs |
| `pi/` | Instalação do Raspberry Pi Zero 2W (port forwarding, UPnP, DNS, nginx, Mono opcional) |
| `tests/smoke_test.py` | Teste de ponta a ponta usado no CI |

### Recursos

* Contas com cookie `.ROBLOSECURITY` (Forms Authentication, igual ao de 2015), cadastro com "sou menor de 13" (SuperSafeChat).
* Publicação de jogos pelo site (`Develop.aspx`) ou pelo Studio 2015 (`/Data/Upload.ashx`).
* `Visit.ashx` e `Join.ashx` assinados com `--rbxsig` (RSA 1024 + SHA-1).
* `PlaceLauncher.ashx`, tickets de autenticação de uso único, `Login/Negotiate.ashx`, `ValidateTicket.ashx`.
* `/GetAllowedMD5Hashes/` e `/GetAllowedSecurityVersions/` com `apiKey`.
* Master server compatível com Novetus (`list.php`, `delist.php`, `serverlist.txt`).
* Moderação estilo 2015: ban por tempo / "Account Deleted", página `NotApproved.aspx`, ban de IP, filtro de palavras.
* Limite de requisições por IP (45 a cada 30 s, o mesmo do servidor antigo) e flood check no login.
* Assets que não estão no servidor são redirecionados para o `assetdelivery.roblox.com`, como antes.

Veja [docs/security-2015.md](docs/security-2015.md) para a lista completa das medidas de segurança (e das fraquezas mantidas de propósito).

### Rodando

* **Windows / IIS:** [docs/setting-up.md](docs/setting-up.md)
* **Raspberry Pi Zero 2W:** [docs/raspberry-pi.md](docs/raspberry-pi.md)

### Downloads

* Roblox Studio 2015 Version (January 14):
  + [~~Download Point 1 - Archive~~](https://archive.frickinfire.com/ROBLOX/Executables/Windows/RobloxStudio/ROBLOXArchive/2015/January%2014/)
  + [~~Download Point 2 - Ufile~~](https://ufile.io/2n226a2u)
  + [~~Download Point 3 - Mediafire~~](https://www.mediafire.com/file/sipe7u37cn3f3l8/January_14.tar/file)
  + [Download Point 4 - CatBox](https://files.catbox.moe/xdux95.7z)

* Fiddler Classic (opcional, só para os executáveis oficiais de 2015; o DNS do Raspberry Pi cuida do resto):
  + [Download Point 1 - Telerik](https://www.telerik.com/download/fiddler)
