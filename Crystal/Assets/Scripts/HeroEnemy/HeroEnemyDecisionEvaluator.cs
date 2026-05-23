using UnityEngine;

namespace Crystal.HeroEnemy
{
    public enum HeroEnemyDecision
    {
        Idle,
        Dead,
        Retreat,
        SeekHealing,
        ApproachGroundTarget,
        ApproachTarget,
        LeapPlungeAttack,
        BowAttack,
        JumpForLineOfSight,
        MovingBowAttack
    }

    public struct HeroEnemyDecisionInput
    {
        public bool HeroDead;
        public bool WasDamagedRecently;
        public bool HeroLowHealth;
        public bool HealingAvailable;
        public bool TargetLowHealth;
        public bool TargetOnGround;
        public bool TargetOnCeiling;
        public bool HasLineOfSight;
        public bool InLeapRange;
        public bool InBowRange;
        public bool CanLeapPlunge;
        public bool CanBow;
        public bool HasHeightAdvantage;
        public float TargetStationaryDuration;
        public float TargetOnGroundDuration;
        public float TimeSinceLastBow;
        public bool PostPlungePressureActive;
    }

    public static class HeroEnemyDecisionEvaluator
    {
        private static readonly (HeroEnemyDecision Decision, float Score)[] _candidateBuffer = new (HeroEnemyDecision, float)[8];
        private static int _candidateCount;

        public static HeroEnemyDecision Evaluate(HeroEnemyDecisionInput input, HeroEnemyDecisionWeights weights)
        {
            if (input.HeroDead)
                return HeroEnemyDecision.Dead;

            if (input.WasDamagedRecently)
                return HeroEnemyDecision.Retreat;

            if (input.HeroLowHealth && input.HealingAvailable)
                return HeroEnemyDecision.SeekHealing;

            if (input.TargetOnCeiling)
            {
                if (input.CanBow && input.InBowRange && input.HasLineOfSight)
                    return HeroEnemyDecision.BowAttack;

                return HeroEnemyDecision.JumpForLineOfSight;
            }

            _candidateCount = 0;

            if (input.CanLeapPlunge && input.InLeapRange)
            {
                float leapScore = input.HasHeightAdvantage ? weights.leapWithHeightScore : weights.leapWithoutHeightScore;
                if (input.TargetStationaryDuration > weights.targetStationaryThreshold)
                    leapScore += weights.targetStationaryBonus;
                if (input.TargetOnGroundDuration > weights.targetOnGroundThreshold)
                    leapScore += weights.targetOnGroundLeapBonus;
                if (input.TargetLowHealth)
                    leapScore += weights.targetLowHealthLeapBonus;
                _candidateBuffer[_candidateCount++] = (HeroEnemyDecision.LeapPlungeAttack, leapScore);
            }

            if (input.CanBow && input.InBowRange && input.HasLineOfSight)
            {
                float bowScore = weights.bowBaseScore;
                if (input.TimeSinceLastBow < weights.recentBowTimeThreshold)
                    bowScore += weights.recentBowPenalty;
                if (input.PostPlungePressureActive)
                    bowScore += weights.postPlungePressureBowBonus;
                _candidateBuffer[_candidateCount++] = (HeroEnemyDecision.BowAttack, bowScore);
            }

            if (input.CanBow && input.InBowRange && !input.InLeapRange)
                _candidateBuffer[_candidateCount++] = (HeroEnemyDecision.MovingBowAttack, weights.movingBowBaseScore);

            if (!input.InLeapRange || _candidateCount == 0)
                _candidateBuffer[_candidateCount++] = (HeroEnemyDecision.ApproachGroundTarget, weights.approachBaseScore);

            float totalScore = 0f;
            for (int i = 0; i < _candidateCount; i++)
                totalScore += Mathf.Max(0f, _candidateBuffer[i].Score);

            if (totalScore <= 0f)
                return HeroEnemyDecision.ApproachTarget;

            float roll = Random.value * totalScore;
            float accumulated = 0f;
            for (int i = 0; i < _candidateCount; i++)
            {
                float score = Mathf.Max(0f, _candidateBuffer[i].Score);
                accumulated += score;
                if (roll <= accumulated)
                    return _candidateBuffer[i].Decision;
            }

            return _candidateBuffer[_candidateCount - 1].Decision;
        }
    }
}
