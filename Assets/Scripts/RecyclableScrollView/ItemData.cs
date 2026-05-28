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
    public Sprite seedSprite;           // Sprite hạt giống trong inventory
    public Sprite harvestedSprite;      // Sprite quả thu hoạch trong inventory

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
    public string harvestedItemName;    // "pumpkin_fruit"
    public int harvestQuantity = 1;     // Số lượng thu hoạch

    // Method để lấy sprite cho inventory
    //public Sprite GetInventorySprite(ItemContext context)
    //{
    //    switch (context)
    //    {
    //        case ItemContext.Seed:
    //            return seedSprite;
    //        case ItemContext.Harvested:
    //            return harvestedSprite;
    //        case ItemContext.Default:
    //        default:
    //            return harvestedSprite != null ? harvestedSprite : seedSprite;
    //    }
    //}
}

//public enum ItemContext
//{
//    Default,
//    Seed,       // Hạt giống
//    Harvested   // Quả thu hoạch
//}

