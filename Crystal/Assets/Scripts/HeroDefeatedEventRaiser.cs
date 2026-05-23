using UnityEngine;

namespace Crystal
{
    public sealed class HeroDefeatedEventRaiser : MonoBehaviour
    {
        [SerializeField] private HeroDefeatedEventChannel heroDefeatedEventChannel;

        public void RaiseHeroDefeated()
        {
            if (heroDefeatedEventChannel == null)
            {
                Debug.LogWarning("[HeroDefeatedEventRaiser] Cannot raise hero defeated event because no event channel is assigned.", this);
                return;
            }

            heroDefeatedEventChannel.Raise();
        }
    }
}
