using UnityEngine;

// Hace que el fondo siga a la cámara pero un poco más lento que el jugador.
// Así da sensación de profundidad y nunca se ve el borde de la imagen.
public class FondoParallax : MonoBehaviour
{
    [Tooltip("Si lo dejas vacío, usa la Main Camera automáticamente")]
    public Transform camara;

    [Header("Qué tanto sigue a la cámara (1 = pegado a la cámara, 0 = quieto en el mundo)")]
    [Range(0f, 1f)] public float factorX = 0.9f;
    [Range(0f, 1f)] public float factorY = 0.9f;

    private Vector3 posicionInicial;
    private Vector3 camaraInicial;

    void Start()
    {
        if (camara == null && Camera.main != null) camara = Camera.main.transform;
        posicionInicial = transform.position;
        if (camara != null) camaraInicial = camara.position;
    }

    void LateUpdate()
    {
        if (camara == null) return;

        Vector3 desplazamiento = camara.position - camaraInicial;
        transform.position = new Vector3(
            posicionInicial.x + desplazamiento.x * factorX,
            posicionInicial.y + desplazamiento.y * factorY,
            posicionInicial.z);
    }
}