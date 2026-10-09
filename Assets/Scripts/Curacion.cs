using System.Collections;
using UnityEngine;

// Burbuja de oxígeno que sueltan los enemigos al morir.
// Flota, pulsa, se acerca al submarino como imán y parpadea antes de desaparecer.
[RequireComponent(typeof(SpriteRenderer))]
public class Curacion : MonoBehaviour
{
    [Header("Curación")]
    public int cantidadCuracion = 1;

    [Header("Animación (arrastra los 4 cuadros de la burbuja)")]
    public Sprite[] cuadros;
    public float velocidadAnimacion = 6f;

    [Header("Flotar")]
    public float alturaFlotar = 0.15f;
    public float velocidadFlotar = 2f;

    [Header("Imán hacia el submarino")]
    [Tooltip("A qué distancia empieza a acercarse sola al submarino")]
    public float radioIman = 3f;
    public float velocidadIman = 8f;

    [Header("Tiempo de vida (0 = nunca desaparece)")]
    public float tiempoVida = 12f;
    [Tooltip("Los últimos segundos parpadea para avisar que va a desaparecer")]
    public float tiempoParpadeo = 3f;

    private SpriteRenderer sr;
    private Transform jugador;
    private Vector3 posicionBase;
    private Vector3 escalaOriginal;
    private float tiempo;
    private float fase;
    private bool recogida = false;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        GameObject j = GameObject.FindGameObjectWithTag("Player");
        if (j != null) jugador = j.transform;

        posicionBase = transform.position;
        escalaOriginal = transform.localScale;
        transform.localScale = Vector3.zero;   // aparece "creciendo"
        fase = Random.Range(0f, 10f);
    }

    void Update()
    {
        if (recogida) return;
        tiempo += Time.deltaTime;

        // Aparecer creciendo con un pequeño rebote
        float crecer = Mathf.Clamp01(tiempo / 0.25f);
        float rebote = 1f + Mathf.Sin(crecer * Mathf.PI) * 0.2f;
        transform.localScale = escalaOriginal * crecer * rebote;

        // Animación de cuadros
        if (cuadros != null && cuadros.Length > 0)
            sr.sprite = cuadros[(int)((tiempo + fase) * velocidadAnimacion) % cuadros.Length];

        // Imán: si el submarino está cerca, la burbuja va hacia él
        float flotar = 1f;
        if (jugador != null)
        {
            float distancia = Vector2.Distance(posicionBase, jugador.position);
            if (distancia < radioIman)
            {
                float fuerza = 1f - distancia / radioIman + 0.3f;
                posicionBase = Vector3.MoveTowards(posicionBase, jugador.position, velocidadIman * fuerza * Time.deltaTime);
                flotar = 0.3f;
            }
        }

        // Flotar suavemente arriba y abajo
        float y = Mathf.Sin((tiempo + fase) * velocidadFlotar) * alturaFlotar * flotar;
        transform.position = posicionBase + new Vector3(0f, y, 0f);

        // Tiempo de vida y parpadeo final
        if (tiempoVida > 0f)
        {
            float restante = tiempoVida - tiempo;
            if (restante <= 0f)
            {
                Destroy(gameObject);
                return;
            }
            sr.enabled = restante > tiempoParpadeo || ((int)(tiempo * 10f) % 2 == 0);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (recogida) return;
        if (!other.CompareTag("Player")) return;

        SubmarinoSalud salud = other.GetComponent<SubmarinoSalud>();
        if (salud == null) salud = other.GetComponentInParent<SubmarinoSalud>();
        if (salud != null) salud.Curar(cantidadCuracion);

        StartCoroutine(Recoger());
    }

    IEnumerator Recoger()
    {
        recogida = true;
        sr.enabled = true;
        foreach (Collider2D c in GetComponents<Collider2D>()) c.enabled = false;

        // Se infla y se desvanece rápido
        Vector3 inicio = transform.localScale;
        Color color = sr.color;
        float t = 0f;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            float p = t / 0.2f;
            transform.localScale = inicio * (1f + p * 0.8f);
            sr.color = new Color(color.r, color.g, color.b, 1f - p);
            yield return null;
        }
        Destroy(gameObject);
    }
}