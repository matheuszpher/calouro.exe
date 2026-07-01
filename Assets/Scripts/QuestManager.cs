using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Controla o objetivo da demo ("O Primeiro Dia"):
/// falar com o Coordenador → falar com o Natan (escolha) → chegar no Bloco 1.
/// Mostra o objetivo atual na tela e a tela de "Fim da demo".
/// </summary>
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }
    public static bool IsGameOver { get; private set; }

    private int step;          // 0: coord, 1: natan, 2: bloco1, 3: fim
    private bool acceptedHelp;

    private Text objectiveText;
    private GameObject endPanel;
    private Text endText;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        IsGameOver = false;
        Time.timeScale = 1f;
        BuildUI();
        UpdateObjective();
    }

    private void OnDestroy()
    {
        if (Instance == this) { Instance = null; IsGameOver = false; Time.timeScale = 1f; }
    }

    private void Update()
    {
        // Na tela de fim, Enter reinicia a demo.
        if (step != 3) return;
        var kb = Keyboard.current;
        if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void OnTalked(string npcId)
    {
        if (step == 0 && npcId == "coordenador")
        {
            step = 1;
            UpdateObjective();
        }
        else if (step == 1 && npcId == "natan")
        {
            DialogueManager.Instance?.StartChoice(
                "Natan",
                "Bora estudar programação juntos essa semana?",
                "Bora! (aceitar)",
                "Agora não, valeu (recusar)",
                choice =>
                {
                    acceptedHelp = (choice == 0);
                    // Escolha afeta o estresse (aceitar ajuda alivia; recusar pesa um pouco).
                    AcademicHud.Instance?.AddStress(acceptedHelp ? -15f : 10f);
                    step = 2;
                    UpdateObjective();
                });
        }
    }

    public void OnReachedGoal()
    {
        if (step != 2) return;
        step = 3;
        IsGameOver = true;
        ShowEnd();
    }

    private void UpdateObjective()
    {
        string t;
        switch (step)
        {
            case 0: t = "Objetivo: fale com o Coordenador (na Convivência)"; break;
            case 1: t = "Objetivo: vá ao RU (007, à esquerda) e fale com o Natan"; break;
            case 2: t = "Objetivo: vá até o Bloco 1 (001, à direita)"; break;
            default: t = ""; break;
        }
        if (objectiveText != null) objectiveText.text = t;
    }

    private void ShowEnd()
    {
        string msg = acceptedHelp
            ? "Você topou estudar com o Natan.\nSeu primeiro dia terminou bem — o semestre promete!"
            : "Você preferiu se virar sozinho por enquanto.\nPrimeiro dia concluído. Bom semestre, calouro!";

        if (endText != null) endText.text = "Fim da demo\n\n" + msg + "\n\n(Enter para jogar de novo)";
        if (endPanel != null) endPanel.SetActive(true);
        if (objectiveText != null) objectiveText.text = "";
        Time.timeScale = 0f;
    }

    // ------------------------------------------------------------------ UI

    private void BuildUI()
    {
        var canvasGO = new GameObject("QuestCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Objetivo (canto superior esquerdo).
        objectiveText = CreateText(canvasGO.transform, "Objective", font, 30, TextAnchor.UpperLeft);
        objectiveText.color = new Color(1f, 1f, 0.85f);
        objectiveText.fontStyle = FontStyle.Bold;
        var oRT = objectiveText.rectTransform;
        oRT.anchorMin = new Vector2(0f, 1f);
        oRT.anchorMax = new Vector2(0f, 1f);
        oRT.pivot = new Vector2(0f, 1f);
        oRT.anchoredPosition = new Vector2(28f, -22f);
        oRT.sizeDelta = new Vector2(900f, 60f);

        // Painel de fim (tela cheia).
        endPanel = new GameObject("EndPanel");
        endPanel.transform.SetParent(canvasGO.transform, false);
        var eImg = endPanel.AddComponent<Image>();
        eImg.color = new Color(0f, 0f, 0f, 0.92f);
        var eRT = endPanel.GetComponent<RectTransform>();
        eRT.anchorMin = Vector2.zero;
        eRT.anchorMax = Vector2.one;
        eRT.offsetMin = Vector2.zero;
        eRT.offsetMax = Vector2.zero;

        endText = CreateText(endPanel.transform, "EndText", font, 40, TextAnchor.MiddleCenter);
        endText.color = Color.white;
        var etRT = endText.rectTransform;
        etRT.anchorMin = new Vector2(0.1f, 0.2f);
        etRT.anchorMax = new Vector2(0.9f, 0.8f);
        etRT.offsetMin = Vector2.zero;
        etRT.offsetMax = Vector2.zero;

        endPanel.SetActive(false);
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
