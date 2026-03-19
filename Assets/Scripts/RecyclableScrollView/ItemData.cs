using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item Data")]
[System.Serializable]
public class ItemData : ScriptableObject
{
    public string itemName;
    public string description;
    public Sprite itemSprite;
    public int maxStack = 99;
}