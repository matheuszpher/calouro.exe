using UnityEngine;

/// <summary>
/// Movimento decorativo de NPC — dois modos:
///  - BackAndForth: anda alguns passos numa direção fixa e volta, em loop.
///  - RandomArea: fica escolhendo um ponto aleatório dentro de um quadrado ao
///    redor de onde nasceu e andando até lá (com uma pequena pausa), soltando
///    o bicho mais livre sem deixar sair da zona.
/// Move o transform direto (o NPC não tem Rigidbody2D, só o CircleCollider2D de
/// trigger da interação) e pausa durante diálogo/pausa do jogo.
/// </summary>
public class NpcPatrol : MonoBehaviour
{
    public enum Mode { BackAndForth, RandomArea }
    public Mode mode = Mode.BackAndForth;

    [Header("Modo BackAndForth")]
    public Vector2 direction = Vector2.right;
    public float stepDistance = 0.6f; // tamanho de cada "passo"
    public int steps = 4;             // nunca passa disso antes de voltar

    [Header("Modo RandomArea")]
    public float areaSize = 10f;      // quadrado areaSize x areaSize ao redor da origem
    public float waitMin = 0.4f;
    public float waitMax = 1.6f;

    public float speed = 1.0f;

    private Vector3 origin;
    private Vector3 target;
    private bool goingOut = true;
    private float waitTimer;

    private void Start()
    {
        origin = transform.position;
        if (mode == Mode.RandomArea)
            PickRandomTarget();
        else
            target = origin + (Vector3)(direction.normalized * (stepDistance * steps));
    }

    private void PickRandomTarget()
    {
        float half = areaSize / 2f;
        target = origin + new Vector3(Random.Range(-half, half), Random.Range(-half, half), 0f);
    }

    private void Update()
    {
        if (DialogueManager.IsActive) return;

        if (mode == Mode.RandomArea)
        {
            if (waitTimer > 0f) { waitTimer -= Time.deltaTime; return; }
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            if (Vector3.Distance(transform.position, target) < 0.05f)
            {
                waitTimer = Random.Range(waitMin, waitMax);
                PickRandomTarget();
            }
            return;
        }

        Vector3 dest = goingOut ? target : origin;
        transform.position = Vector3.MoveTowards(transform.position, dest, speed * Time.deltaTime);
        if (Vector3.Distance(transform.position, dest) < 0.02f)
            goingOut = !goingOut;
    }
}
