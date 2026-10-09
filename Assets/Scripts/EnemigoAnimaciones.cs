using System.Collections;
using UnityEngine;

// Anima a un enemigo sin necesidad de Animator: solo arrastras los cuadros de cada animación.
// - Reposo y Movimiento cambian SOLOS según si el enemigo se está moviendo.
// - Aviso, Disparo, Golpe y Muerte se activan desde los otros scripts del enemigo
//   llamando a: IniciarAviso(), Disparar(), RecibirGolpe() y Morir().
[RequireComponent(typeof(SpriteRenderer))]
public class EnemigoAnimaciones : MonoBehaviour
{
    [Header("Cuadros de cada animación (arrástralos en orden)")]
    public Sprite[] reposo;
    public Sprite[] movimiento;
    public Sprite[] aviso;
    public Sprite[] disparo;
    public Sprite[] golpeado;
    public Sprite[] muerte;

    [Header("Velocidades (cuadros por segundo)")]
    public float velocidadReposo = 6f;
    public float velocidadMovimiento = 8f;
    public float velocidadDisparo = 12f;
    public float velocidadGolpe = 14f;
    public float velocidadMuerte = 10f;

    [Header("Otros ajustes")]
    [Tooltip("Desde qué velocidad se considera que se está moviendo")]
    public float umbralMovimiento = 0.15f;
    [Tooltip("El dibujo mira a la derecha; si está marcado, se voltea para mirar siempre al jugador")]
    public bool mirarAlJugador = true;

    private SpriteRenderer sr;
    private Transform jugador;
    private Vector3 posicionAnterior;
    private float tiempoCiclo;
    private Coroutine animacionUnaVez;
    private bool muriendo = false;

    // Duración de la animación de muerte (útil para destruir al enemigo después)
    public float DuracionMuerte
    {
        get { return (muerte != null && muerte.Length > 0) ? muerte.Length / Mathf.Max(1f, velocidadMuerte) : 0f; }
    }

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        posicionAnterior = transform.position;
        GameObject j = GameObject.FindGameObjectWithTag("Player");
        if (j != null) jugador = j.transform;
    }

    void Update()
    {
        // Mirar hacia el jugador
        if (mirarAlJugador && jugador != null && !muriendo)
            sr.flipX = jugador.position.x < transform.position.x;

        float rapidez = (transform.position - posicionAnterior).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        posicionAnterior = transform.position;

        // Si hay una animación "de una vez" sonando (aviso, disparo, golpe, muerte), no la interrumpimos
        if (animacionUnaVez != null || muriendo) return;

        bool seMueve = rapidez > umbralMovimiento && movimiento != null && movimiento.Length > 0;
        Sprite[] ciclo = seMueve ? movimiento : reposo;
        float vel = seMueve ? velocidadMovimiento : velocidadReposo;
        if (ciclo == null || ciclo.Length == 0) return;

        tiempoCiclo += Time.deltaTime;
        sr.sprite = ciclo[(int)(tiempoCiclo * vel) % ciclo.Length];
    }

    // ---------- Para llamar desde los otros scripts ----------

    // El aviso dura exactamente "duracion" segundos y se queda en el último cuadro (brillando)
    public void IniciarAviso(float duracion)
    {
        if (muriendo || aviso == null || aviso.Length == 0) return;
        float vel = aviso.Length / Mathf.Max(0.05f, duracion);
        Reproducir(aviso, vel, true);
    }

    public void Disparar()
    {
        if (muriendo) return;
        Reproducir(disparo, velocidadDisparo, false);
    }

    public void RecibirGolpe()
    {
        if (muriendo) return;
        Reproducir(golpeado, velocidadGolpe, false);
    }

    // Reproduce la muerte. Si "destruirAlTerminar" está marcado, borra al enemigo al final.
    public void Morir(bool destruirAlTerminar)
    {
        if (muriendo) return;
        muriendo = true;
        if (animacionUnaVez != null) StopCoroutine(animacionUnaVez);

        // Ya no choca ni hace daño mientras se desarma
        foreach (Collider2D c in GetComponents<Collider2D>()) c.enabled = false;

        StartCoroutine(RutinaMuerte(destruirAlTerminar));
    }

    // ---------- Funcionamiento interno ----------

    void Reproducir(Sprite[] cuadros, float vel, bool quedarseEnUltimo)
    {
        if (cuadros == null || cuadros.Length == 0) return;
        if (animacionUnaVez != null) StopCoroutine(animacionUnaVez);
        animacionUnaVez = StartCoroutine(RutinaUnaVez(cuadros, vel, quedarseEnUltimo));
    }

    IEnumerator RutinaUnaVez(Sprite[] cuadros, float vel, bool quedarseEnUltimo)
    {
        float espera = 1f / Mathf.Max(1f, vel);
        for (int i = 0; i < cuadros.Length; i++)
        {
            sr.sprite = cuadros[i];
            yield return new WaitForSeconds(espera);
        }

        if (quedarseEnUltimo)
        {
            // Se queda brillando hasta que otro script llame a Disparar()
            sr.sprite = cuadros[cuadros.Length - 1];
            yield return new WaitForSeconds(3f); // por seguridad, no se queda así para siempre
        }

        animacionUnaVez = null;
    }

    IEnumerator RutinaMuerte(bool destruir)
    {
        if (muerte != null && muerte.Length > 0)
        {
            float espera = 1f / Mathf.Max(1f, velocidadMuerte);
            for (int i = 0; i < muerte.Length; i++)
            {
                sr.sprite = muerte[i];
                yield return new WaitForSeconds(espera);
            }
        }
        sr.enabled = false;
        if (destruir) Destroy(gameObject);
    }
}