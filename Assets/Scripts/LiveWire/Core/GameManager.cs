using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LiveWire
{
    public class GameManager : MonoBehaviour
    {
        public const string MainMenuScene = "MainMenu";
        public const string GameScene = "GameScene";
        public const string GameOverScene = "GameOver";

        public const float StartingTime = 60f;
        public const float TimeStepPerPhase = 5f;
        public const float MinimumTime = 20f;
        public const float WrongWirePenalty = 5f;

        public static GameManager Instance { get; private set; }

        public int CurrentPhase { get; private set; } = 1;
        public float LastRemainingTime { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public float GetPhaseTime(int phase)
        {
            float t = StartingTime - (phase - 1) * TimeStepPerPhase;
            return Mathf.Max(MinimumTime, t);
        }

        public int GetWireCount(int phase)
        {
            if (phase <= 2) return 2;
            if (phase <= 4) return 3;
            if (phase <= 6) return 4;
            return 5;
        }

        public DificuldadeBomba GetBombDifficulty(int phase)
        {
            return phase >= 4 ? DificuldadeBomba.Dificil : DificuldadeBomba.Facil;
        }

        public bool ShuffleConnectors(int phase) => phase >= 5;
        public bool MovingConnectors(int phase) => phase >= 7;
        public bool SimilarColors(int phase) => phase >= 3;

        public void StartNewRun()
        {
            CurrentPhase = 1;
            SceneManager.LoadScene(GameScene);
        }

        public void AdvancePhase(float remainingTime)
        {
            LastRemainingTime = remainingTime;
            CurrentPhase++;
            SceneManager.LoadScene(GameScene);
        }

        public void SetCurrentPhase(int phase)
        {
            CurrentPhase = Mathf.Max(1, phase);
        }

        public void SetLastRemainingTime(float remainingTime)
        {
            LastRemainingTime = Mathf.Max(0f, remainingTime);
        }

        public void RetryPhase()
        {
            SceneManager.LoadScene(GameScene);
        }

        public void GoToMenu()
        {
            AudioManager.Instance?.SilenciarTudo();
            CurrentPhase = 1;
            SceneManager.LoadScene(MainMenuScene);
        }

        public void TriggerGameOver(Vector3 bombPosition, Camera playerCamera)
        {
            StartCoroutine(GameOverSequence(bombPosition, playerCamera));
        }

        IEnumerator GameOverSequence(Vector3 bombPosition, Camera playerCamera)
        {
            AudioManager.Instance?.SilenciarTudo();
            AudioManager.Instance?.PlayExplosion();

            UIFlasher flasher = FindAnyObjectByType<UIFlasher>();
            if (flasher != null) flasher.FlashWhite(1.6f);

            SpawnExplosionEffect(bombPosition);

            PlayerController pc = FindAnyObjectByType<PlayerController>();
            if (pc != null) pc.enabled = false;

            if (playerCamera != null)
            {
                Transform camT = playerCamera.transform;
                Vector3 startPos = camT.position;
                Quaternion startRot = camT.rotation;
                Quaternion endRot = Quaternion.Euler(80f, startRot.eulerAngles.y, Random.Range(-10f, 10f));
                Vector3 endPos = new Vector3(startPos.x, 0.3f, startPos.z);
                float t = 0f;
                float duration = 2.2f;
                while (t < duration)
                {
                    t += Time.unscaledDeltaTime;
                    float n = Mathf.Clamp01(t / duration);
                    float eased = 1f - Mathf.Pow(1f - n, 3f);
                    camT.position = Vector3.Lerp(startPos, endPos, eased);
                    camT.rotation = Quaternion.Slerp(startRot, endRot, eased);
                    yield return null;
                }
            }

            yield return new WaitForSecondsRealtime(0.8f);
            SceneManager.LoadScene(GameOverScene);
        }

        void SpawnExplosionEffect(Vector3 position)
        {
            GameObject go = new GameObject("Explosion");
            go.transform.position = position;

            Light light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.55f, 0.15f);
            light.intensity = 120f;
            light.range = 40f;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 1.2f;
            main.startLifetime = 1.4f;
            main.startSpeed = 12f;
            main.startSize = 0.8f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.6f, 0.1f), new Color(1f, 0.2f, 0.05f));
            main.maxParticles = 400;
            var emission = ps.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 300) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            renderer.material = new Material(shader);
            renderer.material.color = new Color(1f, 0.55f, 0.1f);

            StartCoroutine(FadeLight(light));
            Destroy(go, 3.5f);
        }

        IEnumerator FadeLight(Light light)
        {
            float t = 0f;
            float start = light.intensity;
            while (t < 1.2f && light != null)
            {
                t += Time.unscaledDeltaTime;
                light.intensity = Mathf.Lerp(start, 0f, t / 1.2f);
                yield return null;
            }
        }
    }
}
