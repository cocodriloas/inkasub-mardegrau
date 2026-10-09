using UnityEngine;

public class MetaNivel : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.instancia.MostrarVictoria();
        }
    }
}   