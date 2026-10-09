using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Controla el menú principal: Jugar, Opciones (volumen), Créditos y Salir.
// Va en un objeto vacío llamado "MenuManager" dentro de la escena del menú.
public class MenuPrincipal : MonoBehaviour
{
    [Header("Escena del juego (nombre EXACTO)")]
    public string escenaJuego = "SampleScene";

    [Header("Paneles")]
    public GameObject panelPrincipal;
    public GameObject panelOpciones;
    public GameObject panelCreditos;

    [Header("Opciones")]
    public Slider sliderVolumen;

    [Header("Logo flotando (opcional)")]
    public RectTransform logo;
    public float alturaFlotarLogo = 10f;
    public float velocidadFlotarLogo = 1.5f;

    private Vector2 posicionLogo;

    void Start()
    {
        Time.timeScale = 1f; // por si venimos de un juego pausado

        // Cargar el volumen guardado (0.8 si es la primera vez)
        float volumen = PlayerPrefs.GetFloat("volumen", 0.8f);
        AudioListener.volume = volumen;
        if (sliderVolumen != null)
        {
            sliderVolumen.minValue = 0f;
            sliderVolumen.maxValue = 1f;
            sliderVolumen.value = volumen;
            sliderVolumen.onValueChanged.AddListener(CambiarVolumen);
        }

        if (logo != null) posicionLogo = logo.anchoredPosition;
        MostrarPrincipal();
    }

    void Update()
    {
        if (logo != null)
        {
            float y = Mathf.Sin(Time.unscaledTime * velocidadFlotarLogo) * alturaFlotarLogo;
            logo.anchoredPosition = posicionLogo + new Vector2(0f, y);
        }

        // Escape vuelve al panel principal
        if (Input.GetKeyDown(KeyCode.Escape)) MostrarPrincipal();
    }

    // ---------- Botones (se conectan en "On Click" de cada botón) ----------

    public void Jugar()
    {
        SceneManager.LoadScene(escenaJuego);
    }

    public void AbrirOpciones()
    {
        MostrarSolo(panelOpciones);
    }

    public void AbrirCreditos()
    {
        MostrarSolo(panelCreditos);
    }

    public void MostrarPrincipal()
    {
        MostrarSolo(panelPrincipal);
    }

    public void CambiarVolumen(float valor)
    {
        AudioListener.volume = valor;
        PlayerPrefs.SetFloat("volumen", valor);
    }

    public void Salir()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false; // en el editor detiene el Play
#else
        Application.Quit();                   // en el juego exportado cierra la ventana
#endif
    }

    // ---------- Interno ----------

    void MostrarSolo(GameObject panel)
    {
        if (panelPrincipal != null) panelPrincipal.SetActive(panel == panelPrincipal);
        if (panelOpciones != null) panelOpciones.SetActive(panel == panelOpciones);
        if (panelCreditos != null) panelCreditos.SetActive(panel == panelCreditos);
    }
}