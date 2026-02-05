using UnityEngine;

public class MissionManager : MonoBehaviour
{
    public bool arMissionCompleted;
    public bool circuitBreakerMissionComplete;
    public bool appliancesMissionCompleted;

    public void OnCircuitBreakerFound()
    {
        // Mission handling will be implemented here.
    }

    public void OnCircuitBreakerQuizCompleted(bool success)
    {
        // Handle quiz completion for circuit breaker.
    }

    public void OnAppliancesMissionFound()
    {
        // Mission handling will be implemented here.
    }

    public void OnAppliancesQuizCompleted(bool success)
    {
        // Handle quiz completion for appliances.
    }

    public void OnPlayerWentOutside()
    {
        // Handle player going outside / evacuation sequence.
    }

    public void OnEvacuationQuizCompleted(bool success)
    {
        // Handle evacuation quiz completion.
    }

    public bool CanStartEvacuation()
    {
        return arMissionCompleted && circuitBreakerMissionComplete && appliancesMissionCompleted;
    }
}
