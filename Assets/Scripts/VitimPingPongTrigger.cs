using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Fica no Vitim, na mesa de pingue-pongue da Convivência. Ao aceitar o convite
/// ("Bora, to dentro!" — ver DialogueManager.EndDialogue), anda ele e o jogador
/// até os lados opostos da mesa e carrega a cena separada do minigame, guardando
/// em PingPongSession tudo que precisa pra voltar exatamente pro mesmo lugar.
/// </summary>
public class VitimPingPongTrigger : MonoBehaviour
{
    public static bool CutsceneActive { get; private set; }

    public Vector3 vitimTableSpot;
    public Vector3 playerTableSpot;
    public float walkSpeed = 4.5f;

    private bool started;

    public void BeginMatch()
    {
        if (started) return;
        started = true;
        StartCoroutine(WalkToTableThenLoad());
    }

    private IEnumerator WalkToTableThenLoad()
    {
        CutsceneActive = true;

        var player = GameObject.FindWithTag("Player");
        var rb = player != null ? player.GetComponent<Rigidbody2D>() : null;
        var playerCtrl = player != null ? player.GetComponent<PlayerController2D>() : null;
        if (playerCtrl != null) playerCtrl.enabled = false;

        // Cutscene curta e roteirizada: o destino já foi escolhido livre de móveis,
        // então o caminho até lá anda direto pela posição (sem física), pra não
        // travar caso passe perto de algum colisor no meio do caminho. Kinematic
        // evita que o motor de física empurre o corpo de volta ao detectar overlap.
        if (rb != null) rb.bodyType = RigidbodyType2D.Kinematic;

        // Anda os dois até a posição final por até 5s (segurança contra distâncias
        // grandes) — depois desse prazo, encaixa direto.
        const float timeout = 5f;
        float elapsed = 0f;
        while (elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            bool playerDone = true;

            if (player != null)
            {
                bool done = Vector3.Distance(player.transform.position, playerTableSpot) <= 0.08f;
                if (!done)
                {
                    playerDone = false;
                    player.transform.position = Vector3.MoveTowards(player.transform.position, playerTableSpot, walkSpeed * Time.deltaTime);
                    if (rb != null) rb.position = player.transform.position;
                }
            }

            bool vitimDone = Vector3.Distance(transform.position, vitimTableSpot) <= 0.08f;
            if (!vitimDone)
                transform.position = Vector3.MoveTowards(transform.position, vitimTableSpot, walkSpeed * Time.deltaTime);

            if (playerDone && vitimDone) break;
            yield return null;
        }

        if (player != null)
        {
            player.transform.position = playerTableSpot;
            if (rb != null) { rb.position = playerTableSpot; rb.linearVelocity = Vector2.zero; }
        }
        transform.position = vitimTableSpot;

        yield return new WaitForSeconds(0.2f);

        var cam = Camera.main != null ? Camera.main.GetComponent<CameraFollow2D>() : null;
        PingPongSession.ReturnSpawn = playerTableSpot;
        PingPongSession.ReturnFront = InteriorController.Instance != null && InteriorController.Instance.PeekReturnPos().HasValue
            ? InteriorController.Instance.PeekReturnPos().Value
            : playerTableSpot;
        if (cam != null)
        {
            PingPongSession.RoomBoundsMin = cam.boundsMin;
            PingPongSession.RoomBoundsMax = cam.boundsMax;
        }
        PingPongSession.PlayerScale = player != null ? player.transform.localScale.x : 1f;
        PingPongSession.Active = true;

        CutsceneActive = false;
        started = false;

        // Reabilita o controle ANTES de trocar de cena: se "PingPongMinigame" não
        // existir/estiver fora dos Build Settings, LoadScene lança uma exceção e a
        // troca não acontece — sem isso, o jogador ficaria travado (ver
        // Tools > Calouro > Montar Cena do Pingue-Pongue).
        if (playerCtrl != null) playerCtrl.enabled = true;
        SceneManager.LoadScene("PingPongMinigame");
    }
}
