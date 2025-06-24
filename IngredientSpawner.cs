using UnityEngine;

public class IngredientSpawner : MonoBehaviour
{
    [Header("Configuración del Ingrediente")]
    [Tooltip("Arrastra aquí el Prefab del ingrediente que quieres que aparezca en este punto.")]
    public GameObject ingredientPrefab;

    [Tooltip("El tiempo en segundos que tardará en reaparecer el ingrediente una vez que se recoge.")]
    public float respawnTime = 5.0f;

    [Header("Punto de Aparición (Opcional)")]
    [Tooltip("Asigna un Transform si quieres que el ingrediente aparezca en una posición específica. Si se deja vacío, usará la posición de este mismo objeto.")]
    public Transform spawnPoint;

    // --- Variables privadas ---
    private GameObject currentIngredientInstance; // Referencia a la poción que está actualmente en el spawner.
    private float timer; // Cronómetro para el tiempo de reaparición.
    private bool isWaitingForRespawn = false; // Estado para saber si estamos esperando para reaparecer.

    void Start()
    {
        // Al iniciar el juego, creamos el primer ingrediente inmediatamente.
        if (ingredientPrefab != null)
        {
            SpawnIngredient();
        }
        else
        {
            Debug.LogError("¡No se ha asignado un Prefab de ingrediente en el Spawner!", this.gameObject);
        }
    }

    void Update()
    {
        // Si la instancia del ingrediente ha sido destruida (porque el jugador la cogió o se rompió)...
        if (currentIngredientInstance == null && !isWaitingForRespawn)
        {
            // ...y no estábamos ya esperando, empezamos la cuenta atrás para reaparecer.
            isWaitingForRespawn = true;
            timer = respawnTime;
        }

        // Si estamos en modo de espera...
        if (isWaitingForRespawn)
        {
            // ...restamos tiempo al cronómetro.
            timer -= Time.deltaTime;

            // Si el tiempo llega a cero...
            if (timer <= 0)
            {
                // ...creamos un nuevo ingrediente y salimos del modo de espera.
                SpawnIngredient();
                isWaitingForRespawn = false;
            }
        }
    }

    /// <summary>
    /// Crea una nueva instancia del ingrediente en la posición y rotación correctas.
    /// </summary>
    private void SpawnIngredient()
    {
        // Determina la posición y rotación para el nuevo ingrediente.
        // Si el usuario definió un spawnPoint, lo usamos. Si no, usamos la posición de este objeto.
        Vector3 spawnPosition = (spawnPoint != null) ? spawnPoint.position : transform.position;
        Quaternion spawnRotation = (spawnPoint != null) ? spawnPoint.rotation : transform.rotation;
        
        // Instanciamos el nuevo ingrediente y guardamos una referencia a él.
        currentIngredientInstance = Instantiate(ingredientPrefab, spawnPosition, spawnRotation);
        
        Debug.Log($"Ha reaparecido {ingredientPrefab.name} en {name}");
    }
}