using Firebase.Database;
using Firebase.Extensions;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class SeedSlot : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image seedImage;
    [SerializeField] private Text seedNameText;
    [SerializeField] private Text quantityText;
    [SerializeField] private GameObject selectionBorder;
    [SerializeField] private Button slotButton;

    [Header("Slot ID")]
    [SerializeField] private string slotId = "slot1";

    [Header("Slot Data")]
    public string plantType = ""; 
    public int quantity = 0;

  

    private PlayerFarmController farmController;

    private void Awake()
    {
        farmController = FindObjectOfType<PlayerFarmController>();

        if (slotButton != null)
        {
            slotButton.onClick.AddListener(OnSlotClicked);
        }
    }

    public void SetSeed(string seedType, int seedQuantity, Sprite seedSprite, string displayName)
    {
        // KIỂM TRA NẾU CÙNG LOẠI THỊ CỘNG DỒN
        if (!string.IsNullOrEmpty(plantType) && plantType == seedType)
        {
            // Cùng loại seed - cộng dồn quantity
            quantity += seedQuantity;
            Debug.Log($"Added {seedQuantity} to existing {seedType}. New quantity: {quantity}");
        }
        else
        {
            // LOẠI KHÁC - TRẢ SEED CŨ VỀ INVENTORY TRƯỚC KHI THAY THẾ
            if (!string.IsNullOrEmpty(plantType) && quantity > 0)
            {
                Debug.Log($"Returning {quantity}x {plantType} to inventory before replacing with {seedType}");

                // TRẢ SEED CŨ VỀ INVENTORY
                ReturnSeedToInventory();
            }

            // THIẾT LẬP SEED MỚI
            plantType = seedType;
            quantity = seedQuantity;

            if (seedImage != null)
                seedImage.sprite = seedSprite;

            Debug.Log($"Set new seed {seedType} with quantity: {quantity}");
        }

        // Cập nhật UI
        if (seedNameText != null)
            seedNameText.text = displayName;

        if (quantityText != null)
            quantityText.text = $"x{quantity}";

        if (slotButton != null)
            slotButton.interactable = quantity > 0;

        // LƯU SLOT DATA LÊN FIREBASE
        SaveSlotDataToFirebase();
    }

    private void ReturnSeedToInventory()
    {
        if (string.IsNullOrEmpty(plantType) || quantity <= 0)
            return;

        if (farmController?.recyclableInventory == null)
        {
            Debug.LogError("Cannot return seed - RecyclableInventory not found!");
            return;
        }

        // TẠO SEED ITEM ĐỂ TRẢ VỀ INVENTORY
        string seedItemName = $"{plantType}_seed";

        // LẤY DESCRIPTION TỪ DATABASE
        string seedDescription = farmController.recyclableInventory.itemDatabase?.GetDescription(seedItemName)
                               ?? $"Hạt giống {plantType}";

        // TẠO INVENTORY ITEM
        InventoryItems returnSeed = new InventoryItems(
            seedItemName,      
            seedDescription,    
            quantity            
        );

        // THÊM VỀ INVENTORY
        farmController.recyclableInventory.AddInventoryItem(returnSeed);

        Debug.Log($"Returned {quantity}x {seedItemName} to inventory");
    }
    private void SaveSlotDataToFirebase()
    {
        if (LoadDataManager.firebaseUser == null || !LoadDataManager.IsUserDataLoaded || !LoadDataManager.HasUserRecord)
        {
            Debug.LogError("User data is not validly loaded. Seed slot will not be saved.");
            return;
        }

        var slotData = new SeedSlotData
        {
            plantType = this.plantType,
            quantity = this.quantity,
            slotId = this.slotId
        };

        string json = JsonConvert.SerializeObject(slotData);

        FirebaseDatabase.DefaultInstance
            .GetReference("Users")
            .Child(LoadDataManager.firebaseUser.UserId)
            .Child("SeedSlots")
            .Child(slotId)
            .SetRawJsonValueAsync(json)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log($"{slotId} saved to Firebase: {plantType} x{quantity}");
                }
                else
                {
                    Debug.LogError($"Failed to save {slotId}: {task.Exception}");
                }
            });
    }
    public void LoadSlotDataFromFirebase()
    {
        if (LoadDataManager.firebaseUser == null || !LoadDataManager.IsUserDataLoaded || !LoadDataManager.HasUserRecord)
        {
            Debug.LogError("User data is not validly loaded. Seed slot will not be loaded.");
            return;
        }

        FirebaseDatabase.DefaultInstance
            .GetReference("Users")
            .Child(LoadDataManager.firebaseUser.UserId)
            .Child("SeedSlots")
            .Child(slotId)
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && task.Result.Value != null)
                {
                    try
                    {
                        string json = task.Result.GetRawJsonValue();
                        var slotData = JsonConvert.DeserializeObject<SeedSlotData>(json);

                        if (slotData != null && !string.IsNullOrEmpty(slotData.plantType))
                        {
                            plantType = slotData.plantType;
                            quantity = slotData.quantity;

                            UpdateSlotVisual();

                            Debug.Log($"{slotId} loaded: {plantType} x{quantity}");
                        }
                        else
                        {
                            ClearSlot(false);
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Error loading {slotId}: {e.Message}");
                        ClearSlot(false);
                    }
                }
                else
                {
                    Debug.Log($"{slotId} not found in Firebase - clearing slot");
                    ClearSlot(false);
                }
            });
    }
    private void UpdateSlotVisual()
    {
        if (string.IsNullOrEmpty(plantType) || quantity <= 0)
        {
            ClearSlot(saveToFirebase: false);
            return;
        }

        if (farmController?.recyclableInventory?.itemDatabase != null)
        {
            string seedItemName = $"{plantType}_seed";
            var sprite = farmController.recyclableInventory.itemDatabase.GetSprite(seedItemName);
            var displayName = farmController.recyclableInventory.itemDatabase.GetDescription(seedItemName);

            if (seedImage != null)
                seedImage.sprite = sprite;

            if (seedNameText != null)
                seedNameText.text = displayName ?? plantType;
        }

        if (quantityText != null)
            quantityText.text = $"x{quantity}";

        if (slotButton != null)
            slotButton.interactable = quantity > 0;
    }

    public void ClearSlot(bool saveToFirebase = true)
    {
        plantType = "";
        quantity = 0;

        if (seedImage != null)
            seedImage.sprite = null;

        if (seedNameText != null)
            seedNameText.text = "";

        if (quantityText != null)
            quantityText.text = "";

        if (slotButton != null)
            slotButton.interactable = false;

        SetSelected(false);
        if (farmController != null && farmController.currentSelectedSlot == this)
        {
            Debug.Log($"Clearing selected slot {slotId}");
            farmController.currentSelectedSlot = null;
            farmController.selectedPlantData = null;
        }
        if (saveToFirebase)
            SaveSlotDataToFirebase();
    }

    public void SetSelected(bool isSelected)
    {
        Debug.Log($"   SetSelected called on {gameObject.name}: {isSelected}");
        Debug.Log($"   selectionBorder: {selectionBorder != null}");

        if (selectionBorder != null)
        {
            selectionBorder.SetActive(isSelected);
            Debug.Log(message: $" SelectionBorder set to: {isSelected}");
        }
        else
        {
            Debug.LogError($" SelectionBorder is NULL on {gameObject.name}!");
        }
    }

    public void OnSlotClicked()
    {
        Debug.Log($"   OnSlotClicked called on {gameObject.name}");
        Debug.Log($"   plantType: '{plantType}'");
        Debug.Log($"   quantity: {quantity}");
        Debug.Log($"   farmController: {farmController != null}");

        if (!string.IsNullOrEmpty(plantType) && quantity > 0)
        {
            Debug.Log($"Calling SelectPlantFromSlot for {plantType}");
            farmController?.SelectPlantFromSlot(this);
        }
        else
        {
            Debug.LogWarning($"Cannot select slot - plantType: '{plantType}', quantity: {quantity}");
        }
    }
    public bool CanPlant()
    {
        return !string.IsNullOrEmpty(plantType) && quantity > 0;
    }

    public void UseSeed()
    {
        if (quantity > 0)
        {
            quantity--;

            if (quantityText != null)
                quantityText.text = quantity > 0 ? $"x{quantity}" : "";

            if (slotButton != null)
                slotButton.interactable = quantity > 0;

            if (quantity <= 0)
            {
                ClearSlot();
            }

            SaveSlotDataToFirebase();
        }
    }
}
