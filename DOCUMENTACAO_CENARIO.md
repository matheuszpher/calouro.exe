# Documentação do Cenário — calouro.exe

Este guia explica **tudo que foi adicionado recentemente** ao cenário do jogo: os
prédios (blocos e R.U.), as telas de interior, a grama e a vegetação. Está escrito
para que **qualquer pessoa**, mesmo sem experiência com programação ou Unity,
consiga entender o que cada coisa faz e **como mudar** os valores.

> **Leia primeiro isto:** o cenário do jogo é montado **automaticamente** por um
> "montador" (um arquivo de código). Você não arrasta objetos na mão — você muda
> **números** dentro desse arquivo e manda o Unity **remontar** a cena. Nada do que
> você mudar aparece no jogo até você fazer o passo do item 1.

---

## 1. Como aplicar QUALQUER mudança (o passo mais importante)

Sempre que você alterar um número neste guia, ou quando eu trocar uma arte, você
precisa **remontar a cena** dentro do Unity:

1. Abra o projeto no **Unity**.
2. Espere ele terminar de "compilar" (barrinha no canto inferior direito).
3. No menu superior, clique em **`Tools` → `Calouro` → `Montar Cena Top-Down`**.
4. Salve a cena com **`Ctrl + S`**.
5. Aperte **Play** (o triângulo ▶ no topo) para testar.

Se você mudou uma **imagem** (arte), o Unity também precisa reimportá-la — isso
acontece sozinho quando você foca a janela do Unity, e o montador reforça os ajustes
de importação ao rodar. **Só recarregar a cena sem rodar o montador não aplica as
mudanças.**

O arquivo que você vai editar na maioria das vezes é:

```
Assets/Editor/TopDownSceneBuilder.cs
```

Para editar: clique com o botão direito nele dentro do Unity → *Open*, ou abra com
qualquer editor de texto (Bloco de Notas, VS Code). **Sempre salve o arquivo antes
de remontar.**

---

## 2. Conceitos básicos (coordenadas e tamanhos)

- O mundo é medido em **unidades**. O personagem tem mais ou menos **1 unidade** de
  altura.
- Cada posição é escrita como `new Vector2(X, Y)`:
  - **X** cresce para a **direita** (negativo = esquerda).
  - **Y** cresce para **cima** (negativo = baixo).
- O campus vai aproximadamente de `X = -40` a `X = 40` e `Y = -42` a `Y = 42`.
- Um **prédio** é posicionado pelo seu **centro**.
- Frações "norm" (ex.: `0.5`) significam "**metade** da imagem", `0.0` = borda
  esquerda/topo, `1.0` = borda direita/base. Elas são usadas para dizer **onde**,
  dentro de uma arte, fica uma porta ou uma parede — independente do tamanho.

---

## 3. Mapa dos arquivos que foram adicionados/alterados

### Imagens (artes)
| Arquivo | O que é |
|---|---|
| `Assets/Art/Campus/bloco1_ext.png` | **Exterior** do Bloco 1 (perspectiva, no campus) |
| `Assets/Art/Campus/bloco2_ext.png` | **Exterior** do Bloco 2 |
| `Assets/Art/Campus/bloco34_ext.png` | **Exterior** dos Blocos 3 e 4 (mesma arte) |
| `Assets/Art/Campus/ru_ext.png` | **Exterior** do R.U. (visto de lado) |
| `Assets/Art/Campus/bloco_pixel.png` | **Interior** do bloco (corredor visto de cima, com 6 portas) |
| `Assets/Art/Campus/ru_pixel.png` | **Interior** do R.U. (refeitório visto de cima) |
| `Assets/Art/Env/grass_tile.png` | Textura de **grama** que se repete pelo chão |
| `Assets/Art/Env/bush.png` | Arbusto (vegetação) |
| `Assets/Art/Env/tree.png` | Árvore (vegetação) |

### Scripts (código)
| Arquivo | O que faz |
|---|---|
| `Assets/Editor/TopDownSceneBuilder.cs` | **O montador**. Monta o campus, os prédios, interiores, grama e vegetação. É aqui que você mexe. |
| `Assets/Scripts/InteriorController.cs` | Faz a **troca de tela** ao entrar/sair de prédios (suporta prédio-dentro-de-prédio). |
| `Assets/Scripts/BuildingDoor.cs` | A **porta**: mostra "Aperte E para entrar" e dispara a troca de tela. |
| `Assets/Scripts/RoomExit.cs` | O **tapete de saída**: ao pisar, volta para a tela anterior. |
| `Assets/Scripts/NpcInteractable.cs` | Personagem com quem se conversa (ex.: Natan). |

---

## 4. Como os prédios funcionam (o sistema de camadas)

O jogo tem **3 camadas** de tela:

```
CAMPUS (exterior)  →  [entra pela porta]  →  INTERIOR DO BLOCO (corredor)  →  [entra numa sala]  →  SALA DE AULA
CAMPUS (exterior)  →  [entra pela porta]  →  INTERIOR DO R.U. (refeitório, onde está o Natan)
```

- No **campus** você vê o prédio **por fora** (as artes `*_ext.png`). Ele é sólido:
  você não atravessa.
- Ao chegar na **porta** (na base do prédio) aparece **"Aperte E para entrar"**.
  Apertando **E**, a tela **troca** para o interior.
- Dentro do bloco você anda pelo **corredor** (`bloco_pixel.png`). As **3 portas do
  lado direito** levam, cada uma, a uma **sala de aula**.
- Para **voltar**, pise no **tapete verde** perto da entrada — ele te devolve para a
  tela anterior (a sala volta ao corredor; o corredor volta ao campus).

Tudo isso é montado pelas funções do arquivo `TopDownSceneBuilder.cs`. Abaixo, como
mexer em cada parte.

---

## 5. Mudando os PRÉDIOS no campus

Procure no `TopDownSceneBuilder.cs` pelo trecho que começa com o comentário
**`001–004 — Blocos didáticos`**. Você verá algo assim:

```csharp
BuildBlocoBuilding(root, "BLOCO 1 (001)", PosBloco1, 12f,
    Bloco1ExtPath, new Vector4(0.225f, 0.098f, 0.774f, 0.878f), 0.504f, 0.878f);
```

Cada número tem um significado. Veja a tabela:

| Parte | Exemplo | O que é | Como mudar |
|---|---|---|---|
| Nome | `"BLOCO 1 (001)"` | Etiqueta que aparece no mundo | Troque o texto |
| Posição | `PosBloco1` ou `new Vector2(13f, 10f)` | **Centro** do prédio no campus | Mude os dois números (X, Y) |
| Tamanho | `12f` | **Altura** do prédio em unidades (a largura é calculada sozinha) | Aumente para prédio maior, diminua para menor |
| Arte | `Bloco1ExtPath` | Qual imagem externa usar | Use `Bloco1ExtPath`, `Bloco2ExtPath` ou `Bloco34ExtPath` |
| Caixa da arte | `new Vector4(0.225, 0.098, 0.774, 0.878)` | Onde o desenho fica dentro da imagem (esquerda, topo, direita, base) | **Não precisa mexer** — já medido |
| Porta X | `0.504f` | Posição horizontal da porta na arte (0.5 = centro) | Só mexa se a porta estiver desalinhada |
| Porta base | `0.878f` | Altura da base da porta na arte | Só mexa se o gatilho não cair na porta |

**Exemplos práticos:**

- *Mover o Bloco 2 mais para a direita:* troque `new Vector2(13f, 10f)` por
  `new Vector2(16f, 10f)` (aumentei o X de 13 para 16).
- *Deixar o Bloco 1 maior:* troque o `12f` por `15f`.
- *Onde ficam as posições nomeadas* (`PosBloco1`, `PosPortal`): estão no topo do
  arquivo, procure por `private static readonly Vector2 PosBloco1`.

### O R.U.

Procure pelo comentário **`007 — RU`**:

```csharp
BuildRUBuilding(root, "RU (007)", new Vector2(-22f, 2f), 22f,
    RUExtPath, new Vector4(0.022f, 0.301f, 0.977f, 0.627f), 0.506f, 0.627f);
```

Mesma lógica dos blocos. O R.U. é **largo**, por isso a altura é `22f` (parece maior,
mas por ser deitado ele fica baixo e comprido).

> ⚠️ **Cuidado ao mover prédios:** se dois prédios ficarem muito perto, podem se
> sobrepor. Depois de mexer, sempre remonte (item 1) e olhe no Play.

---

## 6. Mudando o INTERIOR do bloco (corredor e salas)

Procure pela função **`BuildBlocoInterior`**. Dentro dela:

### Largura do corredor caminhável
```csharp
float corridorLeft = c.x - 0.20f * size.x;
float corridorRight = c.x + 0.20f * size.x;
```
O `0.20` é a **metade da largura** do corredor (em fração da arte). 
- Corredor **mais largo** → aumente para `0.22`.
- Corredor **mais estreito** → diminua para `0.18`.
- **Mude os dois** para manter centralizado.

### Tamanho do vão da porta (entrada/saída)
```csharp
float gapHalf = 0.085f * size.x;
```
`0.085` é a metade do buraco por onde se passa. Maior = passagem mais larga.

### Vasos de planta (têm colisão — o jogador contorna)
```csharp
var pots = new[]
{
    new Vector2(0.337f, 0.086f), new Vector2(0.652f, 0.086f),
    new Vector2(0.337f, 0.372f), new Vector2(0.652f, 0.372f),
    new Vector2(0.337f, 0.671f), new Vector2(0.652f, 0.671f),
};
```
Cada linha é **um vaso**: `(fração X, fração Y)` medida a partir do **canto superior
esquerdo** da arte.
- **Remover** um vaso: apague a linha dele.
- **Adicionar** um vaso: copie uma linha e mude os números.
- **Mover** um vaso: `X` maior = mais à direita; `Y` maior = mais para baixo.

### As 3 portas das salas (lado direito)
```csharp
float[] dy = { 0.307f, 0.0185f, -0.326f };
```
São as **alturas** das 3 portas (de cima para baixo), medidas a partir do centro.
- Número **maior** = porta mais para **cima**; **menor/negativo** = mais para **baixo**.
- A posição horizontal do gatilho é `c.x + 0.145f * size.x` (perto da parede direita).

> Cada porta abre uma **sala de aula** montada pela função `BuildInteriorRoom`
> (lousa, mesa do professor, carteiras). Para mexer na sala em si, é essa função.

---

## 7. Mudando o INTERIOR do R.U. (refeitório e o Natan)

Procure pela função **`BuildRUInterior`**.

### Largura do salão caminhável
```csharp
float hallLeft = leftEdge + 0.242f * size.x;
float hallRight = leftEdge + 0.708f * size.x;
```
`0.242` e `0.708` são as **paredes** esquerda e direita do salão (frações da arte).
O jogador anda **entre** elas. Aproxime os números para um salão mais estreito.

### Onde o Natan aparece
```csharp
CreateNpc(interiorsRoot, "NPC_Natan", CharsFolder + "/natan.png",
    new Vector2((gL + gR) / 2f, bottom + 5.0f), "Natan", "natan",
    new[]
    {
        "E aí, calouro! Eu sou o Natan.",
        ...
    });
```
- Para **mover o Natan**, mude o `bottom + 5.0f` (maior = mais para cima/fundo do
  salão) ou troque a posição inteira por `new Vector2(X, Y)`.
- Para **mudar as falas** dele, edite as linhas de texto entre aspas.

> **Importante:** o Natan agora fica **dentro** do R.U. A missão "vá ao RU e fale com
> o Natan" exige que o jogador **entre** no prédio. Se quiser o Natan no campus de
> novo, me peça.

---

## 8. Grama do chão

Procure, dentro de `BuildCampus`, pela linha:

```csharp
Sprite grass = GetEnvSprite(GrassTilePath, 32f, repeat: true);
```

- O `32f` é o **PPU** (pixels por unidade). Ele controla o **tamanho** da grama:
  - Número **maior** (ex.: `40f`) → grama **mais fina** (pixels menores), mas repete
    mais vezes.
  - Número **menor** (ex.: `24f`) → grama **maior/mais visível**, repete menos.
- A imagem da grama é `Assets/Art/Env/grass_tile.png`. Se você quiser trocar por
  outra textura de grama, substitua esse arquivo (de preferência **quadrada e sem
  emenda**) e remonte.

**Nitidez:** todas essas texturas são importadas em modo **Point** (sem desfoque),
sem *mipmaps* e sem compressão. Isso é feito automaticamente pelo montador. Se a
grama parecer borrada, confira no Unity: clique na textura → no Inspector, *Filter
Mode* deve estar **Point (no filter)**. Rodar o montador (item 1) já corrige isso.

---

## 9. Vegetação (árvores e arbustos)

Procure pela função **`ScatterFoliage`**. Ela espalha vegetação pelo gramado, em
grupos, de forma aleatória, **evitando** prédios e caminhos.

### Quantidade
```csharp
const int clusters = 95;
```
Cada "cluster" é um grupo com 1 árvore + alguns arbustos.
- **Mais vegetação** → aumente (ex.: `130`).
- **Menos** → diminua (ex.: `60`).

### Áreas proibidas (onde NÃO nasce vegetação)
```csharp
Block(2, 10, 7, 11, 1.5f); Block(13, 10, 6, 9, 1.5f);
...
```
Cada `Block(X, Y, Largura, Altura, Margem)` é um **retângulo bloqueado** (um prédio,
um caminho). Se você **mover ou aumentar um prédio**, atualize o `Block`
correspondente para a vegetação não nascer em cima dele.
- Para **liberar** uma área, apague o `Block` dela.
- Para **proteger** uma área nova, copie uma linha `Block(...)` e ajuste.

### Faixa do topo sempre livre
```csharp
if (y > 24f) return false; // avenida/estacionamento livres
```
Nada de vegetação acima de `Y = 24` (onde ficam a avenida e o estacionamento).

### Aparência
As árvores/arbustos **não têm colisão** (o jogador atravessa). Se quiser que as
árvores **bloqueiem** a passagem (tronco sólido), me avise. As imagens são
`tree.png` e `bush.png` — dá para substituí-las por outras.

---

## 10. Componentes de script (o que aparece no Inspector do Unity)

Estes são os "componentes" que o montador coloca nos objetos. Você normalmente
**não precisa** mexer neles na mão (o montador preenche tudo), mas aqui está o que
cada propriedade significa, caso queira ajustar um objeto específico clicando nele
no Unity.

### `BuildingDoor` (a porta do prédio)
Fica no objeto `Door_...`. Ao apertar **E** perto dela, troca de tela.
| Propriedade | Significado |
|---|---|
| **Room Spawn** | Onde o jogador aparece **dentro** ao entrar |
| **Return Position** | Onde o jogador volta **ao sair** |
| **Room Bounds Min / Max** | O "enquadramento" da câmera na tela de dentro (canto inferior-esquerdo e superior-direito) |
| **Room Label** | O nome que aparece na dica "Aperte E para entrar — ..." |

### `InteriorController` (o controlador de troca de tela)
Existe **um só** na cena (objeto `InteriorController`). Não tem propriedades para
ajustar — ele guarda uma "pilha" de telas para saber para onde voltar quando você
sai. Graças a ele funciona **campus → bloco → sala** e a volta em ordem.

### `RoomExit` (o tapete de saída)
Fica nos objetos `CExit_...` / `RExit_...`. Não tem propriedades: **ao pisar**, volta
para a tela anterior.

### `NpcInteractable` (personagens, ex.: Natan, Coordenador)
| Propriedade | Significado |
|---|---|
| **Npc Name** | Nome exibido no diálogo |
| **Npc Id** | Identificador usado pela missão (`natan`, `coordenador`) |
| **Lines** | As falas, uma por linha |

---

## 11. Receitas rápidas (passo a passo)

**Quero mover um bloco:** abra `TopDownSceneBuilder.cs` → ache a linha
`BuildBlocoBuilding(...)` do bloco → mude a posição (o `Vector2`) → salve → remonte
(item 1).

**Quero deixar um prédio maior/menor:** na mesma linha, mude o número da **altura**
(ex.: `12f`). Salve e remonte.

**Quero mais/menos árvores:** função `ScatterFoliage` → mude `const int clusters`.
Salve e remonte.

**Quero trocar a arte da grama:** substitua `Assets/Art/Env/grass_tile.png` por outra
imagem com o mesmo nome. Remonte.

**Quero mudar a fala do Natan:** função `BuildRUInterior` → edite os textos entre
aspas. Remonte.

**A grama está borrada:** remonte (item 1). Se persistir, clique na textura no Unity
e confira *Filter Mode = Point*.

**Uma árvore nasceu em cima de um prédio:** você mexeu num prédio mas não no `Block`
correspondente em `ScatterFoliage`. Ajuste o `Block(...)` daquele prédio.

**O gatilho da porta não está na porta desenhada:** na linha do prédio, ajuste os dois
últimos números (`Porta X` e `Porta base`) em pequenos passos (ex.: `0.878` → `0.86`).

---

## 12. Resumo das mudanças recentes (histórico)

- **Blocos e R.U. viraram prédios de duas telas:** exterior em perspectiva no campus
  + interior top-down acessado por **transição de tela** ao apertar **E** na porta.
- **`InteriorController`** virou "empilhável", permitindo campus → bloco → sala e a
  volta na ordem certa.
- **Interior do bloco** (`bloco_pixel`) tem corredor caminhável, vasos com colisão e
  3 portas que levam a salas de aula.
- **Interior do R.U.** (`ru_pixel`) tem o salão caminhável e o **Natan dentro**.
- **Grama** trocada por uma textura nítida que se repete pelo mapa (importada em modo
  Point, sem desfoque).
- **Vegetação** (árvores/arbustos) espalhada em grupos pelo gramado, evitando prédios
  e caminhos.

---

*Dúvida em algo que não está aqui? Peça e eu detalho ou ajusto o comportamento.*
