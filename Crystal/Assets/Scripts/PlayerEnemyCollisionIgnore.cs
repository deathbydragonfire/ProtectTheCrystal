using UnityEngine;

/// <summary>
/// Disables physical collision between the Player and Enemy layers at runtime.
/// Equivalent to unchecking Player/Enemy in the Physics 2D Layer Collision Matrix.
/// Attach to any persistent GameObject in the scene.
/// </summary>
public class PlayerEnemyCollisionIgnore : MonoBehaviour
{
    private const string PlayerLayerName = "Player";
    private const string EnemyLayerName  = "Enemy";

    private void Awake()
    {
        int playerLayer = LayerMask.NameToLayer(PlayerLayerName);
        int enemyLayer  = LayerMask.NameToLayer(EnemyLayerName);

        if (playerLayer == -1)
        {
            Debug.LogError($"[PlayerEnemyCollisionIgnore] Layer '{PlayerLayerName}' not found.");
            return;
        }

        if (enemyLayer == -1)
        {
            Debug.LogError($"[PlayerEnemyCollisionIgnore] Layer '{EnemyLayerName}' not found.");
            return;
        }

        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);
        Debug.Log("[PlayerEnemyCollisionIgnore] Awake — Player/Enemy layer collision disabled.");
    }
}
