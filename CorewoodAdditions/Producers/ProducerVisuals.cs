using System;
using UnityEngine;
using UnityEngine.Audio;

namespace CorewoodAdditions.Producers;

internal static class ProducerVisuals
{
    private static AudioMixer _sfxMixer;
    private static AudioMixerGroup _sfxGroup;

    internal static GameObject FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
            {
                return child.gameObject;
            }
            GameObject found = FindChildRecursive(child, childName);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }

    // AudioMan groups looping ZSFX by m_hash for concurrency limiting. A bundle ZSFX defaults to hash 0, which it
    // would share with every other unconfigured looping sound, so give each prefab's sounds their own group.
    internal static void AssignSfxHash(GameObject prefab, string key)
    {
        int hash = key.GetStableHashCode();
        foreach (ZSFX sfx in prefab.GetComponentsInChildren<ZSFX>(includeInactive: true))
        {
            sfx.m_hash = hash;
        }
    }

    // AudioMan only drops a looping ZSFX from m_loopingSfx when it is destroyed, not disabled. Re-enabling it
    // registers it again, and the duplicate entry makes the concurrency check mute it (volume 0 while "playing").
    internal static void UnregisterLoopingSfx(GameObject root)
    {
        if (root == null || AudioMan.instance == null)
        {
            return;
        }
        foreach (ZSFX sfx in root.GetComponentsInChildren<ZSFX>(includeInactive: true))
        {
            AudioMan.instance.m_loopingSfx.Remove(sfx);
            sfx.ConcurrencyEnable();
        }
    }

    // Shows or hides a producer's SFX child. Unregister before hiding so re-enabling doesn't leave a muted duplicate.
    internal static void SetSfxEnabled(GameObject sfx, bool enabled)
    {
        if (sfx == null || sfx.activeSelf == enabled)
        {
            return;
        }
        if (!enabled)
        {
            UnregisterLoopingSfx(sfx);
        }
        sfx.SetActive(enabled);
    }

    // ZSFX multiplies its volume by this modifier every update, so changing it applies live, including while playing.
    internal static void SetSfxVolume(GameObject root, float volume)
    {
        if (root == null)
        {
            return;
        }
        volume = Mathf.Clamp01(volume);
        foreach (ZSFX sfx in root.GetComponentsInChildren<ZSFX>(includeInactive: true))
        {
            sfx.SetVolumeModifier(volume);
        }
    }

    // Asset bundle AudioSources are not wired to Valheim's mixer, so they ignore the SFX volume slider.
    internal static void RouteAudioToSfx(GameObject root)
    {
        if (root == null)
        {
            return;
        }
        AudioMixerGroup sfx = GetSfxGroup();
        if (sfx == null)
        {
            return;
        }
        foreach (AudioSource source in root.GetComponentsInChildren<AudioSource>(includeInactive: true))
        {
            source.outputAudioMixerGroup = sfx;
        }
    }

    private static AudioMixerGroup GetSfxGroup()
    {
        AudioMixer mixer = AudioMan.instance != null ? AudioMan.instance.m_masterMixer : null;
        if (mixer == null)
        {
            return null;
        }
        if (mixer != _sfxMixer)
        {
            _sfxMixer = mixer;
            _sfxGroup = Array.Find(mixer.FindMatchingGroups(string.Empty), g => g != null && string.Equals(g.name, "SFX", StringComparison.OrdinalIgnoreCase));
        }
        return _sfxGroup;
    }
}
