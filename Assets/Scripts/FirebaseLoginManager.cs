 using Firebase.Auth;
using Firebase.Extensions;
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
    [Header("Switch form")]
    public Button buttonMoveToSignIn;
    public Button buttonMoveToRegister;

    public GameObject registerForm;
    public GameObject loginForm;
    private FirebaseAuth auth;
    private FirebaseDatabaseManager databaseManager;
    private void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
       
        buttonRegister.onClick.AddListener(RegisterAccountWithFirebase);
        buttonLogin.onClick.AddListener(SigninAccountWithFirebase);
       
        buttonMoveToRegister.onClick.AddListener(SwitchForm);
        buttonMoveToSignIn.onClick.AddListener(SwitchForm);
    }

    private void Awake()
    {
        databaseManager = GetComponent<FirebaseDatabaseManager>();
    }

    public void RegisterAccountWithFirebase()
    {
        string email = ipRegisterEmail.text;
        string password = ipRegisterPassword.text;

        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if(task.IsCanceled)
            {
                Debug.Log("Dang ky bi huy");
                return;
            }
            if(task.IsFaulted)
            {
                Debug.Log("Dang ky that bai");
                return;
            }
            if (task.IsCompleted)
            {
                Debug.Log("Dang ky thanh cong");
                Map mapInGame = new Map();  
                User userInGame = new User("", 100, 100, mapInGame);

                FirebaseUser firebaseUser = task.Result.User;

                databaseManager.WriteDatabase("Users/" + firebaseUser.UserId, userInGame.ToString());
                LoadingManager.NEXT_SCENE =("PlayScene");
                SceneManager.LoadScene("LoadingScene");
            }
        });
    }

    public void SigninAccountWithFirebase()
    {
        string email = ipLoginEmail.text;
        string password = ipLoginPassword.text;

        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.Log("Dang nhap bi huy");
                return;
            }

            if (task.IsFaulted)
            {
                Debug.Log("Dang nhap that bai");
            }
            if (task.IsCompleted)
            {
                Debug.Log("Dang nhap thanh cong");
                FirebaseUser user = task.Result.User;
                LoadingManager.NEXT_SCENE = ("PlayScene");
                SceneManager.LoadScene("LoadingScene");
            }
        });
    }

    public void SwitchForm()
    { 
        loginForm.SetActive(!loginForm.activeSelf);
        registerForm.SetActive(!registerForm.activeSelf);
    }
}
