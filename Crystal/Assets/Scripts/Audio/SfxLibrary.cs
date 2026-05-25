using System.Collections.Generic;
using UnityEngine;

namespace Crystal
{
    /// <summary>
    /// ScriptableObject registry that maps string keys to AudioClips.
    /// Build this asset once and assign it to the SfxPlayer in the scene.
    /// </summary>
    [CreateAssetMenu(fileName = "SfxLibrary", menuName = "Crystal/Audio/Sfx Library")]
    public sealed class SfxLibrary : ScriptableObject
    {
        [SerializeField] private SfxEntry[] entries = System.Array.Empty<SfxEntry>();

        private Dictionary<string, AudioClip> lookup;

        /// <summary>Builds the lookup dictionary. Call this once before any Play requests.</summary>
        public void Initialize()
        {
            lookup = new Dictionary<string, AudioClip>(entries.Length);

            foreach (SfxEntry entry in entries)
            {
                if (string.IsNullOrEmpty(entry.Key))
                {
                    Debug.LogWarning("[SfxLibrary] Skipping entry with empty key.", this);
                    continue;
                }

                if (entry.Clip == null)
                {
                    Debug.LogWarning($"[SfxLibrary] Key '{entry.Key}' has no AudioClip assigned.", this);
                    continue;
                }

                if (!lookup.TryAdd(entry.Key, entry.Clip))
                {
                    Debug.LogWarning($"[SfxLibrary] Duplicate key '{entry.Key}' — only the first entry will be used.", this);
                }
            }
        }

        /// <summary>Attempts to retrieve the AudioClip registered under the given key.</summary>
        public bool TryGet(string key, out AudioClip clip)
        {
            if (lookup == null)
            {
                Debug.LogError("[SfxLibrary] Library has not been initialized. Call Initialize() first.");
                clip = null;
                return false;
            }

            return lookup.TryGetValue(key, out clip);
        }
    }
}
