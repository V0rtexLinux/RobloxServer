# Páginas no estilo 2013

O site imita o roblox.com de 2012–2013. O CSS e as imagens das páginas abaixo vêm da recriação do site de 2013
feita pela comunidade [RobloxLabs](https://github.com/RobloxLabs/web) (licença MIT, veja `RobloxServer.Web/Content/Pages/LICENSE-RobloxLabs.txt`), adaptados para Web Forms.

| Página | Endereço | O que tem |
| --- | --- | --- |
| Landing (cidade azul animada) | `/` sem login, `Landing.aspx` | abas *Sign up* (aniversário, gênero, usuário, senha) e *Login*. Menores de 13 anos entram com SuperSafeChat |
| Login | `Login.aspx` | layout do *NewLogin*: entrar à esquerda, "Not a member?" com o aniversário à direita |
| My ROBLOX | `/` com login, `/Home`, `My/Home.aspx` | "Hello, nome!", avatar, pessoas, caixa *What are you up to?* com **My Feed**, **Recently Played Games** e notícias (jogos novos) |
| Character | `My/Character.aspx` | manequim do *Avatar Body Colors*: clique numa parte do corpo e escolha a cor na paleta. As cores vão para o jogo (`Asset/BodyColors.ashx`) |
| Perfil | `User.aspx?id=` (sem id: o seu) | status, avatar, lugares ativos |
| Build | `Develop.aspx` | aba *Places* com a lista no estilo 2013 (Public/Private, visitas, engrenagem) e *Create New Place* |
| Games | `/Games` | lista de jogos com filtros de ordem e gênero |
| People | `Browse.aspx`, `/People` | busca de usuários |
| Catalog | `Catalog.aspx` | grade com categorias (Hats, Hair, Face, Neck, Shoulder, Front, Back, Waist, Shirts, Pants, T-Shirts, Faces, Gear), busca, ordem e detalhe do item com **Download .rbxm** (`Asset/Rbxm.ashx?id=`). Só itens da conta ROBLOX criados entre 2007 e 2013; o admin carrega a lista em **Sync catalog** |
| Messages | `Messages.aspx` | Inbox, Sent, ler, responder e escrever mensagens |
| Friends | `Friends.aspx` | pedidos de amizade, lista com status online, adicionar por nome |
| My Stuff | `Inventory.aspx` | itens que você pegou no Catalog, por categoria, com download do .rbxm |
| Groups | `Groups.aspx` | criar, buscar, entrar e sair de grupos (limite de grupos pelo Builders Club) |
| Trade | `Trade.aspx` | troca de 1 item por 1 item entre dois usuários |
| Builders Club | `BuildersClub.aspx` | BC / Turbo / Outrageous; sem pagamento, quem concede é o admin |
| Forum | `Forum.aspx` | fórum com categorias, boards, threads e busca |

Dados dos sistemas novos ficam em `App_Data` (Catalog.xml, Messages.xml, Friendships.xml, Inventory.xml, Groups.xml, GroupMembers.xml, Trades.xml, BuildersClub.xml).
