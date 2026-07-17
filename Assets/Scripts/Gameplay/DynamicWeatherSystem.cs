using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace RushBank.Gameplay
{
    public enum WeatherState
    {
        Sunny,
        Rainy
    }

    public class DynamicWeatherSystem : MonoBehaviour
    {
        public static DynamicWeatherSystem Instance { get; private set; }

        [Header("Cycle")]
        [SerializeField] private bool startCycleOnEnable = true;
        [SerializeField] private WeatherState initialState = WeatherState.Sunny;
        [SerializeField, Min(1f)] private float sunnyDuration = 90f;
        [SerializeField, Min(1f)] private float rainyDuration = 35f;
        [SerializeField, Min(0.1f)] private float transitionSeconds = 3f;

        [Header("Patience Balance")]
        [SerializeField, Min(0f)] private float sunnyPatienceMultiplier = 1f;
        [SerializeField, Min(1f)] private float rainyPatienceMultiplier = 1.15f;

        [Header("Lighting")]
        [SerializeField] private Light sunLight;
        [SerializeField] private Color sunnyLightColor = new Color(1f, 0.86f, 0.48f);
        [SerializeField] private Color rainyLightColor = new Color(0.55f, 0.67f, 0.92f);
        [SerializeField, Min(0f)] private float sunnyLightIntensity = 1.15f;
        [SerializeField, Min(0f)] private float rainyLightIntensity = 0.68f;
        [SerializeField] private Color sunnyAmbientColor = new Color(0.92f, 0.82f, 0.62f);
        [SerializeField] private Color rainyAmbientColor = new Color(0.36f, 0.43f, 0.58f);

        [Header("Rain Feedback")]
        [SerializeField] private ParticleSystem rainWindowParticles;
        [SerializeField] private AudioSource rainAudioSource;
        [SerializeField] private AudioClip rainLoopClip;
        [SerializeField, Range(0f, 1f)] private float rainAudioVolume = 0.35f;

        [Header("Customer Visuals")]
        [SerializeField] private bool enableUmbrellasDuringRain = true;

        public UnityEvent<WeatherState> OnWeatherChanged = new UnityEvent<WeatherState>();
        public UnityEvent<float> OnPatienceMultiplierChanged = new UnityEvent<float>();

        private Coroutine cycleRoutine;
        private Coroutine transitionRoutine;
        private Coroutine rainAudioFadeRoutine;
        private WeatherState currentState;

        public float activePatienceMultiplier = 1f;
        public WeatherState CurrentState => currentState;
        public bool IsRainy => currentState == WeatherState.Rainy;
        public bool HasUmbrellaForNewCustomers => enableUmbrellasDuringRain && IsRainy;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ResolveMissingReferences();
            ApplyStateImmediate(initialState);
        }

        private void OnEnable()
        {
            if (startCycleOnEnable && cycleRoutine == null)
            {
                cycleRoutine = StartCoroutine(WeatherCycleRoutine());
            }
        }

        private void OnDisable()
        {
            if (cycleRoutine != null)
            {
                StopCoroutine(cycleRoutine);
                cycleRoutine = null;
            }

            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
            }

            if (rainAudioFadeRoutine != null)
            {
                StopCoroutine(rainAudioFadeRoutine);
                rainAudioFadeRoutine = null;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void ForceSunny()
        {
            StartWeatherTransition(WeatherState.Sunny);
        }

        public void ForceRainy()
        {
            StartWeatherTransition(WeatherState.Rainy);
        }

        public void SetCycleDurations(float sunnySeconds, float rainySeconds)
        {
            sunnyDuration = Mathf.Max(1f, sunnySeconds);
            rainyDuration = Mathf.Max(1f, rainySeconds);
        }

        private IEnumerator WeatherCycleRoutine()
        {
            while (enabled)
            {
                if (currentState != WeatherState.Sunny)
                {
                    StartWeatherTransition(WeatherState.Sunny);
                }

                yield return new WaitForSeconds(sunnyDuration);
                StartWeatherTransition(WeatherState.Rainy);
                yield return new WaitForSeconds(rainyDuration);
                StartWeatherTransition(WeatherState.Sunny);
            }
        }

        private void StartWeatherTransition(WeatherState nextState)
        {
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
            }

            transitionRoutine = StartCoroutine(TransitionWeatherRoutine(nextState));
        }

        private IEnumerator TransitionWeatherRoutine(WeatherState nextState)
        {
            var fromLightColor = sunLight != null ? sunLight.color : Color.white;
            var fromLightIntensity = sunLight != null ? sunLight.intensity : 1f;
            var fromAmbient = RenderSettings.ambientLight;
            var toLightColor = nextState == WeatherState.Rainy ? rainyLightColor : sunnyLightColor;
            var toLightIntensity = nextState == WeatherState.Rainy ? rainyLightIntensity : sunnyLightIntensity;
            var toAmbient = nextState == WeatherState.Rainy ? rainyAmbientColor : sunnyAmbientColor;

            if (nextState == WeatherState.Rainy)
            {
                EnableRainFeedback();
            }
            else
            {
                FadeOutRainAudio();
                StopRainParticles();
            }

            var elapsed = 0f;
            while (elapsed < transitionSeconds)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, elapsed / transitionSeconds);
                ApplyLighting(Color.Lerp(fromLightColor, toLightColor, t), Mathf.Lerp(fromLightIntensity, toLightIntensity, t), Color.Lerp(fromAmbient, toAmbient, t));
                yield return null;
            }

            currentState = nextState;
            activePatienceMultiplier = nextState == WeatherState.Rainy ? rainyPatienceMultiplier : sunnyPatienceMultiplier;
            ApplyLighting(toLightColor, toLightIntensity, toAmbient);

            if (nextState == WeatherState.Sunny)
            {
                EnsureRainFeedbackStopped();
            }

            OnWeatherChanged.Invoke(currentState);
            OnPatienceMultiplierChanged.Invoke(activePatienceMultiplier);
            transitionRoutine = null;
        }

        private void ApplyStateImmediate(WeatherState state)
        {
            currentState = state;
            activePatienceMultiplier = state == WeatherState.Rainy ? rainyPatienceMultiplier : sunnyPatienceMultiplier;

            var lightColor = state == WeatherState.Rainy ? rainyLightColor : sunnyLightColor;
            var intensity = state == WeatherState.Rainy ? rainyLightIntensity : sunnyLightIntensity;
            var ambient = state == WeatherState.Rainy ? rainyAmbientColor : sunnyAmbientColor;
            ApplyLighting(lightColor, intensity, ambient);

            if (state == WeatherState.Rainy)
            {
                EnableRainFeedback();
            }
            else
            {
                EnsureRainFeedbackStopped();
            }
        }

        private void ApplyLighting(Color lightColor, float intensity, Color ambient)
        {
            if (sunLight != null)
            {
                sunLight.color = lightColor;
                sunLight.intensity = intensity;
            }

            RenderSettings.ambientLight = ambient;
        }

        private void EnableRainFeedback()
        {
            if (rainWindowParticles != null && !rainWindowParticles.isPlaying)
            {
                rainWindowParticles.Play();
            }

            if (rainAudioSource == null)
            {
                return;
            }

            if (rainAudioFadeRoutine != null)
            {
                StopCoroutine(rainAudioFadeRoutine);
                rainAudioFadeRoutine = null;
            }

            if (rainLoopClip != null)
            {
                rainAudioSource.clip = rainLoopClip;
            }

            rainAudioSource.loop = true;
            rainAudioSource.volume = rainAudioVolume;
            if (!rainAudioSource.isPlaying)
            {
                rainAudioSource.Play();
            }
        }

        private void FadeOutRainAudio()
        {
            if (rainAudioSource == null)
            {
                return;
            }

            if (rainAudioFadeRoutine != null)
            {
                StopCoroutine(rainAudioFadeRoutine);
            }

            rainAudioFadeRoutine = StartCoroutine(FadeRainAudioRoutine(rainAudioSource.volume, 0f, transitionSeconds));
        }

        private IEnumerator FadeRainAudioRoutine(float fromVolume, float toVolume, float seconds)
        {
            var elapsed = 0f;
            while (elapsed < seconds && rainAudioSource != null)
            {
                elapsed += Time.deltaTime;
                rainAudioSource.volume = Mathf.Lerp(fromVolume, toVolume, Mathf.Clamp01(elapsed / seconds));
                yield return null;
            }

            if (rainAudioSource != null)
            {
                rainAudioSource.Stop();
                rainAudioSource.volume = rainAudioVolume;
            }

            rainAudioFadeRoutine = null;
        }

        private void StopRainParticles()
        {
            if (rainWindowParticles != null)
            {
                rainWindowParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private void EnsureRainFeedbackStopped()
        {
            StopRainParticles();
            if (rainAudioSource != null)
            {
                rainAudioSource.Stop();
                rainAudioSource.volume = rainAudioVolume;
            }
        }

        private void ResolveMissingReferences()
        {
            if (sunLight == null)
            {
                sunLight = RenderSettings.sun;
            }

            if (rainAudioSource == null)
            {
                rainAudioSource = GetComponent<AudioSource>();
            }
        }
    }
}
