using UnityEngine;

/// <summary>
/// Disables physical collision between the Player and Enemy layers at runtime,
/// while leaving the PlayerAttack layer unaffected so attack hitboxes can still
/// trigger against enemies.
/// </summary>
public class PlayerEnemyCollisionIgnore : MonoBehaviour
{
    private const string PlayerLayerName       = "Player";
    private const string EnemyLayerName        = "Enemy";
    private const string PlayerAttackLayerName = "PlayerAttack";

    private void Awake()
    {
        int playerLayer       = LayerMask.NameToLayer(PlayerLayerName);
        int enemyLayer        = LayerMask.NameToLayer(EnemyLayerName);
        int playerAttackLayer = LayerMask.NameToLayer(PlayerAttackLayerName);

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
        Debug.Log("[PlayerEnemyCollisionIgnore] Awake — Player/Enemy layer collision disabled.");

        // Ensure PlayerAttack <-> Enemy is explicitly enabled so hitbox triggers fire.
        if (playerAttackLayer != -1)
        {
            Physics2D.IgnoreLayerCollision(playerAttackLayer, enemyLayer, false);
            Debug.Log("[PlayerEnemyCollisionIgnore] Awake — PlayerAttack/Enemy trigger interaction enabled.");
        }
        else
        {
            Debug.LogWarning($"[PlayerEnemyCollisionIgnore] Layer '{PlayerAttackLayerName}' not found — attack hitbox may not trigger against enemies.");
        }
    }
}
