using UnityEngine;
using UnityEngine.UI;

public class BaitShopItemUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Text baitNameText;
    [SerializeField] private Text priceText;
    [SerializeField] private Button purchaseButton;

    private NpcFishermanController.BaitShopItem baitItem;
    private NpcFishermanController fishermanController;

    public void Setup(NpcFishermanController.BaitShopItem item, NpcFishermanController controller)
    {
        baitItem = item;
        fishermanController = controller;

        if (baitNameText != null)
            baitNameText.text = item.baitData.description;

        if (priceText != null)
            priceText.text = $"Mua {item.price} vàng";

        if (purchaseButton != null)
        {
            purchaseButton.onClick.RemoveAllListeners();
            purchaseButton.onClick.AddListener(() => OnPurchaseClicked());
            UpdateButtonState();
        }
    }

    private void UpdateButtonState()
    {
        if (baitItem == null)
        {
            Debug.LogWarning(" BaitItem is not initialized yet!");
            return;
        }

        if (purchaseButton != null && LoadDataManager.userInGame != null)
        {
            bool canAfford = LoadDataManager.userInGame.Gold >= baitItem.price;
            purchaseButton.interactable = canAfford;

            var buttonImage = purchaseButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = canAfford ? Color.white : Color.gray;
            }
        }
    }

    private void OnPurchaseClicked()
    {
        if (fishermanController == null)
        {
            Debug.LogError(" FishermanController is null!");
            return;
        }

        fishermanController.PurchaseBait(baitItem);
        UpdateButtonState();
    }

    private void OnEnable()
    {
        UpdateButtonState();
    }
}