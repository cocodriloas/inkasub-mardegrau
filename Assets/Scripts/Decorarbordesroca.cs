using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Herramienta de un solo uso: pone piedras, corales y algas irregulares a lo largo
// de los bordes de cada pared de roca, para que no se vean como rectángulos perfectos.
// Las piezas son SOLO decoración (sin colisión): el juego se sigue sintiendo igual.
// Uso: agrégalo a cualquier objeto, llena "Piezas Borde" con los sprites cortados (Pivot: Bottom)
// y haz clic derecho en el título del componente -> "Decorar bordes de las paredes".
public class DecorarBordesRoca : MonoBehaviour
{
    [Header("Piezas de borde (arrastra aquí todos los sprites cortados)")]
    public Sprite[] piezasBorde;

    [Header("Qué paredes decorar")]
    public string nombreEmpiezaCon = "Roca";

    [Header("Aspecto")]
    [Tooltip("Distancia promedio entre piezas (en unidades de Unity)")]
    public float separacion = 0.7f;
    [Tooltip("Tamaño mínimo y máximo de cada pieza")]
    public Vector2 rangoEscala = new Vector2(0.8f, 1.3f);
    [Tooltip("Cuánto se mete la base de cada pieza dentro de la pared (en unidades de Unity)")]
    public float hundimiento = 0.15f;
    [Tooltip("Probabilidad de que una pieza quede DELANTE de la pared (el resto queda detrás)")]
    [Range(0f, 1f)] public float probabilidadDelante = 0.6f;
    [Tooltip("Cambia este número para obtener otra distribución al azar")]
    public int semilla = 123;

    const string PREFIJO = "BordeRoca_";

    [ContextMenu("Decorar bordes de las paredes")]
    void Decorar()
    {
        if (piezasBorde == null || piezasBorde.Length == 0)
        {
            Debug.LogError("DecorarBordesRoca: primero arrastra los sprites de las piezas a 'Piezas Borde'.");
            return;
        }

        Limpiar(); // para no duplicar si lo usas dos veces
        Random.InitState(semilla);

        int paredes = 0, piezas = 0;
        SpriteRenderer[] todos = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (SpriteRenderer pared in todos)
        {
            if (!pared.name.StartsWith(nombreEmpiezaCon)) continue;
            if (pared.name.StartsWith(PREFIJO)) continue;
            if (pared.drawMode != SpriteDrawMode.Tiled) continue; // solo paredes ya convertidas a roca

            Vector2 tam = pared.size;
            bool largoEnX = tam.x >= tam.y;
            float largo = largoEnX ? tam.x : tam.y;
            float mitadGrosor = (largoEnX ? tam.y : tam.x) * 0.5f;

            Vector2 ejeLargo = largoEnX ? Vector2.right : Vector2.up;
            Vector2 normal = largoEnX ? Vector2.up : Vector2.right;

            // Los dos lados largos de la pared
            for (int lado = -1; lado <= 1; lado += 2)
            {
                float pos = -largo * 0.5f + Random.Range(0f, separacion);
                while (pos < largo * 0.5f)
                {
                    CrearPieza(pared, ejeLargo * pos, normal * lado, mitadGrosor);
                    piezas++;
                    pos += separacion * Random.Range(0.6f, 1.4f);
                }
            }
            paredes++;
        }

        Debug.Log("DecorarBordesRoca: " + piezas + " piezas puestas en " + paredes + " paredes.");
    }

    void CrearPieza(SpriteRenderer pared, Vector2 posicionEnPared, Vector2 haciaAfuera, float mitadGrosor)
    {
        Sprite sprite = piezasBorde[Random.Range(0, piezasBorde.Length)];
        if (sprite == null) return;

        float escala = Random.Range(rangoEscala.x, rangoEscala.y);

        GameObject go = new GameObject(PREFIJO + sprite.name);
        go.transform.SetParent(pared.transform, false);

        // La base de la pieza (su Pivot debe estar en "Bottom") se apoya en el borde
        // de la pared, un poquito hundida, y la pieza crece "hacia afuera"
        float distancia = mitadGrosor - hundimiento;
        go.transform.localPosition = posicionEnPared + haciaAfuera * distancia;
        float angulo = Mathf.Atan2(haciaAfuera.y, haciaAfuera.x) * Mathf.Rad2Deg - 90f;
        go.transform.localRotation = Quaternion.Euler(0, 0, angulo + Random.Range(-8f, 8f));
        go.transform.localScale = new Vector3(escala, escala, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.flipX = Random.value > 0.5f;
        sr.sortingLayerID = pared.sortingLayerID;
        sr.sortingOrder = pared.sortingOrder + (Random.value < probabilidadDelante ? 1 : -1);
        sr.sharedMaterial = pared.sharedMaterial;

#if UNITY_EDITOR
        Undo.RegisterCreatedObjectUndo(go, "Decorar bordes de roca");
#endif
    }

    [ContextMenu("Quitar decoraciones de bordes")]
    void Limpiar()
    {
        List<GameObject> borrar = new List<GameObject>();
        foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.name.StartsWith(PREFIJO)) borrar.Add(t.gameObject);
        }

        foreach (GameObject go in borrar)
        {
#if UNITY_EDITOR
            Undo.DestroyObjectImmediate(go);
#else
            DestroyImmediate(go);
#endif
        }

        if (borrar.Count > 0) Debug.Log("DecorarBordesRoca: se quitaron " + borrar.Count + " piezas.");
    }
}