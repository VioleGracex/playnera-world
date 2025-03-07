using UnityEngine;

public class DraggableItem : Draggable
{
    public override DraggableType Type => DraggableType.Item;
    public bool canBeHeld;

}