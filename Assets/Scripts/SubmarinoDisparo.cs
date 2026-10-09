    using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SubmarinoDisparo : MonoBehaviour
{
    [Header("Configuración de disparo")]
    public GameObject prefabProyectil;
    [Tooltip("Solo se usa si no asignas las bocas de abajo")]
    public float distanciaSpawn = 0.7f;

    [Header("Bocas de los cañones (objetos vacíos HIJOS del Submarino)")]
    [Tooltip("Punta del cañón normal (debajo del casco)")]
    public Transform bocaCanon;
    [Tooltip("Punta del cañón de arriba del Doble Cañón")]
    public Transform bocaDobleArriba;
    [Tooltip("Punta del cañón de abajo del Doble Cañón")]
    public Transform bocaDobleAbajo;

    [Header("Munición (bacterias)")]
    public int balasMaximas = 20;
    public float tiempoRecarga = 1.5f;
    [Tooltip("Tecla para recargar a mano antes de quedarse sin balas")]
    public KeyCode teclaRecarga = KeyCode.R;
    private int balasActuales;
    private bool recargando = false;
    private Coroutine rutinaRecarga;

    [Header("UI de munición")]
    public Text textoMunicion;
    public GameObject objetoRecargando;

    [Header("Habilidad: Disparo Doble Disolvente (se desbloquea con 3 bacterias)")]
    public bool habilidadDesbloqueada = false;
    public KeyCode teclaHabilidad = KeyCode.K;
    public float duracionHabilidad = 5f;
    public float cooldownHabilidad = 15f;
    public float separacionDisparoDoble = 0.3f;
    [Tooltip("Durante la habilidad: munición infinita y disparo automático manteniendo el clic")]
    public float cadenciaDisparoDoble = 0.12f;

    [Header("Pieza visual del doble cañón (objeto hijo del submarino)")]
    public DobleCanonVisual visualDobleCanon;

    private bool dobleDisparoActivo = false;
    private float cooldownRestante = 0f;
    private float activoRestante = 0f;
    private float siguienteDisparoAuto = 0f;
    private SubmarinoApuntarMouse apuntado;
    private SubmarinoAnimaciones anim;

    // ---------- Datos para el indicador del HUD ----------
    public bool HabilidadDesbloqueada { get { return habilidadDesbloqueada; } }
    public bool HabilidadActiva { get { return dobleDisparoActivo; } }
    public bool HabilidadLista { get { return habilidadDesbloqueada && !dobleDisparoActivo && cooldownRestante <= 0f; } }
    // 1 = llena, 0 = vacía (mientras está activa baja; mientras recarga sube)
    public float ProgresoHabilidad
    {
        get
        {
            if (!habilidadDesbloqueada) return 0f;
            if (dobleDisparoActivo) return Mathf.Clamp01(activoRestante / Mathf.Max(0.01f, duracionHabilidad));
            return 1f - Mathf.Clamp01(cooldownRestante / Mathf.Max(0.01f, cooldownHabilidad));
        }
    }

    void Start()
    {
        apuntado = GetComponent<SubmarinoApuntarMouse>();
        anim = GetComponent<SubmarinoAnimaciones>();
        if (anim == null) anim = gameObject.AddComponent<SubmarinoAnimaciones>();
        if (visualDobleCanon == null) visualDobleCanon = GetComponentInChildren<DobleCanonVisual>(true);
        if (visualDobleCanon == null)
            Debug.LogWarning("SubmarinoDisparo: no encontré el objeto DobleCanon (con DobleCanonVisual) dentro del Submarino.");

        balasActuales = balasMaximas;
        ActualizarUIMunicion();
        if (objetoRecargando != null) objetoRecargando.SetActive(false);
    }

    void Update()
    {
        if (Time.timeScale == 0f) return; // juego en pausa: no disparar

        if (cooldownRestante > 0) cooldownRestante -= Time.deltaTime;
        if (dobleDisparoActivo) activoRestante -= Time.deltaTime;

        if (dobleDisparoActivo)
        {
            // Munición infinita: basta con mantener el clic
            if (Input.GetMouseButton(0) && Time.time >= siguienteDisparoAuto)
            {
                Disparar();
                siguienteDisparoAuto = Time.time + cadenciaDisparoDoble;
            }
        }
        else if (Input.GetMouseButtonDown(0) && !recargando)
        {
            Disparar();
        }

        // Recargar a mano con R
        if (Input.GetKeyDown(teclaRecarga) && !recargando && !dobleDisparoActivo && balasActuales < balasMaximas)
        {
            rutinaRecarga = StartCoroutine(Recargar());
        }

        // Activar la habilidad con K
        if (habilidadDesbloqueada && Input.GetKeyDown(teclaHabilidad) && !dobleDisparoActivo && cooldownRestante <= 0)
        {
            StartCoroutine(ActivarDobleDisparoTemporal());
        }
    }

    void Disparar()
    {
        if (!dobleDisparoActivo && balasActuales <= 0) return;

        Vector2 direccion = apuntado != null ? apuntado.DireccionApuntada : (Vector2)transform.right;
        Vector2 perpendicular = new Vector2(-direccion.y, direccion.x);

        if (!dobleDisparoActivo)
        {
            Vector3 puntoDeSpawn = bocaCanon != null
                ? PosicionBoca(bocaCanon)
                : transform.position + (Vector3)direccion * distanciaSpawn;

            // Desde la boca del cañón, directo hacia el mouse
            Vector2 dirDesdeBoca = DireccionAlMouse(puntoDeSpawn, direccion);
            Instantiate(prefabProyectil, puntoDeSpawn, Rotacion(dirDesdeBoca));
            balasActuales -= 1;
            AudioManager.Sonar(Sonido.Disparo);
        }
        else
        {
            Vector3 arriba, abajo;
            if (bocaDobleArriba != null && bocaDobleAbajo != null)
            {
                arriba = PosicionBoca(bocaDobleArriba);
                abajo = PosicionBoca(bocaDobleAbajo);
            }
            else
            {
                arriba = transform.position + (Vector3)direccion * distanciaSpawn + (Vector3)perpendicular * separacionDisparoDoble;
                abajo = transform.position + (Vector3)direccion * distanciaSpawn - (Vector3)perpendicular * separacionDisparoDoble;
            }

            // Las dos balas salen paralelas, en la dirección del mouse
            Instantiate(prefabProyectil, arriba, Rotacion(direccion));
            Instantiate(prefabProyectil, abajo, Rotacion(direccion));
            // Durante la habilidad NO se gastan bacterias

            if (visualDobleCanon != null) visualDobleCanon.Disparar();
            AudioManager.Sonar(Sonido.DisparoDoble);
        }

        if (balasActuales < 0) balasActuales = 0;
        ActualizarUIMunicion();

        if (!dobleDisparoActivo && balasActuales <= 0 && !recargando)
        {
            rutinaRecarga = StartCoroutine(Recargar());
        }
    }

    // ---------- Bocas de los cañones ----------

    // Posición real de la boca, aunque el submarino esté volteado mirando a la izquierda
    Vector3 PosicionBoca(Transform boca)
    {
        Vector3 local = boca.localPosition;
        SpriteRenderer srSub = GetComponent<SpriteRenderer>();
        if (srSub != null && srSub.flipX) local.x = -local.x;
        return transform.TransformPoint(local);
    }

    Vector2 DireccionAlMouse(Vector3 desde, Vector2 porDefecto)
    {
        if (Camera.main == null) return porDefecto;
        Vector3 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = (Vector2)(mouse - desde);
        // Si el mouse está casi encima de la boca, usamos la dirección normal
        return dir.sqrMagnitude < 0.25f ? porDefecto : dir.normalized;
    }

    Quaternion Rotacion(Vector2 dir)
    {
        return Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
    }

    // Dibuja las bocas en la Scene para ubicarlas fácil (solo en el editor)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        if (bocaCanon != null) Gizmos.DrawWireSphere(bocaCanon.position, 0.12f);
        Gizmos.color = Color.cyan;
        if (bocaDobleArriba != null) Gizmos.DrawWireSphere(bocaDobleArriba.position, 0.12f);
        if (bocaDobleAbajo != null) Gizmos.DrawWireSphere(bocaDobleAbajo.position, 0.12f);
    }

    private IEnumerator Recargar()
    {
        recargando = true;
        if (objetoRecargando != null) objetoRecargando.SetActive(true);

        if (anim != null) anim.IniciarRecarga();
        AudioManager.Sonar(Sonido.Recarga);

        yield return new WaitForSeconds(tiempoRecarga);

        TerminarRecarga();
    }

    void TerminarRecarga()
    {
        balasActuales = balasMaximas;
        recargando = false;
        rutinaRecarga = null;
        if (objetoRecargando != null) objetoRecargando.SetActive(false);
        if (anim != null) anim.TerminarRecarga();
        ActualizarUIMunicion();
    }

    void ActualizarUIMunicion()
    {
        if (textoMunicion == null) return;
        textoMunicion.text = dobleDisparoActivo ? "MAX" : balasActuales + " / " + balasMaximas;
    }

    private IEnumerator ActivarDobleDisparoTemporal()
    {
        // Si estaba recargando, la habilidad termina la recarga al instante
        if (recargando)
        {
            if (rutinaRecarga != null) StopCoroutine(rutinaRecarga);
            TerminarRecarga();
        }

        dobleDisparoActivo = true;
        activoRestante = duracionHabilidad;
        ActualizarUIMunicion();
        if (visualDobleCanon != null) visualDobleCanon.Mostrar();   // se despliega el cañón

        yield return new WaitForSeconds(duracionHabilidad);

        dobleDisparoActivo = false;
        activoRestante = 0f;
        if (visualDobleCanon != null) visualDobleCanon.Ocultar();   // se repliega
        cooldownRestante = cooldownHabilidad;

        balasActuales = balasMaximas; // sale de la habilidad con el cargador lleno
        ActualizarUIMunicion();
    }
}