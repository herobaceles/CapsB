using UnityEngine;
using UnityEngine.AI;

public class PlayerMovementController : MonoBehaviour
{
    [SerializeField] public MonoBehaviour[] movementScriptsToDisable;
    [SerializeField] public string animatorSpeedParam = "Speed";
    [SerializeField] public bool disableCharacterControllerOnARStart = true;
    [SerializeField] public bool disableNavMeshAgentOnARStart = true;
    [SerializeField] public bool stopRigidbodyOnARStart = true;

    public void StopAllMovement(GameObject player)
    {
        if (player == null) return;

        if (movementScriptsToDisable != null)
        {
            for (int i = 0; i < movementScriptsToDisable.Length; i++)
            {
                if (movementScriptsToDisable[i] != null)
                    movementScriptsToDisable[i].enabled = false;
            }
        }

        if (disableCharacterControllerOnARStart)
        {
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
        }

        if (disableNavMeshAgentOnARStart)
        {
            var agent = player.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.isStopped = true;
                agent.ResetPath();
                agent.enabled = false;
            }
        }

        if (stopRigidbodyOnARStart)
        {
            var rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.Sleep();
            }
        }
    }

    public void ResumeAllMovement(GameObject player)
    {
        if (player == null) return;

        if (movementScriptsToDisable != null)
        {
            for (int i = 0; i < movementScriptsToDisable.Length; i++)
            {
                if (movementScriptsToDisable[i] != null)
                    movementScriptsToDisable[i].enabled = true;
            }
        }

        if (disableCharacterControllerOnARStart)
        {
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = true;
        }

        if (disableNavMeshAgentOnARStart)
        {
            var agent = player.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.enabled = true;
                agent.isStopped = false;
            }
        }

        if (stopRigidbodyOnARStart)
        {
            var rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.WakeUp();
            }
        }
    }
}
