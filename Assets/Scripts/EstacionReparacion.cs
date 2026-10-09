using System.Collections;
using UnityEngine;

// Estación de reparación de un solo uso.
// Cuadros: 0 y 1 = disponible (parpadea), 2 = usándose (destello), 3 = usada (apagada).
public class EstacionReparacion : MonoBehaviour
{
    [Header("Animación (arrastra los 4 cuadros de la estación)")]
    public Sprite[] cuadros;
    [Tooltip("Qué tan rápido parpadea su luz mientras está disponible")]
    public float velocidadParpadeo = 2f;
    [Tooltip("Cuánto dura el destello al usarla")]
    public float duracionUsando = 0.6f;

    private bool jugadorCerca = false;
    private bool usada = false;
    private SubmarinoSalud saludJugador;
    private SpriteRenderer sr;
    private float tiempo;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // Luz parpadeando mientras está disponible
        if (!usada && sr != null && cuadros != null && cuadros.Length >= 2)
        {
            tiempo += Time.deltaTime;
            sr.sprite = cuadros[(int)(tiempo * velocidadParpadeo) % 2];
        }

        if (jugadorCerca && !usada && Input.GetKeyDown(KeyCode.E))
        {
            if (saludJugador != null)
            {
                saludJugador.CurarCompleto();
            }
            usada = true;
            AudioManager.Sonar(Sonido.Estacion);
            if (DialogoUI.instancia != null) DialogoUI.instancia.MostrarPrompt(false);
            StartCoroutine(Usar());
        }
    }

    IEnumerator Usar()
    {
        // Destello verde y chispas
        if (sr != null && cuadros != null && cuadros.Length >= 3) sr.sprite = cuadros[2];
        yield return new WaitForSeconds(duracionUsando);

        // Queda apagada para siempre (ya no se desactiva el objeto)
        if (sr != null && cuadros != null && cuadros.Length >= 4) sr.sprite = cuadros[3];
        else if (cuadros == null || cuadros.Length < 4) gameObject.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = true;
            saludJugador = other.GetComponent<SubmarinoSalud>();
            if (saludJugador == null) saludJugador = other.GetComponentInParent<SubmarinoSalud>();
            if (!usada && DialogoUI.instancia != null)
            {
                DialogoUI.instancia.MostrarPrompt(true);
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = false;
            if (!usada && DialogoUI.instancia != null)
            {
                DialogoUI.instancia.MostrarPrompt(false);
            }
        }
    }
}
