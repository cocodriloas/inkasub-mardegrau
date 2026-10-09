using System.Collections;
using UnityEngine;

// Sonidos propios del Iceberg de Basura. Va en el MISMO objeto que JefeIceberg.
// El volumen se multiplica por el "Volumen Efectos" de tu AudioManager.
// Si dejas un sonido vacío, se usa el sonido normal de tu AudioManager (o no suena nada).
public class JefeSonidos : MonoBehaviour
{
    [Header("Sonidos (arrastra los .wav de la carpeta Sonidos_Jefe)")]
    public AudioClip risa;
    public AudioClip cambioFase;
    public AudioClip golpeBola;
    public AudioClip golpeDisparo;
    public AudioClip latigazo;
    public AudioClip rugidoMuerte;

    [Header("Volumen de los sonidos del jefe")]
    [Range(0f, 2f)] public float volumen = 1f;

    [Header("Risa")]
    [Tooltip("Probabilidad de que el jefe se ría cuando te golpea con una bola o un disparo")]
    [Range(0f, 1f)] public float probabilidadRisaAlGolpear = 0.35f;
    [Tooltip("Segundos mínimos entre una risa y otra")]
    public float esperaEntreRisas = 6f;

    private AudioSource fuente;
    private float ultimaRisa = -100f;

    void Awake()
    {
        fuente = gameObject.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0f;   // sonido 2D: se oye igual en toda la pantalla
    }

    public void Sonar(AudioClip clip, float multiplicador = 1f)
    {
        if (clip == null || fuente == null) return;
        float efectos = AudioManager.instancia != null ? AudioManager.instancia.volumenEfectos : 0.8f;
        fuente.PlayOneShot(clip, efectos * volumen * multiplicador);
    }

    public void Reir()
    {
        if (risa == null) return;
        ultimaRisa = Time.time;
        Sonar(risa, 1.1f);
    }

    public void ReirEn(float segundos)
    {
        if (risa == null) return;
        StartCoroutine(ReirDespues(segundos));
    }

    // Se ríe a veces cuando te golpea, sin repetirse demasiado
    public void QuizasReir()
    {
        if (risa == null) return;
        if (Time.time - ultimaRisa < esperaEntreRisas) return;
        if (Random.value > probabilidadRisaAlGolpear) return;
        ultimaRisa = Time.time;
        StartCoroutine(ReirDespues(0.35f));
    }

    IEnumerator ReirDespues(float segundos)
    {
        yield return new WaitForSeconds(segundos);
        Reir();
    }
}