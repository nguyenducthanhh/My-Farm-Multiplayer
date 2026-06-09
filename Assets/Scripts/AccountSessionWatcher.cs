using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AccountSessionWatcher : MonoBehaviour
{
    private const string ActiveSessionField = "ActiveSessionId";
    private const string LocalSessionIdKey = "AccountSessionId";
    private const string LoginSceneName = "LoginScene";

    private static AccountSessionWatcher instance;

    private DatabaseReference activeSessionRef;
    private string listeningUserId;
    private bool logoutRequested;
    private bool isLoggingOut;

    public static string LocalSessionId => PlayerPrefs.GetString(LocalSessionIdKey, "");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
#if UNITY_EDITOR
        FirebaseDatabase.DefaultInstance.SetPersistenceEnabled(false);
#endif

        if (instance != null)
            return;

        GameObject watcherObject = new GameObject(nameof(AccountSessionWatcher));
        instance = watcherObject.AddComponent<AccountSessionWatcher>();
        DontDestroyOnLoad(watcherObject);
    }

    public static Task StartNewSessionAsync(string userId)
    {
        string sessionId = Guid.NewGuid().ToString("N");
        PlayerPrefs.SetString(LocalSessionIdKey, sessionId);
        PlayerPrefs.Save();

        return FirebaseDatabase.DefaultInstance
            .GetReference("Users")
            .Child(userId)
            .Child(ActiveSessionField)
            .SetValueAsync(sessionId);
    }

    public static Task ClearCurrentSessionAsync()
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        string sessionId = LocalSessionId;

        if (user == null || string.IsNullOrEmpty(sessionId))
            return Task.CompletedTask;

        DatabaseReference sessionRef = FirebaseDatabase.DefaultInstance
            .GetReference("Users")
            .Child(user.UserId)
            .Child(ActiveSessionField);

        return sessionRef.GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted || task.IsCanceled || task.Result?.Value?.ToString() != sessionId)
                return Task.CompletedTask;

            return sessionRef.RemoveValueAsync();
        }).Unwrap();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        StopListening();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == LoginSceneName)
        {
            StopListening();
            return;
        }

        TryStartListening();
    }

    private void Update()
    {
        if (!logoutRequested || isLoggingOut)
            return;

        isLoggingOut = true;
        StopListening();

        PlayerProfilePanel profilePanel = FindObjectOfType<PlayerProfilePanel>();
        if (profilePanel != null)
        {
            profilePanel.LogoutBecauseAccountLoggedInElsewhere();
        }
        else
        {
            FirebaseAuth.DefaultInstance.SignOut();
            SceneManager.LoadScene(LoginSceneName);
        }
    }

    private void TryStartListening()
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null || listeningUserId == user.UserId)
            return;

        StopListening();
        listeningUserId = user.UserId;

        if (string.IsNullOrEmpty(LocalSessionId))
        {
            StartNewSessionAsync(user.UserId).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError($"Could not create login session: {task.Exception}");
                    return;
                }

                AttachListener(user.UserId);
            });

            return;
        }

        AttachListener(user.UserId);
    }

    private void AttachListener(string userId)
    {
        activeSessionRef = FirebaseDatabase.DefaultInstance
            .GetReference("Users")
            .Child(userId)
            .Child(ActiveSessionField);

        activeSessionRef.ValueChanged += OnActiveSessionChanged;
        Debug.Log("Account session watcher started.");
    }

    private void StopListening()
    {
        if (activeSessionRef != null)
            activeSessionRef.ValueChanged -= OnActiveSessionChanged;

        activeSessionRef = null;
        listeningUserId = null;
        logoutRequested = false;
        isLoggingOut = false;
    }

    private void OnActiveSessionChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError($"Could not check login session: {args.DatabaseError.Message}");
            return;
        }

        string activeSessionId = args.Snapshot?.Value?.ToString();
        if (string.IsNullOrEmpty(activeSessionId))
            return;

        if (activeSessionId != LocalSessionId)
        {
            Debug.LogWarning("This account logged in on another device. Current device will be logged out.");
            logoutRequested = true;
        }
    }
}
