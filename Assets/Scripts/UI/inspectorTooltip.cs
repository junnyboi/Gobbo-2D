using UnityEngine;

public interface inspectorTooltip
{
    void HighlightRoom(Room newRoom);
    void SetTooltipOffset(Vector3 offset);
    void TurnOffDetailedTooltip();
    GameObject TurnOnInspectorTooltip();
}