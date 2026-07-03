# Alterações — Sessão 03/07/2026

Sessão longa, sem acesso ao Unity-MCP (porta 22377 recusando conexão o tempo todo) — todo o trabalho foi feito por código + medição de arte via PowerShell/System.Drawing (substituindo o Python/Pillow de sessões anteriores), sem confirmação visual direta em nenhum momento. **Recomendo fortemente** abrir o Editor, rodar `Tools > Calouro > Montar Cena Top-Down`, salvar e testar o fluxo completo antes de qualquer apresentação.

## Resumo

| Área | O que mudou |
|---|---|
| Calendário | Semestre agora tem um dia absoluto (1–100) com contador "Faltam N dias" fixo no HUD |
| Dia 4 | Trote implementado — perseguição no próprio campus (Natan/Enzo/Matheus/Vitim) |
| Save | Save em disco (JSON) + tela de título com Novo Jogo / Continuar |
| Dia 28 | Side Quest do notebook do Aragão, completa |
| Diálogo | Todo NPC agora varia a fala depois do 1º encontro (não repete mais a apresentação) |
| Matemática | Prova virou 4 labirintos em sequência (2,5 pontos cada), sem portal físico |
| Cenário | RU afastado da Convivência, porta virou lateral, novos caminhos de chão, personagem nova (Gabi) |
| Áudio | Música tema em loop desde a tela de título |

---

## 1. Calendário do semestre (100 dias)

Substitui a ideia solta de "semana" por um calendário explícito — ver roadmap `3.1B` pra tabela completa dos 14 dias jogáveis planejados.

- `GameProgress.SemesterDay` (1–100) é a fonte única da verdade; `GameProgress.JumpSemesterDayTo()` avança nos saltos temporais (nunca recua).
- `AcademicHud.week` virou uma propriedade derivada de `SemesterDay` (`arredondar(dia ÷ 5,56)`) — não é mais escrito diretamente por fora.
- Contador fixo **"Faltam N dias pro fim do semestre"** no topo-centro da tela, sempre visível, atualiza em tempo real.
- `QuestManager`: objetivos de time skip agora carregam `semesterDayAfterSkip` (dia de destino) e, opcionalmente, `skipLine1`/`skipLine2` (mensagens custom da transição, em vez das genéricas "Algumas semanas depois...").

## 2. Dia 4 — Trote (perseguição no campus)

Implementado como perseguição no próprio mapa em vez do runner de cena separada do roadmap original (`3.6`) — decisão registrada no roadmap. Script novo: `TroteChase.cs`.

- Assim que o Dia 4 começa, Natan, Enzo, Matheus e Vitim saem de onde estavam (RU, Convivência, corredor do Bloco 4, campus) e cercam o jogador a ~10 unidades de distância.
- Os veteranos ficam parados enquanto a tela de transição está preta/clareando e por 1,5s depois — sem isso, o jogador aparecia cercado sem chance de reagir.
- **Pego:** cena de "sujaram você de ovo" (+15 estresse, flag `trote_pego`, flag `trote_fedendo` faz qualquer NPC comentar o cheiro pelo resto do dia).
- **Escapou:** sobreviver ~20s ou entrar em qualquer prédio (flag `trote_escapou`, sem penalidade).
- Ao fim, os 4 NPCs voltam à vida normal com falas de zoação sobre o trote (variam se pego ou se escapou) — Vitim e Enzo voltam pro lugar de origem (escala restaurada), Natan e Matheus passam a ficar na Convivência.

### Bugs corrigidos no processo
- **Jeferson preso em frente ao RU:** o `SetActive(false)` que o esconde ao fim da abertura só existia em memória — uma troca de cena inteira (voltar do pingue-pongue, ou futuramente carregar um save) recarregava o GameObject "visível" de novo. `CampusTourCutscene.Update()` agora reesconde ele sempre que detecta `GameProgress.CampusTourSeen == true`.
- **Veteranos sobrepostos após o trote:** reposicionamento explícito em vez de deixar cada um onde a corrida terminou.

## 3. Save em disco + tela de título

Implementa o essencial do roadmap `3.2` (estava 100% pendente).

- `SaveSystem.cs` novo: `Save()` / `Load()` / `HasSave()` / `Delete()`, JSON via `JsonUtility` em `Application.persistentDataPath/save.json`.
- Autosave dispara ao entrar no objetivo `notebook_prof` (Dia 28) — ou seja, logo depois da Prova R1. Escolhido esse ponto (não o instante exato do fim da prova) pra não gravar estado transitório em pleno time skip.
- `TitleScreen` ganhou uma tela de menu antes do nome: **Novo Jogo** / **Continuar** (Continuar só aparece se houver save). "Novo Jogo" com save existente já apaga o save antigo (aviso no texto da tela, sem modal de confirmação separado).
- Ao carregar: reaplica o objetivo salvo (`QuestManager.ActivateObjective`) e manda o jogador pro spawn do campus (`InteriorController.ForceCampus`) — não guardamos posição exata no mapa, só o objetivo.

## 4. Dia 28 — Notebook Desaparecido (SQ1)

Roadmap `3.9` implementado por completo, com uma simplificação de escopo: **as 4 etapas acontecem no mesmo dia** (decisão de 03/07), em vez de espalhadas por várias visitas/dias como o plano original previa.

Cadeia: `notebook_prof` (Aragão) → `notebook_ru` (Gabi, no RU) → `notebook_lab` (objeto do caderno, Bloco 2 Sala 2) → `notebook_devolucao` (Aragão de novo, +1.0 Ética, flag `notebook_devolvido`).

- **Aragão sai da sala dele** e aparece dentro do salão da Convivência assim que o jogador precisa falar com ele sobre o caderno sumido — usa o Vitim (sempre parado perto da mesa de pingue-pongue nessa altura do jogo) como âncora de posição em tempo de execução, em vez de coordenada fixa. Volta sozinho pra sala quando a conversa acaba.
- **Gabi** é uma personagem nova (não existia antes) — atendente do RU, pista da quest. Sprite próprio criado a partir de `atendente-cantina.png` (ver seção 6).
- **Laboratório do Bloco 2** = a Sala 2 do bloco (já existia, vazia) virou o "laboratório" pra fins de narrativa — sem construção de sala nova. O caderno é um objeto simples (quadrado + interação), sem arte própria.
- **Narrativa:** a Narrativa oficial (§7.1) não está neste repositório — os diálogos da quest foram escritos do zero, no mesmo tom dos outros NPCs. Comparar com o texto oficial depois, se existir.

## 5. Sistema geral de variação de diálogo

Reclamação: NPCs sempre repetiam a fala de apresentação, mesmo depois de já concluída a interação com eles (ex.: Aragão se apresentando de novo depois de já ter devolvido o caderno). Resolvido de forma genérica, pra todos os personagens:

- `NpcInteractable.repeatLines`: lista de "conversas alternativas", sorteada aleatoriamente a cada vez que o jogador já conhece aquele NPC.
- `NpcInteractable.CurrentLines()`: prioridade — fala de objetivo específico (quest em andamento) → `repeatLines` (se já se conheceram) → `lines` (apresentação, só na 1ª vez).
- `DialogueManager` marca o NPC como conhecido (flag `conheceu_<npcId>`) assim que a 1ª conversa termina.
- Aplicado a **todos** os NPCs com diálogo: Rainara, Aragão, Paulyne, Jeferson, Yasmin, Enzo, Natan, Vitim, Matheus, Emilly, Gabi, e até o objeto do caderno.
- As escolhas A/B (ex.: convite de pingue-pongue) não foram alteradas — continuam podendo repetir, o que é intencional em alguns casos (ex.: jogar de novo).

## 6. Prova de Matemática — 4 labirintos

- `MazeController` agora roda **4 labirintos em sequência**, cada um valendo até 2,5 pontos (soma dá a nota 0–10), em vez de um único labirinto valendo tudo.
- Dificuldade crescente: o 1º é o corredor "cobrinha" de sempre; os outros 3 são labirintos de verdade, **gerados por backtracking recursivo** (`TopDownSceneBuilder.GenerateMaze`, sempre solucionáveis, com bifurcações e becos sem saída reais) — crescem de 5×5 até 9×9 células.
- **Sem portal físico no campus:** a prova agora começa falando com o Aragão dentro da sala dele (igual às outras 3 disciplinas), não mais andando até um portal perto do RU. `MazePortal.cs` foi removido do projeto (arquivo deletado).

## 7. Cenário

### Gabi (personagem nova)
`atendente-cantina.png` veio numa grade 7×5 com ciclos de caminhada completos — formato diferente do padrão 4×3 usado por todo NPC do jogo. Recortei os quadros necessários (2 de frente, 2 de lado, 1 de costas) e montei uma folha nova de 4×3 (`Assets/Sprites/Characters/gabi.png`), já no formato padrão — passa pelo fatiamento automático do montador sem precisar de nenhum código especial. A pose de lado da arte original olha pra esquerda, então ela usa `invertSide: true` (mesmo ajuste do Batatinha).

### Caminho Blocos 1/2 → passarela (`caminho_cima.png`)
Textura nova ligando a saída norte dos Blocos 1 e 2 até a passarela da entrada — passou por **3 rodadas de ajuste** depois de feedback visual:
1. 1ª tentativa preservava a proporção do canvas quadrado (pernas do "П" exatas nas portas dos blocos) — ficou gigante (~35×35) e não conectava com a passarela.
2. Arte trocada pela versão corrigida do usuário (`CIMA-CERTO.png`) e recalibrada — tamanho/conexão corretos, mas as pernas ficavam mais próximas uma da outra do que as portas de verdade.
3. Largura esticada (só ela, sem mexer na altura) até as pernas caírem exatas nas portas dos Blocos 1 e 2 (11 unidades de distância).
4. Altura esticada pra baixo (mantendo o topo fixo) até a base encostar no topo visual dos blocos, fechando o vão de grama que sobrava.

### RU afastado da Convivência + porta lateral
- RU movido de x=-22 para **x=-32** (mapa estendido em `MapXMin` de -40 para -52 pra caber).
- Novo caminho reto (`pedaco_caminho.png`) preenche o vão entre o RU e a Convivência — tamanho calculado dinamicamente a partir da distância real entre os dois prédios, não um valor fixo.
- **Porta do RU passou do lado sul pro lado leste** (de frente pra Convivência/o caminho novo). `BuildRUBuilding` ganhou lógica própria de porta (não usa mais a função genérica `BuildExterior` dos Blocos, que continua south-only e não foi tocada).
- Cutscene de abertura do Dia 1 ajustada: o Jeferson entra pelo lado leste do RU agora, com uma rota mais curta (não precisa mais contornar até o sul do prédio). Comportamento (câmera acompanhando, desaparecer ao entrar) inalterado.

## 8. Música tema

- `musica-tema.mpeg` era, na prática, um MP3 (confirmado pelos bytes do arquivo) — copiado para `Assets/Audio/musica_tema.mp3` com a extensão certa.
- `MusicPlayer.cs` novo: `AudioSource` em loop, volume fixo em 0,5, tocando desde o `Awake()`. Sem persistência entre cenas (`DontDestroyOnLoad`) — reinicia ao voltar do pingue-pongue. Sem controle de volume ainda (isso é do menu de pausa, roadmap `3.15`, que não existe).

---

## Como testar

1. Unity → **Tools > Calouro > Montar Cena Top-Down**
2. **Ctrl+S**
3. **Play** — tela de título deve mostrar só "Novo Jogo" (sem save ainda)

Pontos de verificação sugeridos, na ordem do fluxo:
- **Dia 4:** os 4 veteranos cercando o jogador, com espaço real pra fugir; testar pego e escapou.
- **Prova R1 (Dia 20):** Matemática agora são 4 mapas seguidos, cada um mostrando "Mapa X/4".
- **Dia 28:** achar o Aragão dentro da Convivência (não na sala); falar com a Gabi no RU; achar o caderno no Bloco 2 Sala 2; devolver.
- **Save:** fechar o jogo depois do Dia 28 e escolher "Continuar" no título — deve retomar direto no objetivo certo.
- **Cenário:** RU mais afastado da Convivência com o caminho novo entre os dois; porta do RU no lado leste; caminho conectando Blocos 1/2 à passarela.
- **Repetir diálogo:** falar de novo com qualquer NPC já conhecido — não deve repetir a apresentação.
- **Áudio:** música tocando desde a tela de título.

## Ajustes em aberto (dependem de feedback visual/jogado)

- Caminho Blocos 1/2 → passarela: 4 rodadas de ajuste sem nenhuma confirmação visual real — a mais arriscada das mudanças desta sessão.
- Porta leste do RU / caminho até a Convivência: posição da porta (`doorNormY = 0.6`) foi estimada visualmente a partir da arte, não medida por pixel.
- Diálogos da SQ1 do notebook são texto novo (Narrativa oficial não disponível neste repositório).
- Balanceamento dos 4 labirintos da prova de Matemática (tempos bom/ruim por rodada) não foi jogado, só calculado.
