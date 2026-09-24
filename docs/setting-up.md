# Instalando o RobloxServer (Windows / IIS)

O site é um *Web Application* ASP.NET 4.6 (Visual Studio 2015). Ele roda no IIS, no IIS Express
ou no Mono (veja o [modo local do Raspberry Pi](raspberry-pi.md)).

## 1. Compilar

* Instale o Visual Studio 2015 (ou mais novo) com "ASP.NET and web development" e o .NET Framework 4.6.
* Abra `RobloxServer.sln` e compile em **Release** (gera `RobloxServer.Web\bin\RobloxServer.dll`).
  * Sem Visual Studio: `msbuild RobloxServer.sln /p:Configuration=Release`.

## 2. Publicar no IIS

1. Ative o IIS com **ASP.NET 4.6** (Painel de Controle → Recursos do Windows → IIS → Recursos de Desenvolvimento de Aplicativos → ASP.NET 4.x).
2. Crie um site apontando para a pasta `RobloxServer.Web`, porta **80**, sem *host name* (os clientes vão chegar como `www.roblox.com`).
3. O Application Pool deve ser **.NET CLR v4.0, Integrated**.
4. Dê permissão de escrita em `RobloxServer.Web\App_Data` para `IIS AppPool\<nome do pool>` (é onde ficam usuários, places, chaves e logs).
5. Abra `http://localhost/` e crie a primeira conta: **ela vira administradora**.

> Para testar sem IIS: no Visual Studio aperte F5 (IIS Express).

## 3. Configurar (`Web.config` → `<appSettings>`)

| Chave | Para que serve |
| --- | --- |
| `BaseUrl` | URL que os clientes usam (padrão `http://www.roblox.com/`) |
| `ApiKey` | apiKey dos servidores de jogo (estilo RCC). Vazio = aberto |
| `Administrators` | nomes de administradores, separados por vírgula |
| `HostPolicy` | quem pode hospedar: `Anyone`, `Owner` ou `Admin` |
| `RequireAuthTickets` | exige ticket de autenticação de cada jogador |
| `AllowedMD5Hashes` / `AllowedSecurityVersions` | listas de verificação do cliente de 2015 |
| `TrustedProxies` | IP do Raspberry Pi, para confiar no `X-Forwarded-For` |
| `PublicGameAddress` | IP público ou nome DDNS mostrado a quem está fora da rede |
| `RateLimitRequests` / `RateLimitWindowSeconds` | limite por IP (45 / 30 s) |

## 4. Fazer os clientes acharem o servidor

Os clientes de 2015 chamam `http://www.roblox.com/`. Escolha uma opção:

* **Raspberry Pi com dnsmasq** (recomendado, vale para a rede toda): [raspberry-pi.md](raspberry-pi.md).
* **Arquivo hosts** em cada PC: `192.168.1.2 www.roblox.com api.roblox.com assetgame.roblox.com`.
* **Fiddler Classic**: regra `AutoResponder`/`FiddlerScript` redirecionando `www.roblox.com` para o servidor.

## 5. Assinatura de scripts

Na primeira execução o servidor cria uma chave RSA de 1024 bits em `App_Data\Keys`.
Os clientes 2015 só executam `Visit.ashx`/`Join.ashx` assinados com a chave embutida neles, então
substitua a chave pública do Roblox no executável pela sua (`App_Data\Keys\PublicKeyBlob.txt`,
ou `http://www.roblox.com/Keys/PublicKey.ashx`). É o mesmo procedimento de qualquer revival de 2015.
