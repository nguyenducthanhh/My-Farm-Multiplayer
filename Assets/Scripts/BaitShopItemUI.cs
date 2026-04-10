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
            baitNameText.text = $"Mồi {item.baitData.description}";

        if (priceText != null)
            priceText.text = $"{item.price} Gold";

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
            bool canAfford = LoadDataManager.userInGame.Gold >= baitItem.price;
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
        fishermanController.PurchaseBait(baitItem);
        UpdateButtonState();
    }

    private void OnEnable()
    {
        UpdateButtonState();
    }
}

