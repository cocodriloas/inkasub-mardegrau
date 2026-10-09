using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SubmarinoSalud : MonoBehaviour
{
    [Header("Vida del submarino")]
    public int vidaMaxima = 5;
    private int vidaActual;

    [Header("Vidas con iconos de submarino (se crean solos al dar Play)")]
    [Tooltip("Objeto vacío dentro del Canvas donde aparecerán los submarinitos")]
    public RectTransform contenedorVidas;
    [Tooltip("La imagen del submarinito (puedes usar el primer cuadro de Navegando)")]
    public Sprite iconoVida;
    public Vector2 tamañoIcono = new Vector2(80, 50);
    public float espacioEntreIconos = 10f;
    public Color colorVidaLlena = Color.white;
    public Color colorVidaPerdida = new Color(0.1f, 0.1f, 0.15f, 0.55f); // silueta oscura
    public Color colorCuracion = new Color(0.4f, 1f, 0.7f, 1f);           // destello verde al curar

    [Header("UI antigua (opcional, puedes dejarla vacía)")]
    public Slider barraVida;

    [Header("Flash rojo en pantalla al recibir daño")]
    public Image imagenFlashDaño;
    public float duracionFlash = 0.15f;

    [Header("Invulnerabilidad tras recibir daño")]
    public float tiempoInvulnerabilidad = 1f;
    private float tiempoRestanteInvulnerable = 0f;

    private bool estaDerrotado = false;
    private SubmarinoAnimaciones anim;
    private Image[] iconos;

    void Start()
    {
        anim = GetComponent<SubmarinoAnimaciones>();
        if (anim == null) anim = gameObject.AddComponent<SubmarinoAnimaciones>();

        vidaActual = vidaMaxima;

        if (barraVida != null)
        {
            barraVida.maxValue = vidaMaxima;
            barraVida.value = vidaActual;
        }

        CrearIconos();
        ActualizarIconos();
    }

    void Update()
    {
        if (tiempoRestanteInvulnerable > 0)
            tiempoRestanteInvulnerable -= Time.deltaTime;
    }

    void OnCollisionEnter2D(Collision2D colision)
    {
        if (colision.gameObject.CompareTag("Enemigo"))
        {
            RecibirDaño(1);
        }
    }

    void OnCollisionStay2D(Collision2D colision)
    {
        if (colision.gameObject.CompareTag("Enemigo"))
        {
            RecibirDaño(1);
        }
    }

    public void RecibirDaño(int cantidad)
    {
        if (estaDerrotado || tiempoRestanteInvulnerable > 0) return;

        int vidaAnterior = vidaActual;
        vidaActual -= cantidad;
        if (vidaActual < 0) vidaActual = 0;
        tiempoRestanteInvulnerable = tiempoInvulnerabilidad;
        Debug.Log("Vida del submarino: " + vidaActual);

        if (barraVida != null) barraVida.value = vidaActual;
        if (imagenFlashDaño != null) StartCoroutine(FlashDaño());

        // Los submarinitos que se perdieron se vuelven silueta con un "salto"
        ActualizarIconos();
        for (int i = vidaActual; i < vidaAnterior; i++)
            AnimarIcono(i, colorVidaPerdida);

        // Animación y sonido de daño
        if (anim != null) anim.ReproducirDaño();
        AudioManager.Sonar(Sonido.DanoSubmarino);

        if (vidaActual <= 0)
        {
            estaDerrotado = true;
            GameManager.instancia.MostrarDerrota();
        }
    }

    private IEnumerator FlashDaño()
    {
        Color c = imagenFlashDaño.color;
        c.a = 0.4f;
        imagenFlashDaño.color = c;

        yield return new WaitForSeconds(duracionFlash);

        c.a = 0f;
        imagenFlashDaño.color = c;
    }

    public void Curar(int cantidad)
    {
        if (estaDerrotado) return;
        int vidaAnterior = vidaActual;
        vidaActual = Mathf.Min(vidaActual + cantidad, vidaMaxima);
        DespuesDeCurar(vidaAnterior);
    }

    public void CurarCompleto()
    {
        if (estaDerrotado) return;
        int vidaAnterior = vidaActual;
        vidaActual = vidaMaxima;
        DespuesDeCurar(vidaAnterior);
    }

    void DespuesDeCurar(int vidaAnterior)
    {
        if (barraVida != null) barraVida.value = vidaActual;

        // Los submarinitos recuperados vuelven a colorearse con un destello verde
        ActualizarIconos();
        for (int i = vidaAnterior; i < vidaActual; i++)
            AnimarIcono(i, colorCuracion);

        // Animación y sonido de curación
        if (anim != null) anim.ReproducirCuracion();
        AudioManager.Sonar(Sonido.Curacion);
    }

    // ---------------- Iconos de vida ----------------

    void CrearIconos()
    {
        if (contenedorVidas == null || iconoVida == null) return;

        iconos = new Image[vidaMaxima];
        for (int i = 0; i < vidaMaxima; i++)
        {
            GameObject go = new GameObject("Vida_" + (i + 1), typeof(RectTransform), typeof(Image));
            go.transform.SetParent(contenedorVidas, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(0, 0.5f);
            rt.pivot = new Vector2(0, 0.5f);
            rt.sizeDelta = tamañoIcono;
            rt.anchoredPosition = new Vector2(i * (tamañoIcono.x + espacioEntreIconos), 0);

            Image img = go.GetComponent<Image>();
            img.sprite = iconoVida;
            img.preserveAspect = true;
            img.raycastTarget = false;

            iconos[i] = img;
        }
    }

    void ActualizarIconos()
    {
        if (iconos == null) return;
        for (int i = 0; i < iconos.Length; i++)
        {
            iconos[i].color = i < vidaActual ? colorVidaLlena : colorVidaPerdida;
        }
    }

    void AnimarIcono(int indice, Color colorFinal)
    {
        if (iconos == null || indice < 0 || indice >= iconos.Length) return;
        StartCoroutine(RutinaIcono(iconos[indice], colorFinal));
    }

    IEnumerator RutinaIcono(Image icono, Color colorFinal)
    {
        RectTransform rt = icono.rectTransform;
        float duracion = 0.35f;
        float t = 0f;

        // Destello: primero blanco brillante y grande, luego vuelve a su tamaño con su color final
        while (t < duracion)
        {
            t += Time.unscaledDeltaTime;
            float p = t / duracion;
            float escala = Mathf.Lerp(1.5f, 1f, p);
            rt.localScale = new Vector3(escala, escala, 1f);
            icono.color = Color.Lerp(Color.white, colorFinal, p);
            yield return null;
        }

        rt.localScale = Vector3.one;
 
 
        icono.color = colorFinal;
    }
}
