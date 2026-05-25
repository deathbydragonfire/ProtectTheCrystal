using System;
using UnityEngine;

namespace Crystal
{
    /// <summary>
    /// Pairs a string key with an AudioClip for use in an SfxLibrary.
    /// </summary>
    [Serializable]
    public struct SfxEntry
    {
        [Tooltip("Unique key used to request this clip via SfxPlayer.Play().")]
        public string Key;

        [Tooltip("The audio clip to play.")]
        public AudioClip Clip;
    }
}
