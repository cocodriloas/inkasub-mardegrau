using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instancia;

    [Header("Paneles de fin de juego")]
    public GameObject panelDerrota;
    public GameObject panelVictoria;

    [Header("Pausa (se abre con Esc o P)")]
    public GameObject panelPausa;

    [Header("Nombre EXACTO de la escena del menú")]
    public string escenaMenu = "MenuPrincipal";

    [Header("Puntos de control")]
    [Tooltip("Botón 'Continuar' del panel de derrota. Solo aparece si hay un punto de control guardado.")]
    public GameObject botonContinuar;

    private bool pausado = false;
    private bool juegoTerminado = false;

    public bool EstaPausado { get { return pausado; } }

    void Awake()
    {
        instancia = this;
        Time.timeScale = 1f;
        if (panelPausa != null) panelPausa.SetActive(false);
    }

    void Update()
    {
        if (juegoTerminado) return;

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            if (pausado) Reanudar();
            else Pausar();
        }
    }

    public void MostrarDerrota()
    {
        juegoTerminado = true;
        if (panelPausa != null) panelPausa.SetActive(false);

        // "Continuar" solo aparece si ya pasaste por un cangrejo
        if (botonContinuar != null) botonContinuar.SetActive(ProgresoJuego.hayPuntoControl);

        panelDerrota.SetActive(true);
        Time.timeScale = 0f; // pausa el juego (enemigos, física, todo)
        AudioManager.TocarJingle(false);
    }

    public void MostrarVictoria()
    {
        juegoTerminado = true;
        if (panelPausa != null) panelPausa.SetActive(false);
        panelVictoria.SetActive(true);
        Time.timeScale = 0f;
        AudioManager.TocarJingle(true);
    }

    // ---------- Pausa ----------

    public void Pausar()
    {
        if (juegoTerminado) return;
        pausado = true;
        if (panelPausa != null) panelPausa.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Reanudar()
    {
        pausado = false;
        if (panelPausa != null) panelPausa.SetActive(false);
        Time.timeScale = 1f;
    }

    // ---------- Botones ----------

    // Reiniciar desde cero: borra los puntos de control
    public void Reiniciar()
    {
        ProgresoJuego.Borrar();
        Time.timeScale = 1f; // importante: restaurar el tiempo antes de recargar
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Continuar: recarga el nivel y aparece en el último cangrejo (el progreso se conserva)
    public void ContinuarDesdePuntoControl()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void IrAlMenu()
    {
        ProgresoJuego.Borrar();
        Time.timeScale = 1f;
        SceneManager.LoadScene(escenaMenu);
    }
}