GDD ANTIGO: # Documento de Game Design
# Calouro.exe
## Sobrevivendo ao Primeiro Semestre

**Equipe:**
- Enzo Hariel - 566785
- Natan Lucena - 563641
- Emilly Paiva Belo - 563639 (Turma 01)
- Victor Veras Martins - 571603 (Turma 01)
- Matheus Rodrigues - 563640

---

## Introdução

Calouro.exe é um RPG 2D de aventura e sobrevivência ambientado no campus da Universidade Federal do Ceará (UFC) em Quixadá. O jogador assume o papel de um calouro recém-chegado ao curso de Engenharia de Software e deve sobreviver ao primeiro semestre acadêmico.

O jogo mistura exploração de ambiente, interação com NPCs, minigames e escolhas com consequências reais na vida acadêmica do personagem. O tom é dramático e nostálgico, buscando retratar com autenticidade e certo humor a experiência real de ser calouro na universidade federal.

Ao final do semestre, o desempenho acadêmico acumulado pelo jogador determina um de três finais possíveis: aprovação direta, avaliação final (prova de recuperação), ou reprovação.

## História

Você acabou de chegar a Quixadá. Mala nas mãos, coração acelerado e um mapa impresso que claramente não bate com o campus real. É o primeiro dia do semestre.

O Prof. Coordenador de Engenharia de Software te recebe na aula inaugural de Introdução à Engenharia de Software. Ele conhece tudo sobre o campus e sobre sobreviver academicamente e vai aparecer ao longo do semestre como seu principal mentor.

Mas sobreviver não é só ir às aulas. Há veteranos planejando trotes, um professor de Matemática Básica que vive perdendo o notebook, colegas que precisam de ajuda para não reprovar, e a fila do RU que parece desafiar as leis da física. Cada decisão que você tomar ao longo do semestre vai moldar quem você se torna — e se você passa de ano.

## Personagens

### Personagem Principal

**O Calouro** — personagem jogável sem nome fixo (definido pelo jogador no início). Recém-chegado ao curso de Engenharia de Software, curioso, um pouco perdido, mas determinado a passar no primeiro semestre.

### NPCs Principais

| NPC | Descrição |
|---|---|
| Prof. Coordenador | Professor de Introdução à Engenharia de Software e coordenador do curso. Aparece regularmente ao longo do jogo como guia e mentor. Seu conselho influencia as escolhas do jogador. |
| Prof. de Matemática Básica | Professor desastrado que vive perdendo o notebook. Protagonista da side quest principal. Simpático mas confuso. |
| Veteranos | Grupo de alunos dos semestres anteriores. Aparecem no minigame de trote. Não são vilões — só querem se divertir. |
| Colegas de Turma | Vários alunos do primeiro semestre. Alguns pedem ajuda, outros dão dicas, alguns são apenas cenário. |
| Atendente do RU | Personagem do Restaurante Universitário. Conhece tudo que acontece no campus. |
| Alunos de IA | Calouros do novo curso de Inteligência Artificial. Ficam curiosos com a obra do Bloco 5 em construção. |

## Mundo — Campus de Quixadá

O campus é dividido em áreas navegáveis interconectadas. O jogador se move livremente entre elas durante o horário de aula e nos intervalos.

| Área | Descrição |
|---|---|
| Blocos 1 a 4 | Quatro blocos de dois andares com salas de aula e laboratórios. É onde acontecem as aulas e o minigame de prova-labirinto. |
| Área de Convivência | Espaço central do campus. Ponto de encontro de alunos, onde ocorrem muitas interações e eventos sociais. |
| Bloco Administrativo | Primeiro andar: Restaurante Universitário (RU). Segundo andar: Direção do campus. |
| Obra do Bloco 5 | Área em construção para o futuro curso de IA. Cercada e inacessível diretamente, mas visível e relevante narrativamente. |

## Controles

| Controle | Descrição |
|---|---|
| A / Seta Esquerda | Mover para a esquerda |
| D / Seta Direita | Mover para a direita |
| W / Seta Cima | Subir escadas / interagir com andares superiores |
| S / Seta Baixo | Descer escadas |
| Espaço | Pular (minigame de trote) |
| E | Interagir com NPC ou objeto |
| ESC | Menu de pausa / caderneta de notas |
| Mouse Esquerdo | Confirmar seleção em menus e diálogos |

## Elementos do Jogo

### Barra de Estresse

Substitui a barra de vida tradicional. Representa o nível de estresse acadêmico e social do calouro. Aumenta ao falhar em minigames, perder prazos ou tomar decisões ruins. Se chegar ao máximo, o personagem entra em colapso e perde o dia de aula, impactando as notas.

### Caderneta Acadêmica

Menu de status do jogador. Exibe as 5 disciplinas do semestre, nota atual em cada uma (0 a 10), nível de estresse atual e progresso no semestre (semanas 1 a 18).

### Disciplinas do Semestre

| Disciplina | Impacto no Jogo |
|---|---|
| Fundamentos da Programação | Minigame de debug de código. Nota influencia no final. |
| Interação Humano-Computador | Escolhas de diálogo e design nas interações com NPCs. |
| Ética | As escolhas morais ao longo do jogo afetam a nota. |
| Matemática Básica | Minigame de prova-labirinto. Side quest do notebook. |
| Intro. à Engenharia de Software | Guiada pelo Coordenador. Progresso narrativo principal. |

### Interações e Consequências

O jogo tem um sistema de escolhas pontuais: certas decisões desbloqueiam ou bloqueiam eventos futuros. Exemplos:

- Ajudar o colega a estudar: reduz o estresse dele
- Denunciar um veterano excessivo: muda diálogo com NPCs, mas pode criar tensão social
- Pular aula para descansar: reduz estresse, mas aumenta a dificuldade na próxima prova
- Encontrar o notebook do professor: desbloqueia benefício especial na prova de Matemática

## Sistema de Pontuação

Cada disciplina tem uma nota de 0 a 10, calculada a partir das ações do jogador ao longo do semestre:

- Minigames de prova: cada minigame concluído gera uma nota (tempo ou precisão viram pontuação)
- Side quests concluídas: bonificam notas das disciplinas relacionadas
- Faltas por colapso de estresse: penalizam notas de todas as disciplinas

Ao fim do semestre, a média de cada disciplina determina o desfecho:

| Resultado | Condição |
|---|---|
| Aprovação Direta | Média >= 7.0 em todas as disciplinas |
| Avaliação Final | Média entre 4.0 e 6.9 em uma ou mais disciplinas |
| Reprovação | Média < 4.0 em duas ou mais disciplinas |

## Ações do Jogo

### Explorar o Campus

O jogador se move pelo campus entre aulas, intervalos e momentos livres. Cada área tem NPCs com quem interagir, itens para coletar (apostilas, marmita do RU, dicas de estudo) e eventos que surgem aleatoriamente.

### Dialogar com NPCs

Interações com professores, colegas e funcionários abrem menus de diálogo com opções de resposta. As escolhas afetam notas, estresse e o desfecho de side quests.

### Realizar Minigames

Três minigames principais ligados ao progresso acadêmico. Cada um pode ser jogado múltiplas vezes ao longo do semestre (representando as avaliações da disciplina).

### Gerenciar o Tempo

O semestre tem 18 semanas. Cada semana o jogador tem slots de tempo livre que pode alocar: estudar, explorar, descansar, ajudar colegas ou fazer side quests. Escolhas bem feitas equilibram nota e estresse.

## Minigames

### Minigame 1 — Fuga do Trote (Runner)

**Contexto:** Na primeira semana, os veteranos organizam uma emboscada de trote na área de convivência.

**Mecânica:** O jogador deve correr pelo campus desviando de veteranos e obstáculos. Tela de rolagem lateral automática. O jogador usa pulo, abaixar e mudança de rota para fugir. Quanto mais longe chegar sem ser pego, melhor a recompensa (menos estresse, bônus social).

| Elemento | Detalhes |
|---|---|
| Tipo | Endless runner com fim definido (chegar ao Bloco 1 seguro) |
| Controles | Espaço para pular, S para abaixar, D para acelerar |
| Falha | Ser pego por 3 veteranos = cena de trote (embarrassing, aumenta estresse) |
| Recompensa | Chegar seguro = reputação de calouro esperto, bônus de energia |

### Minigame 2 — Prova Labirinto (Puzzle)

**Contexto:** Cada prova de Matemática Básica é representada como um labirinto a ser resolvido.

**Mecânica:** O jogador navega por um labirinto 2D visto de cima. O tempo de resolução vira a nota: resolver rápido = nota alta, resolver devagar = nota baixa. O labirinto cresce em complexidade a cada avaliação.

| Elemento | Detalhes |
|---|---|
| Tipo | Puzzle de navegação com cronômetro visível |
| Controles | Setas direcionais |
| Nota | Tempo de conclusão mapeado para nota de 0 a 10 |
| Bônus | Ter encontrado o notebook do professor reduz a complexidade do labirinto |

### Minigame 3 — Debug do Código (Puzzle)

**Contexto:** Avaliação de Fundamentos da Programação. O professor mostra um trecho de código na lousa com bugs propositais.

**Mecânica:** O jogador vê um código em tela com blocos visuais (linhas coloridas). Alguns blocos estão fora de ordem, trocados ou errados. O jogador deve identificar e corrigir os bugs antes do tempo acabar. Quanto mais rápido e preciso, maior a nota.

| Elemento | Detalhes |
|---|---|
| Tipo | Puzzle de identificação e correção com cronômetro |
| Controles | Mouse para clicar e arrastar blocos de código |
| Tipos de bug | Linha fora de ordem, variável errada, lógica invertida |
| Nota | Precisão (bugs corrigidos) x velocidade = nota final |

## Side Quests

### Side Quest — O Notebook Desaparecido

**Gatilho:** O Prof. de Matemática Básica aparece na área de convivência visivelmente desesperado antes da segunda avaliação.

**Descrição:** O professor perdeu seu notebook com todas as notas da turma e material de aula. O jogador precisa investigar o campus conversando com NPCs para descobrir onde o notebook está.

**Etapas:**
1. Conversar com o professor para entender o problema
2. Falar com a atendente do RU (viu o professor passar com o notebook)
3. Investigar o laboratório de informática do Bloco 2
4. Encontrar o notebook esquecido embaixo de uma mesa
5. Devolver ao professor

**Consequências:**
- Concluir a quest: professor reduz a complexidade do próximo labirinto + diálogo especial de gratidão
- Ignorar a quest: professor aparece na avaliação claramente estressado, labirinto fica mais difícil

### Side Quest — O Colega em Risco

**Gatilho:** Um colega de turma te procura na semana 6 visivelmente perdido com o conteúdo de Fundamentos da Programação.

**Descrição:** Seu colega Gabriel está com média baixa e pode reprovar. Ele pede ajuda para estudar antes da próxima avaliação.

**Decisão central:** O jogador escolhe entre ajudar o colega (gasta slot de tempo livre) ou focar nos próprios estudos.

**Consequências:**
- Ajudar: colega passa na disciplina, aparece mais tarde com uma dica valiosa para o minigame de debug. Seu próprio estresse aumenta levemente.
- Recusar: colega reprova, interações futuras ficam tensas. Nenhum custo imediato para você.

## Progressão do Semestre

O semestre é dividido em 4 arcos narrativos:

| Arco | Semanas | Eventos Principais |
|---|---|---|
| Chegada | 1 a 3 | Tutorial com o Coordenador, minigame do trote, conhecer o campus e primeiros colegas |
| Primeiras Provas | 4 a 7 | Primeiro labirinto, primeiro debug, gatilho da side quest do notebook |
| Virada do Semestre | 8 a 13 | Provas mais difíceis, side quest do colega em risco, gestão de tempo exigente |
| Reta Final | 14 a 18 | Última rodada de provas, consequências das escolhas, cutscene e desfecho final |

## Cutscenes

### Cutscene 1 — Chegada ao Campus
Abertura do jogo. O calouro chega de ônibus em Quixadá, olha para o campus, nervoso. Apresentação em quadrinhos digitais. Duração: 90 segundos.

### Cutscene 2 — Aula Inaugural
O Coordenador se apresenta para a turma. Tom inspirador mas realista: "Engenharia de Software não é só programar. É resolver problemas." Apresenta o semestre. Duração: 60 segundos.

### Cutscene 3 — Final: Aprovação Direta
O calouro olha a caderneta com médias acima de 7. Última cena: pegando o ônibus de volta para casa, olhando pela janela com um sorriso discreto. Tom nostálgico.

### Cutscene 4 — Final: Avaliação Final
O calouro olha a lista de pendências. Expressão tensa. O Coordenador aparece: "Ainda dá tempo. Não desiste." Cena de estudo noturno. Tom de superação.

### Cutscene 5 — Final: Reprovação
O calouro olha a caderneta em silêncio. Não tem narrador — só a música e o campus ao fundo. Uma última fala do Coordenador: "Um semestre não define uma carreira." Tom de melancolia honesta.

## Dificuldade

O jogo tem dificuldade única, calibrada para ser desafiadora mas justa. A dificuldade é regulada por:

- Complexidade dos labirintos aumentando a cada avaliação
- Número de bugs no minigame de debug crescendo ao longo do semestre
- Velocidade dos veteranos no runner sendo fixa (jogo de introdução)
- Gestão de slots de tempo livre ficando mais escassa na reta final
- Estresse acumulado não resetando entre semanas (pressão cresce organicamente)

## Estilo Visual e Áudio

**Estilo visual:** pixel art 2D com paleta de cores quentes e nostálgicas. Inspiração em jogos como Stardew Valley e Undertale no aspecto de personagens expressivos e cenários simples mas detalhados.

**Trilha sonora:** instrumental lo-fi e chiptune. Temas distintos para cada área do campus (RU mais animado, biblioteca mais calma, corredores de aula mais tensos nas vésperas de prova).

**Interface:** minimalista. Caderneta acadêmica acessível pelo ESC. Barra de estresse visível discretamente no canto da tela.