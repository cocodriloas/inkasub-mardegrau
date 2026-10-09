using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DialogoUI : MonoBehaviour
{
    // Una única referencia accesible desde cualquier script con "DialogoUI.instancia"
    public static DialogoUI instancia;

    [Header("Referencias de UI (arrastrar en el Inspector)")]
    public GameObject objetoPrompt;
    public GameObject objetoDialogo;
    public Text textoDialogo;

    [Header("Retrato del personaje que habla (abajo a la izquierda)")]
    public Image imagenRetrato;
    [Tooltip("Opcional: texto donde aparece el nombre del personaje")]
    public Text textoNombre;

    [Header("Flecha que aparece cuando la frase terminó de escribirse")]
    public RectTransform indicadorContinuar;
    [Tooltip("Cuántos píxeles sube y baja la flecha")]
    public float reboteFlecha = 6f;

    [Header("Efecto de máquina de escribir")]
    public float letrasPorSegundo = 40f;

    [Header("Animación del retrato")]
    [Tooltip("Qué tan rápido abre y cierra la boca mientras habla")]
    public float velocidadBoca = 8f;
    [Tooltip("Cada cuántos segundos parpadea (mínimo y máximo)")]
    public Vector2 tiempoEntreParpadeos = new Vector2(2f, 5f);
    public float duracionParpadeo = 0.15f;

    public bool EstaEscribiendo { get; private set; }

    private Sprite[] retratoActual;   // 0 = boca cerrada, 1 = abierta, 2 = parpadeo, 3 = abierta
    private Coroutine escritura;
    private string textoCompleto = "";
    private float tiempoBoca;
    private float proximoParpadeo;
    private float finParpadeo;
    private Vector2 posicionFlecha;

    void Awake()
    {
        instancia = this;

        if (objetoPrompt == null) Debug.LogError("DialogoUI: falta asignar 'Objeto Prompt' en el Inspector.");
        if (objetoDialogo == null) Debug.LogError("DialogoUI: falta asignar 'Objeto Dialogo' en el Inspector.");
        if (textoDialogo == null) Debug.LogError("DialogoUI: falta asignar 'Texto Dialogo' en el Inspector.");

        MostrarRetrato(false);
        if (textoNombre != null) textoNombre.gameObject.SetActive(false);
        if (indicadorContinuar != null)
        {
            posicionFlecha = indicadorContinuar.anchoredPosition;
            indicadorContinuar.gameObject.SetActive(false);
        }
    }

    public void MostrarPrompt(bool mostrar)
    {
        if (objetoPrompt != null) objetoPrompt.SetActive(mostrar);
    }

    // Versión simple (la usa el Recolector de bacterias): sin retrato
    public void MostrarDialogo(string mensaje)
    {
        MostrarDialogo(mensaje, null, null);
    }

    // Versión completa (la usan los NPC): con retrato animado y nombre
    public void MostrarDialogo(string mensaje, Sprite[] retrato, string nombre)
    {
        if (objetoDialogo != null) objetoDialogo.SetActive(true);

        retratoActual = retrato;
        bool hayRetrato = retrato != null && retrato.Length > 0 && retrato[0] != null;
        MostrarRetrato(hayRetrato);
        if (hayRetrato) imagenRetrato.sprite = retrato[0];
        ProgramarParpadeo();

        if (textoNombre != null)
        {
            bool hayNombre = !string.IsNullOrEmpty(nombre);
            textoNombre.gameObject.SetActive(hayNombre);
            if (hayNombre) textoNombre.text = nombre;
        }

        if (escritura != null) StopCoroutine(escritura);
        escritura = StartCoroutine(Escribir(mensaje));
    }

    // Si el texto se está escribiendo, lo completa de golpe y devuelve true.
    // Así, presionar E a mitad de una frase la termina en vez de saltarla.
    public bool CompletarSiEscribiendo()
    {
        if (!EstaEscribiendo) return false;
        if (escritura != null) StopCoroutine(escritura);
        textoDialogo.text = textoCompleto;
        EstaEscribiendo = false;
        return true;
    }

    public void OcultarDialogo()
    {
        if (escritura != null) StopCoroutine(escritura);
        EstaEscribiendo = false;
        if (objetoDialogo != null) objetoDialogo.SetActive(false);
        MostrarRetrato(false);
        if (textoNombre != null) textoNombre.gameObject.SetActive(false);
    }

    IEnumerator Escribir(string mensaje)
    {
        EstaEscribiendo = true;
        textoCompleto = mensaje;
        tiempoBoca = 0f;

        if (textoDialogo != null)
        {
            textoDialogo.text = "";
            float espera = 1f / Mathf.Max(1f, letrasPorSegundo);
            for (int i = 1; i <= mensaje.Length; i++)
            {
                textoDialogo.text = mensaje.Substring(0, i);
                // Un "tic" suave cada 2 letras (no en espacios)
                if (i % 2 == 0 && !char.IsWhiteSpace(mensaje[i - 1])) AudioManager.Sonar(Sonido.TextoDialogo);
                yield return new WaitForSecondsRealtime(espera);
            }
        }

        EstaEscribiendo = false;
    }

    void Update()
    {
        ActualizarFlecha();

        if (imagenRetrato == null || !imagenRetrato.gameObject.activeInHierarchy) return;
        if (retratoActual == null || retratoActual.Length == 0) return;

        if (EstaEscribiendo && retratoActual.Length >= 2)
        {
            // Hablando: alterna boca cerrada / abierta
            tiempoBoca += Time.unscaledDeltaTime;
            int paso = (int)(tiempoBoca * velocidadBoca) % 4;
            int indice = (paso == 1) ? 1 : (paso == 3 && retratoActual.Length >= 4 ? 3 : 0);
            imagenRetrato.sprite = retratoActual[indice];
            return;
        }

        // Callado: boca cerrada y parpadea de vez en cuando
        float ahora = Time.unscaledTime;
        if (retratoActual.Length >= 3 && ahora >= proximoParpadeo)
        {
            finParpadeo = ahora + duracionParpadeo;
            ProgramarParpadeo();
        }
        bool parpadeando = retratoActual.Length >= 3 && ahora < finParpadeo;
        imagenRetrato.sprite = retratoActual[parpadeando ? 2 : 0];
    }

    void ActualizarFlecha()
    {
        if (indicadorContinuar == null) return;

        bool dialogoVisible = objetoDialogo != null && objetoDialogo.activeInHierarchy;
        bool mostrar = dialogoVisible && !EstaEscribiendo;
        if (indicadorContinuar.gameObject.activeSelf != mostrar)
            indicadorContinuar.gameObject.SetActive(mostrar);

        if (mostrar)
        {
            float y = Mathf.Sin(Time.unscaledTime * 6f) * reboteFlecha;
            indicadorContinuar.anchoredPosition = posicionFlecha + new Vector2(0f, y);
        }
    }

    void ProgramarParpadeo()
    {
        proximoParpadeo = Time.unscaledTime + Random.Range(tiempoEntreParpadeos.x, tiempoEntreParpadeos.y);
    }

    void MostrarRetrato(bool mostrar)
    {
        if (imagenRetrato != null) imagenRetrato.gameObject.SetActive(mostrar);
    }
}
