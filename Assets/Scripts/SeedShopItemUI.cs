using UnityEngine;
using UnityEngine.UI;

public class SeedShopItemUI : MonoBehaviour
{
    [Header("UI Components")]

    [SerializeField] private Text seedNameText;
    [SerializeField] private Button purchaseButton;

    private NpcFarmerController.SeedShopItem seedItem;
    private NpcFarmerController farmerController;

    public void Setup(NpcFarmerController.SeedShopItem item, NpcFarmerController controller)
    {
        seedItem = item;
        farmerController = controller;

        //if (seedNameText != null)
        //    seedNameText.text = $"Hạt {item.seedData.description}";


        if (purchaseButton != null)
        {
            purchaseButton.onClick.RemoveAllListeners();
            purchaseButton.onClick.AddListener(() => OnPurchaseClicked());
            UpdateButtonState();
        }
    }

    private void UpdateButtonState()
    {
        if (purchaseButton != null && LoadDataManager.userInGame != null)
        {
            bool canAfford = LoadDataManager.userInGame.Gold >= seedItem.price;
            purchaseButton.interactable = canAfford;

            // Đổi màu button
            var buttonImage = purchaseButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = canAfford ? Color.white : Color.gray;
            }
        }
    }

    private void OnPurchaseClicked()
    {
        farmerController.PurchaseSeed(seedItem);
        UpdateButtonState();
    }
}


