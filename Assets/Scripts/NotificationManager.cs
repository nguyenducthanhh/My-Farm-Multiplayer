using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance { get; private set; }

    [SerializeField] private Text rewardText;

    private Queue<(string message, float duration)> notificationQueue = new Queue<(string, float)>();
    private Coroutine currentNotificationCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static void ShowReward(string message)
    {
        ShowReward(message, 5f);
    }

    public static void ShowReward(string message, float duration)
    {
        if (Instance != null)
        {
            Instance.EnqueueNotification(message, duration);
        }
    }

    private void EnqueueNotification(string message, float duration)
    {
        notificationQueue.Enqueue((message, duration));

        if (currentNotificationCoroutine == null)
        {
            currentNotificationCoroutine = StartCoroutine(ProcessNotificationQueue());
        }
    }

    private IEnumerator ProcessNotificationQueue()
    {
        while (notificationQueue.Count > 0)
        {
            var (message, duration) = notificationQueue.Dequeue();

            if (rewardText != null)
            {
                rewardText.text = message;
                rewardText.gameObject.SetActive(true);
                Debug.Log($" Notification: {message}");
            }

            yield return new WaitForSeconds(duration);

            if (rewardText != null)
            {
                rewardText.gameObject.SetActive(false);
                rewardText.text = "";
            }
        }

        currentNotificationCoroutine = null;
    }
}