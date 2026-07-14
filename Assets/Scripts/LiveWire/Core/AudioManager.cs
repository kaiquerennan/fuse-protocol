using System.Collections;
using UnityEngine;

namespace LiveWire
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        AudioSource tickSource;
        AudioSource hissSource;
        AudioSource electricHissSource;
        AudioSource tensionSource;
        AudioSource oneShotSource;

        AudioClip tickClip;
        AudioClip explosionClip;
        AudioClip shockClip;
        AudioClip successClip;
        AudioClip almostClip;
        AudioClip hissClip;
        AudioClip clickClip;
        AudioClip connectClip;
        AudioClip alertClip;
        AudioClip tensionClip;
        AudioClip reliefClip;
        AudioClip electricHissClip;

        Coroutine tickRoutine;
        float currentTickInterval = 1f;
        float savedHissVolume;
        bool hissPaused;
        bool finalWireMode;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            tickClip = ProceduralAudio.GenerateTick();
            explosionClip = ProceduralAudio.GenerateExplosion();
            shockClip = ProceduralAudio.GenerateShock();
            successClip = ProceduralAudio.GenerateSuccess();
            almostClip = ProceduralAudio.GenerateAlmost();
            hissClip = ProceduralAudio.GenerateHiss();
            clickClip = ProceduralAudio.GenerateClick();
            connectClip = ProceduralAudio.GenerateConnect();
            alertClip = ProceduralAudio.GenerateAlert();
            tensionClip = ProceduralAudio.GenerateTension();
            reliefClip = ProceduralAudio.GenerateRelief();
            electricHissClip = ProceduralAudio.GenerateElectricHiss();

            tickSource = gameObject.AddComponent<AudioSource>();
            tickSource.playOnAwake = false;
            tickSource.loop = false;

            hissSource = gameObject.AddComponent<AudioSource>();
            hissSource.playOnAwake = false;
            hissSource.loop = true;
            hissSource.clip = hissClip;
            hissSource.volume = 0f;

            electricHissSource = gameObject.AddComponent<AudioSource>();
            electricHissSource.playOnAwake = false;
            electricHissSource.loop = true;
            electricHissSource.clip = electricHissClip;
            electricHissSource.volume = 0f;

            tensionSource = gameObject.AddComponent<AudioSource>();
            tensionSource.playOnAwake = false;
            tensionSource.loop = true;
            tensionSource.clip = tensionClip;
            tensionSource.volume = 0f;

            oneShotSource = gameObject.AddComponent<AudioSource>();
            oneShotSource.playOnAwake = false;
        }

        public void StartTicking()
        {
            StopTicking();
            tickRoutine = StartCoroutine(TickLoop());
        }

        public void StopTicking()
        {
            if (tickRoutine != null)
            {
                StopCoroutine(tickRoutine);
                tickRoutine = null;
            }
        }

        public void SetTickIntensity(float normalizedUrgency)
        {
            float u = Mathf.Clamp01(normalizedUrgency);
            currentTickInterval = Mathf.Lerp(1f, 0.12f, u);
            tickSource.pitch = Mathf.Lerp(1f, 1.6f, u);
        }

        IEnumerator TickLoop()
        {
            while (true)
            {
                float vol = finalWireMode ? 0.95f : 0.7f;
                tickSource.PlayOneShot(tickClip, vol);
                yield return new WaitForSeconds(currentTickInterval);
            }
        }

        public void StartHiss()
        {
            if (!hissSource.isPlaying) hissSource.Play();
            hissSource.volume = 0.05f;
            hissPaused = false;
        }

        public void SetHissIntensity(float normalized)
        {
            if (hissPaused) { savedHissVolume = Mathf.Clamp01(normalized) * 0.55f; return; }
            if (!hissSource.isPlaying) hissSource.Play();
            hissSource.volume = Mathf.Clamp01(normalized) * 0.55f;
        }

        public void StopHiss()
        {
            hissSource.volume = 0f;
            hissSource.Stop();
            hissPaused = false;
        }

        public void PauseHiss()
        {
            savedHissVolume = hissSource.volume;
            hissSource.volume = 0f;
            hissPaused = true;
        }

        public void ResumeHiss()
        {
            if (!hissSource.isPlaying) hissSource.Play();
            hissSource.volume = savedHissVolume;
            hissPaused = false;
        }

        public void StartElectricHiss()
        {
            if (!electricHissSource.isPlaying) electricHissSource.Play();
            StartCoroutine(FadeSource(electricHissSource, 0.38f, 1.5f));
        }

        public void StopElectricHiss()
        {
            StartCoroutine(FadeSource(electricHissSource, 0f, 0.3f, stopAfter: true));
        }

        public void StartTensionRising()
        {
            if (!tensionSource.isPlaying) tensionSource.Play();
            StartCoroutine(FadeSource(tensionSource, 0.5f, 0.25f));
        }

        public void StopTensionRising()
        {
            StartCoroutine(FadeSource(tensionSource, 0f, 0.15f, stopAfter: true));
        }

        public void SwitchToFinalWireAmbience()
        {
            finalWireMode = true;
            StartCoroutine(FadeSource(electricHissSource, 0f, 0.6f, stopAfter: true));
            StartCoroutine(FadeSource(hissSource, 0f, 0.6f));
            hissPaused = true;
            currentTickInterval = 0.35f;
            tickSource.pitch = 1.55f;
        }

        IEnumerator FadeSource(AudioSource src, float target, float duration, bool stopAfter = false)
        {
            if (src == null) yield break;
            float start = src.volume;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                src.volume = Mathf.Lerp(start, target, t / duration);
                yield return null;
            }
            src.volume = target;
            if (stopAfter && target <= 0.0001f) src.Stop();
        }

        public void PlayShock() => oneShotSource.PlayOneShot(shockClip, 0.9f);
        public void PlaySuccess() => oneShotSource.PlayOneShot(successClip, 0.9f);
        public void PlayAlmost() => oneShotSource.PlayOneShot(almostClip, 0.8f);
        public void PlayExplosion() => oneShotSource.PlayOneShot(explosionClip, 1f);
        public void PlayClick() => oneShotSource.PlayOneShot(clickClip, 0.6f);
        public void PlayConnect() => oneShotSource.PlayOneShot(connectClip, 0.7f);
        public void PlayAlert() => oneShotSource.PlayOneShot(alertClip, 0.85f);
        public void PlayRelief() => oneShotSource.PlayOneShot(reliefClip, 0.8f);

        public void ResetRunState()
        {
            finalWireMode = false;
            hissPaused = false;
        }

        // Corta TODO o áudio contínuo da bomba (tick, hiss ambiente, hiss
        // elétrico de desarme e tensão). Usado no game over e ao voltar ao menu:
        // como o AudioManager é DontDestroyOnLoad e essas fontes são em loop,
        // qualquer uma deixada tocando vazaria para a próxima cena e tocaria
        // "para sempre".
        public void SilenciarTudo()
        {
            StopAllCoroutines();
            tickRoutine = null;

            if (tickSource != null) tickSource.Stop();
            SilenciarLoop(hissSource);
            SilenciarLoop(electricHissSource);
            SilenciarLoop(tensionSource);

            hissPaused = false;
            finalWireMode = false;
        }

        static void SilenciarLoop(AudioSource src)
        {
            if (src == null) return;
            src.volume = 0f;
            src.Stop();
        }
    }
}
