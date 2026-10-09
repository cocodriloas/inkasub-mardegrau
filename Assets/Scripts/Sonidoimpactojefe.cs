using UnityEngine;

// JefeAtaques se lo agrega solo a cada bola y disparo del jefe.
// Cuando el proyectil toca al submarino, suena el golpe y a veces el jefe se ríe.
public class SonidoImpactoJefe : MonoBehaviour
{
    [HideInInspector] public JefeSonidos sonidos;
    [HideInInspector] public AudioClip clip;

    private bool usado = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (usado || sonidos == null) return;
        if (!other.CompareTag("Player")) return;

        usado = true;
        sonidos.Sonar(clip);
        sonidos.QuizasReir();
    }
}