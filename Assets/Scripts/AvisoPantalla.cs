using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Texto grande en el centro de la pantalla para avisos cortos:
// "Oleada 1/3", "¡Zona despejada!", "Primero habla con la Tortuga", etc.
// Va en un Text (Legacy) dentro del Canvas.
[RequireComponent(typeof(Text))]
public class AvisoPantalla : MonoBehaviour
{
    public static AvisoPantalla instancia;

    public float duracionPorDefecto = 2f;

    private Text texto;
    private Coroutine rutina;
    private Vector3 escalaOriginal;

    void Awake()
    {
        instancia = this;
        texto = GetComponent<Text>();
        escalaOriginal = transform.localScale;
        PonerAlfa(0f);
    }

    public static void Mostrar(string mensaje)
    {
        Mostrar(mensaje, -1f);
    }

    public static void Mostrar(string mensaje, float duracion)
    {
        if (instancia == null)
        {
            Debug.Log("Aviso: " + mensaje + " (no hay AvisoPantalla en el Canvas)");
            return;
        }
        if (duracion <= 0f) duracion = instancia.duracionPorDefecto;
        if (instancia.rutina != null) instancia.StopCoroutine(instancia.rutina);
        instancia.rutina = instancia.StartCoroutine(instancia.Rutina(mensaje, duracion));
    }

    IEnumerator Rutina(string mensaje, float duracion)
    {
        texto.text = mensaje;

        // Aparece con un pequeño "salto"
        float t = 0f;
        while (t < 0.2f)
        {
            t += Time.unscaledDeltaTime;
            float p = t / 0.2f;
            PonerAlfa(p);
            transform.localScale = escalaOriginal * Mathf.Lerp(1.4f, 1f, p);
            yield return null;
        }
        PonerAlfa(1f);
        transform.localScale = escalaOriginal;

        yield return new WaitForSecondsRealtime(duracion);

        // Se desvanece
        t = 0f;
        while (t < 0.4f)
        {
            t += Time.unscaledDeltaTime;
            PonerAlfa(1f - t / 0.4f);
            yield return null;
        }
        PonerAlfa(0f);
        rutina = null;
    }

    void PonerAlfa(float a)
    {
        Color c = texto.color;
        c.a = a;
        texto.color = c;
        foreach (Outline o in GetComponents<Outline>())
        {
            Color oc = o.effectColor;
            oc.a = a;
            o.effectColor = oc;
        }
    }
}
