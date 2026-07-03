using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// HUD acadêmico: contador de dias pro fim do semestre (canto superior direito,
/// sempre visível) e caderneta acadêmica (abre no ESC, pausa o jogo) com as 5
/// disciplinas, notas e semana atual. Constrói a própria UI.
/// Decisão de 04/07/2026: mecânica de estresse removida do jogo (barra e toda a
/// lógica associada) — não faz mais parte do escopo, contrariando o "nunca
/// cortar" original do roadmap-v2.md seção 2 (registrado lá o motivo da mudança).
/// </summary>
public class AcademicHud : MonoBehaviour
{
    public static AcademicHud Instance { get; private set; }

    [Header("Semestre")]
    public int totalWeeks = 18;

    /// <summary>
    /// Semana exibida na caderneta, derivada de GameProgress.SemesterDay (o dia
    /// absoluto do semestre é a fonte única da verdade — ver roadmap-v2.md, 3.1).
    /// 100 dias não divide igual em 18 semanas, então isso é só decoração coerente.
    /// </summary>
    public int week => Mathf.Clamp(
        Mathf.RoundToInt(GameProgress.SemesterDay / (GameProgress.SemesterTotalDays / (float)totalWeeks)),
        1, totalWeeks);

    private readonly string[] disciplines =
    {
        "Fundamentos da Programação",
        "Interação Humano-Computador",
        "Ética",
        "Matemática Básica",
        "Intro. à Engenharia de Software",
    };
    private GameObject caderneta;
    private Text cadernetaText;
    private Text daysLeftText;
    private bool open;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildUI();
        SetCaderneta(false);
        UpdateDaysLeft();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        UpdateDaysLeft();

        // ESC abre/fecha a caderneta (não durante diálogo, cutscene de abertura,
        // nem na tela de fim). Na abertura o ESC é o "pular" da cutscene.
        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame
            && !DialogueManager.IsActive && !QuestManager.IsGameOver && !TitleScreen.IsShowing
            && !CampusTourCutscene.Active && !DayTransition.Active && !ExamManager.Active)
        {
            SetCaderneta(!open);
        }
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
        sb.AppendLine($"Dia {GameProgress.SemesterDay} de {GameProgress.SemesterTotalDays}  ·  Semana {week} / {totalWeeks}");
        sb.AppendLine();

        string objetivo = QuestManager.Instance != null ? QuestManager.Instance.CurrentObjectiveTitle : "";
        sb.AppendLine("Objetivo atual:");
        sb.AppendLine("  " + (string.IsNullOrEmpty(objetivo) ? "—" : objetivo));
        sb.AppendLine();

        sb.AppendLine("NOTAS");
        for (int i = 0; i < disciplines.Length; i++)
        {
            // Cada nota vem da sua fonte real (provas/interações).
            // Ordem: 0 FUP, 1 IHC, 2 Ética, 3 Matemática, 4 IES.
            float g = i == 0 ? GameProgress.FupGrade
                    : i == 1 ? GameProgress.IhcGrade
                    : i == 2 ? GameProgress.EthicsGrade
                    : i == 3 ? GameProgress.MathGrade
                    : GameProgress.IesGrade;
            string nota = g < 0f ? "—" : g.ToString("0.0");
            sb.AppendLine($"{disciplines[i]}:  {nota}");
        }
        sb.AppendLine();
        sb.AppendLine("(ESC para fechar)");
        cadernetaText.text = sb.ToString();
    }

    private int lastDaysLeftShown = int.MinValue;

    /// <summary>Contador fixo no topo da tela: quantos dias faltam pro fim do semestre.</summary>
    private void UpdateDaysLeft()
    {
        if (daysLeftText == null) return;
        int daysLeft = Mathf.Max(0, GameProgress.SemesterTotalDays - GameProgress.SemesterDay);
        if (daysLeft == lastDaysLeftShown) return;
        lastDaysLeftShown = daysLeft;
        daysLeftText.text = daysLeft == 1 ? "Falta 1 dia pro fim do semestre" : $"Faltam {daysLeft} dias pro fim do semestre";
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

        // Contador de dias do semestre (canto superior direito, sempre visível —
        // mesmo lugar/caixa onde antes ficava a barra de estresse, removida em
        // 04/07/2026).
        daysLeftText = CreateText(canvasGO.transform, "DaysLeft", font, 24, TextAnchor.MiddleRight);
        daysLeftText.color = new Color(0.9f, 0.9f, 0.9f);
        var dRT = daysLeftText.rectTransform;
        dRT.anchorMin = new Vector2(1f, 1f);
        dRT.anchorMax = new Vector2(1f, 1f);
        dRT.pivot = new Vector2(1f, 1f);
        dRT.anchoredPosition = new Vector2(-28f, -22f);
        dRT.sizeDelta = new Vector2(340f, 36f);

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
