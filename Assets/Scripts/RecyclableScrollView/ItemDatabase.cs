//using System.Collections.Generic;
//using UnityEngine;

//[CreateAssetMenu(fileName = "Item Database", menuName = "Inventory/Item Database")]
//public class ItemDatabase : ScriptableObject
//{
//    [Header("All Items")]
//    public List<ItemData> allItems = new List<ItemData>();

//    private Dictionary<string, ItemData> itemDict = new Dictionary<string, ItemData>();

//    private void OnEnable()
//    {
//        InitializeDictionary();
//    }

//    public void InitializeDictionary()
//    {
//        itemDict.Clear();
//        foreach (var item in allItems)
//        {
//            if (item != null && !string.IsNullOrEmpty(item.itemName))
//            {
//                itemDict[item.itemName] = item;
//            }
//        }
//        Debug.Log($"ItemDatabase initialized with {itemDict.Count} items");
//    }

//    public ItemData GetItem(string itemName)
//    {
//        if (string.IsNullOrEmpty(itemName)) return null;

//        itemDict.TryGetValue(itemName, out ItemData item);
//        return item;
//    }

//    public Sprite GetSprite(string itemName)
//    {
//        var item = GetItem(itemName);
//        return item?.itemSpriteFruit;
//    }

//    public string GetDescription(string itemName)
//    {
//        var item = GetItem(itemName);
//        return item?.description ?? "";
//    }

//    public bool IsValidItem(string itemName)
//    {
//        return itemDict.ContainsKey(itemName);
//    }

//    [ContextMenu("Refresh Database")]
//    public void RefreshDatabase()
//    {
//        InitializeDictionary();
//    }
//}
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Item Database", menuName = "Inventory/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [Header("All Items")]
    public List<ItemData> allItems = new List<ItemData>();

    private Dictionary<string, ItemData> itemDict = new Dictionary<string, ItemData>();

    private void OnEnable()
    {
        InitializeDictionary();
    }

    public void InitializeDictionary()
    {
        itemDict.Clear();
        foreach (var item in allItems)
        {
            if (item != null && !string.IsNullOrEmpty(item.itemName))
            {
                itemDict[item.itemName] = item;
            }
        }
        Debug.Log($"ItemDatabase initialized with {itemDict.Count} items");
    }

    public ItemData GetItem(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return null;

        itemDict.TryGetValue(GetBaseItemName(itemName), out ItemData item);
        return item;
    }

    public Sprite GetSprite(string itemName)
    {
        var item = GetItem(GetBaseItemName(itemName));
        if (item == null) return null;

        // Xác định loại item và trả về sprite phù hợp
        if (itemName.Contains("_seed"))
        {
            return item.seedSprite != null ? item.seedSprite : item.harvestedSprite;
        }
        else if (itemName.Contains("_fruit") || itemName.Contains("_harvested"))
        {
            return item.harvestedSprite != null ? item.harvestedSprite : item.seedSprite;
        }
        else
        {
            // Mặc định trả về harvested sprite
            return item.harvestedSprite != null ? item.harvestedSprite : item.seedSprite;
        }
    }

    private string GetBaseItemName(string itemName)
    {
        if (itemName.EndsWith("_seed"))
            return itemName.Replace("_seed", "");
        else if (itemName.EndsWith("_fruit"))
            return itemName.Replace("_fruit", "");
        else if (itemName.EndsWith("_harvested"))
            return itemName.Replace("_harvested", "");

        return itemName;
    }

    public string GetDescription(string itemName)
    {
        var item = GetItem(GetBaseItemName(itemName));
        if (item == null) return "";

        // Tạo description phù hợp với context
        if (itemName.Contains("_seed"))
        {
            return $"Hạt giống {item.description}";
        }
        else if (itemName.Contains("_fruit") || itemName.Contains("_harvested"))
        {
            return item.description;
        }
        else if (itemName.Contains("_bait"))
        {
            return $"Mồi {item.description}";
        }
        return item.description;
    }

    public bool IsValidItem(string itemName)
    {
        return itemDict.ContainsKey(GetBaseItemName(itemName));
    }

    public bool IsSeedItem(string itemName)
    {
        return itemName.Contains("_seed");
    }

    public bool IsFruitItem(string itemName)
    {
        return itemName.Contains("_fruit") || itemName.Contains("_harvested");
    }

    public int GetSellPrice(string itemName)
    {
        var item = GetItem(GetBaseItemName(itemName));
        if (item == null) return 0;

        if (IsFruitItem(itemName))
        {
            return item.sellPrice * 2;
        }
        else if (IsSeedItem(itemName))
        {
            return item.sellPrice;
        }

        return item.sellPrice;
    }

    // Thêm methods cho compatibility với InventoryCell
    public ItemType GetItemType(string itemName)
    {
        if (IsSeedItem(itemName))        // Contains "_seed"
            return ItemType.Seed;
        else if (IsFruitItem(itemName))  // Contains "_fruit" 
            return ItemType.Fruit;
        else
            return ItemType.Default;
    }

    public string GetDisplayName(string itemName)
    {
        return GetDescription(itemName);
    }
    public bool CanFeedAnimals(string itemName)
    {
        var item = GetItem(GetBaseItemName(itemName));

        // Chỉ fruits mới có thể cho động vật ăn
        if (IsFruitItem(itemName) && item?.isFood == true)
        {
            return true;
        }

        return false;
    }

    [ContextMenu("Refresh Database")]
    public void RefreshDatabase()
    {
        InitializeDictionary();
    }
}

// Thêm enum ItemType
public enum ItemType
{
    Default,
    Seed,
    Fruit,
    Tool,
    Material
}

