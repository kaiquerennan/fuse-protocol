using UnityEngine;
using UnityEngine.UI;

namespace LiveWire
{
    public class MainMenuBootstrap : MonoBehaviour
    {
        void Awake()
        {
            EnsureGlobals();
            BuildScene();
            TelaClassificacaoIndicativa.ExibirSeNecessario();
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
            Camera.main?.gameObject.SetActive(false);
            GameObject camGO = new GameObject("MenuCamera", typeof(Camera), typeof(AudioListener));
            Camera cam = camGO.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.02f, 0.03f);
            cam.orthographic = false;
            camGO.tag = "MainCamera";
            camGO.transform.position = new Vector3(0f, 1.5f, -5f);

            Canvas canvas = SceneBuildHelpers.CreateCanvas("MenuCanvas", 10);
            SceneBuildHelpers.EnsureEventSystem();

            SceneBuildHelpers.CreateText(canvas.transform, "Title", "FUSE PROTOCOL", 140,
                new Color(1f, 0.15f, 0.15f), TextAnchor.MiddleCenter,
                new Vector2(0f, 200f), new Vector2(1400f, 200f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            SceneBuildHelpers.CreateText(canvas.transform, "Subtitle", "DESARME A BOMBA ANTES QUE O TEMPO ACABE", 26,
                new Color(0.7f, 0.9f, 0.9f), TextAnchor.MiddleCenter,
                new Vector2(0f, 100f), new Vector2(1400f, 60f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            Button play = SceneBuildHelpers.CreateButton(canvas.transform, "JOGAR", new Vector2(0f, -60f), new Vector2(380f, 80f));
            Button quit = SceneBuildHelpers.CreateButton(canvas.transform, "SAIR", new Vector2(0f, -170f), new Vector2(380f, 80f));

            SceneBuildHelpers.CreateVolumeControl(canvas.transform, new Vector2(0f, -290f));

            SceneBuildHelpers.CreateText(canvas.transform, "Hint", "WASD + MOUSE | E INTERAGE | CLIQUE PARA LIGAR FIOS", 20,
                new Color(0.4f, 0.5f, 0.6f), TextAnchor.MiddleCenter,
                new Vector2(0f, -370f), new Vector2(1600f, 40f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            MainMenuController controller = gameObject.AddComponent<MainMenuController>();
            controller.playButton = play;
            controller.quitButton = quit;
        }
    }
}
