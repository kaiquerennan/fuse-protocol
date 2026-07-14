using UnityEngine;
using UnityEngine.UI;

namespace LiveWire
{
    public class MainMenuController : MonoBehaviour
    {
        public Button playButton;
        public Button quitButton;

        void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (playButton != null) playButton.onClick.AddListener(OnPlay);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuit);
        }

        void OnPlay()
        {
            if (GameManager.Instance == null)
            {
                GameObject go = new GameObject("GameManager");
                go.AddComponent<GameManager>();
            }
            GameManager.Instance.StartNewRun();
        }

        void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
