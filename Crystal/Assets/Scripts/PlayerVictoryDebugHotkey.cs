using UnityEngine;

namespace Crystal
{
    public sealed class PlayerVictoryDebugHotkey : MonoBehaviour
    {
        [SerializeField] private PlayerVictoryEventChannel playerVictoryEventChannel;

        [ContextMenu("Raise Player Victory")]
        public void RaisePlayerVictory()
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
