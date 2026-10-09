using System.Collections;
using UnityEngine;

// Un muro que se puede ABRIR y CERRAR con animación.
// Lo usan: GeneradorOleadas, ZonaEncuentro y MuroBloqueoNPC.
// Sirve con la barra roja actual (se desvanece) o con el arte de la red (cuadros).
public class Barrera : MonoBehaviour
{
    [Header("Estado al empezar el juego")]
    public bool empiezaCerrada = true;

    [Header("Animación con cuadros (opcional; si está vacío, se desvanece)")]
    [Tooltip("Cuadros de la barrera cerrada meciéndose (en bucle)")]
    public Sprite[] cuadrosCerrada;
    [Tooltip("Cuadros de la barrera rompiéndose/abriéndose")]
    public Sprite[] cuadrosAbriendose;
    public float velocidadAnimacion = 10f;
    [Tooltip("Si está marcado, al abrirse se queda el último cuadro (por ejemplo, los postes de roca)")]
    public bool quedarUltimoCuadro = true;

    [Header("Aviso al chocar con ella cerrada (opcional)")]
    public string mensajeAlChocar = "";

    private SpriteRenderer sr;
    private Collider2D[] colisionadores;
    private bool abierta;
    private Vector3 escalaOriginal;
    private Color colorOriginal;
    private Coroutine animacion;
    private float tiempo;
    private float ultimoAviso = -10f;

    public bool EstaAbierta { get { return abierta; } }

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        colisionadores = GetComponents<Collider2D>();
        escalaOriginal = transform.localScale;
        if (sr != null) colorOriginal = sr.color;

        abierta = !empiezaCerrada;
        AplicarColisiones();
        if (abierta && sr != null)
        {
            if (quedarUltimoCuadro && cuadrosAbriendose != null && cuadrosAbriendose.Length > 0)
                sr.sprite = cuadrosAbriendose[cuadrosAbriendose.Length - 1];
            else
                sr.enabled = false;
        }
    }

    void Update()
    {
        // Red cerrada meciéndose con la corriente
        if (!abierta && animacion == null && sr != null && cuadrosCerrada != null && cuadrosCerrada.Length > 0)
        {
            tiempo += Time.deltaTime;
            sr.sprite = cuadrosCerrada[(int)(tiempo * velocidadAnimacion * 0.4f) % cuadrosCerrada.Length];
        }
    }

    // ---------- Para llamar desde otros scripts ----------

    public void Abrir()
    {
        if (abierta) return;
        abierta = true;
        AplicarColisiones();
        AudioManager.Sonar(Sonido.Desbloqueo);
        Reiniciar(StartCoroutine(RutinaAbrir()));
    }

    public void Cerrar()
    {
        if (!abierta) return;
        abierta = false;
        AplicarColisiones();
        AudioManager.Sonar(Sonido.Garra);
        Reiniciar(StartCoroutine(RutinaCerrar()));
    }

    // ---------- Interno ----------

    void Reiniciar(Coroutine nueva)
    {
        animacion = nueva;
    }

    void AplicarColisiones()
    {
        foreach (Collider2D c in colisionadores) c.enabled = !abierta;
    }

    IEnumerator RutinaAbrir()
    {
        if (sr != null && cuadrosAbriendose != null && cuadrosAbriendose.Length > 0)
        {
            float espera = 1f / Mathf.Max(1f, velocidadAnimacion);
            foreach (Sprite s in cuadrosAbriendose)
            {
                sr.sprite = s;
                yield return new WaitForSeconds(espera);
            }
            if (!quedarUltimoCuadro) sr.enabled = false;
        }
        else if (sr != null)
        {
            // Sin cuadros: se encoge y se desvanece
            float t = 0f;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                float p = t / 0.5f;
                transform.localScale = new Vector3(escalaOriginal.x, escalaOriginal.y * (1f - p), escalaOriginal.z);
                sr.color = new Color(colorOriginal.r, colorOriginal.g, colorOriginal.b, colorOriginal.a * (1f - p));
                yield return null;
            }
            sr.enabled = false;
            transform.localScale = escalaOriginal;
            sr.color = colorOriginal;
        }
        animacion = null;
    }

    IEnumerator RutinaCerrar()
    {
        if (sr == null) { animacion = null; yield break; }
        sr.enabled = true;

        if (cuadrosAbriendose != null && cuadrosAbriendose.Length > 0)
        {
            // La red se "rearma": los cuadros de abrirse, al revés
            float espera = 1f / Mathf.Max(1f, velocidadAnimacion);
            for (int i = cuadrosAbriendose.Length - 1; i >= 0; i--)
            {
                sr.sprite = cuadrosAbriendose[i];
                yield return new WaitForSeconds(espera);
            }
        }
        else
        {
            // Sin cuadros: crece y aparece
            float t = 0f;
            while (t < 0.4f)
            {
                t += Time.deltaTime;
                float p = t / 0.4f;
                transform.localScale = new Vector3(escalaOriginal.x, escalaOriginal.y * p, escalaOriginal.z);
                sr.color = new Color(colorOriginal.r, colorOriginal.g, colorOriginal.b, colorOriginal.a * p);
                yield return null;
            }
            transform.localScale = escalaOriginal;
            sr.color = colorOriginal;
        }
        animacion = null;
    }

    void OnCollisionEnter2D(Collision2D colision)
    {
        if (abierta || string.IsNullOrEmpty(mensajeAlChocar)) return;
        if (!colision.gameObject.CompareTag("Player")) return;
        if (Time.time - ultimoAviso < 2.5f) return;
        ultimoAviso = Time.time;
        AvisoPantalla.Mostrar(mensajeAlChocar);
    }
}
