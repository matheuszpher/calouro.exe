using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Tela de título (overlay). Aparece ao iniciar, pausa o jogo, deixa digitar o
/// nome do calouro e, em seguida, escolher o personagem (calouro ou caloura).
/// Enter avança/começa; Esc sai do jogo; Backspace volta um passo.
/// </summary>
public class TitleScreen : MonoBehaviour
{
    public static bool IsShowing { get; private set; }

    private enum Step { Name, Character }
    private Step step = Step.Name;

    private GameObject panel;
    private GameObject nameStep;
    private GameObject charStep;
    private Text nameText;
    private string playerName = "";

    // 0 = calouro (homem), 1 = caloura (mulher).
    private int charIndex = 0;
    private static readonly string[] CharIds = { "calouro", "caloura" };
    private readonly Image[] cards = new Image[2];
    private readonly Image[] cardImages = new Image[2];
    private PlayerAppearance appearance;

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
        GoToNameStep();
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
        if (!IsShowing || step != Step.Name) return;
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

        if (kb.escapeKey.wasPressedThisFrame)
        {
            Quit();
            return;
        }

        if (step == Step.Name)
        {
            if (kb.backspaceKey.wasPressedThisFrame && playerName.Length > 0)
            {
                playerName = playerName.Substring(0, playerName.Length - 1);
                RefreshName();
            }
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
                GoToCharacterStep();
        }
        else // Step.Character
        {
            if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame)
            {
                charIndex = 0;
                RefreshChar();
            }
            if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame)
            {
                charIndex = 1;
                RefreshChar();
            }
            if (kb.backspaceKey.wasPressedThisFrame)
                GoToNameStep();
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
                Confirm();
        }
    }

    private void GoToNameStep()
    {
        step = Step.Name;
        if (nameStep != null) nameStep.SetActive(true);
        if (charStep != null) charStep.SetActive(false);
        RefreshName();
    }

    private void GoToCharacterStep()
    {
        step = Step.Character;
        if (nameStep != null) nameStep.SetActive(false);
        if (charStep != null) charStep.SetActive(true);

        // Usa as próprias folhas fatiadas do Player como retrato das opções.
        appearance = Object.FindFirstObjectByType<PlayerAppearance>();
        if (appearance != null)
        {
            SetPreview(0, appearance.calouroFrames);
            SetPreview(1, appearance.calouraFrames);
        }
        RefreshChar();
    }

    private void SetPreview(int i, Sprite[] frames)
    {
        if (cardImages[i] == null || frames == null || frames.Length == 0) return;
        cardImages[i].sprite = frames[0]; // pose 0 = parado de frente
        cardImages[i].enabled = true;
    }

    private void Confirm()
    {
        GameProgress.PlayerCharacter = CharIds[charIndex];
        if (appearance == null) appearance = Object.FindFirstObjectByType<PlayerAppearance>();
        if (appearance != null) appearance.Apply();
        Hide();
    }

    private void RefreshName()
    {
        if (nameText != null)
            nameText.text = "Nome: " + (playerName.Length > 0 ? playerName : "_");
    }

    private void RefreshChar()
    {
        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] == null) continue;
            bool sel = i == charIndex;
            cards[i].color = sel ? new Color(0.2f, 0.35f, 0.6f, 1f)
                                 : new Color(0.12f, 0.14f, 0.18f, 1f);
        }
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
        Anchor(title.rectTransform, new Vector2(0.5f, 0.82f), new Vector2(1200f, 110f));

        var subtitle = CreateText(panel.transform, "Subtitle", font, 34, TextAnchor.MiddleCenter);
        subtitle.text = "Sobrevivendo ao Primeiro Semestre";
        subtitle.color = new Color(0.8f, 0.8f, 0.85f);
        Anchor(subtitle.rectTransform, new Vector2(0.5f, 0.74f), new Vector2(1200f, 60f));

        BuildNameStep(font);
        BuildCharStep(font);
    }

    private void BuildNameStep(Font font)
    {
        nameStep = new GameObject("NameStep");
        nameStep.transform.SetParent(panel.transform, false);
        StretchFull(nameStep);

        nameText = CreateText(nameStep.transform, "NameField", font, 40, TextAnchor.MiddleCenter);
        nameText.color = Color.white;
        Anchor(nameText.rectTransform, new Vector2(0.5f, 0.45f), new Vector2(1000f, 70f));

        var typeHint = CreateText(nameStep.transform, "TypeHint", font, 24, TextAnchor.MiddleCenter);
        typeHint.text = "(digite seu nome)";
        typeHint.color = new Color(0.6f, 0.6f, 0.65f);
        Anchor(typeHint.rectTransform, new Vector2(0.5f, 0.38f), new Vector2(800f, 40f));

        var hint = CreateText(nameStep.transform, "StartHint", font, 30, TextAnchor.MiddleCenter);
        hint.text = "[Enter] Continuar        [Esc] Sair";
        hint.color = new Color(1f, 0.9f, 0.5f);
        Anchor(hint.rectTransform, new Vector2(0.5f, 0.22f), new Vector2(1000f, 60f));
    }

    private void BuildCharStep(Font font)
    {
        charStep = new GameObject("CharStep");
        charStep.transform.SetParent(panel.transform, false);
        StretchFull(charStep);

        var prompt = CreateText(charStep.transform, "CharPrompt", font, 34, TextAnchor.MiddleCenter);
        prompt.text = "Escolha seu personagem";
        prompt.color = Color.white;
        Anchor(prompt.rectTransform, new Vector2(0.5f, 0.62f), new Vector2(1200f, 60f));

        string[] labels = { "Calouro", "Caloura" };
        float[] xs = { 0.4f, 0.6f };
        for (int i = 0; i < 2; i++)
        {
            var card = CreateImage(charStep.transform, "Card" + i);
            card.color = new Color(0.12f, 0.14f, 0.18f, 1f);
            Anchor(card.rectTransform, new Vector2(xs[i], 0.42f), new Vector2(300f, 380f));
            cards[i] = card;

            var preview = CreateImage(card.transform, "Preview");
            preview.enabled = false;
            preview.preserveAspect = true;
            Anchor(preview.rectTransform, new Vector2(0.5f, 0.56f), new Vector2(240f, 280f));
            cardImages[i] = preview;

            var label = CreateText(card.transform, "Label", font, 30, TextAnchor.MiddleCenter);
            label.text = labels[i];
            label.color = Color.white;
            Anchor(label.rectTransform, new Vector2(0.5f, 0.08f), new Vector2(280f, 50f));
        }

        var hint = CreateText(charStep.transform, "CharHint", font, 28, TextAnchor.MiddleCenter);
        hint.text = "[< >] Escolher     [Enter] Jogar     [Backspace] Voltar";
        hint.color = new Color(1f, 0.9f, 0.5f);
        Anchor(hint.rectTransform, new Vector2(0.5f, 0.18f), new Vector2(1200f, 60f));
    }

    private void Anchor(RectTransform rt, Vector2 anchor, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
    }

    private void StretchFull(GameObject go)
    {
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private Image CreateImage(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.raycastTarget = false;
        return img;
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
