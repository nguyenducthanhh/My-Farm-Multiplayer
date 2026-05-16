using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerProfilePanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject profilePanel;      // Kéo thả ProfilePanel vào đây
    [SerializeField] private Button openProfileButton;     // Kéo thả button mở vào đây
    [SerializeField] private Button closeProfileButton;    // Kéo thả button đóng vào đây
    [SerializeField] private Button logoutButton;          // ✅ THÊM: Kéo thả button đăng xuất vào đây
    [SerializeField] private PlayerMovement playerMovement; // ✅ THÊM: Kéo thả Player vào đây để gọi hàm save position
    [Header("Profile Display")]
    [SerializeField] private Text currentLevelText;        // Kéo thả Text Level vào đây
    [SerializeField] private Text experienceText;          // Kéo thả Text Exp vào đây

    private void Start()
    {
        // ✅ Chỉ setup button listeners
        if (openProfileButton != null)
            openProfileButton.onClick.AddListener(OpenProfile);

        if (closeProfileButton != null)
            closeProfileButton.onClick.AddListener(CloseProfile);

        // ✅ THÊM: Setup logout button
        if (logoutButton != null)
        {
            logoutButton.onClick.AddListener(OnLogoutClicked);
            Debug.Log("✅ Logout button listener added");
        }
        else
        {
            Debug.LogError("❌ logoutButton is NOT assigned in Inspector!");
        }

        // Ban đầu ẩn trang cá nhân
        if (profilePanel != null)
            profilePanel.SetActive(false);
    }

    // ✅ Mở trang cá nhân
    public void OpenProfile()
    {
        if (profilePanel != null)
        {
            profilePanel.SetActive(true);
            RefreshProfileDisplay();
            Debug.Log("✅ Profile panel opened");
        }
    }

    // ✅ Đóng trang cá nhân
    public void CloseProfile()
    {
        if (profilePanel != null)
        {
            profilePanel.SetActive(false);
            Debug.Log("✅ Profile panel closed");
        }
    }

    // ✅ THÊM: Hàm logout
    private void OnLogoutClicked()
    {
        Debug.Log("🔓 Logout button clicked");

        // ✅ BƯỚC 1: Lưu vị trí trước khi logout
        SavePlayerPositionBeforeLogout();

        // ✅ BƯỚC 2: Logout khỏi Firebase
        LogoutFromFirebase();
    }

    // ✅ THÊM: Lưu vị trí trước khi logout
    private void SavePlayerPositionBeforeLogout()
    {
        if (LoadDataManager.userInGame == null || LoadDataManager.firebaseUser == null)
        {
            Debug.LogWarning("⚠️ User data or Firebase user is null!");
            return;
        }

        // ✅ Gọi hàm save position từ PlayerMovement
        
        if (playerMovement != null)
        {
            playerMovement.SavePlayerPositionBeforeLogout();
            Debug.Log("✅ Position saved before logout");
        }
        else
        {
            Debug.LogWarning("⚠️ PlayerMovement not found in scene!");
        }
    }

    // ✅ THÊM: Logout khỏi Firebase
    private void LogoutFromFirebase()
    {
        Firebase.Auth.FirebaseAuth.DefaultInstance.SignOut();
        Debug.Log("✅ User logged out from Firebase");

        // ✅ Chuyển về Login Scene
        SceneManager.LoadScene("LoginScene"); // Thay tên scene của bạn nếu khác
    }

    private void Update()
    {
        // ✅ Tự động refresh khi trang đang mở
        if (profilePanel != null && profilePanel.activeInHierarchy)
        {
            if (LevelSystem.Instance != null && LoadDataManager.userInGame != null)
            {
                RefreshProfileDisplay();
            }
        }
    }

    // ✅ Cập nhật thông tin hiển thị
    private void RefreshProfileDisplay()
    {
        if (LoadDataManager.userInGame == null || LevelSystem.Instance == null)
            return;

        // 🎯 Cấp hiện tại
        if (currentLevelText != null)
            currentLevelText.text = $"{LevelSystem.Instance.GetCurrentLevel()}";

        // ⭐ Kinh nghiệm hiện tại
        if (experienceText != null)
        {
            int currentExp = LevelSystem.Instance.GetCurrentExperience();
            int nextLevelExp = LevelSystem.Instance.GetExperienceToNextLevel();
            experienceText.text = $"{currentExp}/{nextLevelExp}";
        }
    }
}