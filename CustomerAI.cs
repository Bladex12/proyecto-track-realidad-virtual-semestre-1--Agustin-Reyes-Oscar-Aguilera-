using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class CustomerAI : MonoBehaviour
{
    private enum State { WalkingToDesk, WaitingForPotion, Leaving }
    private State currentState;

    [Header("Componentes (Asignar en Prefab)")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;

    [Header("UI (Asignar en Prefab)")]
    [SerializeField] private Slider patienceBarSlider;
    [SerializeField] private Image patienceBarFill;
    [SerializeField] private Image requestIconImage;

    [Header("Efectos (Asignar en Prefab)")]
    [SerializeField] private ParticleSystem failureParticles;
    [SerializeField] private AudioClip failureSound;

    // --- Variables de Estado Internas ---
    private CustomerManager customerManager;
    private PotionRequestData currentRequest;
    private Transform exitPoint;
    private float maxWaitTime;
    private float currentWaitTime;
    private int animIDIsWalking;

    // [MEJORA CLAVE] Guardamos la referencia al transform del Canvas de la UI.
    private Transform uiCanvasTransform;

    private void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponent<Animator>();
        animIDIsWalking = Animator.StringToHash("IsWalking");

        // [MEJORA CLAVE] Buscamos el Canvas padre de la barra de paciencia para poder orientarlo.
        if (patienceBarSlider != null)
        {
            uiCanvasTransform = patienceBarSlider.GetComponentInParent<Canvas>().transform;
        }

        ValidateDependencies();
    }

    // Este método se llama después de que todas las cámaras y personajes se hayan movido.
    // Es el lugar perfecto para orientar la UI hacia la cámara.
    private void LateUpdate()
    {
        // Si el canvas de la UI existe y está activo...
        if (uiCanvasTransform != null && uiCanvasTransform.gameObject.activeInHierarchy)
        {
            // ...haz que mire hacia la cámara principal.
            if (Camera.main != null)
            {
                uiCanvasTransform.LookAt(Camera.main.transform);
                // Opcional: Si la UI se ve al revés, descomenta la siguiente línea para girarla 180 grados.
                // uiCanvasTransform.Rotate(0, 180, 0);
            }
        }
    }

    public void Setup(PotionRequestData request, float waitTime, Transform arrivalPoint, Transform leavePoint, CustomerManager manager, bool isAtDesk)
    {
        if (request == null || arrivalPoint == null || leavePoint == null || manager == null)
        {
            Debug.LogError("Setup del cliente recibió parámetros nulos. Autodestruyendo.", this);
            Destroy(gameObject);
            return;
        }

        this.currentRequest = request;
        this.maxWaitTime = waitTime > 0 ? waitTime : 60f;
        this.currentWaitTime = this.maxWaitTime;
        this.customerManager = manager;
        this.exitPoint = leavePoint;

        agent.SetDestination(arrivalPoint.position);

        ChangeState(isAtDesk ? State.WaitingForPotion : State.WalkingToDesk);

        if (requestIconImage != null && request.potionIcon != null)
        {
            requestIconImage.sprite = request.potionIcon;
            requestIconImage.gameObject.SetActive(true);
        }
        
        // La barra se desactiva al inicio por defecto.
        if (patienceBarSlider != null)
        {
            patienceBarSlider.gameObject.SetActive(false);
        }
    }

    public void GoToSpot(Transform newSpot, bool isNowAtDesk)
    {
        agent.SetDestination(newSpot.position);
    }

    private void Update()
    {
        switch (currentState)
        {
            case State.WalkingToDesk:     HandleWalkingToDeskState();      break;
            case State.WaitingForPotion:  HandleWaitingForPotionState();   break;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (currentState != State.WaitingForPotion) return;
        if (other.TryGetComponent<Potion>(out Potion deliveredPotion))
        {
            if (deliveredPotion.type == currentRequest.potionPrefab.type)
            {
                Destroy(other.gameObject);
                Leave(true);
            }
        }
    }

    private void ChangeState(State newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        
        animator.SetBool(animIDIsWalking, (newState == State.WalkingToDesk || newState == State.Leaving));
        
        if (patienceBarSlider != null)
        {
            patienceBarSlider.gameObject.SetActive(newState == State.WaitingForPotion);
        }

        if (newState == State.WaitingForPotion)
        {
            patienceBarSlider.maxValue = maxWaitTime;
            patienceBarSlider.value = maxWaitTime;
        }

        if (newState == State.Leaving)
        {
            if (requestIconImage != null) requestIconImage.gameObject.SetActive(false);
        }
    }

    private void HandleWalkingToDeskState()
    {
        // Esta es la condición clave. Si no se cumple, el cliente se queda "patinando".
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            ChangeState(State.WaitingForPotion);
        }
    }

    private void HandleWaitingForPotionState()
    {
        currentWaitTime -= Time.deltaTime;
        if (patienceBarSlider != null) patienceBarSlider.value = currentWaitTime;

        if (patienceBarFill != null)
        {
            float remainingPercentage = maxWaitTime > 0 ? currentWaitTime / maxWaitTime : 0;
            patienceBarFill.color = Color.Lerp(Color.red, Color.green, remainingPercentage);
        }

        if (currentWaitTime <= 0)
        {
            Leave(false);
        }
    }
    
    private void Leave(bool success)
    {
        if (currentState == State.Leaving) return;

        if (customerManager != null)
        {
            customerManager.OnCustomerLeft(this);
        }

        customerManager.UpdateScore(success ? currentRequest.pointsOnSuccess : currentRequest.pointsOnFailure);

        if (!success)
        {
            if (failureParticles != null) failureParticles.Play();
            if (failureSound != null) AudioSource.PlayClipAtPoint(failureSound, transform.position, 1.0f);
        }

        agent.SetDestination(this.exitPoint.position);
        ChangeState(State.Leaving);
        Destroy(gameObject, 15f);
    }

    private void ValidateDependencies()
    {
        if (patienceBarSlider == null) Debug.LogError("Dependencia no asignada en el prefab: 'Patience Bar Slider'.", this);
        if (patienceBarFill == null) Debug.LogError("Dependencia no asignada en el prefab: 'Patience Bar Fill'.", this);
        if (requestIconImage == null) Debug.LogError("Dependencia no asignada en el prefab: 'Request Icon Image'.", this);
    }
}
