# 🎮 calouro.exe — Roadmap do MVP

RPG 2D top-down de sobrevivência acadêmica (UFC Quixadá). Baseado no GDD
*"Calouro.exe — Sobrevivendo ao Primeiro Semestre"*.

> **Como usar:** vamos **uma fase por vez**. Ao terminar, você confere no jogo e a gente marca os itens. Só avanço pra próxima fase depois do seu OK.

**Legenda:** ✅ feito · 🔜 próxima etapa · ⬜ a fazer · ❓ a definir · ⚠️ depende de asset que ainda não temos

---

## 🎯 Escopo do MVP — "O Primeiro Dia"

O jogo completo é enorme (18 semanas, 3 minigames, side quests, notas, cutscenes, 3 finais). O **MVP é uma fatia vertical** do início do arco **Chegada**, que prova o *loop* central do jogo:

**Explorar → conversar com NPC (com escolhas) → cumprir um objetivo → ver consequência.**

O que o MVP **inclui**:
- Campus **provisório** (blocos coloridos, sem pixel art ainda) com 2–3 áreas ligadas
- Personagem jogável (o Calouro) andando + câmera que segue
- **Sistema de diálogo** com opções de resposta
- NPCs: **Coordenador** (dá o objetivo), **Natan** (colega de turma) e mais 1
- **1 objetivo simples**: o Coordenador te orienta → você encontra/fala com o Natan → conclui a demo
- **1 minigame: Prova-Labirinto** (puzzle top-down com cronômetro → nota)
- **HUD** com barra de estresse + **Caderneta** (ESC) mostrando as 5 disciplinas (stub)
- Menu de título e tela de "Fim da demo"

O que fica **fora do MVP** (vai pro backlog): os outros 2 minigames (runner e debug), sistema de notas real, 18 semanas, side quests completas, cutscenes, escolhas com consequências de longo prazo, save/load, pixel art e áudio finais.

**Personagens do MVP (arte placeholder = sprites atuais):**
| Papel | Sprite | Status |
|------|--------|--------|
| Calouro (jogável) | `aragao` (provisório) | ✅ |
| Natan (colega/NPC) | `natan` | 🔜 |
| Coordenador (NPC) | `jeferson` | ⬜ |
| NPC extra (ex.: Atendente RU) | ❓ a escolher | ⬜ |

---

## Fase 0 — Fundações ✅ (concluída)
- [x] Projeto Unity 6 (URP 2D) + novo Input System
- [x] Importar os 11 personagens e fatiar em 12 poses (4×3)
- [x] Personagem controlável (WASD/setas) com Rigidbody2D e colisão
- [x] Animação de caminhada **direcional**
- [x] Ferramenta `Tools > Calouro > Montar Cena Top-Down`

## Fase 1 — Campus (amplo, mapa oficial UFC) + câmera ✅
- [x] **Câmera que segue o jogador** (com limites do mapa)
- [x] Campus amplo baseado no mapa oficial: Blocos 1–4 (001–004), RU/Adm (007), Convivência (005), Guarita (006), Depósitos (008/009), Avenida + estacionamento, Limite do Campus
- [x] Prédios com paredes e **portas**; rótulos de texto em cada área
- [x] Spawn na Convivência

## Fase 1.5 — Assets de interior (folhas em `Assets/Art/Campus`) 🔜
- [x] Importar + fatiar as 2 folhas (`Tools > Calouro > Fatiar Assets de Campus`): piso, parede, porta, janela, lousa, mesa do prof, carteira, cadeira, lixeira, ar-condicionado, relógio, extintor
- [x] Re-skin externo: prédios com **piso em tiles** reais, paredes-sprite e porta
- [x] Mobília nos blocos didáticos (lousa + mesa do prof + carteiras)
- [ ] ⬜ **Interiores enterráveis** (entrar na porta → sala montada com os tiles)
- [ ] ⬜ Ajustar perspectiva/escala das paredes verticais e ordenação (sorting por Y)
- [ ] ⚠️ Tiles de **área externa** (grama, asfalto, árvore, fachada) — ainda não temos

## Fase 2 — Sistema de diálogo 🔜 (em andamento)
- [x] Caixa de diálogo (UI) com nome do NPC + texto
- [x] Avançar falas (E / clique)
- [x] Dica "Aperte E para falar" ao chegar perto
- [x] Opções de resposta (escolha com 1/2)
- [ ] ⬜ Estrutura de dados de diálogo reutilizável (ScriptableObject ou JSON)

## Fase 3 — NPCs e interação 🔜 (em andamento)
- [x] Componente de NPC (sprite + gatilho de proximidade)
- [x] Tecla **E** para interagir (Input System) + travar movimento no diálogo
- [x] Colocar **Natan** no mapa (RU)
- [x] Colocar **Coordenador** (`jeferson`) no centro
- [ ] ⬜ 1 NPC extra
- [ ] ❓ Definir qual sprite é o NPC extra

## Fase 4 — Objetivo / loop do MVP ✅
- [x] Quest simples: Coordenador → falar com o Natan → chegar no Bloco 1
- [x] Objetivo atual mostrado na tela (HUD, canto superior esquerdo)
- [x] Condição de conclusão → tela **"Fim da demo"** (Enter reinicia)
- [x] Escolha na conversa do Natan que muda a fala final (consequência)

## Fase 4.5 — Minigame: Prova-Labirinto ✅
- [x] Área de labirinto (paredes provisórias) navegável top-down (região fora do campus)
- [x] Cronômetro visível
- [x] Chegar à saída → tempo vira **nota (0–10)** de Matemática
- [x] Voltar ao campus após concluir; nota aparece na Caderneta
- [x] Portal no RU (aperte E) para entrar na prova

## Fase 5 — HUD e caderneta ✅
- [x] **Barra de estresse** no canto superior direito (sobe com o tempo; a escolha do Natan altera)
- [x] **Caderneta** no **ESC**: 5 disciplinas + nota (stub "—") + semana + estresse
- [x] Pausa o jogo ao abrir a caderneta

## Fase 6 — Fluxo de telas ✅
- [x] Tela de título (Enter = Jogar / Esc = Sair), pausa o jogo até começar
- [x] Digitar o nome do calouro no início (aparece na caderneta)
- [~] Transições simples: título → jogo (fade/telas extras podem vir depois)

## Fase 7 — Polimento e build ⬜
- [ ] ⬜ Ajuste de colisões, velocidade e câmera
- [ ] ⬜ Ícone e nome da janela
- [ ] ⬜ **Build Windows (.exe)** e teste em máquina limpa
- [ ] ⬜ Corrigir bugs do teste

---

## 🧱 Pendências de assets (bloqueiam itens ⚠️)
- [ ] **Pixel art do campus** (tiles: chão, paredes, mobília, escadas)
- [ ] **Mais poses de costas** dos personagens (folha atual só tem 1 → "andar pra cima" sem ciclo real)
- [ ] Arte de UI (caixa de diálogo, caderneta, barra de estresse)
- [ ] **Áudio**: trilha lo-fi/chiptune + efeitos
- [ ] Arte das cutscenes (quadrinhos digitais)

## 🗒️ Backlog pós-MVP (do GDD, fora da entrega inicial)
- **Minigame 1 — Fuga do Trote** (runner lateral: pular/abaixar/acelerar)
- **Minigame 3 — Debug do Código** (arrastar blocos, corrigir bugs)
- _(Minigame 2 — Prova-Labirinto entrou no MVP, ver Fase 4.5)_
- Sistema de **notas** real (0–10 por disciplina) e 3 finais
- **18 semanas** com slots de tempo livre (estudar/descansar/ajudar)
- Side quests completas (Notebook Desaparecido, Colega em Risco)
- Escolhas com **consequências de longo prazo**
- **Cutscenes** (chegada, aula inaugural, 3 finais)
- Save/load, menu de configurações, temas de áudio por área

---

### ✅ Próximo passo sugerido
**Fase 1** — câmera seguindo o jogador + montar as áreas provisórias do campus.
Me dá o OK (e responda os ❓) que eu sigo por essa etapa e paro pra você conferir.
