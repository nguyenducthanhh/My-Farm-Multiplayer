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
            // Convert "salmon_fish" -> "Cá hồi"
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

        // ✅ CHỈ CẬP NHẬT BUTTON STATE - KHÔNG HIỂN THỊ QUANTITY
        if (sellButton != null)
        {
            sellButton.interactable = hasFish;

            // Đổi màu button
            var buttonImage = sellButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = hasFish ? Color.white : Color.gray;
            }

            // Cập nhật text button
            var buttonText = sellButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = hasFish ? "Bán 1 con" : "Hết cá";
            }
        }
    }
    private void OnSellClicked()
    {
        // ✅ ĐỐN GIẢN: Luôn bán 1 con
        int quantityToSell = 1;

        // Kiểm tra có cá không
        int availableQuantity = fishermanController.GetFishQuantityInInventory(fishData.fishType);
        if (availableQuantity <= 0)
        {
            Debug.Log($"Không có {GetFishDisplayName(fishData.fishType)} để bán!");
            return;
        }

        // Bán 1 con
        fishermanController.SellFish(fishData, quantityToSell);

        Debug.Log($"Đã bán 1 {GetFishDisplayName(fishData.fishType)} với giá {fishData.sellPrice} gold");
    }

    private string GetFishDisplayName(string fishType)
    {
        // Convert fish types to Vietnamese names
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

