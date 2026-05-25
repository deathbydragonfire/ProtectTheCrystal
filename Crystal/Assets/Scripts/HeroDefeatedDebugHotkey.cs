using UnityEngine;

namespace Crystal
{
    public sealed class HeroDefeatedDebugHotkey : MonoBehaviour
    {
        [SerializeField] private HeroDefeatedEventChannel heroDefeatedEventChannel;

        [ContextMenu("Raise Hero Defeated")]
        public void RaiseHeroDefeated()
        {
            if (heroDefeatedEventChannel == null)
            {
                Debug.LogWarning("[HeroDefeatedDebugHotkey] Cannot raise hero defeated event because no event channel is assigned.", this);
                return;
            }

            heroDefeatedEventChannel.Raise();
        }
    }
}
