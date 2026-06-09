using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Item Database", menuName = "Inventory/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [Header(header: "All Items")]
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
            return item.description;
        }
        return item.description;
    }

}


