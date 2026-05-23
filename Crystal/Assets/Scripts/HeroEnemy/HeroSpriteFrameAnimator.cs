using UnityEngine;

namespace Crystal.HeroEnemy
{
    public enum HeroAnimationAction
    {
        Idle,
        Run,
        Jump,
        Dash,
        Retreat,
        Hurt,
        Heal,
        AimBow,
        ShootBow,
        Leap,
        Plunge,
        Death
    }

    public sealed class HeroSpriteFrameAnimator : MonoBehaviour
    {
        [Header("Renderer")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Defaults")]
        [SerializeField] private float defaultFramesPerSecond = 10f;

        [Header("Movement Frames")]
        [SerializeField] private Sprite[] idleFrames = new Sprite[0];
        [SerializeField] private Sprite[] runFrames = new Sprite[0];
        [SerializeField] private Sprite[] jumpFrames = new Sprite[0];
        [SerializeField] private Sprite[] dashFrames = new Sprite[0];
        [SerializeField] private Sprite[] retreatFrames = new Sprite[0];

        [Header("Combat Frames")]
        [SerializeField] private Sprite[] hurtFrames = new Sprite[0];
        [SerializeField] private Sprite[] healFrames = new Sprite[0];
        [SerializeField] private Sprite[] aimBowFrames = new Sprite[0];
        [SerializeField] private Sprite[] shootBowFrames = new Sprite[0];
        [SerializeField] private Sprite[] leapFrames = new Sprite[0];
        [SerializeField] private Sprite[] plungeFrames = new Sprite[0];
        [SerializeField] private Sprite[] deathFrames = new Sprite[0];

        [Header("Frame Rates")]
        [SerializeField] private float idleFramesPerSecond = 8f;
        [SerializeField] private float runFramesPerSecond = 12f;
        [SerializeField] private float jumpFramesPerSecond = 12f;
        [SerializeField] private float dashFramesPerSecond = 16f;
        [SerializeField] private float retreatFramesPerSecond = 12f;
        [SerializeField] private float combatFramesPerSecond = 12f;

        private HeroAnimationAction currentAction = HeroAnimationAction.Idle;
        private int currentFrameIndex;
        private float frameTimer;
        private bool completedNonLoopingAnimation;

        public HeroAnimationAction CurrentAction => currentAction;
        public bool IsCurrentAnimationComplete => completedNonLoopingAnimation;

        public void Play(HeroAnimationAction action, bool forceRestart = false)
        {
            if (!forceRestart && currentAction == action)
                return;

            currentAction = action;
            currentFrameIndex = 0;
            frameTimer = 0f;
            completedNonLoopingAnimation = false;
            ApplyCurrentFrame();
        }

        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            ApplyCurrentFrame();
        }

        private void Update()
        {
            Sprite[] frames = GetFrames(currentAction);
            if (spriteRenderer == null || frames == null || frames.Length == 0 || completedNonLoopingAnimation)
                return;

            float frameDuration = 1f / Mathf.Max(0.01f, GetFramesPerSecond(currentAction));
            frameTimer += Time.deltaTime;

            while (frameTimer >= frameDuration)
            {
                frameTimer -= frameDuration;
                AdvanceFrame(frames);
            }
        }

        private void AdvanceFrame(Sprite[] frames)
        {
            currentFrameIndex++;

            if (currentFrameIndex < frames.Length)
            {
                ApplyCurrentFrame();
                return;
            }

            if (ShouldLoop(currentAction))
            {
                currentFrameIndex = 0;
                ApplyCurrentFrame();
                return;
            }

            currentFrameIndex = frames.Length - 1;
            completedNonLoopingAnimation = true;
            ApplyCurrentFrame();
        }

        private void ApplyCurrentFrame()
        {
            Sprite[] frames = GetFrames(currentAction);
            if (spriteRenderer == null || frames == null || frames.Length == 0)
                return;

            currentFrameIndex = Mathf.Clamp(currentFrameIndex, 0, frames.Length - 1);
            spriteRenderer.sprite = frames[currentFrameIndex];
        }

        private Sprite[] GetFrames(HeroAnimationAction action)
        {
            switch (action)
            {
                case HeroAnimationAction.Run:
                    return runFrames;
                case HeroAnimationAction.Jump:
                    return jumpFrames;
                case HeroAnimationAction.Dash:
                    return dashFrames;
                case HeroAnimationAction.Retreat:
                    return retreatFrames;
                case HeroAnimationAction.Hurt:
                    return hurtFrames;
                case HeroAnimationAction.Heal:
                    return healFrames;
                case HeroAnimationAction.AimBow:
                    return aimBowFrames;
                case HeroAnimationAction.ShootBow:
                    return shootBowFrames;
                case HeroAnimationAction.Leap:
                    return leapFrames;
                case HeroAnimationAction.Plunge:
                    return plungeFrames;
                case HeroAnimationAction.Death:
                    return deathFrames;
                default:
                    return idleFrames;
            }
        }

        private float GetFramesPerSecond(HeroAnimationAction action)
        {
            switch (action)
            {
                case HeroAnimationAction.Idle:
                    return idleFramesPerSecond;
                case HeroAnimationAction.Run:
                    return runFramesPerSecond;
                case HeroAnimationAction.Jump:
                    return jumpFramesPerSecond;
                case HeroAnimationAction.Dash:
                    return dashFramesPerSecond;
                case HeroAnimationAction.Retreat:
                    return retreatFramesPerSecond;
                default:
                    return combatFramesPerSecond;
            }
        }

        private static bool ShouldLoop(HeroAnimationAction action)
        {
            return action == HeroAnimationAction.Idle
                || action == HeroAnimationAction.Run
                || action == HeroAnimationAction.Retreat
                || action == HeroAnimationAction.AimBow;
        }

        private void OnValidate()
        {
            defaultFramesPerSecond = Mathf.Max(0.01f, defaultFramesPerSecond);
            idleFramesPerSecond = Mathf.Max(0.01f, idleFramesPerSecond);
            runFramesPerSecond = Mathf.Max(0.01f, runFramesPerSecond);
            jumpFramesPerSecond = Mathf.Max(0.01f, jumpFramesPerSecond);
            dashFramesPerSecond = Mathf.Max(0.01f, dashFramesPerSecond);
            retreatFramesPerSecond = Mathf.Max(0.01f, retreatFramesPerSecond);
            combatFramesPerSecond = Mathf.Max(0.01f, combatFramesPerSecond);
        }
    }
}
