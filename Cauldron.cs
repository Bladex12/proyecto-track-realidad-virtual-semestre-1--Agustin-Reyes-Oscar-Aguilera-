using UnityEngine;
using System.Collections.Generic;
using System.Linq; // ¡Muy importante! Necesario para comparar las listas de ingredientes.

// [System.Serializable] permite que podamos ver y editar esta clase en el Inspector de Unity.
[System.Serializable]
public class Recipe
{
    [Tooltip("Nombre de la receta para organizarte.")]
    public string recipeName;
    [Tooltip("Lista de ingredientes necesarios. El orden no importa.")]
    public List<PotionType> requiredIngredients;
    [Tooltip("El prefab de la poción que se generará si la receta es correcta.")]
    public GameObject resultPrefab;
}

[RequireComponent(typeof(AudioSource))]
public class Cauldron : MonoBehaviour
{
    [Header("Configuración de Recetas")]
    [Tooltip("Aquí defines todas las combinaciones posibles.")]
    public List<Recipe> recipes;

    [Header("Efectos y Sonidos")]
    [Tooltip("Partículas que se emiten al añadir cualquier poción.")]
    public ParticleSystem depositParticles;
    [Tooltip("Sonido al añadir cualquier poción.")]
    public AudioClip depositSound;
    [Tooltip("Partículas para una combinación exitosa.")]
    public ParticleSystem successParticles;
    [Tooltip("Sonido para una combinación exitosa.")]
    public AudioClip successSound;
    [Tooltip("Partículas para una combinación fallida.")]
    public ParticleSystem failureParticles;
    [Tooltip("Sonido para una combinación fallida.")]
    public AudioClip failureSound;

    [Header("Punto de Aparición")]
    [Tooltip("Un objeto vacío que marca dónde aparecerá la nueva poción.")]
    public Transform resultSpawnPoint;

    // --- Variables privadas ---
    private List<PotionType> currentIngredients = new List<PotionType>();
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    // Este método se activa cuando un Collider entra en el Trigger del caldero.
    private void OnTriggerEnter(Collider other)
    {
        // 1. Verificamos si el objeto que entró tiene el script Potion.
        Potion potion = other.GetComponent<Potion>();
        if (potion != null)
        {
            // ¡Es una poción!
            ProcessPotion(potion);
        }
    }

    private void ProcessPotion(Potion potion)
    {
        // 2. Añadimos el tipo de la poción a nuestra lista de ingredientes actuales.
        currentIngredients.Add(potion.type);

        // 3. Reproducimos efectos de depósito.
        if (depositParticles != null) depositParticles.Play();
        if (depositSound != null) audioSource.PlayOneShot(depositSound);

        // 4. Destruimos el objeto de la poción que se usó.
        Destroy(potion.gameObject);

        // 5. Comprobamos si la combinación actual coincide con alguna receta.
        CheckForRecipeMatch();
    }

    private void CheckForRecipeMatch()
    {
        Recipe matchedRecipe = null;

        // Iteramos sobre todas las recetas que definimos en el Inspector.
        foreach (var recipe in recipes)
        {
            // Comparamos si la lista de ingredientes de la receta y la del caldero son iguales.
            // Usamos OrderBy para que la comparación no dependa del orden en que se añadieron las pociones.
            if (recipe.requiredIngredients.Count == currentIngredients.Count &&
                recipe.requiredIngredients.OrderBy(p => p).SequenceEqual(currentIngredients.OrderBy(p => p)))
            {
                matchedRecipe = recipe;
                break; // Encontramos una receta, no hace falta seguir buscando.
            }
        }

        if (matchedRecipe != null)
        {
            // ¡ÉXITO! La combinación es correcta.
            ProcessSuccess(matchedRecipe);
        }
        else
        {
            // Si no hay una receta que coincida, comprobamos si la combinación es imposible.
            // Una combinación es fallida si ya no puede formar parte de ninguna receta más larga.
            bool canStillFormARecipe = recipes.Any(r => r.requiredIngredients.Count > currentIngredients.Count &&
                                                 currentIngredients.All(ing => r.requiredIngredients.Contains(ing)));

            if (!canStillFormARecipe && currentIngredients.Count > 0)
            {
                // ¡FALLO! La combinación no es correcta ni puede llegar a serlo.
                ProcessFailure();
            }
            // Si la combinación aún puede ser parte de una receta, no hacemos nada y esperamos más ingredientes.
        }
    }

    private void ProcessSuccess(Recipe recipe)
    {
        Debug.Log("¡Receta correcta! Creando: " + recipe.recipeName);

        // Reproducimos efectos de éxito.
        if (successParticles != null) successParticles.Play();
        if (successSound != null) audioSource.PlayOneShot(successSound);

        // Creamos la nueva poción en el punto de aparición.
        if (recipe.resultPrefab != null && resultSpawnPoint != null)
        {
            Instantiate(recipe.resultPrefab, resultSpawnPoint.position, resultSpawnPoint.rotation);
        }

        // Limpiamos el caldero para la siguiente combinación.
        ClearCauldron();
    }

    private void ProcessFailure()
    {
        Debug.Log("¡Combinación fallida!");

        // Reproducimos efectos de fallo.
        if (failureParticles != null) failureParticles.Play();
        if (failureSound != null) audioSource.PlayOneShot(failureSound);

        // Limpiamos el caldero.
        ClearCauldron();
    }

    // Método para limpiar la lista de ingredientes.
    private void ClearCauldron()
    {
        currentIngredients.Clear();
    }
}