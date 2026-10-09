using UnityEngine;

// Anima una decoración (alga, anémona, burbujas) pasando sus cuadros en bucle.
// No necesita Animator. Cada copia empieza en un cuadro y velocidad un poco distintos,
// así las algas no se mueven todas iguales "como robots".
[RequireComponent(typeof(SpriteRenderer))]
public class DecoracionAnimada : MonoBehaviour
{
    [Tooltip("Arrastra aquí los cuadros de la animación, en orden")]
    public Sprite[] cuadros;

    [Tooltip("Cuadros por segundo")]
    public float velocidad = 6f;

    [Tooltip("Variación al azar de la velocidad entre copias (0.15 = ±15%)")]
    [Range(0f, 0.5f)] public float variacionVelocidad = 0.15f;

    [Tooltip("Si está marcado, cada copia empieza en un cuadro al azar")]
    public bool empezarEnCuadroAlAzar = true;

    [Tooltip("Voltear horizontalmente al azar para que no se vean todas iguales")]
    public bool volteoAlAzar = true;

    private SpriteRenderer sr;
    private float tiempo;
    private float velocidadReal;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        velocidadReal = velocidad * (1f + Random.Range(-variacionVelocidad, variacionVelocidad));
        if (empezarEnCuadroAlAzar && cuadros != null && cuadros.Length > 0)
            tiempo = Random.Range(0f, cuadros.Length / Mathf.Max(0.01f, velocidadReal));
        if (volteoAlAzar) sr.flipX = Random.value > 0.5f;
    }

    void Update()
    {
        if (cuadros == null || cuadros.Length == 0) return;
        tiempo += Time.deltaTime;
        int indice = (int)(tiempo * velocidadReal) % cuadros.Length;
        sr.sprite = cuadros[indice];
    }
}