using UnityEngine;

// Requiere que el GameObject tenga un Rigidbody para las colisiones.
[RequireComponent(typeof(Rigidbody))]
public class Potion : MonoBehaviour
{
    [Tooltip("Define qué tipo de poción es esta instancia.")]
    public PotionType type;

    [Tooltip("Prefab del efecto de partículas al romperse.")]
    public GameObject breakEffectPrefab;

    [Tooltip("Sonido que se reproduce al romperse.")]
    public AudioClip breakSound;

    private AudioSource audioSource;

    void Start()
    {
        // Añadimos un AudioSource en tiempo de ejecución para reproducir el sonido de rotura.
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Comprobamos si la poción ha chocado con un objeto con la etiqueta "Floor".
        // Esto previene que se rompa al chocar con el caldero o la mano del jugador.
        if (collision.gameObject.CompareTag("Floor"))
        {
            BreakPotion();
        }
    }

    private void BreakPotion()
    {
        // Si hay un efecto de partículas definido, lo instanciamos en la posición de la poción.
        if (breakEffectPrefab != null)
        {
            Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);
        }

        // Si hay un sonido de rotura, lo reproducimos en la posición de la poción.
        // AudioSource.PlayClipAtPoint es útil porque crea una fuente de sonido temporal que no se destruye con la poción.
        if (breakSound != null)
        {
            AudioSource.PlayClipAtPoint(breakSound, transform.position);
        }

        // Finalmente, destruimos el objeto de la poción.
        Destroy(gameObject);
    }
}