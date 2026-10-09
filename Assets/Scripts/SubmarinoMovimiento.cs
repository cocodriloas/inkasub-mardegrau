using System.Collections;
using UnityEngine;

public class SubmarinoMovimiento : MonoBehaviour
{
    [Header("Velocidad de movimiento")]
    public float velocidad = 5f;

    [Header("Dash (se desbloquea con 2 bacterias)")]
    public bool dashDesbloqueado = false;
    public float velocidadDash = 15f;
    public float duracionDash = 0.2f;
    public float cooldownDash = 1f;

    [Header("Efecto visual del dash: 'fantasmas' del submarino")]
    [Tooltip("Cada cuánto deja una copia transparente mientras hace dash")]
    public float intervaloFantasma = 0.03f;
    [Tooltip("Cuánto tarda cada copia en desaparecer")]
    public float duracionFantasma = 0.25f;
    public Color colorFantasma = new Color(0.4f, 0.9f, 1f, 0.6f);

    [Header("(Opcional) Estela antigua: déjalo vacío para no usarla")]
    public TrailRenderer estelaDash;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Vector2 direccionMovimiento;
    private Vector2 ultimaDireccion = Vector2.right;
    private bool estaHaciendoDash = false;
    private float tiempoRestanteCooldown = 0f;
    private SubmarinoAnimaciones anim;

    // Para un posible indicador del dash en el HUD
    public bool DashListo { get { return dashDesbloqueado && !estaHaciendoDash && tiempoRestanteCooldown <= 0f; } }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<SubmarinoAnimaciones>();
        if (anim == null) anim = gameObject.AddComponent<SubmarinoAnimaciones>();
        if (estelaDash != null) estelaDash.emitting = false;
    }

    void Update()
    {
        if (Time.timeScale == 0f) return; // juego en pausa

        float movimientoX = Input.GetAxisRaw("Horizontal");
        float movimientoY = Input.GetAxisRaw("Vertical");
        direccionMovimiento = new Vector2(movimientoX, movimientoY).normalized;

        if (direccionMovimiento.sqrMagnitude > 0.01f)
            ultimaDireccion = direccionMovimiento;

        if (tiempoRestanteCooldown > 0)
            tiempoRestanteCooldown -= Time.deltaTime;

        // Dash con clic derecho del mouse
        if (dashDesbloqueado && Input.GetMouseButtonDown(1) && !estaHaciendoDash && tiempoRestanteCooldown <= 0)
        {
            StartCoroutine(HacerDash());
        }
    }

    void FixedUpdate()
    {
        if (!estaHaciendoDash)
        {
            rb.linearVelocity = direccionMovimiento * velocidad;
        }
    }

    private IEnumerator HacerDash()
    {
        estaHaciendoDash = true;
        rb.linearVelocity = ultimaDireccion * velocidadDash;

        if (estelaDash != null) estelaDash.emitting = true;

        // Animación y sonido de dash
        if (anim != null) anim.ReproducirDash(duracionDash);
        AudioManager.Sonar(Sonido.Dash);

        // Deja "fantasmas" del submarino mientras avanza
        float t = 0f;
        float siguienteFantasma = 0f;
        while (t < duracionDash)
        {
            if (t >= siguienteFantasma)
            {
                CrearFantasma();
                siguienteFantasma += intervaloFantasma;
            }
            t += Time.deltaTime;
            yield return null;
        }

        estaHaciendoDash = false;
        tiempoRestanteCooldown = cooldownDash;

        if (estelaDash != null) estelaDash.emitting = false;
    }

    void CrearFantasma()
    {
        if (sr == null || sr.sprite == null) return;

        GameObject fantasma = new GameObject("FantasmaDash");
        fantasma.transform.SetPositionAndRotation(transform.position, transform.rotation);
        fantasma.transform.localScale = transform.lossyScale;

        SpriteRenderer copia = fantasma.AddComponent<SpriteRenderer>();
        copia.sprite = sr.sprite;
        copia.flipX = sr.flipX;
        copia.flipY = sr.flipY;
        copia.color = colorFantasma;
        copia.sortingLayerID = sr.sortingLayerID;
        copia.sortingOrder = sr.sortingOrder - 1;
        copia.sharedMaterial = sr.sharedMaterial;

        StartCoroutine(DesvanecerFantasma(copia));
    }

    IEnumerator DesvanecerFantasma(SpriteRenderer copia)
    {
        float t = 0f;
        Color inicio = copia.color;
        while (t < duracionFantasma && copia != null)
        {
            t += Time.deltaTime;
            copia.color = new Color(inicio.r, inicio.g, inicio.b, inicio.a * (1f - t / duracionFantasma));
            yield return null;
        }
        if (copia != null) Destroy(copia.gameObject);
    }
}
