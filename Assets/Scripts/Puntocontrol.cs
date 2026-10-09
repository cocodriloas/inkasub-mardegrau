using UnityEngine;

// Un cangrejo que guarda el progreso cuando el submarino pasa cerca.
// Al activarse: guarda la posición, muestra "¡Punto de control!", suena, cura vidas y el cangrejo levanta las pinzas.
// Si pierdes, el panel de derrota ofrece "Continuar" y reapareces aquí.
// Sin dibujos asignados, usa un círculo naranja que se vuelve turquesa al activarse (para probar).
[RequireComponent(typeof(SpriteRenderer))]
public class PuntoControl : MonoBehaviour
{
    [Header("Orden en el nivel (1 = el primero, 2 = el siguiente, ...)")]
    public int orden = 1;

    [Header("Dibujos del cangrejo (hoja Cangrejo_PuntoControl, 3 x 2 cuadros)")]
    [Tooltip("Cuadros 1, 2 y 3: dormido")]
    public Sprite[] dormido;
    [Tooltip("Cuadros 4 y 5: despierta (se reproduce una vez)")]
    public Sprite[] despertando;
    [Tooltip("Cuadros 5 y 6: activado (en bucle)")]
    public Sprite[] activo;
    public float fpsDormido = 2f;
    public float fpsDespertar = 6f;
    public float fpsActivo = 5f;
    [Tooltip("Ancho del cangrejo en unidades del juego")]
    public float ancho = 3.5f;
    public int ordenCapa = 1;

    [Header("Al activarse")]
    public string mensaje = "¡Punto de control!";
    [Tooltip("Cuántas vidas recupera el submarino (0 = ninguna)")]
    public int curarVidas = 1;
    [Tooltip("Dónde reaparece el submarino, respecto al cangrejo")]
    public Vector2 reaparicion = new Vector2(0f, 1.5f);
    [Tooltip("Qué tan cerca debe pasar el submarino para activarlo, en unidades")]
    public float radioActivacion = 3f;

    [Header("Sonido al activarse")]
    [Tooltip("Arrastra aquí Cangrejo_PuntoControl.wav. Si lo dejas vacío, suena el sonido de estación de tu AudioManager.")]
    public AudioClip sonidoActivar;
    [Range(0f, 2f)] public float volumenSonido = 1f;

    private SpriteRenderer sr;
    private bool activado = false;
    private bool conDibujos = false;
    private float inicioActivacion = -1000f;
    private Vector3 escalaBase = Vector3.one;
    private AudioSource fuente;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        sr.sortingOrder = ordenCapa;

        conDibujos = dormido != null && dormido.Length > 0 && dormido[0] != null;
        if (conDibujos)
        {
            sr.sprite = dormido[0];
            float anchoDibujo = Mathf.Max(0.01f, dormido[0].bounds.size.x);
            transform.localScale = Vector3.one * (ancho / anchoDibujo);
        }
        else
        {
            sr.sprite = CrearSpriteCirculo();
            transform.localScale = Vector3.one * 1.2f;
        }
        escalaBase = transform.localScale;

        fuente = gameObject.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0f;   // sonido 2D: se oye igual en toda la pantalla

        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col == null) col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = radioActivacion / Mathf.Max(0.01f, transform.localScale.x);
    }

    void Start()
    {
        // Después de recargar la escena, los cangrejos que ya pasaste aparecen activados
        if (ProgresoJuego.hayPuntoControl && orden <= ProgresoJuego.orden)
        {
            activado = true;
            inicioActivacion = Time.time - 1000f;
        }
    }

    void Update()
    {
        float t = Time.time;

        if (!conDibujos)
        {
            AnimarMarcador(t);
            return;
        }

        if (!activado)
        {
            Mostrar(dormido, fpsDormido, t, true);
            return;
        }

        float desdeActivar = t - inicioActivacion;
        float duracion = (despertando != null && despertando.Length > 0) ? despertando.Length / Mathf.Max(0.5f, fpsDespertar) : 0f;
        if (desdeActivar < duracion) Mostrar(despertando, fpsDespertar, desdeActivar, false);
        else Mostrar(activo, fpsActivo, t, true);
    }

    void Mostrar(Sprite[] cuadros, float fps, float tiempo, bool bucle)
    {
        if (cuadros == null || cuadros.Length == 0) return;
        int i = (int)(tiempo * Mathf.Max(0.5f, fps));
        i = bucle ? i % cuadros.Length : Mathf.Min(i, cuadros.Length - 1);
        if (cuadros[i] != null) sr.sprite = cuadros[i];
    }

    // Círculo de prueba: naranja dormido, turquesa y latiendo cuando está activo
    void AnimarMarcador(float t)
    {
        if (!activado)
        {
            sr.color = new Color(0.85f, 0.5f, 0.3f, 1f);
            transform.localScale = escalaBase;
        }
        else
        {
            sr.color = new Color(0.2f, 0.9f, 0.9f, 1f);
            transform.localScale = escalaBase * (1f + 0.12f * Mathf.Sin(t * 6f));
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (activado || !EsElJugador(other)) return;

        activado = true;
        inicioActivacion = Time.time;

        // Si ya pasaste por un cangrejo más adelante, este no cambia tu progreso
        if (orden < ProgresoJuego.orden) return;

        ProgresoJuego.GuardarPunto(orden, (Vector2)transform.position + reaparicion);
        GuardarEstado(other);

        if (curarVidas > 0)
        {
            SubmarinoSalud salud = other.GetComponentInParent<SubmarinoSalud>();
            if (salud != null) salud.Curar(curarVidas);
        }

        if (!string.IsNullOrEmpty(mensaje)) AvisoPantalla.Mostrar(mensaje, 2f);

        if (sonidoActivar != null)
        {
            // Usa el volumen de efectos de tu AudioManager
            float efectos = AudioManager.instancia != null ? AudioManager.instancia.volumenEfectos : 0.8f;
            fuente.PlayOneShot(sonidoActivar, efectos * volumenSonido);
        }
        else
        {
            AudioManager.Sonar(Sonido.Estacion);
        }
    }

    // Guarda con qué NPC hablaste, tus habilidades y qué bacterias quedan en el nivel
    void GuardarEstado(Collider2D other)
    {
        foreach (NPCInteraccion npc in FindObjectsByType<NPCInteraccion>(FindObjectsSortMode.None))
        {
            if (npc.yaHablo) ProgresoJuego.npcsHablados.Add(npc.name);
        }

        SubmarinoRecolector recolector = other.GetComponentInParent<SubmarinoRecolector>();
        SubmarinoMovimiento movimiento = other.GetComponentInParent<SubmarinoMovimiento>();
        SubmarinoDisparo disparo = other.GetComponentInParent<SubmarinoDisparo>();
        if (recolector != null) ProgresoJuego.bacterias = recolector.bacteriasRecolectadas;
        if (movimiento != null) ProgresoJuego.dash = movimiento.dashDesbloqueado;
        if (disparo != null) ProgresoJuego.dobleDisparo = disparo.habilidadDesbloqueada;

        ProgresoJuego.bacteriasRestantes.Clear();
        foreach (GameObject bacteria in GameObject.FindGameObjectsWithTag("Bacteria"))
            ProgresoJuego.bacteriasRestantes.Add(bacteria.name);
        ProgresoJuego.guardoBacterias = true;
    }

    // Reconoce al submarino igual que tus otros scripts
    bool EsElJugador(Collider2D other)
    {
        if (other.CompareTag("Player")) return true;
        if (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Player")) return true;
        if (other.GetComponentInParent<SubmarinoMovimiento>() != null) return true;
        return false;
    }

    Sprite CrearSpriteCirculo()
    {
        int n = 32;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        float centro = (n - 1) / 2f;
        float radio = n / 2f - 1f;
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float d = Mathf.Sqrt((x - centro) * (x - centro) + (y - centro) * (y - centro));
                tex.SetPixel(x, y, d <= radio ? Color.white : new Color(0f, 0f, 0f, 0f));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
    }

    // Dibuja en la ventana Scene el área de activación y dónde reaparecerá el submarino
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radioActivacion);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position + new Vector3(reaparicion.x, reaparicion.y, 0f), 0.5f);
    }
}