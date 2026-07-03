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
| Criação de personagem | Nome + **escolha de personagem na tela de título: calouro (homem) ou caloura (mulher)** — só essas duas opções, sem customização além disso (decisão do time em 02/07/2026, substitui o "aparência fixa" anterior). Folhas 6x4 `calouro.png`/`caloura.png`; a escolha vive em `GameProgress.PlayerCharacter` e é aplicada pelo `PlayerAppearance` |
| Aula pulada / café | Contador `AulasPuladas`; 2+ → −0.5 em todas as notas no Arco 4 |
| Documentos de design | GDD e Narrativa devem ser copiados para `docs/` no repositório (tarefa 3.20) |
| **Calendário do semestre (NOVO, decisão do time em 03/07/2026)** | O semestre passa a ter uma linha do tempo explícita de **100 dias**, com o jogador jogando **14 dias concretos** (ver tabela em 3.1B) e o resto coberto por time skips. `GameProgress.SemesterDay` (1–100) é a fonte única da verdade; `AcademicHud.week` vira **derivado** dele (`arredondar(dia ÷ 5,56)`, já que 100 não divide igual em 18 semanas) — não é mais escrito diretamente. Contador **"Faltam N dias"** fixo no topo da tela (`AcademicHud`), sempre visível. Só Matemática e Fundamentos (os 2 minigames "nunca cortar") são sempre jogados quando caem numa prova; provas narrativas (IHC/Ética/Intro ES) que caírem dentro de um time skip futuro podem virar **notícia numa tela de resumo ao voltar** (ver 3.1B), em vez de sempre jogadas |
| **Notebook (SQ1) resolvido no mesmo dia (ajuste sobre 3.9, decisão de 03/07/2026)** | A side quest do notebook (professor → atendente do RU → laboratório do Bloco 2 → devolução) acontece **inteira no mesmo dia jogável**, sem expirar por visitas em dias diferentes — substitui o "expira após 2 visitas à convivência" de 3.9 |
| **Gabriel/Gabriela espelha o gênero do jogador (ajuste sobre 3.10, decisão de 03/07/2026)** | O NPC da SQ2 é **Gabriel** se `GameProgress.PlayerCharacter == "calouro"` e **Gabriela** se `"caloura"` (sprite e nome trocam com o gênero, mesma convenção 6x4 do `PlayerAppearance`) |

### Cut-list de emergência (em ordem — cortar de cima para baixo)
1. Minigame do Vitim (pingue-pongue, 3.7B) — flavor opcional, não afeta notas/estresse/finais; se cortar, volta a ser a linha de diálogo original ("Iai, vai marcar time de fora?" sem aceitar o convite)
2. Eventos aleatórios de exploração (coletáveis fixos ficam; a aleatoriedade sai)
3. Referências de autenticidade opcionais (Dias, Cedro, calourada) → 1 linha de diálogo cada ou somem
4. Resumos narrados de IHC/Ética viram texto na própria caderneta
5. Side Quest 2 reduzida a 1 cena de diálogo (decisão + consequência no Arco 4)
6. Progressão de dificuldade dos minigames: 1 nível só
7. Cutscene 3 (transição) cortada — fade simples
8. Áudio: 1 trilha única
9. Cena da denúncia do veterano vira escolha dentro de um diálogo existente
10. **Nunca cortar:** os 3 finais, o trote jogável, os 2 labirintos gerando nota, a side quest do notebook, o colapso de estresse, o save/Continuar e o menu de pausa.

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

**3.1B Calendário dos 100 dias (NOVO — decisão do time em 03/07/2026)**

Substitui a ideia solta de "semana" como calendário principal. O semestre tem **100 dias absolutos** (`GameProgress.SemesterDay`); o jogador só joga **14 deles** — os outros são cobertos por time skips (`DayTransition`, mensagem "Algumas semanas depois..."). Os 4 Arcos continuam existindo, agora como faixas de dia (proporcional às semanas antigas: 3/4/6/5 de 18 semanas → 17/22/33/28 dias de 100).

| Dia | Arco | Conteúdo | Status |
|---|---|---|---|
| 1 | 1 (dias 1–17) | Aula IHC (Rainara) + Matemática (Aragão) + ética c/ Emilly + ping-pong Vitim | ✅ pronto |
| 2 | 1 | Aula FUP (Paulete) + ética c/ Yasmin + Enzo | ✅ pronto |
| 3 | 1 | Ajudar Matheus + estudar c/ Natan | ✅ pronto |
| 4 | 1 | Trote — perseguição no campus (3.6, ver nota) | ✅ pronto |
| *(sem dia fixo ainda)* | 1 | Denúncia do veterano (3.12) — não incluída no Dia 4; falta decidir onde entra | ☐ a fazer |
| *skip → 20* | 1→2 | "Algumas semanas depois..." — cobre resto da adaptação + convite da Calourada (1 linha/notícia) | ✅ mecanismo pronto (`semesterDayAfterSkip`) |
| 20 | 2 (dias 18–39) | Prova R1: IHC + IES + FUP + Matemática (bloco já implementado, vira a "R1" do roadmap) | ✅ pronto |
| 28 | 2 | Notebook desaparecido (SQ1, 3.9) — completo no mesmo dia (ver ajuste na seção 2) | ✅ pronto |
| 32 | 2 | Gabriel/Gabriela pede ajuda — sessão de estudo (SQ2, 3.10) | ☐ a fazer |
| 37 | 2 | 2º slot de tempo livre (3.3) | ☐ a fazer |
| 48 | 3 (dias 40–72) | Slot de tempo livre (respiro do meio do semestre, 3.3) | ☐ a fazer |
| 58 | 3 | Conversa do Coordenador (meio do semestre, 3.1 Arco 3) | ☐ a fazer |
| 68 | 3 | Evento do Cedro (slot especial, reset parcial de estresse, 3.3/3.12) | ☐ a fazer |
| 70 | 3 | Prova R2: Matemática (labirinto) + Fundamentos (debug) — sempre jogada (cut-list: nunca cortar) | ☐ a fazer |
| 85 | 4 (dias 73–100) | Consequências visíveis (Gabriel/veteranos) + última conversa (resposta A/B/C) | ☐ a fazer |
| 97 | 4 | Rodada final de provas (labirinto + debug finais) — sempre jogada → `CalcularFinal()` | ☐ a fazer |
| 100 | 4 | Cutscene do final + créditos (desfecho, não é "dia jogável") | ☐ a fazer |

- [x] `GameProgress.SemesterDay`/`SemesterTotalDays` + `JumpSemesterDayTo` (dia absoluto do semestre, nunca recua)
- [x] `AcademicHud.week` derivado de `SemesterDay` (não é mais escrito por fora); caderneta mostra "Dia X de 100 · Semana Y/18"
- [x] Contador fixo "Faltam N dias" no topo da tela (`AcademicHud`, sempre visível, atualiza em tempo real)
- [x] `QuestManager`: objetivo de time skip carrega `semesterDayAfterSkip` (hoje só o Dia 3→20; próximos dias da tabela precisam do mesmo campo ao serem implementados)
- [x] Dia 4 (Trote — perseguição no campus, ver 3.6)
- [x] Dia 28 (Notebook Desaparecido — SQ1, ver 3.9)
- [ ] Implementar o conteúdo dos dias 32, 37, 48, 58, 68, 70, 85, 97 (cada um nasce como tarefa própria nas seções já existentes: 3.10 pro Dia 32, 3.3 pros dias de slot livre, 3.1/3.7 pros Dias 70 e 97, 3.11 pro Dia 100)
- [ ] Tela de resumo ao voltar de um time skip futuro, listando notícias de provas narrativas resolvidas automaticamente (só quando essa necessidade aparecer — nenhum dia da tabela acima ainda pula prova sem jogar)

**3.2 Save em disco (NOVO — decisão do time)**
- [x] `SaveSystem` estático (04/07/2026): `Save()` → `JsonUtility.ToJson` gravado em `Application.persistentDataPath + "/save.json"`; `Load()` → lê e popula `GameProgress`/`AcademicHud.stress`; `HasSave()`; `Delete()`. **Diferença do plano original:** não existe `GameProgress.Data` (DTO único) — o `SaveSystem` lê/escreve os campos estáticos direto, e a classe `SaveData` fica privada dentro do próprio `SaveSystem.cs`
- [x] Autosave no 1º ponto estável depois da Prova R1 (objetivo `notebook_prof`, Dia 28) — ainda **não** há autosave "no fim de cada arco" nem no menu de pausa (que ainda não existe, 3.15)
- [x] `TitleScreen`: tela de menu antes do nome/personagem, com **Novo Jogo** e **Continuar** (só aparece se `HasSave()`). **Simplificação:** "Novo Jogo" com save existente já apaga o save (aviso no hint da tela, não um modal de confirmação separado); `ResetRun()` não existe ainda (não faz falta: título só aparece uma vez, no início do processo — falta quando "Voltar ao Título" existir, 3.15)
- [x] Ao carregar: sem `ArcDirector` ainda, então retoma do **objetivo salvo exato** (`GameProgress.CurrentObjectiveId`) em vez do início de um arco — reaplica sala/gating via `QuestManager.ActivateObjective` e manda o jogador pro spawn do campus (`InteriorController.ForceCampus`)
- [ ] Testar: salvar no Dia 28, fechar o jogo, Continuar → estado íntegro (notas, flags, estresse, dia do semestre)

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
- [x] Matemática: cada rodada de prova (R1/R2/final) agora é **4 labirintos em sequência** (dificuldade crescente — o 1º é o corredor de sempre, os outros 3 são labirintos de verdade gerados por backtracking recursivo), 2,5 pontos cada, somando 0–10 (decisão de 04/07/2026, ver `MazeController.cs`/`TopDownSceneBuilder.GenerateMaze`)
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

> **Substituído (decisão de 04/07/2026):** implementado como **perseguição no próprio campus** em vez de runner em cena separada — ver `TroteChase.cs`. Natan, Enzo, Matheus e Vitim (os mesmos NPCs dos Dias 1–3, sem NPCs "veterano" dedicados ainda) saem de onde estavam e correm atrás do jogador assim que o Dia 4 começa; pego = cena de "sujaram de ovo" (+15 estresse, flag `trote_pego`, `trote_fedendo` faz qualquer NPC comentar o cheiro pelo resto do dia); escapar = sobreviver ~20s ou entrar em qualquer prédio (`trote_escapou`). Motivo: reaproveita infraestrutura existente (NPCs, `InteriorController`, `DialogueManager`) em vez de uma cena nova só pra isso, e não exige personagens "veterano" que ainda não existem. Os itens abaixo (scroll automático, Espaço/S/D, escolha Fugir/Negociar) ficam registrados como o design original, mas não é isso que está implementado.
- [x] Perseguição implementada (`TroteChase.cs`) — pego dá cena de ovo/sujeira + estresse + flag de cheiro; escapar por tempo ou entrando num prédio
- [ ] Cena nova; scroll automático; Espaço pula, S abaixa, D acelera (mapear no Input System existente)
  - ⚠️ **Lição do pingue-pongue (3.7B, primeira "cena nova" real do projeto):** `SceneManager.LoadScene` recarrega o `SampleScene` inteiro do zero ao voltar, o que reabre a `TitleScreen` (ela sempre se mostra no `Start()`) e perde qualquer estado que não seja `GameProgress`. Use o mesmo padrão de handoff estático (`PingPongSession.cs`) pra guardar o que precisa sobreviver à troca de cena e sinalizar "isso é um retorno, não um jogo novo" — lido no `Awake()` (restaura posição/câmera) e no `Start()` (pula a tela de título), nessa ordem.
- [ ] Chão via `OverlapCircle`; abaixar reduz o collider
- [ ] Veteranos como triggers → 3 capturas = pego (`SetFlag("trote_pego")`, +15 estresse, mini-cutscene cômica) | chegar ao fim = `SetFlag("trote_escapou")`
- [ ] Escolha pré-minigame: [A] Fugir | [B] Negociar → começa com 3s de vantagem (veteranos entram atrasados)

**3.7 Minigame 3 — Labirinto de Debug (Perseguição)**
- [ ] Parametrizar `MazeController` (disciplina alvo da nota, tilemap usado, perseguidor on/off) em vez de duplicar o script
- [ ] `PerseguidorAI` por waypoints (`MoveTowards` entre pontos pré-definidos; sem pathfinding real)
- [ ] Captura = +8s no cronômetro (não reinicia) e +5 de estresse
- [ ] R2/final: perseguidor mais rápido + labirinto maior
- [ ] Se `gabriel_ajudado`: antes do debug final, diálogo do Gabriel com a dica → −10s no tempo final

**3.7B Minigame 4 — Pingue-pongue com o Vitim (NOVO — expansão de escopo consciente, 01/07/2026)**
- [ ] Ao aceitar o convite do Vitim ("Bora, to dentro!") na mesa de ping pong da Convivência, os dois bonecos andam automaticamente para lados opostos da mesa e carrega a cena `PingPongMinigame` via `SceneManager` (padrão de cena própria dos minigames)
- [ ] Barra do jogador: só eixo vertical, W/S ou setas (Input System novo, `Keyboard.current`). Barra do Vitim: IA
- [ ] Bola com física simples 2D (`Rigidbody2D`, reflexão nas barras e nas bordas superior/inferior)
- [ ] Partida termina em 7 pontos **ou** vantagem de 4 (ex.: 4×0, 5×1...), o que vier primeiro
- [ ] IA do Vitim com dificuldade equilibrada (velocidade/erro variáveis) — deve ser possível ganhar e perder, não é nem perfeita nem trivial
- [ ] Entre um ponto e outro, fala aleatória do Vitim — pool de 10 variações ("Boa bola!", "Pra um calouro você joga bem", "Tu já jogava antes né, safado!", etc.)
- [ ] Ao fim da partida, volta para a Convivência (posição de retorno da mesa); resultado é só flavor — **não** afeta notas, estresse ou finais
- [ ] Ver cut-list (seção 2, item 1): se faltar tempo, é o primeiro corte — volta a ser só a linha de diálogo original

**3.8 Variações do labirinto de Matemática**
- [ ] 2 tilemaps por rodada (reduzido/padrão) ativados conforme `notebook_devolvido`
- [ ] Labirinto final: entrada secreta ativa se `notebook_devolvido` (atalho que economiza ~20s)
- [ ] Dica de atalho dos veteranos antes do labirinto final se (`trote_escapou` OU `trote_pego`) **E NÃO** `veterano_denunciado`

### FASE D — Conteúdo Narrativo e Mundo

**3.9 Side Quest 1 — Notebook Desaparecido** ✅ implementada (04/07/2026, Dia 28 do calendário — ver 3.1B)
- [x] 4 etapas em sequência no `QuestManager` (`notebook_prof` → `notebook_ru` → `notebook_lab` → `notebook_devolucao`), todas no mesmo dia — **substitui** as flags de etapa/expiração do plano original (decisão de 03/07/2026: sem expirar por visitas em dias diferentes)
- [x] Diálogos escritos do zero (a Narrativa §7.1 não está disponível neste repositório — se o texto oficial existir, comparar e substituir depois). Professor = Aragão (reaproveitado, falas alternativas via `NpcInteractable.ObjectiveLineSet`); atendente do RU = NPC novo `atendente_ru` (reaproveita o sprite da Yasmin — sem arte própria ainda)
- [x] Notebook = objeto interagível simples (`notebook_objeto`, sem folha de sprite — um quadrado colorido) no Bloco 2, Sala 2 (virou o "laboratório" — sala já existia, vazia, não foi preciso construir uma nova). **Simplificação:** sempre presente e interagível (como todo NPC do jogo), em vez de collider ativo só na etapa 3 — fora de ordem, falar com ele não faz nada (mesmo padrão de qualquer outro NPC de quest)
- [x] Devolver dá +1.0 Ética e a flag `notebook_devolvido` (mantém a consequência cruzada já prevista em 3.8 — entrada secreta no labirinto final)

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
- [ ] Dias (faz-tudo) em 2 pontos do mapa | convite da Calourada (transição Arco 1→2) | Cedro (slot especial fim do Arco 3, reset parcial de estresse)
- [ ] Mesa de ping pong: **promovida a minigame dedicado com o Vitim** (decisão do time em 01/07/2026, expansão de escopo consciente sobre a decisão original de "1 linha de diálogo") → ver 3.7B

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
- [x] Música tema em loop (`MusicPlayer.cs`), tocando desde a tela de título — `Assets/Audio/musica_tema.mp3` (04/07/2026). Ainda falta a trilha tensa de minigames e a emocional de finais
- [ ] SFX mínimos: passo, confirmação de diálogo, captura, coletável (opcional)
- [ ] Volume master controlado pelo slider da pausa (3.15) — por enquanto o volume é fixo (`MusicPlayer.volume`, sem UI)

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
