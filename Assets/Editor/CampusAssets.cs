using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// Fatia as duas folhas de assets de campus (interiores) em sprites nomeados
/// e disponibiliza cada peça por uma chave semântica (piso, parede, porta,
/// janela, carteira, lousa, etc.). As caixas foram detectadas por componentes
/// conectados (alpha) nas folhas originais.
///
/// Coordenadas dos Defs estão em pixels com origem no CANTO SUPERIOR ESQUERDO
/// (como as folhas são lidas); a conversão para o retângulo do Unity (origem
/// embaixo) é feita em EnsureSliced.
/// </summary>
public static class CampusAssets
{
    public const string Folder = "Assets/Art/Campus";
    public const string Sheet1 = Folder + "/campus_sheet1.png";
    public const string Sheet2 = Folder + "/campus_sheet2.png";
    public const float PixelsPerUnit = 100f;

    /// <summary>Uma peça: chave, folha, caixa (canto sup. esq.) e pivô.</summary>
    private struct Def
    {
        public string key;
        public int sheet;      // 1 ou 2
        public int x, y, w, h; // topo-esquerda
        public bool bottomPivot;
        public Def(string key, int sheet, int x, int y, int w, int h, bool bottomPivot)
        { this.key = key; this.sheet = sheet; this.x = x; this.y = y; this.w = w; this.h = h; this.bottomPivot = bottomPivot; }
    }

    // Peças estruturais (pivô central, boas para tiling) e mobília (pivô embaixo).
    private static readonly Def[] Defs =
    {
        // ---- Folha 2 (pisos, paredes, portas, janelas, mobília) ----
        new Def("floor",       2,   33,  27, 143, 135, false),
        new Def("floorB",      2,  213,  27, 139, 135, false),
        new Def("wallH",       2,   32, 224, 228, 110, false),
        new Def("wallV",       2,  276, 224, 222, 110, false),
        new Def("pillar",      2,  744, 223, 192, 107, false),
        new Def("wallCap",     2, 1337, 233, 154,  75, false),
        new Def("doorClosed",  2,  424, 394, 161, 115, true),
        new Def("doorOpen",    2,  617, 395, 289, 111, false),
        new Def("window",      2,   33, 394, 364, 112, false),
        new Def("board",       2,   33, 763, 264,  86, true),
        new Def("muralBlue",   2, 1221, 396, 271,  91, true),
        new Def("teacherDesk", 2, 1071, 588, 154, 110, true),
        new Def("chair",       2,  947, 586,  79, 105, true),
        new Def("deskStudent", 2,  351, 768,  79,  90, true),
        new Def("trash",       2, 1211, 763,  63,  94, true),
        new Def("ac",          2, 1302, 771, 190,  75, true),

        // ---- Folha 1 (lousa grande, relógio, extintor, janela com vegetação) ----
        new Def("whiteboard",  1,  766,  22, 729, 137, true),
        new Def("clock",       1, 1294, 809,  92,  86, true),
        new Def("fireExt",     1, 1419, 772,  72, 125, true),
        new Def("windowHedge", 1,   29, 493, 302,  85, false),
    };

    private static readonly Dictionary<string, string> KeyToPath = new Dictionary<string, string>();
    private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

    [MenuItem("Tools/Calouro/Fatiar Assets de Campus")]
    public static void SliceMenu()
    {
        EnsureSliced(force: true);
        Debug.Log("[Calouro] Assets de campus fatiados. Peças: " + string.Join(", ", Defs.Select(d => d.key)));
    }

    /// <summary>Garante que as duas folhas estão importadas e fatiadas.</summary>
    public static void EnsureSliced(bool force = false)
    {
        KeyToPath.Clear();
        Cache.Clear();
        foreach (var d in Defs) KeyToPath[d.key] = d.sheet == 1 ? Sheet1 : Sheet2;

        SliceSheet(Sheet1, 1);
        SliceSheet(Sheet2, 2);
    }

    private static void SliceSheet(string path, int sheet)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) { Debug.LogError($"[Calouro] Não achei {path}."); return; }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.isReadable = true;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect; // permite drawMode Tiled/Sliced
        settings.spriteExtrude = 1;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex == null) return;
        int H = tex.height;

        var factories = new SpriteDataProviderFactories();
        factories.Init();
        var dp = factories.GetSpriteEditorDataProviderFromObject(importer);
        dp.InitSpriteEditorDataProvider();

        var rects = new List<SpriteRect>();
        foreach (var d in Defs)
        {
            if (d.sheet != sheet) continue;
            float ry = H - (d.y + d.h); // topo-esq -> baixo-esq
            rects.Add(new SpriteRect
            {
                name = d.key,
                spriteID = StableGuid("campus_" + d.key),
                rect = new Rect(d.x, ry, d.w, d.h),
                alignment = d.bottomPivot ? SpriteAlignment.BottomCenter : SpriteAlignment.Center,
                pivot = d.bottomPivot ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 0.5f),
            });
        }

        dp.SetSpriteRects(rects.ToArray());
        var nameProvider = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (nameProvider != null)
            nameProvider.SetNameFileIdPairs(rects.Select(s => new SpriteNameFileIdPair(s.name, s.spriteID)));
        dp.Apply();
        (dp.targetObject as AssetImporter)?.SaveAndReimport();
    }

    /// <summary>Sprite por chave semântica (ex.: "floor", "doorClosed", "chair").</summary>
    public static Sprite Get(string key)
    {
        if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;
        if (KeyToPath.Count == 0)
            foreach (var d in Defs) KeyToPath[d.key] = d.sheet == 1 ? Sheet1 : Sheet2;
        if (!KeyToPath.TryGetValue(key, out var path)) return null;

        var sprite = AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .FirstOrDefault(s => s.name == key);
        if (sprite != null) Cache[key] = sprite;
        else Debug.LogWarning($"[Calouro] Sprite '{key}' não encontrado em {path} (rode 'Fatiar Assets de Campus').");
        return sprite;
    }

    private static GUID StableGuid(string s)
    {
        using (var md5 = MD5.Create())
        {
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(s));
            var sb = new StringBuilder(32);
            foreach (var b in hash) sb.Append(b.ToString("x2"));
            GUID.TryParse(sb.ToString(), out var guid);
            return guid;
        }
    }
}
