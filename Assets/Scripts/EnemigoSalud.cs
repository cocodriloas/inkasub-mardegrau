using UnityEngine;

public class EnemigoSalud : MonoBehaviour
{
    [Header("Vida del enemigo")]
    public int vidaMaxima = 3;
    private int vidaActual;

    [Header("Drop de curación al morir")]
    public GameObject prefabCuracion;
    public float probabilidadDrop = 0.25f;

    [Header("Efectos")]
    [Tooltip("Efecto de partículas al morir (opcional; puedes dejarlo vacío si ya tiene animación de muerte)")]
    public GameObject prefabEfectoMuerte;

    private bool muerto = false;
    private EnemigoAnimaciones anim;

    void Start()
    {
        vidaActual = vidaMaxima;
        anim = GetComponent<EnemigoAnimaciones>();
    }

    // CAMBIADO: ahora es "virtual" para que el jefe (JefeIceberg y JefeOjo) pueda recibir el daño a su manera.
    // Para los demás enemigos funciona exactamente igual que antes.
    public virtual void RecibirDaño(int cantidad)
    {
        if (muerto) return;

        vidaActual -= cantidad;
        Debug.Log(gameObject.name + " recibió daño. Vida restante: " + vidaActual);

        if (vidaActual > 0)
        {
            // Animación de golpe
            if (anim != null) anim.RecibirGolpe();
            AudioManager.Sonar(Sonido.EnemigoGolpeado);
            return;
        }

        Morir();
    }

    void Morir()
    {
        muerto = true;
        AudioManager.Sonar(Sonido.EnemigoMuere);

        // Efecto visual/sonoro de muerte
        if (prefabEfectoMuerte != null)
        {
            Instantiate(prefabEfectoMuerte, transform.position, Quaternion.identity);
        }

        // Ítem de curación según probabilidad
        if (prefabCuracion != null && Random.value <= probabilidadDrop)
        {
            Instantiate(prefabCuracion, transform.position, Quaternion.identity);
        }

        if (anim != null)
        {
            // Apagar todo lo demás (moverse, disparar, atacar) mientras se desarma
            foreach (MonoBehaviour script in GetComponents<MonoBehaviour>())
            {
                if (script != this && script != anim) script.enabled = false;
            }

            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.simulated = false;
            }

            // La animación de muerte se reproduce y luego borra al enemigo
            anim.Morir(true);
        }
        else
        {
            // Enemigos sin animaciones: desaparecen al instante como antes
            Destroy(gameObject);
        }
    }
}