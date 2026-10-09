using System.Collections;
using UnityEngine;

// Pieza visual del Doble Cañón. Va en un objeto HIJO del Submarino.
// Aparece desplegándose cuando se activa la habilidad, brilla mientras está activo,
// hace el destello en cada disparo y se repliega al terminar.
[RequireComponent(typeof(SpriteRenderer))]
public class DobleCanonVisual : MonoBehaviour
{
    [Header("Cuadros de cada animación")]
    public Sprite[] despliegue;
    public Sprite[] activo;
    public Sprite[] disparo;

    [Header("Velocidades (cuadros por segundo)")]
    public float velocidadDespliegue = 12f;
    public float velocidadActivo = 8f;
    public float velocidadDisparo = 16f;

    private SpriteRenderer sr;
    private SpriteRenderer srSubmarino;
    private float posicionXOriginal;
    private float tiempo;
    private bool visible = false;
    private Coroutine animacionUnaVez;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (despliegue == null || despliegue.Length == 0 || activo == null || activo.Length == 0)
            Debug.LogWarning("DobleCanonVisual: faltan cuadros en Despliegue o Activo (revisa el Inspector del objeto DobleCanon).");
        if (transform.parent != null) srSubmarino = transform.parent.GetComponent<SpriteRenderer>();
        posicionXOriginal = transform.localPosition.x;
        sr.enabled = false;
    }

    void LateUpdate()
    {
        // Seguir el volteo del submarino (cuando mira a la izquierda)
        if (srSubmarino != null)
        {
            sr.flipX = srSubmarino.flipX;
            Vector3 p = transform.localPosition;
            p.x = srSubmarino.flipX ? -posicionXOriginal : posicionXOriginal;
            transform.localPosition = p;
            sr.sortingLayerID = srSubmarino.sortingLayerID;
            sr.sortingOrder = srSubmarino.sortingOrder + 1;
        }

        // Brillo en bucle mientras está activo
        if (visible && animacionUnaVez == null && activo != null && activo.Length > 0)
        {
            tiempo += Time.deltaTime;
            sr.sprite = activo[(int)(tiempo * velocidadActivo) % activo.Length];
        }
    }

    // ---------- Para llamar desde SubmarinoDisparo ----------

    public void Mostrar()
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true); // por si quedó apagado en la escena
        visible = true;
        sr.enabled = true;
        Reproducir(despliegue, velocidadDespliegue, false, false);
    }

    public void Disparar()
    {
        if (!visible) return;
        Reproducir(disparo, velocidadDisparo, false, false);
    }

    public void Ocultar()
    {
        if (!visible) return;
        visible = false;
        // Se repliega (despliegue al revés) y desaparece
        Reproducir(despliegue, velocidadDespliegue, true, true);
    }

    // ---------- Funcionamiento interno ----------

    void Reproducir(Sprite[] cuadros, float vel, bool alReves, bool ocultarAlFinal)
    {
        if (animacionUnaVez != null) StopCoroutine(animacionUnaVez);
        animacionUnaVez = StartCoroutine(Rutina(cuadros, vel, alReves, ocultarAlFinal));
    }

    IEnumerator Rutina(Sprite[] cuadros, float vel, bool alReves, bool ocultarAlFinal)
    {
        if (cuadros != null && cuadros.Length > 0)
        {
            float espera = 1f / Mathf.Max(1f, vel);
            for (int i = 0; i < cuadros.Length; i++)
            {
                sr.sprite = cuadros[alReves ? cuadros.Length - 1 - i : i];
                yield return new WaitForSeconds(espera);
            }
        }
        if (ocultarAlFinal) sr.enabled = false;
        animacionUnaVez = null;
    }
}
