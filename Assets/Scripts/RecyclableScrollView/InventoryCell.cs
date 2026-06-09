using PolyAndCode.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class InventoryCell : MonoBehaviour, ICell, IPointerClickHandler
{
    [SerializeField] private Image itemImage;
    [SerializeField] private Text itemNameText;
    [SerializeField] private Text quantityText;
    [SerializeField] private Button cellButton;

    private string itemName;
    private int itemQuantity;
    private PlayerFarmController farmController;

    private void Awake()
    {
        Debug.Log($"InventoryCell Awake: {gameObject.name}");

        farmController = FindObjectOfType<PlayerFarmController>();
        Debug.Log($"FarmController found: {farmController != null}");

        if (cellButton == null)
        {
            cellButton = GetComponent<Button>();
            Debug.Log($"Found existing Button: {cellButton != null}");
        }

        if (cellButton == null)
        {
            cellButton = gameObject.AddComponent<Button>();
            Debug.Log($"Added Button to {gameObject.name}");
        }

        if (cellButton != null)
        {
            cellButton.onClick.RemoveAllListeners();
            cellButton.onClick.AddListener(() => OnButtonClick());
            Debug.Log($"Button event added to {gameObject.name}");
            Debug.Log($"Button interactable: {cellButton.interactable}");
        }

        if (itemImage != null)
        {
            itemImage.raycastTarget = true;
            Debug.Log($"Image raycastTarget set to true");
        }
    }

    public void ConfigureCell(string itemName, int quantity)
    {
        Debug.Log($"ConfigureCell called: {itemName} x{quantity} on {gameObject.name}");

        this.itemName = itemName;
        this.itemQuantity = quantity;

        if (itemNameText != null)
        {
            string vietnameseDisplayName = GetVietnameseDisplayName(itemName);
            itemNameText.text = vietnameseDisplayName;
            Debug.Log($"Display name: {vietnameseDisplayName}");
        }

        if (quantityText != null)
            quantityText.text = $"x{quantity}";

        if (cellButton != null)
        {
            Debug.Log($"Button exists after configure: interactable={cellButton.interactable}");
        }
        else
        {
            Debug.LogError($"❌ Button is NULL after configure!");
        }
    }
    private string GetVietnameseDisplayName(string itemName)
    {
        if (farmController?.recyclableInventory?.itemDatabase != null)
        {
            string description = farmController.recyclableInventory.itemDatabase.GetDescription(itemName);

            if (!string.IsNullOrEmpty(description))
            {
                return description;
            }
        }

        return ConvertToVietnamese(itemName);
    }
    private string ConvertToVietnamese(string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
            return itemName;

        var translations = new Dictionary<string, string>
    {
        // Seeds
        { "strawberry_seed", "Hạt giống dâu tây" },
        { "grape_seed", "Hạt giống nho" },
        { "carrot_seed", "Hạt giống cà rốt" },
        { "corn_seed", "Hạt giống ngô" },
        { "paddy_seed", "Hạt giống lúa" },
        
        // Fruits
        { "strawberry_fruit", "Dâu tây" },
        { "grape_fruit", "Nho" },
        { "carrot_fruit", "Củ cà rốt" },
        { "corn_fruit", "Bắp ngô" },
        { "paddy_fruit", "Lúa" },
        
        // Fallback patterns
        { "strawberry", "Dâu tây" },
        { "grape", "Nho" },
        { "carrot", "Cà rốt" },
        { "corn", "Ngô" },
        { "paddy", "Lúa" }
    };

        if (translations.ContainsKey(itemName.ToLower()))
        {
            return translations[itemName.ToLower()];
        }

        if (itemName.EndsWith("_seed"))
        {
            string baseName = itemName.Replace("_seed", "");
            if (translations.ContainsKey(baseName))
            {
                return $"Hạt giống {translations[baseName].ToLower()}";
            }
            return $"Hạt giống {baseName}";
        }

        if (itemName.EndsWith("_fruit"))
        {
            string baseName = itemName.Replace("_fruit", "");
            if (translations.ContainsKey(baseName))
            {
                return $"{translations[baseName].ToLower()}";
            }
            return $"{baseName}";
        }

        return itemName;
    }

    public void SetSprite(Sprite sprite)
    {
        if (itemImage != null)
            itemImage.sprite = sprite;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"IPointerClick: {itemName}");
        HandleClick();
    }

    public void OnButtonClick()
    {
        Debug.Log($"Button Click: {itemName}");
        HandleClick();
    }

    private void HandleClick()
    {
        Debug.Log($"=== INVENTORY CELL CLICKED: {itemName} ===");

        if (farmController == null)
        {
            Debug.LogError("farmController is NULL!");
            farmController = FindObjectOfType<PlayerFarmController>();
            if (farmController == null)
            {
                Debug.LogError("No PlayerFarmController found in scene!");
                return;
            }
        }

        if (string.IsNullOrEmpty(itemName))
        {
            Debug.LogWarning("itemName is empty!");
            return;
        }

        if (itemName.Contains("_seed"))
        {
            Debug.Log($"This is a SEED: {itemName}");
            HandleSeedClick();
        }
        else if (itemName.Contains("_fruit"))
        {
            Debug.Log($"This is a FRUIT: {itemName}");
            HandleFruitClick();
        }
        else
        {
            Debug.Log($"{itemName} - No specific action available");
        }
    }

    private void HandleSeedClick()
    {
        Debug.Log($"HandleSeedClick called for: {itemName}");

        string baseItemName = itemName.Replace("_seed", newValue: "");
        Debug.Log($"Looking for PlantData: {baseItemName}");

        var plantData = farmController.GetPlantData(baseItemName);

        if (plantData != null)
        {
            Debug.Log($"PlantData FOUND for: {baseItemName}");
            Debug.Log($"Calling OnSeedSelected({itemName}, {itemQuantity})");

            farmController.OnSeedSelected(itemName, itemQuantity, itemImage.sprite);
        }
        else
        {
            Debug.LogError($" NO PlantData found for: {baseItemName}");

            if (farmController.allPlantDatas != null && farmController.allPlantDatas.Count > 0)
            {
                string availableTypes = string.Join(", ", farmController.allPlantDatas.ConvertAll(p => p.plantType));
                Debug.Log($"Available PlantData types: {availableTypes}");
            }
            else
            {
                Debug.LogError("allPlantDatas is NULL or EMPTY!");
            }
        }
    }

    private void HandleFruitClick()
    {
        Debug.Log($"Selected fruit: {itemName} - Can be sold or fed to animals");
        ShowFruitActionMenu();
    }

    private void ShowFruitActionMenu()
    {

        Debug.Log("Fruit action menu - Coming soon!");
    }
}


