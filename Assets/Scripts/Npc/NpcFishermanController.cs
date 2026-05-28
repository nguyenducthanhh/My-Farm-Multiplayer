using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using Firebase.Extensions;
using static NpcFishermanController;

public class NpcFishermanController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject fishShopMenu;
    [SerializeField] private Button interactButton;
    [SerializeField] private Button closeMenuButton;

    [Header("Bait Shop")]
    [SerializeField] private BaitShopItemUI baitShopItem;
    [SerializeField] private List<BaitShopItem> baitsForSale = new List<BaitShopItem>();

    [Header("Fish Sell Buttons")]
    [SerializeField] private FishSellItemUI[] fishSellButtons = new FishSellItemUI[6];

    [Header("Player References")]
    [SerializeField] private RecyclableInventory playerInventory;

    [Header("UI Display")]
    [SerializeField] private Text playerGoldText;

    private bool playerInRange = false;

    [System.Serializable]
    public class BaitShopItem
    {
        public ItemData baitData;
        public int price;
        public int quantityPerPurchase = 1;
    }

    [System.Serializable]
    public class FishSellData
    {
        public string fishType; // "salmon_fish", "tuna_fish", etc.
        public int sellPrice;
        public int experienceReward;
    }

    [Header("Fish Sell Data")]
    [SerializeField] private List<FishSellData> fishSellPrices = new List<FishSellData>();

    private void Start()
    {
        if (interactButton != null)
            interactButton.onClick.AddListener(OpenShopMenu);

        if (closeMenuButton != null)
            closeMenuButton.onClick.AddListener(CloseShopMenu);

        SetupBaitShop();
        SetupFishSellButtons();

        fishShopMenu?.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = true;
            if (interactButton != null)
                interactButton.gameObject.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = false;
            if (interactButton != null)
                interactButton.gameObject.SetActive(false);
            CloseShopMenu();
        }
    }

    public void OpenShopMenu()
    {
        if (playerInRange && fishShopMenu != null)
        {
            fishShopMenu.SetActive(true);
            UpdateGoldDisplay();
            RefreshFishSellButtons(); // Cập nhật buttons dựa trên inventory
        }
    }

    public void CloseShopMenu()
    {
        if (fishShopMenu != null)
            fishShopMenu.SetActive(false);
    }

    private void SetupBaitShop()
    {
        if (baitShopItem != null && baitsForSale.Count > 0)
        {
            baitShopItem.Setup(baitsForSale[0], this);
        }
    }

    private void SetupFishSellButtons()
    {
        for (int i = 0; i < fishSellButtons.Length && i < fishSellPrices.Count; i++)
        {
            if (fishSellButtons[i] != null)
            {
                fishSellButtons[i].Setup(fishSellPrices[i], this);
            }
        }
    }

    public void RefreshFishSellButtons()
    {
        foreach (var button in fishSellButtons)
        {
            if (button != null)
                button.RefreshButton();
        }
    }

    // ===== BAIT PURCHASE =====
    public void PurchaseBait(BaitShopItem baitItem)
    {
        if (!CanAfford(baitItem.price))
        {
            Debug.Log($"Không đủ tiền! Cần {baitItem.price} gold, hiện có {LoadDataManager.userInGame.Gold}");
            return;
        }

        LoadDataManager.userInGame.Gold -= baitItem.price;

        // Tạo bait item
        string baitItemName =baitItem.baitData.itemName;
        string baitDescription = playerInventory.itemDatabase?.GetDescription(baitItemName)
                               ?? baitItem.baitData.description;

        InventoryItems newBait = new InventoryItems(
            baitItemName,                    // "worm_bait"
            baitDescription,                 // "Mồi giun"
            baitItem.quantityPerPurchase     // Số lượng
        );

        playerInventory.AddInventoryItem(newBait);
        SaveUserDataToFirebase();
        UpdateGoldDisplay();
        UsernameWizard.UpdateGoldDisplay();

        Debug.Log($"Đã mua {baitItem.quantityPerPurchase}x {baitItemName}");
    }

    // ===== FISH SELLING =====
    public void SellFish(FishSellData fishData, int quantity)
    {
        // Kiểm tra có cá trong inventory không
        var fishInInventory = playerInventory._invenItems?.Find(item => item.name == fishData.fishType);

        if (fishInInventory == null || fishInInventory.quantity < quantity)
        {
            Debug.Log($"Không có đủ {fishData.fishType} để bán!");
            return;
        }

        // Tính tiền
        int totalPrice = fishData.sellPrice * quantity;

        int totalExperience = fishData.experienceReward * quantity;


        // Cộng gold
        LoadDataManager.userInGame.Gold += totalPrice;

        if (LevelSystem.Instance != null)
        {
            LevelSystem.Instance.AddExperience(totalExperience);
            Debug.Log($"✅ +{totalExperience} Exp từ bán cá");
        }
        // Trừ cá khỏi inventory
        playerInventory.RemoveInventoryItem(fishData.fishType, quantity);

        SaveUserDataToFirebase();
        UpdateGoldDisplay();
        UsernameWizard.UpdateGoldDisplay();
        RefreshFishSellButtons(); // Cập nhật lại buttons

        Debug.Log($"Đã bán {quantity}x {fishData.fishType} với giá {totalPrice} gold");
    }

    private bool CanAfford(int price)
    {
        return LoadDataManager.userInGame != null && LoadDataManager.userInGame.Gold >= price;
    }

    public bool HasFishInInventory(string fishType)
    {
        if (playerInventory?._invenItems == null) return false;

        var fishItem = playerInventory._invenItems.Find(item => item.name == fishType);
        return fishItem != null && fishItem.quantity > 0;
    }

    public int GetFishQuantityInInventory(string fishType)
    {
        if (playerInventory?._invenItems == null) return 0;

        var fishItem = playerInventory._invenItems.Find(item => item.name == fishType);
        return fishItem?.quantity ?? 0;
    }

    private void SaveUserDataToFirebase()
    {
        if (LoadDataManager.userInGame == null || LoadDataManager.firebaseUser == null)
        {
            Debug.LogError("User data or Firebase user is null!");
            return;
        }

        FirebaseDatabase.DefaultInstance
            .GetReference("Users")
            .Child(LoadDataManager.firebaseUser.UserId)
            .Child("Gold")
            .SetValueAsync(LoadDataManager.userInGame.Gold)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log("Gold updated successfully!");
                }
                else
                {
                    Debug.LogError("Failed to save gold: " + task.Exception);
                }
            });
    }

    private void UpdateGoldDisplay()
    {
        if (playerGoldText != null && LoadDataManager.userInGame != null)
        {
            playerGoldText.text = $"Vàng: {LoadDataManager.userInGame.Gold}";
        }
    }
}

