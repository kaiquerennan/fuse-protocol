using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LiveWire
{
    public class ModuloFios : ModuloBomba
    {
        enum ModoFios { Reacao, Diagnostico }
        enum RegraReacao { ComVermelho, NaoSozinho, DepoisVerdeApagar, ComAzul, MaisRapido, MaisDevagar }

        class FioReacao
        {
            public string Nome;
            public Color Cor;
            public float Intervalo;
            public bool Aceso;
            public bool AcesoAnterior;
            public float UltimoApagou = -999f;
            public Image Corpo;
            public Image Brilho;
            public Text Estado;
        }

        class FioDiagnostico
        {
            public string Nome;
            public Color Cor;
            public bool Energizado;
            public bool Marcado;
            public Image Painel;
            public Text Rotulo;
        }

        readonly List<FioDiagnostico> selecionados = new();

        RectTransform raizDinamica;
        Text dicaTexto;
        Text cronometroTexto;
        Text feedbackTexto;
        Image timingFill;
        Image ponteiro;

        ModoFios modoAtual;
        RegraReacao regraAtual;
        FioReacao[] fiosReacao;
        FioDiagnostico[] fiosDiagnostico;
        float inicioReacao;
        float tempoRestante;
        int testesUsados;
        float voltagemAtual;
        bool falhaFatal;

        static readonly Color Vermelho = new(1f, 0.18f, 0.16f);
        static readonly Color Azul = new(0.18f, 0.52f, 1f);
        static readonly Color Verde = new(0.2f, 1f, 0.4f);
        static readonly Color Amarelo = new(1f, 0.84f, 0.15f);
        static readonly Color Laranja = new(1f, 0.48f, 0.12f);

        protected override void ConstruirConteudo(RectTransform contentRoot)
        {
            raizDinamica = contentRoot;
        }

        public override void Inicializar()
        {
            PrepararModulo("MODO SORTEADO. COMPLETE EM 30 SEGUNDOS.", "INICIALIZANDO CIRCUITO");
            falhaFatal = false;
            tempoRestante = 30f;
            testesUsados = 0;
            voltagemAtual = 0f;
            selecionados.Clear();
            LimparConteudo();

            modoAtual = Random.value < 0.5f ? ModoFios.Reacao : ModoFios.Diagnostico;
            if (modoAtual == ModoFios.Reacao)
                ConstruirModoReacao();
            else
                ConstruirModoDiagnostico();
        }

        public override void Interagir()
        {
            if (Resolvido) return;
            DefinirStatus(modoAtual == ModoFios.Reacao ? "CORTE NO MOMENTO EXATO" : "TESTE PARES E MARQUE NEUTROS");
        }

        public override bool Validar() => Resolvido;

        public override void Resetar()
        {
            if (!Resolvido && !falhaFatal)
                Inicializar();
        }

        void Update()
        {
            if (Resolvido || falhaFatal || raizDinamica == null || !raizDinamica.gameObject.activeInHierarchy) return;

            tempoRestante -= Time.unscaledDeltaTime;
            if (cronometroTexto != null)
                cronometroTexto.text = $"{Mathf.Max(0f, tempoRestante):0.0}s";

            if (tempoRestante <= 0f)
            {
                GameOverImediato("TEMPO ESGOTADO");
                return;
            }

            if (modoAtual == ModoFios.Reacao)
                AtualizarModoReacao();
            else
                AtualizarPonteiroDiagnostico();
        }

        void ConstruirModoReacao()
        {
            DefinirInstrucao("MODO REACAO: observe os pulsos e corte o fio no instante certo.");
            DefinirStatus("MODO REACAO");

            CriarCabecalho("MODO REACAO", "FIOS COM PULSO");
            regraAtual = (RegraReacao)Random.Range(0, 6);
            dicaTexto.text = DicaReacao(regraAtual);

            timingFill = CriarBarra(new Vector2(0f, -78f), new Vector2(-18f, 12f), new Color(0.2f, 1f, 0.45f));
            CriarTexto(raizDinamica, "TimingLabel", "TIMING PERFEITO", 13, new Color(0.66f, 0.92f, 0.75f), TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -94f), new Vector2(-18f, 20f));

            fiosReacao = new[]
            {
                new FioReacao { Nome = "VERMELHO", Cor = Vermelho, Intervalo = 2f },
                new FioReacao { Nome = "AZUL", Cor = Azul, Intervalo = 1f },
                new FioReacao { Nome = "VERDE", Cor = Verde, Intervalo = 3f },
                new FioReacao { Nome = "AMARELO", Cor = Amarelo, Intervalo = 0.5f }
            };

            inicioReacao = Time.unscaledTime + Random.Range(0f, 0.35f);
            for (int i = 0; i < fiosReacao.Length; i++)
                CriarLinhaReacao(fiosReacao[i], i);
        }

        void CriarLinhaReacao(FioReacao fio, int indice)
        {
            Image linha = CriarPainel(raizDinamica, $"FioReacao_{indice}", new Color(0.04f, 0.065f, 0.06f, 0.96f),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -126f - indice * 64f), new Vector2(-18f, 52f));

            CriarTexto(linha.transform, "Nome", fio.Nome, 15, fio.Cor, TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(60f, 0f), new Vector2(118f, 0f));

            fio.Brilho = CriarPainel(linha.transform, "Brilho", new Color(fio.Cor.r, fio.Cor.g, fio.Cor.b, 0.08f),
                new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-18f, 0f), new Vector2(-248f, 30f));

            fio.Corpo = CriarPainel(linha.transform, "Corpo", fio.Cor * 0.35f,
                new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-18f, 0f), new Vector2(-280f, 12f));

            fio.Estado = CriarTexto(linha.transform, "Estado", "apagado", 13, new Color(0.72f, 0.88f, 0.78f), TextAnchor.MiddleCenter,
                new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-128f, 0f), new Vector2(70f, 0f));

            Button botao = CriarBotao(linha.transform, "CORTAR", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-42f, 0f), new Vector2(78f, 34f), new Color(0.12f, 0.08f, 0.07f), 13);
            FioReacao fioLocal = fio;
            botao.onClick.AddListener(() => CortarFioReacao(fioLocal));
        }

        void AtualizarModoReacao()
        {
            int acesos = 0;
            foreach (FioReacao fio in fiosReacao)
            {
                fio.AcesoAnterior = fio.Aceso;
                float fase = Mathf.Repeat(Time.unscaledTime - inicioReacao, fio.Intervalo);
                fio.Aceso = fase < fio.Intervalo * 0.5f;
                if (fio.AcesoAnterior && !fio.Aceso) fio.UltimoApagou = Time.unscaledTime;
                if (fio.Aceso) acesos++;

                float brilho = fio.Aceso ? 1f : 0.24f;
                fio.Corpo.color = new Color(fio.Cor.r * brilho, fio.Cor.g * brilho, fio.Cor.b * brilho, 1f);
                fio.Brilho.color = new Color(fio.Cor.r, fio.Cor.g, fio.Cor.b, fio.Aceso ? 0.38f : 0.08f);
                fio.Estado.text = fio.Aceso ? "ACESO" : "apagado";
            }

            if (timingFill != null)
            {
                float qualidade = QualidadeTiming(regraAtual, acesos);
                timingFill.fillAmount = qualidade;
                timingFill.color = Color.Lerp(new Color(1f, 0.24f, 0.12f), new Color(0.25f, 1f, 0.45f), qualidade);
            }
        }

        void CortarFioReacao(FioReacao fio)
        {
            if (Resolvido || falhaFatal) return;

            int acesos = 0;
            foreach (FioReacao item in fiosReacao)
                if (item.Aceso) acesos++;

            if (ReacaoCorreta(regraAtual, fio, acesos))
            {
                TimerController.Instance?.AddTime(5f);
                AudioManager.Instance?.PlaySuccess();
                DefinirStatus("DESARMADO! +5 segundos", new Color(0.35f, 1f, 0.58f));
                EmitirResolvido("PROXIMO DESAFIO LIBERADO");
                return;
            }

            TimerController.Instance?.ApplyPenalty(3f);
            AudioManager.Instance?.PlayShock();
            CameraShake.Instance?.Impulse(0.35f);
            DefinirStatus("CHOQUE! -3 segundos", new Color(1f, 0.45f, 0.42f));
        }

        void ConstruirModoDiagnostico()
        {
            DefinirInstrucao("MODO DIAGNOSTICO: faca ate 3 medicoes e corte somente fios neutros.");
            DefinirStatus("MODO DIAGNOSTICO");

            CriarCabecalho("MODO DIAGNOSTICO", "MULTIMETRO DIGITAL");
            dicaTexto.text = "Use a logica! 0V = mesmo potencial, 220V = potenciais diferentes.";

            Image display = CriarPainel(raizDinamica, "Display", new Color(0.01f, 0.018f, 0.015f, 0.98f),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -96f), new Vector2(-18f, 72f));
            feedbackTexto = CriarTexto(display.transform, "Voltage", "000V", 42, new Color(0.36f, 1f, 0.56f), TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ponteiro = CriarPainel(display.transform, "Ponteiro", new Color(1f, 0.25f, 0.12f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-70f, 8f), new Vector2(140f, 5f), new Vector2(0f, 0.5f));

            CriarBotao(raizDinamica, "TESTAR PAR", new Vector2(0f, 1f), new Vector2(0.5f, 1f),
                new Vector2(112f, -178f), new Vector2(142f, 36f), new Color(0.08f, 0.14f, 0.12f), 14)
                .onClick.AddListener(TestarParDiagnostico);

            CriarBotao(raizDinamica, "CORTAR MARCADOS", new Vector2(0.5f, 1f), new Vector2(1f, 1f),
                new Vector2(-112f, -178f), new Vector2(170f, 36f), new Color(0.18f, 0.07f, 0.06f), 14)
                .onClick.AddListener(CortarMarcadosDiagnostico);

            fiosDiagnostico = new[]
            {
                new FioDiagnostico { Nome = "VERMELHO", Cor = Vermelho },
                new FioDiagnostico { Nome = "AZUL", Cor = Azul },
                new FioDiagnostico { Nome = "VERDE", Cor = Verde },
                new FioDiagnostico { Nome = "AMARELO", Cor = Amarelo },
                new FioDiagnostico { Nome = "LARANJA", Cor = Laranja }
            };

            List<int> pool = new() { 0, 1, 2, 3, 4 };
            int energizados = Random.Range(2, 4);
            for (int i = 0; i < energizados; i++)
            {
                int pick = Random.Range(0, pool.Count);
                fiosDiagnostico[pool[pick]].Energizado = true;
                pool.RemoveAt(pick);
            }

            for (int i = 0; i < fiosDiagnostico.Length; i++)
                CriarLinhaDiagnostico(fiosDiagnostico[i], i);
        }

        void CriarLinhaDiagnostico(FioDiagnostico fio, int indice)
        {
            Image linha = CriarPainel(raizDinamica, $"FioDiagnostico_{indice}", new Color(0.04f, 0.065f, 0.06f, 0.96f),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -226f - indice * 44f), new Vector2(-18f, 36f));
            fio.Painel = linha;

            CriarPainel(linha.transform, "Corpo", fio.Cor, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-8f, 0f), new Vector2(-300f, 10f));

            fio.Rotulo = CriarTexto(linha.transform, "Rotulo", fio.Nome, 13, fio.Cor, TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(62f, 0f), new Vector2(126f, 0f));

            Button selecionar = CriarBotao(linha.transform, "SEL", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-108f, 0f), new Vector2(58f, 26f), new Color(0.07f, 0.11f, 0.1f), 12);
            selecionar.onClick.AddListener(() => AlternarSelecao(fio));

            Button marcar = CriarBotao(linha.transform, "CORTAR?", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-38f, 0f), new Vector2(70f, 26f), new Color(0.12f, 0.07f, 0.06f), 11);
            marcar.onClick.AddListener(() =>
            {
                fio.Marcado = !fio.Marcado;
                AtualizarDiagnosticoVisual();
            });
        }

        void AlternarSelecao(FioDiagnostico fio)
        {
            if (selecionados.Contains(fio))
                selecionados.Remove(fio);
            else if (selecionados.Count < 2)
                selecionados.Add(fio);
            else
                DefinirStatus("MAXIMO DE 2 FIOS POR MEDICAO");

            AudioManager.Instance?.PlayClick();
            AtualizarDiagnosticoVisual();
        }

        void TestarParDiagnostico()
        {
            if (selecionados.Count != 2)
            {
                DefinirStatus("SELECIONE EXATAMENTE DOIS FIOS");
                AudioManager.Instance?.PlayAlert();
                return;
            }

            if (testesUsados >= 3)
            {
                DefinirStatus("SEM TESTES RESTANTES. DECIDA OS CORTES.");
                AudioManager.Instance?.PlayAlert();
                return;
            }

            testesUsados++;
            voltagemAtual = selecionados[0].Energizado == selecionados[1].Energizado ? 0f : 220f;
            feedbackTexto.text = $"{voltagemAtual:000}V";
            DefinirStatus($"{selecionados[0].Nome} <-> {selecionados[1].Nome}: {voltagemAtual:0}V  TESTES {testesUsados}/3");
            selecionados.Clear();
            AudioManager.Instance?.PlayClick();
            AtualizarDiagnosticoVisual();
        }

        void CortarMarcadosDiagnostico()
        {
            bool cortouEnergizado = false;
            bool todosNeutrosMarcados = true;
            bool marcouAlgum = false;

            foreach (FioDiagnostico fio in fiosDiagnostico)
            {
                if (fio.Marcado) marcouAlgum = true;
                if (fio.Marcado && fio.Energizado) cortouEnergizado = true;
                if (!fio.Energizado && !fio.Marcado) todosNeutrosMarcados = false;
            }

            if (!marcouAlgum)
            {
                DefinirStatus("MARQUE PELO MENOS UM FIO NEUTRO");
                AudioManager.Instance?.PlayAlert();
                return;
            }

            if (cortouEnergizado)
            {
                GameOverImediato("FIO ENERGIZADO CORTADO");
                return;
            }

            if (todosNeutrosMarcados)
            {
                AudioManager.Instance?.PlaySuccess();
                EmitirResolvido("NEUTROS ISOLADOS. PROXIMO DESAFIO LIBERADO");
                return;
            }

            TimerController.Instance?.ApplyPenalty(3f);
            AudioManager.Instance?.PlayShock();
            CameraShake.Instance?.Impulse(0.35f);
            DefinirStatus("AINDA HA NEUTROS SEM MARCAR. -3 segundos", new Color(1f, 0.45f, 0.42f));
        }

        void AtualizarDiagnosticoVisual()
        {
            foreach (FioDiagnostico fio in fiosDiagnostico)
            {
                bool selecionado = selecionados.Contains(fio);
                fio.Painel.color = fio.Marcado
                    ? new Color(0.18f, 0.08f, 0.05f, 0.97f)
                    : selecionado ? new Color(0.08f, 0.18f, 0.14f, 0.97f) : new Color(0.04f, 0.065f, 0.06f, 0.96f);
                fio.Rotulo.text = fio.Marcado ? $"{fio.Nome} *" : fio.Nome;
            }
        }

        void AtualizarPonteiroDiagnostico()
        {
            if (ponteiro == null) return;
            float alvo = Mathf.Lerp(8f, -78f, Mathf.InverseLerp(0f, 300f, voltagemAtual));
            ponteiro.rectTransform.localEulerAngles = new Vector3(0f, 0f, Mathf.LerpAngle(ponteiro.rectTransform.localEulerAngles.z, alvo, Time.unscaledDeltaTime * 8f));
        }

        void CriarCabecalho(string modo, string subtitulo)
        {
            CriarTexto(raizDinamica, "Modo", modo, 22, new Color(0.74f, 1f, 0.84f), TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -16f), new Vector2(-112f, 30f));
            cronometroTexto = CriarTexto(raizDinamica, "Cronometro", "30.0s", 22, new Color(1f, 0.86f, 0.42f), TextAnchor.MiddleRight,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-52f, -16f), new Vector2(100f, 30f));
            CriarTexto(raizDinamica, "Subtitulo", subtitulo, 15, new Color(0.62f, 0.9f, 0.76f), TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -44f), new Vector2(-18f, 24f));
            dicaTexto = CriarTexto(raizDinamica, "Dica", string.Empty, 14, new Color(1f, 0.92f, 0.48f), TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -64f), new Vector2(-18f, 42f));
        }

        Image CriarBarra(Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
        {
            Image bg = CriarPainel(raizDinamica, "BarBg", new Color(0.025f, 0.045f, 0.04f, 0.95f),
                new Vector2(0f, 1f), new Vector2(1f, 1f), anchoredPosition, sizeDelta);
            Image fill = CriarPainel(bg.transform, "BarFill", color, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;
            return fill;
        }

        Button CriarBotao(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Color color, int fontSize)
        {
            Image image = CriarPainel(parent, label, color, anchorMin, anchorMax, anchoredPosition, sizeDelta);
            Button button = TransformarEmBotao(image, color, new Color(0.18f, 0.26f, 0.22f), new Color(0.1f, 0.18f, 0.14f));
            CriarTexto(image.transform, "Label", label, fontSize, Color.white, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }

        void LimparConteudo()
        {
            if (raizDinamica == null) return;
            for (int i = raizDinamica.childCount - 1; i >= 0; i--)
            {
                GameObject child = raizDinamica.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }

        string DicaReacao(RegraReacao regra) => regra switch
        {
            RegraReacao.ComVermelho => "Corte um fio quando ele ACENDER junto com o VERMELHO.",
            RegraReacao.NaoSozinho => "Corte um fio quando ele NAO estiver piscando sozinho.",
            RegraReacao.DepoisVerdeApagar => "Corte 0.5 segundos depois que o VERDE apagar.",
            RegraReacao.ComAzul => "Corte um fio que pisca na mesma hora que o AZUL.",
            RegraReacao.MaisRapido => "Corte o fio que pisca MAIS RAPIDO entre todos.",
            RegraReacao.MaisDevagar => "Corte o fio que pisca MAIS DEVAGAR entre todos.",
            _ => "Corte no momento correto."
        };

        bool ReacaoCorreta(RegraReacao regra, FioReacao fio, int acesos)
        {
            FioReacao vermelho = fiosReacao[0];
            FioReacao azul = fiosReacao[1];
            FioReacao verde = fiosReacao[2];
            FioReacao amarelo = fiosReacao[3];

            return regra switch
            {
                RegraReacao.ComVermelho => fio != vermelho && fio.Aceso && vermelho.Aceso,
                RegraReacao.NaoSozinho => fio.Aceso && acesos > 1,
                RegraReacao.DepoisVerdeApagar => !verde.Aceso && Time.unscaledTime - verde.UltimoApagou >= 0.42f && Time.unscaledTime - verde.UltimoApagou <= 0.68f,
                RegraReacao.ComAzul => fio != azul && fio.Aceso && azul.Aceso,
                RegraReacao.MaisRapido => fio == amarelo && fio.Aceso,
                RegraReacao.MaisDevagar => fio == verde && fio.Aceso,
                _ => false
            };
        }

        float QualidadeTiming(RegraReacao regra, int acesos)
        {
            float qualidade = 0f;
            foreach (FioReacao fio in fiosReacao)
                if (ReacaoCorreta(regra, fio, acesos)) qualidade = 1f;

            if (regra == RegraReacao.DepoisVerdeApagar)
            {
                FioReacao verde = fiosReacao[2];
                if (!verde.Aceso)
                {
                    float delta = Mathf.Abs((Time.unscaledTime - verde.UltimoApagou) - 0.5f);
                    qualidade = Mathf.Max(qualidade, 1f - Mathf.Clamp01(delta / 0.5f));
                }
            }

            return qualidade;
        }

        void GameOverImediato(string motivo)
        {
            if (falhaFatal) return;
            falhaFatal = true;
            DefinirStatus(motivo, new Color(1f, 0.35f, 0.32f));
            AudioManager.Instance?.PlayShock();
            AudioManager.Instance?.PlayAlert();
            CameraShake.Instance?.Impulse(0.75f);

            Gerenciador?.Fechar(restoreControl: false);
            Vector3 bombPos = Gerenciador != null && Gerenciador.bombManager != null ? Gerenciador.bombManager.GetBombPosition() : Vector3.zero;
            Camera cam = PlayerController.Instance != null ? PlayerController.Instance.playerCamera : Camera.main;
            GameManager.Instance?.TriggerGameOver(bombPos, cam);
        }
    }
}
