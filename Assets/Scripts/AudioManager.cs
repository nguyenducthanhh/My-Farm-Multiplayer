
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource effectSource;

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

    // Mute control
    private bool isMuted = false;

    public static AudioManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            isMuted = PlayerPrefs.GetInt("AudioMuted", defaultValue: 0) == 1;
            ApplyMuteState();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ToggleMute()
    {
        isMuted = !isMuted;
        ApplyMuteState();

        PlayerPrefs.SetInt("AudioMuted", isMuted ? 1 : 0);
        PlayerPrefs.Save();

        Debug.Log(isMuted ? " MUTED" : " UNMUTED");
    }

    // Áp dụng trạng thái mute
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

    // Kiểm tra trạng thái mute
    public bool IsMuted()
    {
        return isMuted;
    }

    //  Farm Actions
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

    // Fishing Actions
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

    // UI Sounds
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

    // Music Control
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