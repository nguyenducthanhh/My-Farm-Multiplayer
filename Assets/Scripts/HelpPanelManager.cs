using UnityEngine;
using UnityEngine.UI;

public class HelpPanelManager : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private GameObject helpPanel;
    [SerializeField] private Button helpButton;        // Nút Help (dấu ?)
    [SerializeField] private Button closeButton;       // Nút Đóng (Đóng)

    [Header("Settings")]
    [SerializeField] private bool closeOnBackgroundClick = true;

    private CanvasGroup canvasGroup;
    private bool isPanelOpen = false;

    private void Start()
    {
        // ✅ Lấy CanvasGroup để tối ưu hiệu ứng fade
        if (helpPanel != null)
            canvasGroup = helpPanel.GetComponent<CanvasGroup>();

        // ✅ Gán sự kiện cho các nút
        if (helpButton != null)
            helpButton.onClick.AddListener(OpenHelpPanel);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseHelpPanel);

        // ✅ Ẩn panel khi start
        if (helpPanel != null)
            helpPanel.SetActive(false);

        isPanelOpen = false;

        // ✅ Nếu có background click để đóng
        if (closeOnBackgroundClick)
        {
            Image panelImage = helpPanel?.GetComponent<Image>();
            if (panelImage != null)
            {
                Button panelButton = helpPanel.AddComponent<Button>();
                panelButton.onClick.AddListener(CloseHelpPanel);
            }
        }
    }

    /// <summary>
    /// Mở panel Help
    /// </summary>
    public void OpenHelpPanel()
    {
        if (helpPanel == null)
        {
            Debug.LogWarning("⚠️ Help panel không được gán!");
            return;
        }

        helpPanel.SetActive(true);
        isPanelOpen = true;

        // ✅ Fade in effect
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            StartCoroutine(FadePanel(0f, 1f, 0.3f));
        }

        Debug.Log("📖 Help panel mở");
    }

    /// <summary>
    /// Đóng panel Help
    /// </summary>
    public void CloseHelpPanel()
    {
        if (helpPanel == null || !isPanelOpen)
            return;

        // ✅ Fade out effect
        if (canvasGroup != null)
        {
            StartCoroutine(FadePanel(1f, 0f, 0.3f));
        }
        else
        {
            helpPanel.SetActive(false);
        }

        isPanelOpen = false;

        Debug.Log("📖 Help panel đóng");
    }

    /// <summary>
    /// Hiệu ứng Fade (mở/đóng dần)
    /// </summary>
    private System.Collections.IEnumerator FadePanel(float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = endAlpha;

        // ✅ Nếu fade out → Ẩn panel
        if (endAlpha == 0f)
            helpPanel.SetActive(false);
    }

    /// <summary>
    /// Bật/Tắt panel (Toggle)
    /// </summary>
}