using UnityEngine;

namespace Crystal.HeroEnemy
{
    public sealed class HeroHealingPickupSpawner : MonoBehaviour
    {
        [SerializeField] private HeroHealingPickup pickupPrefab;
        [SerializeField] private Transform[] spawnPoints = new Transform[0];
        [SerializeField] private Vector2 spawnDelayRange = new Vector2(15f, 35f);
        [SerializeField] private bool spawnImmediatelyOnStart;

        private HeroHealingPickup activePickup;
        private bool waitingForNextSpawn;
        private float nextSpawnTime;

        public HeroHealingPickup ActivePickup => activePickup;
        public float NextSpawnTime => nextSpawnTime;

        public HeroHealingPickup SpawnNow()
        {
            ClearInactivePickup();

            if (HasActivePickup())
                return activePickup;

            Transform spawnPoint = SelectSpawnPoint();
            if (spawnPoint == null || pickupPrefab == null)
            {
                Debug.LogWarning("[HeroHealingPickupSpawner] Cannot spawn because pickupPrefab or spawnPoints are not assigned.", this);
                waitingForNextSpawn = false;
                return null;
            }

            activePickup = Instantiate(pickupPrefab, spawnPoint.position, spawnPoint.rotation, transform);
            activePickup.name = pickupPrefab.name;
            waitingForNextSpawn = false;
            return activePickup;
        }

        private void OnEnable()
        {
            if (spawnImmediatelyOnStart)
                SpawnNow();
            else
                ScheduleNextSpawn();
        }

        private void Update()
        {
            if (HasActivePickup())
                return;

            ClearInactivePickup();

            if (!waitingForNextSpawn)
                ScheduleNextSpawn();

            if (Time.time >= nextSpawnTime)
                SpawnNow();
        }

        private bool HasActivePickup()
        {
            return activePickup != null && activePickup.IsAvailable;
        }

        private void ClearInactivePickup()
        {
            if (activePickup == null || activePickup.IsAvailable)
                return;

            Destroy(activePickup.gameObject);
            activePickup = null;
        }

        private void ScheduleNextSpawn()
        {
            nextSpawnTime = Time.time + Random.Range(spawnDelayRange.x, spawnDelayRange.y);
            waitingForNextSpawn = true;
        }

        private Transform SelectSpawnPoint()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
                return null;

            int startIndex = Random.Range(0, spawnPoints.Length);
            for (int offset = 0; offset < spawnPoints.Length; offset++)
            {
                Transform candidate = spawnPoints[(startIndex + offset) % spawnPoints.Length];
                if (candidate != null)
                    return candidate;
            }

            return null;
        }

        private void OnValidate()
        {
            spawnDelayRange.x = Mathf.Max(0f, spawnDelayRange.x);
            spawnDelayRange.y = Mathf.Max(spawnDelayRange.x, spawnDelayRange.y);

            if (spawnPoints == null)
                spawnPoints = new Transform[0];
        }
    }
}
