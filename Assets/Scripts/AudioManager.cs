//using UnityEngine;

//public class AudioManager : MonoBehaviour
//{
//    [SerializeField] private AudioSource musicSource;      // Nhạc nền
//    [SerializeField] private AudioSource effectSource;     // Âm thanh hiệu ứng

//    [Header("Farm Sounds")]
//    [SerializeField] private AudioClip hoeSound;           // Âm thanh xới đất
//    [SerializeField] private AudioClip waterSound;         // Âm thanh tưới nước
//    [SerializeField] private AudioClip plantSound;         // Âm thanh trồng cây
//    [SerializeField] private AudioClip harvestSound;       // Âm thanh gặt

//    [Header("Fishing Sounds")]
//    [SerializeField] private AudioClip castSound;          // Âm thanh thả câu
//    [SerializeField] private AudioClip reelSound;          // Âm thanh giật cá

//    [Header("UI Sounds")]
//    [SerializeField] private AudioClip coinSound;          // Âm thanh nhận tiền
//    [SerializeField] private AudioClip buttonClickSound;   // Âm thanh click nút
//    [SerializeField] private AudioClip successSound;       // Âm thanh thành công

//    public static AudioManager Instance { get; private set; }

//    private void Awake()
//    {
//        if (Instance == null)
//        {
//            Instance = this;
//        }
//        else
//        {
//            Destroy(gameObject);
//        }
//    }

//    // ✅ Farm Actions
//    public void PlayHoeSound()
//    {
//        effectSource.PlayOneShot(hoeSound);
//    }

//    public void PlayWaterSound()
//    {
//        effectSource.PlayOneShot(waterSound);
//    }

//    public void PlayPlantSound()
//    {
//        effectSource.PlayOneShot(plantSound);
//    }

//    public void PlayHarvestSound()
//    {
//        effectSource.PlayOneShot(harvestSound);
//    }

//    // ✅ Fishing Actions
//    public void PlayCastSound()
//    {
//        effectSource.PlayOneShot(castSound);
//    }

//    public void PlayReelSound()
//    {
//        effectSource.PlayOneShot(reelSound);
//    }

//    // ✅ UI Sounds
//    public void PlayCoinSound()
//    {
//        effectSource.PlayOneShot(coinSound);
//    }

//    public void PlayButtonClick()
//    {
//        effectSource.PlayOneShot(buttonClickSound);
//    }

//    public void PlaySuccessSound()
//    {
//        effectSource.PlayOneShot(successSound);
//    }

//    // ✅ Music Control
//    public void PlayMusic()
//    {
//        if (musicSource != null && !musicSource.isPlaying)
//            musicSource.Play();
//    }

//    public void StopMusic()
//    {
//        if (musicSource != null)
//            musicSource.Stop();
//    }

//    public void SetMusicVolume(float volume)
//    {
//        if (musicSource != null)
//            musicSource.volume = Mathf.Clamp01(volume);
//    }

//    public void SetEffectVolume(float volume)
//    {
//        if (effectSource != null)
//            effectSource.volume = Mathf.Clamp01(volume);
//    }
//}
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioSource musicSource;      // Nhạc nền
    [SerializeField] private AudioSource effectSource;     // Âm thanh hiệu ứng

    [Header("Farm Sounds")]
    [SerializeField] private AudioClip hoeSound;
    [SerializeField] private AudioClip waterSound;
    [SerializeField] private AudioClip plantSound;
    [SerializeField] private AudioClip harvestSound;

    [Header("Fishing Sounds")]
    [SerializeField] private AudioClip castSound;
    [SerializeField] private AudioClip reelSound;

    [Header("UI Sounds")]
    [SerializeField] private AudioClip coinSound;
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip successSound;

    // ✅ Mute control
    private bool isMuted = false;

    public static AudioManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("✅ AudioManager initialized");

            // ✅ Load mute state từ PlayerPrefs
            isMuted = PlayerPrefs.GetInt("AudioMuted", 0) == 1;
            ApplyMuteState();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ✅ Bật/tắt âm thanh toàn bộ
    public void ToggleMute()
    {
        isMuted = !isMuted;
        ApplyMuteState();

        // ✅ Lưu state
        PlayerPrefs.SetInt("AudioMuted", isMuted ? 1 : 0);
        PlayerPrefs.Save();

        Debug.Log(isMuted ? "🔇 MUTED" : "🔊 UNMUTED");
    }

    // ✅ Áp dụng trạng thái mute
    private void ApplyMuteState()
    {
        if (isMuted)
        {
            musicSource.mute = true;
            effectSource.mute = true;
        }
        else
        {
            musicSource.mute = false;
            effectSource.mute = false;
        }
    }

    // ✅ Kiểm tra trạng thái mute
    public bool IsMuted()
    {
        return isMuted;
    }

    // ✅ Farm Actions
    public void PlayHoeSound()
    {
        if (effectSource == null || hoeSound == null) return;
        effectSource.PlayOneShot(hoeSound);
    }

    public void PlayWaterSound()
    {
        if (effectSource == null || waterSound == null) return;
        effectSource.PlayOneShot(waterSound);
    }

    public void PlayPlantSound()
    {
        if (effectSource == null || plantSound == null) return;
        effectSource.PlayOneShot(plantSound);
    }

    public void PlayHarvestSound()
    {
        if (effectSource == null || harvestSound == null) return;
        effectSource.PlayOneShot(harvestSound);
    }

    // ✅ Fishing Actions
    public void PlayCastSound()
    {
        if (effectSource == null || castSound == null) return;
        effectSource.PlayOneShot(castSound);
    }

    public void PlayReelSound()
    {
        if (effectSource == null || reelSound == null) return;
        effectSource.PlayOneShot(reelSound);
    }

    // ✅ UI Sounds
    public void PlayCoinSound()
    {
        if (effectSource == null || coinSound == null) return;
        effectSource.PlayOneShot(coinSound);
    }

    public void PlayButtonClick()
    {
        if (effectSource == null || buttonClickSound == null) return;
        effectSource.PlayOneShot(buttonClickSound);
    }

    public void PlaySuccessSound()
    {
        if (effectSource == null || successSound == null) return;
        effectSource.PlayOneShot(successSound);
    }

    // ✅ Music Control
    public void PlayMusic()
    {
        if (musicSource == null) return;
        if (!musicSource.isPlaying)
            musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource == null) return;
        musicSource.Stop();
    }
}