using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Crystal
{
    public sealed class HeroDefeatedDebugHotkey : MonoBehaviour
    {
        [SerializeField] private HeroDefeatedEventChannel heroDefeatedEventChannel;
        [SerializeField] private bool testHotkeyEnabled = true;

        private void Update()
        {
            if (!testHotkeyEnabled)
                return;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null || !Keyboard.current.pKey.wasPressedThisFrame)
                return;

            RaiseHeroDefeated();
#else
            Debug.LogWarning("[HeroDefeatedDebugHotkey] P-key testing requires the Unity Input System package.", this);
            testHotkeyEnabled = false;
#endif
        }

        private void RaiseHeroDefeated()
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
