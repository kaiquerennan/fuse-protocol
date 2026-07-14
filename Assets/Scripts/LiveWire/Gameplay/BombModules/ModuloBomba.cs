using System;
using UnityEngine;
using UnityEngine.UI;

namespace LiveWire
{
    public abstract class ModuloBomba : MonoBehaviour
    {
        public bool Resolvido { get; protected set; }
        public string NomeModulo { get; private set; }

        public event Action<ModuloBomba> AoResolver;
        public event Action<ModuloBomba, string> AoFalhar;
        public event Action<ModuloBomba, string> AoAtualizarStatus;
        // Eventos consumidos pelas Vistas 3D (VistaModulo3D). Disparam mesmo
        // quando ContainerRaiz e null, permitindo o modulo rodar sem UI Canvas.
        public event Action AoIniciar;
        public event Action<string> AoMudarInstrucao;

        public GerenciadorDeBomba Gerenciador { get; private set; }
        protected DificuldadeBomba Dificuldade => Gerenciador != null ? Gerenciador.DificuldadeAtual : DificuldadeBomba.Facil;
        protected RectTransform ContainerRaiz { get; private set; }
        protected RectTransform ConteudoRaiz { get; private set; }
        protected Text TituloTexto { get; private set; }
        protected Text InstrucaoTexto { get; private set; }
        protected Text StatusTexto { get; private set; }
        protected Image CartaoImagem { get; private set; }

        bool uiConstruida;

        public void Configurar(GerenciadorDeBomba gerenciador, RectTransform container, string nomeModulo)
        {
            Gerenciador = gerenciador;
            ContainerRaiz = container;
            NomeModulo = nomeModulo;

            ConstruirEstruturaBase();
            Gerenciador?.RegistrarModulo(this);
        }

        // Configuracao headless: usado pelo modo 3D (sem RectTransform). A Vista
        // se vincula via VistaModulo3D.Vincular e consome os eventos.
        public void ConfigurarHeadless(GerenciadorDeBomba gerenciador, string nomeModulo)
        {
            Gerenciador = gerenciador;
            ContainerRaiz = null;
            NomeModulo = nomeModulo;
            Gerenciador?.RegistrarModulo(this);
        }

        void ConstruirEstruturaBase()
        {
            // Em modo headless (sem container) nao construimos UI Canvas; as
            // Vistas 3D sao responsaveis pela apresentacao.
            if (uiConstruida || ContainerRaiz == null) return;

            CartaoImagem = ContainerRaiz.GetComponent<Image>();
            if (CartaoImagem == null)
                CartaoImagem = ContainerRaiz.gameObject.AddComponent<Image>();

            CartaoImagem.sprite = SceneBuildHelpers.GetWhiteSprite();
            CartaoImagem.color = new Color(0.05f, 0.09f, 0.09f, 0.97f);

            BombUiFactory.CreateImage(
                ContainerRaiz,
                "GlowLineTop",
                new Color(0.28f, 0.95f, 0.72f, 0.25f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, -12f),
                new Vector2(-24f, 4f),
                new Vector2(0.5f, 1f));

            TituloTexto = BombUiFactory.CreateText(
                ContainerRaiz,
                "Titulo",
                NomeModulo,
                28,
                new Color(0.9f, 1f, 0.96f),
                TextAnchor.MiddleLeft,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, -34f),
                new Vector2(-48f, 36f),
                new Vector2(0.5f, 1f));
            TituloTexto.fontStyle = FontStyle.Bold;

            InstrucaoTexto = BombUiFactory.CreateText(
                ContainerRaiz,
                "Instrucao",
                string.Empty,
                18,
                new Color(0.64f, 0.9f, 0.8f),
                TextAnchor.UpperLeft,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, -82f),
                new Vector2(-48f, 56f),
                new Vector2(0.5f, 1f));

            ConteudoRaiz = BombUiFactory.CreateRect(
                ContainerRaiz,
                "Conteudo",
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, -8f),
                new Vector2(-32f, -180f));

            StatusTexto = BombUiFactory.CreateText(
                ContainerRaiz,
                "Status",
                string.Empty,
                18,
                new Color(1f, 0.88f, 0.4f),
                TextAnchor.MiddleLeft,
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 22f),
                new Vector2(-48f, 52f),
                new Vector2(0.5f, 0f));

            ConstruirConteudo(ConteudoRaiz);
            uiConstruida = true;
        }

        protected abstract void ConstruirConteudo(RectTransform contentRoot);

        public abstract void Inicializar();
        public abstract void Interagir();
        public abstract bool Validar();
        public abstract void Resetar();

        // Ganchos do ciclo de vida do painel da bomba. O gerenciador chama
        // IniciarSessao quando o painel e aberto e EncerrarSessao quando ele e
        // fechado (cancelado, explodido ou desarmado). Modulos com audio/efeitos
        // continuos sobrescrevem para liga-los/desliga-los junto com o painel,
        // evitando que vazem para o resto do jogo. Padrao: nao faz nada.
        public virtual void IniciarSessao() { }
        public virtual void EncerrarSessao() { }

        protected void PrepararModulo(string instrucao, string statusInicial)
        {
            Resolvido = false;
            // Sinaliza re-init pra Vista 3D antes de mexer em estado.
            AoIniciar?.Invoke();
            DefinirInstrucao(instrucao);
            DefinirStatus(statusInicial, new Color(1f, 0.88f, 0.4f));
            DefinirCorDoCartao(new Color(0.05f, 0.09f, 0.09f, 0.97f));
        }

        protected void DefinirInstrucao(string texto)
        {
            if (InstrucaoTexto != null) InstrucaoTexto.text = texto;
            AoMudarInstrucao?.Invoke(texto);
        }

        protected void DefinirStatus(string texto, Color? color = null)
        {
            if (StatusTexto != null)
            {
                StatusTexto.text = texto;
                StatusTexto.color = color ?? new Color(1f, 0.88f, 0.4f);
            }
            AoAtualizarStatus?.Invoke(this, texto);
        }

        protected void DefinirCorDoCartao(Color color)
        {
            if (CartaoImagem != null) CartaoImagem.color = color;
        }

        protected Text CriarTexto(
            Transform parent,
            string name,
            string text,
            int fontSize,
            Color color,
            TextAnchor alignment,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Vector2? pivot = null)
        {
            return BombUiFactory.CreateText(parent, name, text, fontSize, color, alignment, anchorMin, anchorMax, anchoredPosition, sizeDelta, pivot);
        }

        protected Image CriarPainel(
            Transform parent,
            string name,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Vector2? pivot = null)
        {
            return BombUiFactory.CreatePanel(parent, name, color, anchorMin, anchorMax, anchoredPosition, sizeDelta, pivot);
        }

        protected RectTransform CriarRect(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Vector2? pivot = null)
        {
            return BombUiFactory.CreateRect(parent, name, anchorMin, anchorMax, anchoredPosition, sizeDelta, pivot);
        }

        protected Button TransformarEmBotao(Image image, Color normal, Color highlighted, Color pressed)
        {
            return BombUiFactory.AddButton(image, normal, highlighted, pressed);
        }

        protected void EmitirResolvido(string statusFinal)
        {
            if (Resolvido) return;

            Resolvido = true;
            DefinirStatus(statusFinal, new Color(0.35f, 1f, 0.58f));
            DefinirCorDoCartao(new Color(0.08f, 0.15f, 0.11f, 0.98f));
            AoResolver?.Invoke(this);
        }

        protected void EmitirErro(string motivo)
        {
            DefinirStatus(motivo, new Color(1f, 0.45f, 0.42f));
            AoFalhar?.Invoke(this, motivo);
        }
    }
}
