# Documento de Game Design
# Calouro.exe
## Sobrevivendo ao Primeiro Semestre

> **Versão atualizada em 04/07/2026** — reflete o que está realmente implementado no jogo até esta data, não o design original completo. O documento original (design de referência, nem tudo chegou a ser construído) está em `gdd.orignal.md`. O roadmap técnico detalhado, com o que ainda falta, vive em `roadmap-v2.md`.

**Equipe:**
- Natan Lucena - 563641
- Victor Veras Martins - 571603 (Turma 01)
- Emilly Paiva Belo - 563639 (Turma 01)
- Enzo Hariel - 566785
- Matheus Rodrigues - 563640

---

## Introdução

Calouro.exe é um RPG 2D top-down ambientado no campus da Universidade Federal do Ceará (UFC) em Quixadá. O jogador assume o papel de um calouro recém-chegado ao curso de Engenharia de Software e vive um recorte do primeiro semestre acadêmico.

O jogo mistura exploração do campus, interação com NPCs, minigames e escolhas de diálogo com consequências (principalmente na nota de Ética). O tom busca retratar com humor e certa nostalgia a experiência de ser calouro numa universidade federal.

Ao final do semestre, a média das 5 notas do jogador determina um de três desfechos: aprovação direta, avaliação final, ou reprovação.

## História

O jogador aparece já na passarela da Guarita, entrada do campus — é o primeiro dia do semestre. O Prof. Jeferson, coordenador do curso, sobe a passarela para recebê-lo, dá as boas-vindas e faz um tour guiado pelos pontos principais do campus (câmera com legendas, estilo cutscene) antes de indicar a primeira aula.

Ao longo do semestre, o jogador assiste às aulas das 5 disciplinas, ajuda (ou não) um colega em risco de reprovar, foge de um trote dos veteranos, e ajuda o professor de Matemática a recuperar um caderno perdido. No fim do semestre, o Jeferson revisa o desempenho do calouro nota a nota e anuncia o resultado.

## Personagens

### Personagem Principal

**O Calouro/A Caloura** — nome definido livremente pelo jogador na tela de título. Além do nome, o jogador escolhe entre duas aparências: **calouro** (homem) ou **caloura** (mulher) — a escolha define os sprites usados pelo personagem no jogo inteiro (e, de forma espelhada, a aparência do colega da side quest "Colega em Risco", ver abaixo).

### NPCs Principais

| NPC | Papel |
|---|---|
| **Jeferson** | Professor de Introdução à Engenharia de Software e coordenador do curso. Recebe o jogador na abertura, dá o quiz de IES, e reaparece no último dia do semestre para revisar as notas e anunciar o resultado. |
| **Rainara** | Professora de Interação Humano-Computador. |
| **Aragão** | Professor de Matemática Básica. Aplica a Prova-Labirinto. Protagonista da side quest do caderno perdido. |
| **Paulyne** | Professora de Fundamentos da Programação. Aplica o exercício de "montar a solução". |
| **Gabi** | Atendente do Restaurante Universitário (RU). Sabe de tudo que rola no campus — inclusive pistas sobre o caderno sumido do Aragão. |
| **Yasmin, Enzo, Matheus, Natan, Vitim, Emilly** | Colegas de turma do calouro. Aparecem em conversas de ambiente nos primeiros dias — e os quatro primeiros (Natan, Enzo, Matheus e Vitim) também são quem persegue o jogador no trote do Dia 4 (o jogo não tem "veteranos" com sprite/identidade própria — são os mesmos colegas, o trote é deles). Vitim ainda convida o jogador pra uma partida de pingue-pongue na Convivência. |
| **Gabriel / Gabriela** | Colega que pede ajuda pra estudar (side quest "Colega em Risco", Dia 32). Espelha o **gênero oposto** ao escolhido pelo jogador — apostando calouro(a) vira Gabriela/Gabriel usando a própria arte do personagem principal, não um NPC à parte. |

## Mundo — Campus de Quixadá

O jogador se move livremente por um único mapa contínuo (sem telas de carregamento entre áreas externas); interiores de prédios (salas, RU, Convivência) são regiões separadas carregadas ao entrar por uma porta.

| Área | Descrição |
|---|---|
| Blocos 1 a 4 | Prédios com salas de aula. Bloco 1 = IHC, Bloco 2 = Matemática (e um laboratório, cenário da side quest do caderno), Bloco 3 = Fundamentos da Programação, Bloco 4 = Introdução à Engenharia de Software. |
| Área de Convivência (AC) | Espaço social do campus — mesa de pingue-pongue do Vitim, e ponto de encontro usado em duas side quests (o Aragão se refugia lá durante a busca pelo caderno). |
| RU (Restaurante Universitário) | Prédio próprio, à parte dos Blocos — atendida pela Gabi. |
| Guarita | Entrada do campus. O jogador reaparece na passarela logo abaixo dela toda vez que um novo dia/salto de tempo começa. |
| Avenida e Estacionamento | Elementos visuais do entorno do campus, ao norte da Guarita. |

## Controles

| Controle | Descrição |
|---|---|
| WASD / Setas | Mover o personagem nas 4 direções (jogo top-down; não há pulo ou plataforma) |
| E | Interagir com NPC ou objeto próximo |
| 1 / 2 (ou 1-5 nas provas) | Responder escolhas de diálogo e provas de múltipla escolha/ordenação |
| ESC | Abrir/fechar a caderneta acadêmica (pausa o jogo enquanto aberta) |
| Mouse Esquerdo | Avançar diálogo (alternativa ao E) |

## Elementos do Jogo

### Caderneta Acadêmica

Menu de status do jogador, aberto com ESC. Mostra o nome do calouro, o dia atual do semestre ("Dia X de 100 · Semana Y de 18" — a semana é só uma referência derivada do dia, decorativa), o objetivo atual, e a nota (0–10, ou "—" se ainda não avaliada) em cada uma das 5 disciplinas. Um contador **"Faltam N dias pro fim do semestre"** fica sempre visível no canto superior direito da tela, fora da caderneta.

### Calendário do Semestre

O semestre tem uma linha do tempo de **100 dias**, mas nem todo dia é jogado: a maior parte do tempo passa em "saltos" narrados (tela preta com uma ou duas frases, tipo "Algumas semanas depois..."). Os dias efetivamente jogáveis hoje são:

| Dia | O que acontece |
|---|---|
| 1 | Aula de IHC (Rainara), aula de Matemática (Aragão), conversa social (Emilly), pingue-pongue com o Vitim |
| 2 | Aula de Fundamentos (Paulyne), conversa social (Yasmin), amizade com o Enzo |
| 3 | Ajudar o Matheus, estudar com o Natan no RU |
| 4 | Trote: Natan, Enzo, Matheus e Vitim perseguem o jogador pelo campus |
| 20 | Provas oficiais das 4 disciplinas com minigame (IHC, IES, Fundamentos, Matemática) |
| 28 | Side quest do caderno sumido do Aragão (as 4 etapas acontecem no mesmo dia) |
| 32 | Side quest "Colega em Risco" (Gabriel/Gabriela) |
| 100 | Revisão final de notas com o Jeferson e desfecho do semestre |

Entre um dia jogável e outro, o calendário salta direto (hoje o salto vai do Dia 32 pro Dia 100 — o conteúdo dos dias intermediários ainda está no roadmap, não implementado).

### Salvamento

Existe save em disco (1 slot). A tela de título oferece "Novo Jogo" ou "Continuar". O jogo salva automaticamente em pontos-chave da história (hoje: ao encontrar o Aragão na Convivência, no Dia 28).

### Disciplinas do Semestre

| Disciplina | Como a nota é definida |
|---|---|
| Fundamentos da Programação | Exercício "monte a solução": ordenar os passos corretos de um algoritmo. Nota = fração de passos na posição certa. |
| Interação Humano-Computador | Nota fixa de 8.0, concedida ao assistir a aula da Rainara — não há minigame próprio ainda. |
| Ética | Acumulada aos poucos ao longo do jogo, por escolhas de diálogo (com um teto de ganho por dia) e por marcos da história (ex.: devolver o caderno do Aragão). A escolha final da side quest do Gabriel/Gabriela é o último evento de Ética do jogo e pode fechar a nota em 10. |
| Matemática Básica | Minigame de Prova-Labirinto: 4 labirintos gerados proceduralmente em sequência, dificuldade crescente, valendo 2,5 pontos cada (soma até 10). |
| Introdução à Engenharia de Software | Quiz de 5 perguntas de múltipla escolha aplicado pelo Jeferson. |

### Interações e Consequências

- Devolver o caderno do Aragão: +1,0 de Ética.
- Conversas de escolha com NPCs de ambiente (ex.: Emilly): ganho de Ética, uma vez por NPC, respeitando o teto diário.
- Ajudar o Gabriel/Gabriela a estudar: dispara uma revisão geral cobrindo as 5 disciplinas (ver "Side Quests" abaixo) — sem custo de estresse (essa mecânica não existe no jogo).
- Recusar ajudar o Gabriel/Gabriela: sem penalidade imediata.

## Sistema de Pontuação

Cada disciplina tem uma nota de 0 a 10. No Dia 100, o Jeferson revisa as 5 notas e calcula a **média** delas para decidir o desfecho:

| Resultado | Condição |
|---|---|
| Aprovação Direta | Média das 5 notas ≥ 7,0 |
| Avaliação Final | Média entre 4,0 e 6,9 — o jogador faz uma bateria extra (quiz de IES, 2 labirintos de Matemática e o exercício de FUP, com conteúdo diferente do já visto) e precisa fechar média ≥ 6,0 nela pra ser aprovado |
| Reprovação | Média < 4,0, ou reprovação na própria Avaliação Final |

## Ações do Jogo

### Explorar o Campus

O jogador anda livremente entre aulas e conversas, sem tela de carregamento (exceto ao entrar/sair de prédios). Não há coletáveis nem eventos aleatórios de exploração ainda.

### Dialogar com NPCs

Cada NPC tem falas de apresentação na primeira conversa e falas variadas (sorteadas) nas conversas seguintes, pra não repetir a mesma introdução pra sempre. Alguns NPCs têm uma escolha de duas opções ao fim da conversa.

### Realizar Minigames/Provas

- **Prova-Labirinto** (Matemática): navegação top-down por um labirinto, cronômetro define a nota da rodada.
- **Quiz de IES**: pergunta de múltipla escolha, responde com as teclas numéricas.
- **Monte a Solução** (Fundamentos): escolher, na ordem certa, os passos de um algoritmo embaralhado.
- **Pingue-pongue com o Vitim** (opcional, flavor): minigame à parte, sem afetar notas — só uma partida contra a IA do Vitim, com recompensa de Ética por aceitar jogar.

### Gerenciar o Tempo

Não existe um sistema de slots de tempo livre (estudar/descansar/explorar) — a progressão hoje é linear, guiada por uma sequência fixa de objetivos que avança conforme o jogador cumpre cada etapa.

## Minigames

### Trote dos Veteranos (Dia 4)

Ao contrário de um "fuga em corredor automático", o trote acontece no próprio campus: os quatro colegas já conhecidos nos dias anteriores (Natan, Enzo, Matheus, Vitim) cercam o jogador e passam a persegui-lo em movimento livre (top-down).

| Elemento | Detalhes |
|---|---|
| Objetivo | Sobreviver por um tempo ou entrar em qualquer prédio |
| Se for pego | Cena de "sujaram você de ovo"; uma flag de cheiro faz qualquer NPC comentar sobre isso pelo resto do dia |
| Depois do trote | Salto de tempo direto pro Dia 20 (provas) |

### Prova-Labirinto (Matemática)

| Elemento | Detalhes |
|---|---|
| Tipo | Navegação 2D top-down por labirinto gerado proceduralmente |
| Rodadas | 4 labirintos em sequência (dificuldade/tamanho crescente), 2,5 pontos cada |
| Nota | Baseada no tempo de cada rodada (bom tempo = pontuação cheia daquela rodada) |
| Reaproveitado em | Avaliação Final do Dia 100 (versão reduzida, 2 labirintos) |

### Quiz de IES

| Elemento | Detalhes |
|---|---|
| Tipo | 5 perguntas de múltipla escolha (3 alternativas cada) |
| Controles | Teclas 1/2/3 |
| Nota | Proporção de acertos |

### Monte a Solução (Fundamentos)

| Elemento | Detalhes |
|---|---|
| Tipo | Ordenar os passos de um algoritmo simples, embaralhados na tela |
| Controles | Teclas numéricas, na ordem que o jogador acha correta |
| Nota | Proporção de passos que ficaram na posição certa |

## Side Quests

### O Caderno Desaparecido (Dia 28)

O professor Aragão perde um caderno com o material da turma. As etapas (todas no mesmo dia):
1. Falar com o Aragão, que está refugiado dentro da Convivência
2. Perguntar na Gabi, atendente do RU, se alguém viu o caderno
3. Achar o caderno no laboratório do Bloco 2
4. Devolver ao Aragão (de volta à sala dele) — concede +1,0 de Ética

Hoje essa é uma etapa obrigatória da história principal, não uma quest opcional que se possa ignorar.

### Colega em Risco (Dia 32)

Um colega — Gabriel ou Gabriela, dependendo do gênero escolhido pelo jogador (é sempre o oposto) — pede ajuda pra estudar antes de o semestre acabar.

- **Ajudar**: dispara uma revisão geral cobrindo as 5 disciplinas — um quiz de IES com perguntas novas, 2 labirintos de Matemática, uma pergunta de Ética (a última do jogo, pode fechar a nota em 10) e uma revisão do exercício de Fundamentos. As notas de revisão só melhoram a nota já existente, nunca derrubam.
- **Recusar**: sem consequência visível ainda.

Ao fim dessa side quest (aceitando ou recusando), o calendário salta direto pro Dia 100.

## O Fim do Semestre (Dia 100)

O Jeferson encontra o jogador de novo na passarela da Guarita — o mesmo lugar da abertura — e revisa as 5 notas uma a uma, comentando cada uma (de um empolgado "você é muito bom mesmo" a um "isso me preocupa um pouco...", dependendo da nota). Ao final, anuncia a média e o resultado:

- **Aprovado**: deseja sucesso ao jogador.
- **Avaliação Final** (aprovado ou reprovado nela): deseja boa sorte.
- **Reprovado**: pede mais dedicação da próxima vez.

Não há cutscene depois disso — o jogo termina ali mesmo, na conversa.

## Dificuldade

A dificuldade hoje é simples e linear:
- Os 4 labirintos de uma mesma prova crescem em dificuldade dentro da própria rodada.
- Não há penalidade por "estresse" (a mecânica foi removida do escopo) nem gestão de tempo/slots — o jogo não pune exploração ou decisões fora da sequência principal.

## Estilo Visual e Áudio

**Estilo visual:** pixel art 2D. Personagens usam folhas de sprite com poses de caminhada nas 4 direções; interiores "de perto" (Convivência, salas de aula, RU) mostram os personagens numa escala maior que no campus externo, pra compensar a arte mais "de perto" desses ambientes.

**Trilha sonora:** uma música tema única, em loop, tocando desde a tela de título e durante toda a exploração do campus. Ainda não há temas diferentes por área.

**Interface:** minimalista, construída inteiramente por código (sem TextMeshPro). Caderneta acessível pelo ESC; contador de dias restantes sempre visível no canto superior direito.

---

## Fora desta versão

Itens do design original que ainda não foram implementados (continuam no roadmap, `roadmap-v2.md`): minigame de debug de código para Fundamentos, denúncia de veterano excessivo, slots de tempo livre (estudar/descansar/explorar/café), coletáveis e eventos aleatórios de exploração, conteúdo dos Dias 37/48/58/68/70/85/97, menu de pausa completo (hoje só a caderneta), trilhas sonoras por área, e cutscenes de abertura ("chegada de ônibus") e de encerramento (avaliadas e cortadas do escopo em 04/07/2026 — o fim do jogo hoje é só a conversa final com o Jeferson).
