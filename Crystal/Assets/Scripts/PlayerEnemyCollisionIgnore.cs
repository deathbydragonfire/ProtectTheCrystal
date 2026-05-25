using UnityEngine;

/// <summary>
/// Disables physical collision between the Player and Enemy layers at runtime,
/// while leaving attack interactions unaffected so hitboxes can still trigger.
/// </summary>
public class PlayerEnemyCollisionIgnore : MonoBehaviour
{
    private const string PlayerLayerName       = "Player";
    private const string EnemyLayerName        = "Enemy";
    private const string PlayerAttackLayerName = "PlayerAttack";
    private const string EnemyAttackLayerName  = "EnemyAttack";

    private void Awake()
    {
        int playerLayer       = LayerMask.NameToLayer(PlayerLayerName);
        int enemyLayer        = LayerMask.NameToLayer(EnemyLayerName);
        int playerAttackLayer = LayerMask.NameToLayer(PlayerAttackLayerName);
        int enemyAttackLayer  = LayerMask.NameToLayer(EnemyAttackLayerName);

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

        // Suppress physical body-vs-body collision only.
        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);
        Debug.Log("[PlayerEnemyCollisionIgnore] Player/Enemy body collision disabled.");

        // Ensure PlayerAttack <-> Enemy triggers fire (player hits enemy).
        if (playerAttackLayer != -1)
        {
            Physics2D.IgnoreLayerCollision(playerAttackLayer, enemyLayer, false);
            Debug.Log("[PlayerEnemyCollisionIgnore] PlayerAttack/Enemy trigger interaction enabled.");
        }
        else
        {
            Debug.LogWarning($"[PlayerEnemyCollisionIgnore] Layer '{PlayerAttackLayerName}' not found — player attack hitbox may not trigger against enemies.");
        }

        // Ensure EnemyAttack <-> Player triggers fire (enemy hits player).
        if (enemyAttackLayer != -1)
        {
            Physics2D.IgnoreLayerCollision(enemyAttackLayer, playerLayer, false);
            Debug.Log("[PlayerEnemyCollisionIgnore] EnemyAttack/Player trigger interaction enabled.");
        }
        else
        {
            // No dedicated EnemyAttack layer — fall back to ensuring Enemy <-> Player
            // trigger interaction is on so arrows and overlap hits reach Ciela.
            Physics2D.IgnoreLayerCollision(enemyLayer, playerLayer, false);
            Debug.Log("[PlayerEnemyCollisionIgnore] No EnemyAttack layer found — re-enabled Enemy/Player trigger interaction as fallback.");
        }
    }
}
