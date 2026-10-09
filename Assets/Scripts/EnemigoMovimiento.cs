using UnityEngine;

public class EnemigoMovimiento : MonoBehaviour
{
    [Header("Velocidad de persecución")]
    public float velocidad = 2f;

    private Transform objetivo;
    private Rigidbody2D rb;

    void Start()
    {
        GameObject jugador = GameObject.FindGameObjectWithTag("Player");
        if (jugador != null)
        {
            objetivo = jugador.transform;
        }
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if (objetivo == null || rb == null) return;

        // Se mueve con velocidad física: así CHOCA con las paredes y no las atraviesa
        // (el Rigidbody 2D del enemigo debe ser "Dynamic" con Gravity Scale 0)
        Vector2 direccion = ((Vector2)objetivo.position - rb.position);
        if (direccion.magnitude < 0.05f)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        rb.linearVelocity = direccion.normalized * velocidad;
    }

    // Cuando otro script lo apaga (por ejemplo, mientras ataca con la garra), se queda quieto
    void OnDisable()
    {
        if (rb != null && rb.simulated) rb.linearVelocity = Vector2.zero;
    }
}
