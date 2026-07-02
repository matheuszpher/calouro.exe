using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Movimentação 2D top-down usando o novo Input System.
/// Lê WASD e as setas e move o personagem em 8 direções.
/// Se houver um Rigidbody2D, move via física (paredes/obstáculos bloqueiam).
/// Sem Rigidbody2D, move direto pelo transform.
/// </summary>
public class PlayerController2D : MonoBehaviour
{
    [Tooltip("Velocidade de movimento em unidades por segundo.")]
    public float moveSpeed = 5f;

    [Tooltip("Inverte o sprite ao mudar de direção horizontal.")]
    public bool flipSprite = true;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Vector2 input;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // Trava o movimento enquanto um diálogo estiver aberto, durante a cutscene
        // de ir até a mesa de pingue-pongue (ver VitimPingPongTrigger) ou durante o
        // passeio de abertura pelo campus (CampusTourCutscene).
        input = (DialogueManager.IsActive || VitimPingPongTrigger.CutsceneActive
                 || CampusTourCutscene.Active || DayTransition.Active || ExamManager.Active)
            ? Vector2.zero : ReadInput();

        // Normaliza para a diagonal não ser mais rápida.
        if (input.sqrMagnitude > 1f)
            input.Normalize();

        if (flipSprite && spriteRenderer != null && Mathf.Abs(input.x) > 0.01f)
            spriteRenderer.flipX = input.x < 0f;

        // Sem física: move direto pelo transform.
        if (rb == null)
            transform.Translate(input * moveSpeed * Time.deltaTime);
    }

    private void FixedUpdate()
    {
        // Com física: usa velocidade para respeitar colisões.
        if (rb != null)
            rb.linearVelocity = input * moveSpeed;
    }

    private Vector2 ReadInput()
    {
        Vector2 dir = Vector2.zero;
        Keyboard kb = Keyboard.current;
        if (kb == null)
            return dir;

        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) dir.x -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) dir.x += 1f;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) dir.y += 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) dir.y -= 1f;

        return dir;
    }
}
