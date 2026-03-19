using Firebase.Database;
using Firebase.Extensions;
using Newtonsoft.Json;
using PolyAndCode.UI;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class RecyclableInventory : MonoBehaviour, IRecyclableScrollRectDataSource
{
    [SerializeField]public RecyclableScrollRect _recyclableScrollRect;
    [SerializeField]private int _dataLength;
    [SerializeField] private GameObject inventoryGameObject;

    [Header("Item Database")]
    [SerializeField] private ItemDatabase itemDatabase;

    private List<InventoryItems> _invenItems = new List<InventoryItems>();
    private Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

    private void Awake()
    {
        Debug.Log($"RecyclableInventory on GameObject: {gameObject.name}");

        //_recyclableScrollRect.DataSource = this;
        if (_recyclableScrollRect != null)
        {
            Debug.Log($"RecyclableScrollRect on GameObject: {_recyclableScrollRect.gameObject.name}");
            _recyclableScrollRect.DataSource = this;
        }
        if (itemDatabase != null)
        {
            itemDatabase.InitializeDictionary();
        }
        else
        {
            Debug.LogWarning("ItemDatabase not assigned to RecyclableInventory!");
        }
    }
    private void Start()
    {
        LoadInventoryFromFirebase();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            inventoryGameObject.SetActive(!inventoryGameObject.activeSelf);

            if (inventoryGameObject.activeSelf && needReload)
            {
                _recyclableScrollRect.ReloadData();
                needReload = false;
            }
        }
    }
    public int GetItemCount()
    {
        return _invenItems.Count;
    }

    public void SetCell(ICell cell, int index)
    {
        //var item = cell as CelltemData;
        //item.ConfigureCell(_invenItems[index], index);
        if (cell == null)
        {
            Debug.LogError("Cell is null in SetCell!");
            return;
        }

        if (index < 0 || index >= _invenItems.Count)
        {
            Debug.LogError($"Index {index} is out of range. _invenItems.Count = {_invenItems.Count}");
            return;
        }
        var item = cell as InventoryCell;
        if (item == null)
        {
            Debug.LogError("Could not cast cell to InventoryCell!");
            return;
        }
        var data = _invenItems[index];
        if (data == null)
        {
            Debug.LogError($"InventoryItem at index {index} is null!");
            return;
        }
        item.ConfigureCell(data.name, data.quantity);

        LoadSpriteForItem(data, item);

    }
    private void LoadSpriteForItem(InventoryItems item, InventoryCell cell)
    {
        if (item == null || cell == null || string.IsNullOrEmpty(item.name))
        {
            Debug.LogError("Invalid item or cell in LoadSpriteForItem!");
            return;
        }
        if (spriteCache.ContainsKey(item.name))
        {
            cell.SetSprite(spriteCache[item.name]);
            return;
        }


        Sprite sprite = null;

        //Sprite sprite = Resources.Load<Sprite>($"ItemSprites/{item.name}");

        if (itemDatabase != null)
        {
            sprite = itemDatabase.GetSprite(item.name);
            if (sprite != null)
            {
                spriteCache[item.name] = sprite;
                cell.SetSprite(sprite);
                return;
            }
        }

        sprite = Resources.Load<Sprite>($"ItemSprites/{item.name}");

        if (sprite != null)
        {
            spriteCache[item.name] = sprite;
            cell.SetSprite(sprite);
        }
        else
        {
            sprite = Resources.Load<Sprite>("ItemSprites/default");
            if (sprite != null)
            {
                cell.SetSprite(sprite);
            }
        }
    }


    public void SetLstItem(List<InventoryItems> lst)
    {
        _invenItems = lst;
    }


    private bool needReload = false;

    public void AddInventoryItem(InventoryItems item)
    {
        if (item == null)
        {
            Debug.LogError("Item is null!");
            return;
        }
        //_invenItems.Add(item);
        //needReload = true;

        var existingItem = _invenItems.Find(i => i.name == item.name);
        if (existingItem != null)
        {
            existingItem.quantity += item.quantity;
        }
        else
        {
            _invenItems.Add(item);
        }

        //_recyclableScrollRect.ReloadData();
        //if (gameObject.activeInHierarchy && _recyclableScrollRect != null)
        //{
        //    _recyclableScrollRect.ReloadData();
        //    Debug.Log("Inventory reloaded immediately");
        //}
        if (_recyclableScrollRect != null && _recyclableScrollRect.gameObject.activeInHierarchy)
        {
            _recyclableScrollRect.ReloadData();
            Debug.Log("Inventory reloaded immediately");
        }
        else
        {
            needReload = true;
            Debug.Log("Inventory inactive, marked for reload when opened");
        }
        SaveInventoryToFirebase();
    }

    public void RemoveInventoryItem(string itemName, int quantity = 1)
    {
        var item = _invenItems.Find(i => i.name == itemName);
        if (item != null)
        {
            item.quantity -= quantity;
            if (item.quantity <= 0)
            {
                _invenItems.Remove(item);
            }
        }

        if (gameObject.activeInHierarchy && _recyclableScrollRect != null)
        {
            _recyclableScrollRect.ReloadData();
        }
        else
        {
            needReload = true;
        }
        // _recyclableScrollRect.ReloadData();
        SaveInventoryToFirebase();
    }

    public void SaveInventoryToFirebase()
    {
        if (LoadDataManager.userInGame == null)
        {
            Debug.LogError("LoadDataManager.userInGame is null!");
            return;
        }

        if (LoadDataManager.firebaseUser == null)
        {
            Debug.LogError("LoadDataManager.firebaseUser is null!");
            return;
        }

        if (LoadDataManager.userInGame != null)
        {
            LoadDataManager.userInGame.Inventory = _invenItems;

            string json = JsonConvert.SerializeObject(_invenItems);
            Debug.Log($"Saving inventory to Firebase: {json}");
            FirebaseDatabase.DefaultInstance
                .GetReference("users")
                .Child(LoadDataManager.firebaseUser.UserId)
                .Child("Inventory")
                .SetRawJsonValueAsync(json)
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsCompleted)
                    {
                        Debug.Log("Inventory saved to Firebase successfully!");
                    }
                    else
                    {
                        Debug.LogError("Failed to save inventory: " + task.Exception);
                    }
                });
        }
    }
    public void LoadInventoryFromFirebase()
    {
        FirebaseDatabase.DefaultInstance
            .GetReference("users")
            .Child(LoadDataManager.firebaseUser.UserId)
            .Child("Inventory")
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    string json = task.Result.GetRawJsonValue();

                    if (!string.IsNullOrEmpty(json))
                    {
                        _invenItems = JsonConvert.DeserializeObject<List<InventoryItems>>(json);

                        if (LoadDataManager.userInGame != null)
                        {
                            LoadDataManager.userInGame.Inventory = _invenItems;
                        }

                        // _recyclableScrollRect.ReloadData();
                        if (gameObject.activeInHierarchy && _recyclableScrollRect != null)
                        {
                            _recyclableScrollRect.ReloadData();
                        }
                        else
                        {
                            needReload = true;
                        }

                        Debug.Log("Inventory loaded from Firebase successfully!");
                    }
                }
                else
                {
                    Debug.LogError("Failed to load inventory: " + task.Exception);
                }
            });
    }

}
