using UnityEngine;

public class NPCInteraccion : MonoBehaviour
{
    [Header("Nombre que aparece en el diálogo (opcional)")]
    public string nombreNPC = "";

    [Header("Retrato para el diálogo: 0 cerrada, 1 abierta, 2 parpadeo, 3 abierta")]
    public Sprite[] retrato;

    [Header("Diálogos de este NPC, en orden")]
    [TextArea(2, 4)]
    public string[] dialogos;

    [Header("Se pone en true automáticamente cuando el jugador termina de hablar")]
    public bool yaHablo = false;

    [Header("Diagnóstico (déjalo desmarcado si todo funciona)")]
    public bool mostrarMensajesEnConsola = false;

    private bool jugadorCerca = false;
    private bool dialogoAbierto = false;
    private int indiceActual = 0;

    void Start()
    {
        if (DialogoUI.instancia == null)
            Debug.LogError(name + ": no hay ningún DialogoUI en la escena. Revisa el Canvas.");

        if (dialogos == null || dialogos.Length == 0)
            Debug.LogWarning(name + ": este NPC no tiene diálogos escritos.");
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.E)) return;

        if (mostrarMensajesEnConsola)
            Debug.Log(name + ": presionaste E. ¿Jugador cerca? " + jugadorCerca + " | ¿Diálogo abierto? " + dialogoAbierto);

        if (DialogoUI.instancia == null) return;

        if (!dialogoAbierto && jugadorCerca)
        {
            indiceActual = 0;
            dialogoAbierto = true;
            DialogoUI.instancia.MostrarPrompt(false);
            MostrarLineaActual();
        }
        else if (dialogoAbierto)
        {
            // Si la frase aún se está escribiendo, E la completa en vez de saltarla
            if (DialogoUI.instancia.CompletarSiEscribiendo()) return;

            indiceActual++;

            if (indiceActual < dialogos.Length)
            {
                MostrarLineaActual();
            }
            else
            {
                DialogoUI.instancia.OcultarDialogo();
                dialogoAbierto = false;
                yaHablo = true;

                if (jugadorCerca) DialogoUI.instancia.MostrarPrompt(true);
            }
        }
    }

    void MostrarLineaActual()
    {
        if (dialogos == null || dialogos.Length == 0) return;
        bool esLaUltima = (indiceActual == dialogos.Length - 1);
        string sufijo = esLaUltima ? "\n\n[E para salir]" : "\n\n[E para continuar]";
        DialogoUI.instancia.MostrarDialogo(dialogos[indiceActual] + sufijo, retrato, nombreNPC);
    }

    // Reconoce al submarino aunque su Tag haya cambiado o el colisionador esté en un objeto hijo
    bool EsElJugador(Collider2D other)
    {
        if (other.CompareTag("Player")) return true;
        if (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Player")) return true;
        if (other.GetComponentInParent<SubmarinoMovimiento>() != null) return true;
        return false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (mostrarMensajesEnConsola)
            Debug.Log(name + ": algo entró a mi zona -> " + other.name + " (Tag: " + other.tag + ")");

        if (EsElJugador(other))
        {
            jugadorCerca = true;
            if (!dialogoAbierto && DialogoUI.instancia != null) DialogoUI.instancia.MostrarPrompt(true);
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!jugadorCerca && EsElJugador(other))
        {
            jugadorCerca = true;
            if (!dialogoAbierto && DialogoUI.instancia != null) DialogoUI.instancia.MostrarPrompt(true);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (EsElJugador(other))
        {
            jugadorCerca = false;
            // Si el diálogo sigue abierto, NO se cierra al alejarte
            if (!dialogoAbierto && DialogoUI.instancia != null) DialogoUI.instancia.MostrarPrompt(false);
        }
    }
}