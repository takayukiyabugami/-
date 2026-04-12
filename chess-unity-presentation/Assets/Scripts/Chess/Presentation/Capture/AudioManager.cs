using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Chess.Presentation
{
    public sealed class AudioManager : MonoBehaviour, ICaptureCuePlayer
    {
        [Serializable]
        private struct CueClipBinding
        {
            public CaptureCueId cue;
            public AudioClip[] clips;
            public bool spatial3D;
            [Range(0f, 1f)] public float baseVolume;
            [Range(0f, 1f)] public float minPitch;
            [Range(0f, 2f)] public float maxPitch;
            [Min(1)] public int maxVoices;
        }

        [SerializeField] private CaptureEventBus eventBus;
        [SerializeField] private BudgetMonitor budgetMonitor;
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private string bgmDuckParam = "BgmDuckDb";
        [SerializeField] private float duckAttackSeconds = 0.01f;
        [SerializeField] private float duckHoldSeconds = 0.08f;
        [SerializeField] private float duckReleaseSeconds = 0.14f;
        [SerializeField] private CueClipBinding[] bindings = Array.Empty<CueClipBinding>();
        [SerializeField] private int pooledSources = 16;

        private readonly Dictionary<CaptureCueId, CueClipBinding> _bindingByCue = new Dictionary<CaptureCueId, CueClipBinding>(16);
        private readonly Dictionary<CaptureCueId, int> _activeVoicesByCue = new Dictionary<CaptureCueId, int>(16);
        private readonly Queue<AudioSource> _availableSources = new Queue<AudioSource>(32);
        private readonly List<AudioSource> _inUseSources = new List<AudioSource>(32);

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

            BuildBindingMap();
            BuildSourcePool();
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
            if (!_bindingByCue.TryGetValue(cue, out CueClipBinding binding))
            {
                return;
            }

            if (binding.clips == null || binding.clips.Length == 0)
            {
                return;
            }

            _activeVoicesByCue.TryGetValue(cue, out int voices);
            if (voices >= Mathf.Max(1, binding.maxVoices))
            {
                return;
            }

            AudioSource source = AcquireSource();
            if (source == null)
            {
                return;
            }

            float start = Time.realtimeSinceStartup;
            AudioClip clip = binding.clips[UnityEngine.Random.Range(0, binding.clips.Length)];
            source.transform.position = ctx.position;
            source.spatialBlend = binding.spatial3D ? 1f : 0f;
            source.volume = Mathf.Clamp01(binding.baseVolume * Mathf.Lerp(0.7f, 1f, ctx.intensity));
            source.pitch = UnityEngine.Random.Range(binding.minPitch, binding.maxPitch);
            source.clip = clip;
            source.Play();

            _activeVoicesByCue[cue] = voices + 1;
            StartCoroutine(ReleaseAfterPlay(source, cue, clip.length));
            if (cue == CaptureCueId.Impact)
            {
                StartCoroutine(ApplyDucking());
            }

            budgetMonitor?.ReportAudioCost(ctx.moveSerial, (Time.realtimeSinceStartup - start) * 1000f);
        }

        private IEnumerator ReleaseAfterPlay(AudioSource source, CaptureCueId cue, float duration)
        {
            yield return new WaitForSeconds(duration);
            source.Stop();
            source.clip = null;
            _inUseSources.Remove(source);
            _availableSources.Enqueue(source);

            _activeVoicesByCue.TryGetValue(cue, out int voices);
            _activeVoicesByCue[cue] = Mathf.Max(0, voices - 1);
        }

        private IEnumerator ApplyDucking()
        {
            if (audioMixer == null || string.IsNullOrEmpty(bgmDuckParam))
            {
                yield break;
            }

            float targetDb = -5.5f;
            audioMixer.GetFloat(bgmDuckParam, out float currentDb);
            float elapsed = 0f;
            while (elapsed < duckAttackSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duckAttackSeconds);
                audioMixer.SetFloat(bgmDuckParam, Mathf.Lerp(currentDb, targetDb, t));
                yield return null;
            }

            yield return new WaitForSecondsRealtime(duckHoldSeconds);

            elapsed = 0f;
            while (elapsed < duckReleaseSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duckReleaseSeconds);
                audioMixer.SetFloat(bgmDuckParam, Mathf.Lerp(targetDb, 0f, t));
                yield return null;
            }

            audioMixer.SetFloat(bgmDuckParam, 0f);
        }

        private AudioSource AcquireSource()
        {
            if (_availableSources.Count == 0)
            {
                return null;
            }

            AudioSource source = _availableSources.Dequeue();
            _inUseSources.Add(source);
            return source;
        }

        private void BuildBindingMap()
        {
            _bindingByCue.Clear();
            for (int i = 0; i < bindings.Length; i++)
            {
                CueClipBinding binding = bindings[i];
                if (binding.clips == null || binding.clips.Length == 0)
                {
                    continue;
                }

                if (binding.minPitch <= 0f)
                {
                    binding.minPitch = 0.96f;
                }

                if (binding.maxPitch <= 0f || binding.maxPitch < binding.minPitch)
                {
                    binding.maxPitch = Mathf.Max(1.04f, binding.minPitch);
                }

                if (binding.baseVolume <= 0f)
                {
                    binding.baseVolume = 0.9f;
                }

                _bindingByCue[binding.cue] = binding;
            }
        }

        private void BuildSourcePool()
        {
            int count = Mathf.Max(4, pooledSources);
            for (int i = 0; i < count; i++)
            {
                GameObject node = new GameObject($"CaptureAudioSource_{i}");
                node.transform.SetParent(transform, false);
                AudioSource source = node.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = 1f;
                source.maxDistance = 20f;
                _availableSources.Enqueue(source);
            }
        }
    }
}
