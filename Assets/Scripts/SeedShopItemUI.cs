using UnityEngine;
using UnityEngine.UI;

public class SeedShopItemUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Text seedNameText;
    [SerializeField] private Button purchaseButton;
    [SerializeField] private Text levelRequirementText;

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

        UpdateLevelRequirementDisplay();
        UpdateButtonState();
    }

    private void UpdateLevelRequirementDisplay()
    {
        if (levelRequirementText == null || seedItem?.seedData == null)
            return;

        int requiredLevel = GetRequiredLevelForSeed(seedItem.seedData.itemName);
        int currentLevel = LevelSystem.Instance.GetCurrentLevel();

        if (requiredLevel > 0 && currentLevel < requiredLevel)
        {
            levelRequirementText.text = $"Yêu cầu cấp {requiredLevel}";
            levelRequirementText.color = Color.red;
            levelRequirementText.gameObject.SetActive(true);

            Debug.Log($" {seedItem.seedData.itemName} - Yêu cầu cấp {requiredLevel} (Hiện tại: {currentLevel})");
        }
        else if (requiredLevel > 0)
        {
            levelRequirementText.gameObject.SetActive(false);

            Debug.Log($" {seedItem.seedData.itemName} - Đã mở khóa!");

        }
        else
        {
            levelRequirementText.gameObject.SetActive(false);
        }
    }

    private int GetRequiredLevelForSeed(string seedName)
    {
        if (LevelSystem.Instance?.LevelConfigData == null)
            return 0;

        foreach (var unlock in LevelSystem.Instance.LevelConfigData.levelUnlocks)
        {
            if (unlock.unlockedItemName == seedName && unlock.unlockType == LevelConfig.UnlockType.Seed)
            {
                return unlock.requiredLevel;
            }
        }

        return 0;
    }

    private void UpdateButtonState()
    {
        if (purchaseButton != null && LoadDataManager.userInGame != null)
        {
            int requiredLevel = GetRequiredLevelForSeed(seedItem.seedData.itemName);
            int currentLevel = LevelSystem.Instance.GetCurrentLevel();
            bool canAfford = LoadDataManager.userInGame.Gold >= seedItem.price;
            bool hasRequiredLevel = currentLevel >= requiredLevel;

            purchaseButton.interactable = canAfford && hasRequiredLevel;

            var buttonImage = purchaseButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                if (!hasRequiredLevel)
                {
                    buttonImage.color = Color.red; 
                }
                else if (!canAfford)
                {
                    buttonImage.color = Color.gray; 
                }
                else
                {
                    buttonImage.color = Color.white; 
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