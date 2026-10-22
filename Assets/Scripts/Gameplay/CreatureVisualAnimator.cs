using UnityEngine;

namespace MythMayhem.Gameplay
{
    // Code-driven "juice" used when a creature's imported clips contain no baked motion:
    // a spawn pop-in, an attack lunge and a death topple. It animates the visual child so the
    // creature root (whose position/scale the factory sets) is left untouched, which lets it
    // compose on top of a real Animator once the FBX clips actually animate.
    public class CreatureVisualAnimator : MonoBehaviour
    {
        [SerializeField] private Transform visual;
        [SerializeField] private float spawnDuration = 0.4f;
        [SerializeField] private float attackDuration = 0.4f;
        [SerializeField] private float attackLunge = 0.4f;
        [SerializeField] private float attackLeanDegrees = 18f;
        [SerializeField] private float deathFallDuration = 0.8f;
        [SerializeField] private float deathFallDegrees = 85f;
        [SerializeField] private float deathSink = 0.35f;

        private Vector3 baseScale = Vector3.one;
        private Vector3 baseLocalPosition;
        private Quaternion baseLocalRotation = Quaternion.identity;
        private float spawnTime;
        private float attackTime = -1f;
        private float deathTime = -1f;
        private bool bound;
        private bool dead;

        // Picks the model child (the Animator, or the first renderer) and captures its rest pose.
        public void Bind(GameObject root)
        {
            if (visual == null)
            {
                Animator animator = root.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    visual = animator.transform;
                }
                else
                {
                    Renderer renderer = root.GetComponentInChildren<Renderer>();
                    visual = renderer != null ? renderer.transform : root.transform;
                }
            }

            if (visual == null || bound)
                return;

            baseScale = visual.localScale;
            baseLocalPosition = visual.localPosition;
            baseLocalRotation = visual.localRotation;
            spawnTime = 0f;
            bound = true;
            Apply();
        }

        private void Start()
        {
            if (!bound)
                Bind(gameObject);
        }

        private void Update()
        {
            if (!bound)
                return;

            float dt = Time.deltaTime;

            if (dead)
            {
                deathTime += dt;
                Apply();
                return;
            }

            if (spawnTime < spawnDuration)
                spawnTime += dt;

            if (attackTime >= 0f)
            {
                attackTime += dt;
                if (attackTime >= attackDuration)
                    attackTime = -1f;
            }

            Apply();
        }

        public void PlayAttack()
        {
            if (dead)
                return;

            attackTime = 0f;
        }

        public void PlayDeath()
        {
            if (dead)
                return;

            dead = true;
            deathTime = 0f;
            spawnTime = spawnDuration;
            Apply();
        }

        private void Apply()
        {
            if (visual == null)
                return;

            if (dead)
            {
                float fall = EaseOutCubic(deathTime / deathFallDuration);
                visual.localScale = baseScale;
                visual.localRotation = baseLocalRotation * Quaternion.Euler(-deathFallDegrees * fall, 0f, 0f);
                visual.localPosition = baseLocalPosition + Vector3.down * (deathSink * fall);
                return;
            }

            float spawnProgress = spawnDuration > 0f ? Mathf.Clamp01(spawnTime / spawnDuration) : 1f;
            float pop = EaseOutBack(spawnProgress);

            float lunge = 0f;
            float lean = 0f;
            if (attackTime >= 0f && attackDuration > 0f)
            {
                float attackProgress = Mathf.Clamp01(attackTime / attackDuration);
                lunge = Mathf.Sin(attackProgress * Mathf.PI);
                lean = lunge;
            }

            visual.localScale = baseScale * pop;
            visual.localPosition = baseLocalPosition + Vector3.forward * (attackLunge * lunge);
            visual.localRotation = baseLocalRotation * Quaternion.Euler(attackLeanDegrees * lean, 0f, 0f);
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }

        private static float EaseOutCubic(float t)
        {
            float u = Mathf.Clamp01(t);
            float inv = 1f - u;
            return 1f - inv * inv * inv;
        }
    }
}
