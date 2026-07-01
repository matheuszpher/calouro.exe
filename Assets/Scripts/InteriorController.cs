using UnityEngine;

/// <summary>
/// Gerencia a troca de tela para os interiores dos prédios. As salas ficam numa
/// região afastada da cena (como o labirinto). Ao entrar, teleporta o jogador
/// para dentro da sala e ajusta os limites da câmera; ao sair, volta ao campus.
/// </summary>
public class InteriorController : MonoBehaviour
{
    public static InteriorController Instance { get; private set; }
    public static bool InRoom { get; private set; }

    private GameObject player;
    private CameraFollow2D cam;
    private Vector3 returnPos;
    private Vector2 campusMin, campusMax;
    private bool capturedCampus;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        InRoom = false;
    }

    private void OnDestroy()
    {
        if (Instance == this) { Instance = null; InRoom = false; }
    }

    public void EnterRoom(Vector3 spawn, Vector3 returnTo, Vector2 boundsMin, Vector2 boundsMax)
    {
        if (InRoom || MazeController.InMaze) return;
        EnsureRefs();

        if (cam != null && !capturedCampus)
        {
            campusMin = cam.boundsMin;
            campusMax = cam.boundsMax;
            capturedCampus = true;
        }

        returnPos = returnTo;
        InRoom = true;
        if (cam != null)
        {
            cam.boundsMin = boundsMin;
            cam.boundsMax = boundsMax;
            cam.useBounds = true;
        }
        Teleport(spawn);
    }

    public void ExitRoom()
    {
        if (!InRoom) return;
        InRoom = false;
        if (cam != null && capturedCampus)
        {
            cam.boundsMin = campusMin;
            cam.boundsMax = campusMax;
            cam.useBounds = true;
        }
        Teleport(returnPos);
    }

    private void Teleport(Vector3 pos)
    {
        if (player == null) return;
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
        player.transform.position = pos;
        if (rb != null) rb.position = pos;
    }

    private void EnsureRefs()
    {
        if (player == null) player = GameObject.FindWithTag("Player");
        if (cam == null && Camera.main != null) cam = Camera.main.GetComponent<CameraFollow2D>();
    }
}
