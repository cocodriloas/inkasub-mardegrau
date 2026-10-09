using UnityEngine;
using UnityEngine.UI;

// Anima una imagen de la interfaz (UI) pasando sus cuadros en bucle.
// Ejemplo: la tecla "E" que alterna entre normal y presionada.
[RequireComponent(typeof(Image))]
public class UIAnimada : MonoBehaviour
{
    [Tooltip("Arrastra aquí los cuadros, en orden")]
    public Sprite[] cuadros;

    [Tooltip("Cuadros por segundo")]
    public float velocidad = 2f;

    private Image imagen;
    private float tiempo;

    void Awake()
    {
        imagen = GetComponent<Image>();
    }

    void OnEnable()
    {
        tiempo = 0f;
    }

    void Update()
    {
        if (cuadros == null || cuadros.Length == 0) return;
        tiempo += Time.unscaledDeltaTime;
        int indice = (int)(tiempo * velocidad) % cuadros.Length;
        imagen.sprite = cuadros[indice];
    }
}