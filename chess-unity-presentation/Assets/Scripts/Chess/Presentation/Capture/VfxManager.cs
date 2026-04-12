using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Chess.Presentation
{
    public sealed class VfxManager : MonoBehaviour, ICaptureCuePlayer
    {
        [Serializable]
        private struct CueVfxBinding
        {
            public CaptureCueId cue;
            public GameObject prefab;
            [Min(0.01f)] public float lifetimeSeconds;
            [Min(1)] public int highPool;
            [Min(1)] public int mediumPool;
            [Min(1)] public int lowPool;
            public bool faceForward;
        }

        [SerializeField] private CaptureEventBus eventBus;
        [SerializeField] private BudgetMonitor budgetMonitor;
        [SerializeField] private QualityGovernor qualityGovernor;
        [SerializeField] private Transform poolRoot;
        [SerializeField] private Color whiteColor = new Color(0.95f, 0.95f, 1f, 1f);
        [SerializeField] private Color blackColor = new Color(0.45f, 0.45f, 0.55f, 1f);
        [SerializeField] private CueVfxBinding[] bindings = Array.Empty<CueVfxBinding>();

        private readonly Dictionary<CaptureCueId, CuePool> _poolByCue = new Dictionary<CaptureCueId, CuePool>(16);
        private readonly MaterialPropertyBlock _propertyBlock = new MaterialPropertyBlock();

        private void Awake()
        {
            if (eventBus == null)
            {
                eventBus = FindObjectOfType<CaptureEventBus>();
            }

            if (budgetMonitor == null)
            {
                budgetMonitor = FindObjectOfType<BudgetMonitor>();
            }

            if (qualityGovernor == null)
            {
                qualityGovernor = FindObjectOfType<QualityGovernor>();
            }

            if (poolRoot == null)
            {
                GameObject root = new GameObject("VfxPoolRoot");
                root.transform.SetParent(transform, false);
                poolRoot = root.transform;
            }

            BuildPools();
        }

        private void OnEnable()
        {
            if (eventBus != null)
            {
                eventBus.CuePublished += HandleCue;
            }
        }

        private void OnDisable()
        {
            if (eventBus != null)
            {
                eventBus.CuePublished -= HandleCue;
            }
        }

        private void HandleCue(CaptureCueId cue, CaptureCueContext ctx)
        {
            Play(cue, ctx);
        }

        public void Play(CaptureCueId cue, in CaptureCueContext ctx)
        {
            if (!_poolByCue.TryGetValue(cue, out CuePool pool))
            {
                return;
            }

            float start = Time.realtimeSinceStartup;
            GameObject instance = pool.Acquire();
            if (instance == null)
            {
                return;
            }

            Transform t = instance.transform;
            t.position = ctx.position;
            if (pool.FaceForward)
            {
                Vector3 forward = ctx.forward.sqrMagnitude > 0.001f ? ctx.forward : Vector3.forward;
                t.rotation = Quaternion.LookRotation(forward, Vector3.up);
            }

            ApplySideColor(instance, ctx.side);
            instance.SetActive(true);
            StartCoroutine(ReturnAfterLifetime(pool, instance, pool.LifetimeSeconds));
            budgetMonitor?.ReportVfxCost(ctx.moveSerial, (Time.realtimeSinceStartup - start) * 1000f);
        }

        private IEnumerator ReturnAfterLifetime(CuePool pool, GameObject instance, float lifetime)
        {
            yield return new WaitForSeconds(lifetime);
            instance.SetActive(false);
            pool.Release(instance);
        }

        private void ApplySideColor(GameObject instance, ChessSide side)
        {
            Color color = side == ChessSide.White ? whiteColor : blackColor;
            _propertyBlock.SetColor("_BaseColor", color);
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].SetPropertyBlock(_propertyBlock);
            }
        }

        private void BuildPools()
        {
            _poolByCue.Clear();
            for (int i = 0; i < bindings.Length; i++)
            {
                CueVfxBinding binding = bindings[i];
                if (binding.prefab == null)
                {
                    continue;
                }

                int poolSize = ResolvePoolSize(binding);
                CuePool pool = new CuePool(binding.prefab, poolSize, binding.lifetimeSeconds, binding.faceForward, poolRoot);
                _poolByCue[binding.cue] = pool;
            }
        }

        private int ResolvePoolSize(CueVfxBinding binding)
        {
            CaptureQualityPreset preset = qualityGovernor != null ? qualityGovernor.Preset : CaptureQualityPreset.High;
            if (preset == CaptureQualityPreset.Low)
            {
                return binding.lowPool;
            }

            if (preset == CaptureQualityPreset.Medium)
            {
                return binding.mediumPool;
            }

            return binding.highPool;
        }

        private sealed class CuePool
        {
            private readonly Queue<GameObject> _available;
            private readonly List<GameObject> _all;

            public CuePool(GameObject prefab, int size, float lifetimeSeconds, bool faceForward, Transform parent)
            {
                LifetimeSeconds = Mathf.Max(0.01f, lifetimeSeconds);
                FaceForward = faceForward;
                _available = new Queue<GameObject>(size);
                _all = new List<GameObject>(size);

                for (int i = 0; i < size; i++)
                {
                    GameObject instance = UnityEngine.Object.Instantiate(prefab, parent);
                    instance.SetActive(false);
                    _available.Enqueue(instance);
                    _all.Add(instance);
                }
            }

            public float LifetimeSeconds { get; }
            public bool FaceForward { get; }

            public GameObject Acquire()
            {
                if (_available.Count == 0)
                {
                    return null;
                }

                return _available.Dequeue();
            }

            public void Release(GameObject instance)
            {
                if (instance != null)
                {
                    _available.Enqueue(instance);
                }
            }
        }
    }
}
