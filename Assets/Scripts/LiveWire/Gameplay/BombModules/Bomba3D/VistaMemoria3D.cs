using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LiveWire
{
    // Apresentacao 3D do modulo de memoria. Cria 4 pads coloridos em grade 2x2,
    // pulsa o pad correto durante o playback e captura toques do jogador via
    // BombaHotspot. Toda a logica continua em ModuloMemoria.
    public class VistaMemoria3D : VistaModulo3D
    {
        [Header("Layout dos pads")]
        [SerializeField] float padSize = 0.06f;
        [SerializeField] float padSpacing = 0.085f;
        [SerializeField] float padHeight = 0.025f;

        [Header("Iluminacao")]
        [SerializeField] float emissivoIdle = 0.25f;
        [SerializeField] float emissivoBrilho = 3.5f;
        [SerializeField] float duracaoFlash = 0.42f;

        readonly Dictionary<CorBomba, PadVisual> padsVisuais = new();
        ModuloMemoria moduloMemoria;
        Coroutine rotinaFlash;

        class PadVisual
        {
            public Transform Transform;
            public Renderer Renderer;
            public Material Material;
            public Color CorBase;
            public BombaHotspot Hotspot;
            public Vector3 EscalaBase;
        }

        protected override void InscreverEventosEspecificos()
        {
            moduloMemoria = Modulo as ModuloMemoria;
            if (moduloMemoria == null) return;

            ConstruirPadsSeNecessario();

            moduloMemoria.AoIniciarSequencia += HandleIniciarSequencia;
            moduloMemoria.AoPiscarPad += HandlePiscarPad;
            moduloMemoria.AoFimPlayback += HandleFimPlayback;
            moduloMemoria.AoProgressoEntrada += HandleProgresso;
        }

        protected override void DesinscreverEventosEspecificos()
        {
            if (moduloMemoria == null) return;
            moduloMemoria.AoIniciarSequencia -= HandleIniciarSequencia;
            moduloMemoria.AoPiscarPad -= HandlePiscarPad;
            moduloMemoria.AoFimPlayback -= HandleFimPlayback;
            moduloMemoria.AoProgressoEntrada -= HandleProgresso;
        }

        void ConstruirPadsSeNecessario()
        {
            if (padsVisuais.Count > 0) return;

            CorBomba[] cores = BombDefinitions.CoresDaMemoria;
            Vector2[] grid =
            {
                new(-padSpacing * 0.5f, padSpacing * 0.5f),
                new(padSpacing * 0.5f, padSpacing * 0.5f),
                new(-padSpacing * 0.5f, -padSpacing * 0.5f),
                new(padSpacing * 0.5f, -padSpacing * 0.5f),
            };

            for (int i = 0; i < cores.Length && i < grid.Length; i++)
            {
                CorBomba cor = cores[i];
                Vector2 pos = grid[i];

                GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pad.name = $"Pad_{cor}";
                pad.transform.SetParent(transform, false);
                pad.transform.localPosition = new Vector3(pos.x, padHeight * 0.5f, pos.y);
                pad.transform.localScale = new Vector3(padSize, padHeight, padSize);

                Color corBase = BombDefinitions.ObterCorUi(cor);
                Renderer rend = pad.GetComponent<Renderer>();
                Material mat = SceneBuildHelpers.MakeMat(corBase * 0.55f, 0.4f, true);
                mat.SetColor("_EmissionColor", corBase * emissivoIdle);
                rend.sharedMaterial = mat;

                BombaHotspot hotspot = pad.AddComponent<BombaHotspot>();
                CorBomba corLocal = cor;
                hotspot.Pressed += () => HandleClickPad(corLocal);

                padsVisuais[cor] = new PadVisual
                {
                    Transform = pad.transform,
                    Renderer = rend,
                    Material = mat,
                    CorBase = corBase,
                    Hotspot = hotspot,
                    EscalaBase = pad.transform.localScale,
                };
            }
        }

        protected override void HandleIniciar()
        {
            ResetarVisuais();
            DefinirInteracao(false);
        }

        protected override void HandleResolvido(ModuloBomba m)
        {
            ParaRotinaFlash();
            DefinirInteracao(false);
            StartCoroutine(PulsoSucesso());
        }

        protected override void HandleFalha(ModuloBomba m, string motivo)
        {
            ParaRotinaFlash();
            DefinirInteracao(false);
            StartCoroutine(PulsoErro());
        }

        void HandleIniciarSequencia(int total) => ResetarVisuais();

        void HandlePiscarPad(CorBomba cor)
        {
            if (rotinaFlash != null) StopCoroutine(rotinaFlash);
            rotinaFlash = StartCoroutine(FlashPad(cor, duracaoFlash));
        }

        void HandleFimPlayback() => DefinirInteracao(true);

        void HandleProgresso(int indice, int total)
        {
            // Pequeno feedback visual de "ok" no progresso.
        }

        void HandleClickPad(CorBomba cor)
        {
            if (moduloMemoria == null || !moduloMemoria.PodeReceberEntrada) return;
            if (!padsVisuais.TryGetValue(cor, out PadVisual visual)) return;

            StartCoroutine(BumpEscala(visual));
            StartCoroutine(FlashPad(cor, 0.18f));
            moduloMemoria.RegistrarEntradaVista(cor);
        }

        IEnumerator FlashPad(CorBomba cor, float duracao)
        {
            if (!padsVisuais.TryGetValue(cor, out PadVisual visual)) yield break;

            Color brilhante = visual.CorBase * emissivoBrilho;
            visual.Material.SetColor("_EmissionColor", brilhante);

            yield return new WaitForSecondsRealtime(duracao);

            visual.Material.SetColor("_EmissionColor", visual.CorBase * emissivoIdle);
        }

        IEnumerator BumpEscala(PadVisual visual)
        {
            float t = 0f;
            const float dur = 0.12f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float s = 1f + 0.18f * Mathf.Sin((t / dur) * Mathf.PI);
                visual.Transform.localScale = visual.EscalaBase * s;
                yield return null;
            }
            visual.Transform.localScale = visual.EscalaBase;
        }

        IEnumerator PulsoSucesso()
        {
            for (int i = 0; i < 3; i++)
            {
                foreach (PadVisual v in padsVisuais.Values)
                    v.Material.SetColor("_EmissionColor", v.CorBase * emissivoBrilho);
                yield return new WaitForSecondsRealtime(0.18f);
                foreach (PadVisual v in padsVisuais.Values)
                    v.Material.SetColor("_EmissionColor", v.CorBase * emissivoIdle);
                yield return new WaitForSecondsRealtime(0.12f);
            }
        }

        IEnumerator PulsoErro()
        {
            Color vermelho = new Color(1f, 0.18f, 0.16f);
            for (int i = 0; i < 2; i++)
            {
                foreach (PadVisual v in padsVisuais.Values)
                    v.Material.SetColor("_EmissionColor", vermelho * emissivoBrilho);
                yield return new WaitForSecondsRealtime(0.16f);
                foreach (PadVisual v in padsVisuais.Values)
                    v.Material.SetColor("_EmissionColor", v.CorBase * emissivoIdle);
                yield return new WaitForSecondsRealtime(0.12f);
            }
        }

        void ResetarVisuais()
        {
            foreach (PadVisual v in padsVisuais.Values)
            {
                v.Material.SetColor("_EmissionColor", v.CorBase * emissivoIdle);
                v.Transform.localScale = v.EscalaBase;
            }
        }

        void DefinirInteracao(bool habilitado)
        {
            foreach (PadVisual v in padsVisuais.Values)
                v.Hotspot.Interactable = habilitado;
        }

        void ParaRotinaFlash()
        {
            if (rotinaFlash == null) return;
            StopCoroutine(rotinaFlash);
            rotinaFlash = null;
        }
    }
}
