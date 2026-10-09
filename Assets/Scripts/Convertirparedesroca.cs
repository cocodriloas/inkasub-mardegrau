using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Herramienta de un solo uso: convierte las paredes grises (hijas de este objeto)
// en paredes con textura de roca repetida, sin deformarla y sin cambiar su colisión.
// Uso: agrégalo a "LimitesDelNivel", asigna la textura y haz clic derecho en el título
// del componente -> "Convertir paredes a roca". Se puede deshacer con Ctrl + Z.
// Si algo no funciona, usa "Revisar paredes (diagnóstico)" y mira la Console.
public class ConvertirParedesRoca : MonoBehaviour
{
    public Sprite texturaRoca;
    public Color colorRoca = Color.white;

    [Header("Para 'Convertir TODAS las paredes de la escena'")]
    [Tooltip("Solo convierte objetos cuyo nombre empiece con este texto (tus muros se llaman 'Roca1', 'Roca1 (9)', etc.)")]
    public string nombreEmpiezaCon = "Roca";

    [ContextMenu("Convertir TODAS las paredes de la escena")]
    void ConvertirTodaLaEscena()
    {
        if (texturaRoca == null)
        {
            Debug.LogError("ConvertirParedesRoca: primero asigna la textura de roca en el Inspector.");
            return;
        }

        SpriteRenderer[] todos = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int convertidas = 0;
        foreach (SpriteRenderer sr in todos)
        {
            if (!string.IsNullOrEmpty(nombreEmpiezaCon) && !sr.name.StartsWith(nombreEmpiezaCon)) continue;
            if (ConvertirUna(sr)) convertidas++;
        }
        Debug.Log("ConvertirParedesRoca: se convirtieron " + convertidas + " paredes en toda la escena (nombres que empiezan con '" + nombreEmpiezaCon + "').");
    }

    [ContextMenu("Revisar paredes (diagnóstico)")]
    void Revisar()
    {
        Transform[] todos = GetComponentsInChildren<Transform>(true);
        Debug.Log("DIAGNÓSTICO: '" + name + "' tiene " + (todos.Length - 1) + " objetos dentro.");

        int mostrados = 0;
        foreach (Transform t in todos)
        {
            if (t == transform) continue;
            List<string> comps = new List<string>();
            foreach (Component c in t.GetComponents<Component>())
            {
                if (c == null || c is Transform) continue;
                comps.Add(c.GetType().Name);
            }
            Debug.Log("   - " + t.name + "  ->  [" + string.Join(", ", comps) + "]  Escala: " + t.localScale);
            mostrados++;
            if (mostrados >= 15) { Debug.Log("   (solo muestro los primeros 15)"); break; }
        }
    }

    [ContextMenu("Convertir paredes a roca")]
    void Convertir()
    {
        if (texturaRoca == null)
        {
            Debug.LogError("ConvertirParedesRoca: primero asigna la textura de roca en el Inspector.");
            return;
        }

        SpriteRenderer[] renders = GetComponentsInChildren<SpriteRenderer>(true);
        if (renders.Length == 0)
        {
            Debug.LogWarning("ConvertirParedesRoca: no encontré ningún Sprite Renderer dentro de '" + name +
                             "'. Usa 'Revisar paredes (diagnóstico)' y mándame captura de la Console.");
            return;
        }

        int convertidas = 0;

        foreach (SpriteRenderer sr in renders)
        {
            if (ConvertirUna(sr)) convertidas++;
        }

        Debug.Log("ConvertirParedesRoca: se convirtieron " + convertidas + " paredes a roca.");
    }

    bool ConvertirUna(SpriteRenderer sr)
    {
            // No tocar los muros especiales (rojos)
            if (sr.GetComponent<MuroBloqueoNPC>() != null) return false;
            if (sr.GetComponent<MuroAvanceTrasero>() != null) return false;

            // No tocar paredes que tengan objetos hijos (se deformarían)
            if (sr.transform.childCount > 0)
            {
                Debug.LogWarning("ConvertirParedesRoca: salté '" + sr.name + "' porque tiene objetos hijos.");
                return false;
            }

            // Ya convertida antes
            if (sr.sprite == texturaRoca && sr.drawMode == SpriteDrawMode.Tiled) return false;

            Transform t = sr.transform;
            Vector3 escalaVieja = t.localScale;
            Vector2 escala2D = new Vector2(Mathf.Abs(escalaVieja.x), Mathf.Abs(escalaVieja.y));

            Vector2 tamañoSprite = sr.sprite != null ? (Vector2)sr.sprite.bounds.size : Vector2.one;
            Vector2 tamañoPared = Vector2.Scale(escala2D, tamañoSprite);

#if UNITY_EDITOR
            Undo.RecordObject(t, "Convertir paredes a roca");
            Undo.RecordObject(sr, "Convertir paredes a roca");
            foreach (Collider2D col in t.GetComponents<Collider2D>())
                Undo.RecordObject(col, "Convertir paredes a roca");
#endif
            // La colisión debe seguir ocupando exactamente lo mismo
            string tipoColision = AjustarColisiones(t, escala2D);

            // Pasar el tamaño de la "escala" al "tamaño del sprite en mosaico"
            sr.sprite = texturaRoca;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = tamañoPared;
            sr.color = colorRoca;
            t.localScale = Vector3.one;

#if UNITY_EDITOR
            EditorUtility.SetDirty(sr);
            EditorUtility.SetDirty(t);
#endif
            Debug.Log("   Convertida: " + sr.name + " (colisión: " + tipoColision + ")");
            return true;
    }

    string AjustarColisiones(Transform t, Vector2 escala)
    {
        List<string> tipos = new List<string>();

        foreach (Collider2D col in t.GetComponents<Collider2D>())
        {
            if (col is BoxCollider2D caja)
            {
                caja.autoTiling = false;
                caja.offset = Vector2.Scale(caja.offset, escala);
                caja.size = Vector2.Scale(caja.size, escala);
                tipos.Add("Box");
            }
            else if (col is PolygonCollider2D poli)
            {
                poli.offset = Vector2.Scale(poli.offset, escala);
                for (int p = 0; p < poli.pathCount; p++)
                {
                    Vector2[] puntos = poli.GetPath(p);
                    for (int i = 0; i < puntos.Length; i++) puntos[i] = Vector2.Scale(puntos[i], escala);
                    poli.SetPath(p, puntos);
                }
                tipos.Add("Polygon");
            }
            else if (col is EdgeCollider2D borde)
            {
                borde.offset = Vector2.Scale(borde.offset, escala);
                Vector2[] puntos = borde.points;
                for (int i = 0; i < puntos.Length; i++) puntos[i] = Vector2.Scale(puntos[i], escala);
                borde.points = puntos;
                tipos.Add("Edge");
            }
            else if (col is CapsuleCollider2D capsula)
            {
                capsula.offset = Vector2.Scale(capsula.offset, escala);
                capsula.size = Vector2.Scale(capsula.size, escala);
                tipos.Add("Capsule");
            }
            else
            {
                tipos.Add(col.GetType().Name + " (no ajustada)");
            }
#if UNITY_EDITOR
            EditorUtility.SetDirty(col);
#endif
        }

        return tipos.Count > 0 ? string.Join(", ", tipos) : "ninguna";
    }
}