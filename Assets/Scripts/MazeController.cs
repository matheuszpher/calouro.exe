using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Minigame "Prova-Labirinto". O labirinto fica numa região da própria cena,
/// fora da vista do campus. Ao iniciar, teleporta o jogador para a entrada e
/// liga um cronômetro; ao chegar na saída, o tempo vira nota (0–10) de
/// Matemática e o jogador volta ao campus.
/// </summary>
public class MazeController : MonoBehaviour
{
    public static MazeController Instance { get; private set; }
    public static bool InMaze { get; private set; }

    [Tooltip("Posição da entrada do labirinto (definida pelo montador).")]
    public Vector3 mazeStart;

    private float timer;
    private Vector3 returnPos;
    private GameObject player;
    private CameraFollow2D cam;

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

    public void StartMaze(Vector3 returnTo)
    {
        if (InMaze) return;
        EnsureRefs();
        returnPos = returnTo;
        InMaze = true;
        timer = 0f;
        if (cam != null) cam.useBounds = false;
        Teleport(mazeStart);
        if (timerText != null) timerText.gameObject.SetActive(true);
    }

    public void Finish()
    {
        if (!InMaze) return;
        InMaze = false;

        // Tempo → nota: 8s ou menos = 10; 40s = 3; interpolado; limitado a [0,10].
        float grade = Mathf.Clamp(10f - (timer - 8f) * (7f / 32f), 0f, 10f);
        grade = Mathf.Round(grade * 10f) / 10f;
        GameProgress.MathGrade = grade;

        if (cam != null) cam.useBounds = true;
        if (timerText != null) timerText.gameObject.SetActive(false);
        Teleport(returnPos);
        ShowResult($"Prova de Matemática concluída!\nTempo: {timer:0.0}s   Nota: {grade:0.0}\n\n(Veja na caderneta — ESC)");
    }

    private void Update()
    {
        if (InMaze)
        {
            timer += Time.deltaTime;
            if (timerText != null) timerText.text = $"Prova-Labirinto — Tempo: {timer:0.0}s";
        }

        if (resultHideAt > 0f && Time.unscaledTime >= resultHideAt)
        {
            resultHideAt = -1f;
            if (resultPanel != null) resultPanel.SetActive(false);
        }
    }

    private void ShowResult(string msg)
    {
        if (resultText != null) resultText.text = msg;
        if (resultPanel != null) resultPanel.SetActive(true);
        resultHideAt = Time.unscaledTime + 4f;
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
