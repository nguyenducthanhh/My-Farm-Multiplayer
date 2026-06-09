using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item Data")]
[System.Serializable]
public class ItemData : ScriptableObject
{
    [Header("Basic Info")]
    public string itemName;
    public string description;
    public int maxStack = 99;

    [Header("Inventory Sprites")]
    public Sprite seedSprite;  
    public Sprite harvestedSprite; 

    [Header("Shop Info")]
    public int basePrice = 10;
    public int sellPrice = 5;
    public bool isSellable = true;
    public bool isPurchasable = true;

    [Header("Item Type")]
    public bool isSeed = false;
    public bool isFood = false;
    public bool isTool = false;

    [Header("Harvest Info")]
    public string harvestedItemName;
    public int harvestQuantity = 1;

}



