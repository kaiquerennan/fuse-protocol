using UnityEngine;
using UnityEngine.UI;

namespace LiveWire
{
    public class GameOverController : MonoBehaviour
    {
        public Button retryButton;
        public Button menuButton;

        void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (retryButton != null) retryButton.onClick.AddListener(OnRetry);
            if (menuButton != null) menuButton.onClick.AddListener(OnMenu);
        }

        void OnRetry()
        {
            if (GameManager.Instance == null)
            {
                GameObject go = new GameObject("GameManager");
                go.AddComponent<GameManager>();
            }
            GameManager.Instance.RetryPhase();
        }

        void OnMenu()
        {
            if (GameManager.Instance == null)
            {
                GameObject go = new GameObject("GameManager");
                go.AddComponent<GameManager>();
            }
            GameManager.Instance.GoToMenu();
        }
    }
}
