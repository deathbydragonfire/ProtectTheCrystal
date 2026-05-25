using System;
using UnityEngine;

namespace Crystal
{
    /// <summary>
    /// ScriptableObject event channel raised when the player (Ciela) dies.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerDiedEventChannel", menuName = "Crystal/Events/Player Died Event Channel")]
    public sealed class PlayerDiedEventChannel : ScriptableObject
    {
        private event Action Raised;

        /// <summary>Raises the player died event.</summary>
        public void Raise()
        {
            Raised?.Invoke();
        }

        /// <summary>Subscribes a listener to this event channel.</summary>
        public void Subscribe(Action listener)
        {
            Raised += listener;
        }

        /// <summary>Unsubscribes a listener from this event channel.</summary>
        public void Unsubscribe(Action listener)
        {
            Raised -= listener;
        }
    }
}
