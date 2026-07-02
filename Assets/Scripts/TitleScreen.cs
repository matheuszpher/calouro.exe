using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Tela de título (overlay). Aparece ao iniciar, pausa o jogo, deixa digitar o
/// nome do calouro e começa com Enter. Esc sai do jogo.
/// </summary>
public class TitleScreen : MonoBehaviour
{
    public static bool IsShowing { get; private set; }

    private GameObject panel;
    private Text nameText;
    private string playerName = "";

    private void Awake()
    {
        BuildUI();
    }

    private void OnEnable()
    {
        if (Keyboard.current != null) Keyboard.current.onTextInput += OnTextInput;
    }

    private void OnDisable()
    {
        if (Keyboard.current != null) Keyboard.current.onTextInput -= OnTextInput;
    }

    private void Start()
    {
        // Voltando do minigame de pingue-pongue: a cena recarrega inteira, mas
        // isso não é um novo jogo — não mostra a tela de título (ver
        // InteriorController.Awake(), que já restaurou a Convivência antes disso
        // rodar). Start() roda depois de todos os Awake, então é seguro consumir
        // o flag aqui.
        if (PingPongSession.Active)
        {
            PingPongSession.Active = false;
            // O painel nasce ATIVO por padrão (BuildUI não o esconde) — só Show()
            // e Hide() mexem nisso. Sem chamar nenhum dos dois, ele continuava
            // visível mesmo com IsShowing falso, e sem IsShowing nada nele reagia
            // a tecla (nem Enter, nem digitar). Esconde direto aqui, sem passar
            // por Hide() — Hide() sobrescreveria GameProgress.PlayerName pra
            // "Calouro" (o campo local playerName nasce vazio nesta instância).
            if (panel != null) panel.SetActive(false);
            return;
        }
        Show();
    }

    private void Show()
    {
        IsShowing = true;
        if (panel != null) panel.SetActive(true);
        Time.timeScale = 0f;
        RefreshName();
    }

    private void Hide()
    {
        IsShowing = false;
        if (panel != null) panel.SetActive(false);
        Time.timeScale = 1f;
        GameProgress.PlayerName = string.IsNullOrWhiteSpace(playerName) ? "Calouro" : playerName.Trim();
    }

    private void OnTextInput(char c)
    {
        if (!IsShowing) return;
        if ((char.IsLetterOrDigit(c) || c == ' ') && playerName.Length < 16)
        {
            playerName += c;
            RefreshName();
        }
    }

    private void Update()
    {
        if (!IsShowing) return;
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.backspaceKey.wasPressedThisFrame && playerName.Length > 0)
        {
            playerName = playerName.Substring(0, playerName.Length - 1);
            RefreshName();
        }
        if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            Hide();
        if (kb.escapeKey.wasPressedThisFrame)
            Quit();
    }

    private void RefreshName()
    {
        if (nameText != null)
            nameText.text = "Nome: " + (playerName.Length > 0 ? playerName : "_");
    }

    private void Quit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // ------------------------------------------------------------------ UI

    private void BuildUI()
    {
        var canvasGO = new GameObject("TitleCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 110;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        panel = new GameObject("TitlePanel");
        panel.transform.SetParent(canvasGO.transform, false);
        var img = panel.AddComponent<Image>();
        img.color = new Color(0.04f, 0.05f, 0.08f, 0.98f);
        var pRT = panel.GetComponent<RectTransform>();
        pRT.anchorMin = Vector2.zero;
        pRT.anchorMax = Vector2.one;
        pRT.offsetMin = Vector2.zero;
        pRT.offsetMax = Vector2.zero;

        var title = CreateText(panel.transform, "Title", font, 72, TextAnchor.MiddleCenter);
        title.text = "calouro.exe";
        title.color = new Color(0.4f, 0.7f, 1f);
        title.fontStyle = FontStyle.Bold;
        Anchor(title.rectTransform, new Vector2(0.5f, 0.72f), new Vector2(1200f, 110f));

        var subtitle = CreateText(panel.transform, "Subtitle", font, 34, TextAnchor.MiddleCenter);
        subtitle.text = "Sobrevivendo ao Primeiro Semestre";
        subtitle.color = new Color(0.8f, 0.8f, 0.85f);
        Anchor(subtitle.rectTransform, new Vector2(0.5f, 0.62f), new Vector2(1200f, 60f));

        nameText = CreateText(panel.transform, "NameField", font, 40, TextAnchor.MiddleCenter);
        nameText.color = Color.white;
        Anchor(nameText.rectTransform, new Vector2(0.5f, 0.45f), new Vector2(1000f, 70f));

        var typeHint = CreateText(panel.transform, "TypeHint", font, 24, TextAnchor.MiddleCenter);
        typeHint.text = "(digite seu nome)";
        typeHint.color = new Color(0.6f, 0.6f, 0.65f);
        Anchor(typeHint.rectTransform, new Vector2(0.5f, 0.38f), new Vector2(800f, 40f));

        var hint = CreateText(panel.transform, "StartHint", font, 30, TextAnchor.MiddleCenter);
        hint.text = "[Enter] Jogar        [Esc] Sair";
        hint.color = new Color(1f, 0.9f, 0.5f);
        Anchor(hint.rectTransform, new Vector2(0.5f, 0.22f), new Vector2(1000f, 60f));
    }

    private void Anchor(RectTransform rt, Vector2 anchor, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
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
