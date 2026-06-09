using UnityEngine;
using UnityEngine.UI;

public class FishSellItemUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Text fishNameText;
    [SerializeField] private Text priceText;
    [SerializeField] private Button sellButton;


    private NpcFishermanController.FishSellData fishData;
    private NpcFishermanController fishermanController;

    public void Setup(NpcFishermanController.FishSellData data, NpcFishermanController controller)
    {
        fishData = data;
        fishermanController = controller;

        if (fishNameText != null)
        {
            string displayName = GetFishDisplayName(data.fishType);
            fishNameText.text = displayName;
        }

        if (priceText != null)
            priceText.text = $"{data.sellPrice} Gold/ea";

        if (sellButton != null)
        {
            sellButton.onClick.RemoveAllListeners();
            sellButton.onClick.AddListener(() => OnSellClicked());
        }

        RefreshButton();
    }

    public void RefreshButton()
    {
        if (fishermanController == null || fishData == null) return;

        bool hasFish = fishermanController.HasFishInInventory(fishData.fishType);

        if (sellButton != null)
        {
            sellButton.interactable = hasFish;

            var buttonImage = sellButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = hasFish ? Color.white : Color.gray;
            }

            var buttonText = sellButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = hasFish ? "Bán 1 con" : "Hết cá";
            }
        }
    }
    private void OnSellClicked()
    {
        int quantityToSell = 1;

        int availableQuantity = fishermanController.GetFishQuantityInInventory(fishData.fishType);
        if (availableQuantity <= 0)
        {
            Debug.Log($"Không có {GetFishDisplayName(fishData.fishType)} để bán!");
            return;
        }

        fishermanController.SellFish(fishData, quantityToSell);

        Debug.Log($"Đã bán 1 {GetFishDisplayName(fishData.fishType)} với giá {fishData.sellPrice} gold");
        NotificationManager.ShowReward($"Đã bán 1 {GetFishDisplayName(fishData.fishType)} với giá {fishData.sellPrice} vàng");

    }

    private string GetFishDisplayName(string fishType)
    {
        var fishNames = new System.Collections.Generic.Dictionary<string, string>
        {
            { "crab", "Cua hoàng đế" },
            { "lobster", "Tôm hùm Alaska" },
            { "tetra_neon_fish", "Cá Neon Tetra" },
            { "gold_fish", "Cá vàng" },
            { "discus_fish", "Cá dĩa đỏ sọc xanh" },
            { "angel_fish", "Cá thần tiên" }
        };

        return fishNames.ContainsKey(fishType) ? fishNames[fishType] : fishType;
    }

    private void OnEnable()
    {
        RefreshButton();
    }
}

