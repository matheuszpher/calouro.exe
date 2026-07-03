using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Abertura do Dia 1 (roda sozinha logo após a tela de título, uma vez por sessão).
/// O coordenador Jeferson percebe o calouro parado na passarela e sobe até ele, dá
/// as boas-vindas, mostra o campus (a câmera passeia pelos pontos-chave com legenda
/// da narração dele), indica a primeira aula e desce até o RU/administrativo, onde
/// entra (some). No fim, devolve o controle e define o primeiro objetivo na HUD.
/// O jogador fica parado o tempo todo; barras pretas (letterbox) dão clima de cena.
/// Não roda ao voltar de um minigame (aí a tela de título nem aparece — é o que a
/// flag sawTitle detecta).
/// </summary>
public class CampusTourCutscene : MonoBehaviour
{
    /// <summary>Verdadeiro enquanto a abertura roda (trava o jogador e o diálogo).</summary>
    public static bool Active { get; private set; }

    [System.Serializable]
    public class Stop
    {
        [Tooltip("Ponto do campus para onde a câmera vai.")]
        public Vector2 focus;
        [Tooltip("Fala do Jeferson nessa parada. {nome} vira o nome do jogador.")]
        [TextArea] public string line;
    }

    [Header("Abordagem inicial (Jeferson sobe até o calouro)")]
    [Tooltip("Enquadramento da câmera durante a abordagem.")]
    public Vector2 meetingFocus;
    [Tooltip("Onde o Jeferson para, perto do jogador.")]
    public Vector2 approachTarget;
    [Tooltip("Falas de boas-vindas ditas na abordagem. {nome} vira o nome do jogador.")]
    [TextArea] public string[] welcomeLines;

    [Header("Passeio pelo campus")]
    [Tooltip("Paradas do passeio (posição no campus + fala do Jeferson).")]
    public Stop[] stops;
    [Tooltip("Zoom (orthographic size) durante o passeio; menor = mais perto.")]
    public float tourOrthoSize = 6.5f;
    [Tooltip("Tempo do movimento da câmera entre uma parada e outra (segundos).")]
    public float moveDuration = 1.2f;

    [Header("Saída do Jeferson (anda até o RU/administrativo no fim)")]
    [Tooltip("O NPC do coordenador que caminha e entra no RU.")]
    public GameObject coordenador;
    [Tooltip("Waypoints do caminho do Jeferson até a porta do RU.")]
    public Vector2[] coordenadorExitPath;
    [Tooltip("Velocidade da caminhada do Jeferson (unidades/segundo).")]
    public float coordenadorWalkSpeed = 3.6f;

    private bool started;
    private bool sawTitle;
    private bool skip;

    private Camera cam;
    private CameraFollow2D follow;
    private float baseOrtho;

    private GameObject root;
    private RectTransform barTop, barBottom;
    private float barTargetH;
    private Text captionName, captionBody, hintText;

    private void Update()
    {
        if (started) return;

        // Se o passeio já rodou antes (retomando um save, ou voltando de uma
        // troca de cena inteira como o pingue-pongue — que recarrega o
        // GameObject do Jeferson do estado SALVO na cena, ou seja, visível de
        // novo), garante que ele não fique solto em frente ao RU. Checado toda
        // vez (não só no Awake) porque SaveSystem.Load() só roda bem depois,
        // quando o jogador escolhe "Continuar" na tela de título.
        if (GameProgress.CampusTourSeen)
        {
            if (coordenador != null) coordenador.SetActive(false);
            started = true;
            return;
        }

        if (TitleScreen.IsShowing) { sawTitle = true; return; }
        if (!sawTitle) return;                        // título nunca apareceu = volta de minigame → não roda
        Begin();
    }

    private void Begin()
    {
        started = true;
        GameProgress.CampusTourSeen = true;

        follow = Object.FindFirstObjectByType<CameraFollow2D>();
        cam = Camera.main;
        if (cam == null && follow != null) cam = follow.GetComponent<Camera>();
        if (cam == null) return;
        baseOrtho = cam.orthographicSize;

        if (DialogueManager.Instance != null) DialogueManager.Instance.HideActionHint();

        BuildUI();
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        Active = true;
        if (follow != null) follow.enabled = false;   // a câmera passa a ser controlada aqui

        yield return AnimateBars(true);

        // 1) Abordagem: enquadra o encontro e o Jeferson sobe até o jogador.
        yield return MoveCamera(meetingFocus, tourOrthoSize);
        yield return WalkCoordenador(approachTarget, followCam: false);
        FaceCoordenadorToPlayer();

        // 2) Boas-vindas.
        if (welcomeLines != null)
        {
            foreach (var line in welcomeLines)
            {
                SetCaption(line);
                ShowHint("[E] Continuar     [Esc] Pular");
                yield return WaitForAdvance();
                if (skip) break;
            }
        }

        // 3) Passeio pelo campus.
        if (!skip && stops != null)
        {
            foreach (var stop in stops)
            {
                yield return MoveCamera(stop.focus, tourOrthoSize);
                SetCaption(stop.line);
                ShowHint("[E] Continuar     [Esc] Pular");
                yield return WaitForAdvance();
                if (skip) break;
            }
        }

        HideCaption();

        // 4) O Jeferson vai até o RU/administrativo e entra (a câmera acompanha).
        if (skip)
        {
            if (coordenador != null) coordenador.SetActive(false);
        }
        else
        {
            if (coordenador != null)
                yield return MoveCamera(coordenador.transform.position, tourOrthoSize);
            yield return WalkCoordenadorExit();
        }

        // 5) Volta a câmera pro jogador e devolve o controle.
        var player = GameObject.FindWithTag("Player");
        if (player != null)
            yield return MoveCamera(player.transform.position, baseOrtho);

        yield return AnimateBars(false);

        cam.orthographicSize = baseOrtho;
        if (follow != null) follow.enabled = true;     // volta a seguir o jogador (suave)
        Active = false;
        if (root != null) Destroy(root);

        // 6) Inicia a sequência de objetivos (o QuestManager conhece o primeiro).
        if (QuestManager.Instance != null) QuestManager.Instance.StartSequence();
    }

    private void FaceCoordenadorToPlayer()
    {
        if (coordenador == null) return;
        var anim = coordenador.GetComponent<SpriteWalkAnimator>();
        if (anim == null) return;
        var player = GameObject.FindWithTag("Player");
        Vector2 toward = player != null
            ? (Vector2)(player.transform.position - coordenador.transform.position)
            : Vector2.down;
        anim.LockFacing(toward); // encara o jogador (mostra a frente) durante a fala
    }

    /// <summary>Anda o coordenador até um ponto; opcionalmente a câmera acompanha.</summary>
    private IEnumerator WalkCoordenador(Vector2 target, bool followCam)
    {
        if (coordenador == null) yield break;
        var anim = coordenador.GetComponent<SpriteWalkAnimator>();
        if (anim != null) anim.UnlockFacing(); // solta a pose pra a caminhada animar

        Vector3 dest = new Vector3(target.x, target.y, coordenador.transform.position.z);
        while (Vector2.Distance(coordenador.transform.position, target) > 0.06f)
        {
            coordenador.transform.position = Vector3.MoveTowards(
                coordenador.transform.position, dest, coordenadorWalkSpeed * Time.deltaTime);
            if (followCam) FollowWithCamera(coordenador.transform.position);
            yield return null;
        }
    }

    private IEnumerator WalkCoordenadorExit()
    {
        if (coordenador == null || coordenadorExitPath == null || coordenadorExitPath.Length == 0)
            yield break;

        foreach (var wp in coordenadorExitPath)
            yield return WalkCoordenador(wp, followCam: true);

        yield return new WaitForSeconds(0.15f);
        coordenador.SetActive(false); // entrou no RU/administrativo
    }

    private void FollowWithCamera(Vector3 p)
    {
        Vector3 target = new Vector3(p.x, p.y, cam.transform.position.z);
        cam.transform.position = Vector3.Lerp(cam.transform.position, target, Mathf.Clamp01(6f * Time.deltaTime));
    }

    private IEnumerator MoveCamera(Vector2 focus, float ortho)
    {
        Vector3 from = cam.transform.position;
        Vector3 to = new Vector3(focus.x, focus.y, from.z);
        float fromO = cam.orthographicSize;
        float t = 0f;
        float dur = Mathf.Max(0.01f, moveDuration);
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur));
            cam.transform.position = Vector3.Lerp(from, to, k);
            cam.orthographicSize = Mathf.Lerp(fromO, ortho, k);
            yield return null;
        }
        cam.transform.position = to;
        cam.orthographicSize = ortho;
    }

    private IEnumerator WaitForAdvance()
    {
        yield return null; // pula o frame atual pra não engolir o mesmo toque
        while (true)
        {
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) { skip = true; yield break; }
            bool press = (kb != null && (kb.eKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame
                                         || kb.spaceKey.wasPressedThisFrame))
                      || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
            if (press) yield break;
            yield return null;
        }
    }

    private IEnumerator AnimateBars(bool show)
    {
        float t = 0f, dur = 0.4f;
        float from = show ? 0f : barTargetH;
        float to = show ? barTargetH : 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float h = Mathf.Lerp(from, to, Mathf.Clamp01(t / dur));
            SetBars(h);
            yield return null;
        }
        SetBars(to);
    }

    private void SetBars(float h)
    {
        if (barTop != null) barTop.sizeDelta = new Vector2(0f, h);
        if (barBottom != null) barBottom.sizeDelta = new Vector2(0f, h);
    }

    private void SetCaption(string line)
    {
        if (captionName != null) captionName.text = "Jeferson";
        if (captionBody != null)
            captionBody.text = (line ?? "").Replace("{nome}", GameProgress.PlayerName);
    }

    private void HideCaption()
    {
        if (captionName != null) captionName.text = "";
        if (captionBody != null) captionBody.text = "";
        if (hintText != null) hintText.text = "";
    }

    private void ShowHint(string t) { if (hintText != null) hintText.text = t; }

    // ------------------------------------------------------------------ UI

    private void BuildUI()
    {
        var canvasGO = new GameObject("CampusTourCanvas");
        root = canvasGO;
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 105; // acima do diálogo (100), abaixo da tela de título (110)
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        barTargetH = 140f;

        barTop = MakeBar("BarTop", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
        barBottom = MakeBar("BarBottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f));
        SetBars(0f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Painel da legenda (acima da barra de baixo), no estilo da caixa de diálogo.
        var panelGO = new GameObject("CaptionPanel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        var pImg = panelGO.AddComponent<Image>();
        pImg.color = new Color(0f, 0f, 0f, 0.78f);
        pImg.raycastTarget = false;
        var pRT = panelGO.GetComponent<RectTransform>();
        pRT.anchorMin = new Vector2(0.08f, 0.16f);
        pRT.anchorMax = new Vector2(0.92f, 0.34f);
        pRT.offsetMin = Vector2.zero;
        pRT.offsetMax = Vector2.zero;

        captionName = MakeText(panelGO.transform, "CaptionName", font, 32, TextAnchor.UpperLeft);
        captionName.color = new Color(1f, 0.85f, 0.3f);
        captionName.fontStyle = FontStyle.Bold;
        var nRT = captionName.rectTransform;
        nRT.anchorMin = new Vector2(0f, 1f);
        nRT.anchorMax = new Vector2(1f, 1f);
        nRT.pivot = new Vector2(0f, 1f);
        nRT.anchoredPosition = new Vector2(24f, -12f);
        nRT.sizeDelta = new Vector2(-48f, 42f);

        captionBody = MakeText(panelGO.transform, "CaptionBody", font, 28, TextAnchor.UpperLeft);
        captionBody.color = Color.white;
        var bRT = captionBody.rectTransform;
        bRT.anchorMin = Vector2.zero;
        bRT.anchorMax = Vector2.one;
        bRT.offsetMin = new Vector2(24f, 16f);
        bRT.offsetMax = new Vector2(-24f, -56f);

        // Dica centralizada dentro da barra preta de baixo.
        hintText = MakeText(canvasGO.transform, "Hint", font, 22, TextAnchor.LowerCenter);
        hintText.color = new Color(1f, 0.9f, 0.5f);
        var hRT = hintText.rectTransform;
        hRT.anchorMin = new Vector2(0.1f, 0f);
        hRT.anchorMax = new Vector2(0.9f, 0f);
        hRT.pivot = new Vector2(0.5f, 0f);
        hRT.anchoredPosition = new Vector2(0f, 22f);
        hRT.sizeDelta = new Vector2(0f, 34f);
    }

    private RectTransform MakeBar(string name, Vector2 aMin, Vector2 aMax, Vector2 pivot)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root.transform, false);
        var img = go.AddComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.pivot = pivot;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, 0f);
        return rt;
    }

    private Text MakeText(Transform parent, string name, Font font, int size, TextAnchor anchor)
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
