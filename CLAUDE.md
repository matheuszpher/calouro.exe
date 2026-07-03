# CLAUDE.md — Calouro.exe

Guia de desenvolvimento do projeto. Vale para **todo o time e para qualquer sessão de IA** trabalhando neste repositório, do estado atual até a entrega final. Em caso de conflito entre este arquivo e hábito/preferência pessoal, este arquivo vence.

## O que é o projeto

**Calouro.exe — Sobrevivendo ao Primeiro Semestre**: RPG 2D top-down (pixel art) em Unity 6, ambientado no campus da UFC Quixadá. O jogador é um calouro de Engenharia de Software; notas em 5 disciplinas + barra de estresse determinam 1 de 3 finais. MVP de ~1h de gameplay, apresentado para a professora em builds **Windows e Linux**.

Documentos-fonte (cópias devem viver em `docs/`):
- **GDD v1** — mecânicas, minigames, sistema de pontuação
- **Documento de Narrativa v1.0** — arcos, diálogos prontos, side quests, cutscenes, consequências cruzadas

## Como seguir o roadmap (`roadmap-v2.md`)

O `roadmap-v2.md` é a **fonte única de verdade do escopo**. Regras de uso:

1. **Toda tarefa nasce de um checkbox do roadmap.** Antes de codar qualquer coisa, localize o checkbox correspondente (seções 3.1–3.21). Se a tarefa não existe lá, ela está fora do escopo — ou se adiciona conscientemente ao roadmap primeiro (com acordo do time), ou não se faz.
2. **Respeite a ordem das fases (A → E).** As fases são ordenadas por dependência: nada da Fase C+ deve começar antes da Fase A (`GameProgress`/`ArcDirector`/save) existir, porque tudo pluga nela.
3. **Marque o checkbox (`- [x]`) no mesmo commit que conclui a tarefa.** O roadmap desatualizado é pior que nenhum roadmap.
4. **As decisões da seção 2 estão fechadas — não reabrir.** Inclui: sem menu de debug, save em disco com 1 slot, Gabriel no Arco 2/semana 6, ESC = pausa real, estender `GameProgress` em vez de refatorar. Se uma decisão se provar inviável na prática, a mudança é registrada na seção 2 antes de mudar o código.
5. **Faltou tempo? Corte pela cut-list da seção 2, na ordem.** Nunca improvisar um corte que não está na lista; nunca cortar os itens marcados como "nunca cortar".
6. **Ao descobrir um gargalo novo** (algo do GDD/Narrativa sem tarefa), adicione-o como checkbox na seção/fase adequada e cite a seção do documento-fonte — como feito nas seções 1 e 1B.
7. Os textos de diálogo e cutscene **já estão escritos** na Narrativa (§3–§8). Não inventar falas novas para conteúdo que já tem texto pronto; adaptar apenas comprimento quando necessário.

## Convenções de código (C#)

Seguem o padrão já estabelecido em `Assets/Scripts` — consistência com o existente vale mais que preferência:

- **Nomes de classes, métodos, propriedades e variáveis em inglês** (`PlayerController2D`, `AddStress`, `MathGrade`); **comentários, textos de UI e diálogos em PT-BR**.
- Comentário `/// <summary>` em PT-BR no topo de cada classe explicando o papel dela (todos os scripts atuais têm — manter).
- **Flags narrativas em snake_case PT-BR**: `"trote_escapou"`, `"notebook_devolvido"`, `"gabriel_ajudado"`, `"veterano_denunciado"`. Toda flag nova deve ser citada no roadmap (consequências cruzadas).
- Um script = uma responsabilidade = um arquivo com o nome da classe, direto em `Assets/Scripts/` (sem subpastas até que passe de ~30 scripts).
- Estado global **somente** via `GameProgress` (estático, com DTO `SaveData` serializável). Proibido criar outros singletons de estado; managers de cena (`ArcDirector`, `DialogueManager`) podem ser singletons de **comportamento**, mas não guardam progresso.
- Sem eventos de ScriptableObject, sem DOTween, sem frameworks novos. Coroutines e chamadas diretas resolvem — decisão fechada no roadmap (seção 1, item 10).
- **Calendário do semestre**: `GameProgress.SemesterDay` (1–100) é a fonte única da verdade — não escrever `AcademicHud.week` diretamente (é uma propriedade derivada). Saltos de dia usam `GameProgress.JumpSemesterDayTo()`; nunca decrementar o dia.
- **Objetivos do `QuestManager`** aceitam `onActivate` (dispara ao virar o objetivo atual) e `onComplete` (dispara ao ser concluído) — use esses hooks pra lógica extra (mover um NPC, autosave, recompensa) em vez de hardcodar `if (id == "...")` no fluxo central do `QuestManager`.
- **Todo NPC novo com diálogo precisa de `repeatLines`** (`NpcInteractable`): sem isso, ele repete a fala de apresentação (`lines`) pra sempre, mesmo depois do jogador já tê-lo conhecido — quebra a imersão (ver lição da sessão 03/07). `CurrentLines()` já resolve a prioridade sozinho (objetivo específico → `repeatLines` se já conhecido → `lines` na 1ª vez); só falta preencher o `repeatLines` na criação do NPC.
- **Reposicionar um NPC temporariamente** (ex.: sair do lugar de origem pra uma cutscene/side quest e voltar depois) sempre salva `originalPos`/`originalScale` antes de mexer, e restaura os dois ao fim — nunca só a posição (personagens de interiores "de perto" ficam gigantes na escala errada fora do lugar deles). Ver `TroteChase.cs` e `QuestManager.MoveAragaoToConvivencia/Home` como referência.

## Convenções Unity

- **Input System novo, sempre** (`Keyboard.current`, `Mouse.current`). Nunca usar `Input.GetKey`/`GetAxis` legado — o projeto está configurado para o Input System e o legado lança exceção.
- **UI construída por código com uGUI legado (`Text`, `Button`, `Image`)**, seguindo o padrão de `AcademicHud`/`QuestManager`. **Não** migrar para TextMeshPro nem criar prefabs de UI no meio do MVP.
- Câmera: `CameraFollow2D` custom. **Não** instalar Cinemachine.
- Física top-down: `Rigidbody2D` Dynamic com `Gravity Scale = 0`; movimento via `rb.linearVelocity` (Unity 6). Colisão do mapa via Tilemap + colliders compostos.
- Sprite sheets de personagens no formato **4×3** (4 direções × 3 frames), consumidos pelo `SpriteWalkAnimator`. Novos NPCs seguem o mesmo layout.
- Interação: trigger collider + prompt "Pressione E", pelo padrão de `NpcInteractable`. Objetos novos interagíveis (coletáveis, notebook) implementam o mesmo contrato.
- Cena principal: `SampleScene` (campus + interiores). Minigames em cenas próprias carregadas por `SceneManager`. **Salvar todas as cenas antes de entrar em Play Mode ou rodar testes** (cenas sujas abortam `tests-run` do MCP).
- **Cena separada (`SceneManager.LoadScene`) recarrega o jogo inteiro do zero** — todo `Start()` que assume "isso é o começo de uma partida nova" (ex.: `TitleScreen` mostrando a tela de nome) dispara de novo. Se um novo minigame precisar de cena própria (ex.: 3.6 Fuga do Trote), siga o padrão do pingue-pongue: um handoff estático tipo `PingPongSession` (guarda posição/câmera/escala antes de trocar de cena) lido no `Awake()` de quem precisa restaurar estado e no `Start()` de quem precisa **não** se comportar como um jogo novo — nessa ordem, porque todo `Awake()` da cena roda antes de qualquer `Start()`.
- Unity-MCP disponível na porta **8090** para automação do Editor — mas **não confie nele sem checar antes** (`npx unity-mcp-cli status`). Já foi visto indisponível numa sessão inteira por conflito de porta entre dois pacotes MCP instalados ao mesmo tempo (`com.ivanmurzak.unity.mcp` e `com.gamelovers.mcp-unity`, ambos brigando pela porta 8090/22377). Se indisponível: peça ao usuário pra rodar `Tools > Calouro > Montar Cena Top-Down` manualmente e reportar o resultado/console; para medir posições/recortar assets sem o Editor, use Python + Pillow (`PIL.Image`) direto nos arquivos — foi assim que toda a integração de asset desta sessão (recorte de bordas transparentes, medição de paredes/portas/móveis por cor de pixel, verificação por overlay antes de aplicar no código) foi feita.
- **Personagens em interiores "de perto" (arte que ocupa mais o quadro que os prédios do campus) ficam pequenos na escala normal.** Convenção já usada na Convivência, na sala de aula e nos corredores dos blocos: NPCs daquele interior com `scale: 1.6f` (parâmetro de `CreateAmbientNpc`) e o jogador também em 1.6x ao entrar (`BuildingDoor.playerScale = 1.6f`), restaurado ao normal na saída (automático via `InteriorController`). Ao criar um interior novo com arte "de perto", aplique isso de cara em vez de descobrir depois que os personagens ficaram minúsculos.
- Assets de arte em `Assets/Art/` (subpastas `Campus`, `Env`, etc.); áudio em `Assets/Audio/`; documentos em `docs/` na raiz.
- **Porta de prédio no campus**: `BuildExterior` (usado pelos 4 Blocos) só sabe fazer porta ao sul (e norte, opcional) — é a função padrão. Prédios com porta em outro lado (ex.: o RU, porta a leste desde 03/07/2026) têm builder próprio (`BuildRUBuilding`) que não passa por `BuildExterior`. Ao mudar a orientação/posição de um prédio, não presuma que a porta é sul — confira qual builder ele usa.
- **Save em disco existe** (`SaveSystem.cs`, `Application.persistentDataPath/save.json`) — mas o autosave **não** é automático a cada avanço de objetivo; ele é ligado manualmente via `onActivate` no objetivo do ponto de checkpoint desejado (hoje: só em `notebook_prof`, Dia 28). Ao adicionar um novo marco importante, considere se ele merece um autosave também.
- Unity-MCP (porta 8090/22377) segue instável entre sessões — na sessão de 03/07/2026 ficou fora do ar o tempo todo. Sem ele: além de Python/Pillow, **PowerShell + `System.Drawing`** também funciona pra medir/recortar/compor arte (usado pra criar `gabi.png` e medir os caminhos novos) — não precisa de Python instalado.

## Convenções de Git

- Branch de trabalho: `main` (time pequeno, prazo curto — sem git-flow).
- Mensagens de commit em PT-BR, no formato `área: o que mudou` (ex.: `estresse: remove subida passiva e adiciona tabela de eventos`). Uma feature do roadmap por commit sempre que possível, com o checkbox marcado no mesmo commit.
- Nunca commitar `Library/`, `Temp/`, `Logs/`, `obj/`, builds ou zips (respeitar o `.gitignore` de Unity).
- Arquivos `.meta` **sempre** acompanham o asset correspondente no mesmo commit (mover/renomear asset sem o `.meta` quebra referências para o resto do time).
- Antes de dar push: o projeto compila sem erros no console e a cena principal abre em Play Mode.

## Definição de "pronto" (para qualquer tarefa)

1. O checkbox do roadmap está marcado.
2. Console do Unity sem erros (warnings novos justificados).
3. O fluxo afetado foi jogado ao menos uma vez em Play Mode.
4. Se a tarefa mexe em estado global: salvar → fechar → Continuar ainda funciona.
5. Textos visíveis ao jogador estão em PT-BR sem placeholder ("lorem", "TODO", "xxx").

## O que NÃO fazer (até a entrega)

- Não reabrir decisões da seção 2 do roadmap sem registrar a mudança lá primeiro.
- Não adicionar pacotes/assets novos ao projeto sem necessidade comprovada por uma tarefa do roadmap.
- Não refatorar em larga escala ("limpar a arquitetura") durante a semana do MVP — melhorias pontuais no que se está tocando são bem-vindas; reescritas não.
- Não criar sistema de conteúdo genérico (editor de diálogo, framework de quest) — o conteúdo é hardcoded por decisão de escopo.
- Não adicionar features fora do GDD/Narrativa, por melhores que pareçam. O MVP entrega o que os documentos prometem.
