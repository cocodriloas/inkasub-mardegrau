using UnityEngine;

public class MuroAvanceTrasero : MonoBehaviour
{
    [Header("A quién sigue")]
    public Transform jugador;

    [Header("Comportamiento")]
    public float distanciaDetras = 8f;
    public float velocidadAvance = 1f;

    [Header("No empieza a avanzar hasta hablar con este NPC (opcional)")]
    public NPCInteraccion npcRequeridoParaActivarse;

    private float maximaPosicionX;
    private Rigidbody2D rb;
    private bool activado = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if (!activado)
        {
            if (npcRequeridoParaActivarse == null || npcRequeridoParaActivarse.yaHablo)
            {
                activado = true;
                maximaPosicionX = jugador.position.x; // arranca justo desde donde estés al activarse
            }
            else
            {
                return; // todavía no debe moverse para nada
            }
        }

        if (jugador.position.x > maximaPosicionX)
        {
            maximaPosicionX = jugador.position.x;
        }

        float posicionObjetivo = maximaPosicionX - distanciaDetras;

        if (posicionObjetivo > rb.position.x)
        {
            Vector2 nuevaPos = new Vector2(
                Mathf.MoveTowards(rb.position.x, posicionObjetivo, velocidadAvance * Time.fixedDeltaTime),
                rb.position.y
            );
            rb.MovePosition(nuevaPos);
        }
    }
}