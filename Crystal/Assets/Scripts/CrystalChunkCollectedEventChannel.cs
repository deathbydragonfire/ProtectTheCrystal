using System;
using UnityEngine;

namespace Crystal
{
    /// <summary>
    /// ScriptableObject event channel raised whenever a crystal chunk is collected.
    /// Carries the fill amount [0, 1] to add to the CrystalBar.
    /// </summary>
    [CreateAssetMenu(fileName = "CrystalChunkCollectedEventChannel",
                     menuName = "Crystal/Events/Crystal Chunk Collected Event Channel")]
    public sealed class CrystalChunkCollectedEventChannel : ScriptableObject
    {
        private event Action<float> Raised;

        /// <summary>Raises the event with the given fill amount [0, 1].</summary>
        public void Raise(float fillAmount)
        {
            Raised?.Invoke(fillAmount);
        }

        /// <summary>Subscribes a listener to this event channel.</summary>
        public void Subscribe(Action<float> listener)
        {
            Raised += listener;
        }

        /// <summary>Unsubscribes a listener from this event channel.</summary>
        public void Unsubscribe(Action<float> listener)
        {
            Raised -= listener;
        }
    }
}
