<div align=center>
  <img src="https://cdn.discordapp.com/attachments/1168613040040710144/1168613089315389571/Roblox-Logo-2015-500x281.png?ex=655266c0&is=653ff1c0&hm=69be43efadd5290b3533620cd5d7ae9a88a7f07fa536d7a7a37b93fc0cfcd93e&" alt="RS Logo">

  ### RobloxServer é um servidor privado open-source do Roblox 2013!

</div>
<br>

## Procurando a documentação? Clique [aqui](docs/welcome.md).

### Rodando no seu PC (Windows) em 1 minuto

> O `127.0.0.1:8081` que aparece nos logs do GitHub Actions é uma máquina temporária do GitHub, usada só
> para testar e apagada logo depois. O site precisa rodar no **seu** computador.

1. Baixe o site já compilado: aba **Actions** → última execução verde → **Artifacts** → `RobloxServer-site`
   (ou clone o repositório e tenha o Visual Studio / Build Tools instalado, o script compila sozinho).
2. Instale o [IIS Express](https://www.microsoft.com/download/details.aspx?id=48264) se ainda não tiver.
3. Dê dois cliques em **`IniciarServidor.exe`** (ou no antigo `iniciar-servidor.bat`). O navegador abre `http://localhost:8080/`.
4. Crie a primeira conta em *Sign Up* (ela vira administradora) e publique um jogo em *Build*.
5. Coloque os clientes em `RobloxServer.Web/App_Data/Clients/2012M.zip` e `2013M.zip`, clique em **Download ROBLOX**,
   rode o launcher uma vez e depois é só clicar em **Play** na página do jogo. Os servidores são criados pelo bot HOST (veja [Modo bot](#modo-bot-servidores-247)).

O **`IniciarServidor.exe`** abre uma janela com o estado do site e:

* inicia o site no IIS Express (compila antes, se precisar) e o reinicia sozinho se ele fechar;
* **mantém o computador ligado**: enquanto estiver aberto, o Windows não entra em suspensão (a tela ainda pode apagar);
* pode **iniciar junto com o Windows** (já minimizado, perto do relógio) e fica na área de notificação quando minimizado.
  Uso: `IniciarServidor.exe [--port 8080] [--minimized]`.

* com **Aceitar outros PCs (Radmin VPN / rede local)** marcado, amigos entram pelo seu IP do Radmin (`http://26.x.x.x:8080/`)
  ou da rede. O Windows pede permissão de administrador uma vez para liberar a porta do site e a UDP 53640 no firewall.

Sem essa opção, o IIS Express só atende este PC (quem vem de fora vê *Bad Request - Invalid Hostname*).
Para a Internet e o Raspberry Pi, use o IIS completo: [docs/setting-up.md](docs/setting-up.md).

RobloxServer agora é um site **ASP.NET clássico (Web Forms, .NET Framework 4.6, C# 6 / Visual Studio 2015)**,
com handlers `.ashx` e páginas `.aspx` como o roblox.com de 2015. Todo o JavaScript (Node/Express) foi removido.

Os jogos publicados aqui são jogados pelo **[RobloxPlayerLauncher](https://github.com/V0rtexLinux/RobloxServerLauncher)**,
o launcher oficial do site no estilo de 2013 (botão **Play** → o jogo abre), só com os clientes **2012M** e **2013M**.
Um **Raspberry Pi Zero 2W** faz o port forwarding, o UPnP e o DNS da rede. Veja [docs/playing.md](docs/playing.md).

### Estrutura

| Pasta | O que tem |
| --- | --- |
| `RobloxServer.Web/` | O site ASP.NET (abra `RobloxServer.sln` no Visual Studio 2015 ou mais novo) |
| `RobloxServer.Web/Game`, `Asset`, `Login`, ... | Handlers `.ashx` que os clientes 2015 chamam (`/Game/Visit.ashx`, `/asset/?id=`, ...) |
| `RobloxServer.Web/Api` | JSON usado pelo launcher (jogos, servidores) |
| `RobloxServer.Web/Install` | `/install/version.ashx` e `/install/download.ashx`: clientes 2012M/2013M e o launcher |
| `RobloxServer.Starter/` | `IniciarServidor.exe`: inicia o site no IIS Express e mantém o PC ligado |
| `RobloxServer.Web/App_Data` | Templates dos scripts; em execução guarda usuários, places, chaves e logs |
| `pi/` | Instalação do Raspberry Pi Zero 2W (port forwarding, UPnP, DNS, nginx, Mono opcional) |
| `tests/smoke_test.py` | Teste de ponta a ponta usado no CI |

### Recursos

* Visual do roblox.com de 2013: barra azul com o logo vermelho, submenu preto, página de jogos com filtro por gênero e os ícones originais (gênero, "no gear", 13+), botões verdes/azuis do StyleGuide e rodapé azul. As imagens ficam em `RobloxServer.Web/Images`.
* Contas com cookie `.ROBLOSECURITY` (Forms Authentication, igual ao de 2015), cadastro com "sou menor de 13" (SuperSafeChat).
* Publicação de jogos pelo site (`Develop.aspx`) ou pelo Studio 2015 (`/Data/Upload.ashx`).
* `Visit.ashx`, `Join.ashx` e `GameServer.ashx` assinados com `--rbxsig` (RSA 1024 + SHA-1); os scripts de jogo 2012M/2013M
  (`App_Data/Templates/Join.lua` e `GameServer.lua`) seguem a conexão clássica do RBXPri/Novetus (licença MIT).
* Botão **Play** que abre o RobloxPlayerLauncher (`robloxserver-player:`), com a janela "Starting ROBLOX...". Não há mais botão **Host Server**: quem hospeda é o [bot HOST](#modo-bot-servidores-247).
* Clientes e launcher distribuídos pelo próprio site (`/install/`), com versão pelo SHA-256; a página Admin mostra o que está instalado.
* `PlaceLauncher.ashx`, tickets de autenticação de uso único, `Login/Negotiate.ashx`, `ValidateTicket.ashx`.
* `/GetAllowedMD5Hashes/` e `/GetAllowedSecurityVersions/` com `apiKey`.
* Master server compatível com Novetus (`list.php`, `delist.php`, `serverlist.txt`).
* Moderação estilo 2015: ban por tempo / "Account Deleted", página `NotApproved.aspx`, ban de IP, filtro de palavras.
* Limite de requisições por IP (45 a cada 30 s, o mesmo do servidor antigo) e flood check no login.
* Assets que não estão no servidor são redirecionados para o `assetdelivery.roblox.com`, como antes.

Veja [docs/security-2015.md](docs/security-2015.md) para a lista completa das medidas de segurança (e das fraquezas mantidas de propósito).

### Robux e Tix

Moedas **virtuais** (sem dinheiro de verdade). Todo dia de login rende T$ 10, e membros do Builders Club ganham também o salário diário de Robux (BC 15, TBC 35, OBC 60). No catálogo, o item custa R$ preço ou T$ preço x 10. O saldo aparece no topo; a página `Money.aspx` mostra o saldo e, para admin, permite dar ou tirar saldo de um usuário.

### Modo bot (servidores 24/7)

Todo jogo publicado (os que já existem e os novos) ganha um servidor mantido por um bot chamado **HOST**, sem ninguém
clicar em nada. O bot usa a mesma função do antigo botão *Host Server* (`GameStarter.Host` do launcher), só que sem entrar no jogo.

* **Conta HOST:** criada sozinha na primeira vez, sem senha utilizável. Só ela pode hospedar (`PlaceService.CanHost`), e isso vale
  também no servidor, não só na tela. Crie a primeira conta (administradora) antes de iniciar os bots.
* **Lista de jogos:** o launcher pergunta ao site (`Game/Servers.ashx?action=botlist`) a cada 60 s. Jogo novo ganha servidor em cerca de
  um minuto, jogo apagado perde o dele. Se o servidor cair, ou o site reiniciar e esquecer o job, o bot sobe de novo (com espera crescente).
* **Portas UDP:** uma por jogo, a partir da `53641`, fixas em `App_Data/BotPorts.txt`. Libere-as no firewall e no roteador
  (o Cloudflare Tunnel leva só HTTP/HTTPS, não UDP). Para quem está fora da rede, defina `PublicGameAddress` no `Web.config`.
* **Ajustes opcionais** em `App_Data/BotServers.txt` (uma linha cada):
  `placeId;porta;endereco` define porta e endereço de um jogo, `!placeId` desliga o bot desse jogo, `#` é comentário.
* **Peso:** cada jogo é um cliente Roblox rodando 24/7. Em máquina fraca use `!placeId` nos jogos que não precisam ficar abertos.

**Chave dos bots.** Nada secreto vai para o Git. Três formas, da mais simples à mais flexível:

| Situação | Como |
| --- | --- |
| Launcher na **mesma máquina** do site | `RobloxPlayerLauncher.exe --bot-host --site-dir C:\caminho\RobloxServer.Web` (usa `http://localhost:8080/` se não passar `--site`). A chave aleatória fica em `App_Data/bot.key`, criada pelo site ou pelo launcher; apague o arquivo para trocar. |
| Launcher em **outra máquina** | Pareamento único: logado como admin, abra `https://seusite/Game/Servers.ashx?action=botpaircode` (código de 10 min, uso único). Na outra máquina: `RobloxPlayerLauncher.exe --pair --site https://seusite/ --code CODIGO`. Depois: `RobloxPlayerLauncher.exe --bot-host --site https://seusite/`. |
| Chave que você já tem | `RobloxPlayerLauncher.exe --set-key CHAVE` guarda a chave criptografada com o DPAPI do Windows (`%LocalAppData%\RobloxServer\bot.key`, só aquele usuário daquela máquina decifra). `--api-key CHAVE` também funciona, mas deixa a chave no atalho. |

Cada máquina pareada recebe uma chave própria; o site guarda só o hash em `App_Data/bot-keys.txt`. Para revogar uma máquina, apague a linha dela.
O pareamento exige `https://` (ou rede local), porque a chave viaja na resposta.

**Deixar rodando 24/7:** o `IniciarServidor.exe` e o launcher são apps de janela e precisam de uma sessão do Windows aberta
(não rodam bem como serviço). Use login automático e uma Tarefa Agendada "ao fazer logon" para cada um, e desligue a suspensão. O script `hospedagem/instalar-hospedagem.ps1` (PowerShell como Administrador) faz isso tudo: cria as duas tarefas, libera as portas UDP dos bots, desliga a suspensão (inclusive ao fechar a tampa) e, se você passar `-TunnelToken`, instala o Cloudflare Tunnel como serviço. `-Remove` desfaz.

Para uma máquina **sem tela e sem teclado**, `hospedagem/pendrive/` monta um pendrive que instala o Windows 10 sozinho e já liga tudo (veja `montar-pendrive.ps1`). A senha do Windows nunca vai para o Git: ela é passada no script.

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
