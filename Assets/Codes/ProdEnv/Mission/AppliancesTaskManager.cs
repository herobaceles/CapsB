using UnityEngine;

public class AppliancesTaskManager : MonoBehaviour
{
    public static AppliancesTaskManager Instance { get; private set; }

    private const int NumTasks = 3;
    private bool[] taskCompleted = new bool[NumTasks];
    private int tasksDone = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Optionally, show intro dialogue
        var lines = new System.Collections.Generic.List<ProdDialogueLine>
        {
            new ProdDialogueLine("Professor Lingap", "Now lets proceed to securing your appliances. Complete all 3 AR tasks!")
        };
        ProdDialogueManager.Instance.ShowDialogueSequence(lines);
    }

    // Called by ApplianceARTrigger when player enters a trigger
    public void OnARTriggerEntered(int triggerId)
    {
        if (triggerId < 0 || triggerId >= NumTasks || taskCompleted[triggerId]) return;
        StartCoroutine(StartARTask(triggerId));
    }

    // Simulate AR task flow (replace with your AR logic)
    private System.Collections.IEnumerator StartARTask(int triggerId)
    {
        // Show AR UI, lock movement, etc.
        Debug.Log($"Starting AR Task {triggerId + 1}");
        // TODO: Activate AR session and task logic here

        // For demo, wait 2 seconds to simulate AR task
        yield return new WaitForSeconds(2f);

        OnARTaskCompleted(triggerId);
    }

    // Call this when AR task is completed
    public void OnARTaskCompleted(int triggerId)
    {
        if (triggerId < 0 || triggerId >= NumTasks || taskCompleted[triggerId]) return;
        taskCompleted[triggerId] = true;
        tasksDone++;
        Debug.Log($"AR Task {triggerId + 1} complete. {tasksDone}/{NumTasks} done.");

        // TODO: Deactivate AR session, unlock movement, return to world

        if (tasksDone >= NumTasks)
        {
            ShowCompletionDialogue();
        }
    }

    private void ShowCompletionDialogue()
    {
        var lines = new System.Collections.Generic.List<ProdDialogueLine>
        {
            new ProdDialogueLine("Professor Lingap", "Great job! All appliances are secured.")
        };
        ProdDialogueManager.Instance.ShowDialogueSequence(lines);
    }
}
