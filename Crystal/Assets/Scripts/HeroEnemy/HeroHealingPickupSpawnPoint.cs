using UnityEngine;

namespace Crystal.HeroEnemy
{
    public sealed class HeroHealingPickupSpawnPoint : MonoBehaviour
    {
        [SerializeField] private HeroHealingPickup pickupPrefab;
        [SerializeField] private bool spawnOnStart;

        private HeroHealingPickup spawnedPickup;

        public HeroHealingPickup SpawnedPickup => spawnedPickup;

        public HeroHealingPickup Spawn()
        {
            if (spawnedPickup != null)
                return spawnedPickup;

            if (pickupPrefab == null)
            {
                Debug.LogWarning("[HeroHealingPickupSpawnPoint] Cannot spawn because no pickup prefab is assigned.", this);
                return null;
            }

            spawnedPickup = Instantiate(pickupPrefab, transform.position, transform.rotation, transform);
            spawnedPickup.name = pickupPrefab.name;
            return spawnedPickup;
        }

        private void Start()
        {
            if (spawnOnStart)
                Spawn();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 1f, 0.35f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, 0.35f);
            Gizmos.DrawLine(transform.position + Vector3.left * 0.25f, transform.position + Vector3.right * 0.25f);
            Gizmos.DrawLine(transform.position + Vector3.down * 0.25f, transform.position + Vector3.up * 0.25f);
        }
    }
}
