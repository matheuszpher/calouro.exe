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
    private const string PlayerSpritePath = CharsFolder + "/matheus.png";

    private const int Cols = 4;
    private const int Rows = 3;
    private const float CharPixelsPerUnit = 100f;
    // Pose usada por padrão no Player: linha de baixo, 1ª coluna = frente parado.
    private const int DefaultFrame = 8;

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
            SliceGrid(AssetDatabase.GUIDToAssetPath(guid), Cols, Rows);
        AssetDatabase.Refresh();
    }

    private static void SliceGrid(string path, int cols, int rows)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = CharPixelsPerUnit;
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

    private const float MapXMin = -40f, MapXMax = 40f, MapYMin = -42f, MapYMax = 42f;
    private const float WallT = 0.6f;

    // Posições-chave (compartilhadas entre os montadores).
    private static readonly Vector3 SpawnPos = new Vector3(-8f, 3f, 0f);   // Convivência (005)
    private static readonly Vector2 PosCoordenador = new Vector2(-8f, 6f); // Convivência
    private static readonly Vector2 PosNatan = new Vector2(-22f, 2f);      // RU (007)
    private static readonly Vector2 PosBloco1 = new Vector2(2f, 10f);      // Bloco 1 (001)
    private static readonly Vector2 PosBloco1Front = new Vector2(-2.9f, 10f); // frente da porta do Bloco 1
    private static readonly Vector2 PosPortal = new Vector2(2f, -14f);     // Bloco 3 (003)
    private static readonly Vector2 PosMazePortal = new Vector2(-6f, -6f); // portal da prova (área aberta)
    private static readonly Vector3 ReturnPos = new Vector3(-8f, 3f, 0f);

    private static Sprite s_white;
    private static int roomCounter;
    private static Transform interiorsRoot;

    private static void BuildCampus(Transform root, Sprite white)
    {
        var wall = new Color(0.32f, 0.30f, 0.27f);
        var ground = new Color(0.15f, 0.19f, 0.13f);   // gramado do campus
        var path = new Color(0.30f, 0.28f, 0.23f);
        var asphalt = new Color(0.20f, 0.20f, 0.22f);
        var didatico = new Color(0.22f, 0.34f, 0.50f); // blocos 001-004 (azul)
        var servico = new Color(0.34f, 0.40f, 0.24f);  // 005/006/007/008/009 (verde)

        // Chão base do campus inteiro.
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

        // Caminhos internos (visuais).
        CreateQuad(root, "Path_Guarita", new Vector2(-6f, 15f), new Vector2(4f, 14f), path, white, -9, false);
        CreateQuad(root, "Path_Central", new Vector2(2f, 2f), new Vector2(26f, 4f), path, white, -9, false);
        CreateQuad(root, "Path_Blocos_V", new Vector2(2f, -2f), new Vector2(4f, 28f), path, white, -9, false);
        CreateQuad(root, "Path_009", new Vector2(2f, -24f), new Vector2(4f, 16f), path, white, -9, false);

        var roofDidatico = new Color(0.30f, 0.42f, 0.60f);  // telhado dos blocos (azul)
        var roofServico = new Color(0.42f, 0.48f, 0.30f);   // telhado serviço (verde)

        // 006 — Guarita (entrada) — prédio pequeno coberto, sem interior.
        CoveredBlock(root, "GUARITA (006)", new Vector2(-6f, 22f), new Vector2(5f, 4f), roofServico, 'S', false);

        // 007 — Bloco Administrativo / RU (esquerda, largo) — coberto, com sala (interior).
        CoveredBlock(root, "RU / ADM (007)", new Vector2(-22f, 2f), new Vector2(16f, 9f), roofServico, 'E', true);

        // 005 — Convivência (entre o RU e os blocos) — aberta (spawn), piso em peça única.
        Sprite floorS = CampusAssets.Get("floor");
        if (floorS != null) StretchedSprite(root, "Floor_Convivencia", new Vector2(-8f, 2f), new Vector2(8f, 8f), floorS, -8, Color.white);
        else CreateQuad(root, "Floor_Convivencia", new Vector2(-8f, 2f), new Vector2(8f, 8f), new Color(0.22f, 0.32f, 0.20f), white, -8, false);
        Label(root, "CONVIVENCIA (005)", new Vector2(-8f, 6.6f), new Color(0.9f, 1f, 0.9f));

        // 001–004 — Blocos didáticos (2x2) cobertos; cada um tem uma SALA por dentro.
        CoveredBlock(root, "BLOCO 1 (001)", PosBloco1, new Vector2(7f, 16f), roofDidatico, 'W', true);
        CoveredBlock(root, "BLOCO 2 (002)", new Vector2(13f, 10f), new Vector2(7f, 16f), roofDidatico, 'W', true);
        CoveredBlock(root, "BLOCO 3 (003)", PosPortal, new Vector2(7f, 16f), roofDidatico, 'W', true);
        CoveredBlock(root, "BLOCO 4 (004)", new Vector2(13f, -14f), new Vector2(7f, 16f), roofDidatico, 'W', true);

        // 008 / 009 — Depósitos (fechados, sem interior).
        CoveredBlock(root, "DEP. (008)", new Vector2(-24f, -10f), new Vector2(6f, 3f), roofServico, 'X', false);
        CoveredBlock(root, "DEP. (009)", new Vector2(2f, -32f), new Vector2(7f, 3f), roofServico, 'X', false);
    }

    private static void VWall(Transform parent, string name, float x, float yMin, float yMax,
        Color color, Sprite white)
    {
        float cy = (yMin + yMax) * 0.5f;
        float h = Mathf.Abs(yMax - yMin);
        if (h <= 0f) return;
        CreateQuad(parent, name, new Vector2(x, cy), new Vector2(WallT, h), color, white, 0, true);
    }

    private static void VWallWithGap(Transform parent, string baseName, float x, float yMin, float yMax,
        float gapMin, float gapMax, Color color, Sprite white)
    {
        VWall(parent, baseName + "_a", x, yMin, gapMin, color, white);
        VWall(parent, baseName + "_b", x, gapMax, yMax, color, white);
    }

    private static void HWall(Transform parent, string name, float y, float xMin, float xMax, Color color, Sprite white)
    {
        float cx = (xMin + xMax) * 0.5f;
        float wdt = Mathf.Abs(xMax - xMin);
        if (wdt <= 0f) return;
        CreateQuad(parent, name, new Vector2(cx, y), new Vector2(wdt, WallT), color, white, 0, true);
    }

    private static void HWallWithGap(Transform parent, string baseName, float y, float xMin, float xMax,
        float gapMin, float gapMax, Color color, Sprite white)
    {
        HWall(parent, baseName + "_a", y, xMin, gapMin, color, white);
        HWall(parent, baseName + "_b", y, gapMax, xMax, color, white);
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
    /// Monta uma SALA numa região afastada (interiores ficam lado a lado, longe
    /// do campus). Piso em peça única, paredes contínuas (esticadas, eixos certos),
    /// lousa + mesa do professor + carteiras, e um tapete de saída (RoomExit).
    /// Retorna (spawn do jogador, limite mín da câmera, limite máx).
    /// </summary>
    private static (Vector3 spawn, Vector2 bmin, Vector2 bmax) BuildInteriorRoom(string label)
    {
        if (interiorsRoot == null)
        {
            var go = GameObject.Find("Interiors");
            if (go != null) Object.DestroyImmediate(go);
            interiorsRoot = new GameObject("Interiors").transform;
        }

        // Salas enfileiradas bem longe do campus (evita sobrepor campus e labirinto).
        Vector2 c = new Vector2(300f + roomCounter * 40f, -300f);
        roomCounter++;

        Vector2 rsize = new Vector2(18f, 13f);
        float hx = rsize.x / 2f, hy = rsize.y / 2f;
        float top = c.y + hy, bottom = c.y - hy;
        float left = c.x - hx, right = c.x + hx;
        float gap = 3.2f; // vão da porta (embaixo)
        Color clear = new Color(0f, 0f, 0f, 0f);

        Sprite floorS = CampusAssets.Get("floor");
        Sprite wallHS = CampusAssets.Get("wallH");

        // Piso em peça única.
        if (floorS != null) StretchedSprite(interiorsRoot, "RFloor_" + label, c, rsize, floorS, -10, Color.white);
        else CreateQuad(interiorsRoot, "RFloor_" + label, c, rsize, new Color(0.2f, 0.2f, 0.22f), s_white, -10, false);

        // Paredes visuais contínuas (peça única; verticais giradas 90°).
        const float wt = 1.1f;
        if (wallHS != null)
        {
            StretchedSprite(interiorsRoot, "RWallN_" + label, new Vector2(c.x, top), new Vector2(rsize.x + wt, wt), wallHS, 4, Color.white);
            StretchedSprite(interiorsRoot, "RWallW_" + label, new Vector2(left, c.y), new Vector2(wt, rsize.y), wallHS, 4, Color.white, rotate90: true);
            StretchedSprite(interiorsRoot, "RWallE_" + label, new Vector2(right, c.y), new Vector2(wt, rsize.y), wallHS, 4, Color.white, rotate90: true);
            // Parede de baixo em dois pedaços (deixa o vão da porta).
            float segW = (rsize.x - gap) / 2f;
            StretchedSprite(interiorsRoot, "RWallS1_" + label, new Vector2(left + segW / 2f, bottom), new Vector2(segW + wt, wt), wallHS, 4, Color.white);
            StretchedSprite(interiorsRoot, "RWallS2_" + label, new Vector2(right - segW / 2f, bottom), new Vector2(segW + wt, wt), wallHS, 4, Color.white);
        }

        // Colisão das paredes (invisível), com vão embaixo.
        VWall(interiorsRoot, "RcolW_" + label, left, bottom, top, clear, s_white);
        VWall(interiorsRoot, "RcolE_" + label, right, bottom, top, clear, s_white);
        HWall(interiorsRoot, "RcolN_" + label, top, left, right, clear, s_white);
        HWallWithGap(interiorsRoot, "RcolS_" + label, bottom, left, right, c.x - gap / 2f, c.x + gap / 2f, clear, s_white);

        // Mobília: lousa + mesa do professor + carteiras (2x3).
        Sprite board = CampusAssets.Get("board");
        Sprite teacher = CampusAssets.Get("teacherDesk");
        Sprite desk = CampusAssets.Get("deskStudent");
        if (board != null) Prop(interiorsRoot, "RBoard_" + label, new Vector2(c.x, top - 1.6f), board, 5, 1.2f);
        if (teacher != null) Prop(interiorsRoot, "RTeacher_" + label, new Vector2(c.x, top - 3.2f), teacher, 6, 1.0f);
        if (desk != null)
        {
            float startY = top - 5.4f;
            for (int r = 0; r < 3; r++)
                for (int col = 0; col < 2; col++)
                {
                    float x = c.x + (col == 0 ? -2.2f : 2.2f);
                    float y = startY - r * 2.1f;
                    Prop(interiorsRoot, $"RDesk_{label}_{r}_{col}", new Vector2(x, y), desk, 6, 0.9f);
                }
        }

        // Tapete de saída (RoomExit) no vão da porta + sprite de porta.
        var mat = CreateQuad(interiorsRoot, "RExit_" + label, new Vector2(c.x, bottom + 0.3f),
            new Vector2(gap * 0.8f, 1.2f), new Color(0.3f, 1f, 0.4f, 0.5f), s_white, -9, false);
        var mcol = mat.AddComponent<BoxCollider2D>();
        mcol.isTrigger = true;
        mat.AddComponent<RoomExit>();
        Sprite doorS = CampusAssets.Get("doorOpen");
        if (doorS != null) Prop(interiorsRoot, "RDoor_" + label, new Vector2(c.x, bottom), doorS, 5, 1.1f);

        Label(interiorsRoot, "SALA — " + label, new Vector2(c.x, top + 1.2f), new Color(0.96f, 0.96f, 0.88f));

        Vector3 spawn = new Vector3(c.x, bottom + 1.8f, 0f);
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

        CreateNpc(root.transform, "NPC_Natan", CharsFolder + "/natan.png", PosNatan, "Natan", "natan",
            new[]
            {
                "E aí, calouro! Eu sou o Natan.",
                "Bora sobreviver a esse primeiro semestre juntos.",
                "Precisa de algo? É só me chamar.",
            });

        CreateNpc(root.transform, "NPC_Coordenador", CharsFolder + "/jeferson.png", PosCoordenador, "Coordenador", "coordenador",
            new[]
            {
                "Bem-vindo a Quixadá, calouro!",
                "Sou o coordenador de Engenharia de Software.",
                "Passe no RU e fale com o Natan — ele te mostra o campus.",
                "Depois siga para o Bloco 1. Boa sorte no semestre!",
            });
    }

    private static void CreateNpc(Transform parent, string objName, string spritePath, Vector2 pos,
        string displayName, string npcId, string[] lines)
    {
        var go = new GameObject(objName);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadFrame(spritePath, DefaultFrame);
        sr.sortingOrder = 5;

        var trigger = go.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 2.2f;

        var npc = go.AddComponent<NpcInteractable>();
        npc.npcName = displayName;
        npc.npcId = npcId;
        npc.lines = lines;
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

    private static void SetupInteriors()
    {
        if (GameObject.Find("InteriorController") == null)
        {
            var go = new GameObject("InteriorController");
            go.AddComponent<InteriorController>();
        }
    }

    // Labirinto (snake) — corredor de S até E, garantidamente solucionável.
    private const float MazeCell = 1.6f;
    private static readonly string[] MazeMap =
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

    private static void SetupMaze(Sprite white)
    {
        var old = GameObject.Find("Maze");
        if (old != null) Object.DestroyImmediate(old);
        var root = new GameObject("Maze");

        int rows = MazeMap.Length;
        int cols = MazeMap[0].Length;
        const float baseX = 100f, baseY = 0f;
        float left = baseX - cols * MazeCell / 2f;
        float top = baseY + rows * MazeCell / 2f;

        var wallColor = new Color(0.30f, 0.34f, 0.42f);
        var floorColor = new Color(0.12f, 0.13f, 0.16f);
        Vector3 startPos = new Vector3(baseX, baseY, 0f);

        CreateQuad(root.transform, "MazeFloor", new Vector2(baseX, baseY),
            new Vector2(cols * MazeCell, rows * MazeCell), floorColor, white, -10, false);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                char ch = MazeMap[r][c];
                float x = left + (c + 0.5f) * MazeCell;
                float y = top - (r + 0.5f) * MazeCell;

                if (ch == 'W')
                {
                    CreateQuad(root.transform, $"MW_{r}_{c}", new Vector2(x, y),
                        new Vector2(MazeCell, MazeCell), wallColor, white, 0, true);
                }
                else if (ch == 'S')
                {
                    startPos = new Vector3(x, y, 0f);
                }
                else if (ch == 'E')
                {
                    var exit = CreateQuad(root.transform, "MazeExit", new Vector2(x, y),
                        new Vector2(MazeCell * 0.8f, MazeCell * 0.8f), new Color(0.3f, 1f, 0.4f, 0.75f), white, -9, false);
                    var trg = exit.AddComponent<BoxCollider2D>();
                    trg.isTrigger = true;
                    exit.AddComponent<MazeExit>();
                }
            }
        }

        var ctrlGO = GameObject.Find("MazeController") ?? new GameObject("MazeController");
        var ctrl = ctrlGO.GetComponent<MazeController>() ?? ctrlGO.AddComponent<MazeController>();
        ctrl.mazeStart = startPos;

        // Portal no RU (esquerda-baixo) que inicia a prova.
        var oldPortal = GameObject.Find("MazePortal");
        if (oldPortal != null) Object.DestroyImmediate(oldPortal);
        var portal = new GameObject("MazePortal");
        portal.transform.position = new Vector3(PosMazePortal.x, PosMazePortal.y, 0f);
        portal.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
        var psr = portal.AddComponent<SpriteRenderer>();
        psr.sprite = white;
        psr.color = new Color(0.3f, 0.6f, 1f, 0.8f);
        psr.sortingOrder = -7;
        var pcol = portal.AddComponent<CircleCollider2D>();
        pcol.isTrigger = true;
        pcol.radius = 1.4f;
        var mp = portal.AddComponent<MazePortal>();
        mp.returnPosition = ReturnPos;
    }

    private static void SetupQuest()
    {
        if (GameObject.Find("QuestManager") == null)
        {
            var go = new GameObject("QuestManager");
            go.AddComponent<QuestManager>();
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

        var sr = player.GetComponent<SpriteRenderer>();
        if (sr == null) sr = player.AddComponent<SpriteRenderer>();
        sr.sprite = LoadFrame(PlayerSpritePath, DefaultFrame);
        sr.sortingOrder = 10;

        player.transform.position = SpawnPos;

        var pc = player.GetComponent<PlayerController2D>() ?? player.AddComponent<PlayerController2D>();
        pc.flipSprite = false; // o animador cuida do espelhamento por direção

        // Animação direcional (poses por direção do movimento).
        var anim = player.GetComponent<SpriteWalkAnimator>() ?? player.AddComponent<SpriteWalkAnimator>();
        anim.frames = LoadFrames(PlayerSpritePath);
        anim.downFrames = new[] { 0, 1 };
        anim.sideFrames = new[] { 5, 6 };
        anim.upFrames = new[] { 9 };
        anim.downIdle = 8;
        anim.sideIdle = 10;
        anim.upIdle = 9;
        anim.framesPerSecond = 8f;

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
