//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;

//public class UsernameWizard : MonoBehaviour
//{
//    public GameObject usernameWizard;
//    public GameObject storageBox;
//    public Button buttonOk;
//    public InputField inputUsername;

//    [SerializeField] private FirebaseDatabaseManager databaseManager;
//    public Text username;
//    public Text gold;
//    void Start()
//    {
//        if (LoadDataManager.userInGame.Name == "")
//        {
//            usernameWizard.SetActive(true);
//            storageBox.SetActive(false);
//        }
//        else
//        {
//            usernameWizard.SetActive(false);
//            username.text = LoadDataManager.userInGame.Name;
//        }
//        gold.text = "Gold: " + LoadDataManager.userInGame.Gold.ToString();
//        buttonOk.onClick.AddListener(SetNewUsername);
//    }

//    void Update()
//    {

//    }

//    public void SetNewUsername()
//    {
//            LoadDataManager.userInGame.Name = inputUsername.text;

//            databaseManager.WriteDatabase("Users/" + LoadDataManager.firebaseUser.UserId, LoadDataManager.userInGame.ToString());

//            username.text = inputUsername.text;

//            usernameWizard.SetActive(false);
//            storageBox.SetActive(true);

//    }

//}
//27/3
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using Firebase.Extensions;

public class UsernameWizard : MonoBehaviour
{
    public GameObject usernameWizard;
    public GameObject storageBox;
    public Button buttonOk;
    public InputField inputUsername;
    [SerializeField] GameObject seedSlot;


    [SerializeField] private FirebaseDatabaseManager databaseManager;
    public Text username;
    public Text gold;

    public static bool IsEnteringUsername { get; private set; } = false;

    public static UsernameWizard Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void Start()
    {
        // ĐỢI DATA LOAD XONG TRƯỚC KHI SETUP UI
        StartCoroutine(WaitForUserDataAndSetup());
    }

    private IEnumerator WaitForUserDataAndSetup()
    {
        // Đợi cho đến khi LoadDataManager.userInGame được load
        while (LoadDataManager.userInGame == null)
        {
            Debug.Log("UsernameWizard: Waiting for user data to load...");
            yield return new WaitForSeconds(0.1f);
        }

        // Bây giờ mới setup UI
        SetupUI();

        StartCoroutine(AutoUpdateGold());

    }

    private void SetupUI()
    {
        // KIỂM TRA NULL TRƯỚC KHI SỬ DỤNG
        if (LoadDataManager.userInGame == null)
        {
            Debug.LogError("LoadDataManager.userInGame is still null!");
            return;
        }

        if (string.IsNullOrEmpty(LoadDataManager.userInGame.Name))
        {
            usernameWizard.SetActive(true);
            storageBox.SetActive(false);
            seedSlot.SetActive(false);

            IsEnteringUsername = true;

            //if (inputUsername != null)
            //{
            //    inputUsername.onSelect.AddListener(OnUsernameInputFocused);
            //    inputUsername.onDeselect.AddListener(OnUsernameInputUnfocused);
            //}
        }
        else
        {
            usernameWizard.SetActive(false);
            IsEnteringUsername = false;
            if (username != null)
                username.text = LoadDataManager.userInGame.Name;
        }

        //if (gold != null)
        //    gold.text = "Gold: " + LoadDataManager.userInGame.Gold.ToString();
        RefreshGold();

        if (buttonOk != null)
            buttonOk.onClick.AddListener(SetNewUsername);
    }
    //private void OnUsernameInputFocused(string value)
    //{
    //    IsEnteringUsername = true;
    //    Debug.Log("🔒 Username input focused - disabling farm actions");
    //}
    public void RefreshGold()
    {
        if (gold != null && LoadDataManager.userInGame != null)
        {
            gold.text = "Gold: " + LoadDataManager.userInGame.Gold.ToString();
        }
    }
    //private void OnUsernameInputUnfocused(string value)
    //{
    //    // Chỉ disable khi vẫn trong username wizard
    //    if (usernameWizard.activeInHierarchy)
    //    {
    //        IsEnteringUsername = true; // Vẫn trong wizard
    //    }
    //    else
    //    {
    //        IsEnteringUsername = false;
    //    }
    //    Debug.Log($"🔓 Username input unfocused - IsEnteringUsername: {IsEnteringUsername}");
    //}
    private IEnumerator AutoUpdateGold()
    {
        while (true)
        {
            RefreshGold();
            yield return new WaitForSeconds(1f); // Cập nhật mỗi giây
        }
    }

    public static void UpdateGoldDisplay()
    {
        if (Instance != null)
        {
            Instance.RefreshGold();
        }
    }
    public void SetNewUsername()
    {
        if (LoadDataManager.userInGame == null)
        {
            Debug.LogError("Cannot set username - user data is null!");
            return;
        }

        if (inputUsername == null || string.IsNullOrEmpty(inputUsername.text))
        {
            Debug.LogWarning("Username input is empty!");
            return;
        }

        LoadDataManager.userInGame.Name = inputUsername.text;

        // ✅ CHỈ CẬP NHẬT NAME FIELD, KHÔNG GHI ĐÈ TOÀN BỘ USER
        FirebaseDatabase.DefaultInstance
            .GetReference("Users")
            .Child(LoadDataManager.firebaseUser.UserId)
            .Child("Name")  // ← CHỈ CẬP NHẬT FIELD NAME
            .SetValueAsync(inputUsername.text)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log($"✅ Username saved successfully: '{inputUsername.text}'");
                }
                else
                {
                    Debug.LogError("❌ Failed to save username: " + task.Exception);
                }
            });

        // Update UI
        if (username != null)
            username.text = inputUsername.text;

        usernameWizard.SetActive(false);
        storageBox.SetActive(true);
        seedSlot.SetActive(true);

        IsEnteringUsername = false;
        Debug.Log("✅ Username setup completed - farm actions enabled");
    }
}
