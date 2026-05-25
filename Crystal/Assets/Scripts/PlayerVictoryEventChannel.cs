using System;
using UnityEngine;

namespace Crystal
{
    /// <summary>
    /// ScriptableObject event channel raised when the player achieves victory:
    /// the crystal is full and the hero enemy is defeated.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerVictoryEventChannel", menuName = "Crystal/Events/Player Victory Event Channel")]
    public sealed class PlayerVictoryEventChannel : ScriptableObject
    {
        private event Action Raised;

        /// <summary>Raises the player victory event.</summary>
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
