using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Sistema de diálogo simples. Constrói a própria UI (caixa de fala + dica)
/// em tempo de execução. Aperte E (ou clique) para iniciar/avançar.
/// Mostra uma dica "Aperte E para falar" quando o jogador está perto de um NPC.
/// </summary>
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }
    public static bool IsActive { get; private set; }

    private GameObject panel;
    private Text nameText;
    private Text bodyText;
    private GameObject hint;
    private Text hintText;

    private string speaker;
    private string[] lines;
    private int index;
    private NpcInteractable nearby;
    private NpcInteractable activeNpc;
    private bool choosing;
    private System.Action<int> choiceCallback;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        IsActive = false;
        BuildUI();
        HidePanel();
        HideHint();
    }

    private void OnDestroy()
    {
        if (Instance == this) { Instance = null; IsActive = false; }
    }

    private void Update()
    {
        if (TitleScreen.IsShowing) return;

        // Modo de escolha: responde com 1 ou 2.
        if (choosing)
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) Choose(0);
                else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) Choose(1);
            }
            return;
        }

        bool pressed = (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                    || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);

        if (IsActive)
        {
            if (pressed) Advance();
        }
        else if (nearby != null && pressed)
        {
            activeNpc = nearby;
            StartDialogue(nearby.npcName, nearby.lines);
        }
    }

    public void SetNearbyNpc(NpcInteractable npc)
    {
        nearby = npc;
        if (!IsActive) ShowHint($"Aperte E para falar com {npc.npcName}");
    }

    public void ClearNearbyNpc(NpcInteractable npc)
    {
        if (nearby == npc)
        {
            nearby = null;
            HideHint();
        }
    }

    public void StartDialogue(string who, string[] content)
    {
        if (content == null || content.Length == 0) return;
        speaker = who;
        lines = content;
        index = 0;
        IsActive = true;
        HideHint();
        ShowPanel();
        Render();
    }

    private void Advance()
    {
        index++;
        if (lines == null || index >= lines.Length)
        {
            EndDialogue();
            return;
        }
        Render();
    }

    private void EndDialogue()
    {
        IsActive = false;
        HidePanel();

        var npc = activeNpc;
        activeNpc = null;
        if (npc != null && QuestManager.Instance != null)
            QuestManager.Instance.OnTalked(npc.npcId);

        // Se a quest não abriu uma escolha/continuação, volta a mostrar a dica.
        if (!IsActive && !choosing && nearby != null)
            ShowHint($"Aperte E para falar com {nearby.npcName}");
    }

    /// <summary>Mostra uma pergunta com duas opções (responder com 1 ou 2).</summary>
    public void StartChoice(string who, string question, string optionA, string optionB, System.Action<int> onChosen)
    {
        choiceCallback = onChosen;
        choosing = true;
        IsActive = true;
        HideHint();
        ShowPanel();
        if (nameText != null) nameText.text = who;
        if (bodyText != null) bodyText.text = $"{question}\n\n[1] {optionA}\n[2] {optionB}";
    }

    private void Choose(int index)
    {
        choosing = false;
        IsActive = false;
        HidePanel();
        var cb = choiceCallback;
        choiceCallback = null;
        cb?.Invoke(index);
    }

    private void Render()
    {
        if (nameText != null) nameText.text = speaker;
        if (bodyText != null) bodyText.text = lines[index];
    }

    // ------------------------------------------------------------------ UI

    private void BuildUI()
    {
        var canvasGO = new GameObject("DialogueCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Caixa de diálogo (embaixo).
        panel = new GameObject("DialoguePanel");
        panel.transform.SetParent(canvasGO.transform, false);
        var pImg = panel.AddComponent<Image>();
        pImg.color = new Color(0f, 0f, 0f, 0.82f);
        var pRT = panel.GetComponent<RectTransform>();
        pRT.anchorMin = new Vector2(0.05f, 0.05f);
        pRT.anchorMax = new Vector2(0.95f, 0.28f);
        pRT.offsetMin = Vector2.zero;
        pRT.offsetMax = Vector2.zero;

        nameText = CreateText(panel.transform, "Name", font, 34, TextAnchor.UpperLeft);
        nameText.color = new Color(1f, 0.85f, 0.3f);
        nameText.fontStyle = FontStyle.Bold;
        var nRT = nameText.rectTransform;
        nRT.anchorMin = new Vector2(0f, 1f);
        nRT.anchorMax = new Vector2(1f, 1f);
        nRT.pivot = new Vector2(0f, 1f);
        nRT.anchoredPosition = new Vector2(24f, -12f);
        nRT.sizeDelta = new Vector2(-48f, 46f);

        bodyText = CreateText(panel.transform, "Body", font, 28, TextAnchor.UpperLeft);
        bodyText.color = Color.white;
        var bRT = bodyText.rectTransform;
        bRT.anchorMin = Vector2.zero;
        bRT.anchorMax = Vector2.one;
        bRT.offsetMin = new Vector2(24f, 20f);
        bRT.offsetMax = new Vector2(-24f, -60f);

        // Dica "Aperte E".
        hint = new GameObject("Hint");
        hint.transform.SetParent(canvasGO.transform, false);
        var hImg = hint.AddComponent<Image>();
        hImg.color = new Color(0f, 0f, 0f, 0.6f);
        var hRT = hint.GetComponent<RectTransform>();
        hRT.anchorMin = new Vector2(0.5f, 0.32f);
        hRT.anchorMax = new Vector2(0.5f, 0.32f);
        hRT.pivot = new Vector2(0.5f, 0.5f);
        hRT.sizeDelta = new Vector2(620f, 56f);

        hintText = CreateText(hint.transform, "HintText", font, 26, TextAnchor.MiddleCenter);
        hintText.color = Color.white;
        var htRT = hintText.rectTransform;
        htRT.anchorMin = Vector2.zero;
        htRT.anchorMax = Vector2.one;
        htRT.offsetMin = Vector2.zero;
        htRT.offsetMax = Vector2.zero;
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
        t.verticalOverflow = VerticalWrapMode.Truncate;
        t.raycastTarget = false;
        return t;
    }

    /// <summary>Dica de ação genérica (ex.: portais), reaproveitando a UI de dica.</summary>
    public void ShowActionHint(string text) { if (!IsActive) ShowHint(text); }
    public void HideActionHint() { HideHint(); }

    private void ShowPanel() { if (panel != null) panel.SetActive(true); }
    private void HidePanel() { if (panel != null) panel.SetActive(false); }
    private void ShowHint(string t) { if (hint != null) { hint.SetActive(true); if (hintText != null) hintText.text = t; } }
    private void HideHint() { if (hint != null) hint.SetActive(false); }
}
