using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gerencia a troca de tela para os interiores (corredores dos blocos e salas).
/// Os interiores ficam numa região afastada da cena (como o labirinto). Suporta
/// aninhamento: campus → corredor do bloco → sala. Cada EnterRoom empilha o estado
/// atual (limites da câmera + posição de retorno); ExitRoom desempilha e volta um
/// nível. Assim sair da sala volta ao corredor, e sair do corredor volta ao campus.
/// </summary>
public class InteriorController : MonoBehaviour
{
    public static InteriorController Instance { get; private set; }
    public static bool InRoom => Instance != null && Instance.stack.Count > 0;

    private GameObject player;
    private CameraFollow2D cam;

    private struct LocState
    {
        public bool useBounds;
        public Vector2 boundsMin, boundsMax;
        public Vector3 returnPos;
    }
    private readonly Stack<LocState> stack = new Stack<LocState>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        stack.Clear();
    }

    private void OnDestroy()
    {
        if (Instance == this) { Instance = null; stack.Clear(); }
    }

    public void EnterRoom(Vector3 spawn, Vector3 returnTo, Vector2 boundsMin, Vector2 boundsMax)
    {
        if (MazeController.InMaze) return;
        EnsureRefs();

        // Guarda de onde viemos (câmera + retorno) para restaurar ao sair.
        var prev = new LocState { returnPos = returnTo };
        if (cam != null)
        {
            prev.useBounds = cam.useBounds;
            prev.boundsMin = cam.boundsMin;
            prev.boundsMax = cam.boundsMax;
        }
        stack.Push(prev);

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
        if (stack.Count == 0) return;
        var s = stack.Pop();
        if (cam != null)
        {
            cam.boundsMin = s.boundsMin;
            cam.boundsMax = s.boundsMax;
            cam.useBounds = s.useBounds;
        }
        Teleport(s.returnPos);
    }

    private void Teleport(Vector3 pos)
    {
        if (player == null) EnsureRefs();
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
