using System.Collections;
using UnityEngine;

public class EnemigoAtaqueGarra : MonoBehaviour
{
    [Header("Configuración del ataque")]
    [Tooltip("A qué distancia empieza el ataque (la garra estirada llega lejos)")]
    public float rangoAtaque = 2f;
    public float tiempoEntreAtaques = 2f;
    [Tooltip("Cuánto brilla amarillo antes de golpear. Más alto = más tiempo para esquivar")]
    public float tiempoDeCarga = 0.6f;
    [Tooltip("Cuántos segundos después de lanzar el zarpazo la garra hace daño (cuando está estirada)")]
    public float momentoDelGolpe = 0.15f;
    public int dañoGarra = 2;

    [Header("Aviso de color (solo si el enemigo NO tiene EnemigoAnimaciones)")]
    public Color colorDeCarga = Color.yellow;

    private Transform objetivo;
    private SubmarinoSalud saludDelJugador;
    private EnemigoMovimiento movimiento;
    private EnemigoAnimaciones anim;
    private SpriteRenderer sr;
    private Color colorOriginal;

    private float cooldownRestante = 0f;
    private bool estaAtacando = false;

    void Start()
    {
        GameObject jugador = GameObject.FindGameObjectWithTag("Player");
        if (jugador != null)
        {
            objetivo = jugador.transform;
            saludDelJugador = jugador.GetComponent<SubmarinoSalud>();
        }

        movimiento = GetComponent<EnemigoMovimiento>();
        anim = GetComponent<EnemigoAnimaciones>();
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) colorOriginal = sr.color;
    }

    void Update()
    {
        if (cooldownRestante > 0)
            cooldownRestante -= Time.deltaTime;

        if (objetivo == null || estaAtacando) return;

        float distancia = Vector2.Distance(transform.position, objetivo.position);

        if (distancia <= rangoAtaque && cooldownRestante <= 0)
        {
            StartCoroutine(RealizarAtaque());
        }
    }

    private IEnumerator RealizarAtaque()
    {
        estaAtacando = true;

        // Se detiene mientras "carga" el golpe
        if (movimiento != null) movimiento.enabled = false;

        // 1) AVISO: levanta la garra y brilla amarillo
        AudioManager.Sonar(Sonido.AvisoEnemigo);
        if (anim != null) anim.IniciarAviso(tiempoDeCarga);
        else if (sr != null) sr.color = colorDeCarga;

        yield return new WaitForSeconds(tiempoDeCarga);

        // 2) ZARPAZO: la garra se estira
        if (anim != null) anim.Disparar();
        else if (sr != null) sr.color = colorOriginal;
        AudioManager.Sonar(Sonido.Garra);

        // 3) El daño llega justo cuando la garra está estirada
        yield return new WaitForSeconds(momentoDelGolpe);

        if (objetivo != null)
        {
            float distancia = Vector2.Distance(transform.position, objetivo.position);
            if (distancia <= rangoAtaque + 0.3f && saludDelJugador != null)
            {
                saludDelJugador.RecibirDaño(dañoGarra);
            }
        }

        // 4) Recoge la garra antes de volver a moverse
        yield return new WaitForSeconds(0.3f);

        if (movimiento != null) movimiento.enabled = true;

        cooldownRestante = tiempoEntreAtaques;
        estaAtacando = false;
    }

    // Si el enemigo muere a mitad del ataque, se cancela el golpe
    void OnDisable()
    {
        StopAllCoroutines();
        estaAtacando = false;
        if (anim == null && sr != null) sr.color = colorOriginal;
    }
}
