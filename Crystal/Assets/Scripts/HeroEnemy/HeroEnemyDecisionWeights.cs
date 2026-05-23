using UnityEngine;

namespace Crystal.HeroEnemy
{
    [CreateAssetMenu(menuName = "Crystal/HeroEnemy/Decision Weights")]
    public sealed class HeroEnemyDecisionWeights : ScriptableObject
    {
        public float leapWithHeightScore = 80f;
        public float leapWithoutHeightScore = 35f;
        public float targetStationaryThreshold = 0.5f;
        public float targetStationaryBonus = 30f;
        public float targetOnGroundThreshold = 0.4f;
        public float targetOnGroundLeapBonus = 50f;
        public float targetLowHealthLeapBonus = 20f;
        public float bowBaseScore = 40f;
        public float movingBowBaseScore = 30f;
        public float recentBowTimeThreshold = 2.2f;
        public float recentBowPenalty = -20f;
        public float postPlungePressureBowBonus = 60f;
        public float approachBaseScore = 50f;
    }
}
