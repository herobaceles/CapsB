using UnityEngine;
using UnityEngine.EventSystems;

public static class InputUtility
{
    public static void ResetJoysticks(GameObject[] joystickObjectsToReset)
    {
        if (joystickObjectsToReset == null || joystickObjectsToReset.Length == 0) return;
        EventSystem currentEventSystem = EventSystem.current;
        if (currentEventSystem == null) return;

        for (int i = 0; i < joystickObjectsToReset.Length; i++)
        {
            GameObject joystickObj = joystickObjectsToReset[i];
            if (joystickObj == null) continue;

            var pointerEventData = new PointerEventData(currentEventSystem);
            ExecuteEvents.Execute<IPointerUpHandler>(joystickObj, pointerEventData, (handler, data) => handler.OnPointerUp(pointerEventData));
        }
    }
}
