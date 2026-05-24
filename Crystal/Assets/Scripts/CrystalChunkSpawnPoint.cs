using UnityEngine;

namespace Crystal
{
    /// <summary>
    /// Marks a position where a crystal chunk can be spawned.
    /// Tracks whether a crystal is currently occupying this point.
    /// </summary>
    public sealed class CrystalChunkSpawnPoint : MonoBehaviour
    {
        private CrystalChunkPickup occupant;

        /// <summary>True when a crystal chunk is alive at this spawn point.</summary>
        public bool IsOccupied => occupant != null;

        /// <summary>
        /// Spawns the given prefab at this point. Returns the spawned instance,
        /// or null if the point is already occupied or the prefab is null.
        /// </summary>
        public CrystalChunkPickup Spawn(CrystalChunkPickup prefab)
        {
            if (IsOccupied)
                return null;

            if (prefab == null)
            {
                Debug.LogWarning("[CrystalChunkSpawnPoint] Cannot spawn — no prefab provided.", this);
                return null;
            }

            occupant = Instantiate(prefab, transform.position, transform.rotation, transform);
            occupant.name = prefab.name;
            return occupant;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = IsOccupied
                ? new Color(0.4f, 0.8f, 1f, 0.9f)
                : new Color(0.4f, 0.8f, 1f, 0.35f);

            Gizmos.DrawWireSphere(transform.position, 0.3f);
            Gizmos.DrawLine(transform.position + Vector3.left * 0.2f, transform.position + Vector3.right * 0.2f);
            Gizmos.DrawLine(transform.position + Vector3.down * 0.2f, transform.position + Vector3.up * 0.2f);
        }
    }
}
