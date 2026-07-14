using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LiveWire
{
    public class MenuDePausa : MonoBehaviour
    {
        Canvas canvas;
        bool pausado;
        int ultimoFrameBombaAberta = -10;

        void Awake()
        {
            ConstruirUI();
            canvas.gameObject.SetActive(false);
        }

        void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;

            if (pausado) Retomar();
            else if (!BombaUiAberta()) Pausar();
        }

        void LateUpdate()
        {
            if (BombaUiAbertaAgora()) ultimoFrameBombaAberta = Time.frameCount;
        }

        // O painel da bomba e o minigame de fios também fecham com Esc; o mesmo
        // Esc que fecha um deles não pode abrir a pausa, mas a ordem de Update
        // entre os scripts é indefinida — por isso também vale "aberto no frame
        // anterior" (registrado no LateUpdate).
        bool BombaUiAberta()
        {
            if (BombaUiAbertaAgora()) return true;
            return Time.frameCount - ultimoFrameBombaAberta <= 1;
        }

        static bool BombaUiAbertaAgora()
        {
            if (GerenciadorDeBomba.Instance != null && GerenciadorDeBomba.Instance.IsOpen) return true;
            if (WireMinigame.Instance != null && WireMinigame.Instance.IsOpen) return true;
            return false;
        }

        void Pausar()
        {
            pausado = true;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.SetInputLocked(true);
                PlayerController.Instance.SetCursorUnlocked(true);
            }
            canvas.gameObject.SetActive(true);
        }

        void Retomar()
        {
            pausado = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            canvas.gameObject.SetActive(false);
            PlayerController.Instance?.SetInputLocked(false);
        }

        void SairParaMenu()
        {
            Retomar();
            GameManager.Instance?.GoToMenu();
        }

        void OnDestroy()
        {
            if (pausado)
            {
                Time.timeScale = 1f;
                AudioListener.pause = false;
            }
        }

        void ConstruirUI()
        {
            canvas = SceneBuildHelpers.CreateCanvas("PausaCanvas", 90);
            canvas.transform.SetParent(transform, false);
            SceneBuildHelpers.EnsureEventSystem();

            GameObject fundoGO = new GameObject("Fundo", typeof(RectTransform), typeof(Image));
            RectTransform fundoRect = (RectTransform)fundoGO.transform;
            fundoRect.SetParent(canvas.transform, false);
            fundoRect.anchorMin = Vector2.zero;
            fundoRect.anchorMax = Vector2.one;
            fundoRect.offsetMin = Vector2.zero;
            fundoRect.offsetMax = Vector2.zero;
            Image fundo = fundoGO.GetComponent<Image>();
            fundo.sprite = SceneBuildHelpers.GetWhiteSprite();
            fundo.color = new Color(0f, 0f, 0f, 0.85f);

            SceneBuildHelpers.CreateText(canvas.transform, "Titulo", "PAUSADO", 90,
                new Color(1f, 0.15f, 0.15f), TextAnchor.MiddleCenter,
                new Vector2(0f, 200f), new Vector2(1200f, 140f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            SceneBuildHelpers.CreateVolumeControl(canvas.transform, new Vector2(0f, 40f));

            Button continuar = SceneBuildHelpers.CreateButton(canvas.transform, "CONTINUAR",
                new Vector2(0f, -80f), new Vector2(380f, 80f));
            continuar.onClick.AddListener(Retomar);

            Button sair = SceneBuildHelpers.CreateButton(canvas.transform, "SAIR PARA O MENU",
                new Vector2(0f, -190f), new Vector2(380f, 80f));
            sair.onClick.AddListener(SairParaMenu);

            SceneBuildHelpers.CreateText(canvas.transform, "Hint", "ESC PARA CONTINUAR", 20,
                new Color(0.4f, 0.5f, 0.6f), TextAnchor.MiddleCenter,
                new Vector2(0f, -290f), new Vector2(800f, 40f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        }
    }
}
