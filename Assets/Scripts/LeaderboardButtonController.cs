using UnityEngine;
using UnityEngine.UI;

public class LeaderboardButtonController : MonoBehaviour
{
    [SerializeField] private Button openLeaderboardButton;

    private void Start()
    {
        if (openLeaderboardButton != null)
            openLeaderboardButton.onClick.AddListener(OpenLeaderboard);
    }

    private void OpenLeaderboard()
    {
        if (LeaderboardManager.Instance != null)
        {
            LeaderboardManager.Instance.OpenLeaderboard();
            Debug.Log(" Opened leaderboard");
        }
    }
}