using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SfxId
{
    Gun,
    Hit,
    LevelUp,
    ButtonClick,
    ButtonHover,
    GameOver,
    GameClear
}

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("BGM Clips")]
    public AudioClip menuBgm;
    public AudioClip gameBgm;

    [Header("SFX Clips")]
    public AudioClip gun;          // (기본 총소리, 필요 없으면 비워도 됨)
    public AudioClip hit;
    public AudioClip levelUp;
    public AudioClip buttonClick;
    public AudioClip buttonHover;
    public AudioClip gameOver;
    public AudioClip gameClear;

    [Header("BGM Fade")]
    public float bgmFadeDuration = 0.5f;

    [Header("Volumes")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float bgmVolume = 0.6f;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;
    [Range(0f, 1f)] public float uiVolume = 0.9f;

    [Header("SFX Pool")]
    public int sfxSourcePoolSize = 8;

    [Header("Gun SFX Cooldown")]
    public float gunMinInterval = 0.06f; // 발사 1회당 1번 울리게 제어(연타 방지)

    AudioSource bgmSource;
    AudioSource uiSource;
    List<AudioSource> sfxSources = new();
    int sfxIndex = 0;

    float lastGunTime = -999f;

    Coroutine bgmFadeCo;

    const string PREF_MASTER = "VOL_MASTER";
    const string PREF_BGM = "VOL_BGM";
    const string PREF_SFX = "VOL_SFX";
    const string PREF_UI = "VOL_UI";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;

        uiSource = gameObject.AddComponent<AudioSource>();
        uiSource.loop = false;
        uiSource.playOnAwake = false;

        for (int i = 0; i < Mathf.Max(1, sfxSourcePoolSize); i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.loop = false;
            src.playOnAwake = false;
            sfxSources.Add(src);
        }

        LoadVolumes();
        ApplyVolumes();
    }
   
    // --------------------
    // BGM
    // --------------------
    public void PlayMenuBGM(bool restart = false)
    {
        PlayBGM(menuBgm, restart);
    }

    public void PlayGameBGM(bool restart = false)
    {
        PlayBGM(gameBgm, restart);
    }

    public void PlayBGM(AudioClip clip, bool restart = false)
    {
        if (clip == null) return;

        // 같은 곡이면 재시작 옵션 아닐 때는 아무것도 안 함
        if (!restart && bgmSource.isPlaying && bgmSource.clip == clip) return;

        // 페이드 전환
        if (bgmFadeCo != null) StopCoroutine(bgmFadeCo);
        bgmFadeCo = StartCoroutine(FadeToBgm(clip));
    }

    public void StopBGM(bool fadeOut = true)
    {
        if (!fadeOut)
        {
            bgmSource.Stop();
            bgmSource.clip = null;
            return;
        }

        if (bgmFadeCo != null) StopCoroutine(bgmFadeCo);
        bgmFadeCo = StartCoroutine(FadeOutStop());
    }

    IEnumerator FadeToBgm(AudioClip nextClip)
    {
        float targetVol = masterVolume * bgmVolume;

        // 1) Fade Out (현재 재생 중일 때만)
        if (bgmSource.isPlaying && bgmSource.clip != null)
        {
            float start = bgmSource.volume;
            float t = 0f;
            while (t < bgmFadeDuration)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Clamp01(t / bgmFadeDuration);
                bgmSource.volume = Mathf.Lerp(start, 0f, a);
                yield return null;
            }
        }

        // 2) Switch clip & Play
        bgmSource.Stop();
        bgmSource.clip = nextClip;
        bgmSource.Play();

        // 3) Fade In
        bgmSource.volume = 0f;
        {
            float t = 0f;
            while (t < bgmFadeDuration)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Clamp01(t / bgmFadeDuration);
                bgmSource.volume = Mathf.Lerp(0f, targetVol, a);
                yield return null;
            }
        }

        bgmSource.volume = targetVol;
        bgmFadeCo = null;
    }

    IEnumerator FadeOutStop()
    {
        if (!bgmSource.isPlaying)
        {
            bgmFadeCo = null;
            yield break;
        }

        float start = bgmSource.volume;
        float t = 0f;
        while (t < bgmFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / bgmFadeDuration);
            bgmSource.volume = Mathf.Lerp(start, 0f, a);
            yield return null;
        }

        bgmSource.Stop();
        bgmSource.clip = null;

        // 볼륨은 다시 목표값으로 복구(다음 재생 대비)
        bgmSource.volume = masterVolume * bgmVolume;

        bgmFadeCo = null;
    }

    // --------------------
    // SFX / UI
    // --------------------
    public void PlaySfx(SfxId id, float volumeMul = 1f)
    {
        var clip = GetClip(id);
        if (clip == null) return;

        // 기본 Gun(하나짜리) 쓸 경우만
        if (id == SfxId.Gun)
        {
            if (!CanPlayGun()) return;
        }

        var src = NextSfxSource();
        src.clip = clip;
        src.volume = masterVolume * sfxVolume * Mathf.Clamp01(volumeMul);
        src.Play();
    }

    // ✅ 무기별 총소리: “발사 1회당 1번만”
    public void PlayGunShot(AudioClip gunClip, float volumeMul = 1f)
    {
        if (gunClip == null) return;
        if (!CanPlayGun()) return;

        var src = NextSfxSource();
        src.clip = gunClip;
        src.volume = masterVolume * sfxVolume * Mathf.Clamp01(volumeMul);
        src.Play();
    }

    bool CanPlayGun()
    {
        if (Time.unscaledTime - lastGunTime < gunMinInterval) return false;
        lastGunTime = Time.unscaledTime;
        return true;
    }

    public void PlayUI(SfxId id, float volumeMul = 1f)
    {
        var clip = GetClip(id);
        if (clip == null) return;

        uiSource.clip = clip;
        uiSource.volume = masterVolume * uiVolume * Mathf.Clamp01(volumeMul);
        uiSource.Play();
    }

    // 볼륨 조절(UI 슬라이더 연결용)
    public void SetMaster(float v) { masterVolume = Mathf.Clamp01(v); SaveVolumes(); ApplyVolumes(); }
    public void SetBgm(float v) { bgmVolume = Mathf.Clamp01(v); SaveVolumes(); ApplyVolumes(); }
    public void SetSfx(float v) { sfxVolume = Mathf.Clamp01(v); SaveVolumes(); ApplyVolumes(); }
    public void SetUI(float v) { uiVolume = Mathf.Clamp01(v); SaveVolumes(); ApplyVolumes(); }

    // --------------------
    // Internals
    // --------------------
    AudioSource NextSfxSource()
    {
        if (sfxSources.Count == 0) sfxSources.Add(gameObject.AddComponent<AudioSource>());

        var src = sfxSources[sfxIndex];
        sfxIndex = (sfxIndex + 1) % sfxSources.Count;
        return src;
    }

    AudioClip GetClip(SfxId id)
    {
        return id switch
        {
            SfxId.Gun => gun,
            SfxId.Hit => hit,
            SfxId.LevelUp => levelUp,
            SfxId.ButtonClick => buttonClick,
            SfxId.ButtonHover => buttonHover,
            SfxId.GameOver => gameOver,
            SfxId.GameClear => gameClear,
            _ => null
        };
    }

    void ApplyVolumes()
    {
        // BGM은 페이드 코루틴이 볼륨을 만지니까 “목표 볼륨”만 맞춰줌
        bgmSource.volume = masterVolume * bgmVolume;
        uiSource.volume = masterVolume * uiVolume;

        for (int i = 0; i < sfxSources.Count; i++)
            sfxSources[i].volume = masterVolume * sfxVolume;
    }

    void SaveVolumes()
    {
        PlayerPrefs.SetFloat(PREF_MASTER, masterVolume);
        PlayerPrefs.SetFloat(PREF_BGM, bgmVolume);
        PlayerPrefs.SetFloat(PREF_SFX, sfxVolume);
        PlayerPrefs.SetFloat(PREF_UI, uiVolume);
        PlayerPrefs.Save();
    }

    void LoadVolumes()
    {
        masterVolume = PlayerPrefs.GetFloat(PREF_MASTER, masterVolume);
        bgmVolume = PlayerPrefs.GetFloat(PREF_BGM, bgmVolume);
        sfxVolume = PlayerPrefs.GetFloat(PREF_SFX, sfxVolume);
        uiVolume = PlayerPrefs.GetFloat(PREF_UI, uiVolume);
    }
}


