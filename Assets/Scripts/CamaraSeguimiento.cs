using UnityEngine;

public class CamaraSeguimiento : MonoBehaviour
{
    [Header("A quién seguir")]
    public Transform objetivo; // el submarino

    [Header("Suavizado")]
    public float velocidadSeguimiento = 5f;

    [Header("Límites del mapa (opcional, ajustar según tu nivel)")]
    public bool usarLimites = false;
    public float limiteIzquierdo = -10f;
    public float limiteDerecho = 10f;
    public float limiteAbajo = -5f;
    public float limiteArriba = 5f;

    void LateUpdate()
    {
        if (objetivo == null) return;

        // Queremos que la cámara tenga la misma X e Y que el submarino,
        // pero mantenga su propia Z (para no meterse "dentro" de la escena 2D)
        Vector3 posicionDeseada = new Vector3(objetivo.position.x, objetivo.position.y, transform.position.z);

        // Lerp suaviza el movimiento de la cámara en vez de que "salte" de golpe
        Vector3 posicionSuavizada = Vector3.Lerp(transform.position, posicionDeseada, velocidadSeguimiento * Time.deltaTime);

        if (usarLimites)
        {
            posicionSuavizada.x = Mathf.Clamp(posicionSuavizada.x, limiteIzquierdo, limiteDerecho);
            posicionSuavizada.y = Mathf.Clamp(posicionSuavizada.y, limiteAbajo, limiteArriba);
        }

        transform.position = posicionSuavizada;
    }
}