using UnityEngine;

// Un ojo débil del Iceberg de Basura. Lo crea el script JefeIceberg automáticamente.
// Abierto (brillante) = las balas le hacen daño al jefe. Cerrado (apagado) = las balas rebotan sin daño.
// Si en JefeIceberg asignaste los dibujos del ojo, usa esos. Si no, dibuja un círculo rojo.
public class JefeOjo : EnemigoSalud
{
    [HideInInspector] public JefeIceberg jefe;
    [HideInInspector] public SpriteRenderer dibujo;
    [HideInInspector] public Vector3 escalaBase = Vector3.one;
    [HideInInspector] public Sprite spriteCerrado;
    [HideInInspector] public Sprite spriteAbriendo;
    [HideInInspector] public Sprite spriteAbierto;

    private bool abierto = false;
    private float destelloHasta = -1f;
    private float abriendoHasta = -1f;

    public bool Abierto { get { return abierto; } }

    public void Abrir()
    {
        if (!abierto) abriendoHasta = Time.time + 0.25f;
        abierto = true;
    }

    public void Cerrar() { abierto = false; }
    public void Destello() { destelloHasta = Time.time + 0.08f; }

    void Update()
    {
        if (dibujo == null) return;

        bool destello = Time.time < destelloHasta;
        float pulso = Mathf.Sin(Time.time * 8f);

        // Con los dibujos del ojo
        if (spriteCerrado != null && spriteAbierto != null)
        {
            if (!abierto) dibujo.sprite = spriteCerrado;
            else if (Time.time < abriendoHasta && spriteAbriendo != null) dibujo.sprite = spriteAbriendo;
            else dibujo.sprite = spriteAbierto;

            dibujo.color = destello ? new Color(1f, 0.6f, 0.6f, 1f) : Color.white;
            float escala = abierto ? 1f + 0.05f * pulso : 0.9f;
            transform.localScale = escalaBase * (destello ? escala * 1.1f : escala);
            return;
        }

        // Sin dibujos: círculo rojo
        if (destello)
        {
            dibujo.color = Color.white;
            return;
        }

        if (abierto)
        {
            float brillo = 0.75f + 0.25f * pulso;
            dibujo.color = new Color(1f, 0.3f * brillo, 0.25f * brillo, 1f);
            transform.localScale = escalaBase * (1f + 0.08f * pulso);
        }
        else
        {
            dibujo.color = new Color(0.35f, 0.08f, 0.08f, 0.65f);
            transform.localScale = escalaBase * 0.6f;
        }
    }

    // Las balas llaman a este método. Se lo pasamos al jefe, que decide si cuenta como daño.
    public override void RecibirDaño(int cantidad)
    {
        if (jefe == null) return;
        jefe.GolpeEnOjo(this, cantidad);
    }
}