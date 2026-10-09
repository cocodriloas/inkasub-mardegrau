using UnityEngine;

public class EfectoMuerte : MonoBehaviour
{
    public float duracion = 0.3f;
    public float escalaFinal = 2f;

    private SpriteRenderer sr;
    private float tiempoTranscurrido = 0f;
    private Vector3 escalaInicial;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        escalaInicial = transform.localScale;
    }

    void Update()
    {
        tiempoTranscurrido += Time.deltaTime;
        float progreso = tiempoTranscurrido / duracion;

        transform.localScale = Vector3.Lerp(escalaInicial, escalaInicial * escalaFinal, progreso);

        if (sr != null)
        {
            Color c = sr.color;
            c.a = Mathf.Lerp(1f, 0f, progreso);
            sr.color = c;
        }

        if (progreso >= 1f)
        {
            Destroy(gameObject);
        }
    }
}