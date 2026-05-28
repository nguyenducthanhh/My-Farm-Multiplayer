using UnityEngine;
using UnityEngine.UI;

public class SeedShopItemUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Text seedNameText;
    [SerializeField] private Button purchaseButton;
    [SerializeField] private Text levelRequirementText; // ✅ Text yêu cầu cấp độ

    private NpcFarmerController.SeedShopItem seedItem;
    private NpcFarmerController farmerController;

    public void Setup(NpcFarmerController.SeedShopItem item, NpcFarmerController controller)
    {
        seedItem = item;
        farmerController = controller;

        if (purchaseButton != null)
        {
            purchaseButton.onClick.RemoveAllListeners();
            purchaseButton.onClick.AddListener(() => OnPurchaseClicked());
        }

        // ✅ Setup yêu cầu cấp độ
        UpdateLevelRequirementDisplay();
        UpdateButtonState();
    }

    // ✅ Hiển thị/ẩn text yêu cầu cấp độ
    private void UpdateLevelRequirementDisplay()
    {
        if (levelRequirementText == null || seedItem?.seedData == null)
            return;

        // Tìm cấp độ yêu cầu cho hạt giống này
        int requiredLevel = GetRequiredLevelForSeed(seedItem.seedData.itemName);
        int currentLevel = LevelSystem.Instance.GetCurrentLevel();

        if (requiredLevel > 0 && currentLevel < requiredLevel)
        {
            // ✅ Hiển thị text yêu cầu cấp độ (màu đỏ)
            levelRequirementText.text = $"Yêu cầu cấp {requiredLevel}";
            levelRequirementText.color = Color.red;
            levelRequirementText.gameObject.SetActive(true);

            Debug.Log($"🔒 {seedItem.seedData.itemName} - Yêu cầu cấp {requiredLevel} (Hiện tại: {currentLevel})");
        }
        else if (requiredLevel > 0)
        {
            // ✅ ẨN text yêu cầu cấp độ khi đủ cấp
            levelRequirementText.gameObject.SetActive(false);

            Debug.Log($"✅ {seedItem.seedData.itemName} - Đã mở khóa!");
        }
        else
        {
            // ✅ ẨN nếu không có yêu cầu cấp độ
            levelRequirementText.gameObject.SetActive(false);
        }
    }

    // ✅ Tìm cấp độ yêu cầu từ LevelConfig
    private int GetRequiredLevelForSeed(string seedName)
    {
        // ✅ FIX: Sử dụng property public LevelConfigData
        if (LevelSystem.Instance?.LevelConfigData == null)
            return 0;

        foreach (var unlock in LevelSystem.Instance.LevelConfigData.levelUnlocks)
        {
            if (unlock.unlockedItemName == seedName && unlock.unlockType == LevelConfig.UnlockType.Seed)
            {
                return unlock.requiredLevel;
            }
        }

        return 0; // Không có yêu cầu cấp độ
    }

    private void UpdateButtonState()
    {
        if (purchaseButton != null && LoadDataManager.userInGame != null)
        {
            int requiredLevel = GetRequiredLevelForSeed(seedItem.seedData.itemName);
            int currentLevel = LevelSystem.Instance.GetCurrentLevel();
            bool canAfford = LoadDataManager.userInGame.Gold >= seedItem.price;
            bool hasRequiredLevel = currentLevel >= requiredLevel;

            // ✅ Chỉ có thể mua nếu đủ vàng VÀ đủ cấp
            purchaseButton.interactable = canAfford && hasRequiredLevel;

            // Đổi màu button
            var buttonImage = purchaseButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                if (!hasRequiredLevel)
                {
                    buttonImage.color = Color.red; // ← Cấp không đủ
                }
                else if (!canAfford)
                {
                    buttonImage.color = Color.gray; // ← Vàng không đủ
                }
                else
                {
                    buttonImage.color = Color.white; // ← Có thể mua
                }
            }
        }
    }

    private void OnPurchaseClicked()
    {
        farmerController.PurchaseSeed(seedItem);
        UpdateButtonState();
    }
}