using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LiveWire
{
    public class ModuloSincronizadorFrequencia : ModuloBomba
    {
        const int TextureWidth = 768;
        const int TextureHeight = 320;
        // Tolerâncias absolutas da trava. As duas ondas começam alinhadas à
        // esquerda e se afastam para a direita conforme |entrada - alvo|. Usar
        // diferença absoluta (em ciclos/Hz), e não erro relativo, garante que só
        // trave com as ondas realmente sobrepostas — inclusive em frequências
        // altas, onde um erro relativo pequeno já dava uma defasagem visível.
        const float ToleranciaFreqLock = 0.15f;
        const float ToleranciaAmpLock = 0.08f;
        const float TempoTravaNecessario = 1.2f;
        const float DrenoForaDeMargem = 1.0f;
        // Ao sair da margem, a barra segura por este tempo antes de drenar —
        // evita punir o micro-ajuste de quem está segurando o lock.
        const float GracaForaDeMargem = 0.35f;

        RawImage osciloscopio;
        Texture2D textura;
        Image estabilidadeFill;
        Image bordaHeartbeat;
        Image ammeterPlate;
        RectTransform ammeterAgulha;
        Image ringFreq;
        Image ringAmp;
        Text leituraTexto;
        Text ruidoTexto;
        Text frequenciaTexto;
        Text amplitudeTexto;
        RectTransform knobFrequencia;
        RectTransform knobAmplitude;
        FrequencyKnobDragRelay relayFrequencia;
        FrequencyKnobDragRelay relayAmplitude;
        Image[] lockLeds;

        AudioSource hissSource;
        AudioSource ambienceSource;
        AudioClip clipKnobTick;
        AudioClip clipLockTick;
        AudioClip clipGlitch;
        AudioClip clipLockConfirm;

        float alvoFrequencia;
        float alvoAmplitude;
        float entradaFrequencia;
        float entradaAmplitude;
        float frequenciaNormalizada;
        float amplitudeNormalizada;
        float toleranciaFreq;
        float toleranciaAmp;
        float tempoEstavel;
        float tempoForaDeMargem;
        float freqVisual;
        float ampVisual;
        float ruidoVisual;
        float ultimoTickKnob;
        float proximoGlitch;
        float jitterFase;
        float ultimoLockTick;
        bool falhaCritica;
        bool ambienceIniciada;

        Color[] pixels;
        Color[] pixelsAnteriores;

        protected override void ConstruirConteudo(RectTransform contentRoot)
        {
            CarregarAudio();
            if (InstrucaoTexto != null)
                InstrucaoTexto.rectTransform.anchoredPosition = new Vector2(0f, -66f);

            Image metalPanel = CriarPainel(
                contentRoot,
                "RetroElectroMetalPanel",
                new Color(0.055f, 0.068f, 0.064f, 1f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            metalPanel.raycastTarget = false;
            RetroElectroUi.TryApplySprite(metalPanel, RetroElectroUi.BrushedMetal, Image.Type.Tiled);

            Image innerFrame = CriarPainel(
                contentRoot,
                "InnerInstrumentFrame",
                new Color(0.42f, 0.96f, 0.78f, 0.13f),
                new Vector2(0.025f, 0.04f),
                new Vector2(0.975f, 0.985f),
                Vector2.zero,
                Vector2.zero);
            innerFrame.raycastTarget = false;

            // Heartbeat border (4 thin red bars driven by error)
            bordaHeartbeat = CriarPainel(
                contentRoot,
                "HeartbeatBorder",
                new Color(1f, 0.08f, 0.06f, 0f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            bordaHeartbeat.raycastTarget = false;
            bordaHeartbeat.type = Image.Type.Sliced;

            CriarTexto(
                contentRoot,
                "PanelStamp",
                "RETRO ELECTRO SYNC UNIT  //  FREQUENCY LOCK",
                14,
                new Color(0.22f, 0.28f, 0.24f, 0.9f),
                TextAnchor.UpperRight,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-22f, -10f),
                new Vector2(-44f, 22f),
                new Vector2(1f, 1f)).raycastTarget = false;

            // CRT bezel + screen
            RectTransform scopeFrame = CriarRect(
                contentRoot,
                "OsciloscopioFrame",
                new Vector2(0.055f, 0.43f),
                new Vector2(0.945f, 0.94f),
                Vector2.zero,
                Vector2.zero);

            Image scopeBezel = CriarPainel(
                scopeFrame,
                "ScopeBezel",
                new Color(0.014f, 0.018f, 0.016f, 1f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            scopeBezel.raycastTarget = false;

            // Parafusos nos cantos
            for (int i = 0; i < 4; i++)
            {
                Vector2 anchor = new Vector2(i % 2 == 0 ? 0f : 1f, i < 2 ? 0f : 1f);
                Vector2 offset = new Vector2(i % 2 == 0 ? 14f : -14f, i < 2 ? 14f : -14f);
                Image bolt = CriarPainel(
                    scopeFrame,
                    $"Bolt_{i}",
                    new Color(0.08f, 0.08f, 0.08f, 1f),
                    anchor, anchor,
                    offset, new Vector2(10f, 10f));
                bolt.raycastTarget = false;
            }

            GameObject scopeGO = new GameObject("TelaVidro", typeof(RectTransform), typeof(RawImage));
            RectTransform scopeRT = (RectTransform)scopeGO.transform;
            scopeRT.SetParent(scopeFrame, false);
            scopeRT.anchorMin = Vector2.zero;
            scopeRT.anchorMax = Vector2.one;
            scopeRT.offsetMin = new Vector2(28f, 26f);
            scopeRT.offsetMax = new Vector2(-28f, -26f);
            osciloscopio = scopeGO.GetComponent<RawImage>();
            osciloscopio.color = Color.white;
            osciloscopio.raycastTarget = false;

            Image glassOverlay = CriarPainel(
                scopeFrame,
                "GlassReflection",
                new Color(0.45f, 0.85f, 0.7f, 0.06f),
                new Vector2(0f, 0.7f),
                new Vector2(1f, 1f),
                Vector2.zero,
                Vector2.zero);
            glassOverlay.raycastTarget = false;

            leituraTexto = CriarTexto(
                scopeFrame,
                "Leitura",
                string.Empty,
                16,
                new Color(0.54f, 1f, 0.68f),
                TextAnchor.UpperLeft,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(20f, -16f),
                new Vector2(-44f, 50f),
                new Vector2(0f, 1f));
            leituraTexto.raycastTarget = false;

            // Mid strip: lock bar + ammeter
            ConstruirBarraTrava(contentRoot);
            ConstruirAmmeter(contentRoot);

            ruidoTexto = CriarTexto(
                contentRoot,
                "Ruido",
                string.Empty,
                14,
                new Color(0.84f, 1f, 0.72f),
                TextAnchor.MiddleCenter,
                new Vector2(0.08f, 0.245f),
                new Vector2(0.92f, 0.292f),
                Vector2.zero,
                Vector2.zero);
            ruidoTexto.raycastTarget = false;

            // Knobs (maiores, drag vertical, com ring de LED)
            CriarKnob(contentRoot, "KnobFrequencia", "FREQUENCIA", new Vector2(0.25f, 0.125f), out knobFrequencia, out frequenciaTexto, out ringFreq, out relayFrequencia, SetFrequenciaNormalizada);
            CriarKnob(contentRoot, "KnobAmplitude", "AMPLITUDE", new Vector2(0.75f, 0.125f), out knobAmplitude, out amplitudeTexto, out ringAmp, out relayAmplitude, SetAmplitudeNormalizada);
        }

        public override void Inicializar()
        {
            PrepararModulo("GIRE OS KNOBS ATE SOBREPOR AS ONDAS", "AJUSTE FREQUENCIA E AMPLITUDE");

            falhaCritica = false;
            tempoEstavel = 0f;
            tempoForaDeMargem = 0f;
            jitterFase = 0f;
            proximoGlitch = Time.unscaledTime + UnityEngine.Random.Range(5f, 8f);

            bool facil = Dificuldade == DificuldadeBomba.Facil;
            toleranciaFreq = facil ? ToleranciaFreqLock * 1.3f : ToleranciaFreqLock;
            toleranciaAmp = facil ? ToleranciaAmpLock * 1.3f : ToleranciaAmpLock;

            alvoFrequencia = UnityEngine.Random.Range(1.4f, facil ? 3.2f : 4.4f);
            alvoAmplitude = UnityEngine.Random.Range(0.38f, 0.82f);

            frequenciaNormalizada = DistanteDoAlvo(Mathf.InverseLerp(0.8f, 5.0f, alvoFrequencia));
            amplitudeNormalizada = DistanteDoAlvo(Mathf.InverseLerp(0.2f, 1.0f, alvoAmplitude));
            AplicarValoresNormalizados();
            freqVisual = entradaFrequencia;
            ampVisual = entradaAmplitude;

            if (estabilidadeFill != null) estabilidadeFill.fillAmount = 0f;
            relayFrequencia?.DefinirVisualPorValor(frequenciaNormalizada);
            relayAmplitude?.DefinirVisualPorValor(amplitudeNormalizada);
            GarantirTextura();
            AtualizarKnobs(false);
            DesenharOsciloscopio(0f);
        }

        // A ambience so deve tocar enquanto o painel da bomba esta aberto. Por
        // isso ela e ligada/desligada pelos ganchos de sessao do painel, e nao
        // no Inicializar (que tambem roda no rearme entre fases, com o painel
        // fechado, fazendo o som vazar para a exploracao).
        public override void IniciarSessao()
        {
            IniciarAmbience();
        }

        public override void EncerrarSessao()
        {
            PararAmbience();
        }

        public override void Interagir()
        {
            if (!Resolvido)
                DefinirStatus("GIRE OS KNOBS ATE A INTERFERENCIA SUMIR");
        }

        public override bool Validar() => Resolvido;

        public override void Resetar()
        {
            if (!Resolvido) Inicializar();
        }

        void OnDisable()
        {
            PararAmbience();
        }

        void OnDestroy()
        {
            PararAmbience();
        }

        void Update()
        {
            if (Resolvido || falhaCritica || osciloscopio == null || !osciloscopio.gameObject.activeInHierarchy) return;

            float erroFrequencia = Mathf.Abs(entradaFrequencia - alvoFrequencia) / alvoFrequencia;
            float erroAmplitude = Mathf.Abs(entradaAmplitude - alvoAmplitude) / alvoAmplitude;
            float erroMaximo = Mathf.Max(erroFrequencia, erroAmplitude);
            float proximidade = 1f - Mathf.Clamp01(erroMaximo / 0.55f);
            bool sincronizado = Mathf.Abs(entradaFrequencia - alvoFrequencia) <= toleranciaFreq
                             && Mathf.Abs(entradaAmplitude - alvoAmplitude) <= toleranciaAmp;

            if (sincronizado)
            {
                tempoForaDeMargem = 0f;
                tempoEstavel = Mathf.Min(TempoTravaNecessario, tempoEstavel + Time.unscaledDeltaTime);
            }
            else
            {
                tempoForaDeMargem += Time.unscaledDeltaTime;
                if (tempoForaDeMargem > GracaForaDeMargem)
                    tempoEstavel = Mathf.Max(0f, tempoEstavel - Time.unscaledDeltaTime * DrenoForaDeMargem);
            }

            // Ímã visual: conforme a barra de trava enche, a onda desenhada
            // converge para o alvo. O lock continua sendo julgado pelos valores
            // reais; isso só garante que "sobreposto na tela" e "sincronizado"
            // contem a mesma história.
            float aderencia = Mathf.Clamp01(tempoEstavel / TempoTravaNecessario) * 0.85f;
            freqVisual = Mathf.Lerp(entradaFrequencia, alvoFrequencia, aderencia);
            ampVisual = Mathf.Lerp(entradaAmplitude, alvoAmplitude, aderencia);

            ruidoVisual = sincronizado ? 0f : Mathf.Lerp(0.11f, 0.006f, proximidade);
            jitterFase = Mathf.Lerp(jitterFase, (1f - proximidade) * 12f, Time.unscaledDeltaTime * 6f);

            // Glitch periódico: chacoalha a onda de entrada por um instante (sem
            // deslocar a fase do alvo de forma permanente, senão as ondas nunca
            // conseguiriam se sobrepor). Nunca dispara com o sinal sincronizado —
            // chacoalhar a onda bem na hora do lock parecia perda de sincronia.
            if (!sincronizado && Time.unscaledTime >= proximoGlitch && tempoEstavel < TempoTravaNecessario * 0.7f)
            {
                jitterFase = Mathf.Max(jitterFase, 7f);
                proximoGlitch = Time.unscaledTime + UnityEngine.Random.Range(6f, 9f);
                if (clipGlitch != null && hissSource != null)
                    hissSource.PlayOneShot(clipGlitch, 0.55f);
            }

            // Lock-tick acelerando enquanto a barra enche
            if (sincronizado && clipLockTick != null && hissSource != null)
            {
                float intervalo = Mathf.Lerp(0.4f, 0.08f, tempoEstavel / TempoTravaNecessario);
                if (Time.unscaledTime - ultimoLockTick > intervalo)
                {
                    ultimoLockTick = Time.unscaledTime;
                    hissSource.PlayOneShot(clipLockTick, 0.45f);
                }
            }

            if (estabilidadeFill != null)
            {
                estabilidadeFill.fillAmount = tempoEstavel / TempoTravaNecessario;
                estabilidadeFill.color = sincronizado
                    ? new Color(0.2f, 1f, 0.42f, 0.95f)
                    : Color.Lerp(new Color(1f, 0.32f, 0.18f, 0.85f), new Color(1f, 0.92f, 0.18f, 0.9f), proximidade);
            }

            AtualizarLockLeds(proximidade, sincronizado);
            AtualizarHeartbeat(erroMaximo, sincronizado);
            AtualizarAmmeter(proximidade);
            AtualizarRingsKnobs(erroFrequencia, erroAmplitude);
            AtualizarLeituras(erroFrequencia, erroAmplitude, proximidade, sincronizado);
            DesenharOsciloscopio(proximidade);

            if (tempoEstavel >= TempoTravaNecessario)
            {
                if (clipLockConfirm != null && hissSource != null)
                    hissSource.PlayOneShot(clipLockConfirm, 0.9f);
                AudioManager.Instance?.PlaySuccess();
                DefinirStatus("LOCK-ON CONFIRMADO", new Color(0.35f, 1f, 0.58f));
                EmitirResolvido("SINAL SINCRONIZADO");
                PararAmbience();
            }
        }

        // ---------- UI building ----------

        void ConstruirBarraTrava(RectTransform parent)
        {
            RectTransform root = CriarRect(
                parent,
                "LockOnCluster",
                new Vector2(0.08f, 0.31f),
                new Vector2(0.59f, 0.385f),
                Vector2.zero,
                Vector2.zero);

            Image backplate = CriarPainel(
                root,
                "LockOnBackplate",
                new Color(0.045f, 0.062f, 0.055f, 0.96f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            backplate.raycastTarget = false;

            CriarPainel(
                root,
                "LockOnTopEdge",
                new Color(0.42f, 0.95f, 0.72f, 0.24f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, -2f),
                new Vector2(-12f, 2f)).raycastTarget = false;

            CriarTexto(
                root,
                "EstabilidadeLabel",
                "TRAVA DE SINCRONIA",
                12,
                new Color(0.62f, 0.86f, 0.72f, 0.92f),
                TextAnchor.MiddleLeft,
                new Vector2(0.04f, 0.52f),
                new Vector2(0.36f, 1f),
                Vector2.zero,
                Vector2.zero).raycastTarget = false;

            Image bg = CriarPainel(
                root,
                "EstabilidadeBg",
                new Color(0.012f, 0.025f, 0.02f, 1f),
                new Vector2(0.38f, 0.55f),
                new Vector2(0.96f, 0.84f),
                Vector2.zero,
                Vector2.zero);
            bg.raycastTarget = false;

            CriarPainel(
                bg.transform,
                "EstabilidadeGlass",
                new Color(0.68f, 1f, 0.82f, 0.08f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero).raycastTarget = false;

            estabilidadeFill = CriarPainel(
                bg.transform,
                "EstabilidadeFill",
                new Color(0.2f, 1f, 0.38f, 0.92f),
                new Vector2(0.02f, 0.18f),
                new Vector2(0.98f, 0.82f),
                Vector2.zero,
                Vector2.zero);
            estabilidadeFill.type = Image.Type.Filled;
            estabilidadeFill.fillMethod = Image.FillMethod.Horizontal;
            estabilidadeFill.fillAmount = 0f;
            estabilidadeFill.raycastTarget = false;

            // Notches sobre a barra, como escala de instrumento.
            for (int i = 1; i < 5; i++)
            {
                CriarPainel(
                    bg.transform,
                    $"Notch_{i}",
                    new Color(0.75f, 1f, 0.86f, 0.24f),
                    new Vector2(i / 5f, 0.08f),
                    new Vector2(i / 5f, 0.92f),
                    Vector2.zero,
                    new Vector2(1.5f, 0f)).raycastTarget = false;
            }

            lockLeds = CriarLockLeds(root);
        }

        void ConstruirAmmeter(RectTransform parent)
        {
            RectTransform root = CriarRect(
                parent,
                "Ammeter",
                new Vector2(0.64f, 0.29f),
                new Vector2(0.92f, 0.395f),
                Vector2.zero,
                Vector2.zero);

            ammeterPlate = CriarPainel(
                root,
                "AmmeterFace",
                Color.white,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            ammeterPlate.raycastTarget = false;
            ammeterPlate.sprite = ProceduralAmmeter.Get();
            ammeterPlate.type = Image.Type.Simple;

            // Agulha
            RectTransform pivot = CriarRect(
                root,
                "AmmeterPivot",
                new Vector2(0.5f, 0.2f),
                new Vector2(0.5f, 0.2f),
                Vector2.zero,
                Vector2.zero);
            Image agulha = CriarPainel(
                pivot,
                "Agulha",
                new Color(1f, 0.2f, 0.12f, 1f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 26f),
                new Vector2(3f, 52f),
                new Vector2(0.5f, 0f));
            agulha.raycastTarget = false;
            ammeterAgulha = pivot;

            CriarTexto(
                root,
                "AmmeterLabel",
                "SYNC ERROR",
                11,
                new Color(0.68f, 0.92f, 0.76f, 0.9f),
                TextAnchor.LowerCenter,
                Vector2.zero,
                new Vector2(1f, 0f),
                new Vector2(0f, 5f),
                new Vector2(0f, 14f)).raycastTarget = false;
        }

        void CriarKnob(RectTransform parent, string nome, string label, Vector2 anchor, out RectTransform knob, out Text valueText, out Image ring, out FrequencyKnobDragRelay relay, Action<float> onValue)
        {
            RectTransform root = CriarRect(parent, nome, anchor, anchor, Vector2.zero, new Vector2(210f, 205f));

            Image socket = CriarPainel(
                root,
                "SocketPlate",
                new Color(0.035f, 0.046f, 0.042f, 0.96f),
                new Vector2(0.06f, 0.08f),
                new Vector2(0.94f, 0.92f),
                Vector2.zero,
                Vector2.zero);
            socket.raycastTarget = false;

            // Halo / LED ring
            ring = CriarPainel(
                root,
                "LedRing",
                new Color(0.95f, 0.18f, 0.12f, 0.4f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 20f),
                new Vector2(174f, 174f));
            ring.raycastTarget = false;
            RetroElectroUi.TryApplySprite(ring, RetroElectroUi.Light, Image.Type.Simple);

            Image knobImage = CriarPainel(
                root,
                "Dial",
                Color.white,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 20f),
                new Vector2(142f, 142f));
            knobImage.sprite = ProceduralKnob.Get();
            knobImage.type = Image.Type.Simple;
            knob = knobImage.rectTransform;

            relay = knobImage.gameObject.AddComponent<FrequencyKnobDragRelay>();
            relay.ValueChanged += onValue;

            CriarTexto(root, "Label", label, 16, new Color(0.78f, 0.95f, 0.84f), TextAnchor.MiddleCenter,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 30f), new Vector2(0f, 24f)).raycastTarget = false;

            valueText = CriarTexto(root, "Value", string.Empty, 15, new Color(1f, 0.88f, 0.42f), TextAnchor.MiddleCenter,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 7f), new Vector2(0f, 22f));
            valueText.raycastTarget = false;
        }

        Image[] CriarLockLeds(RectTransform parent)
        {
            Image[] leds = new Image[6];
            RectTransform root = CriarRect(parent, "LockLedRail", new Vector2(0.04f, 0.12f), new Vector2(0.96f, 0.42f), Vector2.zero, Vector2.zero);

            for (int i = 0; i < leds.Length; i++)
            {
                Image led = CriarPainel(
                    root,
                    $"LockLed_{i}",
                    new Color(0.07f, 0.13f, 0.1f, 0.9f),
                    new Vector2(i / (float)leds.Length, 0.5f),
                    new Vector2((i + 1) / (float)leds.Length, 0.5f),
                    new Vector2(0f, 0f),
                    new Vector2(-8f, 16f));
                led.raycastTarget = false;
                RetroElectroUi.TryApplySprite(led, RetroElectroUi.Light, Image.Type.Simple);
                leds[i] = led;
            }

            return leds;
        }

        // ---------- Audio ----------

        void CarregarAudio()
        {
            clipKnobTick = Resources.Load<AudioClip>("RetroElectro/Audio/KnobTick");
            clipLockTick = Resources.Load<AudioClip>("RetroElectro/Audio/LockTick");
            clipGlitch = Resources.Load<AudioClip>("RetroElectro/Audio/Glitch");
            clipLockConfirm = Resources.Load<AudioClip>("RetroElectro/Audio/LockConfirm");

            hissSource = gameObject.AddComponent<AudioSource>();
            hissSource.playOnAwake = false;
            hissSource.spatialBlend = 0f;

            ambienceSource = gameObject.AddComponent<AudioSource>();
            ambienceSource.playOnAwake = false;
            ambienceSource.loop = true;
            ambienceSource.spatialBlend = 0f;
            ambienceSource.volume = 0f;
            ambienceSource.clip = Resources.Load<AudioClip>("RetroElectro/Audio/Ambience");
        }

        void IniciarAmbience()
        {
            if (ambienceSource == null || ambienceSource.clip == null || ambienceIniciada) return;
            ambienceSource.volume = 0.28f;
            ambienceSource.Play();
            ambienceIniciada = true;
        }

        void PararAmbience()
        {
            if (ambienceSource == null || !ambienceIniciada) return;
            ambienceSource.Stop();
            ambienceIniciada = false;
        }

        // ---------- Logic helpers ----------

        float DistanteDoAlvo(float alvoNormalizado)
        {
            float candidate = UnityEngine.Random.value < 0.5f
                ? alvoNormalizado - UnityEngine.Random.Range(0.28f, 0.48f)
                : alvoNormalizado + UnityEngine.Random.Range(0.28f, 0.48f);

            if (candidate < 0f || candidate > 1f)
                candidate = alvoNormalizado < 0.5f
                    ? alvoNormalizado + UnityEngine.Random.Range(0.32f, 0.52f)
                    : alvoNormalizado - UnityEngine.Random.Range(0.32f, 0.52f);

            return Mathf.Clamp01(candidate);
        }

        void SetFrequenciaNormalizada(float valor)
        {
            frequenciaNormalizada = Mathf.Clamp01(valor);
            AplicarValoresNormalizados();
            AtualizarKnobs(true);
        }

        void SetAmplitudeNormalizada(float valor)
        {
            amplitudeNormalizada = Mathf.Clamp01(valor);
            AplicarValoresNormalizados();
            AtualizarKnobs(true);
        }

        void AplicarValoresNormalizados()
        {
            entradaFrequencia = Mathf.Lerp(0.8f, 5.0f, frequenciaNormalizada);
            entradaAmplitude = Mathf.Lerp(0.2f, 1.0f, amplitudeNormalizada);
        }

        void AtualizarKnobs(bool tickAudio)
        {
            if (frequenciaTexto != null) frequenciaTexto.text = $"{entradaFrequencia:0.00} Hz";
            if (amplitudeTexto != null) amplitudeTexto.text = $"{entradaAmplitude:0.00} AMP";

            if (tickAudio && Time.unscaledTime - ultimoTickKnob > 0.075f)
            {
                ultimoTickKnob = Time.unscaledTime;
                if (clipKnobTick != null && hissSource != null)
                    hissSource.PlayOneShot(clipKnobTick, 0.5f);
                else
                    AudioManager.Instance?.PlayClick();
            }
        }

        void AtualizarLockLeds(float proximidade, bool sincronizado)
        {
            if (lockLeds == null) return;

            int acesos = Mathf.Clamp(Mathf.CeilToInt(proximidade * lockLeds.Length), 0, lockLeds.Length);
            for (int i = 0; i < lockLeds.Length; i++)
            {
                bool on = sincronizado || i < acesos;
                Color offColor = new Color(0.08f, 0.13f, 0.1f, 0.72f);
                Color warmColor = Color.Lerp(new Color(0.9f, 0.14f, 0.08f, 1f), new Color(1f, 0.85f, 0.12f, 1f), i / (float)(lockLeds.Length - 1));
                lockLeds[i].color = on
                    ? (sincronizado ? new Color(0.24f, 1f, 0.36f, 1f) : warmColor)
                    : offColor;
            }
        }

        void AtualizarHeartbeat(float erroMaximo, bool sincronizado)
        {
            if (bordaHeartbeat == null) return;

            if (sincronizado)
            {
                Color c = bordaHeartbeat.color;
                c.r = 0.24f; c.g = 1f; c.b = 0.36f;
                c.a = 0.18f + Mathf.PingPong(Time.unscaledTime * 1.4f, 0.18f);
                bordaHeartbeat.color = c;
                return;
            }

            float intensidade = Mathf.Clamp01(erroMaximo / 0.6f);
            float taxa = Mathf.Lerp(0.6f, 4.5f, intensidade);
            float pulso = (Mathf.Sin(Time.unscaledTime * taxa * Mathf.PI * 2f) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(0.05f, 0.55f, intensidade) * pulso;
            bordaHeartbeat.color = new Color(1f, 0.08f, 0.06f, alpha);
        }

        void AtualizarAmmeter(float proximidade)
        {
            if (ammeterAgulha == null) return;
            // proximidade 0 → -55°, proximidade 1 → +55°
            float zAlvo = Mathf.Lerp(-55f, 55f, proximidade);
            float jitter = (1f - proximidade) * UnityEngine.Random.Range(-6f, 6f);
            float current = ammeterAgulha.localEulerAngles.z;
            if (current > 180f) current -= 360f;
            float z = Mathf.Lerp(current, zAlvo + jitter, Time.unscaledDeltaTime * 8f);
            ammeterAgulha.localEulerAngles = new Vector3(0f, 0f, z);
        }

        void AtualizarRingsKnobs(float erroFreq, float erroAmp)
        {
            AtualizarRing(ringFreq, erroFreq);
            AtualizarRing(ringAmp, erroAmp);
        }

        void AtualizarRing(Image ring, float erro)
        {
            if (ring == null) return;
            float prox = 1f - Mathf.Clamp01(erro / 0.5f);
            Color cor = Color.Lerp(new Color(1f, 0.12f, 0.08f, 0.65f), new Color(0.28f, 1f, 0.42f, 0.7f), prox);
            cor.a *= 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * Mathf.Lerp(1.4f, 4.5f, prox)));
            ring.color = cor;
        }

        void AtualizarLeituras(float erroFreq, float erroAmp, float proximidade, bool sincronizado)
        {
            AtualizarDicaKnob(frequenciaTexto, $"{entradaFrequencia:0.00} Hz", entradaFrequencia, alvoFrequencia, toleranciaFreq);
            AtualizarDicaKnob(amplitudeTexto, $"{entradaAmplitude:0.00} AMP", entradaAmplitude, alvoAmplitude, toleranciaAmp);

            if (leituraTexto != null)
            {
                leituraTexto.text =
                    $"ALVO  FREQ {alvoFrequencia:0.00}  AMP {alvoAmplitude:0.00}\n" +
                    $"ENTR. FREQ {entradaFrequencia:0.00}  AMP {entradaAmplitude:0.00}";
            }

            if (ruidoTexto != null)
            {
                float ruido = 100f - proximidade * 100f;
                ruidoTexto.text = sincronizado
                    ? $"SINAL ALINHADO  TRAVA {tempoEstavel:0.0}/{TempoTravaNecessario:0.0}s"
                    : $"INTERFERENCIA {ruido:00}%  ERRO F:{erroFreq * 100f:0.0}% A:{erroAmp * 100f:0.0}%";
            }

            DefinirStatus(sincronizado ? "MANTENHA A SINCRONIA" : "BUSCANDO LOCK-ON");
        }

        // Dica de direção junto ao valor do knob: ▲ = arraste para cima,
        // ▼ = para baixo, OK = dentro da margem de trava.
        void AtualizarDicaKnob(Text texto, string valorFormatado, float entrada, float alvo, float tolerancia)
        {
            if (texto == null) return;

            if (Mathf.Abs(entrada - alvo) <= tolerancia)
            {
                texto.text = $"{valorFormatado} OK";
                texto.color = new Color(0.35f, 1f, 0.58f);
            }
            else
            {
                texto.text = $"{valorFormatado} {(entrada < alvo ? "▲" : "▼")}";
                texto.color = new Color(1f, 0.88f, 0.42f);
            }
        }

        // ---------- Scope rendering ----------

        void GarantirTextura()
        {
            if (textura != null) return;

            textura = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, false);
            textura.filterMode = FilterMode.Bilinear;
            pixels = new Color[TextureWidth * TextureHeight];
            pixelsAnteriores = new Color[TextureWidth * TextureHeight];
            osciloscopio.texture = textura;
        }

        void DesenharOsciloscopio(float proximidade)
        {
            GarantirTextura();

            // Phosphor afterglow: fade último frame
            Color bgFade = new Color(0.004f, 0.015f, 0.012f, 1f);
            for (int i = 0; i < pixels.Length; i++)
            {
                Color p = pixelsAnteriores[i];
                pixels[i] = new Color(
                    Mathf.Lerp(p.r, bgFade.r, 0.18f),
                    Mathf.Lerp(p.g, bgFade.g, 0.16f),
                    Mathf.Lerp(p.b, bgFade.b, 0.18f),
                    1f);
            }

            // Grid
            Color grid = new Color(0.04f, 0.18f, 0.12f, 1f);
            for (int x = 0; x < TextureWidth; x += 48)
                DesenharLinha(x, 0, x, TextureHeight - 1, grid, 1);
            for (int y = 0; y < TextureHeight; y += 32)
                DesenharLinha(0, y, TextureWidth - 1, y, grid, 1);

            // Scan-lines
            Color scan = new Color(0f, 0f, 0f, 1f);
            for (int y = 0; y < TextureHeight; y += 2)
            {
                int row = y * TextureWidth;
                for (int x = 0; x < TextureWidth; x++)
                {
                    Color c = pixels[row + x];
                    pixels[row + x] = Color.Lerp(c, scan, 0.18f);
                }
            }

            int jitterX = jitterFase > 0.5f ? UnityEngine.Random.Range(-Mathf.RoundToInt(jitterFase), Mathf.RoundToInt(jitterFase) + 1) : 0;

            Color targetColor = new Color(0.2f, 0.95f, 1f, 1f);
            Color dynamicColor = Color.Lerp(new Color(1f, 0.08f, 0.05f, 1f), new Color(1f, 0.88f, 0.08f, 1f), Mathf.Clamp01(proximidade * 1.25f));
            if (tempoEstavel >= TempoTravaNecessario * 0.9f)
                dynamicColor = new Color(0.28f, 1f, 0.36f, 1f);

            DesenharOnda(alvoFrequencia, alvoAmplitude, 0f, 0f, 0, targetColor, 2);
            DesenharOnda(freqVisual, ampVisual, 0f, ruidoVisual, jitterX, dynamicColor, proximidade > 0.88f ? 3 : 2);

            // Vinheta nos cantos
            ApliacarVinheta();

            // Copia para anteriores e blita
            Array.Copy(pixels, pixelsAnteriores, pixels.Length);
            textura.SetPixels(pixels);
            textura.Apply(false);
        }

        void ApliacarVinheta()
        {
            float cx = TextureWidth * 0.5f;
            float cy = TextureHeight * 0.5f;
            float maxDist = Mathf.Sqrt(cx * cx + cy * cy);
            for (int y = 0; y < TextureHeight; y += 2)
            {
                for (int x = 0; x < TextureWidth; x += 2)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / maxDist;
                    float falloff = Mathf.SmoothStep(0.65f, 1f, d) * 0.55f;
                    int idx = y * TextureWidth + x;
                    Color c = pixels[idx];
                    pixels[idx] = new Color(c.r * (1f - falloff), c.g * (1f - falloff), c.b * (1f - falloff), 1f);
                }
            }
        }

        void DesenharOnda(float frequencia, float amplitude, float faseExtra, float ruido, int jitterX, Color color, int espessura)
        {
            int prevX = 0;
            int prevY = CentroY(frequencia, amplitude, faseExtra, ruido, 0);

            for (int x = 1; x < TextureWidth; x++)
            {
                int y = CentroY(frequencia, amplitude, faseExtra, ruido, x);
                DesenharLinha(prevX + jitterX, prevY, x + jitterX, y, color, espessura);
                prevX = x;
                prevY = y;
            }
        }

        int CentroY(float frequencia, float amplitude, float faseExtra, float ruido, int x)
        {
            float t = x / (float)(TextureWidth - 1);
            float wave = Mathf.Sin((t * frequencia * Mathf.PI * 2f) + Time.unscaledTime * 0.8f + faseExtra);
            float noise = ruido <= 0f ? 0f : (Mathf.PerlinNoise(t * 40f, Time.unscaledTime * 18f) - 0.5f) * ruido;
            float y = 0.5f + (wave * amplitude * 0.42f) + noise;
            return Mathf.Clamp(Mathf.RoundToInt(y * (TextureHeight - 1)), 2, TextureHeight - 3);
        }

        void DesenharLinha(int x0, int y0, int x1, int y1, Color color, int espessura = 1)
        {
            int dx = Mathf.Abs(x1 - x0);
            int sx = x0 < x1 ? 1 : -1;
            int dy = -Mathf.Abs(y1 - y0);
            int sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;

            while (true)
            {
                Plot(x0, y0, color, espessura);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy)
                {
                    err += dy;
                    x0 += sx;
                }
                if (e2 <= dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }

        void Plot(int x, int y, Color color, int espessura)
        {
            for (int ox = -espessura; ox <= espessura; ox++)
            {
                for (int oy = -espessura; oy <= espessura; oy++)
                {
                    int px = x + ox;
                    int py = y + oy;
                    if (px < 0 || px >= TextureWidth || py < 0 || py >= TextureHeight) continue;
                    float falloff = 1f - (Mathf.Abs(ox) + Mathf.Abs(oy)) / (float)(espessura * 2 + 1);
                    Color current = pixels[py * TextureWidth + px];
                    Color glow = new Color(
                        Mathf.Min(1f, current.r + color.r * falloff),
                        Mathf.Min(1f, current.g + color.g * falloff),
                        Mathf.Min(1f, current.b + color.b * falloff),
                        1f);
                    pixels[py * TextureWidth + px] = glow;
                }
            }
        }

        public void MostrarFalhaCritica()
        {
            falhaCritica = true;
            DefinirStatus("CRITICAL FAILURE", new Color(1f, 0.12f, 0.08f));
            if (ruidoTexto != null) ruidoTexto.text = "CRITICAL FAILURE";
            if (leituraTexto != null) leituraTexto.text = "SIGNAL LOST\nCORE DETONATION";
            PararAmbience();
        }
    }

    // Knob com arraste suave: arrastar para cima (ou para a direita) aumenta o
    // valor e gira o dial proporcionalmente. O relay é o dono do valor
    // normalizado (0..1) e o clampa — assim o dial tem batentes de verdade e
    // nunca dessincroniza do valor, mesmo insistindo além do fim do curso.
    // ValueChanged entrega o valor absoluto. A roda do mouse faz o ajuste fino.
    public class FrequencyKnobDragRelay : MonoBehaviour, IDragHandler, IScrollHandler
    {
        public event Action<float> ValueChanged;

        // Graus de giro do dial que equivalem ao curso completo do valor (0..1).
        public float Sweep = 270f;
        // Pixels de arraste (vertical + horizontal) equivalentes ao curso completo.
        public float PixelsParaCursoCompleto = 300f;
        // Teto de variação por evento: um flick rápido chega como um delta
        // grande num frame só; sem o teto, um toquinho virava um salto brusco.
        const float MaxDeltaPorEvento = 0.04f;
        // Passo do ajuste fino via roda do mouse.
        const float PassoScroll = 0.012f;

        RectTransform dial;
        Canvas canvas;
        float valor;

        void Awake()
        {
            dial = (RectTransform)transform;
        }

        // Posiciona o dial num ângulo coerente com um valor normalizado
        // (0 → +135°, 0.5 → 0°, 1 → -135° quando Sweep = 270).
        public void DefinirVisualPorValor(float valorNormalizado)
        {
            valor = Mathf.Clamp01(valorNormalizado);
            AplicarVisual();
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Divide pelo scaleFactor do canvas para a sensibilidade não variar
            // com a resolução. Vertical e horizontal somam: dá para continuar o
            // giro na direção que ainda tem espaço na tela.
            if (canvas == null) canvas = GetComponentInParent<Canvas>();
            float escala = canvas != null ? Mathf.Max(0.001f, canvas.scaleFactor) : 1f;
            float pixels = (eventData.delta.y + eventData.delta.x) / escala;
            Aplicar(pixels / PixelsParaCursoCompleto);
        }

        public void OnScroll(PointerEventData eventData)
        {
            // Só o sinal importa: touchpads mandam deltas grandes e irregulares.
            if (Mathf.Abs(eventData.scrollDelta.y) > Mathf.Epsilon)
                Aplicar(Mathf.Sign(eventData.scrollDelta.y) * PassoScroll);
        }

        void Aplicar(float deltaValor)
        {
            deltaValor = Mathf.Clamp(deltaValor, -MaxDeltaPorEvento, MaxDeltaPorEvento);
            float novoValor = Mathf.Clamp01(valor + deltaValor);
            if (Mathf.Approximately(novoValor, valor)) return;

            valor = novoValor;
            AplicarVisual();
            ValueChanged?.Invoke(valor);
        }

        void AplicarVisual()
        {
            if (dial == null) dial = (RectTransform)transform;
            dial.localEulerAngles = new Vector3(0f, 0f, (0.5f - valor) * Sweep);
        }
    }

    static class ProceduralAmmeter
    {
        static Sprite cache;

        public static Sprite Get()
        {
            if (cache != null) return cache;

            const int W = 384;
            const int H = 128;
            Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, true);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            Color[] px = new Color[W * H];

            Color clear = new Color(0f, 0f, 0f, 0f);
            Color casing = new Color(0.025f, 0.034f, 0.031f, 1f);
            Color face = new Color(0.052f, 0.073f, 0.063f, 1f);
            Color faceHi = new Color(0.12f, 0.18f, 0.145f, 1f);
            Color rim = new Color(0.32f, 0.88f, 0.62f, 0.5f);
            Color tick = new Color(0.68f, 1f, 0.78f, 0.9f);
            Color redTick = new Color(1f, 0.18f, 0.12f, 0.95f);

            Vector2 center = new Vector2(W * 0.5f, H * 1.02f);
            float rx = W * 0.42f;
            float ry = H * 0.88f;

            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    float u = x / (float)(W - 1);
                    float v = y / (float)(H - 1);
                    float edge = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));
                    Color c = casing;

                    float dx = (x - center.x) / rx;
                    float dy = (y - center.y) / ry;
                    float ellipse = dx * dx + dy * dy;

                    if (ellipse < 1f && y < H * 0.95f)
                    {
                        float shade = Mathf.Clamp01(1f - ellipse);
                        c = Color.Lerp(face, faceHi, shade * 0.55f + (1f - v) * 0.18f);
                    }

                    if (edge < 0.035f)
                        c = Color.Lerp(clear, c, Mathf.Clamp01(edge / 0.035f));

                    // Subtle glass sweep.
                    if (ellipse < 0.92f && y < H * 0.56f && x > W * 0.08f && x < W * 0.9f)
                        c = Color.Lerp(c, new Color(0.42f, 0.95f, 0.78f, 1f), 0.055f * (1f - v));

                    px[y * W + x] = c;
                }
            }

            Vector2 pivot = new Vector2(W * 0.5f, H * 0.83f);
            for (int i = 0; i <= 12; i++)
            {
                float t = i / 12f;
                float angle = Mathf.Lerp(205f, 335f, t) * Mathf.Deg2Rad;
                float outer = W * 0.31f;
                float inner = i % 3 == 0 ? W * 0.25f : W * 0.275f;
                Color tc = i >= 10 ? redTick : tick;
                DrawLine(px, W, H,
                    Mathf.RoundToInt(pivot.x + Mathf.Cos(angle) * inner),
                    Mathf.RoundToInt(pivot.y + Mathf.Sin(angle) * inner * 0.62f),
                    Mathf.RoundToInt(pivot.x + Mathf.Cos(angle) * outer),
                    Mathf.RoundToInt(pivot.y + Mathf.Sin(angle) * outer * 0.62f),
                    tc,
                    i % 3 == 0 ? 2 : 1);
            }

            DrawLine(px, W, H, 18, 12, W - 18, 12, rim, 1);
            DrawLine(px, W, H, 18, H - 16, W - 18, H - 16, new Color(0.08f, 0.13f, 0.1f, 0.9f), 2);

            tex.SetPixels(px);
            tex.Apply(true);
            cache = Sprite.Create(tex, new Rect(0f, 0f, W, H), new Vector2(0.5f, 0.5f), 100f);
            cache.name = "ProceduralAmmeter";
            return cache;
        }

        static void DrawLine(Color[] px, int width, int height, int x0, int y0, int x1, int y1, Color color, int thickness)
        {
            int dx = Mathf.Abs(x1 - x0);
            int sx = x0 < x1 ? 1 : -1;
            int dy = -Mathf.Abs(y1 - y0);
            int sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;

            while (true)
            {
                Plot(px, width, height, x0, y0, color, thickness);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy)
                {
                    err += dy;
                    x0 += sx;
                }
                if (e2 <= dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }

        static void Plot(Color[] px, int width, int height, int x, int y, Color color, int thickness)
        {
            for (int ox = -thickness; ox <= thickness; ox++)
            {
                for (int oy = -thickness; oy <= thickness; oy++)
                {
                    int xx = x + ox;
                    int yy = y + oy;
                    if (xx < 0 || xx >= width || yy < 0 || yy >= height) continue;
                    float falloff = 1f - (Mathf.Abs(ox) + Mathf.Abs(oy)) / (float)(thickness * 2 + 1);
                    Color current = px[yy * width + xx];
                    px[yy * width + xx] = Color.Lerp(current, color, Mathf.Clamp01(falloff));
                }
            }
        }
    }

    static class ProceduralKnob
    {
        static Sprite cache;

        public static Sprite Get()
        {
            if (cache != null) return cache;

            const int N = 256;
            Texture2D tex = new Texture2D(N, N, TextureFormat.RGBA32, true);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            Color[] px = new Color[N * N];

            float c = (N - 1) * 0.5f;
            float maxR = N * 0.5f - 1f;

            Color rim = new Color(0.04f, 0.045f, 0.04f, 1f);
            Color faceTop = new Color(0.46f, 0.47f, 0.44f, 1f);
            Color faceBot = new Color(0.13f, 0.135f, 0.125f, 1f);
            Color bevelHi = new Color(0.82f, 0.82f, 0.78f, 1f);
            Color tickColor = new Color(0.88f, 0.9f, 0.82f, 1f);
            Color pointer = new Color(1f, 0.85f, 0.18f, 1f);
            Color centerCap = new Color(0.07f, 0.07f, 0.065f, 1f);
            Color clear = new Color(0f, 0f, 0f, 0f);

            for (int y = 0; y < N; y++)
            {
                for (int x = 0; x < N; x++)
                {
                    float dx = x - c;
                    float dy = y - c;
                    float r = Mathf.Sqrt(dx * dx + dy * dy) / maxR;

                    if (r > 1.0f)
                    {
                        px[y * N + x] = clear;
                        continue;
                    }

                    Color o;
                    float vert = Mathf.Clamp01(0.5f + (dy / maxR) * 0.55f);

                    if (r < 0.82f)
                    {
                        o = Color.Lerp(faceBot, faceTop, Mathf.SmoothStep(0f, 1f, vert));
                        // brushed-metal stripes (subtle)
                        float stripe = (Mathf.Sin(x * 0.55f + y * 0.05f) + Mathf.Sin(x * 0.13f)) * 0.012f;
                        o.r += stripe; o.g += stripe; o.b += stripe;
                    }
                    else if (r < 0.92f)
                    {
                        float t = (r - 0.82f) / 0.10f;
                        float topness = Mathf.Clamp01(0.5f + dy / maxR);
                        Color shine = Color.Lerp(faceTop, bevelHi, Mathf.Pow(topness, 1.3f));
                        o = Color.Lerp(shine, rim, t * t);
                    }
                    else
                    {
                        o = rim;
                    }

                    float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                    float fromTop = Mathf.DeltaAngle(90f, ang);

                    // Tick marks (every 22.5°, only over the usable arc -135..135)
                    if (r > 0.86f && r < 0.965f)
                    {
                        float nearest = Mathf.Round(fromTop / 22.5f) * 22.5f;
                        if (Mathf.Abs(nearest) <= 135.01f && Mathf.Abs(fromTop - nearest) < 1.4f)
                            o = Color.Lerp(o, tickColor, 0.85f);
                    }

                    // Pointer notch from r=0.32 to r=0.78, ~3° wide at top
                    if (r > 0.32f && r < 0.78f && Mathf.Abs(fromTop) < 3.2f)
                    {
                        float edgeFade = 1f - Mathf.Clamp01((Mathf.Abs(fromTop) - 2.0f) / 1.2f);
                        o = Color.Lerp(o, pointer, edgeFade);
                    }

                    // Center cap
                    if (r < 0.16f)
                    {
                        Color cap = Color.Lerp(centerCap, faceTop * 0.55f, Mathf.SmoothStep(0f, 0.16f, r));
                        o = cap;
                    }

                    // Edge anti-alias
                    if (r > 0.985f) o.a = Mathf.Clamp01((1.0f - r) / 0.015f);

                    px[y * N + x] = o;
                }
            }

            tex.SetPixels(px);
            tex.Apply(true);
            cache = Sprite.Create(tex, new Rect(0f, 0f, N, N), new Vector2(0.5f, 0.5f), 100f);
            cache.name = "ProceduralKnob";
            return cache;
        }
    }

    static class RetroElectroUi
    {
        public const string Knob = "RetroElectro/Knob";
        public const string Light = "RetroElectro/Light";
        public const string BrushedMetal = "RetroElectro/BrushedMetal";
        public const string ScopeOff = "RetroElectro/ScopeOff";
        public const string ScopeGrid = "RetroElectro/ScopeGrid";
        public const string AmmeterOn = "RetroElectro/AmmeterOn";

        static readonly System.Collections.Generic.Dictionary<string, Sprite> SpriteCache = new();

        public static bool TryApplySprite(Image image, string resourcePath, Image.Type imageType)
        {
            if (image == null) return false;

            Sprite sprite = LoadSprite(resourcePath);
            if (sprite == null) return false;

            image.sprite = sprite;
            image.type = imageType;
            image.color = Color.white;
            if (imageType == Image.Type.Tiled)
                image.pixelsPerUnitMultiplier = 0.65f;
            return true;
        }

        static Sprite LoadSprite(string resourcePath)
        {
            if (SpriteCache.TryGetValue(resourcePath, out Sprite cached) && cached != null)
                return cached;

            // Tenta como Sprite primeiro (caso o import já esteja Sprite-friendly)
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null)
            {
                Texture2D texture = Resources.Load<Texture2D>(resourcePath);
                if (texture == null) return null;
                sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
                sprite.name = $"RetroElectro_{texture.name}";
            }

            SpriteCache[resourcePath] = sprite;
            return sprite;
        }
    }
}
