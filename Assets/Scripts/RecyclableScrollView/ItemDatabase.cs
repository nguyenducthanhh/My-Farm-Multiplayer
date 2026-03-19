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

        itemDict.TryGetValue(itemName, out ItemData item);
        return item;
    }

    public Sprite GetSprite(string itemName)
    {
        var item = GetItem(itemName);
        return item?.itemSprite;
    }

    public string GetDescription(string itemName)
    {
        var item = GetItem(itemName);
        return item?.description ?? "";
    }

    public bool IsValidItem(string itemName)
    {
        return itemDict.ContainsKey(itemName);
    }

    [ContextMenu("Refresh Database")]
    public void RefreshDatabase()
    {
        InitializeDictionary();
    }
}