using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Pon este script en un objeto vacío con un Box Collider 2D marcado como "Is Trigger".
// Cuando el submarino lo cruza (y ya habló con el Mero Gigante):
//   1) se cierra el muro de escape (Barrera),
//   2) aparece el aviso "¡El Iceberg de Basura!",
//   3) empieza la música del jefe (AudioManager),
//   4) se apaga CamaraSeguimiento y la cámara se aleja hasta el Punto Camara,
//   5) al terminar, avisa por el evento "Al Terminar Transicion" (ahí conectaremos al jefe).
public class SalaJefeDisparador : MonoBehaviour
{
    [Header("Cámara")]
    public Camera camaraPrincipal;                  // Si lo dejas vacío, usa la Main Camera
    public CamaraSeguimiento camaraSeguimiento;     // Si lo dejas vacío, lo busca en la cámara
    public Transform puntoCamara;                   // Objeto vacío en el centro de la sala del jefe
    public float tamanoCamaraJefe = 13f;            // Más grande = el submarino se ve más pequeño
    public float duracionTransicion = 2.5f;         // Segundos que tarda la cámara en alejarse

    [Header("Muro de escape (debe tener el script Barrera, con Empieza Cerrada desmarcado)")]
    public Barrera muroEscape;

    [Header("Requisito (opcional): haber hablado con el Mero Gigante")]
    public NPCInteraccion meroGigante;              // Si lo dejas vacío, no pide hablar con nadie
    public string mensajeSiFaltaHablar = "Primero habla con el Mero Gigante";

    [Header("Aviso al entrar")]
    public string textoAviso = "¡El Iceberg de Basura!";
    public float duracionAviso = 3f;

    [Header("Cuando la cámara termine de alejarse")]
    public UnityEvent alTerminarTransicion;         // Aquí conectaremos la aparición del jefe

    [Header("Ayuda en la ventana Scene")]
    public bool mostrarGuiaEnScene = true;          // Dibuja el rectángulo amarillo y la cruz del centro

    private bool yaActivado = false;
    private float ultimoAvisoFalta = -10f;

    void Awake()
    {
        if (camaraPrincipal == null) camaraPrincipal = Camera.main;

        if (camaraSeguimiento == null && camaraPrincipal != null)
            camaraSeguimiento = camaraPrincipal.GetComponent<CamaraSeguimiento>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Intentar(other);
    }

    // Por si el submarino se queda dentro de la zona mientras termina de hablar con el Mero Gigante
    void OnTriggerStay2D(Collider2D other)
    {
        Intentar(other);
    }

    void Intentar(Collider2D other)
    {
        if (yaActivado) return;
        if (!EsElJugador(other)) return;

        // Si hace falta hablar con el Mero Gigante y todavía no lo hizo
        if (meroGigante != null && !meroGigante.yaHablo)
        {
            if (Time.time - ultimoAvisoFalta > 2.5f)
            {
                ultimoAvisoFalta = Time.time;
                AvisoPantalla.Mostrar(mensajeSiFaltaHablar);
            }
            return;
        }

        yaActivado = true;
        StartCoroutine(IniciarSalaJefe());
    }

    // Reconoce al submarino igual que NPCInteraccion
    bool EsElJugador(Collider2D other)
    {
        if (other.CompareTag("Player")) return true;
        if (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Player")) return true;
        if (other.GetComponentInParent<SubmarinoMovimiento>() != null) return true;
        return false;
    }

    IEnumerator IniciarSalaJefe()
    {
        // 1) Cerrar el muro de escape
        if (muroEscape != null)
        {
            if (!muroEscape.EstaAbierta)
                Debug.LogWarning("SalaJefeDisparador: el muro de escape ya estaba cerrado. Desmarca \"Empieza Cerrada\" en su script Barrera.");
            muroEscape.Cerrar();
        }
        else
        {
            Debug.LogWarning("SalaJefeDisparador: falta asignar el Muro Escape.");
        }

        // 2) Aviso en pantalla
        if (!string.IsNullOrEmpty(textoAviso))
            AvisoPantalla.Mostrar(textoAviso, duracionAviso);

        // 3) Música del jefe
        AudioManager.IniciarMusicaJefe();

        // 4) Apagar el seguimiento y alejar la cámara
        if (camaraSeguimiento != null)
            camaraSeguimiento.enabled = false;
        else
            Debug.LogWarning("SalaJefeDisparador: no encontré CamaraSeguimiento. La cámara podría seguir al submarino.");

        if (camaraPrincipal == null)
        {
            Debug.LogWarning("SalaJefeDisparador: no hay cámara asignada.");
            yield break;
        }

        Transform cam = camaraPrincipal.transform;
        Vector3 posicionInicial = cam.position;
        Vector3 posicionFinal = posicionInicial;

        if (puntoCamara != null)
            posicionFinal = new Vector3(puntoCamara.position.x, puntoCamara.position.y, posicionInicial.z);
        else
            Debug.LogWarning("SalaJefeDisparador: falta el Punto Camara. La cámara solo se alejará.");

        float tamanoInicial = camaraPrincipal.orthographicSize;
        float avance = 0f;

        while (avance < 1f)
        {
            avance += Time.deltaTime / Mathf.Max(0.01f, duracionTransicion);
            float suave = Mathf.SmoothStep(0f, 1f, avance);

            cam.position = Vector3.Lerp(posicionInicial, posicionFinal, suave);
            camaraPrincipal.orthographicSize = Mathf.Lerp(tamanoInicial, tamanoCamaraJefe, suave);

            yield return null;
        }

        cam.position = posicionFinal;
        camaraPrincipal.orthographicSize = tamanoCamaraJefe;

        // 5) Avisar que ya terminó (más adelante aquí aparece el jefe)
        if (alTerminarTransicion != null) alTerminarTransicion.Invoke();
    }

    // Guía visual en la ventana Scene (se ve siempre, no hace falta seleccionar el objeto):
    //  - rectángulo amarillo = lo que verá la cámara en la sala del jefe
    //  - círculo y cruz magenta = el centro de la sala (Punto Camara)
    //  - línea celeste = une este disparador con ese centro, para encontrarlo fácil
    void OnDrawGizmos()
    {
        if (!mostrarGuiaEnScene) return;
        if (puntoCamara == null) return;

        float aspecto = 16f / 9f;
        Camera cam = camaraPrincipal != null ? camaraPrincipal : Camera.main;
        if (cam != null) aspecto = cam.aspect;

        float alto = tamanoCamaraJefe * 2f;
        float ancho = alto * aspecto;

        // Se dibuja un poco hacia adelante (z = -1) para que no lo tapen los sprites del nivel
        Vector3 centro = new Vector3(puntoCamara.position.x, puntoCamara.position.y, -1f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(centro, new Vector3(ancho, alto, 0f));

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(centro, 1.5f);
        Gizmos.DrawLine(centro + Vector3.left * 3f, centro + Vector3.right * 3f);
        Gizmos.DrawLine(centro + Vector3.down * 3f, centro + Vector3.up * 3f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(transform.position.x, transform.position.y, -1f), centro);
    }
}