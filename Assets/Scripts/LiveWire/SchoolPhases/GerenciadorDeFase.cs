using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LiveWire
{
    public class GerenciadorDeFase : MonoBehaviour
    {
        public static GerenciadorDeFase Instance { get; private set; }

        [Header("Fases")]
        [SerializeField] List<DadosDaFaseDaEscola> fases = new();
        [SerializeField] bool iniciarAutomaticamente = true;
        [SerializeField] bool persistirEntreCenas = true;

        [Header("Progressao")]
        [SerializeField] float atrasoAntesDaProximaFase = 1.25f;
        [SerializeField] string cenaAoFinalizarCampanha = "";
        [SerializeField] bool iniciarTimerAutomaticamente = true;

        [Header("Dependencias Opcionais")]
        [SerializeField] GerenciadorDeObjetivo gerenciadorDeObjetivo;

        int indiceDaFaseAtual = -1;
        bool campanhaAtiva;
        Coroutine rotinaDeAvanco;
        IntegracaoBombaFase integracaoAtual;

        public event Action<DadosDaFaseDaEscola, int, PontoDeSpawnDaBomba> AoIniciarFase;
        public event Action<DadosDaFaseDaEscola, int> AoConcluirFase;
        public event Action AoConcluirCampanha;

        public int NumeroDaFaseAtual => indiceDaFaseAtual + 1;
        public DadosDaFaseDaEscola FaseAtual => indiceDaFaseAtual >= 0 && indiceDaFaseAtual < fases.Count ? fases[indiceDaFaseAtual] : null;
        public bool CampanhaAtiva => campanhaAtiva;
        public bool TemFasesConfiguradas => fases != null && fases.Count > 0;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (persistirEntreCenas)
                DontDestroyOnLoad(gameObject);
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        void Start()
        {
            if (iniciarAutomaticamente)
                IniciarCampanha();
        }

        public void IniciarCampanha()
        {
            if (fases.Count == 0)
            {
                Debug.LogWarning("GerenciadorDeFase nao possui fases cadastradas.", this);
                return;
            }

            campanhaAtiva = true;
            CarregarFase(0);
        }

        public void ConfigurarCampanhaEmRuntime(
            List<DadosDaFaseDaEscola> novasFases,
            GerenciadorDeObjetivo objetivo = null,
            bool iniciarAgora = false)
        {
            fases = novasFases ?? new List<DadosDaFaseDaEscola>();
            gerenciadorDeObjetivo = objetivo != null ? objetivo : gerenciadorDeObjetivo;
            iniciarAutomaticamente = false;

            if (iniciarAgora)
                IniciarCampanha();
        }

        public void ReiniciarCampanha()
        {
            if (rotinaDeAvanco != null)
            {
                StopCoroutine(rotinaDeAvanco);
                rotinaDeAvanco = null;
            }

            campanhaAtiva = true;
            CarregarFase(0);
        }

        public void CarregarFase(int indiceDaFase)
        {
            if (indiceDaFase < 0 || indiceDaFase >= fases.Count)
            {
                FinalizarCampanha();
                return;
            }

            indiceDaFaseAtual = indiceDaFase;

            if (GameManager.Instance != null)
                GameManager.Instance.SetCurrentPhase(NumeroDaFaseAtual);

            DadosDaFaseDaEscola fase = FaseAtual;
            if (fase == null)
            {
                Debug.LogError("A fase atual nao foi encontrada.", this);
                return;
            }

            Scene cenaAtual = SceneManager.GetActiveScene();
            if (!fase.UsaCenaAtual && !string.Equals(cenaAtual.name, fase.NomeDaCena, StringComparison.Ordinal))
            {
                SceneManager.LoadScene(fase.NomeDaCena);
                return;
            }

            PrepararFaseNaCenaAtual();
        }

        public void AvancarParaProximaFase()
        {
            if (!campanhaAtiva || rotinaDeAvanco != null)
                return;

            rotinaDeAvanco = StartCoroutine(RotinaAvancarParaProximaFase());
        }

        internal void NotificarBombaDesarmada(IntegracaoBombaFase origem, DadosDaFaseDaEscola fase, PontoDeSpawnDaBomba ponto)
        {
            if (!campanhaAtiva || origem == null || origem != integracaoAtual)
                return;

            if (GameManager.Instance != null && TimerController.Instance != null)
                GameManager.Instance.SetLastRemainingTime(TimerController.Instance.Remaining);

            gerenciadorDeObjetivo?.MarcarObjetivoConcluido();
            AoConcluirFase?.Invoke(fase, NumeroDaFaseAtual);
            AvancarParaProximaFase();
        }

        void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!campanhaAtiva || FaseAtual == null)
                return;

            if (!FaseAtual.UsaCenaAtual && !string.Equals(scene.name, FaseAtual.NomeDaCena, StringComparison.Ordinal))
                return;

            PrepararFaseNaCenaAtual();
        }

        void PrepararFaseNaCenaAtual()
        {
            if (!campanhaAtiva || FaseAtual == null)
                return;

            // A integracao faz a ponte entre o fluxo de fases e a bomba
            // concreta presente na cena atual.
            if (gerenciadorDeObjetivo == null)
                gerenciadorDeObjetivo = FindAnyObjectByType<GerenciadorDeObjetivo>();

            integracaoAtual = FindAnyObjectByType<IntegracaoBombaFase>();
            if (integracaoAtual == null)
            {
                Debug.LogError("GerenciadorDeFase nao encontrou uma IntegracaoBombaFase na cena.", this);
                return;
            }

            if (!integracaoAtual.PrepararFase(this, FaseAtual, out PontoDeSpawnDaBomba pontoSelecionado))
                return;

            AudioManager.Instance?.ResetRunState();

            if (iniciarTimerAutomaticamente && TimerController.Instance != null)
            {
                TimerController.Instance.Begin(FaseAtual.ObterTempoDaFase(NumeroDaFaseAtual));
                AudioManager.Instance?.StartHiss();
                AudioManager.Instance?.SetHissIntensity(0f);
            }

            gerenciadorDeObjetivo?.ExibirInicioDeFase(FaseAtual, NumeroDaFaseAtual);
            gerenciadorDeObjetivo?.AtualizarObjetivoAtivo(FaseAtual);

            AoIniciarFase?.Invoke(FaseAtual, NumeroDaFaseAtual, pontoSelecionado);
        }

        IEnumerator RotinaAvancarParaProximaFase()
        {
            if (atrasoAntesDaProximaFase > 0f)
                yield return new WaitForSecondsRealtime(atrasoAntesDaProximaFase);

            rotinaDeAvanco = null;
            CarregarFase(indiceDaFaseAtual + 1);
        }

        void FinalizarCampanha()
        {
            campanhaAtiva = false;
            indiceDaFaseAtual = fases.Count;

            gerenciadorDeObjetivo?.MarcarCampanhaConcluida();
            AoConcluirCampanha?.Invoke();

            if (!string.IsNullOrWhiteSpace(cenaAoFinalizarCampanha))
                SceneManager.LoadScene(cenaAoFinalizarCampanha);
        }
    }
}
