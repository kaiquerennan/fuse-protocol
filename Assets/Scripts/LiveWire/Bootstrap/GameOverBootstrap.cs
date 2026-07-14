using UnityEngine;
using UnityEngine.UI;

namespace LiveWire
{
    public class GameOverBootstrap : MonoBehaviour
    {
        void Awake()
        {
            EnsureGlobals();
            BuildScene();
        }

        void EnsureGlobals()
        {
            if (GameManager.Instance == null)
            {
                GameObject gm = new GameObject("GameManager");
                gm.AddComponent<GameManager>();
            }
            if (AudioManager.Instance == null)
            {
                GameObject am = new GameObject("AudioManager");
                am.AddComponent<AudioManager>();
            }
        }

        void BuildScene()
        {
            GameObject camGO = new GameObject("GameOverCamera", typeof(Camera), typeof(AudioListener));
            Camera cam = camGO.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            camGO.tag = "MainCamera";

            Canvas canvas = SceneBuildHelpers.CreateCanvas("GameOverCanvas", 10);
            SceneBuildHelpers.EnsureEventSystem();

            SceneBuildHelpers.CreateText(canvas.transform, "Died", "VOCÊ MORREU", 160,
                new Color(1f, 0.1f, 0.1f), TextAnchor.MiddleCenter,
                new Vector2(0f, 180f), new Vector2(1600f, 220f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            int phase = GameManager.Instance != null ? GameManager.Instance.CurrentPhase : 1;
            SceneBuildHelpers.CreateText(canvas.transform, "Phase", $"FASE {phase:00}", 48,
                new Color(0.7f, 0.2f, 0.2f), TextAnchor.MiddleCenter,
                new Vector2(0f, 60f), new Vector2(800f, 80f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            Button retry = SceneBuildHelpers.CreateButton(canvas.transform, "TENTAR NOVAMENTE", new Vector2(0f, -80f), new Vector2(560f, 84f));
            Button menu = SceneBuildHelpers.CreateButton(canvas.transform, "MENU", new Vector2(0f, -190f), new Vector2(560f, 84f));

            GameOverController controller = gameObject.AddComponent<GameOverController>();
            controller.retryButton = retry;
            controller.menuButton = menu;
        }
    }
}
