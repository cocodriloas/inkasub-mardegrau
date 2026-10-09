using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Refuerzos del Iceberg de Basura: manda enemigos desde el suelo cada cierto tiempo, SIN mensajes en pantalla.
// Va en el MISMO objeto que JefeIceberg. Empieza solo cuando empieza la pelea y se detiene cuando el jefe muere
// (al morir, JefeIceberg elimina a los enemigos que queden).
[RequireComponent(typeof(JefeIceberg))]
public class JefeRefuerzos : MonoBehaviour
{
    [Header("Enemigos que aparecen (arrastra tus prefabs de enemigos)")]
    public GameObject[] prefabsEnemigos;

    [Header("Dónde aparecen (objetos vacíos pegados al suelo)")]
    public Transform[] puntosDeAparicion;
    [Tooltip("No aparecen a menos de esta distancia del submarino (si hay otro punto más lejos)")]
    public float distanciaMinimaDelJugador = 6f;

    [Header("Ritmo")]
    public float pausaInicial = 4f;
    [Tooltip("Cada cuántos segundos llega una oleada: un tiempo al azar entre el mínimo y el máximo")]
    public Vector2 segundosEntreOleadas = new Vector2(5f, 10f);
    [Tooltip("Cuántos enemigos llegan en cada oleada en la fase 1, 2 y 3")]
    public int[] enemigosPorOleadaPorFase = { 1, 1, 2 };
    [Tooltip("Máximo de enemigos vivos a la vez. Si ya hay tantos, la oleada espera.")]
    public int maximoVivos = 3;

    private JefeIceberg jefe;
    private Transform jugador;
    private List<GameObject> vivos = new List<GameObject>();

    void Start()
    {
        jefe = GetComponent<JefeIceberg>();

        GameObject j = GameObject.FindGameObjectWithTag("Player");
        if (j != null) jugador = j.transform;

        if (prefabsEnemigos == null || prefabsEnemigos.Length == 0)
            Debug.LogWarning("JefeRefuerzos: faltan los Prefabs Enemigos. No saldrán refuerzos.");
        if (puntosDeAparicion == null || puntosDeAparicion.Length == 0)
            Debug.LogWarning("JefeRefuerzos: faltan los Puntos De Aparicion. No saldrán refuerzos.");

        StartCoroutine(Rutina());
    }

    IEnumerator Rutina()
    {
        // Esperar a que empiece la pelea
        while (!jefe.EstaPeleando)
        {
            if (jefe.EstaMuerto) yield break;
            yield return null;
        }

        yield return new WaitForSeconds(pausaInicial);

        while (jefe.EstaPeleando)
        {
            float minimo = Mathf.Max(1f, segundosEntreOleadas.x);
            float maximo = Mathf.Max(minimo, segundosEntreOleadas.y);
            yield return new WaitForSeconds(Random.Range(minimo, maximo));
            if (!jefe.EstaPeleando) yield break;

            LimpiarLista();
            int libres = maximoVivos - vivos.Count;
            if (libres <= 0) continue;

            int cantidad = Mathf.Min(libres, ValorInt(enemigosPorOleadaPorFase, 1));
            for (int i = 0; i < cantidad; i++) Aparecer();
        }
    }

    void Aparecer()
    {
        if (prefabsEnemigos == null || prefabsEnemigos.Length == 0) return;

        Transform punto = ElegirPunto();
        if (punto == null) return;

        GameObject prefab = prefabsEnemigos[Random.Range(0, prefabsEnemigos.Length)];
        if (prefab == null) return;

        GameObject enemigo = Instantiate(prefab, punto.position, Quaternion.identity);
        vivos.Add(enemigo);
    }

    // Prefiere un punto lejos del submarino. Si todos están cerca, usa el más lejano.
    Transform ElegirPunto()
    {
        if (puntosDeAparicion == null || puntosDeAparicion.Length == 0) return null;

        List<Transform> lejanos = new List<Transform>();
        Transform masLejano = null;
        float mayorDistancia = -1f;

        foreach (Transform p in puntosDeAparicion)
        {
            if (p == null) continue;
            float d = jugador != null ? Vector2.Distance(p.position, jugador.position) : 999f;
            if (d >= distanciaMinimaDelJugador) lejanos.Add(p);
            if (d > mayorDistancia) { mayorDistancia = d; masLejano = p; }
        }

        if (lejanos.Count > 0) return lejanos[Random.Range(0, lejanos.Count)];
        return masLejano;
    }

    void LimpiarLista()
    {
        for (int i = vivos.Count - 1; i >= 0; i--)
        {
            if (vivos[i] == null) vivos.RemoveAt(i);
        }
    }

    int ValorInt(int[] lista, int porDefecto)
    {
        if (lista == null || lista.Length == 0) return porDefecto;
        return lista[Mathf.Clamp(jefe.FaseActual - 1, 0, lista.Length - 1)];
    }
}