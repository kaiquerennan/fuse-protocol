using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LiveWire
{
    public class ModuloMemoria : ModuloBomba
    {
        readonly Dictionary<CorBomba, Image> pads = new();
        readonly Dictionary<CorBomba, Button> botoes = new();
        readonly List<CorBomba> sequencia = new();

        Text informacaoTexto;
        Text progressoTexto;
        Coroutine rotinaSequencia;
        int indiceEntrada;
        CorBomba ultimaCorSelecionada;
        bool temEntradaPendente;
        bool reproduzindo;

        // Eventos consumidos por VistaMemoria3D. UI Canvas continua funcionando
        // pelos campos informacaoTexto/progressoTexto quando ContainerRaiz != null.
        public event Action<int> AoIniciarSequencia;
        public event Action<CorBomba> AoPiscarPad;
        public event Action AoFimPlayback;
        public event Action<int, int> AoProgressoEntrada;

        public bool PodeReceberEntrada => !reproduzindo && !Resolvido;
        public IReadOnlyList<CorBomba> Sequencia => sequencia;

        protected override void ConstruirConteudo(RectTransform contentRoot)
        {
            informacaoTexto = CriarTexto(
                contentRoot,
                "InformacaoMemoria",
                string.Empty,
                18,
                new Color(0.74f, 0.92f, 0.84f),
                TextAnchor.MiddleCenter,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, -10f),
                new Vector2(-4f, 32f),
                new Vector2(0.5f, 1f));

            RectTransform gradeRaiz = CriarRect(
                contentRoot,
                "GradeMemoria",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -10f),
                new Vector2(380f, 320f));

            CriarPad(gradeRaiz, CorBomba.Vermelho, new Vector2(-96f, 84f));
            CriarPad(gradeRaiz, CorBomba.Azul, new Vector2(96f, 84f));
            CriarPad(gradeRaiz, CorBomba.Amarelo, new Vector2(-96f, -84f));
            CriarPad(gradeRaiz, CorBomba.Verde, new Vector2(96f, -84f));

            progressoTexto = CriarTexto(
                contentRoot,
                "ProgressoMemoria",
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

        public override void Inicializar()
        {
            PrepararModulo("OBSERVE A SEQUENCIA E REPITA CLICANDO NAS CORES.", "GERANDO SEQUENCIA");

            StopRotinaSequencia();
            sequencia.Clear();

            int tamanho = UnityEngine.Random.Range(Dificuldade == DificuldadeBomba.Facil ? 3 : 4, 6);
            for (int i = 0; i < tamanho; i++)
            {
                CorBomba cor = BombDefinitions.CoresDaMemoria[UnityEngine.Random.Range(0, BombDefinitions.CoresDaMemoria.Length)];
                sequencia.Add(cor);
            }

            indiceEntrada = 0;
            temEntradaPendente = false;
            reproduzindo = false;
            SetInformacao($"SEQUENCIA DE {sequencia.Count} CORES");
            SetProgresso("OBSERVE");
            HabilitarPads(false);
            AoIniciarSequencia?.Invoke(sequencia.Count);

            rotinaSequencia = StartCoroutine(ReproduzirSequencia());
        }

        public override void Interagir()
        {
            if (Resolvido) return;

            DefinirStatus(reproduzindo
                ? "MEMORIZE A ORDEM DAS CORES"
                : "REPITA A SEQUENCIA CLICANDO NOS QUADROS");
        }

        public override bool Validar()
        {
            if (!temEntradaPendente || reproduzindo || Resolvido) return false;

            temEntradaPendente = false;
            CorBomba esperado = sequencia[indiceEntrada];

            if (ultimaCorSelecionada != esperado)
            {
                HabilitarPads(false);
                SetProgresso("ERRO");
                EmitirErro("SEQUENCIA INCORRETA");
                return false;
            }

            AudioManager.Instance?.PlayConnect();
            indiceEntrada++;
            SetProgresso($"{indiceEntrada}/{sequencia.Count}");
            AoProgressoEntrada?.Invoke(indiceEntrada, sequencia.Count);

            if (indiceEntrada >= sequencia.Count)
            {
                HabilitarPads(false);
                EmitirResolvido("SEQUENCIA REPETIDA CORRETAMENTE");
                return true;
            }

            return true;
        }

        public override void Resetar()
        {
            if (Resolvido) return;

            StopRotinaSequencia();
            indiceEntrada = 0;
            temEntradaPendente = false;
            reproduzindo = false;
            HabilitarPads(false);
            SetProgresso("REPETINDO");
            rotinaSequencia = StartCoroutine(ReproduzirSequencia());
        }

        // Chamado pela VistaMemoria3D quando o jogador toca um pad fisico.
        public void RegistrarEntradaVista(CorBomba cor)
        {
            if (!PodeReceberEntrada) return;

            ultimaCorSelecionada = cor;
            temEntradaPendente = true;
            AudioManager.Instance?.PlayClick();
            Validar();
        }

        void SetInformacao(string txt)
        {
            if (informacaoTexto != null) informacaoTexto.text = txt;
        }

        void SetProgresso(string txt)
        {
            if (progressoTexto != null) progressoTexto.text = txt;
        }

        void CriarPad(RectTransform parent, CorBomba cor, Vector2 anchoredPosition)
        {
            Color corBase = BombDefinitions.ObterCorUi(cor);

            Image pad = CriarPainel(
                parent,
                $"Pad_{cor}",
                corBase,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                anchoredPosition,
                new Vector2(160f, 136f));

            Button botao = TransformarEmBotao(
                pad,
                corBase,
                BombDefinitions.ObterCorClara(cor),
                Color.Lerp(corBase, Color.black, 0.12f));

            CorBomba corLocal = cor;
            botao.onClick.AddListener(() => SelecionarCor(corLocal));

            Text padTexto = CriarTexto(
                pad.transform,
                "Label",
                BombDefinitions.ObterNome(cor),
                18,
                BombDefinitions.ObterCorTexto(cor),
                TextAnchor.MiddleCenter,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            padTexto.fontStyle = FontStyle.Bold;

            pads[cor] = pad;
            botoes[cor] = botao;
        }

        void SelecionarCor(CorBomba cor)
        {
            if (reproduzindo || Resolvido || !botoes.TryGetValue(cor, out Button botao) || !botao.interactable)
                return;

            ultimaCorSelecionada = cor;
            temEntradaPendente = true;
            AudioManager.Instance?.PlayClick();
            Validar();
        }

        IEnumerator ReproduzirSequencia()
        {
            // Enquanto a sequencia toca, os pads ficam travados para evitar
            // entrada prematura e manter o modulo deterministico.
            reproduzindo = true;
            DefinirStatus("MEMORIZE A SEQUENCIA");
            SetProgresso("OBSERVE");

            float duracaoPiscar = Dificuldade == DificuldadeBomba.Facil ? 0.42f : 0.28f;
            float intervalo = Dificuldade == DificuldadeBomba.Facil ? 0.18f : 0.12f;

            yield return new WaitForSecondsRealtime(0.45f);

            for (int i = 0; i < sequencia.Count; i++)
            {
                CorBomba cor = sequencia[i];
                SetProgresso($"MEMORIZE {i + 1}/{sequencia.Count}");
                AoPiscarPad?.Invoke(cor);
                yield return PiscarPad(cor, duracaoPiscar);
                yield return new WaitForSecondsRealtime(intervalo);
            }

            reproduzindo = false;
            indiceEntrada = 0;
            SetProgresso($"0/{sequencia.Count}");
            DefinirStatus("REPITA A SEQUENCIA");
            HabilitarPads(true);
            AoFimPlayback?.Invoke();
            rotinaSequencia = null;
        }

        IEnumerator PiscarPad(CorBomba cor, float duracao)
        {
            if (!pads.TryGetValue(cor, out Image pad))
            {
                // Sem UI Canvas (modo headless 3D): respeita o tempo do flash
                // pra Vista 3D conseguir reproduzir o pulso de luz.
                yield return new WaitForSecondsRealtime(duracao);
                yield break;
            }

            Color original = BombDefinitions.ObterCorUi(cor);
            Color brilho = BombDefinitions.ObterCorClara(cor);
            Vector3 escalaOriginal = pad.rectTransform.localScale;

            pad.color = brilho;
            pad.rectTransform.localScale = escalaOriginal * 1.05f;
            yield return new WaitForSecondsRealtime(duracao);
            pad.color = original;
            pad.rectTransform.localScale = escalaOriginal;
        }

        void HabilitarPads(bool habilitar)
        {
            foreach (Button botao in botoes.Values)
            {
                if (botao != null) botao.interactable = habilitar;
            }
        }

        void StopRotinaSequencia()
        {
            if (rotinaSequencia == null) return;
            StopCoroutine(rotinaSequencia);
            rotinaSequencia = null;
        }
    }
}
