using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ferramentas do projeto Calouro:
///  - Fatia as folhas de personagem (12 poses, grade 4x3) em sprites individuais.
///  - Monta um cenário top-down (chão, paredes com colisão, obstáculos) e
///    configura o Player com Rigidbody2D + colisor usando UMA pose do personagem.
/// Use pelo menu: Tools > Calouro.
/// </summary>
public static class TopDownSceneBuilder
{
    private const string WhitePath = "Assets/Sprites/white.png";
    private const string CharsFolder = "Assets/Sprites/Characters";
    // Folhas do protagonista (escolhido na tela de título). Layout 6x4 (24 poses),
    // diferente dos NPCs (4x3). Ver PlayerAppearance/SpriteWalkAnimator.
    private const string CalouroSpritePath = CharsFolder + "/calouro.png";
    private const string CalouraSpritePath = CharsFolder + "/caloura.png";

    private const int Cols = 4;
    private const int Rows = 3;
    private const float CharPixelsPerUnit = 100f;
    // Pose padrão dos NPCs (folha 4x3): linha de baixo, 1ª coluna = frente parado.
    private const int NpcDefaultFrame = 8;

    // Folhas do jogador: grade 6x4 e maior resolução — PPU próprio para o
    // personagem sair na mesma altura em tela que os NPCs.
    private const int PlayerCols = 6;
    private const int PlayerRows = 4;
    private const float PlayerCharPixelsPerUnit = 140f;
    // Pose usada por padrão no Player: linha de cima, 1ª coluna = frente parado.
    private const int PlayerIdleFrame = 0;

    [MenuItem("Tools/Calouro/Montar Cena Top-Down")]
    public static void BuildScene()
    {
        SliceAllCharacters();
        CampusAssets.EnsureSliced();

        Sprite white = GetWhiteSprite();
        if (white == null)
        {
            Debug.LogError($"[Calouro] Não encontrei {WhitePath}.");
            return;
        }
        s_white = white;
        roomCounter = 0;
        interiorBldgCounter = 0;
        interiorsRoot = null;

        var old = GameObject.Find("Environment");
        if (old != null) Object.DestroyImmediate(old);
        var root = new GameObject("Environment");

        BuildCampus(root.transform, white);

        SetupPlayer();
        SetupCamera();
        SetupDialogue();
        SetupNPCs();
        SetupQuest();
        SetupHud();
        SetupMaze(white);
        SetupInteriors();
        SetupTitle();
        SetupCampusTour();
        SetupMusic();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[Calouro] Campus provisório montado! Salve (Ctrl+S) e dê Play — a câmera segue o jogador (WASD/setas).");
    }

    [MenuItem("Tools/Calouro/Fatiar Personagens em 12 poses")]
    public static void SliceMenu()
    {
        SliceAllCharacters();
        Debug.Log("[Calouro] Personagens fatiados em 12 poses (grade 4x3).");
    }

    private static void SliceAllCharacters()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { CharsFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            // O protagonista (calouro/caloura) vem em folha 6x4 e resolução maior;
            // os NPCs seguem o padrão 4x3.
            if (path == CalouroSpritePath || path == CalouraSpritePath)
                SliceGrid(path, PlayerCols, PlayerRows, PlayerCharPixelsPerUnit);
            else
                SliceGrid(path, Cols, Rows, CharPixelsPerUnit);
        }
        AssetDatabase.Refresh();
    }

    private static void SliceGrid(string path, int cols, int rows, float pixelsPerUnit)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.isReadable = true; // preciso ler os pixels para achar as figuras
        importer.SaveAndReimport();

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex == null) return;
        int W = tex.width, H = tex.height;

        // Detecta as faixas de conteúdo (por alpha): colunas e linhas reais das figuras.
        var colBands = ContentBands(tex, true, cols);
        var rowBands = ContentBands(tex, false, rows);
        bool byContent = colBands.Count == cols && rowBands.Count == rows;

        System.Func<int, int, Rect> cellRect;
        if (byContent)
        {
            // r=0 é a linha de cima; rowBands vem de baixo (y=0) pra cima.
            cellRect = (c, r) =>
            {
                var cb = colBands[c];
                var rb = rowBands[rows - 1 - r];
                return PadRect(cb.x, rb.x, cb.y - cb.x, rb.y - rb.x, W, H, 2);
            };
        }
        else
        {
            int cw = W / cols, ch = H / rows;
            cellRect = (c, r) => new Rect(c * cw, (rows - 1 - r) * ch, cw, ch);
        }

        var factories = new SpriteDataProviderFactories();
        factories.Init();
        var dp = factories.GetSpriteEditorDataProviderFromObject(importer);
        dp.InitSpriteEditorDataProvider();

        string baseName = System.IO.Path.GetFileNameWithoutExtension(path);
        var rects = new List<SpriteRect>();
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int idx = r * cols + c;
                rects.Add(new SpriteRect
                {
                    name = $"{baseName}_{idx:00}",
                    // ID determinístico: re-fatiar mantém o mesmo ID (não quebra refs).
                    spriteID = StableGuid($"{baseName}_{idx:00}"),
                    rect = cellRect(c, r),
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f)
                });
            }
        }

        dp.SetSpriteRects(rects.ToArray());

        var nameProvider = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (nameProvider != null)
            nameProvider.SetNameFileIdPairs(rects.Select(s => new SpriteNameFileIdPair(s.name, s.spriteID)));

        dp.Apply();
        (dp.targetObject as AssetImporter)?.SaveAndReimport();
    }

    /// <summary>
    /// Acha faixas contíguas com conteúdo (alpha) ao longo de um eixo.
    /// columns=true → faixas no eixo X (colunas); false → eixo Y (linhas).
    /// </summary>
    private static List<Vector2Int> ContentBands(Texture2D tex, bool columns, int expected)
    {
        int W = tex.width, H = tex.height;
        Color32[] px;
        try { px = tex.GetPixels32(); }
        catch { return new List<Vector2Int>(); }

        int n = columns ? W : H;
        int other = columns ? H : W;
        var has = new bool[n];
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < other; j++)
            {
                int x = columns ? i : j;
                int y = columns ? j : i;
                if (px[y * W + x].a > 16) { has[i] = true; break; }
            }
        }

        var bands = new List<Vector2Int>();
        int start = -1;
        for (int i = 0; i <= n; i++)
        {
            bool on = i < n && has[i];
            if (on && start < 0) start = i;
            else if (!on && start >= 0)
            {
                if (i - start >= 6) bands.Add(new Vector2Int(start, i)); // ignora ruído fino
                start = -1;
            }
        }
        return bands;
    }

    private static Rect PadRect(float x, float y, float w, float h, int W, int H, int pad)
    {
        float x0 = Mathf.Max(0, x - pad);
        float y0 = Mathf.Max(0, y - pad);
        float x1 = Mathf.Min(W, x + w + pad);
        float y1 = Mathf.Min(H, y + h + pad);
        return new Rect(x0, y0, x1 - x0, y1 - y0);
    }

    private static GUID StableGuid(string key)
    {
        using (var md5 = MD5.Create())
        {
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(key));
            var sb = new StringBuilder(32);
            foreach (var b in hash) sb.Append(b.ToString("x2"));
            GUID.TryParse(sb.ToString(), out var guid);
            return guid;
        }
    }

    private static Sprite[] LoadFrames(string path)
    {
        var sprites = AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .OrderBy(s => s.name, System.StringComparer.Ordinal)
            .ToArray();
        if (sprites.Length > 0)
            return sprites;
        var single = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        return single != null ? new[] { single } : new Sprite[0];
    }

    private static Sprite LoadFrame(string path, int frame)
    {
        var sprites = LoadFrames(path);
        if (sprites.Length == 0) return null;
        return sprites[Mathf.Clamp(frame, 0, sprites.Length - 1)];
    }

    private static GameObject CreateQuad(Transform parent, string name, Vector2 pos, Vector2 size,
        Color color, Sprite sprite, int sortingOrder, bool withCollider)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;

        if (withCollider)
            go.AddComponent<BoxCollider2D>();

        return go;
    }

    /// <summary>
    /// Sprite esticado como UMA peça só (drawMode Simple) para preencher uma área.
    /// rotate90 gira 90° (para paredes verticais usando um sprite de parede horizontal).
    /// </summary>
    private static GameObject StretchedSprite(Transform parent, string name, Vector2 center, Vector2 worldSize,
        Sprite sprite, int sortingOrder, Color color, bool rotate90 = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = center;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;
        if (sprite != null)
        {
            Vector2 b = sprite.bounds.size; // tamanho nativo em unidades (no PPU)
            if (b.x <= 0f || b.y <= 0f) return go;
            if (!rotate90)
                go.transform.localScale = new Vector3(worldSize.x / b.x, worldSize.y / b.y, 1f);
            else
            {
                // Após girar 90°, o eixo X local vira Y do mundo e vice-versa.
                go.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                go.transform.localScale = new Vector3(worldSize.y / b.x, worldSize.x / b.y, 1f);
            }
        }
        return go;
    }

    /// <summary>Objeto decorativo (mobília) posicionado, pivô embaixo, sem colisão por padrão.</summary>
    private static GameObject Prop(Transform parent, string name, Vector2 pos, Sprite sprite,
        int sortingOrder, float scale = 1f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(scale, scale, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;
        return go;
    }

    // ---- Campus UFC Quixadá (amplo, baseado no mapa oficial) ----

    // MapXMin estendido de -40 pra -52 em 04/07/2026 pra abrir espaço pro RU se
    // afastar mais da Convivência (ver BuildCampus — RU e o novo Path_RU_Conv).
    private const float MapXMin = -52f, MapXMax = 40f, MapYMin = -42f, MapYMax = 42f;
    private const float WallT = 0.6f;

    // Posições-chave (compartilhadas entre os montadores).
    // O jogador começa na passarela logo depois da Guarita (entrada do campus).
    private static readonly Vector3 SpawnPos = new Vector3(-6f, 18.5f, 0f);
    // O Coordenador (Jeferson) começa no FIM da passarela, antes da Convivência —
    // dali ele sobe até o calouro na abertura do Dia 1 (ver CampusTourCutscene).
    private static readonly Vector2 PosCoordenador = new Vector2(-6f, 8.5f);
    private static readonly Vector2 ConvCenter = new Vector2(-7f, 2f);
    private const float ConvCanvas = 14f; // convivencia_ext.png é quadrada (1254x1254)
    private static readonly Vector2 PosBloco1 = new Vector2(2f, 10f);      // Bloco 1 (001)
    private static readonly Vector2 PosBloco1Front = new Vector2(2f, 4.4f); // frente da porta do Bloco 1
    private static readonly Vector2 PosPortal = new Vector2(2f, -6f);      // Bloco 3 (003)

    private static Sprite s_white;
    private static int roomCounter;
    private static int interiorBldgCounter;
    private static Transform interiorsRoot;

    // Interiores (top-down) usados após a transição de tela.
    private const string BlocoInteriorPath = "Assets/Art/Campus/bloco_pixel.png";
    private const string RUInteriorPath = "Assets/Art/Campus/ru_interno.png";
    // ac_interno.png já vem cortada (sem a borda branca do arquivo original).
    private const string ACInteriorPath = "Assets/Art/Campus/ac_interno.png";
    // sala_aula.png já vem cortada — 718x857 (aspect 0.838), paredes+lousa+carteiras
    // já desenhadas; só a porta (embaixo, centralizada) é passável.
    private const string SalaAulaPath = "Assets/Art/Campus/sala_aula.png";
    // Exteriores (perspectiva) mostrados no campus. Todos 1122x1402 (aspect 0.8).
    private const string Bloco1ExtPath = "Assets/Art/Campus/bloco1_ext.png";
    private const string Bloco2ExtPath = "Assets/Art/Campus/bloco2_ext.png";
    private const string Bloco34ExtPath = "Assets/Art/Campus/bloco34_ext.png";
    private const string RUExtPath = "Assets/Art/Campus/ru_ext.png";
    // Convivência é quadrada (1254x1254, aspect 1.0), diferente dos blocos/RU (0.8).
    private const string ConvivenciaExtPath = "Assets/Art/Campus/convivencia_ext.png";
    // Guarita (entrada) — arte quadrada 1254x1254; conteúdo em (0.266,0.148)-(0.727,0.738).
    private const string GuaritaExtPath = "Assets/Art/Campus/guarita.png";
    // Fachada de departamento (perspectiva, larga e baixa: conteúdo x 0.140–0.864,
    // y 0.372–0.557 no quadrado). Prédio fechado, sem interior.
    private const string DepartamentoExtPath = "Assets/Art/Campus/departamento.png";
    private const string GrassTilePath = "Assets/Art/Env/grass_tile.png";
    private const string BushPath = "Assets/Art/Env/bush.png";
    private const string TreePath = "Assets/Art/Env/tree.png";
    // Passarela Guarita↔Convivência. Já vem cortada — 340x1024 (aspect 0.332).
    private const string CaminhoEntradaPath = "Assets/Art/Env/caminho_entrada.png";
    private const string MorroGramaPath = "Assets/Art/Env/morro_grama.png";
    // Estrada em H dos blocos (só chão, sem colisão). Já vem cortada — 804x708.
    private const string CaminhoBlocoPath = "Assets/Art/Env/caminho_bloco.png";
    // Saída norte dos Blocos 1/2 até a passarela — formato de "П" (barra
    // horizontal + 2 pernas). Canvas quadrado 1254x1254; conteúdo visível em
    // x 0.094–0.903, y 0.228–0.777 (medido por alpha); pernas em x 0.393–0.595
    // e 0.705–0.903 (centros a ~0,311 do canvas um do outro), barra ocupa até
    // y≈0.455. Só chão, sem colisão — igual à estrada em H.
    private const string CaminhoCimaPath = "Assets/Art/Env/caminho_cima.png";
    // Pedaço de caminho reto (só chão, sem colisão) — liga o RU (agora mais
    // afastado) à Convivência (04/07/2026). Canvas 1254x1254; conteúdo visível
    // em x 0.107–0.865, y 0.405–0.611 (medido por alpha), aspecto ~3.68:1.
    private const string PedacoCaminhoPath = "Assets/Art/Env/pedaco_caminho.png";

    // Música tema (loop, tocando desde a tela de título — ver SetupMusic/MusicPlayer).
    private const string MusicThemePath = "Assets/Audio/musica_tema.mp3";

    private static void BuildCampus(Transform root, Sprite white)
    {
        var wall = new Color(0.32f, 0.30f, 0.27f);
        var ground = new Color(0.15f, 0.19f, 0.13f);   // gramado do campus
        var path = new Color(0.30f, 0.28f, 0.23f);
        var asphalt = new Color(0.20f, 0.20f, 0.22f);
        var didatico = new Color(0.22f, 0.34f, 0.50f); // blocos 001-004 (azul)
        var servico = new Color(0.34f, 0.40f, 0.24f);  // 005/006/007/008/009 (verde)

        // Chão base do campus inteiro — grama em tile que se repete (1 draw só).
        Sprite grass = GetEnvSprite(GrassTilePath, 32f, repeat: true);
        if (grass != null)
            TiledSprite(root, "Ground", Vector2.zero,
                new Vector2(MapXMax - MapXMin, MapYMax - MapYMin), grass, -10);
        else
            CreateQuad(root, "Ground", Vector2.zero,
                new Vector2(MapXMax - MapXMin, MapYMax - MapYMin), ground, white, -10, false);

        // Muros externos (Limite do Campus).
        float mw = MapXMax - MapXMin + WallT;
        float mh = MapYMax - MapYMin + WallT;
        CreateQuad(root, "Wall_Top", new Vector2(0f, MapYMax + WallT / 2f), new Vector2(mw, WallT), wall, white, 0, true);
        CreateQuad(root, "Wall_Bottom", new Vector2(0f, MapYMin - WallT / 2f), new Vector2(mw, WallT), wall, white, 0, true);
        CreateQuad(root, "Wall_Left", new Vector2(MapXMin - WallT / 2f, 0f), new Vector2(WallT, mh), wall, white, 0, true);
        CreateQuad(root, "Wall_Right", new Vector2(MapXMax + WallT / 2f, 0f), new Vector2(WallT, mh), wall, white, 0, true);

        // Avenida (topo) + estacionamento/entrada.
        CreateQuad(root, "Avenida", new Vector2(0f, 38f), new Vector2(76f, 5f), asphalt, white, -9, false);
        Label(root, "AV. JOSE DE FREITAS QUEIROZ", new Vector2(0f, 38f), Color.white);
        CreateQuad(root, "Estacionamento", new Vector2(0f, 29f), new Vector2(58f, 9f), new Color(0.26f, 0.26f, 0.28f), white, -9, false);
        Label(root, "ESTACIONAMENTO", new Vector2(0f, 29f), new Color(0.85f, 0.85f, 0.9f));

        // Morrinho gramado "UFC" (canteiro/rotatória) no final do estacionamento da
        // frente — a extremidade direita, longe da entrada da Guarita (x=-6). Arte
        // top-down (morro_grama.png): oval de grama com borda de pedra, ocupando o
        // meio do quadrado (conteúdo x 0.197–0.800, y 0.281–0.703). Colisão só no
        // corpo do morro pra o jogador contornar (é um canteiro elevado).
        Vector2 morroCenter = new Vector2(21f, 29f);
        const float morroCanvas = 12f;
        Sprite morroArt = GetEnvSprite(MorroGramaPath, 100f, repeat: false);
        if (morroArt != null)
        {
            StretchedSprite(root, "Morro_UFC", morroCenter, new Vector2(morroCanvas, morroCanvas), morroArt, -7, Color.white);
            // Caixa interna do oval (um pouco menor que o conteúdo, pra encostar sem travar longe).
            CreateQuad(root, "MorroCol", morroCenter,
                new Vector2(0.52f * morroCanvas, 0.34f * morroCanvas), new Color(0f, 0f, 0f, 0f), s_white, 0, true);
        }

        // Caminhos internos (visuais/passarelas). Toda rua converge na Convivência
        // ou na rua de entrada (ao lado da Guarita) — nunca fica solta no meio do mato.

        // Passarela da entrada (caminho_entrada.png) — liga a Guarita à Convivência.
        // O fim dela (y=6.9) encosta bem no desenho visível da AC — o conteúdo
        // visível de convivencia_ext.png só começa em y≈6.9 (mesma fração 0.151 de
        // convBTop), o resto do quadrado de 14x14 é margem transparente. O começo
        // (y=22) entra um pouco embaixo da Guarita, igual a passarela reta antiga.
        const float entradaX = -6f, entradaTop = 22f, entradaBottom = 6.9f;
        const float entradaH = entradaTop - entradaBottom, entradaW = entradaH * (340f / 1024f);
        Sprite entradaArt = GetEnvSprite(CaminhoEntradaPath, 100f, repeat: false);
        Vector2 entradaCenter = new Vector2(entradaX, (entradaTop + entradaBottom) / 2f);
        if (entradaArt != null)
            StretchedSprite(root, "Path_Entrada", entradaCenter, new Vector2(entradaW, entradaH), entradaArt, -8, Color.white);
        else
            CreateQuad(root, "Path_Entrada", entradaCenter, new Vector2(entradaW, entradaH), path, white, -9, false);
        float entradaLeft = entradaX - entradaW / 2f, entradaRight = entradaX + entradaW / 2f;
        float entradaWalkL = entradaLeft + (125f / 340f) * entradaW;
        float entradaWalkR = entradaLeft + (210f / 340f) * entradaW;

        // Só tem física onde a arte mostra a cerca de madeira (o resto é só sebe
        // baixa/grama, sem barreira de verdade) — medido por cor na imagem. A
        // cerca alterna de lado ao longo da curva; o trecho final (perto da AC)
        // não tem cerca nenhuma, por isso não tinha física ali antes.
        void CercaMadeira(string name, bool ladoEsquerdo, float imgY0, float imgY1)
        {
            float wy0 = entradaTop - (imgY0 / 1024f) * entradaH;
            float wy1 = entradaTop - (imgY1 / 1024f) * entradaH;
            float x0 = ladoEsquerdo ? entradaLeft : entradaWalkR;
            float x1 = ladoEsquerdo ? entradaWalkL : entradaRight;
            CreateQuad(root, name, new Vector2((x0 + x1) / 2f, (wy0 + wy1) / 2f),
                new Vector2(x1 - x0, wy0 - wy1), new Color(0f, 0f, 0f, 0f), s_white, 0, true);
        }
        CercaMadeira("Path_Entrada_Cerca_O1", true, 0f, 215f);
        CercaMadeira("Path_Entrada_Cerca_O2", true, 420f, 505f);
        CercaMadeira("Path_Entrada_Cerca_L1", false, 270f, 425f);
        CercaMadeira("Path_Entrada_Cerca_L2", false, 540f, 700f);
        CercaMadeira("Path_Entrada_Cerca_L3", false, 710f, 820f);

        // Estrada em H dos blocos (caminho_bloco.png) — é só chão (sem colisão,
        // totalmente caminhável por cima), sempre desenhado embaixo (sorting -8).
        // As duas colunas do H caem quase exatas em cima de x=2 (Bloco 1/3) e x=13
        // (Bloco 2/4) sem precisar mover nenhum bloco — só a altura/escala foi
        // ajustada (proporção original da arte preservada).
        const float blocoW = 17.07f, blocoH = 15.03f;
        Vector2 blocoCenter = new Vector2(6.21f, 2f);
        Sprite blocoArt = GetEnvSprite(CaminhoBlocoPath, 100f, repeat: false);
        if (blocoArt != null)
            StretchedSprite(root, "Path_Blocos_H", blocoCenter, new Vector2(blocoW, blocoH), blocoArt, -8, Color.white);
        else
            CreateQuad(root, "Path_Blocos_H", blocoCenter, new Vector2(blocoW, blocoH), path, white, -9, false);

        // Trechos que ficam fora da estrada em H: da ponta de baixo dela até a porta
        // sul de verdade dos Blocos 3 e 4 (a arte em H só cobre o lado norte deles).
        CreateQuad(root, "Path_009", new Vector2(2f, -14.25f), new Vector2(4f, 17.5f), path, white, -9, false);
        CreateQuad(root, "Path_Bloco4_Sul", new Vector2(13f, -8f), new Vector2(4f, 5.5f), path, white, -9, false);

        // Saída norte (túnel) do Bloco 1/2 → conecta com a passarela da entrada.
        // Arte "П" nova (caminho_cima.png, gerada 01/07): barra no topo, 2 pernas
        // que descem e um BRAÇO à esquerda no topo pra encostar na passarela.
        // Canvas QUADRADO (1254×1254) → escala uniforme, sem distorção.
        // Ancoragem:
        //  - as 2 pernas ficam a 11u uma da outra (= largura das portas dos Blocos
        //    1 x=2 e 2 x=13), então a escala sai de "espaçamento das pernas = 11u";
        //  - deslocado pra ESQUERDA até o BRAÇO entrar DEBAIXO da passarela (o braço
        //    vai até x=-5,5, dentro da passarela) — a conexão fica sem emenda porque
        //    a passarela desenha por cima (ver sortingOrder abaixo);
        //  - deslocado pra CIMA (base das pernas em y=15,5) a pedido.
        // IMPORTANTE (sortingOrder -9): este chão fica ABAIXO de tudo, menos a grama
        // (grama/chão = -10). Passarela (-8), ruas, blocos e guarita (+3) desenham
        // por cima — assim o caminho nunca COBRE nada; ele só "aparece" sobre a
        // grama e some por baixo de quem cruza com ele. Por isso pode entrar sob a
        // passarela/blocos sem problema.
        // Frações medidas por alpha na arte: conteúdo x 0,079–0,923, y 0,388–0,707;
        // pernas centradas em x 0,376 e 0,859 (espaçamento 0,483); braço em x 0,079.
        const float saidaArmLeftFrac = 0.0789f;    // aresta esquerda do braço (entra sob a passarela)
        const float saidaFeetFrac = 0.7073f;       // base das pernas
        const float saidaLegSpacingFrac = 0.4833f; // distância entre os centros das 2 pernas
        const float saidaDoorSpacing = 11f;        // portas dos Blocos 1 (x=2) e 2 (x=13)
        const float saidaArmX = -5.5f;             // o braço vai até aqui (sob a passarela — conexão sem emenda)
        const float saidaFeetY = 15.5f;            // base das pernas (deslocado pra cima a pedido)
        // Escala uniforme: pernas a 11u ⇒ canvas quadrado ≈22,8 (sem distorção).
        float saidaCanvas = saidaDoorSpacing / saidaLegSpacingFrac;
        float saidaCenterX = saidaArmX - (saidaArmLeftFrac - 0.5f) * saidaCanvas;
        float saidaCenterY = saidaFeetY + (saidaFeetFrac - 0.5f) * saidaCanvas;
        Vector2 saidaCenter = new Vector2(saidaCenterX, saidaCenterY);
        Vector2 saidaCanvasSize = new Vector2(saidaCanvas, saidaCanvas);
        Sprite saidaArt = GetEnvSprite(CaminhoCimaPath, 100f, repeat: false);
        if (saidaArt != null)
            StretchedSprite(root, "Path_SaidaNorte_12", saidaCenter, saidaCanvasSize, saidaArt, -9, Color.white);
        else
            CreateQuad(root, "Path_SaidaNorte_12", new Vector2(3.5f, 17.4f), new Vector2(21f, 5f), path, white, -9, false);

        var roofServico = new Color(0.42f, 0.48f, 0.30f);   // telhado serviço (verde)

        // 006 — Guarita (entrada) — arte TOP-DOWN (guarita.png: bicicletário + guarita
        // e piso), sem interior. Fica no TOPO da passarela: a base da guarita (y≈21.9)
        // encosta no topo da passarela (entradaTop=22) — a imagem da guarita termina
        // onde a passarela começa. Deslocada pra esquerda (x=-6.6) pra a ENTRADA (no
        // lado direito da arte) alinhar com a passarela (x=-6). O jogador entra pelo sul
        // (spawn em y=18.5) e a colisão fica só na parte sólida de cima (fundo/booth).
        Vector2 guaritaCenter = new Vector2(-7f, 24.15f);
        const float guaritaCanvas = 9.5f;
        Sprite guaritaArt = GetEnvSprite(GuaritaExtPath, 100f, repeat: false);
        if (guaritaArt != null)
        {
            StretchedSprite(root, "Ext_Guarita", guaritaCenter, new Vector2(guaritaCanvas, guaritaCanvas), guaritaArt, 3, Color.white);

            // Colisão só no terço de cima do conteúdo (fração 0.148–0.42 na arte).
            float gcl = guaritaCenter.x + (0.266f - 0.5f) * guaritaCanvas;
            float gcr = guaritaCenter.x + (0.727f - 0.5f) * guaritaCanvas;
            float gctop = guaritaCenter.y + (0.5f - 0.148f) * guaritaCanvas;
            float gcbot = guaritaCenter.y + (0.5f - 0.42f) * guaritaCanvas;
            CreateQuad(root, "GuaritaCol", new Vector2((gcl + gcr) / 2f, (gcbot + gctop) / 2f),
                new Vector2(gcr - gcl, gctop - gcbot), new Color(0f, 0f, 0f, 0f), s_white, 0, true);
        }
        else
        {
            CoveredBlock(root, "GUARITA (006)", new Vector2(-6f, 22f), new Vector2(5f, 4f), roofServico, 'S', false);
        }

        // 007 — RU: exterior (lateral) no campus; entrar faz TRANSIÇÃO de tela para
        // o refeitório (ru_interno), onde está o Natan.
        // Afastado mais da Convivência em 04/07/2026 (era x=-22) e a porta
        // passou do lado SUL pro lado LESTE (de frente pra Convivência, ligada
        // pelo Path_RU_Conv) — ver BuildRUBuilding.
        BuildRUBuilding(root, "RU (007)", new Vector2(-32f, 2f), 22f,
            RUExtPath, new Vector4(0.022f, 0.301f, 0.977f, 0.627f), 0.6f);

        // Caminho reto ligando a porta leste do RU à Convivência (pedaco_caminho.png,
        // 04/07/2026) — preenche o vão criado ao afastar o RU. Conteúdo medido por
        // alpha: x 0.107–0.865 (aspecto ~3,68:1), esticado sem distorcer (canvas
        // quadrado, mesma escala nos dois eixos) até preencher exatamente o vão
        // entre a borda direita do RU e a borda esquerda da Convivência.
        {
            const float pedacoContentFracW = 0.758f; // (1084-134)/1254, medido por alpha
            float ruRight = -32f + (0.977f - 0.5f) * (22f * 0.8f); // borda direita do conteúdo do RU
            float convLeft = ConvCenter.x + (0.33f - 0.5f) * ConvCanvas; // borda esquerda do conteúdo da AC
            float pedacoContentW = convLeft - ruRight;
            float pedacoSide = pedacoContentW / pedacoContentFracW; // canvas quadrado, mesma escala nos 2 eixos
            Vector2 pedacoCenter = new Vector2((ruRight + convLeft) / 2f, 2f);
            Sprite pedacoArt = GetEnvSprite(PedacoCaminhoPath, 100f, repeat: false);
            if (pedacoArt != null)
                StretchedSprite(root, "Path_RU_Conv", pedacoCenter, new Vector2(pedacoSide, pedacoSide), pedacoArt, -8, Color.white);
            else
                CreateQuad(root, "Path_RU_Conv", pedacoCenter, new Vector2(pedacoContentW, 4f), path, white, -9, false);
        }

        // 005 — Convivência (entre o RU e os blocos): arte externa convivencia_ext.png
        // (prédio coberto + deck/escada + jardim, quadrada — canvasW = canvasH, ao
        // contrário dos blocos/RU que são 0.8). Só a parte COBERTA (telhado/parede)
        // é sólida; o deck, a escada e o jardim continuam caminháveis — é ali que o
        // jogador nasce e o Coordenador fica (PosCoordenador/SpawnPos).
        Sprite convArt = GetEnvSprite(ConvivenciaExtPath, 100f, repeat: false);
        if (convArt != null)
            StretchedSprite(root, "Ext_Convivencia", ConvCenter, new Vector2(ConvCanvas, ConvCanvas), convArt, 3, Color.white);
        else
            CreateQuad(root, "Floor_Convivencia", ConvCenter, new Vector2(8f, 8f), new Color(0.22f, 0.32f, 0.20f), white, -8, false);

        // Caixa sólida só do prédio coberto (telhado + parede), medida na arte.
        float convBL = ConvCenter.x + (0.33f - 0.5f) * ConvCanvas;
        float convBR = ConvCenter.x + (0.86f - 0.5f) * ConvCanvas;
        float convBTop = ConvCenter.y + (0.5f - 0.151f) * ConvCanvas;
        float convBBot = ConvCenter.y + (0.5f - 0.522f) * ConvCanvas;
        CreateQuad(root, "ConvCol", new Vector2((convBL + convBR) / 2f, (convBTop + convBBot) / 2f),
            new Vector2(convBR - convBL, convBTop - convBBot), new Color(0f, 0f, 0f, 0f), s_white, 0, true);

        // Árvores do jardim (canto esquerdo da arte) — colisão no tronco, igual aos
        // vasos dos blocos; a copa desenhada continua visível por cima do jogador.
        var convTrees = new[]
        {
            new Vector2(0.207f, 0.303f),
            new Vector2(0.160f, 0.558f),
            new Vector2(0.223f, 0.521f),
            new Vector2(0.287f, 0.596f),
        };
        for (int t = 0; t < convTrees.Length; t++)
        {
            float tx = ConvCenter.x + (convTrees[t].x - 0.5f) * ConvCanvas;
            float ty = ConvCenter.y + (0.5f - convTrees[t].y) * ConvCanvas;
            CreateQuad(root, $"ConvTree_{t}", new Vector2(tx, ty), new Vector2(1.2f, 1.2f),
                new Color(0f, 0f, 0f, 0f), s_white, 0, true);
        }

        Label(root, "CONVIVENCIA (005)", new Vector2(ConvCenter.x, convBTop + 0.8f), new Color(0.9f, 1f, 0.9f));

        // Interior da Convivência (ac_interno.png): diferente dos blocos (só
        // norte/sul), aqui os 4 lados do prédio coberto têm porta própria — entrar
        // por cima aparece em cima, pela direita aparece pela direita, etc., e sai
        // sempre pelo mesmo lado por onde entrou.
        float acDoorX = (convBL + convBR) / 2f;
        const float acSideY = 2.5f; // altura das portas leste/oeste (longe das árvores e do Bloco 1)
        Vector3 acNorthFront = new Vector3(acDoorX, convBTop + 2.6f, 0f);
        Vector3 acSouthFront = new Vector3(acDoorX, convBBot - 2.6f, 0f);
        Vector3 acEastFront = new Vector3(convBR + 2.6f, acSideY, 0f);
        Vector3 acWestFront = new Vector3(convBL - 2.6f, acSideY, 0f);

        BuildConvivenciaInterior(acNorthFront, acSouthFront, acEastFront, acWestFront,
            out Vector3 acNorthSpawn, out Vector3 acSouthSpawn, out Vector3 acEastSpawn, out Vector3 acWestSpawn,
            out Vector2 acBmin, out Vector2 acBmax);

        // Personagens ficam pequenos demais no salão de 26x26 da AC — o jogador
        // também aumenta ao entrar (mesma escala do Vitim) e volta ao normal ao sair.
        const float acPlayerScale = 1.6f;
        void ConvDoor(string name, Vector2 triggerPos, Vector3 spawn, Vector3 front)
        {
            var trig = new GameObject(name);
            trig.transform.SetParent(root, false);
            trig.transform.position = triggerPos;
            var box = trig.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(2.6f, 2.2f);
            var bd = trig.AddComponent<BuildingDoor>();
            bd.roomSpawn = spawn;
            bd.returnPosition = front;
            bd.roomBoundsMin = acBmin;
            bd.roomBoundsMax = acBmax;
            bd.roomLabel = "Convivência (005)";
            bd.playerScale = acPlayerScale;
        }
        ConvDoor("Door_Convivencia_N", new Vector2(acDoorX, convBTop + 0.9f), acNorthSpawn, acNorthFront);
        ConvDoor("Door_Convivencia_S", new Vector2(acDoorX, convBBot - 0.9f), acSouthSpawn, acSouthFront);
        ConvDoor("Door_Convivencia_L", new Vector2(convBR + 0.9f, acSideY), acEastSpawn, acEastFront);
        ConvDoor("Door_Convivencia_O", new Vector2(convBL - 0.9f, acSideY), acWestSpawn, acWestFront);

        // 001–004 — Blocos didáticos: exterior (perspectiva) no campus; entrar faz
        // TRANSIÇÃO para o interior top-down (bloco_pixel) com as 3 salas. Todos
        // funcionam como túnel de dois lados: porta sul E porta norte (hasNorthDoor),
        // dá pra entrar/sair por qualquer lado nos 4 blocos.
        // Blocos 2-4 usam uma "canvasH" maior (15.3 vs 12) porque a arte deles tem
        // a construção ocupando uma fração menor do canvas (~0.61 de altura contra
        // ~0.78 do Bloco 1) — sem compensar, ficavam visivelmente menores que o Bloco 1.
        // Fractions de conteúdo/porta remedidas em 02/07/2026 (arte com correção de
        // estilo) — altura e âncora inferior mantidas iguais à arte anterior, só a
        // largura mudou (arte nova é mais estreita).
        BuildBlocoBuilding(root, "BLOCO 1 (001)", PosBloco1, 12f,
            Bloco1ExtPath, new Vector4(0.286f, 0.098f, 0.714f, 0.878f), 0.500f, 0.878f, true);
        BuildBlocoBuilding(root, "BLOCO 2 (002)", new Vector2(13f, 10f), 15.3f,
            Bloco2ExtPath, new Vector4(0.332f, 0.185f, 0.668f, 0.796f), 0.500f, 0.796f, true);
        BuildBlocoBuilding(root, "BLOCO 3 (003)", PosPortal, 15.3f,
            Bloco34ExtPath, new Vector4(0.340f, 0.184f, 0.660f, 0.796f), 0.500f, 0.796f, true);
        BuildBlocoBuilding(root, "BLOCO 4 (004)", new Vector2(13f, -6f), 15.3f,
            Bloco34ExtPath, new Vector4(0.340f, 0.184f, 0.660f, 0.796f), 0.500f, 0.796f, true);

        // 008 / 009 — Departamentos (prédios fechados, sem interior), com a arte de
        // fachada departamento.png. O 009 fica na mesma coluna do Bloco 3 (x=2),
        // ligado pela Path_Blocos_V + Path_009.
        DepartamentoBuilding(root, "DEPTO. (008)", new Vector2(-24f, -10f), 8f);
        DepartamentoBuilding(root, "DEPTO. (009)", new Vector2(2f, -22f), 9f);

        // Vegetação preenchendo o gramado (clusters, longe de prédios/caminhos).
        ScatterFoliage(root);
    }

    /// <summary>
    /// Espalha árvores/arbustos em CLUSTERS pelo gramado, com posição/escala
    /// aleatórias, evitando prédios, caminhos e a faixa norte (avenida/estac.).
    /// Puramente decorativo (sem colisão) — o jogador atravessa.
    /// </summary>
    private static void ScatterFoliage(Transform root)
    {
        Sprite tree = GetEnvSprite(TreePath, 32f, repeat: false);
        Sprite bush = GetEnvSprite(BushPath, 32f, repeat: false);
        if (tree == null && bush == null) return;

        var group = new GameObject("Foliage").transform;
        group.SetParent(root, false);

        // Áreas proibidas: (x, y, meia-largura, meia-altura) já com margem.
        var blocked = new List<Vector4>();
        void Block(float x, float y, float w, float h, float m)
            => blocked.Add(new Vector4(x, y, w / 2f + m, h / 2f + m));
        // Footprints dos exteriores em perspectiva (base ao sul).
        Block(2, 10, 7, 11, 1.5f); Block(13, 10, 7, 11, 1.5f);
        Block(2, -6, 7, 11, 1.5f); Block(13, -6, 7, 11, 1.5f);
        Block(-32, 2.8f, 18, 9, 1.5f); Block(-6, 22, 5, 4, 1.5f);
        Block(-24, -10, 8, 3, 1.5f); Block(2, -22, 9, 3, 1.5f); // Departamentos 008/009
        Block(-7, 2, 11, 10, 1.5f);                      // Convivência / spawn
        Block(-16.5f, 2f, 15f, 5f, 1f);                  // Path_RU_Conv (04/07/2026)
        Block(1, 2, 28, 5, 1f); Block(2, -2, 4, 28, 1f); // caminhos (praça central / coluna esq.)
        Block(2, -19, 4, 8, 1f); Block(-6, 15, 4, 14, 1f);
        Block(13, -4.5f, 4, 21, 1f); Block(7.5f, 16.35f, 32.9f, 16.1f, 1f); // coluna dir. / saída norte 1-2 (caminho_cima.png)
        Block(-6, -6, 3, 3, 2f);                        // portal da prova

        bool Free(float x, float y)
        {
            if (y > 24f) return false; // avenida/estacionamento livres
            foreach (var b in blocked)
                if (Mathf.Abs(x - b.x) < b.z && Mathf.Abs(y - b.y) < b.w) return false;
            return true;
        }

        Random.InitState(20260701);
        const int clusters = 95;
        for (int i = 0; i < clusters; i++)
        {
            float cx = Random.Range(MapXMin + 2f, MapXMax - 2f);
            float cy = Random.Range(MapYMin + 2f, 24f);
            if (!Free(cx, cy)) continue;

            if (tree != null && Random.value < 0.7f)
                PlaceFoliage(group, "Tree", tree, cx, cy, Random.Range(0.85f, 1.3f));

            int nb = Random.Range(2, 5);
            for (int k = 0; k < nb && bush != null; k++)
            {
                float bx = cx + Random.Range(-3f, 3f);
                float by = cy + Random.Range(-3f, 3f);
                if (Free(bx, by))
                    PlaceFoliage(group, "Bush", bush, bx, by, Random.Range(0.7f, 1.15f));
            }
        }
    }

    private static void PlaceFoliage(Transform parent, string name, Sprite sprite,
        float x, float y, float scale)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, y, 0f);
        go.transform.localScale = new Vector3(scale, scale, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = -2; // acima do chão/caminhos, abaixo dos prédios e do jogador
    }

    /// <summary>Sprite renderizado em modo Tiled (repete a arte para preencher a área).</summary>
    private static GameObject TiledSprite(Transform parent, string name, Vector2 center,
        Vector2 worldSize, Sprite sprite, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = center;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = worldSize;
        sr.sortingOrder = sortingOrder;
        return go;
    }

    /// <summary>Importa (se preciso) e retorna um sprite de Assets/Art/Env.</summary>
    private static Sprite GetEnvSprite(string path, float ppu, bool repeat)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { Debug.LogWarning($"[Calouro] Não achei {path}."); return null; }
        }

        bool changed = false;
        if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; changed = true; }
        if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; changed = true; }
        if (!Mathf.Approximately(importer.spritePixelsPerUnit, ppu)) { importer.spritePixelsPerUnit = ppu; changed = true; }
        var wm = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        if (importer.wrapMode != wm) { importer.wrapMode = wm; changed = true; }
        // Pixel-art nítido: Point (sem bilinear), sem mipmaps, sem compressão.
        if (importer.filterMode != FilterMode.Point) { importer.filterMode = FilterMode.Point; changed = true; }
        if (importer.mipmapEnabled) { importer.mipmapEnabled = false; changed = true; }
        if (importer.textureCompression != TextureImporterCompression.Uncompressed) { importer.textureCompression = TextureImporterCompression.Uncompressed; changed = true; }

        var st = new TextureImporterSettings();
        importer.ReadTextureSettings(st);
        if (st.spriteMeshType != SpriteMeshType.FullRect) { st.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(st); changed = true; }

        if (changed) importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    /// <summary>Importa (se preciso) e retorna um clipe de áudio de Assets/Audio.</summary>
    private static AudioClip GetAudioClip(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as AudioImporter;
        if (importer == null)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) { Debug.LogWarning($"[Calouro] Não achei {path}."); return null; }
        }
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }

    /// <summary>
    /// Prédio COBERTO visto por fora: telhado opaco (peça única) + porta.
    /// É sólido (não dá pra entrar andando). Se hasInterior, cria uma sala numa
    /// região afastada e uma porta (BuildingDoor) que troca de tela ao apertar E.
    /// door: N/S/L/O = lado da porta; 'X' = sem porta (fechado).
    /// Retorna a posição em frente à porta (útil para portais/objetivos).
    /// </summary>
    private static Vector2 CoveredBlock(Transform root, string label, Vector2 center, Vector2 size,
        Color roofColor, char door, bool hasInterior)
    {
        float hx = size.x / 2f, hy = size.y / 2f;
        float top = center.y + hy, bottom = center.y - hy;
        float left = center.x - hx, right = center.x + hx;

        // Contorno escuro (borda do prédio).
        CreateQuad(root, "Edge_" + label, center, new Vector2(size.x + 0.5f, size.y + 0.5f),
            new Color(0.08f, 0.09f, 0.12f), s_white, -1, false);

        // Telhado opaco em peça única + colisão sólida (BoxCollider2D no quad).
        CreateQuad(root, "Roof_" + label, center, size, roofColor, s_white, 0, true);

        // Posição da porta (na borda) e da frente (fora do prédio).
        Vector2 doorPos, frontPos; Vector2 frontStand;
        switch (door)
        {
            case 'N': doorPos = new Vector2(center.x, top); frontStand = new Vector2(center.x, top + 1.4f); break;
            case 'S': doorPos = new Vector2(center.x, bottom); frontStand = new Vector2(center.x, bottom - 1.4f); break;
            case 'E': doorPos = new Vector2(right, center.y); frontStand = new Vector2(right + 1.4f, center.y); break;
            case 'W': doorPos = new Vector2(left, center.y); frontStand = new Vector2(left - 1.4f, center.y); break;
            default: doorPos = center; frontStand = center; break;
        }
        frontPos = frontStand;

        // Sprite de porta na frente do prédio.
        Sprite doorS = CampusAssets.Get("doorClosed");
        if (doorS != null && door != 'X')
            Prop(root, label + "_door", doorPos, doorS, 2, 1.2f);

        if (hasInterior && door != 'X')
        {
            var (spawn, bmin, bmax) = BuildInteriorRoom(label);

            var trig = new GameObject("Door_" + label);
            trig.transform.SetParent(root, false);
            trig.transform.position = frontPos;
            var box = trig.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(2.4f, 2.4f);
            var bd = trig.AddComponent<BuildingDoor>();
            bd.roomSpawn = spawn;
            bd.returnPosition = new Vector3(frontStand.x, frontStand.y, 0f);
            bd.roomBoundsMin = bmin;
            bd.roomBoundsMax = bmax;
            bd.roomLabel = label;
        }

        Label(root, label, new Vector2(center.x, center.y), new Color(0.96f, 0.96f, 0.88f));
        return frontPos;
    }

    /// <summary>
    /// Prédio de departamento (fachada perspectiva departamento.png), fechado e sem
    /// interior — substitui os antigos placeholders "DEP.". Coloca a arte e uma
    /// colisão sólida só no corpo desenhado (conteúdo x 0.140–0.864, y 0.372–0.557,
    /// mesmo padrão da guarita/convivência). visibleWidth = largura do prédio em unidades.
    /// </summary>
    private static void DepartamentoBuilding(Transform root, string label, Vector2 center, float visibleWidth)
    {
        float canvas = visibleWidth / 0.724f;
        Sprite art = GetEnvSprite(DepartamentoExtPath, 100f, repeat: false);
        if (art != null)
        {
            StretchedSprite(root, "Ext_" + label, center, new Vector2(canvas, canvas), art, 3, Color.white);
            float dl = center.x + (0.140f - 0.5f) * canvas;
            float dr = center.x + (0.864f - 0.5f) * canvas;
            float dtop = center.y + (0.5f - 0.372f) * canvas;
            float dbot = center.y + (0.5f - 0.557f) * canvas;
            CreateQuad(root, "DepCol_" + label, new Vector2((dl + dr) / 2f, (dbot + dtop) / 2f),
                new Vector2(dr - dl, dtop - dbot), new Color(0f, 0f, 0f, 0f), s_white, 0, true);
            // Rótulo sobre o corpo do prédio (conteúdo centrado em y-frac 0.4645).
            Label(root, label, new Vector2(center.x, center.y + (0.5f - 0.4645f) * canvas),
                new Color(0.96f, 0.96f, 0.88f));
        }
        else
        {
            CoveredBlock(root, label, center, new Vector2(visibleWidth, 3f),
                new Color(0.42f, 0.48f, 0.30f), 'X', false);
        }
    }

    private static void EnsureInteriorsRoot()
    {
        if (interiorsRoot != null) return;
        var go = GameObject.Find("Interiors");
        if (go != null) Object.DestroyImmediate(go);
        interiorsRoot = new GameObject("Interiors").transform;
    }

    /// <summary>
    /// Posições de "frente" (fora do prédio) nos lados sul e norte, calculadas a
    /// partir do canvas/conteúdo — usadas tanto pelas portas (BuildExterior) quanto
    /// pelos tapetes de saída do interior (BuildBlocoInterior), para os dois lados
    /// sempre concordarem sobre onde cada saída deixa o jogador no mundo.
    /// </summary>
    private static (Vector3 south, Vector3 north) BlocoFrontPositions(Vector2 center, float canvasH,
        Vector4 content, float doorNormX, float doorBottomNormY)
    {
        float canvasW = canvasH * 0.8f;
        float doorX = center.x + (doorNormX - 0.5f) * canvasW;
        float doorGY = center.y + (0.5f - doorBottomNormY) * canvasH;
        float ctop = center.y + (0.5f - content.y) * canvasH;
        Vector3 south = new Vector3(doorX, doorGY - 2.6f, 0f);
        Vector3 north = new Vector3(center.x, ctop + 2.6f, 0f);
        return (south, north);
    }

    /// <summary>
    /// Bloco: exterior no campus + interior por transição de tela. Funciona como
    /// um túnel: entra pela porta sul (sempre existe) e pode sair pelo tapete norte
    /// do corredor, chegando do outro lado do prédio. Se hasNorthDoor, também dá
    /// para ENTRAR pelo lado norte (sem porta visível) — usado nos Blocos 3 e 4.
    /// </summary>
    private static void BuildBlocoBuilding(Transform root, string label, Vector2 center, float canvasH,
        string extPath, Vector4 content, float doorNormX, float doorBottomNormY, bool hasNorthDoor)
    {
        var (southFront, northFront) = BlocoFrontPositions(center, canvasH, content, doorNormX, doorBottomNormY);
        var (southSpawn, northSpawn, bmin, bmax) = BuildBlocoInterior(label, southFront, northFront);
        // Personagens ficam pequenos demais no corredor do bloco — mesmo ajuste de
        // escala (1.6x) já feito na AC e na sala de aula.
        BuildExterior(root, label, center, canvasH, extPath, content, doorNormX, doorBottomNormY,
            southSpawn, bmin, bmax, hasNorthDoor ? (Vector3?)northSpawn : null, playerScale: 1.6f);
    }

    /// <summary>
    /// RU: exterior (lateral) no campus + refeitório por transição de tela.
    /// Diferente de BuildExterior (usado pelos Blocos): a porta fica no lado
    /// LESTE do prédio (de frente pra Convivência), não no sul — decisão de
    /// 04/07/2026, pra o RU se afastar da Convivência com um caminho entre os
    /// dois. doorNormY = fração vertical (0 topo, 1 base) do conteúdo onde a
    /// porta fica.
    /// </summary>
    private static void BuildRUBuilding(Transform root, string label, Vector2 center, float canvasH,
        string extPath, Vector4 content, float doorNormY)
    {
        var (spawn, bmin, bmax) = BuildRUInterior(label);

        float canvasW = canvasH * 0.8f; // artes externas são 1122x1402 (0.8)
        Vector2 size = new Vector2(canvasW, canvasH);

        Sprite art = GetEnvSprite(extPath, 100f, repeat: false);
        if (art != null)
            StretchedSprite(root, "Ext_" + label, center, size, art, 3, Color.white);
        else
            CreateQuad(root, "Ext_" + label, center, size, new Color(0.5f, 0.6f, 0.4f), s_white, 3, false);

        // Retângulo do conteúdo visível no mundo (canvas: y cresce pra baixo).
        float cl = center.x + (content.x - 0.5f) * canvasW;
        float cr = center.x + (content.z - 0.5f) * canvasW;
        float ctop = center.y + (0.5f - content.y) * canvasH;
        float cbot = center.y + (0.5f - content.w) * canvasH;

        // Colisão sólida sobre o corpo do prédio.
        CreateQuad(root, "ExtCol_" + label, new Vector2((cl + cr) / 2f, (ctop + cbot) / 2f),
            new Vector2(cr - cl, ctop - cbot), new Color(0f, 0f, 0f, 0f), s_white, 0, true);

        // Gatilho da porta, agora do lado LESTE (à direita da fachada), virado
        // pra Convivência/o novo caminho.
        float doorY = ctop + (cbot - ctop) * doorNormY;
        var trig = new GameObject("Door_" + label);
        trig.transform.SetParent(root, false);
        trig.transform.position = new Vector3(cr + 0.9f, doorY, 0f);
        var box = trig.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(2.2f, 2.6f);
        var bd = trig.AddComponent<BuildingDoor>();
        bd.roomSpawn = spawn;
        bd.returnPosition = new Vector3(cr + 2.6f, doorY, 0f); // mais a leste (não re-entra)
        bd.roomBoundsMin = bmin;
        bd.roomBoundsMax = bmax;
        bd.roomLabel = label;

        Label(root, label, new Vector2(center.x, ctop + 0.8f), new Color(0.96f, 0.96f, 0.88f));
    }

    /// <summary>
    /// Desenha um prédio em PERSPECTIVA no campus (arte 1122x1402), com colisão
    /// sólida sobre o corpo e um gatilho de porta (E) na base que faz a TRANSIÇÃO
    /// de tela para o interior (spawn/limites vindos do montador do interior).
    /// content = (nx0, ny0, nx1, ny1) do conteúdo visível dentro do canvas.
    /// </summary>
    private static void BuildExterior(Transform root, string label, Vector2 center, float canvasH,
        string extPath, Vector4 content, float doorNormX, float doorBottomNormY,
        Vector3 spawn, Vector2 bmin, Vector2 bmax, Vector3? northSpawn = null, float playerScale = 1f)
    {
        float canvasW = canvasH * 0.8f; // artes externas são 1122x1402 (0.8)
        Vector2 size = new Vector2(canvasW, canvasH);

        Sprite art = GetEnvSprite(extPath, 100f, repeat: false);
        if (art != null)
            StretchedSprite(root, "Ext_" + label, center, size, art, 3, Color.white);
        else
            CreateQuad(root, "Ext_" + label, center, size, new Color(0.5f, 0.6f, 0.4f), s_white, 3, false);

        // Retângulo do conteúdo visível no mundo (canvas: y cresce pra baixo).
        float cl = center.x + (content.x - 0.5f) * canvasW;
        float cr = center.x + (content.z - 0.5f) * canvasW;
        float ctop = center.y + (0.5f - content.y) * canvasH;
        float cbot = center.y + (0.5f - content.w) * canvasH;

        // Colisão sólida sobre o corpo do prédio.
        CreateQuad(root, "ExtCol_" + label, new Vector2((cl + cr) / 2f, (ctop + cbot) / 2f),
            new Vector2(cr - cl, ctop - cbot), new Color(0f, 0f, 0f, 0f), s_white, 0, true);

        // Gatilho da porta, logo à frente (ao sul) da base do prédio.
        float doorX = center.x + (doorNormX - 0.5f) * canvasW;
        float doorGY = center.y + (0.5f - doorBottomNormY) * canvasH;
        var trig = new GameObject("Door_" + label);
        trig.transform.SetParent(root, false);
        trig.transform.position = new Vector3(doorX, doorGY - 0.9f, 0f);
        var box = trig.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(2.6f, 2.2f);
        var bd = trig.AddComponent<BuildingDoor>();
        bd.roomSpawn = spawn;
        bd.returnPosition = new Vector3(doorX, doorGY - 2.6f, 0f); // ao sul do gatilho (não re-entra)
        bd.roomBoundsMin = bmin;
        bd.roomBoundsMax = bmax;
        bd.roomLabel = label;
        bd.playerScale = playerScale;

        // Porta norte (só existe se o bloco funcionar como túnel de dois lados —
        // Blocos 3 e 4). Sem sprite de porta, igual à porta sul.
        if (northSpawn.HasValue)
        {
            var (_, northFront) = BlocoFrontPositions(center, canvasH, content, doorNormX, doorBottomNormY);
            var ntrig = new GameObject("DoorNorte_" + label);
            ntrig.transform.SetParent(root, false);
            ntrig.transform.position = new Vector3(center.x, ctop + 0.9f, 0f);
            var nbox = ntrig.AddComponent<BoxCollider2D>();
            nbox.isTrigger = true;
            nbox.size = new Vector2(2.6f, 2.2f);
            var nbd = ntrig.AddComponent<BuildingDoor>();
            nbd.roomSpawn = northSpawn.Value;
            nbd.returnPosition = northFront;
            nbd.roomBoundsMin = bmin;
            nbd.roomBoundsMax = bmax;
            nbd.roomLabel = label;
            nbd.playerScale = playerScale;
        }

        Label(root, label, new Vector2(center.x, ctop + 0.8f), new Color(0.96f, 0.96f, 0.88f));
    }

    /// <summary>
    /// Monta o INTERIOR de um bloco (arte top-down bloco_pixel) numa região afastada.
    /// Corredor central caminhável, vasos com colisão, 3 portas do lado direito que
    /// levam a salas de aula, e DOIS tapetes de saída (RoomExit) — um ao sul e um ao
    /// norte — que funcionam como um túnel: cada um sempre leva para o respectivo
    /// lado de fora do prédio (southExit/northExit), não importa por onde se entrou.
    /// Retorna (spawn sul, spawn norte, limite mín, limite máx) da câmera.
    /// </summary>
    private static (Vector3, Vector3, Vector2, Vector2) BuildBlocoInterior(string label,
        Vector3 southExit, Vector3 northExit)
    {
        EnsureInteriorsRoot();
        Vector2 c = new Vector2(400f + interiorBldgCounter * 70f, -600f);
        interiorBldgCounter++;

        Vector2 size = new Vector2(18f, 27f);
        float hx = size.x / 2f, hy = size.y / 2f;
        float top = c.y + hy, bottom = c.y - hy;
        float leftEdge = c.x - hx, rightEdge = c.x + hx;
        Color clear = new Color(0f, 0f, 0f, 0f);

        Sprite art = GetBlocoSprite();
        if (art != null)
            StretchedSprite(interiorsRoot, "Corredor_" + label, c, size, art, -10, Color.white);
        else
            CreateQuad(interiorsRoot, "Corredor_" + label, c, size, new Color(0.3f, 0.3f, 0.32f), s_white, -10, false);

        float corridorLeft = c.x - 0.20f * size.x;
        float corridorRight = c.x + 0.20f * size.x;
        // Laterais sólidas.
        CreateQuad(interiorsRoot, "CkL_" + label, new Vector2((leftEdge + corridorLeft) / 2f, c.y),
            new Vector2(corridorLeft - leftEdge, size.y), clear, s_white, 0, true);
        CreateQuad(interiorsRoot, "CkR_" + label, new Vector2((corridorRight + rightEdge) / 2f, c.y),
            new Vector2(rightEdge - corridorRight, size.y), clear, s_white, 0, true);
        float gapHalf = 0.085f * size.x;
        float gapL = c.x - gapHalf, gapR = c.x + gapHalf;

        // Topo com vão central (funciona como túnel: tapete de saída pro lado norte).
        CreateQuad(interiorsRoot, "CkTopL_" + label, new Vector2((corridorLeft + gapL) / 2f, top - 0.3f),
            new Vector2(gapL - corridorLeft, 0.6f), clear, s_white, 0, true);
        CreateQuad(interiorsRoot, "CkTopR_" + label, new Vector2((gapR + corridorRight) / 2f, top - 0.3f),
            new Vector2(corridorRight - gapR, 0.6f), clear, s_white, 0, true);
        var topMat = CreateQuad(interiorsRoot, "CExitTop_" + label, new Vector2(c.x, top - 1.1f),
            new Vector2(gapHalf * 1.8f, 1.0f), new Color(0.3f, 1f, 0.4f, 0.3f), s_white, -9, false);
        var topCol = topMat.AddComponent<BoxCollider2D>();
        topCol.isTrigger = true;
        var topExit = topMat.AddComponent<RoomExit>();
        topExit.useOverridePosition = true;
        topExit.overridePosition = northExit;

        // Base com vão central (entrada/saída original, ao sul) e tapete de saída.
        CreateQuad(interiorsRoot, "CkBotL_" + label, new Vector2((corridorLeft + gapL) / 2f, bottom + 0.3f),
            new Vector2(gapL - corridorLeft, 0.6f), clear, s_white, 0, true);
        CreateQuad(interiorsRoot, "CkBotR_" + label, new Vector2((gapR + corridorRight) / 2f, bottom + 0.3f),
            new Vector2(corridorRight - gapR, 0.6f), clear, s_white, 0, true);
        var mat = CreateQuad(interiorsRoot, "CExit_" + label, new Vector2(c.x, bottom + 1.1f),
            new Vector2(gapHalf * 1.8f, 1.0f), new Color(0.3f, 1f, 0.4f, 0.3f), s_white, -9, false);
        var mcol = mat.AddComponent<BoxCollider2D>();
        mcol.isTrigger = true;
        var southExitComp = mat.AddComponent<RoomExit>();
        southExitComp.useOverridePosition = true;
        southExitComp.overridePosition = southExit;

        // Vasos (colisão): 2 colunas × 3 fileiras.
        var pots = new[]
        {
            new Vector2(0.337f, 0.086f), new Vector2(0.652f, 0.086f),
            new Vector2(0.337f, 0.372f), new Vector2(0.652f, 0.372f),
            new Vector2(0.337f, 0.671f), new Vector2(0.652f, 0.671f),
        };
        for (int p = 0; p < pots.Length; p++)
        {
            float px = leftEdge + pots[p].x * size.x;
            float py = top - pots[p].y * size.y;
            CreateQuad(interiorsRoot, $"Vaso_{label}_{p}", new Vector2(px, py),
                new Vector2(0.6f, 0.45f), clear, s_white, 0, true);
        }

        // NPCs de ambiente no corredor (papo simples, sem ligação com a quest
        // principal). Yasmin anda até 4 passos (NpcPatrol); Enzo fica parado.
        // Vitim mudou pra Convivência (ver BuildConvivenciaInterior), na mesa de pingpong.
        if (label == "BLOCO 3 (003)")
            CreateAmbientNpc(interiorsRoot, "yasmin.png", new Vector2(c.x, c.y), "Yasmin", "yasmin",
                new[]
                {
                    "Oi! Você também tá tentando decorar o mapa do campus?",
                    "Eu ainda me perco tentando achar o Bloco 4.",
                },
                patrolDir: Vector2.up,
                choiceQuestion: "Ela te pede uma ajuda com a lista de FUP. Você ajuda?",
                optionA: "Claro, bora ver isso.", optionB: "Agora não, tô sem tempo.",
                replyA: "Aê! Você me salvou. Depois te pago um café.",
                replyB: "Tranquilo, depois eu tento de novo. Valeu mesmo assim!",
                scale: 1.6f, ethicsRewardA: 1.0f, ethicsRewardB: 0f,
                repeatLines: new[]
                {
                    new[] { "Oi de novo! Ainda tô decorando esse mapa do campus, viu." },
                    new[] { "E aí! Achou o Bloco 4 mais fácil de achar agora?" },
                });
        else if (label == "BLOCO 4 (004)")
            CreateAmbientNpc(interiorsRoot, "enzo.png", new Vector2(c.x, c.y), "Enzo", "enzo",
                new[]
                {
                    "E aí! Sou o Enzo, do Bloco 4.",
                    "Perdi a aula de ontem e tô sem o material... tava vendo se alguém me empresta as anotações.",
                },
                choiceQuestion: "Você empresta suas anotações pro Enzo?",
                optionA: "Claro, te mando tudo hoje!", optionB: "Ah, eu ainda preciso delas.",
                replyA: "Mano, salvou demais! Semana que vem eu te retribuo.",
                replyB: "Sem problema, eu me viro. Valeu mesmo assim!",
                scale: 1.6f, ethicsRewardA: 1.0f, ethicsRewardB: 0f,
                repeatLines: new[]
                {
                    new[] { "E aí! Valeu de novo pelas anotações." },
                    new[] { "Oi! Bloco 4 continua um labirinto pra mim, confesso." },
                });

        // 3 portas do lado direito → salas de aula.
        float[] dy = { 0.307f, 0.0185f, -0.326f };
        for (int i = 0; i < dy.Length; i++)
        {
            float doorY = c.y + dy[i] * size.y;
            string salaLabel = label + " — Sala " + (i + 1);
            var (sspawn, sbmin, sbmax) = BuildInteriorRoom(salaLabel);

            // Rainara (professora de IHC) fica na Sala 1 do Bloco 1 — é a ÚNICA
            // sala liberada por enquanto (ClassSchedule.CurrentRoomId). As outras
            // mostram um "pensamento" de sala errada em vez de abrir (ver BuildingDoor).
            // Professores das aulas do Dia 1. A sala liberada de cada momento é
            // controlada em runtime pelo sistema de objetivos (ver QuestManager /
            // ClassSchedule); a atribuição de edição abaixo é só um valor inicial.
            // Personagens em 1.6x (arte da sala é "de perto").
            if (label == "BLOCO 1 (001)" && i == 0)
            {
                ClassSchedule.CurrentRoomId = salaLabel;
                ClassSchedule.CurrentRoomLabel = "IHC com a Rainara (Bloco 1, Sala 1)";

                float rx = (sbmin.x + sbmax.x) / 2f;
                CreateAmbientNpc(interiorsRoot, "rainara.png", new Vector2(rx, sbmax.y - 6f), "Rainara", "rainara",
                    new[]
                    {
                        "Ah, você é o novo calouro! Bem-vindo à aula de Interação Humano-Computador.",
                        "O resumo de hoje é simples: IHC é pensar em QUEM vai usar o sistema — não só em fazer funcionar.",
                        "Boa aula! E não perca a próxima: você tem Matemática Básica agora, com o professor Aragão, no Bloco 2, Sala 1.",
                    },
                    choiceQuestion: "Já pensou em como isso vai te ajudar no curso?",
                    optionA: "Parece interessante!", optionB: "Ainda não sei bem o que esperar.",
                    replyA: "Ótimo! Você vai gostar das aulas, então.",
                    replyB: "Sem pressa — isso fica mais claro com o tempo. Bons estudos!",
                    scale: 1.6f,
                    examObjective: "prova_ihc", examKind: "ihc",
                    examLines: new[]
                    {
                        "Semanas se passaram e chegou a hora da avaliação de IHC.",
                        "Como a nossa disciplina é mais de reflexão, avaliei sua participação e as discussões em aula.",
                        "Pronto: sua nota de IHC já está registrada. Mandou bem!",
                    },
                    repeatLines: new[]
                    {
                        new[] { "Oi de novo! Como estão indo os estudos?", "Continue de olho em quem vai usar o que você constrói — isso é IHC na prática." },
                        new[] { "Voltou pra rever a matéria? Que bom te ver por aqui.", "Qualquer dúvida sobre a disciplina, pode aparecer na sala." },
                    });
            }
            else if (label == "BLOCO 2 (002)" && i == 0)
            {
                float ax = (sbmin.x + sbmax.x) / 2f;
                CreateAmbientNpc(interiorsRoot, "aragao.png", new Vector2(ax, sbmax.y - 6f), "Aragão", "aragao",
                    new[]
                    {
                        "Bom dia! Eu sou o professor Aragão, de Matemática Básica.",
                        "Não se assuste com o nome — a ideia é reforçar a base: lógica, frações, um pouco de raciocínio.",
                        "Preste atenção nas listas: elas caem direto na prova — aquele nosso 'labirinto' no fim do módulo.",
                    },
                    choiceQuestion: "Matemática te dá um frio na barriga?",
                    optionA: "Um pouco, confesso.", optionB: "Não, eu curto!",
                    replyA: "Normal. Com prática melhora — e eu tô aqui pra isso.",
                    replyB: "Ótimo! Então vai se dar bem no labirinto.",
                    scale: 1.6f,
                    examObjective: "prova_mat", examKind: "mat",
                    examLines: new[]
                    {
                        "Chegou a prova de Matemática Básica!",
                        "São 4 labirintos em sequência, cada um valendo 2,5 pontos — e vão ficando mais difíceis.",
                        "Resolve rápido que a nota de cada um é melhor. Boa sorte!",
                    },
                    altLines: new[]
                    {
                        new NpcInteractable.ObjectiveLineSet
                        {
                            objectiveId = "notebook_prof",
                            lines = new[]
                            {
                                "Ei, calouro! Que bom que passou por aqui de novo.",
                                "Confesso que tô com um probleminha: sumiu meu caderno de anotações — tinha até um esboço de questões pra próxima prova.",
                                "Acho que esqueci ele no RU, na correria depois da última prova. Você topa dar uma olhada lá?",
                                "Vou te esperando na minha sala — Bloco 2, Sala 1. Quando encontrar o caderno, é só me trazer lá. Valeu mesmo!",
                            },
                        },
                        new NpcInteractable.ObjectiveLineSet
                        {
                            objectiveId = "notebook_devolucao",
                            lines = new[]
                            {
                                "Opa, meu caderno! Você achou mesmo!",
                                "Muito obrigado, calouro. Isso me economizou um baita perrengue.",
                            },
                        },
                    },
                    repeatLines: new[]
                    {
                        new[] { "E aí, calouro! Como estão os estudos de Matemática?", "Aquele labirinto ensina mais do que parece, hein?" },
                        new[] { "Valeu de novo por aquele dia do caderno.", "Continue treinando lógica — ajuda até fora da minha matéria." },
                    });
            }
            else if (label == "BLOCO 2 (002)" && i == 1)
            {
                // Laboratório: onde o caderno perdido do Aragão aparece (SQ1, 3.9).
                // Sem arte própria ainda — um objeto simples (quadrado + interação),
                // já que não é um personagem (sem folha de sprites 4x3).
                float nx = (sbmin.x + sbmax.x) / 2f;
                float ny = sbmax.y - 6f;
                var notebookGO = CreateQuad(interiorsRoot, "NotebookObjeto", new Vector2(nx, ny),
                    new Vector2(0.6f, 0.5f), new Color(0.85f, 0.7f, 0.2f), s_white, 5, false);
                var nCol = notebookGO.AddComponent<CircleCollider2D>();
                nCol.isTrigger = true;
                nCol.radius = 1.6f;
                var nInteract = notebookGO.AddComponent<NpcInteractable>();
                nInteract.npcName = "Caderno";
                nInteract.npcId = "notebook_objeto";
                nInteract.lines = new[]
                {
                    "Um caderno emborcado embaixo de uma bancada, cheio de anotações de Matemática.",
                    "Só pode ser do professor Aragão — bora devolver.",
                };
                nInteract.repeatLines = new[]
                {
                    new NpcInteractable.LineSet { lines = new[] { "A bancada agora está vazia — o caderno já foi devolvido." } },
                };
            }
            else if (label == "BLOCO 3 (003)" && i == 0)
            {
                float px = (sbmin.x + sbmax.x) / 2f;
                CreateAmbientNpc(interiorsRoot, "paulete.png", new Vector2(px, sbmax.y - 6f), "Paulyne", "paulete",
                    new[]
                    {
                        "Oi, calouro! Eu sou a professora Paulyne, de Fundamentos da Programação.",
                        "Aqui a gente aprende a pensar como um programador: dividir o problema em passos e resolver um de cada vez.",
                        "Na avaliação você vai montar a solução de um probleminha — nada de decoreba, é raciocínio.",
                    },
                    choiceQuestion: "Já mexeu com programação antes?",
                    optionA: "Só um pouquinho.", optionB: "Nunca, é tudo novo.",
                    replyA: "Ótima base! A gente aprofunda daqui.",
                    replyB: "Melhor ainda — sem vícios. Vai por mim, você pega rápido.",
                    scale: 1.6f,
                    examObjective: "prova_fup", examKind: "fup",
                    examLines: new[]
                    {
                        "Hora da prova de Fundamentos da Programação!",
                        "Vou te dar um probleminha: você monta a solução colocando os passos na ordem certa. Bora?",
                    },
                    repeatLines: new[]
                    {
                        new[] { "Oi de novo! Continue treinando a lógica de programação.", "Dividir problemas em passos pequenos é a chave." },
                        new[] { "Como estão indo os estudos de FUP?", "Qualquer dúvida sobre lógica, pode aparecer na sala." },
                    });
            }
            else if (label == "BLOCO 4 (004)" && i == 0)
            {
                float jx = (sbmin.x + sbmax.x) / 2f;
                CreateAmbientNpc(interiorsRoot, "jeferson.png", new Vector2(jx, sbmax.y - 6f), "Jeferson", "jeferson_prova",
                    new[]
                    {
                        "Opa! Além de coordenador, eu também sou professor de Introdução à Engenharia de Software.",
                        "A prova de IES acontece aqui nesta sala, no período de avaliações.",
                    },
                    scale: 1.6f,
                    examObjective: "prova_ies", examKind: "ies",
                    examLines: new[]
                    {
                        "Chegou a prova de Introdução à Engenharia de Software!",
                        "São algumas perguntas objetivas sobre os conceitos que a gente viu. Responde com calma.",
                    },
                    repeatLines: new[]
                    {
                        new[] { "Opa, calouro! Como está se adaptando ao curso?", "Engenharia de Software é sobre processo — não esquece disso." },
                        new[] { "Precisando de alguma orientação? Pode perguntar.", "Bora focar no resto do semestre." },
                    });
            }

            var trig = new GameObject("SalaDoor_" + salaLabel);
            trig.transform.SetParent(interiorsRoot, false);
            trig.transform.position = new Vector3(c.x + 0.145f * size.x, doorY, 0f);
            var box = trig.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(1.6f, 1.8f);
            var bd = trig.AddComponent<BuildingDoor>();
            bd.roomSpawn = sspawn;
            bd.returnPosition = new Vector3(c.x + 0.05f * size.x, doorY, 0f);
            bd.roomBoundsMin = sbmin;
            bd.roomBoundsMax = sbmax;
            bd.roomLabel = salaLabel;
            bd.classroomId = salaLabel;
            // Salas com aula agendada usam o id-constante (mesma fonte que o objetivo
            // usa), pra o gating casar sem risco de divergência de texto/traço.
            if (label == "BLOCO 1 (001)" && i == 0) bd.classroomId = ClassSchedule.RoomIHC;
            else if (label == "BLOCO 2 (002)" && i == 0) bd.classroomId = ClassSchedule.RoomAragao;
            else if (label == "BLOCO 3 (003)" && i == 0) bd.classroomId = ClassSchedule.RoomFUP;
            else if (label == "BLOCO 4 (004)" && i == 0) bd.classroomId = ClassSchedule.RoomIES;
            bd.playerScale = 1.6f; // mesma escala da Rainara — arte da sala é "de perto"
        }

        Label(interiorsRoot, "CORREDOR — " + label, new Vector2(c.x, top + 1.0f), new Color(0.96f, 0.96f, 0.88f));
        Vector3 southSpawn = new Vector3(c.x, bottom + 3.0f, 0f);
        Vector3 northSpawn = new Vector3(c.x, top - 3.0f, 0f);
        return (southSpawn, northSpawn, new Vector2(leftEdge, bottom), new Vector2(rightEdge, top));
    }

    /// <summary>
    /// Monta o INTERIOR do RU (arte top-down ru_interno) numa região afastada. Salão
    /// central caminhável (entre norm 0.242 e 0.708), laterais sólidas, tapete de
    /// saída (RoomExit) na base e o Natan dentro. Retorna (spawn, mín, máx).
    /// </summary>
    private static (Vector3, Vector2, Vector2) BuildRUInterior(string label)
    {
        EnsureInteriorsRoot();
        Vector2 c = new Vector2(400f + interiorBldgCounter * 70f, -600f);
        interiorBldgCounter++;

        Vector2 size = new Vector2(18f, 27f);
        float hx = size.x / 2f, hy = size.y / 2f;
        float top = c.y + hy, bottom = c.y - hy;
        float leftEdge = c.x - hx, rightEdge = c.x + hx;
        Color clear = new Color(0f, 0f, 0f, 0f);

        Sprite art = GetEnvSprite(RUInteriorPath, 100f, repeat: false);
        if (art != null)
            StretchedSprite(interiorsRoot, "Refeitorio_" + label, c, size, art, -10, Color.white);
        else
            CreateQuad(interiorsRoot, "Refeitorio_" + label, c, size, new Color(0.4f, 0.42f, 0.44f), s_white, -10, false);

        // Colisões medidas na arte ru_interno.png (salão centrado): floor caminhável
        // de 0.240 a 0.760 na largura; parede inferior sólida (a saída é o tapete).
        float hallLeft = leftEdge + 0.240f * size.x;
        float hallRight = leftEdge + 0.760f * size.x;
        // Laterais sólidas.
        CreateQuad(interiorsRoot, "RkL_" + label, new Vector2((leftEdge + hallLeft) / 2f, c.y),
            new Vector2(hallLeft - leftEdge, size.y), clear, s_white, 0, true);
        CreateQuad(interiorsRoot, "RkR_" + label, new Vector2((hallRight + rightEdge) / 2f, c.y),
            new Vector2(rightEdge - hallRight, size.y), clear, s_white, 0, true);
        // Topo fechado.
        CreateQuad(interiorsRoot, "RkT_" + label, new Vector2((hallLeft + hallRight) / 2f, top - 0.3f),
            new Vector2(hallRight - hallLeft, 0.6f), clear, s_white, 0, true);
        // Balcão de comida (estrutura sólida perto do topo: x 0.34–0.66, y 0.15–0.29).
        CreateQuad(interiorsRoot, "RkBalcao_" + label,
            new Vector2(c.x, c.y + (0.5f - 0.22f) * size.y),
            new Vector2(0.32f * size.x, 0.14f * size.y), clear, s_white, 0, true);
        // Base: cantos sólidos + vão central (tapete de saída), simétrico ao centro.
        float gL = leftEdge + 0.360f * size.x;
        float gR = leftEdge + 0.640f * size.x;
        CreateQuad(interiorsRoot, "RkBotL_" + label, new Vector2((hallLeft + gL) / 2f, bottom + 0.3f),
            new Vector2(gL - hallLeft, 0.6f), clear, s_white, 0, true);
        CreateQuad(interiorsRoot, "RkBotR_" + label, new Vector2((gR + hallRight) / 2f, bottom + 0.3f),
            new Vector2(hallRight - gR, 0.6f), clear, s_white, 0, true);
        var mat = CreateQuad(interiorsRoot, "RExit_" + label, new Vector2((gL + gR) / 2f, bottom + 1.1f),
            new Vector2((gR - gL) * 0.9f, 1.0f), new Color(0.3f, 1f, 0.4f, 0.3f), s_white, -9, false);
        var mcol = mat.AddComponent<BoxCollider2D>();
        mcol.isTrigger = true;
        mat.AddComponent<RoomExit>();

        // Natan no salão, um pouco à frente da entrada. É a sessão de estudo do
        // Dia 3 (objetivo "estudar_natan"): estudar junto dá +1.0 de Ética e leva
        // ao salto temporal pras provas.
        CreateAmbientNpc(interiorsRoot, "natan.png",
            new Vector2((gL + gR) / 2f, bottom + 5.0f), "Natan", "natan",
            new[]
            {
                "E aí, calouro! Eu sou o Natan.",
                "As primeiras provas tão chegando... Matemática no labirinto, o quiz do Jeferson, aquele problema de FUP...",
                "Tô montando um resumão aqui no RU. Cola comigo que a gente revisa junto.",
            },
            choiceQuestion: "Estudar em grupo pra reta final antes das provas?",
            optionA: "Bora! Junto rende mais.", optionB: "Prefiro revisar sozinho.",
            replyA: "Isso! Duas cabeças pensam melhor. Já já você tá afiado.",
            replyB: "De boa, cada um no seu ritmo. Qualquer dúvida, tô aqui.",
            ethicsRewardA: 1.0f, ethicsRewardB: 0f,
            repeatLines: new[]
            {
                new[] { "Fala, calouro! Bons estudos." },
                new[] { "Se precisar revisar de novo, só chamar." },
            });

        // Gabi, atendente do RU — pista da side quest do notebook (3.9, Dia 28).
        // Arte própria (gabi.png, folha 4x3 já no padrão dos outros NPCs — ver
        // atendente-cantina.png original, recortada em GetCell/composta em
        // 03/07/2026). Pose de lado olha pra ESQUERDA na arte, por isso
        // invertSide: true (mesmo ajuste do Batatinha). Offset de +3.5 em X do
        // Natan pra não sobrepor nos Dias 1–3 (antes do trote levar o Natan embora).
        CreateAmbientNpc(interiorsRoot, "gabi.png",
            new Vector2((gL + gR) / 2f + 3.5f, bottom + 5.0f), "Gabi", "atendente_ru",
            new[]
            {
                "Oi! Eu sou a Gabi, trabalho aqui no RU.",
                "Precisando de alguma coisa?",
            },
            // gabi.png foi recortada de uma folha 7x5 e cada célula ficou bem maior
            // (187px de personagem) que a das outras folhas (108px). Sem correção
            // ela renderiza ~1,73x maior que os demais NPCs — 0.58 ≈ 108/187 iguala
            // a altura dela à do Natan (mesmo RU, escala 1x).
            scale: 0.58f,
            invertSide: true,
            repeatLines: new[]
            {
                new[] { "Precisando de mais alguma coisa?" },
                new[] { "Volta sempre! O RU é praticamente sua segunda casa agora, né?" },
            },
            altLines: new[]
            {
                new NpcInteractable.ObjectiveLineSet
                {
                    objectiveId = "notebook_ru",
                    lines = new[]
                    {
                        "Um caderno? Ah, sim — tinha um esquecido numa mesa outro dia.",
                        "Acho que um aluno do Bloco 2 pegou pra devolver, mas nunca mais vi. Se eu fosse você, dava uma olhada no laboratório de lá.",
                    },
                },
            });

        Label(interiorsRoot, "REFEITÓRIO — " + label, new Vector2(c.x, top + 1.0f), new Color(0.96f, 0.96f, 0.88f));
        Vector3 spawn = new Vector3((gL + gR) / 2f, bottom + 3.0f, 0f);
        return (spawn, new Vector2(leftEdge, bottom), new Vector2(rightEdge, top));
    }

    /// <summary>
    /// Monta o INTERIOR da Convivência (ac_interno.png: mesa de pingpong, mesas,
    /// balcão de lanches) numa região afastada. Diferente dos blocos (só norte/
    /// sul), aqui as 4 direções têm entrada/saída própria: entrar por um lado
    /// aparece no lado correspondente de dentro, e sair devolve pro mesmo lado de
    /// fora — não é um "atalho" de um lado pro outro.
    /// </summary>
    private static void BuildConvivenciaInterior(Vector3 northFront, Vector3 southFront,
        Vector3 eastFront, Vector3 westFront,
        out Vector3 northSpawn, out Vector3 southSpawn, out Vector3 eastSpawn, out Vector3 westSpawn,
        out Vector2 bmin, out Vector2 bmax)
    {
        EnsureInteriorsRoot();
        Vector2 c = new Vector2(400f + interiorBldgCounter * 70f, -600f);
        interiorBldgCounter++;

        // 26x26 (maior que os 20x20 dos blocos) — dá folga suficiente entre os
        // móveis e as 4 saídas sem precisar de paredes/corredores internos.
        const float size = 26f;
        float half = size / 2f;
        float top = c.y + half, bottom = c.y - half, left = c.x - half, right = c.x + half;
        Color clear = new Color(0f, 0f, 0f, 0f);

        Sprite art = GetEnvSprite(ACInteriorPath, 100f, repeat: false);
        if (art != null)
            StretchedSprite(interiorsRoot, "AC_Interior", c, new Vector2(size, size), art, -10, Color.white);
        else
            CreateQuad(interiorsRoot, "AC_Interior", c, new Vector2(size, size), new Color(0.85f, 0.6f, 0.55f), s_white, -10, false);

        // Física dos móveis (retângulos medidos na arte, fração do canvas 0..1;
        // y cresce pra baixo na arte, por isso "top - ny*size").
        void Furniture(string name, float nx0, float ny0, float nx1, float ny1)
        {
            float fl = left + nx0 * size, fr = left + nx1 * size;
            float ft = top - ny0 * size, fb = top - ny1 * size;
            CreateQuad(interiorsRoot, name, new Vector2((fl + fr) / 2f, (ft + fb) / 2f),
                new Vector2(fr - fl, ft - fb), clear, s_white, 0, true);
        }
        Furniture("AC_PingPong", 0.102f, 0.209f, 0.292f, 0.573f);
        // As 4 mesinhas do meio: colisor um pouco menor que a arte (encolhido pra
        // dentro ~0.03 de cada lado) — do jeito que estava medido, o vão entre uma
        // mesa e outra ficava com menos de 1 unidade, impossível de atravessar.
        Furniture("AC_Mesa_TL", 0.449f, 0.127f, 0.574f, 0.262f);
        Furniture("AC_Mesa_TR", 0.702f, 0.127f, 0.827f, 0.262f);
        Furniture("AC_Mesa_BL", 0.449f, 0.351f, 0.574f, 0.485f);
        Furniture("AC_Mesa_BR", 0.702f, 0.351f, 0.827f, 0.485f);
        Furniture("AC_Balcao1", 0.355f, 0.607f, 0.857f, 0.772f);
        Furniture("AC_Balcao2", 0.365f, 0.802f, 0.891f, 0.977f);

        // Saída ao encostar em QUALQUER ponto da borda (não só um tapete pontual) —
        // cada lado sempre volta pro lado de fora correspondente, não importa por
        // onde se entrou. Sem isso o jogador conseguia passar da borda do salão e
        // andar num "void" em vez de sair.
        void ExitEdge(string name, bool horizontal, float edgeCoord, Vector3 front)
        {
            float span = size - 2f; // quase toda a borda, com margem pros cantos
            Vector2 pos = horizontal ? new Vector2(c.x, edgeCoord) : new Vector2(edgeCoord, c.y);
            Vector2 boxSize = horizontal ? new Vector2(span, 1.6f) : new Vector2(1.6f, span);
            var mat = CreateQuad(interiorsRoot, name, pos, boxSize,
                new Color(0.3f, 1f, 0.4f, 0.15f), s_white, -9, false);
            var col = mat.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            var exit = mat.AddComponent<RoomExit>();
            exit.useOverridePosition = true;
            exit.overridePosition = front;
        }
        float pingPongColX = c.x - 8f; // acima/abaixo da mesa de pingpong (fora da altura dela)
        float eastRowY = c.y - 1f;     // faixa livre de móveis do lado leste
        float westRowY = c.y - 5f;     // do lado oeste a faixa livre é mais embaixo (abaixo da mesa)
        ExitEdge("AC_ExitNorte", true, top - 0.8f, northFront);
        ExitEdge("AC_ExitSul", true, bottom + 0.8f, southFront);
        ExitEdge("AC_ExitLeste", false, right - 0.8f, eastFront);
        ExitEdge("AC_ExitOeste", false, left + 0.8f, westFront);

        // Vitim fica parado na frente da mesa de pingpong, desafiando quem passa.
        // Escala maior (1.6x) porque os personagens ficam pequenos demais nesse
        // salão de 26x26 — a arte da AC é mais "de perto" que a dos blocos/RU.
        Vector2 vitimIdleSpot = new Vector2(c.x - 4f, c.y + 2.8f);

        // Posições da partida: centralizadas na LARGURA real da mesa (medida na
        // arte: vai de c.x-10.3 a c.x-5.4, centro em c.x-7.9), uma logo acima da
        // ponta norte (c.y+7.6) e outra logo abaixo da ponta sul (c.y-1.9). A
        // caminhada até lá ignora física (ver VitimPingPongTrigger), então não há
        // problema em cruzar por cima do colisor da mesa no caminho.
        Vector2 vitimTableSpot = new Vector2(c.x - 7.9f, c.y + 8.2f);
        Vector2 playerTableSpot = new Vector2(c.x - 7.9f, c.y - 2.5f);

        var vitim = CreateAmbientNpc(interiorsRoot, "vitim.png", vitimIdleSpot, "Vitim", "vitim",
            new[] { "Ei! Chegou na hora certa." },
            choiceQuestion: "Iai, vai marcar time de fora?",
            optionA: "Bora, to dentro!", optionB: "Agora não, valeu.",
            replyA: "Boa! Só esperar terminar esse ponto.",
            replyB: "Show, quando quiser é só chamar.",
            scale: 1.6f,
            // Repete antes do Dia 4 (trote) — depois disso o TroteChase troca
            // por falas de zoação sobre a corrida, ver TroteChase.Resolve.
            repeatLines: new[]
            {
                new[] { "Bora outra? Sempre tem espaço na mesa." },
                new[] { "E aí, quer jogar de novo?" },
            });

        // Aceitando o convite (opção A), os dois andam até os lados opostos da
        // mesa e o minigame de pingue-pongue carrega (ver VitimPingPongTrigger e
        // o especial-case em DialogueManager.EndDialogue).
        var pingPongTrigger = vitim.gameObject.AddComponent<VitimPingPongTrigger>();
        pingPongTrigger.vitimTableSpot = vitimTableSpot;
        pingPongTrigger.playerTableSpot = playerTableSpot;

        Label(interiorsRoot, "ÁREA DE CONVIVÊNCIA — interior", new Vector2(c.x, top + 1f), new Color(0.96f, 0.96f, 0.88f));

        northSpawn = new Vector3(pingPongColX, top - 3f, 0f);
        southSpawn = new Vector3(pingPongColX, bottom + 3f, 0f);
        eastSpawn = new Vector3(right - 4f, eastRowY, 0f);
        westSpawn = new Vector3(left + 3f, westRowY, 0f);
        bmin = new Vector2(left, bottom);
        bmax = new Vector2(right, top);
    }

    /// <summary>Garante que a arte do bloco está importada como Sprite e a retorna.</summary>
    private static Sprite GetBlocoSprite()
    {
        var importer = AssetImporter.GetAtPath(BlocoInteriorPath) as TextureImporter;
        if (importer == null)
        {
            AssetDatabase.ImportAsset(BlocoInteriorPath, ImportAssetOptions.ForceSynchronousImport);
            importer = AssetImporter.GetAtPath(BlocoInteriorPath) as TextureImporter;
            if (importer == null) { Debug.LogWarning($"[Calouro] Não achei {BlocoInteriorPath}."); return null; }
        }

        bool changed = false;
        if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; changed = true; }
        if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; changed = true; }
        // Pixel-art nítido.
        if (importer.filterMode != FilterMode.Point) { importer.filterMode = FilterMode.Point; changed = true; }
        if (importer.mipmapEnabled) { importer.mipmapEnabled = false; changed = true; }
        if (importer.textureCompression != TextureImporterCompression.Uncompressed) { importer.textureCompression = TextureImporterCompression.Uncompressed; changed = true; }
        if (changed) importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(BlocoInteriorPath);
    }

    /// <summary>
    /// Monta uma SALA DE AULA numa região afastada (interiores ficam lado a lado,
    /// longe do campus), usando a arte única `sala_aula.png` (paredes, lousa, mesa
    /// do professor e carteiras já desenhadas). Só a porta (embaixo, centralizada)
    /// é passável — o resto da borda tem colisão sólida (medida na própria arte).
    /// Retorna (spawn do jogador, limite mín da câmera, limite máx).
    /// </summary>
    private static (Vector3 spawn, Vector2 bmin, Vector2 bmax) BuildInteriorRoom(string label)
    {
        EnsureInteriorsRoot();

        // Salas enfileiradas bem longe do campus (evita sobrepor campus e labirinto).
        Vector2 c = new Vector2(300f + roomCounter * 40f, -300f);
        roomCounter++;

        // sala_aula.png é 718x857 (aspect 0.838) — mantém a mesma proporção aqui.
        const float rw = 14f, rh = 16.7f;
        float hx = rw / 2f, hy = rh / 2f;
        float top = c.y + hy, bottom = c.y - hy;
        float left = c.x - hx, right = c.x + hx;
        Color clear = new Color(0f, 0f, 0f, 0f);

        Sprite art = GetEnvSprite(SalaAulaPath, 100f, repeat: false);
        if (art != null)
            StretchedSprite(interiorsRoot, "RFloor_" + label, c, new Vector2(rw, rh), art, -10, Color.white);
        else
            CreateQuad(interiorsRoot, "RFloor_" + label, c, new Vector2(rw, rh), new Color(0.9f, 0.85f, 0.75f), s_white, -10, false);

        // Colisão sólida da borda (frações medidas pixel a pixel na arte): paredes
        // de ~4.7% dos lados, ~16.7% em cima (já inclui a lousa) e ~9.8% embaixo,
        // com um vão de porta centralizado de ~15% da largura.
        void Wall(string name, float x0, float y0, float x1, float y1)
        {
            CreateQuad(interiorsRoot, name, new Vector2((x0 + x1) / 2f, (y0 + y1) / 2f),
                new Vector2(x1 - x0, y1 - y0), clear, s_white, 0, true);
        }
        float leftIn = left + 0.047f * rw;
        float rightIn = right - 0.047f * rw;
        float topIn = top - 0.167f * rh;
        float bottomIn = bottom + 0.098f * rh;
        float doorX0 = left + 0.423f * rw;
        float doorX1 = left + 0.574f * rw;

        Wall("RWallN_" + label, left, topIn, right, top);
        Wall("RWallW_" + label, left, bottom, leftIn, top);
        Wall("RWallE_" + label, rightIn, bottom, right, top);
        Wall("RWallS1_" + label, left, bottom, doorX0, bottomIn);
        Wall("RWallS2_" + label, doorX1, bottom, right, bottomIn);

        // Física do birô da professora e das 12 carteiras (grade 4x3), medida na
        // arte (frações do canvas 0..1). As carteiras usam um colisor menor que o
        // desenho (só o "núcleo" da cadeira) pra sobrar corredor de verdade entre
        // uma fileira/coluna e outra — meio a meio dá pra transitar.
        void Furniture(float nx0, float ny0, float nx1, float ny1, string name)
        {
            float fl = left + nx0 * rw, fr = left + nx1 * rw;
            float ft = top - ny0 * rh, fb = top - ny1 * rh;
            CreateQuad(interiorsRoot, name, new Vector2((fl + fr) / 2f, (ft + fb) / 2f),
                new Vector2(fr - fl, ft - fb), clear, s_white, 0, true);
        }
        Furniture(0.397f, 0.1925f, 0.606f, 0.3325f, "RBiro_" + label);

        float[] chairColX0 = { 0.1393f, 0.3545f, 0.5704f, 0.7841f };
        float[] chairColX1 = { 0.2228f, 0.4380f, 0.6540f, 0.8677f };
        float[] chairRowY0 = { 0.4078f, 0.5683f, 0.7305f };
        float[] chairRowY1 = { 0.4778f, 0.6383f, 0.8005f };
        for (int row = 0; row < 3; row++)
            for (int col = 0; col < 4; col++)
                Furniture(chairColX0[col], chairRowY0[row], chairColX1[col], chairRowY1[row],
                    $"RCadeira_{label}_{row}_{col}");

        // Tapete de saída (RoomExit) no vão da porta — volta pra onde o jogador
        // entrou. requireInteract=true (só sai apertando E): o spawn dessa sala
        // fica relativamente perto do tapete, e sair automaticamente ao pisar
        // expulsava o jogador de volta assim que ele entrava.
        var mat = CreateQuad(interiorsRoot, "RExit_" + label, new Vector2(c.x, bottom + 0.8f),
            new Vector2(doorX1 - doorX0, 1.6f), new Color(0.3f, 1f, 0.4f, 0.35f), s_white, -9, false);
        var mcol = mat.AddComponent<BoxCollider2D>();
        mcol.isTrigger = true;
        mat.AddComponent<RoomExit>().requireInteract = true;

        Label(interiorsRoot, "SALA — " + label, new Vector2(c.x, top + 1.2f), new Color(0.96f, 0.96f, 0.88f));

        // Bem mais afastado do tapete de saída (que termina em bottom+1.6) do que
        // antes (bottom+2.6) — com o jogador em escala 1.6x nessa sala, a margem
        // curta deixava o colisor dele já nascer encostando no tapete, disparando
        // a saída no mesmo instante em que entrava (parecia que nem tinha entrado).
        Vector3 spawn = new Vector3(c.x, bottom + 4f, 0f);
        return (spawn, new Vector2(left, bottom), new Vector2(right, top));
    }

    /// <summary>Rótulo de texto no mundo (TextMesh).</summary>
    private static void Label(Transform root, string text, Vector2 pos, Color color)
    {
        var go = new GameObject("Label_" + text);
        go.transform.SetParent(root, false);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);

        var tm = go.AddComponent<TextMesh>();
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tm.font = font;
        tm.text = text;
        tm.fontSize = 40;
        tm.characterSize = 0.09f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = color;

        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null && font != null)
        {
            mr.sharedMaterial = font.material;
            mr.sortingOrder = 20;
        }
    }

    private static void SetupCamera()
    {
        var camObj = Camera.main != null ? Camera.main.gameObject : GameObject.Find("Main Camera");
        if (camObj == null) return;

        var cam = camObj.GetComponent<Camera>();
        if (cam != null) cam.orthographicSize = 8f;

        var follow = camObj.GetComponent<CameraFollow2D>() ?? camObj.AddComponent<CameraFollow2D>();
        var player = GameObject.Find("Player");
        if (player != null) follow.target = player.transform;
        follow.useBounds = true;
        follow.boundsMin = new Vector2(MapXMin - WallT, MapYMin - WallT);
        follow.boundsMax = new Vector2(MapXMax + WallT, MapYMax + WallT);
    }

    private static void SetupDialogue()
    {
        if (GameObject.Find("DialogueManager") == null)
        {
            var go = new GameObject("DialogueManager");
            go.AddComponent<DialogueManager>();
        }
    }

    private static void SetupNPCs()
    {
        var old = GameObject.Find("NPCs");
        if (old != null) Object.DestroyImmediate(old);
        var root = new GameObject("NPCs");

        // Natan agora fica DENTRO do RU (criado em BuildRUInterior).
        // Falar com o Jeferson dispara a cutscene do passeio pelo campus (ver
        // DialogueManager.EndDialogue + CampusTourCutscene). Por isso a fala aqui é
        // só uma introdução curta que puxa o tour.
        CreateNpc(root.transform, "NPC_Coordenador", CharsFolder + "/jeferson.png", PosCoordenador, "Coordenador", "coordenador",
            new[]
            {
                "Opa! Você deve ser o calouro novo. Eu sou o Jeferson, coordenador de Engenharia de Software.",
                "Chegou na hora certa — deixa eu te mostrar o campus.",
            });

        // Batatinha (cachorro do campus) — no mato perto da Convivência. Só um "Au au!",
        // sem escolha (é um cachorro). Anda livre (área aleatória 10x10) ao redor
        // de onde nasceu, em vez de só ida-e-volta reta.
        // A folha do cachorro tem poses em posições diferentes da folha humana
        // (índices 2/3 = lado direito andando, 4/7 = lado esquerdo, 11 = lado
        // direito parado) — por isso não reaproveita a convenção padrão (5/6/10).
        CreateAmbientNpc(root.transform, "batata.png", new Vector2(-9f, -5f), "Batatinha", "batatinha",
            new[] { "Au au!" }, patrolAreaSize: 10f,
            downFrames: new[] { 0, 5 }, sideFrames: new[] { 2, 3 }, upFrames: new[] { 1, 6 },
            downIdle: 8, sideIdle: 11, upIdle: 9, invertSide: true, scale: 0.8f);

        // Alunos perambulando pelos caminhos que interligam os blocos (ambiente,
        // sem quest). Andam em vaivém/roam sobre os trechos caminháveis — o miolo
        // da estrada em H, as colunas entre os blocos e a rua norte — sempre longe
        // dos colisores dos prédios (medidos: colunas livres em y∈(-1, 5.4), miolo
        // do H livre em x∈(4, 11)). Falam no máximo uma linha.
        // Matheus: além do papo de ambiente, é a interação ÉTICA do Dia 3 no campus
        // (objetivo "ajudar_matheus"). Ajudar dá +1.0 de Ética (uma vez).
        CreateAmbientNpc(root.transform, "matheus.png", new Vector2(6.5f, 2f), "Matheus", "aluno_matheus",
            new[]
            {
                "E aí, calouro! Tudo certo pras provas?",
                "Cara, tô travado num exercício de revisão e as provas são já já...",
            },
            patrolAreaSize: 4f,
            choiceQuestion: "Você senta pra revisar o exercício junto com o Matheus?",
            optionA: "Bora, a gente resolve isso.", optionB: "Foi mal, tô correndo agora.",
            replyA: "Aê! Contigo junto fica bem mais tranquilo. Valeu!",
            replyB: "De boa, depois eu tento sozinho. Bons estudos!",
            ethicsRewardA: 1.0f, ethicsRewardB: 0f,
            // Repete antes do Dia 4 (trote) — depois disso o TroteChase troca
            // por falas de zoação sobre a corrida, ver TroteChase.Resolve.
            repeatLines: new[]
            {
                new[] { "E aí! Valeu de novo por aquela ajuda." },
                new[] { "Bora, ainda tenho mais exercícios pra revisar depois." },
            });

        // Emilly: interação ÉTICA obrigatória do Dia 1 (objetivo "interacao_etica").
        // Fica parada no deck da Convivência (fácil de achar). Ajudar dá +1.0 de
        // Ética (uma vez); ignorar não dá nada. (aragao.png virou o professor Aragão,
        // dentro do Bloco 2 — ver BuildBlocoInterior.)
        CreateAmbientNpc(root.transform, "emilly.png", new Vector2(-3f, -1f), "Emilly", "emilly",
            new[]
            {
                "Oi! Eu sou a Emilly, também sou caloura.",
                "Cara, tô boiando numa lista de exercícios... tava vendo se alguém me dava uma luz.",
            },
            choiceQuestion: "Você para pra ajudar a Emilly com a lista?",
            optionA: "Claro, bora resolver junto!", optionB: "Agora não dá, foi mal.",
            replyA: "Sério? Valeu demais! Assim o primeiro dia fica bem melhor.",
            replyB: "Ah... tranquilo, depois eu vejo. Boa aula!",
            ethicsRewardA: 1.0f, ethicsRewardB: 0f,
            repeatLines: new[]
            {
                new[] { "Oi de novo! Como estão as matérias?" },
                new[] { "Valeu por aquela ajuda com a lista, hein." },
            });
    }

    private static NpcInteractable CreateNpc(Transform parent, string objName, string spritePath, Vector2 pos,
        string displayName, string npcId, string[] lines)
    {
        var go = new GameObject(objName);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;

        var frames = LoadFrames(spritePath);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = frames.Length > NpcDefaultFrame ? frames[NpcDefaultFrame] : LoadFrame(spritePath, NpcDefaultFrame);
        sr.sortingOrder = 5;

        // Todo NPC tem animador, mesmo parado — é o que permite virar de frente
        // pro jogador (LockFacing) ao iniciar uma fala, e anima quem tem NpcPatrol.
        var anim = go.AddComponent<SpriteWalkAnimator>();
        anim.frames = frames;
        anim.framesPerSecond = 8f;

        var trigger = go.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 2.2f;

        var npc = go.AddComponent<NpcInteractable>();
        npc.npcName = displayName;
        npc.npcId = npcId;
        npc.lines = lines;
        return npc;
    }

    /// <summary>
    /// NPC de ambiente (papo simples, sem ligação com a quest principal): fala
    /// algumas linhas e, opcionalmente, faz uma pergunta A/B só de flavor (sem
    /// afetar nota/estresse) e/ou anda (patrol de ida-e-volta ou área livre).
    /// </summary>
    private static NpcInteractable CreateAmbientNpc(Transform parent, string spriteFile, Vector2 pos,
        string displayName, string npcId, string[] lines, Vector2? patrolDir = null, float patrolAreaSize = 0f,
        string choiceQuestion = null, string optionA = null, string optionB = null,
        string replyA = null, string replyB = null,
        int[] downFrames = null, int[] sideFrames = null, int[] upFrames = null,
        int downIdle = 8, int sideIdle = 10, int upIdle = 9, bool invertSide = false,
        float scale = 1f, float ethicsRewardA = 0f, float ethicsRewardB = 0f,
        string examObjective = null, string examKind = null, string[] examLines = null,
        NpcInteractable.ObjectiveLineSet[] altLines = null, string[][] repeatLines = null)
    {
        var npc = CreateNpc(parent, "NPC_" + displayName, CharsFolder + "/" + spriteFile, pos, displayName, npcId, lines);
        if (!Mathf.Approximately(scale, 1f))
            npc.transform.localScale = new Vector3(scale, scale, 1f);
        if (altLines != null) npc.objectiveLines = altLines;
        if (repeatLines != null)
        {
            npc.repeatLines = new NpcInteractable.LineSet[repeatLines.Length];
            for (int k = 0; k < repeatLines.Length; k++)
                npc.repeatLines[k] = new NpcInteractable.LineSet { lines = repeatLines[k] };
        }

        if (choiceQuestion != null)
        {
            npc.hasChoice = true;
            npc.choiceQuestion = choiceQuestion;
            npc.choiceOptionA = optionA;
            npc.choiceOptionB = optionB;
            npc.choiceReplyA = replyA;
            npc.choiceReplyB = replyB;
            npc.ethicsRewardA = ethicsRewardA;
            npc.ethicsRewardB = ethicsRewardB;
        }

        if (!string.IsNullOrEmpty(examObjective))
        {
            npc.examObjective = examObjective;
            npc.examKind = examKind;
            npc.examLines = examLines;
        }

        // CreateNpc já deixou um SpriteWalkAnimator pronto (parado); aqui só
        // sobrescrevemos os índices de pose se a folha não seguir a convenção
        // humana padrão (ex.: o cachorro).
        var anim = npc.GetComponent<SpriteWalkAnimator>();
        if (downFrames != null) anim.downFrames = downFrames;
        if (sideFrames != null) anim.sideFrames = sideFrames;
        if (upFrames != null) anim.upFrames = upFrames;
        anim.downIdle = downIdle;
        anim.sideIdle = sideIdle;
        anim.upIdle = upIdle;
        anim.invertSide = invertSide;

        if (patrolDir.HasValue)
        {
            var patrol = npc.gameObject.AddComponent<NpcPatrol>();
            patrol.mode = NpcPatrol.Mode.BackAndForth;
            patrol.direction = patrolDir.Value;
        }
        else if (patrolAreaSize > 0f)
        {
            var patrol = npc.gameObject.AddComponent<NpcPatrol>();
            patrol.mode = NpcPatrol.Mode.RandomArea;
            patrol.areaSize = patrolAreaSize;
        }

        return npc;
    }

    private static void SetupHud()
    {
        if (GameObject.Find("AcademicHud") == null)
        {
            var go = new GameObject("AcademicHud");
            go.AddComponent<AcademicHud>();
        }
    }

    private static void SetupTitle()
    {
        if (GameObject.Find("TitleScreen") == null)
        {
            var go = new GameObject("TitleScreen");
            go.AddComponent<TitleScreen>();
        }
    }

    /// <summary>Música tema em loop, tocando desde a tela de título (3.18 do roadmap — 1ª trilha).</summary>
    private static void SetupMusic()
    {
        var old = GameObject.Find("MusicPlayer");
        if (old != null) Object.DestroyImmediate(old);

        var go = new GameObject("MusicPlayer");
        var player = go.AddComponent<MusicPlayer>();
        player.theme = GetAudioClip(MusicThemePath);
        player.volume = 0.5f;
    }

    /// <summary>
    /// Abertura do Dia 1 (roda sozinha após a tela de título): o Jeferson percebe o
    /// calouro na passarela e sobe até ele, dá as boas-vindas, mostra o campus
    /// (câmera passeia pelos pontos-chave), indica a 1ª aula e caminha até o
    /// RU/administrativo. No fim, define o primeiro objetivo na HUD. Posições reusam
    /// as constantes do campus.
    /// </summary>
    private static void SetupCampusTour()
    {
        var old = GameObject.Find("CampusTour");
        if (old != null) Object.DestroyImmediate(old);

        var go = new GameObject("CampusTour");
        var tour = go.AddComponent<CampusTourCutscene>();
        tour.tourOrthoSize = 6.5f;
        tour.moveDuration = 1.2f;
        tour.coordenadorWalkSpeed = 4.2f;
        tour.coordenador = GameObject.Find("NPC_Coordenador");

        // 1) Abordagem: o Jeferson sobe a passarela até perto do jogador (que nasce
        //    na Guarita, y=18.5) e a câmera enquadra os dois.
        tour.meetingFocus = new Vector2(-6f, 16.5f);
        tour.approachTarget = new Vector2(-6f, 15.5f);
        tour.welcomeLines = new[]
        {
            "Ei! Ei, calouro! Peraí!",
            "Você tem cara de quem tá meio perdido, né? Relaxa, todo mundo começa assim. Eu sou o Jeferson, coordenador de Engenharia de Software.",
            "Deixa eu te dar as boas-vindas e mostrar o campus rapidinho — assim você não se perde logo no primeiro dia.",
        };

        // 2) Passeio pelo campus.
        tour.stops = new[]
        {
            TourStop(new Vector2(-6f, 1f), "Essa é a nossa Convivência — ponto de encontro entre uma aula e outra."),
            TourStop(new Vector2(-32f, 2f), "Ali fica o RU, o Restaurante Universitário, coladinho no prédio administrativo. Comida barata e o Natan quase sempre por perto."),
            TourStop(PosBloco1, "O Bloco 1 é onde ficam a maioria das salas — inclusive a da sua primeira aula."),
            TourStop(new Vector2(13f, 10f), "Do lado, o Bloco 2, com mais salas e os laboratórios."),
            TourStop(new Vector2(7.5f, -6f), "Lá embaixo, os Blocos 3 e 4. Projeto e prova vão te trazer bastante por aqui."),
            TourStop(new Vector2(-9f, 24f), "E aquela é a Guarita, por onde você entrou. Vai cruzar por ela todo santo dia."),
            TourStop(new Vector2(-6f, 1f), "É isso, {nome}. Sua primeira aula é de IHC, com a professora Rainara, no Bloco 1, Sala 1. Chega junto! Eu vou ali pro administrativo — qualquer coisa, me procura."),
        };

        // 3) O Jeferson vai até a porta do RU (007), que também é o administrativo,
        //    e entra (some). A câmera acompanha. Desde que a porta passou pro lado
        //    LESTE do RU (04/07/2026, de frente pra Convivência, ligada pelo novo
        //    Path_RU_Conv), a rota ficou mais direta: desvia pelo corredor aberto a
        //    OESTE da Convivência (x=-11, entre a Conv. em x≥-9.4 e o RU em x≤-23.6)
        //    e desce só até a altura do caminho novo (y=2, não mais até o sul do
        //    RU), sem atravessar nenhum prédio.
        tour.coordenadorExitPath = new[]
        {
            new Vector2(-11f, 15.5f),
            new Vector2(-11f, 2f),
            new Vector2(-22.7f, 2f),
        };

        // O primeiro objetivo é iniciado pelo próprio QuestManager ao fim da abertura
        // (tour chama StartSequence) — não depende de nenhum id salvo aqui.
    }

    private static CampusTourCutscene.Stop TourStop(Vector2 focus, string line)
        => new CampusTourCutscene.Stop { focus = focus, line = line };

    private static void SetupInteriors()
    {
        if (GameObject.Find("InteriorController") == null)
        {
            var go = new GameObject("InteriorController");
            go.AddComponent<InteriorController>();
        }
    }

    // Labirintos da Prova de Matemática — 4 mapas de dificuldade crescente
    // (2.5 pontos cada, ver MazeController). O 1º é o corredor "cobrinha" de
    // sempre (garantidamente solucionável, sem escolha nenhuma); os outros 3
    // são labirintos de verdade — gerados por backtracking recursivo (sempre
    // solucionáveis, com bifurcações e becos sem saída de verdade) e crescem
    // de tamanho a cada mapa.
    private const float MazeCell = 1.6f;
    private static readonly string[] MazeMapEasy =
    {
        "WWWWWWWWWW",
        "WS.......W",
        "WWWWWWWW.W",
        "W........W",
        "W.WWWWWWWW",
        "W........W",
        "WWWWWWWW.W",
        "W.......EW",
        "WWWWWWWWWW",
    };

    /// <summary>
    /// Gera um labirinto perfeito (uma única solução, sem loops) de cellsX x
    /// cellsY células por backtracking recursivo. Semente fixa: o mapa sai
    /// sempre igual a cada vez que a cena é montada, sem precisar guardar o
    /// resultado. S fica na célula (0,0), E na célula mais distante (canto oposto).
    /// </summary>
    private static string[] GenerateMaze(int cellsX, int cellsY, int seed)
    {
        int w = cellsX * 2 + 1;
        int h = cellsY * 2 + 1;
        var open = new bool[w, h];
        var visited = new bool[cellsX, cellsY];
        var rng = new System.Random(seed);

        void Carve(int cx, int cy)
        {
            visited[cx, cy] = true;
            open[cx * 2 + 1, cy * 2 + 1] = true;

            var dirs = new (int dx, int dy)[] { (1, 0), (-1, 0), (0, 1), (0, -1) };
            for (int i = dirs.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (dirs[i], dirs[j]) = (dirs[j], dirs[i]);
            }

            foreach (var (dx, dy) in dirs)
            {
                int nx = cx + dx, ny = cy + dy;
                if (nx < 0 || ny < 0 || nx >= cellsX || ny >= cellsY || visited[nx, ny]) continue;
                open[cx * 2 + 1 + dx, cy * 2 + 1 + dy] = true; // derruba a parede entre as duas células
                Carve(nx, ny);
            }
        }

        Carve(0, 0);

        var rows = new string[h];
        for (int y = 0; y < h; y++)
        {
            var sb = new StringBuilder();
            for (int x = 0; x < w; x++) sb.Append(open[x, y] ? '.' : 'W');
            rows[y] = sb.ToString();
        }

        var startRow = rows[1].ToCharArray(); startRow[1] = 'S'; rows[1] = new string(startRow);
        int ex = cellsX * 2 - 1, ey = cellsY * 2 - 1;
        var endRow = rows[ey].ToCharArray(); endRow[ex] = 'E'; rows[ey] = new string(endRow);
        return rows;
    }

    private static void SetupMaze(Sprite white)
    {
        var old = GameObject.Find("Maze");
        if (old != null) Object.DestroyImmediate(old);
        var root = new GameObject("Maze");

        string[][] maps =
        {
            MazeMapEasy,
            GenerateMaze(5, 5, 20260704),
            GenerateMaze(7, 7, 20260705),
            GenerateMaze(9, 9, 20260706),
        };

        var wallColor = new Color(0.30f, 0.34f, 0.42f);
        var floorColor = new Color(0.12f, 0.13f, 0.16f);

        const float baseX = 100f, baseY = 0f, gapX = 80f; // um labirinto do lado do outro, bem espaçados
        var starts = new Vector3[maps.Length];

        for (int m = 0; m < maps.Length; m++)
        {
            var map = maps[m];
            int rows = map.Length;
            int cols = map[0].Length;
            float ox = baseX + m * gapX;
            float left = ox - cols * MazeCell / 2f;
            float top = baseY + rows * MazeCell / 2f;

            var mazeRoot = new GameObject($"Maze_{m + 1}");
            mazeRoot.transform.SetParent(root.transform, false);

            Vector3 startPos = new Vector3(ox, baseY, 0f);

            CreateQuad(mazeRoot.transform, "MazeFloor", new Vector2(ox, baseY),
                new Vector2(cols * MazeCell, rows * MazeCell), floorColor, white, -10, false);

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    char ch = map[r][c];
                    float x = left + (c + 0.5f) * MazeCell;
                    float y = top - (r + 0.5f) * MazeCell;

                    if (ch == 'W')
                    {
                        CreateQuad(mazeRoot.transform, $"MW_{r}_{c}", new Vector2(x, y),
                            new Vector2(MazeCell, MazeCell), wallColor, white, 0, true);
                    }
                    else if (ch == 'S')
                    {
                        startPos = new Vector3(x, y, 0f);
                    }
                    else if (ch == 'E')
                    {
                        var exit = CreateQuad(mazeRoot.transform, "MazeExit", new Vector2(x, y),
                            new Vector2(MazeCell * 0.8f, MazeCell * 0.8f), new Color(0.3f, 1f, 0.4f, 0.75f), white, -9, false);
                        var trg = exit.AddComponent<BoxCollider2D>();
                        trg.isTrigger = true;
                        exit.AddComponent<MazeExit>();
                    }
                }
            }

            starts[m] = startPos;
        }

        var ctrlGO = GameObject.Find("MazeController") ?? new GameObject("MazeController");
        var ctrl = ctrlGO.GetComponent<MazeController>() ?? ctrlGO.AddComponent<MazeController>();
        ctrl.mazeStarts = starts;

        // Sem portal no campus: a Prova de Matemática agora é iniciada falando com
        // o Aragão na sala dele (ver examObjective "prova_mat" em BuildBlocoInterior
        // e DialogueManager.LaunchExam).
        var oldPortal = GameObject.Find("MazePortal");
        if (oldPortal != null) Object.DestroyImmediate(oldPortal);
    }

    private static void SetupQuest()
    {
        if (GameObject.Find("QuestManager") == null)
        {
            var go = new GameObject("QuestManager");
            go.AddComponent<QuestManager>();
        }

        // Minigame do Dia 4 (trote): Natan/Enzo/Matheus/Vitim correndo atrás do
        // jogador no próprio campus (ver TroteChase e roadmap-v2.md, 3.1B/3.6).
        if (GameObject.Find("TroteChase") == null)
        {
            var go = new GameObject("TroteChase");
            go.AddComponent<TroteChase>();
        }

        // Transição de fim de dia (tela preta "Dia N finalizado / Boa sorte no Dia
        // N+1"). Reaparece o jogador na passarela da Guarita a cada novo dia.
        var oldDt = GameObject.Find("DayTransition");
        if (oldDt != null) Object.DestroyImmediate(oldDt);
        var dtGO = new GameObject("DayTransition");
        var dt = dtGO.AddComponent<DayTransition>();
        dt.campusSpawn = SpawnPos;

        // Provas interativas (quiz de IES / montar solução de FUP).
        if (GameObject.Find("ExamManager") == null)
        {
            var exGO = new GameObject("ExamManager");
            exGO.AddComponent<ExamManager>();
        }

        var oldGoal = GameObject.Find("GoalZone");
        if (oldGoal != null) Object.DestroyImmediate(oldGoal);

        var goal = new GameObject("GoalZone");
        goal.transform.position = PosBloco1Front;
        var box = goal.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(4f, 8f);
        goal.AddComponent<GoalZone>();
    }

    private static void SetupPlayer()
    {
        var player = GameObject.Find("Player");
        if (player == null)
        {
            player = new GameObject("Player");
            player.AddComponent<SpriteRenderer>();
        }
        player.tag = "Player"; // necessário para os NPCs detectarem o jogador

        var calouroFrames = LoadFrames(CalouroSpritePath);
        var calouraFrames = LoadFrames(CalouraSpritePath);

        var sr = player.GetComponent<SpriteRenderer>();
        if (sr == null) sr = player.AddComponent<SpriteRenderer>();
        sr.sprite = calouroFrames.Length > PlayerIdleFrame ? calouroFrames[PlayerIdleFrame] : LoadFrame(CalouroSpritePath, PlayerIdleFrame);
        sr.sortingOrder = 10;

        player.transform.position = SpawnPos;

        var pc = player.GetComponent<PlayerController2D>() ?? player.AddComponent<PlayerController2D>();
        pc.flipSprite = false; // o animador cuida do espelhamento por direção

        // Animação direcional (poses por direção do movimento). Layout 6x4 do
        // protagonista — o PlayerAppearance reaplica isso ao trocar de personagem.
        var anim = player.GetComponent<SpriteWalkAnimator>() ?? player.AddComponent<SpriteWalkAnimator>();
        anim.frames = calouroFrames;
        anim.downFrames = new[] { 3, 4, 5 };
        anim.sideFrames = new[] { 9, 10, 11 };
        anim.upFrames = new[] { 15, 16, 17 };
        anim.downIdle = 0;
        anim.sideIdle = 6;
        anim.upIdle = 12;
        anim.invertSide = true;   // as poses de lado encaram a direita
        anim.swayWhenUp = false;  // há quadros reais de costas
        anim.framesPerSecond = 8f;

        // Escolha do personagem (calouro/caloura) na tela de título.
        var appearance = player.GetComponent<PlayerAppearance>() ?? player.AddComponent<PlayerAppearance>();
        appearance.calouroFrames = calouroFrames;
        appearance.calouraFrames = calouraFrames;

        var rb = player.GetComponent<Rigidbody2D>() ?? player.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var col = player.GetComponent<BoxCollider2D>() ?? player.AddComponent<BoxCollider2D>();
        if (sr.sprite != null)
        {
            Vector2 b = sr.sprite.bounds.size;
            col.size = new Vector2(b.x * 0.6f, b.y * 0.5f);
            col.offset = new Vector2(0f, -b.y * 0.2f); // colisor focado nos "pés"
        }
    }

    private static Sprite GetWhiteSprite()
    {
        var importer = AssetImporter.GetAtPath(WhitePath) as TextureImporter;
        if (importer == null) return null;

        bool changed = false;
        if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; changed = true; }
        if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; changed = true; }
        if (!Mathf.Approximately(importer.spritePixelsPerUnit, 16f)) { importer.spritePixelsPerUnit = 16f; changed = true; }
        if (changed) importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(WhitePath);
    }
}
