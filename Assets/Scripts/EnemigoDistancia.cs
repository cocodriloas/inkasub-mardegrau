using UnityEngine;

public class EnemigoDistancia : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject prefabProyectilEnemigo;

    [Header("Comportamiento")]
    public float distanciaPreferida = 5f; // qué tan lejos quiere mantenerse del submarino
    public float velocidad = 1.5f;
    public float tiempoEntreDisparos = 2f;
    public float anguloDiagonal = 25f; // qué tan abiertos van los disparos laterales

    [Header("Aviso antes de disparar (brillo amarillo)")]
    [Tooltip("Cuántos segundos brilla antes de disparar. Da tiempo al jugador para esquivar")]
    public float tiempoAviso = 0.7f;
    [Tooltip("Mientras avisa, se mueve más lento (0 = se queda quieto, 1 = velocidad normal)")]
    [Range(0f, 1f)] public float lentitudDuranteAviso = 0.3f;

    private Transform objetivo;
    private float tiempoRestanteParaDisparar;
    private bool avisando = false;
    private EnemigoAnimaciones anim;
    private Rigidbody2D rb;
    private Vector2 velocidadDeseada;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        GameObject jugador = GameObject.FindGameObjectWithTag("Player");
        if (jugador != null)
        {
            objetivo = jugador.transform;
        }
        anim = GetComponent<EnemigoAnimaciones>();
        tiempoRestanteParaDisparar = tiempoEntreDisparos;
    }

    void Update()
    {
        if (objetivo == null) return;

        float distanciaActual = Vector2.Distance(transform.position, objetivo.position);
        float vel = avisando ? velocidad * lentitudDuranteAviso : velocidad;

        // Si está muy lejos, se acerca. Si está muy cerca, se aleja. Si no, se queda.
        Vector2 haciaJugador = ((Vector2)objetivo.position - (Vector2)transform.position).normalized;
        if (distanciaActual > distanciaPreferida + 0.5f) velocidadDeseada = haciaJugador * vel;
        else if (distanciaActual < distanciaPreferida - 0.5f) velocidadDeseada = -haciaJugador * vel;
        else velocidadDeseada = Vector2.zero;

        // Sin Rigidbody se mueve como antes (pero así atraviesa paredes)
        if (rb == null) transform.position += (Vector3)velocidadDeseada * Time.deltaTime;

        // Manejo del temporizador de disparo
        tiempoRestanteParaDisparar -= Time.deltaTime;

        // Un momento antes de disparar: empieza a brillar (aviso)
        if (!avisando && tiempoRestanteParaDisparar <= tiempoAviso)
        {
            avisando = true;
            if (anim != null) anim.IniciarAviso(Mathf.Max(0.1f, tiempoRestanteParaDisparar));
            AudioManager.Sonar(Sonido.AvisoEnemigo);
        }

        if (tiempoRestanteParaDisparar <= 0)
        {
            if (anim != null) anim.Disparar();
            Disparar();
            AudioManager.Sonar(Sonido.DisparoEnemigo);
            avisando = false;
            tiempoRestanteParaDisparar = Mathf.Max(tiempoEntreDisparos, tiempoAviso + 0.2f);
        }
    }

    // Movimiento con física: así choca con las paredes y no las atraviesa
    void FixedUpdate()
    {
        if (rb != null) rb.linearVelocity = velocidadDeseada;
    }

    void OnDisable()
    {
        velocidadDeseada = Vector2.zero;
        if (rb != null && rb.simulated) rb.linearVelocity = Vector2.zero;
    }

    // --- SISTEMA DE DISPARO TRIPLE ---

    void Disparar()
    {
        if (prefabProyectilEnemigo == null || objetivo == null) return;

        Vector2 direccionCentral = ((Vector2)objetivo.position - (Vector2)transform.position).normalized;

        // Disparo central: apunta directo al jugador
        DispararEnDireccion(direccionCentral);

        // Dos disparos en diagonal fija respecto al central
        Vector2 direccionIzquierda = RotarVector(direccionCentral, anguloDiagonal);
        Vector2 direccionDerecha = RotarVector(direccionCentral, -anguloDiagonal);

        DispararEnDireccion(direccionIzquierda);
        DispararEnDireccion(direccionDerecha);
    }

    void DispararEnDireccion(Vector2 direccion)
    {
        GameObject proyectil = Instantiate(prefabProyectilEnemigo, transform.position, Quaternion.identity);
        ProyectilEnemigoMovimiento mov = proyectil.GetComponent<ProyectilEnemigoMovimiento>();
        if (mov != null) mov.SetDireccion(direccion);
    }

    Vector2 RotarVector(Vector2 vector, float grados)
    {
        float rad = grados * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos);
    }
}
