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

## Convenções Unity

- **Input System novo, sempre** (`Keyboard.current`, `Mouse.current`). Nunca usar `Input.GetKey`/`GetAxis` legado — o projeto está configurado para o Input System e o legado lança exceção.
- **UI construída por código com uGUI legado (`Text`, `Button`, `Image`)**, seguindo o padrão de `AcademicHud`/`QuestManager`. **Não** migrar para TextMeshPro nem criar prefabs de UI no meio do MVP.
- Câmera: `CameraFollow2D` custom. **Não** instalar Cinemachine.
- Física top-down: `Rigidbody2D` Dynamic com `Gravity Scale = 0`; movimento via `rb.linearVelocity` (Unity 6). Colisão do mapa via Tilemap + colliders compostos.
- Sprite sheets de personagens no formato **4×3** (4 direções × 3 frames), consumidos pelo `SpriteWalkAnimator`. Novos NPCs seguem o mesmo layout.
- Interação: trigger collider + prompt "Pressione E", pelo padrão de `NpcInteractable`. Objetos novos interagíveis (coletáveis, notebook) implementam o mesmo contrato.
- Cena principal: `SampleScene` (campus + interiores). Minigames em cenas próprias carregadas por `SceneManager`. **Salvar todas as cenas antes de entrar em Play Mode ou rodar testes** (cenas sujas abortam `tests-run` do MCP).
- Unity-MCP disponível na porta **8090** para automação do Editor (criar objetos, rodar testes, screenshots).
- Assets de arte em `Assets/Art/` (subpastas `Campus`, `Env`, etc.); áudio em `Assets/Audio/`; documentos em `docs/` na raiz.

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
