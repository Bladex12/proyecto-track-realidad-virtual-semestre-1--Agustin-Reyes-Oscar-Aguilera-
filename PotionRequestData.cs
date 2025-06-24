using UnityEngine;

/// <summary>
/// Contiene toda la información sobre un único tipo de pedido que un cliente puede hacer.
/// Es una clase de datos pura, diseñada para ser configurada en la lista del CustomerManager.
/// </summary>
[System.Serializable]
public class PotionRequestData
{
    [Header("Identificación y Apariencia")]
    [Tooltip("Nombre descriptivo para este pedido, útil para la depuración en el Inspector.")]
    public string requestName = "Nuevo Pedido";

    [Tooltip("El prefab de la poción que el cliente pedirá. Debe tener el script 'Potion'.")]
    public Potion potionPrefab;

    [Tooltip("El icono 2D que se mostrará en la UI del cliente para representar el pedido.")]
    public Sprite potionIcon;

    [Header("Lógica del Juego")]
    [Tooltip("Peso de probabilidad. Un número más alto hace que este pedido sea más probable en comparación con otros.")]
    [Min(0f)] // La probabilidad no puede ser negativa.
    public float probabilityWeight = 1f;

    [Space(10)]

    [Tooltip("Puntos obtenidos al entregar la poción correctamente.")]
    [Min(0)] // El éxito no debería restar puntos.
    public int pointsOnSuccess = 10;
    
    [Tooltip("Puntos perdidos si el cliente se va o se le entrega la poción incorrecta. Debe ser un valor negativo o cero.")]
    public int pointsOnFailure = -5;
}
