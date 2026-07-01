using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// HUD acadêmico: barra de estresse (canto superior direito, sempre visível) e
/// caderneta acadêmica (abre no ESC, pausa o jogo) com as 5 disciplinas, notas
/// (stub), semana e estresse atual. Constrói a própria UI.
/// </summary>
public class AcademicHud : MonoBehaviour
{
    public static AcademicHud Instance { get; private set; }

    [Header("Estresse")]
    public float stress = 20f;
    public float maxStress = 100f;
    [Tooltip("Quanto o estresse sobe por segundo (placeholder pra ver a barra mexer).")]
    public float stressPerSecond = 1f;

    [Header("Semestre")]
    public int week = 1;
    public int totalWeeks = 18;

    private readonly string[] disciplines =
    {
        "Fundamentos da Programação",
        "Interação Humano-Computador",
        "Ética",
        "Matemática Básica",
        "Intro. à Engenharia de Software",
    };
    private readonly float[] grades = { -1f, -1f, -1f, -1f, -1f }; // -1 = não avaliado

    private RectTransform stressFill;
    private Image stressFillImg;
    private GameObject caderneta;
    private Text cadernetaText;
    private bool open;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildUI();
        SetCaderneta(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        // Sobe devagar com o tempo (só anda quando não está pausado).
        stress = Mathf.Clamp(stress + stressPerSecond * Time.deltaTime, 0f, maxStress);
        UpdateStressBar();

        // ESC abre/fecha a caderneta (não durante diálogo nem na tela de fim).
        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame
            && !DialogueManager.IsActive && !QuestManager.IsGameOver && !TitleScreen.IsShowing)
        {
            SetCaderneta(!open);
        }
    }

    public void AddStress(float delta)
    {
        stress = Mathf.Clamp(stress + delta, 0f, maxStress);
        UpdateStressBar();
    }

    private void SetCaderneta(bool value)
    {
        open = value;
        if (caderneta != null) caderneta.SetActive(open);
        Time.timeScale = open ? 0f : 1f;
        if (open) RefreshCaderneta();
    }

    private void RefreshCaderneta()
    {
        if (cadernetaText == null) return;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("CADERNETA ACADÊMICA");
        sb.AppendLine($"Calouro: {GameProgress.PlayerName}");
        sb.AppendLine($"Semana {week} / {totalWeeks}");
        sb.AppendLine($"Estresse: {Mathf.RoundToInt(stress)}%");
        sb.AppendLine();
        for (int i = 0; i < disciplines.Length; i++)
        {
            // Matemática Básica (índice 3) usa a nota da Prova-Labirinto.
            float g = (i == 3) ? GameProgress.MathGrade : grades[i];
            string nota = g < 0f ? "—" : g.ToString("0.0");
            sb.AppendLine($"{disciplines[i]}:  {nota}");
        }
        sb.AppendLine();
        sb.AppendLine("(ESC para fechar)");
        cadernetaText.text = sb.ToString();
    }

    private void UpdateStressBar()
    {
        if (stressFill == null) return;
        float pct = maxStress > 0f ? Mathf.Clamp01(stress / maxStress) : 0f;
        stressFill.anchorMin = new Vector2(0f, 0f);
        stressFill.anchorMax = new Vector2(pct, 1f);
        stressFill.offsetMin = Vector2.zero;
        stressFill.offsetMax = Vector2.zero;
        if (stressFillImg != null)
            stressFillImg.color = Color.Lerp(new Color(0.3f, 0.8f, 0.3f), new Color(0.85f, 0.2f, 0.2f), pct);
    }

    // ------------------------------------------------------------------ UI

    private void BuildUI()
    {
        var canvasGO = new GameObject("HudCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Barra de estresse (canto superior direito).
        var bg = new GameObject("StressBar");
        bg.transform.SetParent(canvasGO.transform, false);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.6f);
        var bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(1f, 1f);
        bgRT.anchorMax = new Vector2(1f, 1f);
        bgRT.pivot = new Vector2(1f, 1f);
        bgRT.anchoredPosition = new Vector2(-28f, -22f);
        bgRT.sizeDelta = new Vector2(340f, 36f);

        var fill = new GameObject("Fill");
        fill.transform.SetParent(bg.transform, false);
        stressFillImg = fill.AddComponent<Image>();
        stressFillImg.color = new Color(0.3f, 0.8f, 0.3f);
        stressFill = fill.GetComponent<RectTransform>();

        var label = CreateText(bg.transform, "Label", font, 22, TextAnchor.MiddleCenter);
        label.color = Color.white;
        label.text = "Estresse";
        var lRT = label.rectTransform;
        lRT.anchorMin = Vector2.zero;
        lRT.anchorMax = Vector2.one;
        lRT.offsetMin = Vector2.zero;
        lRT.offsetMax = Vector2.zero;

        // Caderneta (centro).
        caderneta = new GameObject("CadernetaPanel");
        caderneta.transform.SetParent(canvasGO.transform, false);
        var cImg = caderneta.AddComponent<Image>();
        cImg.color = new Color(0.05f, 0.06f, 0.09f, 0.96f);
        var cRT = caderneta.GetComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0.28f, 0.16f);
        cRT.anchorMax = new Vector2(0.72f, 0.84f);
        cRT.offsetMin = Vector2.zero;
        cRT.offsetMax = Vector2.zero;

        cadernetaText = CreateText(caderneta.transform, "CadernetaText", font, 30, TextAnchor.UpperLeft);
        cadernetaText.color = Color.white;
        var ctRT = cadernetaText.rectTransform;
        ctRT.anchorMin = Vector2.zero;
        ctRT.anchorMax = Vector2.one;
        ctRT.offsetMin = new Vector2(36f, 28f);
        ctRT.offsetMax = new Vector2(-36f, -28f);
    }

    private Text CreateText(Transform parent, string name, Font font, int size, TextAnchor anchor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.alignment = anchor;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }
}
