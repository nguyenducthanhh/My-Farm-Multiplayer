using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using Firebase.Extensions;

public class NpcFarmerController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject farmerMenu;
    [SerializeField] private Button interactButton;
    [SerializeField] private Button closeMenuButton;

    [Header("Static Shop Items")]
    [SerializeField] private SeedShopItemUI paddyShopItem;
    [SerializeField] private SeedShopItemUI grapeShopItem;
    [SerializeField] private SeedShopItemUI cornShopItem;
    [SerializeField] private SeedShopItemUI carrotShopItem;
    [SerializeField] private SeedShopItemUI strawberryShopItem;

    [SerializeField] private List<SeedShopItem> seedsForSale = new List<SeedShopItem>();

    [Header("Player References")]
    [SerializeField] private RecyclableInventory playerInventory;

    [Header("UI Display")]
    [SerializeField] private Text playerGoldText; 

    private bool playerInRange = false;

    [System.Serializable]
    public class SeedShopItem
    {
        public ItemData seedData;
        public int price;
        public int quantityPerPurchase = 1;
    }

    private void Start()
    {

        UpdateGoldDisplay();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            playerInRange = true;
            interactButton.gameObject.SetActive(true);
            UpdateGoldDisplay(); 
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            playerInRange = false;
            interactButton.gameObject.SetActive(false);
            CloseShopMenu();
        }
    }

    public void OpenShopMenu()
    {
        if (!playerInRange) return;

        farmerMenu.SetActive(true);
        SetupStaticShopItems();
        UpdateGoldDisplay();
    }

    public void CloseShopMenu()
    {
        farmerMenu.SetActive(false);
    }


    private void SetupStaticShopItems()
    {
        // Setup từng item có sẵn
        if (paddyShopItem != null )
            paddyShopItem.Setup(seedsForSale[0], this);

        if (grapeShopItem != null)
            grapeShopItem.Setup(seedsForSale[1], this);

        if (cornShopItem != null)
            cornShopItem.Setup(seedsForSale[2], this);

        if (carrotShopItem != null)
            carrotShopItem.Setup(seedsForSale[3], this);
       
        if (strawberryShopItem != null)
            strawberryShopItem.Setup(seedsForSale[4], this);
    }


    public void PurchaseSeed(SeedShopItem seedItem)
    {
        if (!CanAfford(seedItem.price))
        {
            Debug.Log($"Không đủ tiền! Cần {seedItem.price} gold, hiện có {LoadDataManager.userInGame.Gold}");
            return;
        }

        LoadDataManager.userInGame.Gold -= seedItem.price;

        // TẠO SEED ITEM VỚI TÊN _seed
        string seedItemName = $"{seedItem.seedData.itemName}_seed";
        string seedDescription = playerInventory.itemDatabase?.GetDescription(seedItemName)
                               ?? $"Hạt {seedItem.seedData.description}";

        InventoryItems newSeed = new InventoryItems(
            seedItemName,                    // "pumpkin_seed"
            seedDescription,                 // "Hạt giống bí ngô"
            seedItem.quantityPerPurchase     // Số lượng
        );

        playerInventory.AddInventoryItem(newSeed);
        SaveUserDataToFirebase();
        UpdateGoldDisplay();
        UsernameWizard.UpdateGoldDisplay();
        Debug.Log($"Đã mua {seedItem.quantityPerPurchase}x {seedItemName}");
    }


    private bool CanAfford(int price)
    {
        return LoadDataManager.userInGame != null && LoadDataManager.userInGame.Gold >= price;
    }

    private void SaveUserDataToFirebase()
    {
        if (LoadDataManager.userInGame == null || LoadDataManager.firebaseUser == null)
        {
            Debug.LogError("User data or Firebase user is null!");
            return;
        }


        string userData = LoadDataManager.userInGame.ToString();

        FirebaseDatabase.DefaultInstance
       .GetReference("Users")
       .Child(LoadDataManager.firebaseUser.UserId)
       .Child("Gold")
       .SetValueAsync(LoadDataManager.userInGame.Gold)
       .ContinueWithOnMainThread(task =>
       {
                if (task.IsCompleted)
                {
                    Debug.Log("User data (including Gold) saved to Firebase successfully!");
                }
                else
                {
                    Debug.LogError("Failed to save user data: " + task.Exception);
                }
            });
    }

    private void UpdateGoldDisplay()
    {
        if (playerGoldText != null && LoadDataManager.userInGame != null)
        {
            playerGoldText.text = $"Gold: {LoadDataManager.userInGame.Gold}";
        }
    }

    //[ContextMenu("Add Test Gold")]
    //public void AddTestGold()
    //{
    //    if (LoadDataManager.userInGame != null)
    //    {
    //        LoadDataManager.userInGame.Gold += 100;
    //        SaveUserDataToFirebase();
    //        UpdateGoldDisplay();
    //        Debug.Log("Added 1000 test gold");
    //    }
    //}
}
