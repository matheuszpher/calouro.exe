# Calouro.exe — Roadmap v2.1 (MVP)

> **Contexto:** Unity 6, MVP de ~1h de gameplay cobrindo os 4 arcos, build final apresentada para a professora em **Windows e Linux**.
> **Como usar este documento:** leia primeiro o `CLAUDE.md` na raiz do projeto — ele define como seguir este roadmap e as convenções do código. Este arquivo é a **fonte única de verdade do escopo**: toda tarefa de desenvolvimento nasce de um checkbox daqui.
>
> **Histórico:** v2 partiu do estado real do código e cobriu os 10 gargalos da 1ª auditoria. **v2.1** adiciona os 8 gargalos da 2ª auditoria (seção 1B) e incorpora 4 decisões do time: build Windows+Linux, **sem** menu de debug, Gabriel na semana 6/Arco 2 (GDD literal), e **save em disco entra no escopo**.

---

## 0. Estado Atual do Projeto (o que JÁ existe)

| Sistema | Status | Onde |
|---|---|---|
| Movimento top-down + animação | ✅ Pronto | `PlayerController2D`, `SpriteWalkAnimator` (Input System novo) |
| Câmera seguindo o jogador | ✅ Pronto | `CameraFollow2D` (custom, sem Cinemachine — manter) |
| Interação com NPC + diálogo com escolha A/B | ✅ Pronto | `NpcInteractable`, `DialogueManager.StartChoice` |
| Minigame 2 — Labirinto de Matemática (tempo → nota 0–10) | ✅ Pronto | `MazeController`, `MazeExit`, `MazePortal` |
| HUD: barra de estresse + caderneta (ESC) + semana | ⚠️ Parcial | `AcademicHud` — só a nota de Matemática é real; semana é estática; estresse sobe passivamente por segundo (**contraria o GDD** — deve ser por evento) |
| Interiores de prédios (portas, salas) | ✅ Pronto | `InteriorController`, `BuildingDoor`, `RoomExit` |
| Tela de título + nome do jogador | ✅ Pronto | `TitleScreen`, `GameProgress.PlayerName` |
| Quest linear de demo (coordenador → colega → Bloco 1) | ✅ Protótipo | `QuestManager` — vira a base do "diretor de arcos" |
| Estado global | ⚠️ Mínimo | `GameProgress` estático — só `MathGrade` e `PlayerName` |
| Arte do campus (tiles, blocos, RU) | ⚠️ Em progresso | `Assets/Art/Campus`, `Assets/Art/Env` |

**Não existe ainda:** Runner do trote, Labirinto de Debug (perseguição), arcos/avanço de semanas, slots de tempo livre, side quests do GDD, cutscenes, finais, flags narrativas, notas de IHC/Ética/Intro ES, colapso de estresse, penalidade de 10s, áudio, referências de autenticidade, save em disco, menu de pausa, coletáveis, tutorial integrado, builds Windows/Linux.

---

## 1. Gargalos da 1ª auditoria (o que o Roadmap v1 não cobria)

1. **Espinha dorsal de progressão (o maior gargalo).** O v1 listava sistemas isolados, mas nenhuma seção descrevia **quem sequencia o jogo**: avanço de semanas, marcos que encerram cada arco, quais eventos são obrigatórios e em que ordem, e as 3 rodadas de prova (Arco 2, 3 e 4). → Seção 3.1.
2. **Slots de tempo livre.** Sistema central do GDD ("Gerenciar o Tempo") — o v1 só o citava de passagem, sem itens de implementação. → Seção 3.3.
3. **Notas de IHC, Ética e Intro ES.** A decisão do v1 dizia "via escolhas + resumos narrados", mas nenhum mecanismo foi definido. Sem essas notas, o cálculo dos finais quebra. → Seção 3.4.
4. **Bônus de estudo.** "Estudar sozinho aumenta desempenho no próximo minigame" não aparecia no v1. → Seção 3.3.
5. **Rodada final de provas em sequência** (Narrativa §6.3) — o v1 só cobria o `CalcularFinal()`. → Seção 3.1.
6. **Consequências cruzadas específicas** não itemizadas: entrada secreta (+20s), dica de atalho dos veteranos, "negociar" no trote (+3s), expiração da quest do notebook, penalidade por matar aula 2+ vezes. → Seções 3.8 e 3.12.
7. **Estresse por evento, não por tempo.** O código atual sobe estresse por segundo; o GDD define estresse por evento. → Seção 3.5.
8. **Créditos finais** (Narrativa §9) ausentes no v1. → Seção 3.11.
9. **Falta de ordenação/dependências.** O v1 organizava por sistema, sem dizer o que bloqueia o quê. → Fases A–E deste documento.
10. **Desalinhamento técnico com o código real.** O v1 recomendava Cinemachine, TextMeshPro e SO event channels; o projeto já usa Input System novo, câmera própria e UI por código. **Decisão: estender o que existe, não migrar.**

## 1B. Gargalos da 2ª auditoria (novos — não cobertos nem pelo v2)

11. **Coletáveis e eventos aleatórios de exploração.** O GDD ("Explorar o Campus") define itens coletáveis — apostilas, marmita do RU, dicas de estudo — e "eventos que surgem aleatoriamente". Nada disso tinha tarefa em nenhuma versão do roadmap. Sem eles, a ação "Explorar" dos slots de tempo livre não recompensa nada. → Seção 3.13.
12. **Decisão "Denunciar veterano excessivo".** Está na tabela de Interações do GDD e na tabela de Consequências Cruzadas da Narrativa (§10: "tensão social — sem dicas dos veteranos na reta final"), mas **nenhum documento define onde/quando a escolha acontece** e nenhum roadmap a cobria. Decisão de design tomada na seção 2. → Seção 3.12.
13. **Menu de pausa real.** O GDD define ESC como "Menu de pausa / caderneta de notas". Hoje ESC só abre a caderneta: não pausa o jogo, não tem volume, "Salvar", "Voltar ao título" nem "Sair". Numa apresentação, não ter pausa é risco real. → Seção 3.15.
14. **Save em disco.** O v1/v2 tratavam como opcional; **decidido: entra no MVP** (autosave por arco + botão Continuar). Exige que o estado global seja serializável — afeta o design do `GameProgress` da Fase A. → Seção 3.2.
15. **Tutorial integrado.** A Narrativa §3.2 especifica prompts contextuais (W destacado na escada, "E" sobre o NPC, apostila pulsando, Coordenador mencionando o ESC). O v2 citava "tutorial livre" sem nenhuma tarefa concreta. → Seção 3.14.
16. **Balanceamento das fórmulas de nota.** Nenhuma versão garantia que os 3 finais são **alcançáveis**: quantas rodadas compõem a média de cada disciplina, se os deltas de IHC/Ética/Intro ES permitem chegar a 7.0, e se é possível (sem ser trivial) reprovar. Sem uma passada de balanceamento, o risco é a apresentação só conseguir mostrar 1 final. → Seção 3.17.
17. **Feedback visual de nota/estresse + skip de texto.** As consequências devem aparecer "organicamente" (Narrativa §10), mas o jogador precisa de feedback mecânico (toast "+0.5 Ética", "−10 Estresse"). E sem menu de debug (decisão do time), **testar os 3 finais exige jogar o jogo inteiro várias vezes** — skip/aceleração de diálogo e cutscene deixa de ser polish e vira ferramenta de teste. → Seção 3.16.
18. **Pipeline de build multi-plataforma.** Alvo definido: **Windows e Linux**. Nenhum roadmap tinha tarefas de build (players settings, resolução, teste em cada SO). → Seção 3.19.

---

## 2. Decisões de Escopo (fechadas — não reabrir)

Herdadas do v1: Unity; ~1h de gameplay; Minigame 3 reformulado como labirinto de perseguição; 2 side quests completas; 5 disciplinas na caderneta; sistema de estresse completo; branching misto; cutscenes leves "visual novel"; 3 finais.

| Tópico | Decisão |
|---|---|
| **Plataforma da build final** | **Windows (.exe) e Linux (x86_64)**. Testar as duas antes da entrega |
| **Modo debug/apresentação** | **Não haverá menu de debug** (decisão do time em 01/07/2026). Testes dos 3 finais são feitos jogando + arquivos de save preparados manualmente (ver 3.17) |
| **Save** | **Entra no MVP.** JSON via `JsonUtility` em `Application.persistentDataPath`. Autosave no fim de cada arco; botão **Continuar** na tela de título. 1 slot único |
| **Gatilho do Gabriel (SQ2)** | **Arco 2, semana 6**, conforme o GDD literal. A dica dele continua aparecendo no Arco 4. A "sessão de estudo" ocorre ainda no Arco 2 |
| Estado global | Estender `GameProgress` (estático) com um DTO serializável `SaveData` interno. **Sem** ScriptableObject event channels |
| Progressão | `ArcDirector` (evolução do `QuestManager`) controla a sequência de marcos do arco atual. Semana avança em marcos e em slots gastos |
| Slots de tempo livre | Menu simples (UI por código): 2 slots por arco nos Arcos 2–3, 1 no Arco 4. Estudar / Descansar / Explorar / Ajudar / RU / Café |
| Notas IHC/Ética/Intro ES | Base 5.0 + deltas fixos por escolha (tabela em 3.4). Resumo narrado ao fim de cada arco |
| Estresse | Por **evento** apenas (remover subida passiva). Tabela em 3.5 |
| **Denúncia do veterano** | Nova cena de decisão no **Arco 1, logo após o trote**: um veterano exagera com outro calouro; o jogador escolhe [A] Denunciar na Direção (2º andar do Bloco Administrativo) → `veterano_denunciado` (sem dicas no Arco 4, +0.5 Ética) ou [B] Não se envolver (sem efeito) |
| **Pausa** | ESC abre **painel de pausa** que pausa o jogo (`Time.timeScale = 0`): caderneta integrada + botões Continuar / Volume (slider) / Salvar / Voltar ao Título |
| Cutscenes | Tela estática + texto avançando, via overlay de Canvas na própria cena (reaproveita o estilo do `DialogueManager`) |
| Criação de personagem | Só nome (já existe). Aparência fixa |
| Aula pulada / café | Contador `AulasPuladas`; 2+ → −0.5 em todas as notas no Arco 4 |
| Documentos de design | GDD e Narrativa devem ser copiados para `docs/` no repositório (tarefa 3.20) |

### Cut-list de emergência (em ordem — cortar de cima para baixo)
1. Eventos aleatórios de exploração (coletáveis fixos ficam; a aleatoriedade sai)
2. Referências de autenticidade opcionais (Dias, ping pong, Cedro, calourada) → 1 linha de diálogo cada ou somem
3. Resumos narrados de IHC/Ética viram texto na própria caderneta
4. Side Quest 2 reduzida a 1 cena de diálogo (decisão + consequência no Arco 4)
5. Progressão de dificuldade dos minigames: 1 nível só
6. Cutscene 3 (transição) cortada — fade simples
7. Áudio: 1 trilha única
8. Cena da denúncia do veterano vira escolha dentro de um diálogo existente
9. **Nunca cortar:** os 3 finais, o trote jogável, os 2 labirintos gerando nota, a side quest do notebook, o colapso de estresse, o save/Continuar e o menu de pausa.

---

## 3. Fases de Implementação (ordenadas por dependência)

### FASE A — Espinha Dorsal (bloqueia tudo; fazer primeiro)

**3.1 GameProgress + ArcDirector**
- [ ] Criar classe serializável `SaveData` (campos públicos, compatível com `JsonUtility`): `float mathGrade, fundGrade, ihcGrade, eticaGrade, introEsGrade`, `float stress`, `int week`, `int arc`, `List<string> flags` (JsonUtility não serializa Dictionary — usar lista de strings ativas), `int aulasPuladas`, `bool studyBonus`, `string playerName`, `int slotsRestantesNoArco`
- [ ] `GameProgress` estático passa a envolver uma instância de `SaveData` (`GameProgress.Data`), mantendo `MathGrade`/`PlayerName` como propriedades de compatibilidade para não quebrar `MazeController`/`TitleScreen`
- [ ] Utilitários: `SetFlag(string id)`, `HasFlag(string id)`, `AddStress(float delta)` (movido do HUD para o estado), `AdvanceWeek(int n)`, `ResetRun()` (novo jogo)
- [ ] Transformar `QuestManager` em `ArcDirector`: lista sequencial de marcos por arco (dados hardcoded em C# — não precisa de editor de conteúdo). Cada marco: condição de disparo + ação (diálogo, cutscene, minigame, menu de slots)
  - **Arco 1 (sem. 1–3):** cutscene abertura → tutorial livre (3.14) → cutscene aula inaugural (libera caderneta) → trote (minigame 1) → cena da denúncia do veterano → convite da Calourada → transição p/ Arco 2
  - **Arco 2 (sem. 4–7):** rotina + 2 slots livres → gatilho do notebook (expira em 2 visitas à convivência) → **semana 6: gatilho do Gabriel (SQ2)** → prova labirinto R1 → prova debug R1 → resumo narrado de IHC/Ética → cutscene de transição → **autosave**
  - **Arco 3 (sem. 8–13):** conversa do Coordenador (meio do semestre) → 2 slots livres (inclui evento do Cedro no fim do arco) → prova labirinto R2 → prova debug R2 → fala do Coordenador condicional às notas (≥6.0 / 4.0–5.9 / <4.0) → resumo narrado → **autosave**
  - **Arco 4 (sem. 14–18):** consequências visíveis (bilhete do Gabriel OU dica dele; dicas ou frieza dos veteranos) → última conversa + resposta final A/B/C → rodada final (labirinto + debug em sequência, nota exibida em tempo real após cada um) → `CalcularFinal()` → cutscene do final + créditos
- [ ] `CalcularFinal()`: todas ≥7.0 → Aprovação Direta | alguma 4.0–6.9 (e menos de 2 abaixo de 4.0) → Avaliação Final | 2+ abaixo de 4.0 → Reprovação
- [ ] HUD de objetivo atual (herdado do `QuestManager` — manter e alimentar pelo marco ativo)

**3.2 Save em disco (NOVO — decisão do time)**
- [ ] `SaveSystem` estático: `Save()` → `JsonUtility.ToJson(GameProgress.Data)` gravado em `Application.persistentDataPath + "/save.json"`; `Load()` → lê e popula `GameProgress.Data`; `HasSave()`; `Delete()`
- [ ] Autosave automático no fim de cada arco (chamado pelo `ArcDirector` na transição) e ao usar "Salvar" no menu de pausa
- [ ] `TitleScreen`: botão **Continuar** visível apenas se `HasSave()`; "Novo Jogo" com save existente pede confirmação e chama `Delete()` + `ResetRun()`
- [ ] Ao carregar: `ArcDirector` deve saber retomar do **início do arco salvo** (não é preciso salvar posição no mapa — retomar no marco inicial do arco simplifica e evita bugs de estado)
- [ ] Testar: salvar no Arco 2, fechar o jogo, Continuar → estado íntegro (notas, flags, estresse, semana)

**3.3 Slots de Tempo Livre**
- [ ] Painel de escolha (UI por código) aberto pelo `ArcDirector` nos pontos definidos; mostra quantos slots restam no arco
- [ ] Estudar → `StudyBonus = true` (próximo minigame: −15% na complexidade/tempo alvo; consumido ao usar)
- [ ] Descansar → `AddStress(-10)`
- [ ] Explorar → fecha o menu e libera andar pelo campus até o próximo marco (diálogos opcionais e coletáveis ativos — ver 3.13)
- [ ] Ajudar colega → só aparece se a SQ2 estiver ativa e sem resposta
- [ ] RU fora do horário → pista do notebook (se quest ativa) + `AddStress(-5)`
- [ ] Café na cantina (Arcos 2–3) → `AddStress(-12)` e `AulasPuladas++` (aviso sutil no toast: "isso conta como falta…")
- [ ] Cada slot gasto avança a semana em 1

### FASE B — Sistemas de Nota e Estresse

**3.4 Notas das 5 disciplinas**
- [ ] Matemática: média das rodadas de labirinto jogadas (R1, R2, final)
- [ ] Fundamentos: média das rodadas de debug (R1, R2, final)
- [ ] IHC / Ética / Intro ES — base 5.0 + deltas por escolha:

| Escolha | IHC | Ética | Intro ES |
|---|---|---|---|
| Ajudou Gabriel | — | +0.5 | — |
| Recusou Gabriel | — | −0.5 | — |
| Devolveu o notebook | — | +1.0 | — |
| Denunciou o veterano excessivo | — | +0.5 | — |
| Escolhas "empáticas" em diálogos-chave (3 pontos no jogo, +1.0 cada) | +1.0 | — | — |
| Compareceu aos marcos de aula do Coordenador (por arco) | — | — | +1.5 |
| Colapso de estresse | −1.0 em todas | | |
| 2+ aulas puladas (aplicado no Arco 4) | −0.5 em todas | | |

- [ ] Resumo narrado ao fim de cada arco: "Saíram as notas parciais de IHC e Ética: você está com X e Y"
- [ ] Caderneta (`AcademicHud`) lendo tudo de `GameProgress.Data` (remover o array local de stub)

**3.5 Estresse por evento (corrigir o passivo)**
- [ ] Remover `stressPerSecond` do `AcademicHud`
- [ ] Tabela de eventos: pego no trote +15 | falhar prova (nota <4) +10 | captura no debug +5 cada | ajudar Gabriel +8 | descanso −10 | RU −5 | café −12 | Cedro: reset para 30%
- [ ] Penalidade de prova: `Stress > 70` ao iniciar labirinto/debug → 10s de input travado com overlay de "ansiedade" (escurecer bordas + texto)
- [ ] Colapso a 100: perde o dia (avança semana), −1.0 em todas as notas, estresse volta a 50, mensagem narrada explicando o que houve

### FASE C — Minigames que faltam

**3.6 Minigame 1 — Fuga do Trote (Runner)**
- [ ] Cena nova; scroll automático; Espaço pula, S abaixa, D acelera (mapear no Input System existente)
- [ ] Chão via `OverlapCircle`; abaixar reduz o collider
- [ ] Veteranos como triggers → 3 capturas = pego (`SetFlag("trote_pego")`, +15 estresse, mini-cutscene cômica) | chegar ao fim = `SetFlag("trote_escapou")`
- [ ] Escolha pré-minigame: [A] Fugir | [B] Negociar → começa com 3s de vantagem (veteranos entram atrasados)

**3.7 Minigame 3 — Labirinto de Debug (Perseguição)**
- [ ] Parametrizar `MazeController` (disciplina alvo da nota, tilemap usado, perseguidor on/off) em vez de duplicar o script
- [ ] `PerseguidorAI` por waypoints (`MoveTowards` entre pontos pré-definidos; sem pathfinding real)
- [ ] Captura = +8s no cronômetro (não reinicia) e +5 de estresse
- [ ] R2/final: perseguidor mais rápido + labirinto maior
- [ ] Se `gabriel_ajudado`: antes do debug final, diálogo do Gabriel com a dica → −10s no tempo final

**3.8 Variações do labirinto de Matemática**
- [ ] 2 tilemaps por rodada (reduzido/padrão) ativados conforme `notebook_devolvido`
- [ ] Labirinto final: entrada secreta ativa se `notebook_devolvido` (atalho que economiza ~20s)
- [ ] Dica de atalho dos veteranos antes do labirinto final se (`trote_escapou` OU `trote_pego`) **E NÃO** `veterano_denunciado`

### FASE D — Conteúdo Narrativo e Mundo

**3.9 Side Quest 1 — Notebook Desaparecido**
- [ ] Flags de etapa: `notebook_etapa1..4`; gatilho no Arco 2; expira após 2 visitas à convivência sem interagir → `notebook_expirado`
- [ ] Diálogos das 4 etapas (professor → atendente do RU → laboratório do Bloco 2 → devolução) — textos prontos na Narrativa §7.1
- [ ] Notebook = objeto interagível no Bloco 2, collider ativo só na etapa 3

**3.10 Side Quest 2 — Colega em Risco (Gabriel) — Arco 2, semana 6**
- [ ] Renomear/reaproveitar o NPC "Natan" da demo como Gabriel, com o diálogo da Narrativa §5.2
- [ ] Caminho A: cena de estudo no Bloco 3 (diálogo do ponteiro/GPS), +8 estresse, `gabriel_ajudado`, consome 1 slot
- [ ] Caminho B: `gabriel_recusado` → no Arco 4, bilhete na cadeira vazia ("Tive que voltar pra casa…")
- [ ] Arco 4 (se ajudou): Gabriel aparece antes do debug final com a dica das "variáveis com nomes parecidos"

**3.11 Cutscenes (6) + Créditos**
- [ ] Overlay de Canvas: imagem estática cheia + caixa de texto avançando (reaproveitar estilo do `DialogueManager`)
- [ ] 1-Abertura, 2-Aula Inaugural, 3-Transição (30s, sem diálogo), 4/5/6-Finais — textos prontos na Narrativa §8
- [ ] Resposta final ao Coordenador (A/B/C) ecoada no texto da cutscene final
- [ ] Créditos: texto subindo com a equipe + "UFC Quixadá" ao fim de qualquer final, retornando ao título

**3.12 Diálogos, referências de autenticidade e denúncia do veterano**
- [ ] Coordenador: falas de todos os marcos (variantes por faixa de nota no fim do Arco 3)
- [ ] **Cena da denúncia (novo):** após o trote, um veterano exagera com outro calouro na convivência; escolha [A] Denunciar na Direção → `veterano_denunciado` / [B] Não se envolver
- [ ] Falas ambientes: veterano ("bem-vindo ao caos"), aluna de IA (Bloco 5), atendente do RU (marmita 13h), PAA, rã do RU
- [ ] Dias (faz-tudo) em 2 pontos do mapa | mesa de ping pong (1 linha) | convite da Calourada (transição Arco 1→2) | Cedro (slot especial fim do Arco 3, reset parcial de estresse)

**3.13 Coletáveis e eventos de exploração (NOVO)**
- [ ] Item coletável `IInteractable` genérico com tipo: **Apostila** (+0.3 na disciplina relacionada, máx. 1 por arco por disciplina), **Marmita do RU** (−5 estresse), **Dica de estudo** (bilhete com dica real de minigame)
- [ ] Espalhar 2–3 coletáveis por arco em pontos que incentivem explorar (laboratórios, convivência, RU)
- [ ] Eventos aleatórios leves durante "Explorar": ao entrar numa área, 30% de chance de um evento de 1 diálogo (colega pedindo direção, aviso no mural, fila do RU) — pool de 5–6 eventos escritos; **primeiro item da cut-list**

**3.14 Tutorial integrado (Arco 1)**
- [ ] Prompt contextual de tecla: "W" ao encostar na escada do Bloco 1 (primeira vez)
- [ ] Prompt "E" sobre o primeiro NPC próximo (já existe o prompt de interação — garantir destaque na 1ª vez)
- [ ] Primeira apostila coletável com pulso visual (escala/alpha animado por código)
- [ ] Fala do Coordenador mencionando "você pode ver suas notas apertando ESC" logo após a aula inaugural
- [ ] Nenhuma tela de instrução flutuante — tudo dentro do mundo, conforme Narrativa §3.2

### FASE E — Fechamento

**3.15 Menu de pausa (NOVO)**
- [ ] ESC abre painel que **pausa o jogo** (`Time.timeScale = 0`; bloquear input do player e do diálogo)
- [ ] Conteúdo: caderneta acadêmica integrada (reaproveita `AcademicHud`) + botões **Continuar**, **Volume** (slider único de master via `AudioListener.volume`), **Salvar**, **Voltar ao Título** (com confirmação)
- [ ] Garantir que pausar durante minigame congela cronômetro e perseguidor
- [ ] ESC dentro de diálogo/cutscene: fecha só o painel, não pula conteúdo

**3.16 Feedback visual + skip de texto (NOVO)**
- [ ] Toast discreto no canto (fila de mensagens, 2s cada): "+0.5 Ética", "−10 Estresse", "Semana 7", "Quest registrada na caderneta"
- [ ] Diálogo: 1º clique/tecla completa a linha instantaneamente, 2º avança (padrão do gênero)
- [ ] Segurar tecla (ex.: Ctrl) acelera diálogos e cutscenes já vistos — **isso é a ferramenta de teste dos 3 finais**, já que não haverá menu de debug
- [ ] Flash curto na barra de estresse ao mudar de valor

**3.17 Balanceamento e verificação dos 3 finais (NOVO)**
- [ ] Planilha rápida (pode ser comentário no código ou `docs/balanceamento.md`): nota máxima e mínima alcançável por disciplina, somando minigames + deltas + coletáveis
- [ ] Verificar: jogador mediano (sem side quests, tempos medianos) deve cair em **Avaliação Final**; jogador dedicado alcança **Aprovação Direta**; ignorar tudo leva à **Reprovação**
- [ ] Ajustar fórmula tempo→nota dos labirintos por rodada (labirintos maiores precisam de fator de escala maior)
- [ ] 3 playthroughs completos de verificação (um por final), usando o skip do 3.16; guardar os 3 `save.json` resultantes como fixtures de teste manual
- [ ] Ajustar deltas da tabela 3.4 se algum final se mostrar inalcançável

**3.18 Áudio**
- [ ] 1 trilha lo-fi de exploração + 1 tensa para minigames + 1 emocional para finais (royalty-free: Pixabay/Incompetech)
- [ ] SFX mínimos: passo, confirmação de diálogo, captura, coletável (opcional)
- [ ] Volume master controlado pelo slider da pausa (3.15)

**3.19 Builds Windows + Linux (NOVO)**
- [ ] Player Settings: nome "Calouro.exe", ícone, resolução padrão 1920×1080 em janela redimensionável, `Run In Background` off
- [ ] Build Windows x86_64 e build Linux x86_64 (instalar o Linux Build Support no Hub se faltar)
- [ ] Testar a build Windows em máquina que não é da equipe; testar a Linux (máquina real ou VM/Steam Deck de alguém do time)
- [ ] Conferir que `persistentDataPath` (save) funciona nos dois SOs
- [ ] Zipar as duas builds com nome versionado (`calouro-exe-v1.0-win.zip` / `-linux.zip`)

**3.20 Mapa, arte e documentação**
- [ ] Terminar a montagem do campus com os tiles de `Assets/Art` (convivência, RU, Bloco 2 com laboratório, obra do Bloco 5 visível e cercada)
- [ ] Conferir sprites dos NPCs principais (Coordenador, Prof. Matemática, Prof. Fundamentos, Gabriel, Atendente, veteranos, Dias)
- [ ] Copiar GDD (PDF) e Narrativa (DOCX) para `docs/` no repositório
- [ ] Manter os checkboxes deste roadmap atualizados (ver `CLAUDE.md`)

**3.21 Estabilidade e playtest final**
- [ ] Fluxo título → novo jogo → 4 arcos → final → créditos → título sem crash
- [ ] Fluxo título → Continuar (save do Arco 3) → final sem crash
- [ ] 1 colapso de estresse testado de ponta a ponta
- [ ] Pausa aberta/fechada em exploração, diálogo e minigame sem travar estado
- [ ] Build testada do zero por alguém de fora da equipe

---

## 4. Sugestão de Distribuição na Semana

| Dia | Foco |
|---|---|
| 1 | 3.1 GameProgress/ArcDirector + 3.2 Save + 3.3 Slots — **nada avança sem a Fase A** |
| 2 | 3.4 Notas + 3.5 Estresse por evento + 3.15 Pausa (é pequena e destrava teste de tudo) |
| 3 | 3.6 Runner do trote + 3.8 Variações do labirinto |
| 4 | 3.7 Debug/perseguição + 3.9 Side Quest 1 |
| 5 | 3.10 Side Quest 2 + 3.11 Cutscenes/créditos + 3.16 Feedback/skip |
| 6 | 3.12 Diálogos/denúncia + 3.13 Coletáveis + 3.14 Tutorial + 3.18 Áudio + 3.20 Mapa |
| 7 | 3.17 Balanceamento (3 playthroughs) + 3.19 Builds + 3.21 Playtest — **não adicionar feature no dia 7** |

> Se algo estourar, cortar pela cut-list da seção 2 — nunca improvisar corte fora dela.

---

## 5. Checklist Final de "Pronto para Entregar"

- [ ] Os 4 arcos jogáveis do início ao fim sem crash, com semanas avançando na caderneta
- [ ] 3 minigames funcionam e geram nota (trote → estresse/flags; labirintos → Matemática e Fundamentos)
- [ ] As 5 disciplinas mostram notas reais e coerentes na caderneta
- [ ] Slots de tempo livre funcionam e afetam nota/estresse
- [ ] 2 side quests concluíveis ou ignoráveis, com consequência visível no Arco 4
- [ ] Estresse: penalidade de 10s e colapso testados
- [ ] **Save/Continuar funciona (fechar o jogo no Arco 2+ e retomar)**
- [ ] **Menu de pausa completo (pausa real, volume, salvar, voltar ao título)**
- [ ] **3 finais comprovadamente alcançáveis (playthroughs do 3.17 feitos) + créditos**
- [ ] Consequências cruzadas mínimas: notebook→labirinto fácil+entrada secreta; Gabriel→dica no debug final+Ética; trote/denúncia→dicas ou frieza dos veteranos
- [ ] **Coletáveis e tutorial integrado presentes no Arco 1**
- [ ] Referências de autenticidade presentes (ou cortadas conscientemente pela cut-list)
- [ ] **Builds Windows e Linux geradas, zipadas e testadas**
- [ ] Menu principal, nome do jogador e fluxo completo sem bugs visíveis

---

*Roadmap v2.1 — GDD v1 + Narrativa v1.0 + auditoria do código (01/07/2026) + decisões do time de 01/07/2026 (build Win+Linux; sem menu de debug; Gabriel no Arco 2/semana 6; save em disco). Leia o `CLAUDE.md` antes de começar qualquer tarefa.*
