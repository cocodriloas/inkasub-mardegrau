using UnityEngine;
using System.Collections;

public class ZonaEncuentro : MonoBehaviour
{
    [Header("Enemigos a activar en esta zona")]
    public GameObject[] enemigosAActivar;

    [Header("Muros: se CIERRAN al entrar y se ABREN al vencer a todos (opcional)")]
    public Barrera[] barreras;

    [Header("(Opcional) No empieza hasta hablar con este NPC")]
    public NPCInteraccion npcRequerido;
    public string mensajeSinHablar = "Primero habla con el guardián de esta zona.";

    [Header("Mensajes en pantalla (déjalos vacíos para no mostrar nada)")]
    public string mensajeInicio = "¡Enemigos a la vista!";
    public string mensajeFinal = "¡Zona despejada!";

    private bool yaActivada = false;
    private float ultimoAviso = -10f;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (yaActivada) return;
        if (!other.CompareTag("Player")) return;

        if (npcRequerido != null && !npcRequerido.yaHablo)
        {
            if (Time.time - ultimoAviso > 2.5f)
            {
                ultimoAviso = Time.time;
                AvisoPantalla.Mostrar(mensajeSinHablar);
            }
            return;
        }

        yaActivada = true;
        StartCoroutine(Encuentro());
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!yaActivada && npcRequerido != null && npcRequerido.yaHablo && other.CompareTag("Player"))
        {
            yaActivada = true;
            StartCoroutine(Encuentro());
        }
    }

    IEnumerator Encuentro()
    {
        foreach (Barrera b in barreras) if (b != null) b.Cerrar();
        if (!string.IsNullOrEmpty(mensajeInicio)) AvisoPantalla.Mostrar(mensajeInicio, 1.5f);

        foreach (GameObject enemigo in enemigosAActivar)
        {
            if (enemigo != null) enemigo.SetActive(true);
        }

        // Si esta zona no tiene muros, no hace falta esperar: termina aquí (como antes)
        if (barreras == null || barreras.Length == 0)
        {
            Destroy(gameObject);
            yield break;
        }

        // Esperar a que todos mueran (al morir se destruyen)
        bool quedanVivos = true;
        while (quedanVivos)
        {
            quedanVivos = false;
            foreach (GameObject enemigo in enemigosAActivar)
            {
                if (enemigo != null) { quedanVivos = true; break; }
            }
            yield return new WaitForSeconds(0.5f);
        }

        if (!string.IsNullOrEmpty(mensajeFinal)) AvisoPantalla.Mostrar(mensajeFinal, 2.5f);
        foreach (Barrera b in barreras) if (b != null) b.Abrir();

        Destroy(gameObject);
    }
}
