using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class Oleada
{
    public GameObject[] prefabsEnemigos;
    public Transform[] puntosDeSpawn; // debe tener el mismo tamaño que "prefabsEnemigos"
}

public class GeneradorOleadas : MonoBehaviour
{
    [Header("Oleadas en orden (se generan una tras otra)")]
    public Oleada[] oleadas;

    [Header("Tiempo de espera entre oleadas")]
    public float tiempoEntreOleadas = 2f;

    [Header("Muros: se CIERRAN al entrar y se ABREN al vencer todas las oleadas")]
    public Barrera[] barreras;

    [Header("(Opcional) No empieza hasta hablar con este NPC")]
    public NPCInteraccion npcRequerido;
    public string mensajeSinHablar = "Primero habla con el guardián de esta zona.";

    [Header("Mensajes en pantalla")]
    public string mensajeFinal = "¡Zona despejada!";

    private bool yaActivado = false;
    private float ultimoAviso = -10f;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (yaActivado) return;
        if (!other.CompareTag("Player")) return;

        // ¿Falta hablar con alguien?
        if (npcRequerido != null && !npcRequerido.yaHablo)
        {
            if (Time.time - ultimoAviso > 2.5f)
            {
                ultimoAviso = Time.time;
                AvisoPantalla.Mostrar(mensajeSinHablar);
            }
            return;
        }

        yaActivado = true;
        StartCoroutine(GenerarOleadas());
    }

    // Por si el jugador ya estaba dentro cuando terminó de hablar con el NPC
    void OnTriggerStay2D(Collider2D other)
    {
        if (!yaActivado && npcRequerido != null && npcRequerido.yaHablo && other.CompareTag("Player"))
        {
            yaActivado = true;
            StartCoroutine(GenerarOleadas());
        }
    }

    private IEnumerator GenerarOleadas()
    {
        // Encerrar al jugador
        foreach (Barrera b in barreras) if (b != null) b.Cerrar();

        for (int n = 0; n < oleadas.Length; n++)
        {
            Oleada oleada = oleadas[n];
            AvisoPantalla.Mostrar("Oleada " + (n + 1) + " / " + oleadas.Length, 1.5f);
            yield return new WaitForSeconds(1f);

            List<GameObject> generados = new List<GameObject>();

            for (int i = 0; i < oleada.prefabsEnemigos.Length; i++)
            {
                Vector3 posicion = (i < oleada.puntosDeSpawn.Length && oleada.puntosDeSpawn[i] != null)
                    ? oleada.puntosDeSpawn[i].position : transform.position;
                GameObject enemigo = Instantiate(oleada.prefabsEnemigos[i], posicion, Quaternion.identity);
                generados.Add(enemigo);
            }

            // Esperamos a que TODOS los enemigos de esta oleada mueran antes de mandar la siguiente
            bool quedanVivos = true;
            while (quedanVivos)
            {
                quedanVivos = false;
                foreach (GameObject enemigo in generados)
                {
                    if (enemigo != null) { quedanVivos = true; break; }
                }
                yield return new WaitForSeconds(0.5f);
            }

            if (n < oleadas.Length - 1) yield return new WaitForSeconds(tiempoEntreOleadas);
        }

        // ¡Ganaste! Se abren los muros
        AvisoPantalla.Mostrar(mensajeFinal, 2.5f);
        foreach (Barrera b in barreras) if (b != null) b.Abrir();

        Destroy(gameObject); // ya cumplió su función
    }

    // Dibuja en la Scene (solo en el editor) dónde aparecerá cada enemigo:
    // círculos rojos = puntos de aparición, líneas amarillas = muros de esta zona.
    void OnDrawGizmos()
    {
        if (oleadas != null)
        {
            Gizmos.color = new Color(1f, 0.25f, 0.25f, 0.9f);
            foreach (Oleada o in oleadas)
            {
                if (o == null || o.puntosDeSpawn == null) continue;
                foreach (Transform p in o.puntosDeSpawn)
                {
                    if (p == null) continue;
                    Gizmos.DrawWireSphere(p.position, 0.6f);
                    Gizmos.DrawLine(p.position + Vector3.left * 0.4f, p.position + Vector3.right * 0.4f);
                    Gizmos.DrawLine(p.position + Vector3.up * 0.4f, p.position + Vector3.down * 0.4f);
                }
            }
        }
        if (barreras != null)
        {
            Gizmos.color = Color.yellow;
            foreach (Barrera b in barreras)
                if (b != null) Gizmos.DrawLine(transform.position, b.transform.position);
        }
    }
}