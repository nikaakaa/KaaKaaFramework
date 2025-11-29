using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 音乐音效管理器
/// </summary>
public class MusicMgr : BaseManager<MusicMgr>
{
    // 背景音乐播放组件
    private AudioSource bkMusic = null;
    // 背景音乐大小
    private float bkMusicValue = 0.8f;
    // 追踪当前加载的背景音乐资源的 Key，用于释放句柄
    private string _currentBkMusicKey = null;

    // 管理正在播放的音效
    private List<AudioSource> soundList = new List<AudioSource>();
    // 音效音量大小
    private float soundValue = 0.2f;
    // 音效是否在播放
    private bool soundIsPlay = true;


    private MusicMgr()
    {
        // 依赖外部 MonoMgr 提供 Update 驱动
        // 确保你的 MonoMgr 已经实现并正常工作
        MonoMgr.Instance.AddFixedUpdateListener(Update);
    }


    private void Update()
    {
        if (!soundIsPlay)
            return;

        // 逆向遍历 soundList，安全地移除已播放完毕的音效
        for (int i = soundList.Count - 1; i >= 0; --i)
        {
            if (!soundList[i].isPlaying)
            {
                // 音效播放完毕
                // 1. 清空切片，避免占用
                soundList[i].clip = null;
                // 2. 将音效对象放回缓存池
                PoolMgr.Instance.PushObj(soundList[i].gameObject);
                // 3. 从列表中移除
                soundList.RemoveAt(i);
            }
        }
    }


    /// <summary>
    /// 播放背景音乐
    /// </summary>
    public void PlayBKMusic(string name)
    {
        // 动态创建播放背景音乐的组件，并设置不销毁
        if (bkMusic == null)
        {
            GameObject obj = new GameObject();
            obj.name = "BKMusic";
            GameObject.DontDestroyOnLoad(obj);
            bkMusic = obj.AddComponent<AudioSource>();
        }

        // 核心优化：释放旧的 Audio Clip 句柄
        if (!string.IsNullOrEmpty(_currentBkMusicKey))
        {
            // 如果上一个 Key 和当前 Key 不同，释放旧资源
            if (_currentBkMusicKey != name)
            {
                AddressablesMgr.Instance.Release<AudioClip>(_currentBkMusicKey);
                _currentBkMusicKey = null;
            }
            else
            {
                // 如果是播放同一个资源，只需要 Stop/Play 即可，不需要重新加载
                bkMusic.Stop();
                bkMusic.Play();
                return;
            }
        }

        // 加载新资源
        AddressablesMgr.Instance.LoadAssetAsync<AudioClip>(name, (clip) =>
        {
            if (clip == null)
            {
                Debug.LogError($"[MusicMgr] 背景音乐加载失败: {name}");
                return;
            }

            bkMusic.clip = clip; // 使用新的优化版 API，直接接收结果
            bkMusic.loop = true;
            bkMusic.volume = bkMusicValue;
            bkMusic.Play();

            // 追踪当前加载的资源 Key
            _currentBkMusicKey = name;
        });
    }

    //停止背景音乐
    public void StopBKMusic()
    {
        if (bkMusic == null)
            return;
        bkMusic.Stop();
    }

    //暂停背景音乐
    public void PauseBKMusic()
    {
        if (bkMusic == null)
            return;
        bkMusic.Pause();
    }

    //设置背景音乐大小
    public void ChangeBKMusicValue(float v)
    {
        bkMusicValue = v;
        if (bkMusic == null)
            return;
        bkMusic.volume = bkMusicValue;
    }

    /// <summary>
    /// 播放音效
    /// </summary>
    /// <param name="name">音效名字</param>
    /// <param name="followTransform">音效跟随的 Transform，为 null 则不跟随</param>
    /// <param name="isLoop">是否循环</param>
    /// <param name="callBack">加载结束后的回调</param>
    public void PlaySound(string name,
                          Transform followTransform = null,
                          bool isLoop = false,
                          UnityAction<AudioSource> callBack = null)
    {
        // 1. 异步加载 AudioClip
        AddressablesMgr.Instance.LoadAssetAsync<AudioClip>(name, (clip) =>
        {
            if (clip == null)
            {
                Debug.LogError($"[MusicMgr] 音效加载失败: {name}");
                return;
            }

            // 2. 异步从池中获取 AudioSource GameObject
            PoolMgr.Instance.GetObjAsync("SoundObj", (soundObj) =>
            {
                if (soundObj == null) return;

                // 确保有 AudioSource 组件
                AudioSource source = soundObj.GetComponent<AudioSource>();
                if (source == null) source = soundObj.AddComponent<AudioSource>();

                // 配置 AudioSource
                source.Stop(); // 停止可能正在播放的旧音效
                source.clip = clip; // 使用新的优化版 API，直接接收结果
                source.loop = isLoop;
                source.volume = soundValue;

                // 设置父对象和位置
                if (followTransform != null)
                {
                    // 设置为跟随，世界坐标不变
                    source.transform.SetParent(followTransform, false);
                    source.transform.localPosition = Vector3.zero;
                }
                else
                {
                    // 不跟随，通常由 PoolMgr 自动设置回根节点
                    source.transform.SetParent(null);
                }

                source.Play();

                // 加入列表管理
                if (!soundList.Contains(source))
                    soundList.Add(source);

                callBack?.Invoke(source);
            });
        });
    }

    /// <summary>
    /// 停止播放音效
    /// </summary>
    /// <param name="source">音效组件对象</param>
    public void StopSound(AudioSource source)
    {
        if (soundList.Contains(source))
        {
            source.Stop();
            soundList.Remove(source);

            // 释放 AudioClip 引用并回池
            // 注意：这里音效资源是共享的，不能释放 Addressables 句柄，只清空 clip 引用
            source.clip = null;
            PoolMgr.Instance.PushObj(source.gameObject);
        }
    }

    /// <summary>
    /// 改变音效大小
    /// </summary>
    public void ChangeSoundValue(float v)
    {
        soundValue = v;
        for (int i = 0; i < soundList.Count; i++)
        {
            soundList[i].volume = v;
        }
    }

    /// <summary>
    /// 继续播放或者暂停所有音效
    /// </summary>
    public void PlayOrPauseSound(bool isPlay)
    {
        soundIsPlay = isPlay;
        if (isPlay)
        {
            for (int i = 0; i < soundList.Count; i++)
                soundList[i].UnPause(); // 使用 UnPause 替代 Play，Play 可能会重新开始
        }
        else
        {
            for (int i = 0; i < soundList.Count; i++)
                soundList[i].Pause();
        }
    }

    /// <summary>
    /// 清理所有正在播放的音效 (通常在切换场景时调用)
    /// </summary>
    public void ClearSoundEffects()
    {
        for (int i = 0; i < soundList.Count; i++)
        {
            soundList[i].Stop();
            soundList[i].clip = null;
            PoolMgr.Instance.PushObj(soundList[i].gameObject);
        }
        soundList.Clear();
    }

    /// <summary>
    /// 清理 BGM 和所有 SFX 资源
    /// </summary>
    public void ClearAll()
    {
        // 1. 清理音效
        ClearSoundEffects();

        // 2. 清理 BGM
        if (bkMusic != null)
        {
            bkMusic.Stop();
            bkMusic.clip = null;
            GameObject.Destroy(bkMusic.gameObject); // 销毁持久化的 BGM 对象
            bkMusic = null;
        }

        // 3. 释放 BGM 资源句柄
        if (!string.IsNullOrEmpty(_currentBkMusicKey))
        {
            AddressablesMgr.Instance.Release<AudioClip>(_currentBkMusicKey);
            _currentBkMusicKey = null;
        }
    }
}