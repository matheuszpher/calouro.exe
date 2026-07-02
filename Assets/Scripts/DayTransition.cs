using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Corte de tela entre dias (e time skip): escurece a tela, mostra "Dia N
/// finalizado", troca o estado do jogo por baixo (avança o dia, reposiciona o
/// jogador, ativa o próximo objetivo) e mostra "Boa sorte no Dia N+1!" antes de
/// clarear. O jogador fica parado durante a cena.
/// Construído por código (overlay de Canvas), no estilo dos outros overlays.
/// </summary>
public class DayTransition : MonoBehaviour
{
    public static DayTransition Instance { get; private set; }
    public static bool Active { get; private set; }

    [Tooltip("Onde o jogador reaparece no começo de um novo dia (passarela da Guarita).")]
    public Vector2 campusSpawn = new Vector2(-6f, 18.5f);

    [Tooltip("Segundos com cada mensagem na tela.")]
    public float holdSeconds = 1.6f;
    [Tooltip("Segundos do fade (escurecer/clarear).")]
    public float fadeSeconds = 0.6f;

    private Image black;
    private Text message;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Active = false;
        BuildUI();
        SetAlpha(0f);
        if (message != null) message.text = "";
    }

    private void OnDestroy()
    {
        if (Instance == this) { Instance = null; Active = false; }
    }

    /// <summary>
    /// Roda a transição. onMidpoint é chamado com a tela toda preta (bom pra trocar
    /// dia/objetivo sem o jogador ver o teleporte).
    /// </summary>
    public void Play(string line1, string line2, System.Action onMidpoint)
    {
        StartCoroutine(Run(line1, line2, onMidpoint));
    }

    private IEnumerator Run(string line1, string line2, System.Action onMidpoint)
    {
        Active = true;

        yield return Fade(0f, 1f);

        if (message != null) message.text = line1;
        yield return new WaitForSecondsRealtime(holdSeconds);

        // Tela preta: reposiciona o jogador e troca o estado do jogo.
        RepositionPlayer();
        onMidpoint?.Invoke();

        if (message != null) message.text = line2;
        yield return new WaitForSecondsRealtime(holdSeconds);
        if (message != null) message.text = "";

        yield return Fade(1f, 0f);

        Active = false;
    }

    private void RepositionPlayer()
    {
        Vector3 pos = new Vector3(campusSpawn.x, campusSpawn.y, 0f);

        // Sai de qualquer interior (ex.: a sessão de estudo do Natan é dentro do RU)
        // e restaura os limites de câmera do campus, tudo com a tela preta.
        if (InteriorController.Instance != null)
        {
            InteriorController.Instance.ForceCampus(pos);
        }
        else
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                player.transform.position = pos;
                player.transform.localScale = Vector3.one;
                var rb = player.GetComponent<Rigidbody2D>();
                if (rb != null) { rb.position = campusSpawn; rb.linearVelocity = Vector2.zero; }
            }
        }

        // Encaixa a câmera no novo ponto pra não "voar" ao clarear.
        var cam = Camera.main;
        if (cam != null)
            cam.transform.position = new Vector3(campusSpawn.x, campusSpawn.y, cam.transform.position.z);
    }

    private IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        float dur = Mathf.Max(0.01f, fadeSeconds);
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(from, to, t / dur));
            yield return null;
        }
        SetAlpha(to);
    }

    private void SetAlpha(float a)
    {
        if (black != null) { var c = black.color; c.a = a; black.color = c; }
        if (message != null) { var c = message.color; c.a = a; message.color = c; }
    }

    private void BuildUI()
    {
        var canvasGO = new GameObject("DayTransitionCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 108; // acima da cutscene (105), abaixo da tela de título (110)
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        black = new GameObject("Black").AddComponent<Image>();
        black.transform.SetParent(canvasGO.transform, false);
        black.color = new Color(0f, 0f, 0f, 0f);
        black.raycastTarget = false;
        var rt = black.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var go = new GameObject("Message");
        go.transform.SetParent(canvasGO.transform, false);
        message = go.AddComponent<Text>();
        message.font = font;
        message.fontSize = 56;
        message.alignment = TextAnchor.MiddleCenter;
        message.color = new Color(1f, 1f, 1f, 0f);
        message.fontStyle = FontStyle.Bold;
        message.raycastTarget = false;
        var mRT = message.rectTransform;
        mRT.anchorMin = new Vector2(0.1f, 0.35f);
        mRT.anchorMax = new Vector2(0.9f, 0.65f);
        mRT.offsetMin = Vector2.zero;
        mRT.offsetMax = Vector2.zero;
    }
}
