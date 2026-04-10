using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Chess.Presentation
{
    public sealed class ChessPieceMover : MonoBehaviour, IChessMovePresentation
    {
        private const string ParamSpeed = "Speed";
        private const string ParamAttackType = "AttackType";
        private const string ParamHitLevel = "HitLevel";
        private const string ParamIsCapturing = "IsCapturing";
        private const string ParamIsMoving = "IsMoving";
        private const string ParamMotionPhase = "MotionPhase";
        private const string ParamInterrupt = "Interrupt";

        private const int PhaseMove = 0;
        private const int PhaseAnticipation = 1;
        private const int PhaseDash = 2;
        private const int PhaseImpact = 3;
        private const int PhaseRecovery = 4;

        [Serializable]
        public struct PieceMotionPreset
        {
            public ChessPieceType pieceType;
            [Min(0.05f)] public float moveDuration;
            [Min(0.05f)] public float rotateDuration;
            [Min(0.0f)] public float arcHeight;
            [Min(0.01f)] public float dashDistanceScale;
            [Min(0.05f)] public float anticipationDuration;
            [Min(0.05f)] public float dashDuration;
            [Min(0.01f)] public float impactDuration;
            [Min(0.05f)] public float recoveryDuration;
            [Range(0f, 60f)] public float hitLevel;
        }

        [Header("General Motion")]
        [SerializeField] private bool useRootMotion = false;
        [SerializeField] private bool allowManualCorrectionInRootMotion = true;
        [SerializeField] private float positionTolerance = 0.01f;
        [SerializeField] private float lookAheadWeight = 0.8f;
        [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve dashCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve anticipationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Piece Presets")]
        [SerializeField] private PieceMotionPreset[] presets = new PieceMotionPreset[6]
        {
            new PieceMotionPreset
            {
                pieceType = ChessPieceType.Pawn,
                moveDuration = 0.24f,
                rotateDuration = 0.16f,
                arcHeight = 0.04f,
                dashDistanceScale = 0.95f,
                anticipationDuration = 0.12f,
                dashDuration = 0.11f,
                impactDuration = 0.07f,
                recoveryDuration = 0.15f,
                hitLevel = 24f,
            },
            new PieceMotionPreset
            {
                pieceType = ChessPieceType.Knight,
                moveDuration = 0.35f,
                rotateDuration = 0.22f,
                arcHeight = 0.22f,
                dashDistanceScale = 1.05f,
                anticipationDuration = 0.16f,
                dashDuration = 0.12f,
                impactDuration = 0.08f,
                recoveryDuration = 0.18f,
                hitLevel = 38f,
            },
            new PieceMotionPreset
            {
                pieceType = ChessPieceType.Bishop,
                moveDuration = 0.28f,
                rotateDuration = 0.18f,
                arcHeight = 0.06f,
                dashDistanceScale = 1.0f,
                anticipationDuration = 0.14f,
                dashDuration = 0.10f,
                impactDuration = 0.06f,
                recoveryDuration = 0.14f,
                hitLevel = 28f,
            },
            new PieceMotionPreset
            {
                pieceType = ChessPieceType.Rook,
                moveDuration = 0.30f,
                rotateDuration = 0.20f,
                arcHeight = 0.02f,
                dashDistanceScale = 1.0f,
                anticipationDuration = 0.15f,
                dashDuration = 0.12f,
                impactDuration = 0.09f,
                recoveryDuration = 0.19f,
                hitLevel = 42f,
            },
            new PieceMotionPreset
            {
                pieceType = ChessPieceType.Queen,
                moveDuration = 0.27f,
                rotateDuration = 0.17f,
                arcHeight = 0.07f,
                dashDistanceScale = 1.1f,
                anticipationDuration = 0.13f,
                dashDuration = 0.10f,
                impactDuration = 0.07f,
                recoveryDuration = 0.15f,
                hitLevel = 45f,
            },
            new PieceMotionPreset
            {
                pieceType = ChessPieceType.King,
                moveDuration = 0.31f,
                rotateDuration = 0.22f,
                arcHeight = 0.03f,
                dashDistanceScale = 0.9f,
                anticipationDuration = 0.11f,
                dashDuration = 0.12f,
                impactDuration = 0.08f,
                recoveryDuration = 0.17f,
                hitLevel = 34f,
            },
        };

        [Header("Capture Offsets")]
        [SerializeField] private float anticipationBackStep = 0.14f;
        [SerializeField] private float impactPushScale = 0.08f;

        public event Action<string> VfxCueRequested;
        public event Action<string> SeCueRequested;

        private readonly Dictionary<ChessPieceType, PieceMotionPreset> _presetMap = new Dictionary<ChessPieceType, PieceMotionPreset>(6);
        private int _motionVersion;

        private void Awake()
        {
            CachePresetMap();
        }

        private void OnValidate()
        {
            positionTolerance = Mathf.Clamp(positionTolerance, 0.0001f, 0.05f);
            lookAheadWeight = Mathf.Clamp01(lookAheadWeight);
            if (Application.isPlaying)
            {
                CachePresetMap();
            }
        }

        public IEnumerator PlayMove(in MoveValidationResult validationResult, Action onMoveMidpointEvent)
        {
            int version = ++_motionVersion;
            if (!TryGetPreset(validationResult.movingPieceType, out PieceMotionPreset preset))
            {
                throw new InvalidOperationException($"No motion preset for {validationResult.movingPieceType}");
            }

            Transform piece = validationResult.movingPiece;
            if (piece == null)
            {
                throw new InvalidOperationException("Moving piece transform is null.");
            }

            Animator animator = piece.GetComponent<Animator>();
            SetupAnimator(animator, isCapturing: false, speed: 1f, attackType: 0, hitLevel: 0f, phase: PhaseMove);

            Vector3 start = piece.position;
            Vector3 end = validationResult.worldTo;
            Quaternion startRot = piece.rotation;
            Quaternion endRot = ResolveTargetRotation(validationResult.worldFacing, startRot);

            float elapsed = 0f;
            bool midpointFired = false;
            float duration = Mathf.Max(0.01f, preset.moveDuration);

            while (elapsed < duration)
            {
                if (IsInterrupted(version))
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float moveT = moveCurve.Evaluate(t);
                float arc = Mathf.Sin(t * Mathf.PI) * preset.arcHeight;
                Vector3 target = Vector3.LerpUnclamped(start, end, moveT) + Vector3.up * arc;

                if (!useRootMotion || allowManualCorrectionInRootMotion)
                {
                    piece.position = target;
                }

                float rotT = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, preset.rotateDuration));
                piece.rotation = Quaternion.Slerp(startRot, endRot, rotT);

                if (!midpointFired && t >= 0.5f)
                {
                    midpointFired = true;
                    onMoveMidpointEvent?.Invoke();
                    NotifyVfx("MoveMid");
                }

                yield return null;
            }

            SnapToTarget(piece, end, endRot);
            SetupAnimator(animator, isCapturing: false, speed: 0f, attackType: 0, hitLevel: 0f, phase: PhaseMove);
        }

        public IEnumerator PlayCapture(in MoveValidationResult validationResult, Action onImpactEvent)
        {
            int version = ++_motionVersion;
            if (!TryGetPreset(validationResult.movingPieceType, out PieceMotionPreset preset))
            {
                throw new InvalidOperationException($"No motion preset for {validationResult.movingPieceType}");
            }

            Transform attacker = validationResult.movingPiece;
            Transform victim = validationResult.capturedPiece;
            if (attacker == null)
            {
                throw new InvalidOperationException("Attacker transform is null.");
            }

            Animator attackerAnimator = attacker.GetComponent<Animator>();
            SetupAnimator(
                attackerAnimator,
                isCapturing: true,
                speed: 1f,
                attackType: (int)validationResult.movingPieceType,
                hitLevel: preset.hitLevel,
                phase: PhaseAnticipation);

            Vector3 recoveryTarget = validationResult.worldTo;
            Vector3 attackDirection = (validationResult.worldTo - validationResult.worldFrom);
            if (attackDirection.sqrMagnitude < 0.0001f)
            {
                attackDirection = attacker.forward;
            }

            attackDirection.y = 0f;
            attackDirection.Normalize();

            Vector3 anticipationStart = attacker.position;
            Vector3 anticipationEnd = anticipationStart - attackDirection * anticipationBackStep;
            yield return InterpolateSegment(
                version,
                attacker,
                anticipationStart,
                anticipationEnd,
                preset.anticipationDuration,
                anticipationCurve,
                ResolveTargetRotation(attackDirection, attacker.rotation));

            if (IsInterrupted(version))
            {
                yield break;
            }

            SetupAnimator(attackerAnimator, isCapturing: true, speed: 1.45f, attackType: (int)validationResult.movingPieceType, hitLevel: preset.hitLevel, phase: PhaseDash);
            NotifySe("CaptureDash");

            Vector3 dashTarget = Vector3.LerpUnclamped(anticipationEnd, recoveryTarget, preset.dashDistanceScale);
            yield return InterpolateSegment(version, attacker, anticipationEnd, dashTarget, preset.dashDuration, dashCurve, ResolveTargetRotation(attackDirection, attacker.rotation));

            if (IsInterrupted(version))
            {
                yield break;
            }

            SetupAnimator(attackerAnimator, isCapturing: true, speed: 0.1f, attackType: (int)validationResult.movingPieceType, hitLevel: preset.hitLevel, phase: PhaseImpact);
            onImpactEvent?.Invoke();
            NotifyVfx("CaptureImpact");
            NotifySe("CaptureHit");

            if (victim != null)
            {
                Vector3 pushed = victim.position + attackDirection * impactPushScale;
                victim.position = pushed;
                victim.gameObject.SetActive(false);
            }

            float impactEnd = Time.time + preset.impactDuration;
            while (Time.time < impactEnd)
            {
                if (IsInterrupted(version))
                {
                    yield break;
                }

                yield return null;
            }

            SetupAnimator(attackerAnimator, isCapturing: true, speed: 1f, attackType: (int)validationResult.movingPieceType, hitLevel: preset.hitLevel * 0.5f, phase: PhaseRecovery);
            yield return InterpolateSegment(version, attacker, attacker.position, recoveryTarget, preset.recoveryDuration, moveCurve, ResolveTargetRotation(validationResult.worldFacing, attacker.rotation));

            if (IsInterrupted(version))
            {
                yield break;
            }

            SnapToTarget(attacker, recoveryTarget, ResolveTargetRotation(validationResult.worldFacing, attacker.rotation));
            SetupAnimator(attackerAnimator, isCapturing: false, speed: 0f, attackType: 0, hitLevel: 0f, phase: PhaseRecovery);
        }

        public void CancelPresentation()
        {
            _motionVersion++;
        }

        public void NotifyAnimationEvent(string eventId)
        {
            if (string.IsNullOrEmpty(eventId))
            {
                return;
            }

            if (eventId.StartsWith("VFX_", StringComparison.OrdinalIgnoreCase))
            {
                NotifyVfx(eventId.Substring(4));
                return;
            }

            if (eventId.StartsWith("SE_", StringComparison.OrdinalIgnoreCase))
            {
                NotifySe(eventId.Substring(3));
                return;
            }

            NotifyVfx(eventId);
        }

        private IEnumerator InterpolateSegment(
            int version,
            Transform piece,
            Vector3 start,
            Vector3 end,
            float duration,
            AnimationCurve curve,
            Quaternion desiredRotation)
        {
            float elapsed = 0f;
            Quaternion startRotation = piece.rotation;

            while (elapsed < duration)
            {
                if (IsInterrupted(version))
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
                float eased = curve.Evaluate(t);
                if (!useRootMotion || allowManualCorrectionInRootMotion)
                {
                    piece.position = Vector3.LerpUnclamped(start, end, eased);
                }

                piece.rotation = Quaternion.Slerp(startRotation, desiredRotation, Mathf.Clamp01(t + lookAheadWeight * 0.1f));
                yield return null;
            }
        }

        private Quaternion ResolveTargetRotation(Vector3 desiredForward, Quaternion fallback)
        {
            Vector3 horizontal = desiredForward;
            horizontal.y = 0f;
            if (horizontal.sqrMagnitude < 0.0001f)
            {
                return fallback;
            }

            return Quaternion.LookRotation(horizontal.normalized, Vector3.up);
        }

        private void SnapToTarget(Transform piece, Vector3 targetPosition, Quaternion targetRotation)
        {
            if (Vector3.Distance(piece.position, targetPosition) > positionTolerance)
            {
                piece.position = targetPosition;
            }

            piece.rotation = targetRotation;
        }

        private void SetupAnimator(Animator animator, bool isCapturing, float speed, int attackType, float hitLevel, int phase)
        {
            if (animator == null)
            {
                return;
            }

            animator.applyRootMotion = useRootMotion;
            animator.SetBool(ParamInterrupt, false);
            animator.SetBool(ParamIsMoving, speed > 0.01f);
            animator.SetBool(ParamIsCapturing, isCapturing);
            animator.SetFloat(ParamSpeed, speed);
            animator.SetInteger(ParamAttackType, attackType);
            animator.SetFloat(ParamHitLevel, hitLevel);
            animator.SetInteger(ParamMotionPhase, phase);
        }

        private void NotifyVfx(string id)
        {
            VfxCueRequested?.Invoke(id);
        }

        private void NotifySe(string id)
        {
            SeCueRequested?.Invoke(id);
        }

        private bool TryGetPreset(ChessPieceType pieceType, out PieceMotionPreset preset)
        {
            return _presetMap.TryGetValue(pieceType, out preset);
        }

        private bool IsInterrupted(int version)
        {
            return _motionVersion != version;
        }

        private void CachePresetMap()
        {
            _presetMap.Clear();
            for (int i = 0; i < presets.Length; i++)
            {
                PieceMotionPreset preset = presets[i];
                _presetMap[preset.pieceType] = preset;
            }
        }
    }
}
