using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Firebase.Extensions;

public class PlayerProfilePanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject profilePanel;      
    [SerializeField] private Button openProfileButton;     
    [SerializeField] private Button closeProfileButton;    
    [SerializeField] private Button logoutButton;          
    [SerializeField] private PlayerMovement playerMovement; 
    [Header("Profile Display")]
    [SerializeField] private Text currentLevelText;        
    [SerializeField] private Text experienceText;   

    private void Start()
    {
        if (openProfileButton != null)
            openProfileButton.onClick.AddListener(OpenProfile);

        if (closeProfileButton != null)
            closeProfileButton.onClick.AddListener(CloseProfile);

        if (logoutButton != null)
        {
            logoutButton.onClick.AddListener(OnLogoutClicked);
            Debug.Log(" Logout button listener added");
        }
        else
        {
            Debug.LogError(" logoutButton is NOT assigned in Inspector!");
        }

        if (profilePanel != null)
            profilePanel.SetActive(false);
    }

    public void OpenProfile()
    {
        if (profilePanel != null)
        {
            profilePanel.SetActive(true);
            RefreshProfileDisplay();
            Debug.Log(" Profile panel opened");
        }
    }

    public void CloseProfile()
    {
        if (profilePanel != null)
        {
            profilePanel.SetActive(false);
            Debug.Log(" Profile panel closed");
        }
    }

    private void OnLogoutClicked()
    {
        Debug.Log(" Logout button clicked");

        SavePlayerPositionBeforeLogout();

        LogoutFromFirebase(true);
    }

    public void LogoutBecauseAccountLoggedInElsewhere()
    {
        Debug.LogWarning(" Account logged in elsewhere. Logging out this device.");

        SavePlayerPositionBeforeLogout();
        LogoutFromFirebase(false);
    }

    private void SavePlayerPositionBeforeLogout()
    {
        if (LoadDataManager.userInGame == null || LoadDataManager.firebaseUser == null)
        {
            Debug.LogWarning(" User data or Firebase user is null!");
            return;
        }

        
        if (playerMovement != null)
        {
            playerMovement.SavePlayerPositionBeforeLogout();
            Debug.Log(" Position saved before logout");
        }
        else
        {
            Debug.LogWarning(" PlayerMovement not found in scene!");
        }
    }

    private void LogoutFromFirebase(bool clearCurrentSession)
    {
        if (clearCurrentSession)
        {
            AccountSessionWatcher.ClearCurrentSessionAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                    Debug.LogWarning(" Could not clear active session before logout: " + task.Exception);

                CompleteLogoutFromFirebase();
            });

            return;
        }

        CompleteLogoutFromFirebase();
    }

    private void CompleteLogoutFromFirebase()
    {
        Firebase.Auth.FirebaseAuth.DefaultInstance.SignOut();
        Debug.Log(" User logged out from Firebase");

        SceneManager.LoadScene("LoginScene");
    }

    private void Update()
    {
        if (profilePanel != null && profilePanel.activeInHierarchy)
        {
            if (LevelSystem.Instance != null && LoadDataManager.userInGame != null)
            {
                RefreshProfileDisplay();
            }
        }
    }

    private void RefreshProfileDisplay()
    {
        if (LoadDataManager.userInGame == null || LevelSystem.Instance == null)
            return;

        if (currentLevelText != null)
            currentLevelText.text = $"{LevelSystem.Instance.GetCurrentLevel()}";

        if (experienceText != null)
        {
            int currentExp = LevelSystem.Instance.GetCurrentExperience();
            int nextLevelExp = LevelSystem.Instance.GetExperienceToNextLevel();
            experienceText.text = $"{currentExp}/{nextLevelExp}";
        }
    }
}
