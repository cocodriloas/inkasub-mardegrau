using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// El Iceberg de Basura (jefe final).
// - Aparece cuando se llama a IniciarPelea() (desde SalaJefeDisparador > Al Terminar Transicion).
// - Tiene 4 ojos débiles en su lado izquierdo. Solo los ojos ABIERTOS (brillantes) le hacen daño.
//   Los ojos cerrados y el cuerpo detienen las balas sin hacer daño.
// - Cambia de fase al 60% y al 30% de vida (los ataques y los refuerzos los manejan JefeAtaques y JefeRefuerzos).
// - Al llegar a 0 se desarma y, unos segundos después, aparece la pantalla de victoria.
// Usa tus sistemas: AudioManager, AvisoPantalla y GameManager.
[RequireComponent(typeof(SpriteRenderer))]
public class JefeIceberg : EnemigoSalud
{
    private enum Estado { Oculto, Apareciendo, Peleando, Muerto }

    [Header("Dibujos del jefe (arrastra los cuadros en orden)")]
    [Tooltip("Un dibujo quieto para probar antes de tener las animaciones")]
    public Sprite spriteInicial;
    public Sprite[] aparicion;
    public Sprite[] idleFase1;
    public Sprite[] idleFase2;
    public Sprite[] idleFase3;
    public Sprite[] derrota;

    [Header("Velocidad de las animaciones (cuadros por segundo)")]
    public float fpsAparicion = 4f;
    [Tooltip("Una velocidad por fase: fase 1, fase 2, fase 3")]
    public float[] fpsIdlePorFase = { 3.5f, 4.5f, 7.5f };
    public float fpsDerrota = 4f;

    [Header("Flotar (sube y baja suave)")]
    public float amplitudFlotar = 0.25f;
    public float velocidadFlotar = 1.2f;

    [Header("Cuerpo: detiene las balas y el submarino no lo atraviesa")]
    public Vector2 tamanoCuerpo = new Vector2(8f, 16f);
    public Vector2 desplazamientoCuerpo = Vector2.zero;
    [Tooltip("Daño que recibe si le disparan al cuerpo (0 = ninguno, solo los ojos hacen daño)")]
    public int danoAlCuerpo = 0;
    [Tooltip("Debe ser mayor que el fondo de la sala (-8) y menor que las rocas (-5)")]
    public int ordenCapa = -7;

    [Header("Ojos débiles (posición respecto al centro del jefe)")]
    public Vector2[] posicionesOjos = new Vector2[]
    {
        new Vector2(-4f, 5.5f),
        new Vector2(-4f, 1.8f),
        new Vector2(-4f, -2f),
        new Vector2(-4f, -5.8f)
    };
    public float radioOjo = 1.3f;
    [Tooltip("Cuántos ojos están abiertos a la vez en la fase 1, 2 y 3")]
    public int[] ojosAbiertosPorFase = { 2, 2, 3 };
    [Tooltip("Cada cuántos segundos cambian de lugar los ojos abiertos en la fase 1, 2 y 3")]
    public float[] segundosRotacionPorFase = { 5f, 4f, 3f };

    [Header("Dibujos de los ojos (opcionales; si los dejas vacíos se dibujan círculos rojos)")]
    [Tooltip("Ojo dormido (cuadro 1 de la tira Jefe_Ojos)")]
    public Sprite spriteOjoCerrado;
    [Tooltip("Ojo abriéndose (cuadro 2)")]
    public Sprite spriteOjoAbriendo;
    [Tooltip("Ojo abierto y brillante (cuadro 3)")]
    public Sprite spriteOjoAbierto;

    [Header("Explosión final (opcional: cuadros de la tira Jefe_Explosion)")]
    public Sprite[] explosion;
    [Tooltip("Ancho de la explosión en unidades del juego")]
    public float tamanoExplosion = 18f;
    public float fpsExplosion = 6f;
    [Tooltip("Segundos después de empezar a caer antes de que explote")]
    public float demoraExplosion = 0.8f;

    [Header("Fases (porcentaje de vida que le queda)")]
    [Range(0f, 1f)] public float umbralFase2 = 0.6f;
    [Range(0f, 1f)] public float umbralFase3 = 0.3f;

    [Header("Barra de vida (opcional: un Slider del Canvas)")]
    public Slider barraVida;

    [Header("Golpes y victoria")]
    [Tooltip("Cada cuántos golpes en un ojo el jefe se tiñe de rojo un momento")]
    public int golpesPorDestello = 5;
    public Color colorGolpe = new Color(1f, 0.45f, 0.45f, 1f);
    [Tooltip("Color con el que el jefe avisa que va a atacar")]
    public Color colorAviso = new Color(1f, 0.75f, 0.35f, 1f);
    public float segundosHastaVictoria = 4f;
    public string textoDerrotado = "¡Iceberg de Basura derrotado!";

    // ---------- Estado interno ----------
    private Estado estado = Estado.Oculto;
    private int fase = 1;
    private int vida;
    private SpriteRenderer sr;
    private BoxCollider2D cuerpo;
    private List<JefeOjo> ojos = new List<JefeOjo>();
    private Coroutine rotacionOjos;
    private Vector3 posicionBase;
    private float tiempoIdle;
    private int golpesDesdeDestello;
    private int tintes;
    private bool temblando;
    private JefeSonidos sonidos;

    // Para que JefeAtaques sepa qué está pasando
    public bool EstaPeleando { get { return estado == Estado.Peleando; } }
    public bool EstaMuerto { get { return estado == Estado.Muerto; } }
    public int FaseActual { get { return fase; } }

    // Para que JefeAtaques pueda esconder el dibujo normal mientras muestra el ataque con tentáculo
    public SpriteRenderer Dibujo { get { return sr; } }

    // Hace temblar la cámara (por ejemplo, en el golpe del tentáculo)
    public void Sacudir(float duracion, float intensidad)
    {
        StartCoroutine(Temblor(duracion, intensidad));
    }

    // JefeAtaques lo llama un momento antes de cada ataque
    public void TintarAviso(float duracion)
    {
        StartCoroutine(TintarColor(colorAviso, duracion));
    }

    // Se ejecuta al agregar el script: pone 50 de vida (50 balas)
    void Reset()
    {
        vidaMaxima = 50;
        probabilidadDrop = 0f;
    }

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        sonidos = GetComponent<JefeSonidos>();
        sr.sortingOrder = ordenCapa;
        if (spriteInicial != null) sr.sprite = spriteInicial;
        sr.enabled = false;   // invisible hasta que empiece la pelea

        gameObject.tag = "Enemigo";

        cuerpo = GetComponent<BoxCollider2D>();
        if (cuerpo == null) cuerpo = gameObject.AddComponent<BoxCollider2D>();
        cuerpo.size = tamanoCuerpo;
        cuerpo.offset = desplazamientoCuerpo;
        cuerpo.enabled = false;

        if (barraVida != null) barraVida.gameObject.SetActive(false);
    }

    // ---------- Para llamar desde fuera ----------

    // Conéctalo en SalaJefeDisparador > Al Terminar Transicion
    public void IniciarPelea()
    {
        if (estado != Estado.Oculto) return;
        StartCoroutine(RutinaAparicion());
    }

    [ContextMenu("Probar pelea ahora")]
    void ProbarPelea()
    {
        if (!Application.isPlaying)
        {
            Debug.Log("JefeIceberg: presiona Play primero y luego usa 'Probar pelea ahora'.");
            return;
        }
        IniciarPelea();
    }

    // Lo llaman los ojos cuando una bala los toca
    public void GolpeEnOjo(JefeOjo ojo, int cantidad)
    {
        if (estado != Estado.Peleando) return;
        if (ojo == null || !ojo.Abierto) return;   // ojo cerrado: la bala se pierde sin daño
        ojo.Destello();
        AplicarDano(cantidad);
    }

    // Lo llaman las balas cuando tocan el cuerpo
    public override void RecibirDaño(int cantidad)
    {
        if (estado != Estado.Peleando) return;
        if (danoAlCuerpo > 0) AplicarDano(danoAlCuerpo);
    }

    // ---------- Aparición ----------

    IEnumerator RutinaAparicion()
    {
        estado = Estado.Apareciendo;
        posicionBase = transform.position;
        vida = vidaMaxima;
        fase = 1;

        if (sr.sprite == null && !HayDibujos())
            Debug.LogWarning("JefeIceberg: no hay ningún dibujo asignado (Sprite Inicial o las animaciones).");

        sr.color = Color.white;
        sr.enabled = true;
        AudioManager.Sonar(Sonido.AvisoEnemigo);
        StartCoroutine(Temblor(1.5f, 0.15f));

        yield return StartCoroutine(Reproducir(aparicion, fpsAparicion));

        cuerpo.enabled = true;
        CrearOjos();
        if (sonidos != null) sonidos.Reir();   // se ríe al aparecer

        if (barraVida != null)
        {
            barraVida.gameObject.SetActive(true);
            barraVida.value = 1f;
        }

        estado = Estado.Peleando;
        tiempoIdle = 0f;
        rotacionOjos = StartCoroutine(RotarOjos());
    }

    bool HayDibujos()
    {
        return (aparicion != null && aparicion.Length > 0) || (idleFase1 != null && idleFase1.Length > 0);
    }

    // ---------- Animación ----------

    void Update()
    {
        if (estado == Estado.Oculto || estado == Estado.Muerto) return;

        // Flotar
        transform.position = posicionBase + Vector3.up * Mathf.Sin(Time.time * velocidadFlotar) * amplitudFlotar;

        if (estado != Estado.Peleando) return;

        Sprite[] ciclo = CicloIdle();
        if (ciclo == null || ciclo.Length == 0) return;

        float fps = Mathf.Max(0.5f, ValorFloat(fpsIdlePorFase, 4f));
        tiempoIdle += Time.deltaTime;
        sr.sprite = ciclo[(int)(tiempoIdle * fps) % ciclo.Length];
    }

    Sprite[] CicloIdle()
    {
        if (fase >= 3 && idleFase3 != null && idleFase3.Length > 0) return idleFase3;
        if (fase >= 2 && idleFase2 != null && idleFase2.Length > 0) return idleFase2;
        return idleFase1;
    }

    IEnumerator Reproducir(Sprite[] cuadros, float fps)
    {
        if (cuadros == null || cuadros.Length == 0) yield break;
        float espera = 1f / Mathf.Max(0.5f, fps);
        for (int i = 0; i < cuadros.Length; i++)
        {
            sr.sprite = cuadros[i];
            yield return new WaitForSeconds(espera);
        }
    }

    float ValorFloat(float[] lista, float porDefecto)
    {
        if (lista == null || lista.Length == 0) return porDefecto;
        return lista[Mathf.Clamp(fase - 1, 0, lista.Length - 1)];
    }

    int ValorInt(int[] lista, int porDefecto)
    {
        if (lista == null || lista.Length == 0) return porDefecto;
        return lista[Mathf.Clamp(fase - 1, 0, lista.Length - 1)];
    }

    // ---------- Ojos ----------

    void CrearOjos()
    {
        Sprite dibujoOjo = spriteOjoCerrado != null ? spriteOjoCerrado : CrearSpriteCirculo();
        float diametro = Mathf.Max(0.01f, dibujoOjo.bounds.size.x);
        float escalaMundo = Mathf.Max(0.01f, transform.lossyScale.x);
        float escala = (radioOjo * 2f) / (diametro * escalaMundo);

        ojos.Clear();
        for (int i = 0; i < posicionesOjos.Length; i++)
        {
            GameObject go = new GameObject("Ojo_" + (i + 1));
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(posicionesOjos[i].x, posicionesOjos[i].y, 0f);
            go.transform.localScale = Vector3.one * escala;
            go.tag = "Enemigo";

            SpriteRenderer dibujo = go.AddComponent<SpriteRenderer>();
            dibujo.sprite = dibujoOjo;
            dibujo.sortingOrder = ordenCapa + 1;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = diametro * 0.5f;

            JefeOjo ojo = go.AddComponent<JefeOjo>();
            ojo.jefe = this;
            ojo.dibujo = dibujo;
            ojo.escalaBase = go.transform.localScale;
            ojo.spriteCerrado = spriteOjoCerrado;
            ojo.spriteAbriendo = spriteOjoAbriendo;
            ojo.spriteAbierto = spriteOjoAbierto;
            ojo.Cerrar();
            ojos.Add(ojo);
        }
    }

    // Círculo rojo de relleno mientras no tengas el dibujo final del ojo
    Sprite CrearSpriteCirculo()
    {
        int n = 32;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        float centro = (n - 1) / 2f;
        float radio = n / 2f - 1f;

        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float d = Mathf.Sqrt((x - centro) * (x - centro) + (y - centro) * (y - centro));
                if (d > radio) { tex.SetPixel(x, y, new Color(0f, 0f, 0f, 0f)); continue; }
                float brillo = d < radio * 0.45f ? 1f : (d < radio * 0.8f ? 0.75f : 0.5f);
                tex.SetPixel(x, y, new Color(brillo, brillo, brillo, 1f));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
    }

    IEnumerator RotarOjos()
    {
        while (estado == Estado.Peleando)
        {
            AbrirOjosAlAzar();
            yield return new WaitForSeconds(Mathf.Max(0.5f, ValorFloat(segundosRotacionPorFase, 4f)));
        }
    }

    void AbrirOjosAlAzar()
    {
        if (ojos.Count == 0) return;

        List<int> indices = new List<int>();
        for (int i = 0; i < ojos.Count; i++) indices.Add(i);
        for (int i = indices.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int guardado = indices[i];
            indices[i] = indices[j];
            indices[j] = guardado;
        }

        int cuantos = Mathf.Clamp(ValorInt(ojosAbiertosPorFase, 2), 1, ojos.Count);
        for (int k = 0; k < indices.Count; k++)
        {
            JefeOjo ojo = ojos[indices[k]];
            if (ojo == null) continue;
            if (k < cuantos) ojo.Abrir(); else ojo.Cerrar();
        }
    }

    // ---------- Daño y fases ----------

    void AplicarDano(int cantidad)
    {
        if (estado != Estado.Peleando) return;

        vida = Mathf.Max(0, vida - cantidad);
        AudioManager.Sonar(Sonido.EnemigoGolpeado);
        if (barraVida != null) barraVida.value = (float)vida / vidaMaxima;

        golpesDesdeDestello++;
        if (golpesDesdeDestello >= golpesPorDestello)
        {
            golpesDesdeDestello = 0;
            StartCoroutine(TintarColor(colorGolpe, 0.1f));
        }

        if (vida <= 0)
        {
            StartCoroutine(RutinaDerrota());
            return;
        }

        float porcentaje = (float)vida / vidaMaxima;
        if (fase == 1 && porcentaje <= umbralFase2) CambiarFase(2);
        else if (fase == 2 && porcentaje <= umbralFase3) CambiarFase(3);
    }

    void CambiarFase(int nuevaFase)
    {
        fase = nuevaFase;
        tiempoIdle = 0f;

        AvisoPantalla.Mostrar(nuevaFase == 3 ? "¡Fase final!" : "¡Fase " + nuevaFase + "!", 2f);
        if (sonidos != null && sonidos.cambioFase != null)
        {
            sonidos.Sonar(sonidos.cambioFase);
            sonidos.ReirEn(1.6f);
        }
        else
        {
            AudioManager.Sonar(Sonido.AvisoEnemigo);
        }
        StartCoroutine(Temblor(0.6f, 0.3f));
        StartCoroutine(TintarColor(colorGolpe, 0.25f));

        // Los ojos cambian de ritmo enseguida
        if (rotacionOjos != null) StopCoroutine(rotacionOjos);
        rotacionOjos = StartCoroutine(RotarOjos());
    }

    // ---------- Derrota y victoria ----------

    IEnumerator RutinaDerrota()
    {
        estado = Estado.Muerto;

        if (rotacionOjos != null) StopCoroutine(rotacionOjos);
        foreach (JefeOjo ojo in ojos)
        {
            if (ojo != null) Destroy(ojo.gameObject);
        }
        ojos.Clear();

        cuerpo.enabled = false;
        if (barraVida != null) barraVida.gameObject.SetActive(false);

        if (sonidos != null && sonidos.rugidoMuerte != null) sonidos.Sonar(sonidos.rugidoMuerte, 1.1f);
        else AudioManager.Sonar(Sonido.EnemigoMuere);
        AvisoPantalla.Mostrar(textoDerrotado, 2.5f);
        MatarEnemigosRestantes();

        StartCoroutine(RutinaExplosion());
        StartCoroutine(Temblor(2f, 0.35f));
        StartCoroutine(TintarColor(colorGolpe, 0.2f));
        StartCoroutine(EsperarYGanar());

        if (derrota != null && derrota.Length > 0)
            yield return StartCoroutine(Reproducir(derrota, fpsDerrota));
        else
            yield return new WaitForSeconds(1.5f);

        sr.enabled = false;
    }

    IEnumerator RutinaExplosion()
    {
        if (explosion == null || explosion.Length == 0) yield break;

        yield return new WaitForSeconds(demoraExplosion);

        GameObject go = new GameObject("Explosion_Jefe");
        go.transform.position = transform.position;
        SpriteRenderer r = go.AddComponent<SpriteRenderer>();
        r.sortingOrder = ordenCapa + 3;
        float ancho = Mathf.Max(0.01f, explosion[0].bounds.size.x);
        go.transform.localScale = Vector3.one * (tamanoExplosion / ancho);

        float espera = 1f / Mathf.Max(0.5f, fpsExplosion);
        for (int i = 0; i < explosion.Length; i++)
        {
            r.sprite = explosion[i];
            yield return new WaitForSeconds(espera);
        }
        Destroy(go);
    }

    IEnumerator EsperarYGanar()
    {
        yield return new WaitForSeconds(segundosHastaVictoria);
        if (GameManager.instancia != null) GameManager.instancia.MostrarVictoria();
    }

    void MatarEnemigosRestantes()
    {
        GameObject[] enemigos = GameObject.FindGameObjectsWithTag("Enemigo");
        foreach (GameObject e in enemigos)
        {
            if (e == null || e == gameObject || e.transform.IsChildOf(transform)) continue;
            EnemigoSalud salud = e.GetComponent<EnemigoSalud>();
            if (salud != null) salud.RecibirDaño(9999);
        }
    }

    // ---------- Efectos ----------

    IEnumerator TintarColor(Color color, float duracion)
    {
        tintes++;
        sr.color = color;
        yield return new WaitForSeconds(duracion);
        tintes--;
        if (tintes <= 0)
        {
            tintes = 0;
            sr.color = Color.white;
        }
    }

    // La cámara de la sala del jefe está quieta, así que podemos moverla un poquito
    IEnumerator Temblor(float duracion, float intensidad)
    {
        if (temblando) yield break;
        Camera cam = Camera.main;
        if (cam == null) yield break;

        temblando = true;
        Vector3 posicionOriginal = cam.transform.position;
        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            float fuerza = 1f - t / duracion;
            Vector2 desvio = Random.insideUnitCircle * intensidad * fuerza;
            cam.transform.position = posicionOriginal + new Vector3(desvio.x, desvio.y, 0f);
            yield return null;
        }
        cam.transform.position = posicionOriginal;
        temblando = false;
    }

    // Dibuja en la ventana Scene el cuerpo (amarillo) y los ojos (rojo)
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position + new Vector3(desplazamientoCuerpo.x, desplazamientoCuerpo.y, 0f), new Vector3(tamanoCuerpo.x, tamanoCuerpo.y, 0f));

        Gizmos.color = Color.red;
        if (posicionesOjos != null)
        {
            foreach (Vector2 p in posicionesOjos)
                Gizmos.DrawWireSphere(transform.TransformPoint(new Vector3(p.x, p.y, 0f)), radioOjo);
        }
    }
}