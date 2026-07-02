# Roadmap de Ajustes — História, Estrutura e Avaliações do Jogo

**Projeto:** Calouro.exe — Sobrevivendo ao Primeiro Semestre  
**Objetivo deste checklist:** organizar as mudanças de narrativa, objetivos, NPCs, aulas, progressão por dias, sistema de notas por disciplina, time skip e avaliações/minigames.

---

## 0. Ressalvas novas incorporadas

- [ ] A nota de **Ética** deve vir da relação do jogador com o campus, das side quests e das escolhas sociais/morais.
- [ ] A nota de **Matemática Básica** deve vir do minigame de **Labirinto**, com pelo menos **5 variações de labirinto** para a prova.
- [ ] A nota de **IHC** pode ser uma nota constante/narrativa: após o **time skip**, Rainara entrega a prova ao calouro.
- [ ] O jogo precisa deixar claro que a prova de IHC acontece depois de um salto temporal, não no mesmo dia.
- [ ] A nota de **FUP/Fundamentos da Programação** deve vir de algum minigame de **resolver problema**.
- [ ] A nota de **IES/Introdução à Engenharia de Software** deve vir de um **quiz aplicado por Jeferson**, que também é professor da disciplina.

---

## 1. Ajuste central do início do jogo

- [x] Reposicionar o coordenador **Jeferson** para o final da passarela, antes da Área de Convivência.
- [x] Alterar o gatilho inicial para que **Jeferson vá até o calouro**, e não o calouro até ele.
- [x] Criar uma pequena cena de aproximação: Jeferson percebe o calouro perdido e caminha até ele.
- [x] Travar temporariamente o movimento do jogador durante a abordagem inicial de Jeferson.
- [x] Criar diálogo inicial de boas-vindas explicando que o calouro chegou ao campus e precisa seguir sua primeira aula.
- [x] Após o diálogo, liberar o controle do jogador e iniciar o primeiro objetivo oficial.

---

## 2. Sistema sequencial de objetivos

- [x] Implementar um sistema de objetivos em sequência, onde um objetivo só aparece depois que o anterior for concluído.
- [~] Criar uma estrutura para cada objetivo contendo: `id`, `titulo`, `descricao`, `npc_origem`, `local_destino`, `condicao_de_conclusao`, `proximo_objetivo` e `recompensa`. (núcleo: id, titulo, condição, local_destino, próximo; falta descricao/npc_origem/recompensa)
- [x] Exibir o objetivo atual na HUD ou na caderneta do jogador.
- [x] Impedir que objetivos futuros sejam ativados fora de ordem.
- [ ] Criar marcadores simples no mapa ou no cenário indicando o local do próximo objetivo.
- [x] Criar feedback visual/sonoro quando um objetivo for concluído. (visual; som fica pra depois)
- [x] Salvar o progresso do objetivo atual para evitar perda de avanço ao trocar de cena ou reiniciar o jogo.
- [ ] Adicionar mensagens curtas de orientação quando o jogador tentar interagir com algo fora da sequência.

---

## 3. Dia 1 — Fluxo principal

### 3.1 Encontro com Jeferson

- [x] Iniciar o Dia 1 com o calouro chegando ao campus.
- [x] Posicionar o calouro próximo à entrada/passarela.
- [x] Posicionar Jeferson no final da passarela, antes da Área de Convivência.
- [x] Fazer Jeferson se aproximar automaticamente do calouro.
- [x] Criar diálogo de Jeferson indicando o local da primeira aula.
- [x] Definir o primeiro objetivo: **Ir para a aula de IHC**.

### 3.2 Primeira aula — IHC

- [x] Definir bloco e sala da aula de IHC.
- [x] Criar marcador/entrada da sala correta.
- [x] Criar condição de conclusão: o jogador precisa entrar na sala correta.
- [x] Criar task: **Assistir aula de IHC**.
- [~] Criar pequena cena ou interação representando a aula acontecendo. (diálogo da Rainara faz as vezes da aula; cena elaborada fica pra depois)
- [x] Ao concluir a aula, atualizar a caderneta ou o progresso do dia.
- [x] Liberar o próximo objetivo somente após a aula de IHC terminar.

### 3.3 Rainara indica a próxima aula

- [~] Posicionar **Rainara** próximo à saída da sala de IHC ou em um ponto lógico do corredor. (está na sala da aula)
- [x] Criar diálogo em que Rainara orienta o calouro sobre a próxima aula.
- [x] Rainara deve indicar a aula com o **professor Aragão** em outro bloco e outra sala.
- [x] Definir o objetivo seguinte: **Ir para a aula do professor Aragão**.
- [ ] Atualizar marcador de destino para o novo bloco/sala. (marcadores no mapa = etapa de polimento)

### 3.4 Segunda aula — Professor Aragão

- [x] Definir bloco e sala da aula do professor Aragão.
- [x] Criar validação para garantir que o jogador entrou na sala correta.
- [x] Criar task: **Assistir aula do professor Aragão**.
- [x] Criar uma cena curta da aula ou diálogo inicial do professor.
- [x] Concluir o objetivo ao terminar a aula.
- [x] Atualizar progresso do Dia 1.

### 3.5 Interação final do Dia 1

- [x] Criar objetivo após a aula: **Interagir com alguém no campus**.
- [x] Definir qual NPC pode ser usado nessa primeira interação obrigatória. (Emilly, no deck da Convivência)
- [x] Criar diálogo com escolha moral ou social ligada à disciplina de Ética.
- [x] Fazer essa interação gerar pontos para a cadeira de Ética.
- [x] Exibir feedback de ganho de pontos em Ética.
- [~] Encerrar o Dia 1 após a interação obrigatória. (objetivo encerra; transição formal de dia/time skip vem na Etapa 5)

---

## 4. Sistema de pontos da disciplina de Ética

- [x] Criar variável de nota/progresso da cadeira de **Ética**, indo de 0 a 10.
- [x] Definir que interações sociais e escolhas morais aumentam ou deixam de aumentar a nota de Ética.
- [x] Fazer a nota de Ética crescer aos poucos com o passar das interações.
- [x] Definir quantos pontos cada interação dá, por exemplo: `+0.5`, `+1.0` ou `+2.0`.
- [x] Evitar que o jogador consiga nota 10 em Ética logo no primeiro dia.
- [x] Criar limite por dia ou por arco para o ganho máximo de Ética. (`EthicsDailyCap` = 2.0/dia)
- [x] Adicionar a nota de Ética na caderneta acadêmica.
- [~] Criar feedback narrativo quando o jogador fizer uma escolha ética importante. (aviso "Ética +X" + resposta do NPC; narrativa mais rica depois)
- [ ] Criar consequências futuras para decisões antiéticas ou egoístas. (etapa de expansão)
- [x] Garantir que a nota 10 em Ética seja formada pelo acúmulo de várias interações ao longo dos dias.

---

## 4.1 Sistema de notas por disciplina

### Ética — relação com campus e side quests

- [x] Definir que **Ética** não terá prova tradicional no início.
- [x] Fazer a nota de Ética crescer pela forma como o jogador se relaciona com o campus.
- [x] Pontuar escolhas em side quests, interações sociais, ajuda a colegas e decisões morais.
- [x] Usar a relação com NPCs e campus como base para a nota, não um minigame isolado.
- [~] Registrar bônus de Ética ao concluir side quests de forma positiva. (interações já pontuam; side quests dedicadas depois)
- [x] Registrar ausência de bônus ou pequenas penalidades em decisões egoístas/antiéticas.
- [x] Garantir que a nota de Ética só chegue perto de 10 com várias ações ao longo dos dias/arcos.

### Matemática Básica — labirinto

- [x] Definir que **Matemática Básica** será avaliada pelo minigame de **Labirinto**.
- [ ] Criar pelo menos **5 labirintos diferentes** para a prova. (hoje há 1 layout; variações ficam como melhoria)
- [ ] Sortear ou selecionar uma variação de labirinto quando a prova começar. (depende das 5 variações)
- [x] Fazer o tempo de conclusão virar nota de 0 a 10.
- [ ] Permitir que side quests relacionadas à Matemática reduzam dificuldade ou adicionem bônus de tempo. (quando as side quests entrarem)

### IHC — nota constante após time skip

- [x] Definir que **IHC** pode receber uma nota constante/narrativa.
- [x] Após o time skip, criar cena em que **Rainara entrega a prova de IHC** ao calouro.
- [x] Deixar explícito na tela que houve salto temporal: **Algumas semanas depois...**.
- [x] Evitar que pareça que Rainara entregou a prova imediatamente depois da aula do Dia 1.
- [x] Registrar a nota de IHC diretamente na caderneta após a cena.
- [x] Definir o valor da nota constante, por exemplo: `7.0`, `8.0` ou outro valor escolhido pela equipe. (8.0)

### FUP/Fundamentos da Programação — minigame de resolver problema

- [x] Definir que **FUP** será avaliada por um minigame de **resolver problema**.
- [x] Criar desafios curtos de lógica/programação, sem precisar ser código textual complexo.
- [x] Fazer o jogador montar uma solução com passos, blocos, alternativas ou lógica sequencial.
- [~] Calcular nota por acertos, tempo e quantidade de tentativas. (por acertos na ordem dos passos)
- [x] Registrar a nota de FUP na caderneta ao final do minigame.

### IES/Introdução à Engenharia de Software — quiz do Jeferson

- [x] Definir que **Jeferson** também é o professor de **Introdução à Engenharia de Software**.
- [x] Criar um **quiz do Jeferson** para avaliar IES.
- [x] Fazer o quiz ter perguntas sobre conceitos básicos de Engenharia de Software.
- [x] Calcular nota por quantidade de respostas corretas.
- [x] Registrar a nota de IES na caderneta após o quiz.
- [x] Usar o quiz também como reforço narrativo do papel de mentor do Jeferson.

---

## 5. Estrutura de dias antes do primeiro time skip

### Dia 1

- [x] Encontro automático com Jeferson.
- [x] Aula de IHC.
- [x] Rainara indica a próxima aula.
- [x] Aula com professor Aragão.
- [x] Primeira interação social valendo pontos de Ética.
- [x] Encerramento do dia. (transição "Dia 1 finalizado → Boa sorte no Dia 2")

### Dia 2

- [x] Criar nova rotina de chegada ao campus. (reaparece na passarela da Guarita)
- [x] Criar aula com **Paullyne/Paulete**.
- [x] Definir bloco e sala da aula de Paullyne/Paulete. (Bloco 3, Sala 1 — FUP)
- [x] Criar task: **Assistir aula de Paullyne/Paulete**.
- [x] Criar uma interação social ou acadêmica depois da aula. (Yasmin, no corredor do Bloco 3)
- [x] Fazer a interação do Dia 2 também impactar a nota de Ética.
- [x] Criar possibilidade de pequena escolha: ajudar colega, ignorar colega ou buscar informação.
- [~] Encerrar o Dia 2 após cumprir a sequência obrigatória. (encerra; a transição Dia 2→Dia 3 entra junto com o Dia 3)

### Dia 3

- [x] Criar mais uma sequência curta de aula + interação. (sessão de estudo no RU, véspera das provas)
- [x] Definir professor/personagem do Dia 3. (Natan)
- [~] Definir bloco e sala da aula do Dia 3. (é sessão de estudo no RU, não sala de aula)
- [x] Criar task: **Assistir aula do Dia 3**. (estudar com o Natan)
- [x] Criar interação obrigatória com NPC após a aula.
- [x] Fazer essa interação continuar construindo a nota de Ética.
- [x] Preparar narrativa para avisar que a primeira prova do semestre está chegando.
- [x] Encerrar o Dia 3 com gancho para o time skip.

---

## 6. Time skip após 3 dias

- [x] Criar evento de transição após o fim do Dia 3.
- [x] Exibir mensagem ou cutscene curta: **Alguns dias depois... primeira prova do semestre**. ("Algumas semanas depois… / Período de primeiras provas!")
- [x] Atualizar calendário/progresso do semestre. (semana +5)
- [x] Levar o jogador para o dia da primeira prova. (objetivo "Realizar as primeiras avaliações")
- [x] Manter notas, estresse, objetivos concluídos e escolhas feitas antes do time skip. (tudo em GameProgress)
- [x] Garantir que o time skip só aconteça após os três dias obrigatórios serem concluídos.
- [ ] Criar diálogo ou aviso na caderneta indicando a sala da prova. (entra com as provas, na Etapa 6)

---

## 7. Bloco de avaliações após o time skip

- [x] Criar evento de retorno após o time skip com texto claro: **Algumas semanas depois... período de primeiras provas**.
- [x] Atualizar calendário/progresso do semestre antes de liberar o jogador.
- [~] Mostrar na caderneta quais avaliações estão disponíveis. (a HUD mostra a prova atual da sequência)
- [x] Criar objetivo principal: **Realizar as primeiras avaliações do semestre**. (cadeia prova_ihc→ies→fup→mat)
- [x] Separar as avaliações por disciplina, evitando usar o mesmo minigame para tudo.
- [x] Manter as notas, estresse, side quests e escolhas feitas antes do time skip.
- [x] Impedir que uma avaliação comece se o jogador estiver no local errado, quando houver local específico. (gating de sala)
- [x] Registrar cada nota individualmente na caderneta acadêmica.

### 7.1 Avaliação de IHC — Rainara entrega a prova

- [ ] Depois do time skip, posicionar Rainara em um local lógico do campus ou corredor.
- [ ] Criar diálogo deixando claro que já passou um tempo desde as primeiras aulas.
- [ ] Rainara deve entregar a prova de IHC ao calouro.
- [ ] Deixar explícito que isso é consequência do avanço temporal, não continuação direta do Dia 1.
- [ ] Aplicar a nota constante definida para IHC.
- [ ] Atualizar a caderneta com a nota de IHC.
- [ ] Criar feedback simples, por exemplo: **Nota de IHC registrada na caderneta**.

### 7.2 Avaliação de Matemática Básica — Labirinto

- [ ] Definir a sala/bloco da prova de Matemática Básica.
- [ ] Criar objetivo: **Ir até a sala da prova de Matemática Básica**.
- [ ] Iniciar o minigame de labirinto ao interagir com a porta/professor/mesa da prova.
- [ ] Gerar ou selecionar uma entre **5 variações de labirinto**.
- [ ] Calcular a nota pelo tempo de conclusão.
- [ ] Aplicar bônus ou redução de dificuldade se a side quest relacionada ao professor/notebook estiver concluída.
- [ ] Registrar a nota de Matemática Básica na caderneta.

### 7.3 Avaliação de FUP — Resolver problema

- [ ] Definir a sala/bloco da prova de FUP.
- [ ] Criar objetivo: **Ir até a sala da avaliação de FUP**.
- [ ] Criar minigame de resolver problema com lógica de programação.
- [ ] Permitir que o jogador organize passos para chegar à solução.
- [ ] Validar respostas corretas, parcialmente corretas e erradas.
- [ ] Calcular nota por acertos, tempo e tentativas.
- [ ] Registrar a nota de FUP na caderneta.

### 7.4 Avaliação de IES — Quiz do Jeferson

- [ ] Posicionar Jeferson na sala ou ponto da avaliação de IES.
- [ ] Criar diálogo em que Jeferson explica que ele também é professor de Introdução à Engenharia de Software.
- [ ] Criar quiz com perguntas objetivas.
- [ ] Mostrar uma pergunta por vez, com alternativas.
- [ ] Calcular nota pela quantidade de acertos.
- [ ] Registrar a nota de IES na caderneta.
- [ ] Encerrar com fala curta de Jeferson comentando o desempenho do calouro.

### 7.5 Ética — nota por trajetória

- [ ] Não tratar Ética como prova única nesse primeiro bloco.
- [ ] Calcular Ética com base nas interações acumuladas antes e depois do time skip.
- [ ] Considerar side quests concluídas, ajuda a colegas, decisões morais e relação com NPCs.
- [ ] Atualizar a nota de Ética ao longo da trajetória, não em um único momento.
- [ ] Mostrar na caderneta que Ética está ligada à relação do jogador com o campus.

---

## 8. Minigames e avaliações específicas

### 8.1 Matemática Básica — Labirinto com 5 variações

- [ ] Criar **Labirinto 1 — Tutorial de prova**, curto, com poucos caminhos falsos.
- [ ] Criar **Labirinto 2 — Bifurcações**, com escolhas de caminho e alguns becos sem saída.
- [ ] Criar **Labirinto 3 — Corredores longos**, exigindo memorização e controle de tempo.
- [ ] Criar **Labirinto 4 — Atalhos e armadilhas**, com caminhos rápidos, mas arriscados.
- [ ] Criar **Labirinto 5 — Prova difícil**, maior, com mais becos sem saída e pressão de tempo.
- [ ] Definir se os labirintos serão sorteados ou escolhidos conforme dificuldade/progresso.
- [ ] Adicionar cronômetro visível em todos os labirintos.
- [ ] Criar tela de resultado ao concluir o labirinto.
- [ ] Mapear tempo para nota de 0 a 10.
- [ ] Voltar o jogador para a sala após o fim do minigame.
- [ ] Salvar nota na disciplina de Matemática Básica.

### 8.2 FUP — Minigame de resolver problema

- [ ] Criar minigame baseado em problema de lógica/programação.
- [ ] Apresentar um enunciado curto e objetivo.
- [ ] Permitir que o jogador organize passos da solução em ordem correta.
- [ ] Adicionar alternativas de decisão, blocos de algoritmo ou peças de lógica.
- [ ] Dar feedback quando a solução estiver incompleta ou errada.
- [ ] Calcular nota com base em acertos, tempo e tentativas.
- [ ] Criar pelo menos 3 problemas diferentes para evitar repetição.
- [ ] Salvar nota na disciplina de FUP/Fundamentos da Programação.

### 8.3 IES — Quiz do Jeferson

- [ ] Criar banco inicial com perguntas de Introdução à Engenharia de Software.
- [ ] Usar perguntas sobre requisitos, projeto, testes, manutenção, equipe e processo de software.
- [ ] Criar alternativas com apenas uma resposta correta por questão.
- [ ] Calcular a nota proporcionalmente aos acertos.
- [ ] Mostrar feedback simples ao final do quiz.
- [ ] Salvar nota na disciplina de IES.

### 8.4 IHC — Prova narrativa entregue por Rainara

- [ ] Criar cena pós-time skip com Rainara entregando a prova.
- [ ] Exibir texto de transição antes da cena para deixar o salto temporal claro.
- [ ] Aplicar nota constante definida pela equipe.
- [ ] Salvar nota em IHC.
- [ ] Evitar minigame obrigatório para IHC nessa primeira versão.

---

## 9. Ajustes de NPCs e diálogos

- [ ] Criar versão narrativa definitiva do coordenador **Jeferson** como mentor inicial.
- [ ] Escrever diálogo de Jeferson para orientar a primeira aula.
- [ ] Escrever diálogo de Jeferson como professor de IES e aplicador do quiz.
- [ ] Escrever diálogo de Rainara indicando a aula do professor Aragão.
- [ ] Escrever diálogo pós-time skip em que Rainara entrega a prova de IHC.
- [ ] Escrever fala inicial do professor Aragão.
- [ ] Escrever fala inicial de Paullyne/Paulete para o Dia 2.
- [ ] Criar pelo menos 1 NPC de interação ética no Dia 1.
- [ ] Criar pelo menos 1 NPC de interação ética no Dia 2.
- [ ] Criar pelo menos 1 NPC de interação ética no Dia 3.
- [ ] Garantir que os diálogos tenham tom universitário, simples e natural.
- [ ] Evitar diálogos longos demais durante o tutorial.

---

## 10. Ajustes de mapa e salas

- [ ] Confirmar visualmente onde fica a passarela inicial.
- [ ] Marcar o ponto exato onde Jeferson começa parado.
- [ ] Marcar o ponto onde o calouro começa.
- [ ] Criar ou revisar a entrada da Área de Convivência.
- [ ] Definir os blocos usados nas aulas dos três primeiros dias.
- [ ] Definir as salas usadas em cada aula.
- [ ] Criar identificação simples das salas para o jogador não se perder totalmente.
- [ ] Criar colisões corretas em portas, corredores e passarela.
- [ ] Garantir que o caminho até cada sala seja possível e claro.
- [ ] Bloquear áreas que ainda não devem ser acessadas na sequência inicial, se necessário.

---

## 11. Interface e caderneta

- [x] Criar campo de objetivo atual na HUD.
- [x] Criar aba ou seção de objetivos na caderneta.
- [x] Mostrar disciplinas e notas atuais na caderneta.
- [x] Mostrar progresso de Ética de 0 a 10.
- [x] Mostrar notas separadas de IHC, FUP, IES, Matemática Básica e Ética.
- [x] Mostrar dia atual ou etapa narrativa atual.
- [x] Mostrar mensagem de objetivo concluído.
- [x] Mostrar mensagem de novo objetivo recebido.
- [x] Atualizar a caderneta automaticamente após aulas, interações e provas.

---

## 12. Regras de progressão e bloqueios

- [ ] O jogador só pode assistir à aula de IHC depois de falar com Jeferson.
- [ ] Rainara só deve orientar o jogador depois da aula de IHC concluída.
- [ ] A aula do professor Aragão só deve ser concluída depois da orientação da Rainara.
- [ ] A interação ética final do Dia 1 só deve aparecer depois da aula do professor Aragão.
- [ ] O Dia 2 só começa após o encerramento do Dia 1.
- [ ] O Dia 3 só começa após o encerramento do Dia 2.
- [ ] O time skip só acontece depois do Dia 3.
- [ ] A primeira prova só inicia se o jogador estiver na sala correta.
- [ ] A nota da prova só deve ser registrada depois que o minigame terminar.
- [ ] A nota de IHC só deve aparecer após a cena pós-time skip com Rainara.
- [ ] A nota de IES só deve aparecer depois do quiz do Jeferson.
- [ ] A nota de FUP só deve aparecer depois do minigame de resolver problema.
- [ ] A nota de Matemática Básica só deve aparecer depois do labirinto.

---

## 13. Testes necessários

- [ ] Testar se Jeferson realmente anda até o calouro no início.
- [ ] Testar se o jogador não consegue pular objetivos da sequência.
- [ ] Testar se o objetivo atual aparece corretamente na HUD/caderneta.
- [ ] Testar conclusão da aula de IHC.
- [ ] Testar ativação da Rainara após a aula de IHC.
- [ ] Testar conclusão da aula do professor Aragão.
- [ ] Testar ganho de pontos de Ética após interação obrigatória.
- [ ] Testar transição do Dia 1 para o Dia 2.
- [ ] Testar transição do Dia 2 para o Dia 3.
- [ ] Testar time skip após o Dia 3.
- [ ] Testar entrada na sala da primeira prova.
- [ ] Testar início e conclusão do minigame de labirinto.
- [ ] Testar as 5 variações de labirinto de Matemática Básica.
- [ ] Testar cena pós-time skip da Rainara entregando a prova de IHC.
- [ ] Testar registro da nota constante de IHC.
- [ ] Testar quiz do Jeferson para IES.
- [ ] Testar minigame de resolver problema para FUP.
- [ ] Testar cálculo e registro da nota da prova.
- [ ] Testar salvamento de progresso depois de cada objetivo.

---

## 14. Prioridades sugeridas para implementação

### Prioridade Alta — MVP da nova estrutura

- [ ] Sistema sequencial de objetivos.
- [ ] Jeferson se aproximando do calouro no início.
- [ ] Dia 1 completo: Jeferson → IHC → Rainara → Aragão → interação de Ética.
- [ ] Caderneta com objetivo atual e nota de Ética.
- [ ] Time skip após 3 dias.
- [ ] Primeira prova iniciando apenas na sala correta.
- [ ] Sistema de notas por disciplina: Ética, Matemática, IHC, FUP e IES.
- [ ] Quiz do Jeferson para IES.
- [ ] Cena da Rainara entregando prova de IHC depois do time skip.

### Prioridade Média — Conteúdo e polimento

- [ ] Dia 2 com Paullyne/Paulete.
- [ ] Dia 3 com nova aula e nova interação.
- [ ] Diálogos mais naturais e revisados.
- [ ] Feedback visual de objetivo concluído.
- [ ] Marcadores de destino no mapa.
- [ ] Sistema de pontuação detalhado para Ética.

### Prioridade Baixa — Expansão futura

- [ ] Consequências narrativas das escolhas de Ética.
- [ ] Mais NPCs opcionais no campus.
- [ ] 5 variações do labirinto de Matemática Básica.
- [ ] Minigame de resolver problema para FUP.
- [ ] Variações do labirinto para provas futuras.
- [ ] Cutscenes entre dias.
- [ ] Reações diferentes dos NPCs conforme a nota de Ética.

---

## 15. Critério de pronto da nova versão

- [ ] O jogador entende claramente qual é o próximo objetivo.
- [ ] A sequência narrativa não quebra mesmo se o jogador tentar explorar antes da hora.
- [ ] Jeferson guia o início do jogo sem depender do jogador encontrá-lo.
- [ ] O Dia 1 tem começo, meio e fim bem definidos.
- [ ] A nota de Ética começa a ser construída por interações.
- [ ] Após 3 dias, o jogo avança naturalmente para a primeira prova.
- [ ] A primeira prova só acontece na sala correta.
- [ ] O minigame da prova gera nota e atualiza a caderneta.
- [ ] IHC fica claramente ligada ao time skip e à entrega da prova por Rainara.
- [ ] Matemática Básica usa labirinto com variações.
- [ ] FUP usa minigame de resolver problema.
- [ ] IES usa quiz aplicado por Jeferson.
- [ ] A estrutura criada permite adicionar novos dias, aulas, NPCs e provas sem refazer tudo.
