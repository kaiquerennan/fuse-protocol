using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LiveWire
{
    public class GerenciadorDeObjetivo : MonoBehaviour
    {
        [Header("UI Opcional")]
        [SerializeField] Text textoPrincipal;
        [SerializeField] Text textoSecundario;
        [SerializeField] float duracaoMensagemInicial = 3.5f;
        [SerializeField] bool ocultarMensagemInicial = false;
        [SerializeField] bool usarHudComoFallback = true;

        [Header("Mensagens")]
        [SerializeField] string formatoResumoExplicito = "LOCAL: {0}";
        [SerializeField] string resumoOculto = "OBJETIVO: ENCONTRE A BOMBA";
        [SerializeField] string mensagemConclusao = "Bomba desarmada. Preparando a proxima fase.";
        [SerializeField] string mensagemCampanhaConcluida = "Todas as fases da escola foram concluidas.";

        Coroutine rotinaMensagem;

        public void Configurar(Text principal, Text secundario, bool usarFallbackHud = true)
        {
            textoPrincipal = principal;
            textoSecundario = secundario;
            usarHudComoFallback = usarFallbackHud;
        }

        public void ExibirInicioDeFase(DadosDaFaseDaEscola fase, int numeroDaFase)
        {
            if (fase == null) return;

            string principal = fase.ConstruirMensagemInicial();
            string secundario = $"FASE {numeroDaFase:00}  |  {FaseDaEscolaTexto.ObterNomeEmCaixaAlta(fase.LocalPrincipal)}";
            MostrarMensagem(principal, secundario, duracaoMensagemInicial, ocultarMensagemInicial, new Color(0.78f, 0.96f, 0.86f));
        }

        public void AtualizarObjetivoAtivo(DadosDaFaseDaEscola fase)
        {
            if (fase == null) return;

            if (textoSecundario != null)
            {
                textoSecundario.text = fase.ModoDoObjetivo == ModoDeObjetivoDaFase.Explicito
                    ? string.Format(formatoResumoExplicito, FaseDaEscolaTexto.ObterNomeEmCaixaAlta(fase.LocalPrincipal))
                    : resumoOculto;
            }
        }

        public void MarcarObjetivoConcluido()
        {
            MostrarMensagem(mensagemConclusao, string.Empty, 2f, true, new Color(0.42f, 1f, 0.58f));
        }

        public void MarcarCampanhaConcluida()
        {
            MostrarMensagem(mensagemCampanhaConcluida, string.Empty, 3f, false, new Color(0.42f, 1f, 0.58f));
        }

        public void Limpar()
        {
            if (rotinaMensagem != null)
            {
                StopCoroutine(rotinaMensagem);
                rotinaMensagem = null;
            }

            if (textoPrincipal != null) textoPrincipal.text = string.Empty;
            if (textoSecundario != null) textoSecundario.text = string.Empty;
        }

        void MostrarMensagem(string principal, string secundario, float duracao, bool ocultarAoFinal, Color cor)
        {
            if (textoPrincipal == null && usarHudComoFallback)
            {
                HudController hud = FindAnyObjectByType<HudController>();
                if (hud != null)
                    hud.ShowTemporaryMessage(principal, duracao, cor);
            }

            if (rotinaMensagem != null)
                StopCoroutine(rotinaMensagem);

            if (textoPrincipal != null)
            {
                textoPrincipal.text = principal;
                textoPrincipal.color = cor;
            }

            if (textoSecundario != null)
                textoSecundario.text = secundario;

            rotinaMensagem = StartCoroutine(RotinaMensagem(duracao, ocultarAoFinal));
        }

        IEnumerator RotinaMensagem(float duracao, bool ocultarAoFinal)
        {
            if (duracao > 0f)
                yield return new WaitForSecondsRealtime(duracao);

            if (ocultarAoFinal && textoPrincipal != null)
                textoPrincipal.text = string.Empty;

            rotinaMensagem = null;
        }
    }
}
