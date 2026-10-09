using UnityEngine;

public class SubmarinoRecolector : MonoBehaviour
{
    [Header("Progreso de recolección")]
    public int bacteriasRecolectadas = 0;

    private SubmarinoMovimiento movimiento;
    private SubmarinoDisparo disparo;

    private GameObject bacteriaCercana = null;
    private bool mensajeAbierto = false;
    private bool mostrandoInfoDesbloqueo = false;

    void Start()
    {
        movimiento = GetComponent<SubmarinoMovimiento>();
        disparo = GetComponent<SubmarinoDisparo>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            // Si el mensaje aún se está escribiendo, E lo completa primero
            if ((mensajeAbierto || mostrandoInfoDesbloqueo) && DialogoUI.instancia.CompletarSiEscribiendo())
                return;

            if (mostrandoInfoDesbloqueo)
            {
                DialogoUI.instancia.OcultarDialogo();
                mostrandoInfoDesbloqueo = false;
                return;
            }

            if (bacteriaCercana != null)
            {
                if (!mensajeAbierto)
                {
                    mensajeAbierto = true;
                    DialogoUI.instancia.MostrarPrompt(false);
                    DialogoUI.instancia.MostrarDialogo(MensajeExplicativo() + "\n\n[E para recolectar]");
                }
                else
                {
                    RecolectarBacteria();
                }
            }
        }
    }

    string MensajeExplicativo()
    {
        if (bacteriasRecolectadas == 0)
        {
            return "Esta es Ideonella sakaiensis, una bacteria descubierta en 2016 en Japon, capaz de degradar el PET (un tipo de plastico) usando dos enzimas especiales: PETasa y MHETasa. Cientificos de todo el mundo estan investigando como usarla para descomponer la basura plastica del oceano de forma natural.";
        }
        else
        {
            return "Otra bacteria Ideonella sakaiensis.";
        }
    }

    void RecolectarBacteria()
    {
        DialogoUI.instancia.OcultarDialogo();
        mensajeAbierto = false;

        bacteriasRecolectadas++;
        AudioManager.Sonar(Sonido.RecogerBacteria);
        Debug.Log("Bacterias recolectadas: " + bacteriasRecolectadas);

        Destroy(bacteriaCercana);
        bacteriaCercana = null;

        if (bacteriasRecolectadas == 2)
        {
            movimiento.dashDesbloqueado = true;
            MostrarInfoDesbloqueo("¡Dash desbloqueado! Actívalo con clic derecho del mouse.");
        }
        else if (bacteriasRecolectadas == 3)
        {
            disparo.habilidadDesbloqueada = true;
            MostrarInfoDesbloqueo("¡Disparo Doble Disolvente desbloqueado! Actívalo con la tecla K. Se recarga en 15 segundos.");
        }
    }

    void MostrarInfoDesbloqueo(string mensaje)
    {
        AudioManager.Sonar(Sonido.Desbloqueo);
        mostrandoInfoDesbloqueo = true;
        DialogoUI.instancia.MostrarDialogo(mensaje + "\n\n[E para continuar]");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Bacteria"))
        {
            bacteriaCercana = other.gameObject;
            if (!mensajeAbierto && !mostrandoInfoDesbloqueo)
            {
                DialogoUI.instancia.MostrarPrompt(true);
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject == bacteriaCercana)
        {
            bacteriaCercana = null;
            if (!mensajeAbierto && !mostrandoInfoDesbloqueo)
            {
                DialogoUI.instancia.MostrarPrompt(false);
            }
        }
    }
}
