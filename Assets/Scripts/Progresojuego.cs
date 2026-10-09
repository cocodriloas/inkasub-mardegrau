using System.Collections.Generic;
using UnityEngine;

// Guarda el progreso mientras el juego está abierto (se mantiene al recargar la escena).
// Lo escriben los cangrejos (PuntoControl) y lo lee RestaurarProgreso al empezar la escena.
// Se borra al volver al menú o al reiniciar desde cero (lo hace GameManager).
public static class ProgresoJuego
{
    public static bool hayPuntoControl = false;
    public static int orden = 0;                    // número del último cangrejo activado
    public static Vector2 posicion;                 // dónde reaparece el submarino
    public static HashSet<string> npcsHablados = new HashSet<string>();

    // Habilidades y bacterias del submarino en el momento de pasar por el cangrejo
    public static int bacterias = 0;
    public static bool dash = false;
    public static bool dobleDisparo = false;

    // Nombres de las bacterias que TODAVÍA estaban en el nivel (las demás ya se recolectaron)
    public static bool guardoBacterias = false;
    public static HashSet<string> bacteriasRestantes = new HashSet<string>();

    public static void GuardarPunto(int ordenPunto, Vector2 posicionReaparicion)
    {
        hayPuntoControl = true;
        orden = ordenPunto;
        posicion = posicionReaparicion;
    }

    public static void Borrar()
    {
        hayPuntoControl = false;
        orden = 0;
        posicion = Vector2.zero;
        npcsHablados.Clear();

        bacterias = 0;
        dash = false;
        dobleDisparo = false;
        guardoBacterias = false;
        bacteriasRestantes.Clear();
    }
}