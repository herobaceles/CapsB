using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoBagDropZone : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // ...existing code...
    }

    // Detect when a draggable item enters the bag's trigger
    private void OnTriggerEnter(Collider other)
    {
        // Only process objects tagged as 'EmergencyItem'
        if (other.CompareTag("EmergencyItem"))
        {
            ARMissionManager.Instance.OnItemDroppedInBag(other.gameObject);
        }
    }
}
