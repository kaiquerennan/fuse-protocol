using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LiveWire
{
    public class TelaClassificacaoIndicativa : MonoBehaviour
    {
        const float DuracaoFadeIn = 0.6f;
        const float DuracaoExibicao = 4f;
        const float DuracaoFadeOut = 0.8f;

        static bool jaExibida;

        Canvas canvas;
        CanvasGroup grupo;

        public static void ExibirSeNecessario()
        {
            if (jaExibida) return;
            jaExibida = true;
            GameObject go = new GameObject("TelaClassificacaoIndicativa");
            go.AddComponent<TelaClassificacaoIndicativa>();
        }

        void Awake()
        {
            canvas = SceneBuildHelpers.CreateCanvas("ClassificacaoCanvas", 100);
            canvas.transform.SetParent(transform, false);
            grupo = canvas.gameObject.AddComponent<CanvasGroup>();
            grupo.alpha = 0f;
            grupo.blocksRaycasts = true;

            GameObject fundoGO = new GameObject("Fundo", typeof(RectTransform), typeof(Image));
            RectTransform fundoRect = (RectTransform)fundoGO.transform;
            fundoRect.SetParent(canvas.transform, false);
            fundoRect.anchorMin = Vector2.zero;
            fundoRect.anchorMax = Vector2.one;
            fundoRect.offsetMin = Vector2.zero;
            fundoRect.offsetMax = Vector2.zero;
            Image fundo = fundoGO.GetComponent<Image>();
            fundo.sprite = SceneBuildHelpers.GetWhiteSprite();
            fundo.color = Color.black;

            CriarSelo();

            SceneBuildHelpers.CreateText(canvas.transform, "AvisoClassificacao",
                "NÃO RECOMENDADO PARA MENORES DE 16 ANOS", 40,
                Color.white, TextAnchor.MiddleCenter,
                new Vector2(0f, -220f), new Vector2(1600f, 80f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            StartCoroutine(Exibir());
        }

        void CriarSelo()
        {
            GameObject seloGO = new GameObject("Selo16", typeof(RectTransform), typeof(Image));
            RectTransform seloRect = (RectTransform)seloGO.transform;
            seloRect.SetParent(canvas.transform, false);
            seloRect.anchorMin = seloRect.anchorMax = new Vector2(0.5f, 0.5f);
            seloRect.pivot = new Vector2(0.5f, 0.5f);
            seloRect.anchoredPosition = new Vector2(0f, 60f);
            seloRect.sizeDelta = new Vector2(300f, 300f);
            Image selo = seloGO.GetComponent<Image>();

            Sprite sprite = Resources.Load<Sprite>("ClassificacaoIndicativa16");
            if (sprite != null)
            {
                selo.sprite = sprite;
                selo.preserveAspect = true;
            }
            else
            {
                selo.sprite = SceneBuildHelpers.GetWhiteSprite();
                selo.color = new Color(0.93f, 0.05f, 0.05f);
                SceneBuildHelpers.CreateText(seloRect, "Numero", "16", 160,
                    Color.white, TextAnchor.MiddleCenter,
                    Vector2.zero, new Vector2(300f, 300f),
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            }
        }

        IEnumerator Exibir()
        {
            yield return Fade(0f, 1f, DuracaoFadeIn);
            yield return new WaitForSecondsRealtime(DuracaoExibicao);
            yield return Fade(1f, 0f, DuracaoFadeOut);
            Destroy(gameObject);
        }

        IEnumerator Fade(float de, float para, float duracao)
        {
            float tempo = 0f;
            while (tempo < duracao)
            {
                tempo += Time.unscaledDeltaTime;
                grupo.alpha = Mathf.Lerp(de, para, tempo / duracao);
                yield return null;
            }
            grupo.alpha = para;
        }
    }
}
