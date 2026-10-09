using UnityEngine;

// Ponlo en UN objeto vacío de la escena del nivel (por ejemplo "Progreso").
// Al empezar la escena, si hay un punto de control guardado:
//   - coloca el submarino y la cámara ahí,
//   - devuelve las habilidades (dash y Disparo Doble) y el conteo de bacterias,
//   - recuerda con qué NPC ya hablaste (por ejemplo, el Mero Gigante),
//   - quita las bacterias que ya habías recolectado,
//   - limpia lo que quedó ATRÁS del cangrejo (zonas de combate y enemigos ya vencidos).
// Se ejecuta DESPUÉS de los demás scripts (por eso el fondo con parallax sigue bien a la cámara).
[DefaultExecutionOrder(100)]
public class RestaurarProgreso : MonoBehaviour
{
    [Header("Limpieza de lo que quedó atrás del punto de control (el nivel avanza hacia la derecha)")]
    [Tooltip("Quita las zonas de combate (ZonaEncuentro y GeneradorOleadas) que están a la izquierda del cangrejo")]
    public bool limpiarZonasAtras = true;
    [Tooltip("Quita los enemigos ya activos que están a la izquierda del cangrejo")]
    public bool limpiarEnemigosAtras = true;
    [Tooltip("Solo se limpia lo que está más atrás que esta distancia, en X")]
    public float margenAtras = 3f;

    void Start()
    {
        if (!ProgresoJuego.hayPuntoControl) return;

        Vector2 punto = ProgresoJuego.posicion;

        // 1) Submarino y cámara en el cangrejo
        GameObject jugador = GameObject.FindGameObjectWithTag("Player");
        if (jugador != null)
        {
            jugador.transform.position = new Vector3(punto.x, punto.y, jugador.transform.position.z);

            Rigidbody2D rb = jugador.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.position = punto;
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }

        // La cámara aparece directamente ahí, sin viajar desde el inicio del nivel
        Camera cam = Camera.main;
        if (cam != null)
            cam.transform.position = new Vector3(punto.x, punto.y, cam.transform.position.z);

        // 2) Habilidades y bacterias
        SubmarinoRecolector recolector = FindFirstObjectByType<SubmarinoRecolector>();
        SubmarinoMovimiento movimiento = FindFirstObjectByType<SubmarinoMovimiento>();
        SubmarinoDisparo disparo = FindFirstObjectByType<SubmarinoDisparo>();
        if (recolector != null) recolector.bacteriasRecolectadas = ProgresoJuego.bacterias;
        if (movimiento != null) movimiento.dashDesbloqueado = ProgresoJuego.dash;
        if (disparo != null) disparo.habilidadDesbloqueada = ProgresoJuego.dobleDisparo;

        // Las bacterias que ya recolectaste antes del cangrejo no vuelven a aparecer
        if (ProgresoJuego.guardoBacterias)
        {
            foreach (GameObject bacteria in GameObject.FindGameObjectsWithTag("Bacteria"))
            {
                if (!ProgresoJuego.bacteriasRestantes.Contains(bacteria.name)) Destroy(bacteria);
            }
        }

        // 3) NPC con los que ya hablaste (MuroBloqueoNPC y las zonas se abren solos al verlo)
        foreach (NPCInteraccion npc in FindObjectsByType<NPCInteraccion>(FindObjectsSortMode.None))
        {
            if (ProgresoJuego.npcsHablados.Contains(npc.name)) npc.yaHablo = true;
        }

        // 4) Lo que quedó atrás
        float limite = punto.x - margenAtras;

        if (limpiarZonasAtras)
        {
            foreach (ZonaEncuentro zona in FindObjectsByType<ZonaEncuentro>(FindObjectsSortMode.None))
                if (zona.transform.position.x < limite) Destroy(zona.gameObject);

            foreach (GeneradorOleadas generador in FindObjectsByType<GeneradorOleadas>(FindObjectsSortMode.None))
                if (generador.transform.position.x < limite) Destroy(generador.gameObject);
        }

        if (limpiarEnemigosAtras)
        {
            foreach (GameObject enemigo in GameObject.FindGameObjectsWithTag("Enemigo"))
                if (enemigo.transform.position.x < limite) Destroy(enemigo);
        }
    }
}