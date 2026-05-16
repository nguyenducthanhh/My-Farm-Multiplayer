//// using Firebase.Auth;
////using Firebase.Database;
////using Firebase.Extensions;
////using Newtonsoft.Json;
////using System.Collections;
////using System.Collections.Generic;
////using System.Reflection.Emit;
////using UnityEngine;
////using UnityEngine.SceneManagement;
////using UnityEngine.UI;

////public class FirebaseLoginManager : MonoBehaviour
////{
////    [Header("Register")]
////    public InputField ipRegisterEmail;
////    public InputField ipRegisterPassword;
////    public Button buttonRegister;
////    [Header("Sign In")]
////    public InputField ipLoginEmail;
////    public InputField ipLoginPassword;
////    public Button buttonLogin;
////    public string gold;
////    [Header("Switch form")]
////    public Button buttonMoveToSignIn;
////    public Button buttonMoveToRegister;

////    public GameObject registerForm;
////    public GameObject loginForm;
////    private FirebaseAuth auth;
////    private FirebaseDatabaseManager databaseManager;
////    private void Start()
////    {
////        auth = FirebaseAuth.DefaultInstance;

////        buttonRegister.onClick.AddListener(RegisterAccountWithFirebase);
////        buttonLogin.onClick.AddListener(SigninAccountWithFirebase);

////        buttonMoveToRegister.onClick.AddListener(SwitchForm);
////        buttonMoveToSignIn.onClick.AddListener(SwitchForm);
////    }

////    private void Awake()
////    {
////        databaseManager = GetComponent<FirebaseDatabaseManager>();
////    }

////    public void RegisterAccountWithFirebase()
////    {
////        string email = ipRegisterEmail.text;
////        string password = ipRegisterPassword.text;

////        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
////        {
////            if (task.IsCanceled)
////            {
////                Debug.Log("Dang ky bi huy");
////                return;
////            }
////            if (task.IsFaulted)
////            {
////                Debug.Log("Dang ky that bai");
////                return;
////            }
////            if (task.IsCompleted)
////            {
////                Debug.Log("Dang ky thanh cong");
////                FirebaseUser firebaseUser = task.Result.User;

////                // SỬ DỤNG CÁCH MỚI - TẠO TỪNG FIELD RIÊNG BIỆT
////                CreateUserWithSeparateFields(firebaseUser.UserId);

////                LoadingManager.NEXT_SCENE = ("PlayScene");
////                SceneManager.LoadScene("LoadingScene");
////            }
////        });
////    }

////    private void CreateUserWithSeparateFields(string userId)
////    {
////        var userRef = FirebaseDatabase.DefaultInstance.GetReference("Users").Child(userId);

////        // 1. Lưu Name và Gold riêng biệt
////        userRef.Child("Name").SetValueAsync("");
////        userRef.Child("Gold").SetValueAsync(gold);

////        // 2. Tạo và lưu Map với đầy đủ tilemap data
////        CreateAndSaveDefaultMap(userRef);

////        // 3. Tạo Inventory rỗng
////        List<InventoryItems> emptyInventory = new List<InventoryItems>();
////        string inventoryJson = JsonConvert.SerializeObject(emptyInventory);
////        userRef.Child("Inventory").SetRawJsonValueAsync(inventoryJson);

////        // 4. Tạo Plants rỗng
////        List<PlantTileData> emptyPlants = new List<PlantTileData>();
////        string plantsJson = JsonConvert.SerializeObject(emptyPlants);
////        userRef.Child("Plants").SetRawJsonValueAsync(plantsJson);

////        Debug.Log($"User created with separate fields - Gold: {gold}");
////    }

////    private void CreateAndSaveDefaultMap(DatabaseReference userRef)
////    {
////        // Tạo map giống như trong TileMapManager.WriteAllTileMapToFirebase()
////        List<TilemapDetail> tilemaps = new List<TilemapDetail>();

////        // Tạo default map bounds (có thể adjust theo game của bạn)
////        for (int x = -3; x <= 2; x++)
////        {
////            for (int y = -4; y <= 3; y++)
////            {
////                TilemapDetail tm_detail = new TilemapDetail(x, y, TileMapState.Grass, System.DateTime.Now);
////                tilemaps.Add(tm_detail);
////            }
////        }

////        Map defaultMap = new Map(tilemaps);
////        string mapJson = JsonConvert.SerializeObject(defaultMap);
////        userRef.Child("MapInGame").SetRawJsonValueAsync(mapJson);

////        Debug.Log($"Default map created with {tilemaps.Count} tiles");
////    }

////    public void SigninAccountWithFirebase()
////    {
////        string email = ipLoginEmail.text;
////        string password = ipLoginPassword.text;

////        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
////        {
////            if (task.IsCanceled)
////            {
////                Debug.Log("Dang nhap bi huy");
////                return;
////            }

////            if (task.IsFaulted)
////            {
////                Debug.Log("Dang nhap that bai");
////            }
////            if (task.IsCompleted)
////            {
////                Debug.Log("Dang nhap thanh cong");
////                FirebaseUser user = task.Result.User;
////                LoadingManager.NEXT_SCENE = ("PlayScene");
////                SceneManager.LoadScene("LoadingScene");
////            }
////        });
////    }

////    public void SwitchForm()
////    { 
////        loginForm.SetActive(!loginForm.activeSelf);
////        registerForm.SetActive(!registerForm.activeSelf);
////    }
////}
//using Firebase.Auth;
//using Firebase.Database;
//using Firebase.Extensions;
//using Newtonsoft.Json;
//using System.Collections;
//using System.Collections.Generic;
//using System.Reflection.Emit;
//using UnityEngine;
//using UnityEngine.SceneManagement;
//using UnityEngine.UI;

//public class FirebaseLoginManager : MonoBehaviour
//{
//    [Header("Register")]
//    public InputField ipRegisterEmail;
//    public InputField ipRegisterPassword;
//    public Button buttonRegister;

//    [Header("Sign In")]
//    public InputField ipLoginEmail;
//    public InputField ipLoginPassword;
//    public Button buttonLogin;
//    public string gold;

//    [Header("Switch form")]
//    public Button buttonMoveToSignIn;
//    public Button buttonMoveToRegister;

//    public GameObject registerForm;
//    public GameObject loginForm;

//    private FirebaseAuth auth;
//    private FirebaseDatabaseManager databaseManager;

//    private void Awake()
//    {
//        databaseManager = GetComponent<FirebaseDatabaseManager>();
//    }

//    private void Start()
//    {
//        auth = FirebaseAuth.DefaultInstance;

//        buttonRegister.onClick.AddListener(RegisterAccountWithFirebase);
//        buttonLogin.onClick.AddListener(SigninAccountWithFirebase);

//        buttonMoveToRegister.onClick.AddListener(ShowRegisterForm);
//        buttonMoveToSignIn.onClick.AddListener(ShowLoginForm);

//        // ✅ THÊM: Mở form đăng nhập khi bắt đầu game
//        ShowLoginForm();
//        Debug.Log("🔐 Login form opened at startup");
//    }

//    // ✅ THÊM: Hiển thị form đăng nhập (ẩn form đăng ký)
//    public void ShowLoginForm()
//    {
//        loginForm.SetActive(true);
//        registerForm.SetActive(false);
//        Debug.Log("📝 Showing login form");
//    }

//    // ✅ THÊM: Hiển thị form đăng ký (ẩn form đăng nhập)
//    public void ShowRegisterForm()
//    {
//        loginForm.SetActive(false);
//        registerForm.SetActive(true);
//        Debug.Log("📝 Showing register form");
//    }

//    // ✅ CỐ ĐỊNH: Sửa lại tên method từ SwitchForm
//    public void SwitchForm()
//    {
//        loginForm.SetActive(!loginForm.activeSelf);
//        registerForm.SetActive(!registerForm.activeSelf);
//    }

//    public void RegisterAccountWithFirebase()
//    {
//        string email = ipRegisterEmail.text;
//        string password = ipRegisterPassword.text;

//        // ✅ THÊM: Validate input
//        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
//        {
//            Debug.LogWarning("⚠️ Email or password is empty!");
//            return;
//        }

//        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
//        {
//            if (task.IsCanceled)
//            {
//                Debug.Log("❌ Đăng ký bị hủy");
//                return;
//            }
//            if (task.IsFaulted)
//            {
//                Debug.Log("❌ Đăng ký thất bại: " + task.Exception?.Message);
//                return;
//            }
//            if (task.IsCompleted)
//            {
//                Debug.Log("✅ Đăng ký thành công");
//                FirebaseUser firebaseUser = task.Result.User;

//                // SỬ DỤNG CÁCH MỚI - TẠO TỪNG FIELD RIÊNG BIỆT
//                CreateUserWithSeparateFields(firebaseUser.UserId);

//                LoadingManager.NEXT_SCENE = ("PlayScene");
//                SceneManager.LoadScene("LoadingScene");
//            }
//        });
//    }

//    private void CreateUserWithSeparateFields(string userId)
//    {
//        var userRef = FirebaseDatabase.DefaultInstance.GetReference("Users").Child(userId);

//        // 1. Lưu Name và Gold riêng biệt
//        userRef.Child("Name").SetValueAsync("");
//        userRef.Child("Gold").SetValueAsync(gold);

//        // 2. Tạo và lưu Map với đầy đủ tilemap data
//        CreateAndSaveDefaultMap(userRef);

//        // 3. Tạo Inventory rỗng
//        List<InventoryItems> emptyInventory = new List<InventoryItems>();
//        string inventoryJson = JsonConvert.SerializeObject(emptyInventory);
//        userRef.Child("Inventory").SetRawJsonValueAsync(inventoryJson);

//        // 4. Tạo Plants rỗng
//        List<PlantTileData> emptyPlants = new List<PlantTileData>();
//        string plantsJson = JsonConvert.SerializeObject(emptyPlants);
//        userRef.Child("Plants").SetRawJsonValueAsync(plantsJson);

//        // ✅ THÊM: Tạo Level data mới
//        CreateAndSaveDefaultLevel(userRef);

//        Debug.Log($"✅ User created with separate fields - Gold: {gold}");
//    }

//    // ✅ THÊM: Method tạo Level data default
//    private void CreateAndSaveDefaultLevel(DatabaseReference userRef)
//    {
//        var levelData = new LevelSystem.LevelData
//        {
//            currentLevel = 1,
//            currentExperience = 0,
//            experienceToNextLevel = 100,
//            unlockedItems = new List<string>()
//        };

//        string levelJson = JsonConvert.SerializeObject(levelData);
//        userRef.Child("Level").SetRawJsonValueAsync(levelJson);

//        Debug.Log($"✅ Default level created - Level: 1, EXP: 0");
//    }

//    private void CreateAndSaveDefaultMap(DatabaseReference userRef)
//    {
//        // Tạo map giống như trong TileMapManager.WriteAllTileMapToFirebase()
//        List<TilemapDetail> tilemaps = new List<TilemapDetail>();

//        // Tạo default map bounds (có thể adjust theo game của bạn)
//        for (int x = -3; x <= 2; x++)
//        {
//            for (int y = -4; y <= 3; y++)
//            {
//                TilemapDetail tm_detail = new TilemapDetail(x, y, TileMapState.Grass, System.DateTime.Now);
//                tilemaps.Add(tm_detail);
//            }
//        }

//        Map defaultMap = new Map(tilemaps);
//        string mapJson = JsonConvert.SerializeObject(defaultMap);
//        userRef.Child("MapInGame").SetRawJsonValueAsync(mapJson);

//        Debug.Log($"✅ Default map created with {tilemaps.Count} tiles");
//    }

//    public void SigninAccountWithFirebase()
//    {
//        string email = ipLoginEmail.text;
//        string password = ipLoginPassword.text;

//        // ✅ THÊM: Validate input
//        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
//        {
//            Debug.LogWarning("⚠️ Email or password is empty!");
//            return;
//        }

//        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
//        {
//            if (task.IsCanceled)
//            {
//                Debug.Log("❌ Đăng nhập bị hủy");
//                return;
//            }

//            if (task.IsFaulted)
//            {
//                Debug.Log("❌ Đăng nhập thất bại: " + task.Exception?.Message);
//                return;
//            }

//            if (task.IsCompleted)
//            {
//                Debug.Log("✅ Đăng nhập thành công");
//                FirebaseUser user = task.Result.User;
//                LoadingManager.NEXT_SCENE = ("PlayScene");
//                SceneManager.LoadScene("LoadingScene");
//            }
//        });
//    }
//}
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FirebaseLoginManager : MonoBehaviour
{
    [Header("Register")]
    public InputField ipRegisterEmail;
    public InputField ipRegisterPassword;
    public Button buttonRegister;

    [Header("Sign In")]
    public InputField ipLoginEmail;
    public InputField ipLoginPassword;
    public Button buttonLogin;
    public string gold;

    [Header("Switch form")]
    public Button buttonMoveToSignIn;
    public Button buttonMoveToRegister;

    public GameObject registerForm;
    public GameObject loginForm;

    private FirebaseAuth auth;
    private FirebaseDatabaseManager databaseManager;

    private void Awake()
    {
        databaseManager = GetComponent<FirebaseDatabaseManager>();
    }

    private void Start()
    {
        auth = FirebaseAuth.DefaultInstance;

        buttonRegister.onClick.AddListener(RegisterAccountWithFirebase);
        buttonLogin.onClick.AddListener(SigninAccountWithFirebase);

        buttonMoveToRegister.onClick.AddListener(ShowRegisterForm);
        buttonMoveToSignIn.onClick.AddListener(ShowLoginForm);

        ShowLoginForm();
        Debug.Log("🔐 Login form opened at startup");
    }

    public void ShowLoginForm()
    {
        loginForm.SetActive(true);
        registerForm.SetActive(false);
        Debug.Log("📝 Showing login form");
    }

    public void ShowRegisterForm()
    {
        loginForm.SetActive(false);
        registerForm.SetActive(true);
        Debug.Log("📝 Showing register form");
    }

    public void SwitchForm()
    {
        loginForm.SetActive(!loginForm.activeSelf);
        registerForm.SetActive(!registerForm.activeSelf);
    }

    public void RegisterAccountWithFirebase()
    {
        string email = ipRegisterEmail.text;
        string password = ipRegisterPassword.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            Debug.LogWarning("⚠️ Email or password is empty!");
            return;
        }

        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.Log("❌ Đăng ký bị hủy");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.Log("❌ Đăng ký thất bại: " + task.Exception?.Message);
                return;
            }
            if (task.IsCompleted)
            {
                Debug.Log("✅ Đăng ký thành công");
                FirebaseUser firebaseUser = task.Result.User;

                CreateUserWithSeparateFields(firebaseUser.UserId);

                LoadingManager.NEXT_SCENE = ("PlayScene");
                SceneManager.LoadScene("LoadingScene");
            }
        });
    }

    private void CreateUserWithSeparateFields(string userId)
    {
        var userRef = FirebaseDatabase.DefaultInstance.GetReference("Users").Child(userId);

        // 1. Lưu Name và Gold riêng biệt
        userRef.Child("Name").SetValueAsync("");
        userRef.Child("Gold").SetValueAsync(gold);

        // 2. Tạo và lưu Map với đầy đủ tilemap data
        CreateAndSaveDefaultMap(userRef);

        // 3. Tạo Inventory rỗng
        List<InventoryItems> emptyInventory = new List<InventoryItems>();
        string inventoryJson = JsonConvert.SerializeObject(emptyInventory);
        userRef.Child("Inventory").SetRawJsonValueAsync(inventoryJson);

        // 4. Tạo Plants rỗng
        List<PlantTileData> emptyPlants = new List<PlantTileData>();
        string plantsJson = JsonConvert.SerializeObject(emptyPlants);
        userRef.Child("Plants").SetRawJsonValueAsync(plantsJson);

        // 5. Tạo Level data mới
        CreateAndSaveDefaultLevel(userRef);

        // ✅ THÊM: Tạo vị trí spawn mặc định
        CreateAndSaveDefaultPosition(userRef);

        Debug.Log($"✅ User created with separate fields - Gold: {gold}");
    }

    // ✅ THÊM: Method tạo vị trí spawn default
    private void CreateAndSaveDefaultPosition(DatabaseReference userRef)
    {
        var playerPosition = new User.PlayerPosition
        {
            x = 8.34855f,  // Vị trí spawn X
            y = 0,  // Vị trí spawn Y
            z = 0f      // Vị trí spawn Z
        };

        string positionJson = JsonConvert.SerializeObject(playerPosition);
        userRef.Child("LastPosition").SetRawJsonValueAsync(positionJson);

        Debug.Log($"✅ Default spawn position created: ({playerPosition.x}, {playerPosition.y})");
    }

    private void CreateAndSaveDefaultLevel(DatabaseReference userRef)
    {
        var levelData = new LevelSystem.LevelData
        {
            currentLevel = 1,
            currentExperience = 0,
            experienceToNextLevel = 100,
            unlockedItems = new List<string>()
        };

        string levelJson = JsonConvert.SerializeObject(levelData);
        userRef.Child("Level").SetRawJsonValueAsync(levelJson);

        Debug.Log($"✅ Default level created - Level: 1, EXP: 0");
    }

    private void CreateAndSaveDefaultMap(DatabaseReference userRef)
    {
        List<TilemapDetail> tilemaps = new List<TilemapDetail>();

        for (int x = -3; x <= 2; x++)
        {
            for (int y = -4; y <= 3; y++)
            {
                TilemapDetail tm_detail = new TilemapDetail(x, y, TileMapState.Grass, System.DateTime.Now);
                tilemaps.Add(tm_detail);
            }
        }

        Map defaultMap = new Map(tilemaps);
        string mapJson = JsonConvert.SerializeObject(defaultMap);
        userRef.Child("MapInGame").SetRawJsonValueAsync(mapJson);

        Debug.Log($"✅ Default map created with {tilemaps.Count} tiles");
    }

    public void SigninAccountWithFirebase()
    {
        string email = ipLoginEmail.text;
        string password = ipLoginPassword.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            Debug.LogWarning("⚠️ Email or password is empty!");
            return;
        }

        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.Log("❌ Đăng nhập bị hủy");
                return;
            }

            if (task.IsFaulted)
            {
                Debug.Log("❌ Đăng nhập thất bại: " + task.Exception?.Message);
                return;
            }

            if (task.IsCompleted)
            {
                Debug.Log("✅ Đăng nhập thành công");
                FirebaseUser user = task.Result.User;
                LoadingManager.NEXT_SCENE = ("PlayScene");
                SceneManager.LoadScene("LoadingScene");
            }
        });
    }
}