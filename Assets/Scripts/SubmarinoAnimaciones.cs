using System.Collections;
using UnityEngine;

// Este script es el "director" de las animaciones del submarino.
// Los demás scripts (Salud, Disparo, Movimiento) solo le dicen "reproduce Daño",
// "empieza la Recarga", etc., y este script se encarga de volver a Navegando solo.
public class SubmarinoAnimaciones : MonoBehaviour
{
    [Header("Nombres de los estados en el Animator")]
    [Tooltip("Deben ser EXACTAMENTE iguales a los nombres de las cajitas en la ventana Animator")]
    public string estadoNavegando = "Submarino_Navegando";
    public string estadoRecarga = "Submarino_Recarga";
    public string estadoDaño = "Submarino_Daño";
    public string estadoCuracion = "Submarino_Curacion";
    public string estadoDash = "Submarino_Dash";

    [Header("Cuánto dura cada animación corta (segundos)")]
    public float duracionDaño = 0.5f;
    public float duracionCuracion = 0.8f;
    public float duracionMinimaDash = 0.35f;

    private Animator animator;
    private string estadoBase;              // Navegando normalmente, o Recarga mientras recarga
    private Coroutine animacionTemporal;    // Daño, Curación o Dash que se está reproduciendo

    void Awake()
    {
        animator = GetComponent<Animator>();
        estadoBase = estadoNavegando;
    }

    void Start()
    {
        Reproducir(estadoBase);
    }

    // ---------- Recarga (dura lo que dure la recarga del arma) ----------
    public void IniciarRecarga()
    {
        estadoBase = estadoRecarga;
        if (animacionTemporal == null) Reproducir(estadoBase);
    }

    public void TerminarRecarga()
    {
        estadoBase = estadoNavegando;
        if (animacionTemporal == null) Reproducir(estadoBase);
    }

    // ---------- Animaciones cortas ----------
    public void ReproducirDaño()
    {
        ReproducirTemporal(estadoDaño, duracionDaño);
    }

    public void ReproducirCuracion()
    {
        ReproducirTemporal(estadoCuracion, duracionCuracion);
    }

    public void ReproducirDash(float duracion)
    {
        ReproducirTemporal(estadoDash, Mathf.Max(duracion, duracionMinimaDash));
    }

    // ---------- Funcionamiento interno ----------
    void ReproducirTemporal(string estado, float duracion)
    {
        if (animacionTemporal != null) StopCoroutine(animacionTemporal);
        animacionTemporal = StartCoroutine(RutinaTemporal(estado, duracion));
    }

    IEnumerator RutinaTemporal(string estado, float duracion)
    {
        Reproducir(estado);
        yield return new WaitForSeconds(duracion);
        animacionTemporal = null;
        Reproducir(estadoBase); // vuelve a Navegando (o a Recarga si sigue recargando)
    }

    void Reproducir(string estado)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;

        int hash = Animator.StringToHash(estado);
        if (!animator.HasState(0, hash))
        {
            Debug.LogWarning("SubmarinoAnimaciones: no existe el estado '" + estado +
                             "' en el Animator. Revisa que la cajita tenga ese nombre exacto.");
            return;
        }
        animator.Play(hash, 0, 0f);
    }
}