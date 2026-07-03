# Alterações — Sessão 02/07/2026

Sumarização completa das últimas alterações no **Calouro.exe**. Duas levas de trabalho, cada uma em um commit:

| Commit | Área | Resumo |
|--------|------|--------|
| `e2046ec` | progressão / balanceamento | Mais missões por dia, ping-pong virou missão, ética dobrada (+ lote acumulado da sessão anterior) |
| `b5aafbc` | cenário / arte | Interior novo do RU, morro "UFC" no estacionamento, departamentos substituindo placeholders |

---

## 1. Mais missões e mais pontos de Ética (commit `e2046ec`)

### Problema
Cada dia tinha **apenas 1** interação de ética (Emilly no D1, Yasmin no D2, Natan no D3), acumulando no máximo ~3.0 no total — mesmo o teto diário sendo 2.0/dia. O ping-pong com o Vitim existia, mas **não dava recompensa nenhuma** (só passatempo solto).

### Solução
Cada dia passou a ter **2 interações de ética**, e o ping-pong virou uma missão que dá pontos:

| Dia | Missões de ética (antes → agora) |
|-----|----------------------------------|
| **Dia 1** | Emilly (+1.0) → **jogar ping-pong com o Vitim** (+1.0 ética **+ alívio de estresse**, encerra o dia) |
| **Dia 2** | Yasmin (+1.0) → **conversar com o Enzo** no Bloco 4 (+1.0, encerra o dia) |
| **Dia 3** | **ajudar o Matheus** no campus (+1.0) → estudar com o Natan (+1.0, time skip) |

**Ética máxima possível: 3.0 → 6.0** (respeitando o teto de 2.0/dia).

### Nova cadeia de objetivos (`QuestManager`)
```
Dia 1: ir_aula_ihc → assistir_ihc → ir_aula_aragao → assistir_aragao
       → interacao_etica (Emilly) → jogar_pingpong (Vitim, dayEnd)
Dia 2: ir_aula_fup → assistir_fup → interacao_etica_d2 (Yasmin)
       → socializar_enzo (Enzo, dayEnd)
Dia 3: ajudar_matheus (Matheus) → estudar_natan (Natan, timeSkip)
Provas: prova_ihc → prova_ies → prova_fup → prova_mat
```
Objetivos novos: `jogar_pingpong`, `socializar_enzo`, `ajudar_matheus`.

### Prêmio do ping-pong (cross-scene)
O ping-pong roda em cena separada (`PingPongMinigame`), que recarrega a `SampleScene` ao terminar. O prêmio é entregue no **retorno**:

1. `PingPongGameController.EndMatch` marca `PingPongSession.MatchPlayed = true` (+ `PlayerWon`) antes do `LoadScene`.
2. `QuestManager.Start()` (roda depois de todos os `Awake`, pra o `AcademicHud` já existir) lê o flag:
   - Concede **+1.0 de Ética** (com teto diário, guardado pela flag `pingpong_jogado`).
   - **−8 de estresse** (`AcademicHud.AddStress(-8f)`).
   - Conclui a missão `jogar_pingpong` (`ForceComplete`), se for a atual → dispara o fim do Dia 1.
3. Ganhar ou perder a partida **não muda** o prêmio — jogar já conta como interação social.

O ping-pong é um gate (como as outras missões): se recusar o Vitim, é só voltar depois.

### Enzo e Matheus ganharam escolha ética
- **Enzo** (Bloco 4, id `enzo`): agora pede anotações emprestadas — ajudar dá `ethicsRewardA: 1.0f`.
- **Matheus** (campus, id `aluno_matheus`): pede ajuda num exercício de revisão — ajudar dá `ethicsRewardA: 1.0f`.

### Arquivos alterados
| Arquivo | Mudança |
|---------|---------|
| `Assets/Scripts/QuestManager.cs` | Nova cadeia de objetivos + `Start()` com o prêmio do ping-pong |
| `Assets/Scripts/PingPongGameController.cs` | Marca `MatchPlayed`/`PlayerWon` ao fim da partida |
| `Assets/Scripts/PingPongSession.cs` | Campos estáticos `MatchPlayed` e `PlayerWon` |
| `Assets/Editor/TopDownSceneBuilder.cs` | Escolha ética no Enzo e no Matheus |

---

## 2. Cenário: RU interior, morro e departamentos (commit `b5aafbc`)

3 assets novas integradas ao mapa.

### 2.1 Interior do RU — trocado
- Arte: `ru_pixel.png` → **`ru_interno.png`** (`RUInteriorPath`).
- A arte nova tem o **salão centrado** (a antiga era descentralizada), então:
  - Colisões laterais recentralizadas: piso caminhável de **0.240 a 0.760** da largura.
  - **Colisor novo no balcão de comida** (x 0.34–0.66, y 0.15–0.29) — não dá pra atravessar.
  - Vão de saída (tapete `RoomExit`) recentralizado (0.360–0.640).
- O Natan e o fluxo de entrada/saída continuam iguais.

### 2.2 Morro "UFC" — colocado
- Arte: **`morro_grama.png`** (oval de grama com borda de pedra, top-down).
- Posição: **(21, 29)** — no final direito do estacionamento da frente, longe da entrada da Guarita (x=−6).
- Colisão no corpo do oval, pra o jogador contornar o canteiro elevado.

### 2.3 Departamentos — substituíram os placeholders "DEP."
- Arte: **`departamento.png`** (fachada em perspectiva, larga e baixa).
- Os 2 blocos chapados placeholder viraram prédios reais (fechados, **sem interior**, colisão só no corpo desenhado — mesmo padrão da Guarita/Convivência):
  - **DEPTO. (008)** em (−24, −10), ~8 de largura (abaixo do RU).
  - **DEPTO. (009)** em (2, −22), ~9 de largura (coluna do Bloco 3, ao sul).
- Helper novo `DepartamentoBuilding(root, label, center, visibleWidth)`.
- Vegetação (`ScatterFoliage`) ajustada pra não nascer sobre os departamentos maiores.

### Assets novas
| Arquivo | Uso |
|---------|-----|
| `Assets/Art/Campus/ru_interno.png` | Interior do refeitório |
| `Assets/Art/Campus/departamento.png` | Fachada dos departamentos |
| `Assets/Art/Env/morro_grama.png` | Morrinho "UFC" do estacionamento |

> `GetEnvSprite` importa e configura o texture importer sozinho (Sprite, PPU, Point filter, sem mipmaps) no rebuild. `ru_pixel.png` ficou órfão no projeto (pode ser removido depois).

---

## Como testar
1. Unity → **Tools > Calouro > Montar Cena Top-Down**
2. **Ctrl+S** (salvar a cena)
3. **Play**

Pontos de verificação:
- **RU:** entrar pra ver o interior novo + o balcão sólido.
- **Morro:** andar pra cima a partir do spawn até o estacionamento (extremidade direita).
- **Departamentos:** ao sul do mapa (abaixo do RU e na coluna do Bloco 3).
- **Ética:** conferir o acúmulo na caderneta (ESC) ao longo dos 3 dias; jogar ping-pong no Dia 1 e ver "Ética +1.0 · estresse aliviado".

## Ajustes em aberto (dependem de feedback visual)
- Posição do **morro** (chutei a extremidade direita do estacionamento).
- Posição dos **departamentos** (herdaram o lugar dos placeholders, meio afastados — dá pra deixar mais central/visível).
