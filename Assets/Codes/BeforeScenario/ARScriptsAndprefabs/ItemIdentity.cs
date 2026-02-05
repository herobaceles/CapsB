using UnityEngine;

public enum EmergencyItemId
{
    Water,
    FirstAidKit,
    Flashlight,
    Batteries,
    EmergencyWhistle,
    PowerBank,

    // Distractors / wrong items:
    Candy,
    Toy,
    Perfume,
    Laptop
}

public class ItemIdentity : MonoBehaviour
{
    public EmergencyItemId itemId;
}
