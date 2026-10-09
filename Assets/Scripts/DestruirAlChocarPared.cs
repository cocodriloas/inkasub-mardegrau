using UnityEngine;

// Se pone en los prefabs de las balas (Proyectil y ProyectilEnemigo).
// Si la bala toca una pared (cualquier colisionador sólido que no sea
// el submarino, un enemigo u otra cosa de la lista), la bala desaparece.
public class DestruirAlChocarPared : MonoBehaviour
{
    [Tooltip("Etiquetas que NO cuentan como pared (la bala las atraviesa o las maneja su propio script)")]
    public string[] etiquetasIgnoradas = { "Player", "Enemigo", "Bacteria" };

    [Tooltip("Pequeño efecto al chocar (opcional, por ejemplo burbujas)")]
    public GameObject efectoImpacto;

    void OnTriggerEnter2D(Collider2D other)
    {
        Revisar(other);
    }

    void OnCollisionEnter2D(Collision2D colision)
    {
        Revisar(colision.collider);
    }

    void Revisar(Collider2D other)
    {
        if (other.isTrigger) return; // zonas, NPC, burbujas, etc. no son paredes

        foreach (string etiqueta in etiquetasIgnoradas)
        {
            if (other.CompareTag(etiqueta)) return;
        }
        if (other.GetComponentInParent<SubmarinoMovimiento>() != null) return;
        if (other.GetComponentInParent<EnemigoSalud>() != null) return;

        if (efectoImpacto != null) Instantiate(efectoImpacto, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}
