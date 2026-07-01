# Calouro.exe — Roadmap de Implementação (MVP de 1 Semana)

> **Contexto:** Unity, equipe dedicada full-time por 1 semana, sprites do campus (tiles, mobília) já prontos — falta montar o mapa. Build final será apresentado para a professora. Escopo: **tudo que está no GDD/Narrativa entra**, em formato de resumo jogável dos 4 arcos (~1h de gameplay).

---

## 0. Decisões de Escopo Fechadas

Estas decisões já foram tomadas e **não devem ser reabertas** durante a semana — qualquer dúvida de escopo no meio do desenvolvimento, volte aqui.

| Tópico | Decisão |
|---|---|
| Engine | Unity |
| Duração do MVP | ~1 hora de gameplay, cobrindo os 4 arcos em formato resumido |
| Minigame 1 — Trote | Runner lateral, como descrito no GDD (fuga dos veteranos) |
| Minigame 2 — Matemática | Labirinto puro, navegação contra cronômetro |
| Minigame 3 — Debug | **Reformulado**: labirinto onde o jogador é perseguido pelo professor de Fundamentos (ou veteranos), em vez do sistema visual de arrastar blocos de código. Reaproveita a base técnica do Minigame 2 + elemento de perseguição |
| Side Quests | As duas entram completas (Notebook Desaparecido + Colega em Risco) |
| Disciplinas | As 5 aparecem na Caderneta. Matemática e Fundamentos têm nota calculada via minigame. IHC e Ética têm nota calculada via escolhas de diálogo + **resumos narrados** ("você tirou X na prova de IHC") sem minigame próprio. Intro à ES é guiada pelo Coordenador (reflete progresso narrativo, sem nota de minigame) |
| Estresse | Sistema completo: aumenta em falhas/decisões ruins, reduz com descanso, penalidade de 10s em provas com estresse >70%, colapso ao encher a barra (perde o dia, penalidade em todas as notas) |
| Branching de diálogo | Misto: decisões-chave (trote, notebook, Gabriel, resposta final ao Coordenador) ramificam de verdade. Diálogos ambientes/flavor podem ser cosméticos |
| Cutscenes | Formato leve "visual novel": tela estática/ilustração + caixa de texto avançando, **não** quadrinho animado completo. Mantém as 6 cutscenes do documento |
| Finais | Os 3 finais (Aprovação Direta, Avaliação Final, Reprovação) implementados, cada um com sua cutscene leve |
| Arte | Mapa precisa ser **montado** a partir dos tiles/mobília já existentes. Sprites de personagens (NPCs principais) precisam ser checados no início |
| Apresentação | Build final será visto pela professora → reservar tempo para menu principal, tela de título e fluxo sem crashes/bugs visíveis |

### Cut-list de emergência (só usar se faltar tempo)
1. Reduzir número de diálogos ambientes/decorativos (NPCs de cenário sem fala)
2. Side Quest 2 (Colega em Risco) vira mais curta — 1 cena de diálogo em vez de sessão de estudo + retorno
3. Reduzir a complexidade visual das cutscenes (menos ilustrações, mais texto)
4. Cortar a segunda variação de dificuldade dos minigames (1 nível em vez de progressão Arco2→Arco3→Arco4)
5. **Nunca cortar:** os 3 finais, o trote jogável, a fala do Coordenador, e pelo menos uma decisão com consequência visível.

---

## 1. Referências de Autenticidade do Campus — Onde Entram

| Referência | Onde entra | Tipo |
|---|---|---|
| Medo de reprovar em PAA (Projeto e Análise de Algoritmos) | Falas ambientes de veteranos na Área de Convivência e nos corredores | Diálogo de flavor |
| Lenda da rã encontrada na comida do RU | Diálogo da Atendente do RU ou de um colega de fila | Easter egg de diálogo |
| **Dias** — faz-tudo do campus, querido por todos | Novo NPC ambiente, aparece em 2-3 pontos do mapa (ex: perto da obra do Bloco 5, ou consertando algo). Dá dicas leves de orientação/flavor | NPC opcional recorrente |
| Convites para tomar café na cantina (matando aula) | Novo evento opcional nos slots de tempo livre (Arco 2/3): reduz estresse, mas usado 2+ vezes conta como falta | Opção de slot de tempo livre |
| Visita ao Cedro (açude ao lado da universidade) | Cena contemplativa opcional, no fim do Arco 3 (mesmo slot da "exploração noturna" já prevista) — reset parcial de estresse + diálogo introspectivo | Evento especial de arco |
| Mesa de ping pong: "Vai marcar time de fora?" | Objeto interativo na Área de Convivência | Interação ambiente |
| Convite para a Calourada | Evento na transição Arco 1→Arco 2, reforça o pilar "pertencimento gradual" | Evento narrativo curto com escolha |

---

## 2. Arquitetura Técnica Detalhada (Sistema por Sistema, com Recursos Unity)

### 2.1 Estrutura de Projeto e Pacotes Recomendados

Instalar via Package Manager antes de começar:

- [ ] **2D Tilemap Editor** — para montar o mapa do campus a partir dos tiles existentes
- [ ] **2D Animation** — caso os sprites de personagem usem skeletal/sprite-swap animation; senão, Animator comum já resolve
- [ ] **TextMeshPro** — todo texto de UI e diálogo (qualidade muito superior ao Text legado, suporte a rich text para ênfases nas falas)
- [ ] **Cinemachine** — câmera que segue o jogador suavemente pelo campus (Virtual Camera com Confiner 2D para não sair dos limites do mapa)
- [ ] **Input System** (novo) — se a equipe já tem familiaridade; senão, manter o Input Manager legado (`Input.GetAxis`/`GetKeyDown`) para não gastar tempo migrando, dado o prazo de 1 semana
- [ ] **DOTween (Asset Store, gratuito)** — opcional, mas acelera MUITO fades de cutscene, transições de UI e tweens de câmera/objetos sem escrever Coroutines manuais
- [ ] **Cinemachine + Timeline** (opcional) — só se sobrar tempo para cutscenes mais ricas; o MVP não depende disso (ver seção de Cutscenes)

### 2.2 Organização de Cenas

- [ ] Uma cena por bloco/área principal (Blocos 1-4 podem ser uma cena única ou separadas, dependendo do tamanho do tilemap) carregadas via `SceneManager.LoadSceneAsync` com **Additive loading** para não perder estado entre transições
- [ ] Cena separada para cada Minigame (Trote, Labirinto Matemática, Labirinto Debug) — carregadas sobre a cena de exploração ou substituindo-a, via `SceneManager.LoadScene`
- [ ] Cena dedicada para Cutscenes (reaproveitável, recebe dados de qual cutscene tocar via um `CutsceneData` ScriptableObject)
- [ ] Objeto `GameManager` marcado com `DontDestroyOnLoad` para persistir entre as trocas de cena

### 2.3 GameManager (Singleton)

- [ ] Implementar como **Singleton** clássico (`public static GameManager Instance`) com `DontDestroyOnLoad(gameObject)` no `Awake()`
- [ ] Guarda referência ao arco atual (enum `ArcoNarrativo { Chegada, PrimeirasProvas, Virada, RetaFinal }`)
- [ ] Centraliza chamadas de transição de arco e disparo de cutscenes
- [ ] Recomendado usar o padrão de **ScriptableObject Event Channels** (arquitetura do Ryan Hipple / Unity Open Projects) para desacoplar sistemas: em vez de tudo chamar `GameManager.Instance.Metodo()`, criar SOs como `GameEvent` (`OnArcoMudou`, `OnNotebookEncontrado`, `OnGabrielAjudado`) que qualquer script pode invocar/escutar sem referência direta — facilita muito debug e evita spaghetti em 1 semana de prazo apertado

### 2.4 CadernetaAcademica (Dados de Progresso)

- [ ] Classe `CadernetaData` (não-MonoBehaviour, serializável) contendo:
  - `float notaMatematica, notaFundamentos, notaIHC, notaEtica, notaIntroES`
  - `float estresseAtual` (0–100)
  - `int semanaAtual` (1–18)
  - `HashSet<string>` ou `Dictionary<string,bool>` de flags narrativas
- [ ] Guardar essa instância dentro do `GameManager` (ou um `ScriptableObject` singleton tipo `GameStateSO`) para acesso global
- [ ] Persistência simples: `JsonUtility.ToJson()` / `FromJson()` salvando em `Application.persistentDataPath` via `System.IO.File.WriteAllText` — só necessário se quiserem permitir fechar e continuar depois; **para a demo de 1 sessão, isso é opcional** e pode ficar só em memória (RAM) sem salvar em disco, economizando tempo

### 2.5 Sistema de Flags (Decisões Narrativas)

- [ ] `Dictionary<string, bool>` simples dentro do `GameStateSO`, com chaves como `"trote_escapou"`, `"notebook_devolvido"`, `"gabriel_ajudado"`, `"resposta_coordenador_final"`
- [ ] Métodos utilitários: `SetFlag(string id, bool valor)` e `GetFlag(string id)`
- [ ] Cada ponto de consequência cruzada (tabela do documento de narrativa) consulta essas flags diretamente — evita reescrever lógica de estado em cada script

### 2.6 Movimento e Interação do Jogador

- [ ] **Rigidbody2D** (Dynamic, Gravity Scale 0 se for top-down puro, ou >0 se quiserem física de plataforma nas áreas externas) + **CapsuleCollider2D/BoxCollider2D**
- [ ] Script `PlayerController2D` lendo `Input.GetAxisRaw("Horizontal"/"Vertical")` e aplicando via `rb.linearVelocity` (Unity 6) ou `rb.velocity` (versões anteriores)
- [ ] **Animator Controller** com Blend Tree para direção (idle/andando para cima, baixo, esquerda, direita) — parâmetros float `MoveX`, `MoveY`
- [ ] **Tilemap + TilemapCollider2D + CompositeCollider2D** para colisão sólida do mapa (paredes dos blocos, mobília)
- [ ] Interação com NPCs/objetos via **Trigger Collider** (`OnTriggerEnter2D`/`OnTriggerExit2D`) detectando entrada na área do NPC, mostrando um prompt de UI ("Pressione E") e disparando o diálogo com `Input.GetKeyDown(KeyCode.E)`
- [ ] Interface `IInteractable` com método `Interagir()` implementada por NPCs, objetos coletáveis (apostilas) e o notebook perdido — permite que o mesmo código de interação do jogador funcione para qualquer objeto interagível, sem if/else gigante

### 2.7 Câmera

- [ ] **Cinemachine Virtual Camera** seguindo o `Transform` do jogador
- [ ] **Cinemachine Confiner 2D** com um `PolygonCollider2D` desenhado nos limites de cada área, para a câmera não mostrar fora do mapa
- [ ] Trocar limites do confiner ao mudar de área (script simples que troca a referência do confiner ao entrar em um trigger de transição de área)

### 2.8 Sistema de Diálogo

Duas abordagens possíveis — escolher uma logo no início, não migrar no meio da semana:

**Opção recomendada dado o prazo: ScriptableObject-based custom**
- [ ] `DialogoSO` (ScriptableObject) contendo uma lista de `LinhaDeDialogo` (falante, texto, e opcionalmente lista de `OpcaoDeResposta` com texto + referência para o próximo `DialogoSO` ou um `UnityEvent`/`GameEvent` a disparar)
- [ ] `DialogueUI` (MonoBehaviour com **TextMeshProUGUI** para o texto e **Button** instanciados dinamicamente para as opções) consome o `DialogoSO` atual
- [ ] Efeito de "texto aparecendo letra por letra" via Coroutine simples (`yield return new WaitForSeconds(velocidade)`) — opcional, dá polish mas não é essencial

**Opção alternativa (se a equipe já conhecer): Yarn Spinner**
- [ ] Pacote gratuito da Unity Asset Store, com editor de nós de diálogo visual e suporte nativo a variáveis/condicionais — pode ser **mais rápido** se alguém da equipe já tiver usado antes, mas tem curva de aprendizado se for a primeira vez. Só vale a pena se já houver familiaridade.

- [ ] Cada `DialogoSO` de decisão-chave (trote, notebook, Gabriel, resposta final) deve, ao final, chamar um `GameEvent` ou método direto que seta a flag correspondente no `GameStateSO`

### 2.9 Minigame 1 — Fuga do Trote (Runner)

- [ ] Cena separada, **Rigidbody2D** no jogador com scroll automático (mover o fundo/Tilemap via script, ou mover o próprio jogador em `FixedUpdate` com velocidade constante para a direita)
- [ ] Pulo: `rb.AddForce(Vector2.up * forca, ForceMode2D.Impulse)` no `GetKeyDown(KeyCode.Space)`, checagem de chão via `Physics2D.OverlapCircle` ou Trigger no pé do personagem
- [ ] Abaixar (S): trocar o Collider para uma versão mais baixa (`CapsuleCollider2D.size` menor) e tocar animação de "abaixado"
- [ ] Veteranos como obstáculos: **Trigger Colliders** que, ao colidir com o jogador, incrementam um contador de capturas (`int capturas`) — ao chegar a 3, dispara `GameEvent OnTrotePego`
- [ ] Chegada segura ao Bloco 1: **Trigger Collider** no final da pista disparando `GameEvent OnTroteEscapou`
- [ ] **Animator** com estados: Correndo, Pulando, Abaixado

### 2.10 Minigame 2 — Labirinto de Matemática

- [ ] **Tilemap** dedicado para o labirinto (pode ser gerado à mão para o MVP — gerar proceduralmente não vale o tempo de implementação numa semana)
- [ ] Movimento do jogador igual ao `PlayerController2D` da exploração, reaproveitando o mesmo script
- [ ] Cronômetro: `float tempoDecorrido` incrementado em `Update()`, exibido via **TextMeshProUGUI**
- [ ] Trigger Collider na saída do labirinto dispara o cálculo de nota: nota = função inversa do tempo (quanto menor o tempo, maior a nota), com uma fórmula simples tipo `nota = Mathf.Clamp(10 - (tempo / fatorEscala), 0, 10)`
- [ ] Duas variações de labirinto (reduzida/padrão): dois Tilemaps prontos, ativando um ou outro via `GameObject.SetActive()` conforme a flag `notebook_devolvido`

### 2.11 Minigame 3 — Labirinto de Debug (Perseguição)

- [ ] Reaproveita a base do Minigame 2 (mesmo Tilemap de labirinto, ou um novo com tema de "sala de aula"/lousa)
- [ ] Adicionar um `PerseguidorAI` (professor ou veteranos): script simples de perseguição sem precisar de NavMesh completo — `transform.position = Vector2.MoveTowards(transform.position, jogador.position, velocidade * Time.deltaTime)`, com um **Tilemap-aware pathfinding simplificado** (ex: o perseguidor segue um conjunto de waypoints pré-definidos no labirinto, não pathfinding real) para não gastar tempo configurando NavMesh 2D ou A* Pathfinding Project
- [ ] Se quiserem perseguição mais "inteligente" e já sobrar tempo: pacote gratuito **A* Pathfinding Project (versão free)** tem suporte a grid 2D e seria plug-and-play sobre o Tilemap
- [ ] Trigger Collider no perseguidor colidindo com o jogador = penalidade de tempo/nota (não reinicia o minigame, só penaliza, conforme decisão de escopo)
- [ ] Dificuldade crescente entre rodadas: aumentar `velocidade` do perseguidor e trocar para um Tilemap de labirinto mais complexo

### 2.12 Sistema de Estresse

- [ ] `float estresseAtual` dentro do `GameStateSO`, métodos `AumentarEstresse(float valor)` e `ReduzirEstresse(float valor)` com `Mathf.Clamp(0,100)`
- [ ] UI: **Slider** do uGUI (ou Image com `fillAmount`) atualizado via `GameEvent<float> OnEstresseMudou`
- [ ] Penalidade de 10s "paralisado": no início de qualquer minigame, checar `if (estresseAtual > 70) { desabilitar input por 10s via Coroutine }`, com feedback visual (Animator de "ansiedade" no personagem ou um overlay de UI escurecendo a tela brevemente)
- [ ] Colapso ao atingir 100%: dispara `GameEvent OnColapsoEstresse` → aplica penalidade em todas as notas (`nota -= valorPenalidade` em todas as disciplinas) e pula para o próximo ponto narrativo (perde o dia)

### 2.13 UI Geral

- [ ] **Canvas** em modo `Screen Space - Overlay` para HUD e menus
- [ ] Caderneta Acadêmica: painel ativado/desativado por `Input.GetKeyDown(KeyCode.Escape)`, populando dinamicamente os campos de nota/estresse/semana a partir do `GameStateSO` toda vez que é aberto (`OnEnable()` lendo os dados)
- [ ] Prompt de interação ("Pressione E"): **TextMeshProUGUI** posicionado via `Canvas` em `World Space` acima do NPC, ativado pelo Trigger Collider (seção 2.6)
- [ ] Menu principal: cena própria com **Button** (uGUI) para "Iniciar", "Sair" — pode reaproveitar o mesmo `DialogueUI`/estilo visual do resto do jogo para consistência

### 2.14 Cutscenes (Formato Leve "Visual Novel")

- [ ] Cena dedicada `CutsceneScene` com:
  - **Image** de fundo (full screen) trocável via script, recebendo a ilustração/sprite estático da cena
  - **TextMeshProUGUI** com a fala/narração atual
  - **Button** "Avançar" (ou tecla de input) para passar para a próxima linha
- [ ] `CutsceneSO` (ScriptableObject) com lista de `(Sprite imagem, string texto)` — mesma lógica de dados do `DialogoSO`, reaproveitando boa parte da `DialogueUI` já construída
- [ ] Transições suaves entre imagens: `CanvasGroup.alpha` animado via Coroutine ou **DOTween** (`canvasGroup.DOFade(1, 0.5f)`) se o asset estiver instalado — evita cortes secos
- [ ] As 6 cutscenes do documento usam exatamente essa estrutura: Abertura, Aula Inaugural, Transição, e os 3 finais (cada final troca apenas o `CutsceneSO` carregado, conforme o resultado das médias)

### 2.15 Cálculo de Finais

- [ ] Método `CalcularFinal()` no `GameManager`, chamado após a rodada final de provas:
  - Conta quantas disciplinas têm média < 4.0 e quantas estão entre 4.0–6.9
  - Aplica a lógica do documento: `>=7.0 em todas` → Aprovação Direta | `4.0-6.9 em 1+` → Avaliação Final | `<4.0 em 2+` → Reprovação
- [ ] Resultado determina qual `CutsceneSO` de final é carregado na `CutsceneScene`

### 2.16 Side Quests

- [ ] **Side Quest 1 (Notebook Desaparecido):** sequência de `DialogoSO` encadeados por flags (`notebook_etapa1`, `notebook_etapa2`...) — cada NPC envolvido (Professor, Atendente do RU) checa a flag atual antes de oferecer o diálogo correspondente. O notebook em si é um objeto `IInteractable` escondido embaixo de uma mesa no Tilemap do Bloco 2, só "ativo" (collider habilitado) quando a flag de etapa 3 estiver setada
- [ ] **Side Quest 2 (Colega em Risco):** gatilho via Trigger Collider posicionado em um ponto fixo de maior fluxo (simplificação do "o jogo rastreia onde o jogador passa mais" do documento — para o MVP, um ponto fixo já resolve sem precisar de tracking de movimento real). Decisão A/B no `DialogoSO` seta a flag `gabriel_ajudado` ou `gabriel_recusado`, consultada depois no Arco 4 (dica de bug ou bilhete na cadeira vazia)

### 2.17 Áudio

- [ ] **AudioSource** por área (RU, corredores, área de convivência) com **AudioMixer** dedicado para controlar volume geral/música/SFX separadamente
- [ ] Trigger Colliders nas transições entre áreas chamando `AudioSource.Play()`/`Stop()` ou crossfade simples (`AudioSource.volume` animado via Coroutine) ao entrar/sair de uma região
- [ ] Trilha lo-fi/chiptune royalty-free se não houver tempo de compor original (sites como Incompetech, Pixabay Music ou itch.io de música livre resolvem rápido)

---

## 3. Checklist de Conteúdo (Diálogo & NPCs)

- [ ] Prof. Coordenador — falas de todos os pontos-chave do documento de narrativa (adaptar para `DialogoSO`)
- [ ] Prof. de Matemática — side quest completa + falas de prova
- [ ] Prof. de Fundamentos — falas de introdução ao Debug (rodada 1 e rodada 2)
- [ ] Veteranos — trote + falas ambientes (incluindo referência a PAA)
- [ ] Gabriel — Side Quest 2 completa
- [ ] Atendente do RU — pista da side quest do notebook + easter egg da rã
- [ ] Alunos de IA — diálogo opcional sobre a obra do Bloco 5
- [ ] **Dias** (novo NPC) — 2-3 falas ambientes, recorrente em pontos diferentes do mapa
- [ ] NPCs da mesa de ping pong — linha única "vai marcar time de fora?"
- [ ] Convite para a Calourada — evento curto entre Arco 1 e Arco 2
- [ ] Evento do café na cantina — opção de slot de tempo livre
- [ ] Evento do Cedro — cena contemplativa do Arco 3

---

## 4. Checklist Final de "Pronto para Entregar"

- [ ] Os 4 arcos são jogáveis do início ao fim sem crash
- [ ] Os 3 minigames funcionam e geram nota
- [ ] As 2 side quests podem ser concluídas ou ignoradas, com consequência visível
- [ ] A Caderneta mostra as 5 disciplinas com valores coerentes
- [ ] O sistema de estresse afeta o jogo de forma perceptível (pelo menos 1 colapso testado)
- [ ] Os 3 finais disparam corretamente conforme as médias
- [ ] As referências de autenticidade (Dias, PAA, rã do RU, Cedro, ping pong, calourada) estão implementadas
- [ ] Menu principal e tela de título funcionam
- [ ] Build testado do zero por alguém que não é da equipe, sem travar

---

*Roadmap gerado para o projeto Calouro.exe — UFC Quixadá — Engenharia de Software, com base no GDD v1 e no Documento de Narrativa v1.0.*