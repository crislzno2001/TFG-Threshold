using UnityEngine;

namespace Sprout.Presentation
{
    /// <summary>
    /// Procedural "cute" animation for NPCs that have no rigged clips. Animates a
    /// visual child transform (scale/position/rotation) per emotional state, in the
    /// Animal-Crossing/Tomodachi register. Swap in real animations later by simply
    /// removing this component — gameplay does not depend on it.
    /// </summary>
    public class ProceduralNpcAnimator : MonoBehaviour
    {
        public enum NpcMood { Idle, Talking, Happy, Offended, Sad, Gift }

        [Tooltip("Child transform that holds the mesh. Defaults to this transform.")]
        [SerializeField] private Transform visual;
        [SerializeField] private float bobHeight = 0.06f;
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float talkSpeed = 9f;
        [SerializeField] private float shakeAngle = 12f;

        private NpcMood _mood = NpcMood.Idle;
        private Vector3 _baseLocalPos;
        private Vector3 _baseScale;
        private Quaternion _baseRot;
        private float _t;
        private float _moodTimer; // for transient moods (happy/offended/gift)

        private void Awake()
        {
            if (visual == null) visual = transform;
            _baseLocalPos = visual.localPosition;
            _baseScale = visual.localScale;
            _baseRot = visual.localRotation;
        }

        public void SetMood(NpcMood mood, float transientSeconds = 1.2f)
        {
            _mood = mood;
            _moodTimer = (mood == NpcMood.Happy || mood == NpcMood.Offended || mood == NpcMood.Gift)
                ? transientSeconds : 0f;
        }

        // String entry points so UnityEvents (dialogue reactions) can call them.
        public void PlayIdle()     => SetMood(NpcMood.Idle);
        public void PlayTalking()  => SetMood(NpcMood.Talking);
        public void PlayHappy()    => SetMood(NpcMood.Happy);
        public void PlayOffended() => SetMood(NpcMood.Offended);
        public void PlaySad()      => SetMood(NpcMood.Sad);
        public void PlayGift()     => SetMood(NpcMood.Gift);

        private void Update()
        {
            _t += Time.deltaTime;
            if (visual == null) return;

            Vector3 pos = _baseLocalPos;
            Vector3 scale = _baseScale;
            Quaternion rot = _baseRot;

            switch (_mood)
            {
                case NpcMood.Idle:
                    pos.y += Mathf.Sin(_t * bobSpeed) * bobHeight;
                    break;

                case NpcMood.Talking:
                    float b = Mathf.Abs(Mathf.Sin(_t * talkSpeed));
                    scale = _baseScale + new Vector3(0, b * 0.05f, 0);
                    pos.y += b * bobHeight * 0.5f;
                    break;

                case NpcMood.Happy:
                    pos.y += Mathf.Abs(Mathf.Sin(_t * 8f)) * bobHeight * 2.2f; // little hops
                    scale = _baseScale * (1f + Mathf.Sin(_t * 8f) * 0.04f);
                    break;

                case NpcMood.Offended:
                    rot = _baseRot * Quaternion.Euler(0, 0, Mathf.Sin(_t * 30f) * shakeAngle);
                    break;

                case NpcMood.Sad:
                    rot = _baseRot * Quaternion.Euler(Mathf.LerpAngle(0, 14f, 0.5f + 0.5f * Mathf.Sin(_t)), 0, 0);
                    pos.y -= bobHeight * 0.6f; // droop
                    scale = _baseScale + new Vector3(0.02f, -0.05f, 0.02f);
                    break;

                case NpcMood.Gift:
                    pos.y += Mathf.Abs(Mathf.Sin(_t * 10f)) * bobHeight * 1.6f;
                    rot = _baseRot * Quaternion.Euler(0, Mathf.Sin(_t * 10f) * 6f, 0);
                    break;
            }

            visual.localPosition = Vector3.Lerp(visual.localPosition, pos, 0.4f);
            visual.localScale = Vector3.Lerp(visual.localScale, scale, 0.4f);
            visual.localRotation = Quaternion.Slerp(visual.localRotation, rot, 0.4f);

            if (_moodTimer > 0f)
            {
                _moodTimer -= Time.deltaTime;
                if (_moodTimer <= 0f) _mood = NpcMood.Idle;
            }
        }
    }
}
