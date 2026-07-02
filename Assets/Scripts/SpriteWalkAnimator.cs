using UnityEngine;

/// <summary>
/// Animação direcional 2D: escolhe o conjunto de poses conforme a direção do
/// movimento (frente/baixo, costas/cima, lado) e percorre os quadros para
/// parecer que está andando. Ao parar, mantém a pose "idle" da última direção.
/// O lado esquerdo é o direito espelhado (flipX). Diagonais usam a direção
/// dominante (mais horizontal = lado).
/// Funciona tanto com Rigidbody2D (Player, lê linearVelocity) quanto sem — nesse
/// caso estima a velocidade pela variação de transform.position (NPCs com
/// NpcPatrol, que só movem o transform direto, sem física).
/// </summary>
public class SpriteWalkAnimator : MonoBehaviour
{
    [Tooltip("Todas as poses fatiadas da folha (preenchido pelo montador da cena).")]
    public Sprite[] frames;

    [Header("Ciclos de caminhada (índices das poses)")]
    public int[] downFrames = { 0, 1 };   // andando de frente
    public int[] sideFrames = { 5, 6 };   // andando de lado
    public int[] upFrames = { 9 };        // de costas (só 1 pose nesta arte)

    [Header("Poses paradas (idle)")]
    public int downIdle = 8;
    public int sideIdle = 10;
    public int upIdle = 9;

    [Header("Velocidade")]
    public float framesPerSecond = 8f;

    [Tooltip("Inverter qual lado o sprite encara (se o lado sair ao contrário).")]
    public bool invertSide = false;

    [Tooltip("Balançar (flip) ao subir para simular passos, já que só há 1 pose de costas.")]
    public bool swayWhenUp = true;

    private enum Dir { Down, Up, Side }

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Vector3 lastPos;
    private float timer;
    private int step;
    private Dir dir = Dir.Down;
    private bool faceRight;
    private bool facingLocked;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        lastPos = transform.position;
    }

    /// <summary>
    /// Trava numa pose parada olhando na direção de towardTarget (ex.: do NPC pro
    /// jogador) até UnlockFacing() ser chamado. Usado pelo DialogueManager ao
    /// iniciar uma fala, pra virar o NPC de frente pra quem tá falando com ele.
    /// </summary>
    public void LockFacing(Vector2 towardTarget)
    {
        facingLocked = true;
        if (towardTarget.sqrMagnitude > 0.0001f)
        {
            if (Mathf.Abs(towardTarget.x) >= Mathf.Abs(towardTarget.y))
            {
                dir = Dir.Side;
                faceRight = towardTarget.x > 0f;
            }
            else
            {
                // Numa conversa o NPC nunca dá as costas pra quem tá falando com
                // ele — diferente da caminhada (onde "pra cima" mostra as costas),
                // aqui "jogador ao norte ou ao sul" sempre mostra a frente.
                dir = Dir.Down;
            }
        }
        timer = 0f;
        step = 0;
        SetFrame(IdleFor(dir));
        ApplyFlip(false);
    }

    /// <summary>Libera o Update() normal de novo (caminhada/idle por velocidade).</summary>
    public void UnlockFacing() => facingLocked = false;

    private void Update()
    {
        if (facingLocked) return;
        if (frames == null || frames.Length == 0 || spriteRenderer == null)
            return;

        // Com Rigidbody2D (Player) usa a velocidade física; sem ele (NPCs movidos
        // direto pelo transform, ex.: NpcPatrol) estima pela posição do quadro anterior.
        Vector2 v;
        if (rb != null)
        {
            v = rb.linearVelocity;
        }
        else
        {
            v = Time.deltaTime > 0f ? (Vector2)(transform.position - lastPos) / Time.deltaTime : Vector2.zero;
            lastPos = transform.position;
        }
        bool moving = v.sqrMagnitude > 0.01f;

        if (moving)
        {
            // Direção dominante (diagonais caem no eixo maior).
            if (Mathf.Abs(v.x) >= Mathf.Abs(v.y))
            {
                dir = Dir.Side;
                faceRight = v.x > 0f;
            }
            else
            {
                dir = v.y > 0f ? Dir.Up : Dir.Down;
            }

            timer += Time.deltaTime;
            float interval = 1f / Mathf.Max(1f, framesPerSecond);
            if (timer >= interval)
            {
                timer -= interval;
                step++;
            }

            int[] cycle = CycleFor(dir);
            SetFrame(cycle[step % cycle.Length]);
        }
        else
        {
            timer = 0f;
            step = 0;
            SetFrame(IdleFor(dir)); // para na última direção
        }

        ApplyFlip(moving);
    }

    private void ApplyFlip(bool moving)
    {
        bool flip = false;
        if (dir == Dir.Side)
            flip = faceRight ^ invertSide;
        else if (dir == Dir.Up && swayWhenUp && moving)
            flip = (step % 2 == 1); // balanço pra simular passos de costas
        spriteRenderer.flipX = flip;
    }

    private int[] CycleFor(Dir d)
    {
        switch (d)
        {
            case Dir.Up: return HasFrames(upFrames) ? upFrames : downFrames;
            case Dir.Side: return HasFrames(sideFrames) ? sideFrames : downFrames;
            default: return HasFrames(downFrames) ? downFrames : new[] { 0 };
        }
    }

    private int IdleFor(Dir d)
    {
        switch (d)
        {
            case Dir.Up: return upIdle;
            case Dir.Side: return sideIdle;
            default: return downIdle;
        }
    }

    private static bool HasFrames(int[] arr) => arr != null && arr.Length > 0;

    private void SetFrame(int index)
    {
        if (index >= 0 && index < frames.Length && frames[index] != null)
            spriteRenderer.sprite = frames[index];
    }
}
