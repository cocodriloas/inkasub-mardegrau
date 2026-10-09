using UnityEngine;

// El submarino ya NO gira completo hacia el mouse (eso lo ponía vertical o de cabeza).
// Ahora: mira a la izquierda o a la derecha según dónde esté el mouse,
// y solo se inclina un poquito hacia arriba o abajo (máximo "inclinacionMaxima").
// Los disparos siguen saliendo exactamente hacia el mouse.
public class SubmarinoApuntarMouse : MonoBehaviour
{
    [Header("Inclinación del submarino")]
    [Tooltip("Cuántos grados como máximo se inclina hacia arriba o abajo")]
    public float inclinacionMaxima = 20f;
    [Tooltip("Qué tan rápido gira hacia la nueva inclinación (más alto = más rápido)")]
    public float suavizado = 10f;

    // Dirección real hacia el mouse — la usa SubmarinoDisparo para disparar bien
    public Vector2 DireccionApuntada { get; private set; } = Vector2.right;

    private SpriteRenderer sr;
    private float anguloActual = 0f;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.flipY = false; // ya no se usa el volteo vertical
    }

    void Update()
    {
        Vector3 posicionMouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        posicionMouse.z = transform.position.z;

        Vector2 direccion = (posicionMouse - transform.position);
        if (direccion.sqrMagnitude < 0.0001f) return;
        DireccionApuntada = direccion.normalized;

        bool mirandoIzquierda = direccion.x < 0;

        // Ángulo como si siempre mirara a la derecha (entre -90 y 90)
        float angulo = Mathf.Atan2(direccion.y, Mathf.Abs(direccion.x)) * Mathf.Rad2Deg;
        angulo = Mathf.Clamp(angulo, -inclinacionMaxima, inclinacionMaxima);

        // Si mira a la izquierda, la inclinación va al revés
        float anguloObjetivo = mirandoIzquierda ? -angulo : angulo;

        if (sr != null) sr.flipX = mirandoIzquierda;

        anguloActual = Mathf.LerpAngle(anguloActual, anguloObjetivo, suavizado * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0, 0, anguloActual);
    }
}