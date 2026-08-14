using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer mainAudioMixer;

    [Header("UI Elemanları (Opsiyonel Referanslar)")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Toggle uiSoundToggle;

    private const string MusicKey = "MusicVolume";
    private const string SFXKey = "SFXVolume";
    private const string UIKey = "UIVolume";

    private float lastMusicVol = 1f;
    private float lastSFXVol = 1f;

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

    private void Start()
    {
        LoadAudioSettings();
    }

    // --- 1. MÜZİK SESİ ---
    public void SetMusicVolume(float value)
    {
        value = Mathf.Clamp(value, 0.0001f, 1f);
        float dB = Mathf.Log10(value) * 20f;
        if (mainAudioMixer != null) mainAudioMixer.SetFloat("MusicVolume", dB);
        PlayerPrefs.SetFloat(MusicKey, value);
    }

    public void ToggleMuteMusic()
    {
        float currentVol = PlayerPrefs.GetFloat(MusicKey, 1f);
        if (currentVol > 0.0001f)
        {
            lastMusicVol = currentVol;
            SetMusicVolume(0.0001f);
            if (musicSlider != null) musicSlider.value = 0.0001f;
        }
        else
        {
            SetMusicVolume(lastMusicVol);
            if (musicSlider != null) musicSlider.value = lastMusicVol;
        }
    }

    // --- 2. SES EFEKTLERİ (SFX) ---
    public void SetSFXVolume(float value)
    {
        value = Mathf.Clamp(value, 0.0001f, 1f);
        float dB = Mathf.Log10(value) * 20f;
        if (mainAudioMixer != null) mainAudioMixer.SetFloat("SFXVolume", dB);
        PlayerPrefs.SetFloat(SFXKey, value);
    }

    public void ToggleMuteSFX()
    {
        float currentVol = PlayerPrefs.GetFloat(SFXKey, 1f);
        if (currentVol > 0.0001f)
        {
            lastSFXVol = currentVol;
            SetSFXVolume(0.0001f);
            if (sfxSlider != null) sfxSlider.value = 0.0001f;
        }
        else
        {
            SetSFXVolume(lastSFXVol);
            if (sfxSlider != null) sfxSlider.value = lastSFXVol;
        }
    }

    // --- 3. UI SESLERİ ---
    public void SetUISoundEnabled(bool isEnabled)
    {
        float value = isEnabled ? 1f : 0.0001f;
        float dB = Mathf.Log10(value) * 20f;
        if (mainAudioMixer != null) mainAudioMixer.SetFloat("UIVolume", dB);
        PlayerPrefs.SetInt(UIKey, isEnabled ? 1 : 0);
    }

    // --- KAYITLARI YÜKLEME ---
    private void LoadAudioSettings()
    {
        float musicVol = PlayerPrefs.GetFloat(MusicKey, 1f);
        float sfxVol = PlayerPrefs.GetFloat(SFXKey, 1f);
        bool uiEnabled = PlayerPrefs.GetInt(UIKey, 1) == 1;

        SetMusicVolume(musicVol);
        SetSFXVolume(sfxVol);
        SetUISoundEnabled(uiEnabled);

        if (musicSlider != null) musicSlider.value = musicVol;
        if (sfxSlider != null) sfxSlider.value = sfxVol;
        if (uiSoundToggle != null) uiSoundToggle.isOn = uiEnabled;
    }
}