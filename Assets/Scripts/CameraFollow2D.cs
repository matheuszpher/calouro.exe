using UnityEngine;

/// <summary>
/// Faz a câmera seguir um alvo (o jogador) suavemente, travando dentro dos
/// limites do mapa para não mostrar áreas fora do campus.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraFollow2D : MonoBehaviour
{
    [Tooltip("Alvo a seguir (normalmente o Player).")]
    public Transform target;

    [Tooltip("Suavidade do movimento (quanto maior, mais lento/suave).")]
    public float smoothTime = 0.15f;

    [Tooltip("Limitar a câmera aos limites do mapa.")]
    public bool useBounds = true;
    public Vector2 boundsMin = new Vector2(-24f, -9f);
    public Vector2 boundsMax = new Vector2(24f, 9f);

    private Camera cam;
    private Vector3 velocity;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = new Vector3(target.position.x, target.position.y, transform.position.z);

        if (useBounds && cam != null && cam.orthographic)
        {
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;

            // Se o mapa for menor que a tela, centraliza naquele eixo.
            float minX = boundsMin.x + halfW;
            float maxX = boundsMax.x - halfW;
            float minY = boundsMin.y + halfH;
            float maxY = boundsMax.y - halfH;

            desired.x = minX <= maxX ? Mathf.Clamp(desired.x, minX, maxX) : (boundsMin.x + boundsMax.x) * 0.5f;
            desired.y = minY <= maxY ? Mathf.Clamp(desired.y, minY, maxY) : (boundsMin.y + boundsMax.y) * 0.5f;
        }

        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }
}
