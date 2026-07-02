using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Minigame de pingue-pongue contra o Vitim (convite feito na mesa da
/// Convivência). Roda numa cena própria (PingPongMinigame), carregada e
/// descarregada via SceneManager (ver VitimPingPongTrigger/PingPongSession).
/// Monta a própria arena e UI em código, igual o DialogueManager monta a caixa
/// de diálogo — não depende de nada pré-configurado na cena.
/// Vence quem chegar a 7 pontos primeiro, ou abrir 4 de vantagem antes disso.
/// A dificuldade do Vitim é propositalmente imperfeita (velocidade menor que a
/// do jogador + erro de mira) pra dar chance real de ganhar ou perder.
/// </summary>
public class PingPongGameController : MonoBehaviour
{
    private const float FieldHalfWidth = 9f;
    private const float FieldHalfHeight = 5.2f;
    private const float PaddleHalfHeight = 1.3f;
    private const float BallRadius = 0.18f;
    private const float PaddleX = FieldHalfWidth - 0.6f;

    private const float PlayerSpeed = 10f;
    private const float AiMaxSpeed = 8.5f;       // ainda mais lento que o jogador — dá chance de vencer
    private const float AiReactionNoise = 0.8f;  // erro de mira (unidades)
    private const float AiRetargetInterval = 0.22f; // só "decide" pra onde ir a cada N segundos

    private const float BallBaseSpeed = 9.75f;   // +50% sobre a versão anterior (6.5)
    private const float BallSpeedGrowth = 1.045f; // acelera um pouco a cada rebatida
    private const float BallMaxSpeed = 21f;      // +50% sobre a versão anterior (14)
    private const float MaxBounceAngleDeg = 50f;  // ângulo depende de onde bate na barra

    private const int WinScore = 7;
    private const int MercyMargin = 4;

    private static readonly string[] VitimLines =
    {
        "Boa bola!",
        "Pra um calouro você joga bem.",
        "Tu já jogava antes né, safado!",
        "Relaxa, essa foi sorte.",
        "Aí sim, hein!",
        "Vish, quase que eu não pego essa.",
        "Bora, ainda dá tempo de virar.",
        "Pega leve comigo!",
        "Isso que é reflexo de calouro.",
        "Tá on hoje, hein?",
    };

    private Transform playerPaddle;
    private Transform vitimPaddle;
    private Transform ball;

    private Text playerScoreText;
    private Text vitimScoreText;
    private GameObject messagePanel;
    private Text messageText;
    private GameObject resultPanel;
    private Text resultText;

    private int playerScore, vitimScore;
    private Vector2 ballVel;
    private bool matchOver;
    private bool serving;
    private float aiTargetY;
    private float aiRetimer;

    private void Awake()
    {
        BuildArena();
        BuildUI();
    }

    private void Start()
    {
        ServeBall(Random.value > 0.5f ? 1 : -1);
    }

    private void Update()
    {
        if (matchOver) return;

        MovePlayerPaddle();
        MoveVitimPaddle();
        if (!serving) MoveBall();
    }

    // ------------------------------------------------------------- Arena

    private void BuildArena()
    {
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        camGO.transform.position = new Vector3(0f, 0f, -10f);
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6.5f;
        cam.backgroundColor = new Color(0.04f, 0.05f, 0.07f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        camGO.AddComponent<AudioListener>();

        Sprite white = MakeWhiteSprite();

        CreateQuad("Quadra", Vector3.zero, new Vector2(FieldHalfWidth * 2f, FieldHalfHeight * 2f),
            new Color(0.07f, 0.35f, 0.16f), white, -10);
        CreateQuad("LinhaMeio", Vector3.zero, new Vector2(0.08f, FieldHalfHeight * 2f),
            new Color(1f, 1f, 1f, 0.35f), white, -9);
        CreateQuad("Parede_Cima", new Vector3(0f, FieldHalfHeight + 0.15f, 0f),
            new Vector2(FieldHalfWidth * 2f + 0.6f, 0.3f), Color.white, white, -8);
        CreateQuad("Parede_Baixo", new Vector3(0f, -FieldHalfHeight - 0.15f, 0f),
            new Vector2(FieldHalfWidth * 2f + 0.6f, 0.3f), Color.white, white, -8);

        playerPaddle = CreateQuad("BarraJogador", new Vector3(-PaddleX, 0f, 0f),
            new Vector2(0.35f, PaddleHalfHeight * 2f), new Color(0.3f, 0.65f, 1f), white, 1).transform;
        vitimPaddle = CreateQuad("BarraVitim", new Vector3(PaddleX, 0f, 0f),
            new Vector2(0.35f, PaddleHalfHeight * 2f), new Color(1f, 0.45f, 0.3f), white, 1).transform;
        ball = CreateQuad("Bola", Vector3.zero, new Vector2(BallRadius * 2f, BallRadius * 2f),
            Color.white, white, 2).transform;
    }

    private GameObject CreateQuad(string name, Vector3 pos, Vector2 size, Color color, Sprite sprite, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;
        return go;
    }

    private static Sprite MakeWhiteSprite()
    {
        var tex = new Texture2D(4, 4);
        var pixels = new Color[16];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
    }

    // ------------------------------------------------------------- Jogo

    private void MovePlayerPaddle()
    {
        var kb = Keyboard.current;
        float dir = 0f;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) dir += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) dir -= 1f;
        }
        float y = Mathf.Clamp(playerPaddle.position.y + dir * PlayerSpeed * Time.deltaTime,
            -FieldHalfHeight + PaddleHalfHeight, FieldHalfHeight - PaddleHalfHeight);
        playerPaddle.position = new Vector3(playerPaddle.position.x, y, 0f);
    }

    private void MoveVitimPaddle()
    {
        aiRetimer -= Time.deltaTime;
        if (aiRetimer <= 0f)
        {
            aiRetimer = AiRetargetInterval;
            aiTargetY = ball.position.y + Random.Range(-AiReactionNoise, AiReactionNoise);
        }
        float y = Mathf.MoveTowards(vitimPaddle.position.y, aiTargetY, AiMaxSpeed * Time.deltaTime);
        y = Mathf.Clamp(y, -FieldHalfHeight + PaddleHalfHeight, FieldHalfHeight - PaddleHalfHeight);
        vitimPaddle.position = new Vector3(vitimPaddle.position.x, y, 0f);
    }

    private void MoveBall()
    {
        Vector3 pos = ball.position + (Vector3)(ballVel * Time.deltaTime);

        if (pos.y > FieldHalfHeight - BallRadius) { pos.y = FieldHalfHeight - BallRadius; ballVel.y = -ballVel.y; }
        else if (pos.y < -FieldHalfHeight + BallRadius) { pos.y = -FieldHalfHeight + BallRadius; ballVel.y = -ballVel.y; }

        TryBounce(playerPaddle, ref pos, isPlayerSide: true);
        TryBounce(vitimPaddle, ref pos, isPlayerSide: false);

        ball.position = pos;

        if (pos.x < -FieldHalfWidth - 0.5f) StartCoroutine(PointScored(vitimWins: true));
        else if (pos.x > FieldHalfWidth + 0.5f) StartCoroutine(PointScored(vitimWins: false));
    }

    private void TryBounce(Transform paddle, ref Vector3 pos, bool isPlayerSide)
    {
        // Barra do jogador fica à esquerda (só rebate bola indo pra esquerda); a
        // do Vitim fica à direita (só rebate bola indo pra direita).
        bool approaching = isPlayerSide ? ballVel.x < 0f : ballVel.x > 0f;
        if (!approaching) return;

        float paddleX = paddle.position.x;
        bool crossedX = isPlayerSide
            ? pos.x - BallRadius <= paddleX + 0.15f
            : pos.x + BallRadius >= paddleX - 0.15f;
        if (!crossedX) return;

        float dy = Mathf.Abs(pos.y - paddle.position.y);
        if (dy > PaddleHalfHeight + BallRadius) return;

        float offset = Mathf.Clamp((pos.y - paddle.position.y) / PaddleHalfHeight, -1f, 1f);
        float speed = Mathf.Min(ballVel.magnitude * BallSpeedGrowth, BallMaxSpeed);
        float angle = offset * MaxBounceAngleDeg * Mathf.Deg2Rad;
        float xSign = isPlayerSide ? 1f : -1f; // depois de bater, a bola inverte de lado
        ballVel = new Vector2(Mathf.Cos(angle) * speed * xSign, Mathf.Sin(angle) * speed);

        pos.x = isPlayerSide ? paddleX + 0.15f + BallRadius : paddleX - 0.15f - BallRadius;
    }

    private IEnumerator PointScored(bool vitimWins)
    {
        serving = true;

        if (vitimWins) vitimScore++; else playerScore++;
        UpdateScoreUI();

        bool matchDecided = playerScore >= WinScore || vitimScore >= WinScore
            || (Mathf.Max(playerScore, vitimScore) >= MercyMargin && Mathf.Abs(playerScore - vitimScore) >= MercyMargin);

        if (matchDecided)
        {
            yield return EndMatch(playerScore > vitimScore);
            yield break;
        }

        ShowMessage(VitimLines[Random.Range(0, VitimLines.Length)]);
        yield return new WaitForSeconds(1.8f);
        HideMessage();

        ServeBall(vitimWins ? -1 : 1);
        serving = false;
    }

    private void ServeBall(int towardSign)
    {
        ball.position = Vector3.zero;
        float angle = Random.Range(-25f, 25f) * Mathf.Deg2Rad;
        ballVel = new Vector2(Mathf.Cos(angle) * BallBaseSpeed * towardSign, Mathf.Sin(angle) * BallBaseSpeed);
    }

    private IEnumerator EndMatch(bool playerWon)
    {
        matchOver = true;
        ShowResult(playerWon
            ? "Você venceu o Vitim no ping pong!"
            : "O Vitim levou essa... tenta de novo outra hora!");
        yield return new WaitForSeconds(2.6f);

        // Sinaliza pro QuestManager (ao recarregar a SampleScene) que a partida
        // aconteceu, pra dar o prêmio social e concluir a missão do ping-pong.
        PingPongSession.MatchPlayed = true;
        PingPongSession.PlayerWon = playerWon;
        SceneManager.LoadScene("SampleScene");
    }

    // ------------------------------------------------------------- UI

    private void BuildUI()
    {
        var canvasGO = new GameObject("PingPongCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        playerScoreText = CreateText(canvasGO.transform, "PlacarJogador", font, 64, TextAnchor.MiddleCenter,
            new Vector2(0.32f, 0.86f), new Vector2(160f, 90f));
        playerScoreText.color = new Color(0.3f, 0.65f, 1f);
        playerScoreText.text = "0";

        vitimScoreText = CreateText(canvasGO.transform, "PlacarVitim", font, 64, TextAnchor.MiddleCenter,
            new Vector2(0.68f, 0.86f), new Vector2(160f, 90f));
        vitimScoreText.color = new Color(1f, 0.45f, 0.3f);
        vitimScoreText.text = "0";

        var title = CreateText(canvasGO.transform, "Titulo", font, 28, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.94f), new Vector2(700f, 60f));
        title.text = "Você vs. Vitim — W/S ou setas";
        title.color = Color.white;

        messagePanel = new GameObject("PainelFalaVitim");
        messagePanel.transform.SetParent(canvasGO.transform, false);
        var mImg = messagePanel.AddComponent<Image>();
        mImg.color = new Color(0f, 0f, 0f, 0.75f);
        var mRT = messagePanel.GetComponent<RectTransform>();
        mRT.anchorMin = new Vector2(0.5f, 0.1f);
        mRT.anchorMax = new Vector2(0.5f, 0.1f);
        mRT.pivot = new Vector2(0.5f, 0.5f);
        mRT.sizeDelta = new Vector2(760f, 80f);
        messageText = CreateStretchedText(messagePanel.transform, "TextoFala", font, 26);
        messageText.color = Color.white;
        messagePanel.SetActive(false);

        resultPanel = new GameObject("PainelResultado");
        resultPanel.transform.SetParent(canvasGO.transform, false);
        var rImg = resultPanel.AddComponent<Image>();
        rImg.color = new Color(0f, 0f, 0f, 0.85f);
        var rRT = resultPanel.GetComponent<RectTransform>();
        rRT.anchorMin = Vector2.zero;
        rRT.anchorMax = Vector2.one;
        rRT.offsetMin = Vector2.zero;
        rRT.offsetMax = Vector2.zero;
        resultText = CreateStretchedText(resultPanel.transform, "TextoResultado", font, 44);
        resultText.color = Color.white;
        resultPanel.SetActive(false);
    }

    private Text CreateText(Transform parent, string name, Font font, int size, TextAnchor anchor,
        Vector2 anchorPoint, Vector2 sizeDelta)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.alignment = anchor;
        t.raycastTarget = false;
        var rt = t.rectTransform;
        rt.anchorMin = anchorPoint;
        rt.anchorMax = anchorPoint;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = Vector2.zero;
        return t;
    }

    private Text CreateStretchedText(Transform parent, string name, Font font, int size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.raycastTarget = false;
        var rt = t.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return t;
    }

    private void UpdateScoreUI()
    {
        if (playerScoreText != null) playerScoreText.text = playerScore.ToString();
        if (vitimScoreText != null) vitimScoreText.text = vitimScore.ToString();
    }

    private void ShowMessage(string text)
    {
        if (messagePanel != null) messagePanel.SetActive(true);
        if (messageText != null) messageText.text = "Vitim: \"" + text + "\"";
    }

    private void HideMessage() { if (messagePanel != null) messagePanel.SetActive(false); }

    private void ShowResult(string text)
    {
        if (resultPanel != null) resultPanel.SetActive(true);
        if (resultText != null) resultText.text = text;
    }
}
