# Jogando pelo RobloxServerLauncher

Os jogos publicados no RobloxServer são jogados pelo
[RobloxServerLauncher](https://github.com/V0rtexLinux/RobloxServerLauncher) (fork do Novetus), com o
cliente escolhido na publicação (2006S … 2012M).

## Publicar um jogo

* Pelo site: **Develop** → escolha nome, descrição, cliente, jogadores por servidor, público/privado e o arquivo `.rbxl`.
* Pelo Studio 2015: *File → Publish to ROBLOX* (envia para `/Data/Upload.ashx`).
* Para atualizar: página do jogo → *Configure this game* → *Upload a new version*.

## Jogar

No launcher: **Server Browser → ROBLOXSERVER GAMES...**

1. Endereço do RobloxServer (IP do Raspberry Pi, do PC com IIS ou o nome DDNS) → **REFRESH**.
2. **LOG IN** com a conta do site.
3. Escolha o jogo:

| Botão | O que acontece |
| --- | --- |
| **PLAY SOLO** | baixa o place para `maps/Custom/RobloxServer/` (conferindo o MD5) e abre o Play Solo |
| **HOST SERVER** | `POST /Game/Servers.ashx` registra um job (`jobId` + `serverKey`), abre o servidor na `RobloxPort` e manda heartbeat a cada minuto; ao fechar, o job é removido |
| **JOIN SERVER** | `PlaceLauncher.ashx` escolhe o servidor, `Join.ashx` devolve o join script assinado com endereço, porta e ticket; o launcher entra com o nome da sua conta |

Clicar duas vezes num jogo entra num servidor se houver um, ou abre o Play Solo.

## Como o servidor de jogo confere os jogadores

1. O launcher de quem entra coloca o ticket de uso único num `StringValue` `RSAuthTicket` dentro do jogador.
2. O addon `RobloxServerAuth.lua` no servidor lê o ticket e chama
   `/Game/ValidateTicket.ashx?ticket=…&jobId=…&serverKey=…`.
3. Ticket inválido, reutilizado, de outro servidor, nome diferente da conta ou conta banida → o jogador é expulso.
   Com `RequireAuthTickets=false` no `Web.config`, quem entra sem ticket é aceito (só o ticket inválido expulsa).

## Rede

* Na mesma casa: o site devolve o IP local de quem hospeda.
* Pela Internet: o Raspberry Pi encaminha a porta (`GAME_FORWARDS`) e o site mostra o `PublicGameAddress`.
  Veja [raspberry-pi.md](raspberry-pi.md).
