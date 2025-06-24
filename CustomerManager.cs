using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;

/// <summary>
/// Orquesta la aparición de clientes, gestiona una fila de espera y la puntuación del juego.
/// Es el cerebro central que controla el flujo de clientes.
/// </summary>
public class CustomerManager : MonoBehaviour
{
    [Header("Configuración General")]
    [Tooltip("El prefab del personaje cliente que se creará.")]
    [SerializeField] private GameObject customerPrefab;
    [Tooltip("El punto en el mundo hacia donde los clientes se van al terminar.")]
    [SerializeField] private Transform exitPoint;

    [Header("Configuración de Tiempos")]
    [Tooltip("Tiempo mínimo de espera entre la aparición de clientes.")]
    [SerializeField] [Min(0)] private float minSpawnTime = 10f;
    [Tooltip("Tiempo máximo de espera entre la aparición de clientes.")]
    [SerializeField] [Min(0)] private float maxSpawnTime = 20f;
    [Tooltip("Tiempo total en segundos que un cliente esperará en el mostrador.")]
    [SerializeField] [Range(10, 300)] private float customerWaitTime = 60f;

    [Header("Configuración de la Fila")]
    [Tooltip("El número máximo de clientes que pueden estar en la escena a la vez.")]
    [SerializeField] private int maxCustomers = 5;
    [Tooltip("Los puntos donde los clientes esperarán en orden. El Elemento 0 DEBE ser el mostrador.")]
    [SerializeField] private Transform[] queueSpots;

    [Header("Configuración de Pedidos")]
    [Tooltip("Lista de todos los posibles pedidos que pueden hacer los clientes.")]
    public List<PotionRequestData> possibleRequests;

    [Header("UI y Puntuación")]
    [Tooltip("El componente de texto de TextMeshPro que mostrará la puntuación.")]
    [SerializeField] private TextMeshProUGUI scoreText;
    private int currentScore = 0;

    // --- Variables de Control Internas ---
    private List<CustomerAI> activeCustomers = new List<CustomerAI>();
    private CustomerAI[] occupiedSpots; // Array para saber qué cliente está en qué puesto.
    private bool isGameRunning = true;

    private void Awake()
    {
        // Se asegura de que la lógica de la fila esté lista
        if (queueSpots != null && queueSpots.Length > 0)
        {
            occupiedSpots = new CustomerAI[queueSpots.Length];
        }

        if (!ValidateDependencies())
        {
            isGameRunning = false;
            this.enabled = false;
        }
    }

    private void Start()
    {
        if (!isGameRunning) return;
        UpdateScore(0);
        StartCoroutine(SpawnCustomerRoutine());
    }

    /// <summary>
    /// Método público llamado por un CustomerAI cuando se va (satisfecho o no).
    /// Libera su puesto en la fila y activa el avance.
    /// </summary>
    public void OnCustomerLeft(CustomerAI customer)
    {
        if (customer == null) return;

        activeCustomers.Remove(customer);

        for (int i = 0; i < occupiedSpots.Length; i++)
        {
            if (occupiedSpots[i] == customer)
            {
                occupiedSpots[i] = null; // Libera el puesto
                UpdateQueue(); // Revisa la fila para mover a los demás.
                return; // Sale del bucle una vez encontrado y gestionado.
            }
        }
    }

    /// <summary>
    /// Revisa la fila y mueve a los clientes al siguiente puesto libre.
    /// </summary>
    private void UpdateQueue()
    {
        // Recorre la fila desde el frente (mostrador) hacia atrás.
        for (int i = 0; i < occupiedSpots.Length - 1; i++)
        {
            // Si el puesto actual (i) está libre y el siguiente (i+1) está ocupado...
            if (occupiedSpots[i] == null && occupiedSpots[i + 1] != null)
            {
                // Mueve al cliente del puesto de atrás hacia adelante.
                CustomerAI customerToMove = occupiedSpots[i + 1];
                occupiedSpots[i] = customerToMove;
                occupiedSpots[i + 1] = null; // Libera el puesto de atrás.

                // Le dice al cliente que se mueva a su nuevo puesto.
                // El booleano (i == 0) le indica si ha llegado al mostrador.
                customerToMove.GoToSpot(queueSpots[i], i == 0);
            }
        }
    }

    /// <summary>
    /// Corrutina principal que genera clientes de forma periódica si hay espacio.
    /// </summary>
    private IEnumerator SpawnCustomerRoutine()
    {
        yield return new WaitForSeconds(minSpawnTime); 

        while (isGameRunning)
        {
            // Espera hasta que se cumplan las condiciones para generar un nuevo cliente.
            yield return new WaitUntil(() => activeCustomers.Count < maxCustomers && HasFreeSpot());

            // Espera un tiempo aleatorio antes de generar al siguiente.
            yield return new WaitForSeconds(Random.Range(minSpawnTime, maxSpawnTime));

            int freeSpotIndex = GetFreeSpotIndex();
            
            if (freeSpotIndex != -1)
            {
                SpawnSingleCustomer(freeSpotIndex);
            }
        }
    }

    /// <summary>
    /// Encapsula la lógica para crear una instancia de un cliente.
    /// </summary>
    private void SpawnSingleCustomer(int spotIndex)
    {
        PotionRequestData selectedRequest = PickRandomRequest();
        if (selectedRequest == null) return;

        GameObject customerInstance = Instantiate(customerPrefab, exitPoint.position, exitPoint.rotation);
        if (customerInstance.TryGetComponent<CustomerAI>(out CustomerAI customerAI))
        {
            activeCustomers.Add(customerAI);
            occupiedSpots[spotIndex] = customerAI;
            
            bool isAtDesk = (spotIndex == 0);
            customerAI.Setup(selectedRequest, customerWaitTime, queueSpots[spotIndex], exitPoint, this, isAtDesk);
        }
        else
        {
            Debug.LogError($"El prefab '{customerPrefab.name}' no tiene el script CustomerAI. Destruyendo instancia.", this);
            Destroy(customerInstance);
        }
    }

    private bool HasFreeSpot()
    {
        // Revisa si hay algún puesto nulo (libre) en la lista.
        return occupiedSpots.Any(spot => spot == null);
    }

    private int GetFreeSpotIndex()
    {
        // Busca un puesto libre desde el final de la fila hacia el principio.
        for (int i = queueSpots.Length - 1; i >= 0; i--)
        {
            if (occupiedSpots[i] == null)
            {
                return i;
            }
        }
        return -1; // No hay puestos libres.
    }

    public void UpdateScore(int pointsToAdd)
    {
        currentScore += pointsToAdd;
        if (scoreText != null)
        {
            scoreText.text = $"Puntos: {currentScore}";
        }
    }

    private PotionRequestData PickRandomRequest()
    {
        if (possibleRequests == null || possibleRequests.Count == 0) return null;
        
        float totalWeight = possibleRequests.Sum(request => request.probabilityWeight);
        if (totalWeight <= 0) return null;

        float randomValue = Random.Range(0, totalWeight);
        float cumulativeWeight = 0;
        foreach (var request in possibleRequests)
        {
            if (request.probabilityWeight > 0)
            {
                cumulativeWeight += request.probabilityWeight;
                if (randomValue < cumulativeWeight)
                {
                    return request;
                }
            }
        }
        return null;
    }

    private bool ValidateDependencies()
    {
        bool isValid = true;
        if (customerPrefab == null) { Debug.LogError("¡ERROR! Falta asignar 'Customer Prefab'.", this); isValid = false; }
        if (exitPoint == null) { Debug.LogError("¡ERROR! Falta asignar 'Exit Point'.", this); isValid = false; }
        if (queueSpots == null || queueSpots.Length == 0 || queueSpots.Any(t => t == null)) { Debug.LogError("¡ERROR! 'Queue Spots' está vacía o tiene elementos nulos.", this); isValid = false; }
        if (possibleRequests == null || !possibleRequests.Any()) { Debug.LogError("¡ERROR! 'Possible Requests' está vacía.", this); isValid = false; }
        if (scoreText == null) { Debug.LogWarning("Advertencia: No se ha asignado 'Score Text'.", this); }
        if (minSpawnTime > maxSpawnTime) { Debug.LogWarning("Advertencia: 'Min Spawn Time' es mayor que 'Max Spawn Time'.", this); }
        return isValid;
    }
}
