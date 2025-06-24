using UnityEngine;

public class CauldronReceiver : MonoBehaviour
{
    [Header("Tags")]
    public string tagPocion = "Pocion";
    public string tagCarneHada = "CarneHada";

    [Header("Efectos")]
    public ParticleSystem efectoParticulasPrefab;  // Cambiado a PREFAB
    public AudioClip sonido;                      // Solo el clip, no AudioSource fijo

    [Header("Resultado")]
    public GameObject prefabResultado;
    public Transform spawnPoint;

    private bool tienePocion = false;
    private bool tieneCarne = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(tagPocion))
        {
            TieneIngrediente(1);
            ActivarEfectos();
            Destroy(other.gameObject);
        }
        else if (other.CompareTag(tagCarneHada))
        {
            TieneIngrediente(2);
            ActivarEfectos();
            Destroy(other.gameObject);
        }

        VerificarCombinacion();
    }

    void TieneIngrediente(int tipo)
    {
        if (tipo == 1) tienePocion = true;
        if (tipo == 2) tieneCarne = true;
    }

    void ActivarEfectos()
    {
        // Instanciar partículas en spawnPoint
        if (efectoParticulasPrefab != null && spawnPoint != null)
        {
            ParticleSystem temp = Instantiate(efectoParticulasPrefab, spawnPoint.position, spawnPoint.rotation);
            temp.Play();
            Destroy(temp.gameObject, temp.main.duration + temp.main.startLifetime.constantMax);
        }

        // Instanciar sonido en ese punto
        if (sonido != null && spawnPoint != null)
        {
            AudioSource.PlayClipAtPoint(sonido, spawnPoint.position);
        }
    }

    void VerificarCombinacion()
    {
        if (tienePocion && tieneCarne)
        {
            if (prefabResultado != null && spawnPoint != null)
            {
                Instantiate(prefabResultado, spawnPoint.position, spawnPoint.rotation);
            }

            // Resetear ingredientes
            tienePocion = false;
            tieneCarne = false;
        }
    }
}
