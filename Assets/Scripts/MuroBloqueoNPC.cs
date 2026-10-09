using UnityEngine;

// Muro que no deja pasar hasta hablar con un NPC.
// Si el objeto tiene el componente Barrera, se abre con animación; si no, desaparece.
public class MuroBloqueoNPC : MonoBehaviour
{
    [Header("NPC con el que hay que hablar antes de pasar")]
    public NPCInteraccion npcRequerido;

    [Header("Mensaje de aviso al chocar")]
    public string mensajeAviso = "Primero debes hablar con la Tortuga.";

    private Barrera barrera;
    private bool abierto = false;
    private float ultimoAviso = -10f;

    void Start()
    {
        barrera = GetComponent<Barrera>();
    }

    void Update()
    {
        if (abierto) return;

        if (npcRequerido != null && npcRequerido.yaHablo)
        {
            abierto = true;
            if (barrera != null) barrera.Abrir();     // se abre con animación
            else gameObject.SetActive(false);         // se abre el paso para siempre
        }
    }

    void OnCollisionEnter2D(Collision2D colision)
    {
        if (abierto) return;
        if (!colision.gameObject.CompareTag("Player")) return;
        if (Time.time - ultimoAviso < 2.5f) return;
        ultimoAviso = Time.time;
        AvisoPantalla.Mostrar(mensajeAviso);
    }
}
