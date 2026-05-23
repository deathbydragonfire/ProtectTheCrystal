using System;
using UnityEngine;

namespace Crystal
{
    [CreateAssetMenu(fileName = "HeroDefeatedEventChannel", menuName = "Crystal/Events/Hero Defeated Event Channel")]
    public sealed class HeroDefeatedEventChannel : ScriptableObject
    {
        private event Action Raised;

        public void Raise()
        {
            Raised?.Invoke();
        }

        public void Subscribe(Action listener)
        {
            Raised += listener;
        }

        public void Unsubscribe(Action listener)
        {
            Raised -= listener;
        }
    }
}
