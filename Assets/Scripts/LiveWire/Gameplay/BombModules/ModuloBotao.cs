using UnityEngine;
using UnityEngine.UI;

namespace LiveWire
{
    public class ModuloBotao : ModuloBomba
    {
        Text regraTexto;
        Text feedbackTexto;
        Text rotuloBotaoTexto;
        Image botaoImagem;
        Image progressoImagem;
        Button botaoPrincipal;
        PointerPressRelay relayPressao;

        CorBomba corAtual = CorBomba.Vermelho;
        float ultimaDuracaoPressionada;
        float tempoSegurarNecessario = 3f;
        float limiteCliqueRapido = 0.3f;

        protected override void ConstruirConteudo(RectTransform contentRoot)
        {
            regraTexto = CriarTexto(
                contentRoot,
                "RegraBotao",
                string.Empty,
                18,
                new Color(0.74f, 0.92f, 0.84f),
                TextAnchor.MiddleCenter,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, -10f),
                new Vector2(-4f, 32f),
                new Vector2(0.5f, 1f));

            botaoImagem = CriarPainel(
                contentRoot,
                "BotaoPrincipal",
                BombDefinitions.ObterCorUi(CorBomba.Vermelho),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -10f),
                new Vector2(268f, 268f));
            botaoPrincipal = TransformarEmBotao(
                botaoImagem,
                BombDefinitions.ObterCorUi(CorBomba.Vermelho),
                BombDefinitions.ObterCorClara(CorBomba.Vermelho),
                Color.Lerp(BombDefinitions.ObterCorUi(CorBomba.Vermelho), Color.black, 0.15f));

            relayPressao = botaoImagem.gameObject.AddComponent<PointerPressRelay>();
            relayPressao.Pressed += HandlePressionado;
            relayPressao.Released += HandleSolto;

            rotuloBotaoTexto = CriarTexto(
                botaoImagem.transform,
                "RotuloBotao",
                "BOTAO",
                28,
                BombDefinitions.ObterCorTexto(CorBomba.Vermelho),
                TextAnchor.MiddleCenter,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            rotuloBotaoTexto.fontStyle = FontStyle.Bold;

            Image progressoBase = CriarPainel(
                contentRoot,
                "ProgressoBase",
                new Color(0.08f, 0.12f, 0.12f, 0.95f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 72f),
                new Vector2(360f, 18f),
                new Vector2(0.5f, 0f));

            progressoImagem = CriarPainel(
                progressoBase.transform,
                "ProgressoFill",
                new Color(0.34f, 0.95f, 0.52f),
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                Vector2.zero,
                Vector2.zero);
            progressoImagem.type = Image.Type.Filled;
            progressoImagem.fillMethod = Image.FillMethod.Horizontal;
            progressoImagem.fillOrigin = (int)Image.OriginHorizontal.Left;
            progressoImagem.fillAmount = 0f;

            feedbackTexto = CriarTexto(
                contentRoot,
                "FeedbackBotao",
                string.Empty,
                18,
                new Color(0.92f, 0.96f, 0.98f),
                TextAnchor.MiddleCenter,
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 28f),
                new Vector2(-4f, 36f),
                new Vector2(0.5f, 0f));
        }

        void Update()
        {
            if (Resolvido || relayPressao == null || progressoImagem == null) return;

            if (relayPressao.IsPressed && corAtual == CorBomba.Vermelho)
            {
                float progresso = Mathf.Clamp01(relayPressao.HeldTime / tempoSegurarNecessario);
                progressoImagem.fillAmount = progresso;
                feedbackTexto.text = progresso >= 1f ? "SOLTE AGORA" : "MANTENHA PRESSIONADO";
            }
        }

        public override void Inicializar()
        {
            PrepararModulo("VERMELHO = SEGURE 3s  |  AZUL = CLIQUE RAPIDO.", "AGUARDANDO ACAO");

            corAtual = BombDefinitions.CoresDoBotao[Random.Range(0, BombDefinitions.CoresDoBotao.Length)];
            tempoSegurarNecessario = 3f;
            limiteCliqueRapido = Dificuldade == DificuldadeBomba.Facil ? 0.3f : 0.2f;
            ultimaDuracaoPressionada = 0f;

            if (relayPressao != null) relayPressao.CancelPress();
            if (botaoPrincipal != null) botaoPrincipal.interactable = true;

            Color corBase = BombDefinitions.ObterCorUi(corAtual);
            botaoImagem.color = corBase;
            rotuloBotaoTexto.color = BombDefinitions.ObterCorTexto(corAtual);
            rotuloBotaoTexto.text = BombDefinitions.ObterNome(corAtual);
            regraTexto.text = corAtual == CorBomba.Vermelho
                ? "BOTAO VERMELHO. SEGURE POR 3 SEGUNDOS."
                : $"BOTAO AZUL. SOLTE EM ATE {limiteCliqueRapido:0.0#}s.";
            progressoImagem.fillAmount = 0f;
            feedbackTexto.text = corAtual == CorBomba.Vermelho ? "SEGURE" : "TOQUE E SOLTE";

            ColorBlock colors = botaoPrincipal.colors;
            colors.normalColor = corBase;
            colors.highlightedColor = BombDefinitions.ObterCorClara(corAtual);
            colors.selectedColor = BombDefinitions.ObterCorClara(corAtual);
            colors.pressedColor = Color.Lerp(corBase, Color.black, 0.18f);
            colors.disabledColor = Color.Lerp(corBase, Color.black, 0.38f);
            botaoPrincipal.colors = colors;
        }

        public override void Interagir()
        {
            if (!Resolvido)
                DefinirStatus("USE O MOUSE PARA ACIONAR O BOTAO");
        }

        public override bool Validar()
        {
            if (Resolvido) return true;

            if (botaoPrincipal != null) botaoPrincipal.interactable = false;

            bool sucesso = corAtual switch
            {
                CorBomba.Vermelho => ultimaDuracaoPressionada >= tempoSegurarNecessario,
                CorBomba.Azul => ultimaDuracaoPressionada <= limiteCliqueRapido,
                _ => false,
            };

            if (sucesso)
            {
                progressoImagem.fillAmount = 1f;
                feedbackTexto.text = "ACAO VALIDADA";
                EmitirResolvido("BOTAO EXECUTADO CORRETAMENTE");
                return true;
            }

            feedbackTexto.text = corAtual == CorBomba.Vermelho
                ? "SEGURAR INSUFICIENTE"
                : "CLIQUE LENTO DEMAIS";
            EmitirErro("BOTAO EXECUTADO INCORRETAMENTE");
            return false;
        }

        public override void Resetar()
        {
            if (Resolvido) return;
            Inicializar();
        }

        void HandlePressionado()
        {
            if (Resolvido || botaoPrincipal == null || !botaoPrincipal.interactable) return;
            AudioManager.Instance?.PlayClick();
            feedbackTexto.text = corAtual == CorBomba.Vermelho ? "MANTENHA PRESSIONADO" : "SOLTE RAPIDO";
        }

        void HandleSolto(float duracao)
        {
            if (Resolvido || botaoPrincipal == null || !botaoPrincipal.interactable) return;
            ultimaDuracaoPressionada = duracao;
            AudioManager.Instance?.PlayConnect();
            Validar();
        }

        void OnDestroy()
        {
            if (relayPressao == null) return;
            relayPressao.Pressed -= HandlePressionado;
            relayPressao.Released -= HandleSolto;
        }
    }
}
