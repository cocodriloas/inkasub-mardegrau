using UnityEngine;

public class ProyectilEnemigoMovimiento : MonoBehaviour
{
    [Header("Configuración")]
    public float velocidad = 6f;
    public float tiempoDeVida = 4f;
    public int daño = 1;

    [Header("Apariencia")]
    [Tooltip("Gira el dibujo para que la estela quede siempre detrás (el dibujo original viaja hacia la derecha)")]
    public bool girarHaciaDireccion = true;

    private Vector2 direccion;

    void Start()
    {
        Destroy(gameObject, tiempoDeVida);
    }

    // El enemigo llama a este método justo al crear el proyectil,
    // para decirle hacia dónde ir
    public void SetDireccion(Vector2 nuevaDireccion)
    {
        direccion = nuevaDireccion.normalized;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = direccion * velocidad;

        if (girarHaciaDireccion)
        {
            float angulo = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angulo);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            SubmarinoSalud salud = other.GetComponent<SubmarinoSalud>();
            if (salud != null)
            {
                salud.RecibirDaño(daño);
            }
            Destroy(gameObject);
        }
    }
}