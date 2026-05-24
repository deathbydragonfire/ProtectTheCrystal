using UnityEngine;

namespace Crystal
{
    /// <summary>
    /// Periodically spawns a crystal chunk at one of the configured spawn points.
    /// A spawn is skipped when all points are already occupied.
    /// </summary>
    public sealed class CrystalChunkSpawner : MonoBehaviour
    {
        [SerializeField] private CrystalChunkPickup crystalPrefab;
        [SerializeField] private CrystalChunkSpawnPoint[] spawnPoints = new CrystalChunkSpawnPoint[3];
        [SerializeField] private float spawnInterval = 4f;

        private float nextSpawnTime;

        private void OnEnable()
        {
            ScheduleNextSpawn();
        }

        private void Update()
        {
            if (Time.time < nextSpawnTime)
                return;

            TrySpawn();
            ScheduleNextSpawn();
        }

        /// <summary>
        /// Attempts to spawn a crystal at a random empty spawn point.
        /// Does nothing when all points are occupied.
        /// </summary>
        public void TrySpawn()
        {
            if (crystalPrefab == null)
            {
                Debug.LogWarning("[CrystalChunkSpawner] crystalPrefab is not assigned.", this);
                return;
            }

            CrystalChunkSpawnPoint point = SelectEmptySpawnPoint();

            if (point == null)
            {
                Debug.Log("[CrystalChunkSpawner] All spawn points are occupied — skipping spawn.");
                return;
            }

            point.Spawn(crystalPrefab);
        }

        private void ScheduleNextSpawn()
        {
            nextSpawnTime = Time.time + spawnInterval;
        }

        /// <summary>
        /// Picks a random empty spawn point, or null if none are available.
        /// </summary>
        private CrystalChunkSpawnPoint SelectEmptySpawnPoint()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
                return null;

            int startIndex = Random.Range(0, spawnPoints.Length);

            for (int offset = 0; offset < spawnPoints.Length; offset++)
            {
                CrystalChunkSpawnPoint candidate = spawnPoints[(startIndex + offset) % spawnPoints.Length];

                if (candidate != null && !candidate.IsOccupied)
                    return candidate;
            }

            return null;
        }

        private void OnValidate()
        {
            spawnInterval = Mathf.Max(0f, spawnInterval);

            if (spawnPoints == null)
                spawnPoints = new CrystalChunkSpawnPoint[3];
        }
    }
}
