using UnityEngine;

public class ProyectilMovimiento : MonoBehaviour
{
    [Header("Configuración del proyectil")]
    public float velocidad = 10f;
    public float tiempoDeVida = 3f; // segundos antes de autodestruirse

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // El proyectil siempre se mueve hacia la derecha (transform.right)
        // en su propia dirección local, respecto a cómo esté rotado
        rb.linearVelocity = transform.right * velocidad;

        // Se autodestruye después de "tiempoDeVida" segundos,
        // así no se acumulan proyectiles invisibles llenando la memoria
        Destroy(gameObject, tiempoDeVida);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemigo"))
        {
            EnemigoSalud salud = other.GetComponent<EnemigoSalud>();
            if (salud != null)
            {
                salud.RecibirDaño(1);
            }
            Destroy(gameObject); // el proyectil desaparece al impactar
        }
    }
}