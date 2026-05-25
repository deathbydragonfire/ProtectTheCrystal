using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Crystal
{
    public sealed class PlayerVictoryDebugHotkey : MonoBehaviour
    {
        [SerializeField] private PlayerVictoryEventChannel playerVictoryEventChannel;
        [SerializeField] private bool testHotkeyEnabled = true;

        private void Update()
        {
            if (!testHotkeyEnabled)
                return;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null || !Keyboard.current.tKey.wasPressedThisFrame)
                return;

            RaisePlayerVictory();
#else
            Debug.LogWarning("[PlayerVictoryDebugHotkey] T-key testing requires the Unity Input System package.", this);
            testHotkeyEnabled = false;
#endif
        }

        private void RaisePlayerVictory()
        {
            if (playerVictoryEventChannel == null)
            {
                Debug.LogWarning("[PlayerVictoryDebugHotkey] Cannot raise player victory event because no event channel is assigned.", this);
                return;
            }

            playerVictoryEventChannel.Raise();
        }
    }
}
