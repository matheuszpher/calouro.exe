using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Minigame "Prova-Labirinto". 4 labirintos em sequência, cada um numa região
/// própria da cena, fora da vista do campus, valendo 2.5 pontos cada (soma
/// vira a nota de Matemática). Ao iniciar, teleporta o jogador pro 1º
/// labirinto e liga o cronômetro; ao chegar na saída, o tempo daquela rodada
/// vira pontuação, e o jogador é teleportado pro próximo — até completar os 4.
/// </summary>
public class MazeController : MonoBehaviour
{
    public static MazeController Instance { get; private set; }
    public static bool InMaze { get; private set; }

    [Tooltip("Ponto de entrada de cada um dos 4 labirintos, na ordem (definido pelo montador).")]
    public Vector3[] mazeStarts = new Vector3[4];

    // Cada rodada vale 2.5 pontos; tempo bom/ruim cresce junto com o tamanho do
    // labirinto (mapas maiores demoram mais mesmo sendo bem jogados).
    private static readonly float[] RoundGoodTime = { 8f, 14f, 22f, 32f };
    private static readonly float[] RoundBadTime = { 24f, 38f, 55f, 75f };
    private const float PointsPerRound = 2.5f;

    private int round;
    private int roundsLimit;
    private float roundsTotal;
    private float timer;
    private Vector3 returnPos;
    private GameObject player;
    private CameraFollow2D cam;
    private System.Action<float> reviewOnDone;

    private Text timerText;
    private GameObject resultPanel;
    private Text resultText;
    private float resultHideAt = -1f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        InMaze = false;
        BuildUI();
        if (timerText != null) timerText.gameObject.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) { Instance = null; InMaze = false; }
    }

    /// <summary>
    /// Inicia os labirintos. Uso normal (prova oficial, Dia 20): StartMaze(returnTo),
    /// joga os 4 mapas e grava GameProgress.MathGrade sozinho. Uso pra revisão (ex.:
    /// SQ2 do Gabriel/Gabriela, Dia 32): passa onDone pra receber a nota da rodada em
    /// vez de mexer em MathGrade direto, e rounds pra jogar menos mapas (os primeiros
    /// da lista, mais fáceis) em vez dos 4.
    /// </summary>
    public void StartMaze(Vector3 returnTo, System.Action<float> onDone = null, int rounds = 0)
    {
        if (InMaze) return;
        EnsureRefs();
        returnPos = returnTo;
        InMaze = true;
        round = 0;
        roundsLimit = rounds > 0 ? Mathf.Min(rounds, mazeStarts.Length) : mazeStarts.Length;
        reviewOnDone = onDone;
        roundsTotal = 0f;
        timer = 0f;
        if (cam != null) cam.useBounds = false;
        Teleport(StartOf(round));
        if (timerText != null) timerText.gameObject.SetActive(true);
    }

    /// <summary>Chamado pelo MazeExit da rodada atual — avança pro próximo mapa ou fecha a prova.</summary>
    public void Finish()
    {
        if (!InMaze) return;

        int goodBadIndex = Mathf.Clamp(round, 0, RoundGoodTime.Length - 1);
        float span = Mathf.Max(0.01f, RoundBadTime[goodBadIndex] - RoundGoodTime[goodBadIndex]);
        float factor = Mathf.Clamp01(1f - (timer - RoundGoodTime[goodBadIndex]) / span);
        roundsTotal += factor * PointsPerRound;

        round++;
        if (round < roundsLimit)
        {
            timer = 0f;
            Teleport(StartOf(round));
            ShowResult($"Mapa {round}/{roundsLimit} concluído! Próximo labirinto...", 1.5f);
            return;
        }

        // Fim dos mapas: soma vira nota de 0 a 10 (rescalada pro total de rodadas
        // jogadas, pra uma revisão de 2 mapas valer o mesmo range que a prova de 4).
        InMaze = false;
        float grade = Mathf.Round(Mathf.Clamp(roundsTotal / (roundsLimit * PointsPerRound) * 10f, 0f, 10f) * 10f) / 10f;

        if (cam != null) cam.useBounds = true;
        if (timerText != null) timerText.gameObject.SetActive(false);
        Teleport(returnPos);

        if (reviewOnDone != null)
        {
            var cb = reviewOnDone;
            reviewOnDone = null;
            ShowResult($"Revisão de Matemática concluída!\nNota da rodada: {grade:0.0}");
            cb.Invoke(grade);
            return;
        }

        GameProgress.MathGrade = grade;
        ShowResult($"Prova de Matemática concluída!\nNota: {grade:0.0} (soma dos 4 mapas)\n\n(Veja na caderneta — ESC)");

        // Conclui o objetivo da prova de Matemática, se for o atual.
        QuestManager.Instance?.ForceComplete("prova_mat");
    }

    private Vector3 StartOf(int r) => (r >= 0 && r < mazeStarts.Length) ? mazeStarts[r] : Vector3.zero;

    private void Update()
    {
        if (InMaze)
        {
            timer += Time.deltaTime;
            if (timerText != null)
                timerText.text = $"Prova-Labirinto — Mapa {round + 1}/{roundsLimit} — Tempo: {timer:0.0}s";
        }

        if (resultHideAt > 0f && Time.unscaledTime >= resultHideAt)
        {
            resultHideAt = -1f;
            if (resultPanel != null) resultPanel.SetActive(false);
        }
    }

    private void ShowResult(string msg, float duration = 4f)
    {
        if (resultText != null) resultText.text = msg;
        if (resultPanel != null) resultPanel.SetActive(true);
        resultHideAt = Time.unscaledTime + duration;
    }

    private void Teleport(Vector3 pos)
    {
        if (player == null) return;
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
        player.transform.position = pos;
        if (rb != null) rb.position = pos;
    }

    private void EnsureRefs()
    {
        if (player == null) player = GameObject.FindWithTag("Player");
        if (cam == null && Camera.main != null) cam = Camera.main.GetComponent<CameraFollow2D>();
    }

    // ------------------------------------------------------------------ UI

    private void BuildUI()
    {
        var canvasGO = new GameObject("MazeCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 85;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        timerText = CreateText(canvasGO.transform, "Timer", font, 32, TextAnchor.UpperCenter);
        timerText.color = Color.white;
        var tRT = timerText.rectTransform;
        tRT.anchorMin = new Vector2(0.5f, 1f);
        tRT.anchorMax = new Vector2(0.5f, 1f);
        tRT.pivot = new Vector2(0.5f, 1f);
        tRT.anchoredPosition = new Vector2(0f, -20f);
        tRT.sizeDelta = new Vector2(700f, 50f);

        resultPanel = new GameObject("MazeResult");
        resultPanel.transform.SetParent(canvasGO.transform, false);
        var rImg = resultPanel.AddComponent<Image>();
        rImg.color = new Color(0f, 0f, 0f, 0.85f);
        var rRT = resultPanel.GetComponent<RectTransform>();
        rRT.anchorMin = new Vector2(0.2f, 0.4f);
        rRT.anchorMax = new Vector2(0.8f, 0.6f);
        rRT.offsetMin = Vector2.zero;
        rRT.offsetMax = Vector2.zero;

        resultText = CreateText(resultPanel.transform, "MazeResultText", font, 30, TextAnchor.MiddleCenter);
        resultText.color = Color.white;
        var rtRT = resultText.rectTransform;
        rtRT.anchorMin = Vector2.zero;
        rtRT.anchorMax = Vector2.one;
        rtRT.offsetMin = Vector2.zero;
        rtRT.offsetMax = Vector2.zero;
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
