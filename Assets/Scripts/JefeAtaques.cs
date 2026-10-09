using System.Collections;
using UnityEngine;

// Ataques del Iceberg de Basura. Va en el MISMO objeto que JefeIceberg.
//   Fase 1: lluvia de bolas de basura.
//   Fase 2: lluvia + abanico de microplásticos.
//   Fase 3: lluvia + abanico + tentáculo (el jefe cambia a unos cuadros donde el tentáculo sale de su propio cuerpo).
// Antes de cada ataque el jefe se tiñe de naranja y suena un aviso, para que el jugador pueda esquivar.
// Los proyectiles usan tu ProyectilEnemigoMovimiento y el daño pasa por tu SubmarinoSalud.
// Si dejas vacíos los dibujos, usa el prefab del proyectil (y formas simples para el tentáculo).
[RequireComponent(typeof(JefeIceberg))]
public class JefeAtaques : MonoBehaviour
{
    [Header("Dibujos (de las tiras Jefe_Bolas y Jefe_DisparoMicro)")]
    [Tooltip("Las 4 bolas de basura. Cada desecho elige una al azar.")]
    public Sprite[] dibujosBolas;
    [Tooltip("Los 2 cuadros del disparo de microplásticos")]
    public Sprite[] dibujosDisparo;

    [Header("Proyectil de respaldo (solo se usa si dejas vacíos los dibujos de bolas o disparo)")]
    public GameObject prefabProyectil;

    [Header("Ritmo")]
    public float pausaInicial = 2f;
    [Tooltip("Segundos de descanso entre ataques en la fase 1, 2 y 3")]
    public float[] pausaEntreAtaquesPorFase = { 3.5f, 2.8f, 2f };
    [Tooltip("Segundos de aviso antes de cada ataque")]
    public float tiempoAviso = 1f;

    [Header("Lluvia de bolas")]
    [Tooltip("Cuántas bolas caen en la fase 1, 2 y 3")]
    public int[] cantidadLluviaPorFase = { 14, 20, 28 };
    [Tooltip("Desde dónde hasta dónde caen, en X, respecto al centro del jefe (negativo = a la izquierda)")]
    public Vector2 rangoLluviaX = new Vector2(-26f, -7f);
    [Tooltip("Altura desde donde empiezan a caer, respecto al centro del jefe")]
    public float alturaLluvia = 9f;
    public float velocidadLluvia = 5f;
    [Tooltip("Qué tan abajo del centro del jefe está el suelo. Las bolas desaparecen al llegar ahí.")]
    public float profundidadSuelo = 10.5f;
    public float segundosEntreDesechos = 0.1f;
    [Tooltip("Tamaño de cada bola en unidades del juego (el submarino mide unas 2)")]
    public float diametroBola = 1.8f;
    [Tooltip("Cuánto giran las bolas mientras caen (grados por segundo)")]
    public float giroBolas = 90f;
    [Tooltip("Pequeño temblor de la cámara mientras caen las bolas")]
    public bool temblorEnLluvia = true;
    [Tooltip("Qué tan fuerte tiembla (0.05 = casi nada, 0.2 = fuerte)")]
    public float intensidadTemblorLluvia = 0.08f;
    [Tooltip("Pequeña salpicadura cuando una bola toca el suelo (usa los cuadros de la explosión del jefe)")]
    public bool salpicaduraEnSuelo = true;
    public float tamanoSalpicadura = 2.5f;
    public float fpsSalpicadura = 12f;

    [Header("Abanico de microplásticos")]
    [Tooltip("Cuántos disparos en la fase 1, 2 y 3")]
    public int[] cantidadAbanicoPorFase = { 3, 5, 7 };
    [Tooltip("Qué tan abierto es el abanico, en grados")]
    public float anguloAbanico = 60f;
    public float velocidadAbanico = 6f;
    [Tooltip("De dónde salen los disparos, respecto al centro del jefe")]
    public Vector2 origenAbanico = new Vector2(-5f, 0f);
    [Tooltip("Ancho de cada disparo en unidades del juego")]
    public float anchoDisparo = 2.6f;
    public float fpsDisparo = 8f;

    [Header("Tentáculo (solo fase 3): el jefe cambia a los 4 cuadros de Jefe_AtaqueTentaculo")]
    [Tooltip("Desmárcalo para que no salga el tentáculo")]
    public bool tentaculoActivo = true;
    [Tooltip("Los 4 cuadros de la hoja Jefe_AtaqueTentaculo, en orden")]
    public Sprite[] cuadrosTentaculo;
    [Tooltip("Dónde está el centro del cuerpo del jefe dentro de cada cuadro, en píxeles desde la izquierda")]
    public float centroCuerpoEnCuadroX = 652f;
    [Tooltip("Dónde sale el tentáculo del cuerpo: píxel X desde la izquierda del cuadro")]
    public float baseTentaculoX = 525f;
    [Tooltip("Altura del tentáculo: píxel Y medido desde ARRIBA del cuadro")]
    public float alturaTentaculoY = 259f;
    [Tooltip("Largo de la zona de daño en cada cuadro, en píxeles (el cuadro 1 no hace daño)")]
    public float[] largoDanoPorCuadro = { 0f, 265f, 430f, 133f };
    [Tooltip("Alto de la zona de daño, en píxeles")]
    public float altoDanoPx = 80f;
    [Tooltip("Cuánto dura cada cuadro, en segundos")]
    public float[] segundosPorCuadro = { 0.35f, 0.15f, 0.55f, 0.3f };

    [Header("Orden de dibujo de lo que lanza el jefe")]
    public int ordenProyectiles = 5;

    private JefeIceberg jefe;
    private Transform jugador;
    private Sprite spriteCuadro;
    private int ultimoAtaque = -1;
    private JefeSonidos sonidos;

    void Start()
    {
        jefe = GetComponent<JefeIceberg>();
        sonidos = GetComponent<JefeSonidos>();

        GameObject j = GameObject.FindGameObjectWithTag("Player");
        if (j != null) jugador = j.transform;

        bool hayBolas = dibujosBolas != null && dibujosBolas.Length > 0;
        bool hayDisparo = dibujosDisparo != null && dibujosDisparo.Length > 0;
        if (prefabProyectil == null && (!hayBolas || !hayDisparo))
            Debug.LogWarning("JefeAtaques: faltan dibujos de bolas o de disparo y no hay Prefab Proyectil de respaldo. Alguno de los ataques no saldrá.");

        spriteCuadro = CrearSpriteCuadro();
        StartCoroutine(RutinaAtaques());
    }

    IEnumerator RutinaAtaques()
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
            int ataque = ElegirAtaque();
            ultimoAtaque = ataque;

            if (ataque == 0) yield return StartCoroutine(AtaqueLluvia());
            else if (ataque == 1) yield return StartCoroutine(AtaqueAbanico());
            else yield return StartCoroutine(AtaqueTentaculo());

            yield return new WaitForSeconds(Mathf.Max(0.5f, ValorFloat(pausaEntreAtaquesPorFase, 3f)));
        }
    }

    // Fase 1: solo lluvia. Fase 2: lluvia o abanico. Fase 3: los tres. Nunca repite el mismo ataque dos veces seguidas.
    int ElegirAtaque()
    {
        int disponibles = Mathf.Clamp(jefe.FaseActual, 1, 3);
        if ((!tentaculoActivo || !HayCuadrosTentaculo()) && disponibles > 2) disponibles = 2;   // sin tentáculo: solo lluvia y abanico
        if (disponibles == 1) return 0;

        int ataque = Random.Range(0, disponibles);
        if (ataque == ultimoAtaque) ataque = (ataque + 1) % disponibles;
        return ataque;
    }

    IEnumerator Avisar()
    {
        jefe.TintarAviso(tiempoAviso);
        AudioManager.Sonar(Sonido.AvisoEnemigo);
        yield return new WaitForSeconds(tiempoAviso);
    }

    // ---------- Ataque 1: lluvia de bolas ----------

    IEnumerator AtaqueLluvia()
    {
        yield return StartCoroutine(Avisar());
        if (!jefe.EstaPeleando) yield break;

        int cantidad = Mathf.Max(1, ValorInt(cantidadLluviaPorFase, 6));
        AudioManager.Sonar(Sonido.DisparoEnemigo);

        // Retumba el techo mientras caen las bolas
        if (temblorEnLluvia) jefe.Sacudir(cantidad * segundosEntreDesechos + 0.6f, intensidadTemblorLluvia);

        for (int i = 0; i < cantidad; i++)
        {
            if (!jefe.EstaPeleando) yield break;

            float x = transform.position.x + Random.Range(rangoLluviaX.x, rangoLluviaX.y);
            float y = transform.position.y + alturaLluvia;
            LanzarBola(new Vector3(x, y, 0f));

            yield return new WaitForSeconds(segundosEntreDesechos);
        }
    }

    void LanzarBola(Vector3 posicion)
    {
        // Cuánto tarda en llegar al suelo: ahí desaparece
        float distancia = alturaLluvia + profundidadSuelo;
        float tiempoHastaSuelo = distancia / Mathf.Max(0.1f, velocidadLluvia);

        if (dibujosBolas != null && dibujosBolas.Length > 0)
        {
            Sprite[] elegido = new Sprite[] { dibujosBolas[Random.Range(0, dibujosBolas.Length)] };
            GameObject bola = CrearProyectilPropio(posicion, Vector2.down, elegido, diametroBola, velocidadLluvia, tiempoHastaSuelo, 0.36f, 0f, false, 0f, 0f, SonidoBola());
            Rigidbody2D rb = bola.GetComponent<Rigidbody2D>();
            if (rb != null) rb.angularVelocity = Random.Range(-giroBolas, giroBolas);

            StartCoroutine(Salpicadura(bola, posicion.x, transform.position.y - profundidadSuelo, tiempoHastaSuelo - 0.05f));
        }
        else
        {
            LanzarConPrefab(posicion, Vector2.down, velocidadLluvia, tiempoHastaSuelo, SonidoBola());
        }
    }

    // Una salpicadura pequeña donde la bola toca el suelo (solo si la bola llegó hasta ahí)
    IEnumerator Salpicadura(GameObject bola, float x, float y, float espera)
    {
        Sprite[] cuadros = jefe.explosion;
        if (!salpicaduraEnSuelo || cuadros == null || cuadros.Length == 0) yield break;

        yield return new WaitForSeconds(Mathf.Max(0f, espera));
        if (bola == null) yield break;   // la bola ya dio en el submarino: no hay salpicadura

        GameObject go = new GameObject("Salpicadura");
        go.transform.position = new Vector3(x, y, 0f);
        SpriteRenderer r = go.AddComponent<SpriteRenderer>();
        r.sortingOrder = ordenProyectiles;
        float ancho = Mathf.Max(0.01f, cuadros[0].bounds.size.x);
        go.transform.localScale = Vector3.one * (tamanoSalpicadura / ancho);

        // Solo los 2 últimos cuadros de la explosión (ya expandida y disolviéndose)
        float pausa = 1f / Mathf.Max(1f, fpsSalpicadura);
        for (int i = Mathf.Max(0, cuadros.Length - 2); i < cuadros.Length; i++)
        {
            r.sprite = cuadros[i];
            yield return new WaitForSeconds(pausa);
        }
        Destroy(go);
    }

    // ---------- Ataque 2: abanico de microplásticos ----------

    IEnumerator AtaqueAbanico()
    {
        yield return StartCoroutine(Avisar());
        if (!jefe.EstaPeleando || jugador == null) yield break;

        int cantidad = Mathf.Max(1, ValorInt(cantidadAbanicoPorFase, 3));
        Vector3 origen = transform.position + new Vector3(origenAbanico.x, origenAbanico.y, 0f);

        Vector2 haciaJugador = ((Vector2)jugador.position - (Vector2)origen).normalized;
        float anguloCentral = Mathf.Atan2(haciaJugador.y, haciaJugador.x) * Mathf.Rad2Deg;

        AudioManager.Sonar(Sonido.DisparoEnemigo);
        for (int i = 0; i < cantidad; i++)
        {
            float reparto = cantidad == 1 ? 0f : (i / (float)(cantidad - 1)) - 0.5f;   // de -0.5 a 0.5
            float grados = anguloCentral + reparto * anguloAbanico;
            float rad = grados * Mathf.Deg2Rad;
            Vector2 direccion = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            if (dibujosDisparo != null && dibujosDisparo.Length > 0)
            {
                // El dibujo viaja hacia la izquierda, así que se gira 180° respecto a la dirección
                CrearProyectilPropio(origen, direccion, dibujosDisparo, anchoDisparo, velocidadAbanico, 4f, 0.2f, -0.2f, true, grados + 180f, fpsDisparo, SonidoDisparo());
            }
            else
            {
                LanzarConPrefab(origen, direccion, velocidadAbanico, 4f, SonidoDisparo());
            }
        }
    }

    // ---------- Ataque 3: tentáculo (sale del cuerpo del jefe) ----------

    bool HayCuadrosTentaculo()
    {
        if (cuadrosTentaculo == null || cuadrosTentaculo.Length < 4) return false;
        for (int i = 0; i < 4; i++) if (cuadrosTentaculo[i] == null) return false;
        return true;
    }

    IEnumerator AtaqueTentaculo()
    {
        if (!HayCuadrosTentaculo())
        {
            Debug.LogWarning("JefeAtaques: faltan los 4 Cuadros Tentaculo. Se salta este ataque.");
            yield break;
        }

        Sprite muestra = cuadrosTentaculo[0];
        float anchoPx = Mathf.Max(1f, muestra.rect.width);
        float altoPx = Mathf.Max(1f, muestra.rect.height);
        float upp = muestra.bounds.size.x / anchoPx;          // unidades del juego por cada píxel del dibujo

        float largoMaximo = 0f;
        foreach (float l in largoDanoPorCuadro) largoMaximo = Mathf.Max(largoMaximo, l);

        // 1) Aviso: una franja roja suave en la línea donde va a golpear
        GameObject franja = CrearCuadro("Aviso_Tentaculo", new Color(1f, 0.2f, 0.2f, 0.25f));
        jefe.TintarAviso(tiempoAviso);
        AudioManager.Sonar(Sonido.AvisoEnemigo);

        float t = 0f;
        while (t < tiempoAviso)
        {
            if (!jefe.EstaPeleando) { Destroy(franja); yield break; }
            franja.transform.position = PixelAMundo(baseTentaculoX, alturaTentaculoY, altoPx, upp);
            franja.transform.localScale = new Vector3(largoMaximo * upp, altoDanoPx * upp, 1f);
            t += Time.deltaTime;
            yield return null;
        }
        Destroy(franja);
        if (!jefe.EstaPeleando) yield break;

        // 2) El jefe cambia a los cuadros del ataque. Un dibujo hijo reemplaza al normal mientras dura.
        GameObject dibujoGo = new GameObject("Jefe_AtaqueTentaculo");
        dibujoGo.transform.SetParent(transform, false);
        dibujoGo.transform.localPosition = new Vector3((anchoPx * 0.5f - centroCuerpoEnCuadroX) * upp, 0f, 0f);
        SpriteRenderer rd = dibujoGo.AddComponent<SpriteRenderer>();
        rd.sprite = cuadrosTentaculo[0];
        rd.sortingOrder = jefe.ordenCapa;
        jefe.Dibujo.enabled = false;

        // La zona de daño es invisible y sigue el largo del tentáculo en cada cuadro (tag "Enemigo")
        GameObject zona = new GameObject("Tentaculo_Dano");
        zona.tag = "Enemigo";
        Rigidbody2D rb = zona.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        BoxCollider2D caja = zona.AddComponent<BoxCollider2D>();
        caja.size = Vector2.one;
        caja.enabled = false;

        try
        {
            for (int i = 0; i < 4; i++)
            {
                rd.sprite = cuadrosTentaculo[i];
                float largoPx = i < largoDanoPorCuadro.Length ? largoDanoPorCuadro[i] : 0f;
                float duracion = i < segundosPorCuadro.Length ? segundosPorCuadro[i] : 0.3f;

                if (i == 2)
                {
                    if (sonidos != null && sonidos.latigazo != null) sonidos.Sonar(sonidos.latigazo);
                    else AudioManager.Sonar(Sonido.Garra);
                    jefe.Sacudir(0.35f, 0.2f);
                }

                float tiempo = 0f;
                while (tiempo < duracion)
                {
                    if (!jefe.EstaPeleando) yield break;
                    rd.color = jefe.Dibujo.color;   // sigue el tinte del jefe (golpes, avisos)
                    ActualizarZona(zona, caja, largoPx, altoPx, upp);
                    tiempo += Time.deltaTime;
                    yield return null;
                }
            }
        }
        finally
        {
            if (dibujoGo != null) Destroy(dibujoGo);
            if (zona != null) Destroy(zona);
            if (jefe != null && jefe.Dibujo != null) jefe.Dibujo.enabled = true;
        }
    }

    // Convierte un punto del dibujo (píxeles desde la izquierda y desde ARRIBA) a posición del juego
    Vector3 PixelAMundo(float px, float pyDesdeArriba, float altoPx, float upp)
    {
        float x = (px - centroCuerpoEnCuadroX) * upp;   // 0 = centro del cuerpo del jefe
        float y = (altoPx * 0.5f - pyDesdeArriba) * upp;
        return transform.position + new Vector3(x, y, 0f);
    }

    void ActualizarZona(GameObject zona, BoxCollider2D caja, float largoPx, float altoPx, float upp)
    {
        if (largoPx <= 1f)
        {
            caja.enabled = false;
            return;
        }

        caja.enabled = true;
        float largo = largoPx * upp;
        float alto = altoDanoPx * upp;
        Vector3 b = PixelAMundo(baseTentaculoX, alturaTentaculoY, altoPx, upp);
        zona.transform.position = b + Vector3.left * (largo * 0.5f);
        zona.transform.localScale = new Vector3(largo, alto, 1f);
    }

    // ---------- Proyectiles ----------

    // Crea un proyectil desde cero con los dibujos del jefe, y le pone TU script ProyectilEnemigoMovimiento
    // (así el movimiento y el daño al submarino funcionan igual que en tus otros enemigos).
    GameObject CrearProyectilPropio(Vector3 posicion, Vector2 direccion, Sprite[] dibujos, float tamano, float velocidad, float tiempoDeVida,
                                    float radioRelativo, float desplazamientoRelativo, bool girar, float gradosDibujo, float fps,
                                    AudioClip sonidoImpacto)
    {
        GameObject go = new GameObject("Proyectil_Jefe");
        go.transform.position = posicion;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = dibujos[0];
        sr.sortingOrder = ordenProyectiles;

        float ancho = Mathf.Max(0.01f, dibujos[0].bounds.size.x);
        go.transform.localScale = Vector3.one * (tamano / ancho);
        if (girar) go.transform.rotation = Quaternion.Euler(0f, 0f, gradosDibujo);

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = ancho * radioRelativo;
        col.offset = new Vector2(ancho * desplazamientoRelativo, 0f);

        ProyectilEnemigoMovimiento mov = go.AddComponent<ProyectilEnemigoMovimiento>();
        mov.velocidad = velocidad;
        mov.tiempoDeVida = tiempoDeVida;
        mov.girarHaciaDireccion = false;
        mov.SetDireccion(direccion);

        if (dibujos.Length > 1) StartCoroutine(AnimarDibujos(sr, dibujos, fps));
        AgregarSonidoImpacto(go, sonidoImpacto);
        return go;
    }

    IEnumerator AnimarDibujos(SpriteRenderer sr, Sprite[] dibujos, float fps)
    {
        float espera = 1f / Mathf.Max(1f, fps);
        int i = 0;
        while (sr != null)
        {
            sr.sprite = dibujos[i % dibujos.Length];
            i++;
            yield return new WaitForSeconds(espera);
        }
    }

    // Respaldo: el prefab ProyectilEnemigo de tus otros enemigos
    void LanzarConPrefab(Vector3 posicion, Vector2 direccion, float velocidad, float tiempoDeVida, AudioClip sonidoImpacto)
    {
        if (prefabProyectil == null) return;

        GameObject proyectil = Instantiate(prefabProyectil, posicion, Quaternion.identity);
        AgregarSonidoImpacto(proyectil, sonidoImpacto);
        ProyectilEnemigoMovimiento mov = proyectil.GetComponent<ProyectilEnemigoMovimiento>();
        if (mov == null) return;

        mov.velocidad = velocidad;
        mov.tiempoDeVida = tiempoDeVida;
        mov.SetDireccion(direccion);
    }

    // ---------- Sonidos de impacto ----------

    AudioClip SonidoBola() { return sonidos != null ? sonidos.golpeBola : null; }
    AudioClip SonidoDisparo() { return sonidos != null ? sonidos.golpeDisparo : null; }

    void AgregarSonidoImpacto(GameObject proyectil, AudioClip clip)
    {
        if (proyectil == null || sonidos == null || clip == null) return;
        SonidoImpactoJefe s = proyectil.AddComponent<SonidoImpactoJefe>();
        s.sonidos = sonidos;
        s.clip = clip;
    }

    // ---------- Ayudas ----------

    GameObject CrearCuadro(string nombre, Color color)
    {
        GameObject go = new GameObject(nombre);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spriteCuadro;
        sr.color = color;
        sr.sortingOrder = -6;
        return go;
    }

    // Un cuadrado blanco de 1x1 con el punto de apoyo en el borde derecho
    Sprite CrearSpriteCuadro()
    {
        Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++)
                tex.SetPixel(x, y, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(1f, 0.5f), 4f);
    }

    float ValorFloat(float[] lista, float porDefecto)
    {
        if (lista == null || lista.Length == 0) return porDefecto;
        return lista[Mathf.Clamp(jefe.FaseActual - 1, 0, lista.Length - 1)];
    }

    int ValorInt(int[] lista, int porDefecto)
    {
        if (lista == null || lista.Length == 0) return porDefecto;
        return lista[Mathf.Clamp(jefe.FaseActual - 1, 0, lista.Length - 1)];
    }
}