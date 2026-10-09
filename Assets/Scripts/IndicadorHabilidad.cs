using UnityEngine;
using UnityEngine.UI;

// Indicador del Disparo Doble en el HUD.
// - Bloqueado: muy oscuro.
// - Activo: a color, se va vaciando mientras dura.
// - Recargando: gris, y el color se va "llenando" en círculo.
// - Listo: a color completo y brilla/pulsa un poco (se puede usar con K).
public class IndicadorHabilidad : MonoBehaviour
{
    [Tooltip("Si lo dejas vacío, lo busca solo en el Submarino")]
    public SubmarinoDisparo disparo;

    [Header("Imágenes (hijas de este objeto)")]
    [Tooltip("Imagen de abajo, en gris")]
    public Image iconoGris;
    [Tooltip("Imagen de arriba, a color, con Image Type: Filled / Radial 360")]
    public Image iconoColor;
    [Tooltip("Opcional: el texto 'K' debajo del ícono")]
    public Text textoTecla;

    [Header("Brillo cuando está lista")]
    public float pulsoEscala = 0.08f;
    public float velocidadPulso = 3f;

    private Vector3 escalaOriginal;

    void Start()
    {
        if (disparo == null) disparo = FindFirstObjectByType<SubmarinoDisparo>();
        escalaOriginal = transform.localScale;
        if (iconoGris != null) iconoGris.color = new Color(0.35f, 0.35f, 0.4f, 1f);
    }

    void Update()
    {
        if (disparo == null || iconoColor == null) return;

        float escala = 1f;

        if (!disparo.HabilidadDesbloqueada)
        {
            // Todavía no se consigue: casi negro
            iconoColor.fillAmount = 0f;
            if (iconoGris != null) iconoGris.color = new Color(0.15f, 0.15f, 0.18f, 0.8f);
            if (textoTecla != null) textoTecla.enabled = false;
        }
        else
        {
            if (iconoGris != null) iconoGris.color = new Color(0.35f, 0.35f, 0.4f, 1f);
            if (textoTecla != null) textoTecla.enabled = true;

            iconoColor.fillAmount = disparo.ProgresoHabilidad;

            if (disparo.HabilidadActiva)
            {
                // En uso: pulso rápido y brillante
                escala = 1f + Mathf.Abs(Mathf.Sin(Time.time * 10f)) * 0.1f;
                iconoColor.color = Color.white;
            }
            else if (disparo.HabilidadLista)
            {
                // Lista: brilla suave hacia turquesa y late
                float p = (Mathf.Sin(Time.time * velocidadPulso) + 1f) * 0.5f;
                escala = 1f + p * pulsoEscala;
                iconoColor.color = Color.Lerp(Color.white, new Color(0.6f, 1f, 1f), p);
            }
            else
            {
                // Recargando
                iconoColor.color = new Color(1f, 1f, 1f, 0.9f);
            }
        }

        transform.localScale = escalaOriginal * escala;
    }
}
