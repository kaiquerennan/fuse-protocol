using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LiveWire
{
    public class GerenciadorDeBomba : MonoBehaviour
    {
        public static GerenciadorDeBomba Instance { get; private set; }

        public BombManager bombManager;
        public Canvas painelCanvas;
        public RectTransform painelRaiz;
        public Text tituloTexto;
        public Text subtituloTexto;
        public Text statusGlobalTexto;
        public Text dificuldadeTexto;
        public Text strikesTexto;
        public Text rodapeTexto;
        public Image overlayImagem;
        public bool avancoAutomaticoAoDesarmar = true;

        public int MaxStrikes => 3;
        public int Strikes { get; private set; }
        public bool IsOpen { get; private set; }
        public DificuldadeBomba DificuldadeAtual { get; private set; } = DificuldadeBomba.Facil;
        public IReadOnlyList<ModuloBomba> ModulosAtivos => modulosAtivos;

        readonly List<ModuloBomba> modulosAtivos = new();
        Coroutine rotinaConclusao;
        bool bloqueado;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;

            foreach (ModuloBomba modulo in modulosAtivos)
            {
                if (modulo == null) continue;
                modulo.AoResolver -= HandleModuloResolvido;
                modulo.AoFalhar -= HandleModuloFalhou;
                modulo.AoAtualizarStatus -= HandleStatusAtualizado;
            }
        }

        void Update()
        {
            if (!IsOpen || bloqueado || Keyboard.current == null) return;
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
                bombManager?.CancelMinigame();
        }

        public void Configurar(
            BombManager bomb,
            Canvas canvas,
            RectTransform painel,
            Text titulo,
            Text subtitulo,
            Text statusGlobal,
            Text dificuldade,
            Text strikes,
            Text rodape,
            Image overlay)
        {
            bombManager = bomb;
            painelCanvas = canvas;
            painelRaiz = painel;
            tituloTexto = titulo;
            subtituloTexto = subtitulo;
            statusGlobalTexto = statusGlobal;
            dificuldadeTexto = dificuldade;
            strikesTexto = strikes;
            rodapeTexto = rodape;
            overlayImagem = overlay;

            if (painelCanvas != null) painelCanvas.gameObject.SetActive(false);
            AtualizarCabecalho();
            AtualizarStatusGlobal("APROXIME-SE E ABRA A BOMBA");
            AtualizarTextoStrikes();
        }

        public void RegistrarModulo(ModuloBomba modulo)
        {
            if (modulo == null || modulosAtivos.Contains(modulo)) return;

            modulosAtivos.Add(modulo);
            modulo.AoResolver += HandleModuloResolvido;
            modulo.AoFalhar += HandleModuloFalhou;
            modulo.AoAtualizarStatus += HandleStatusAtualizado;
        }

        public void Abrir(int faseAtual)
        {
            if (IsOpen || bloqueado) return;

            IsOpen = true;
            rotinaConclusao = null;
            DificuldadeAtual = DeterminarDificuldade(faseAtual);

            AtualizarCanvasCamera();
            if (painelCanvas != null) painelCanvas.gameObject.SetActive(true);
            if (overlayImagem != null) overlayImagem.color = new Color(0.01f, 0.05f, 0.05f, 0.97f);

            AtualizarCabecalho();
            AtualizarTextoStrikes();
            AtualizarResumoProgresso();
            AtualizarStatusGlobal("TODOS OS MODULOS FORAM INICIALIZADOS");

            if (rodapeTexto != null) rodapeTexto.text = "ESC FECHA O PAINEL";

            PlayerController.Instance?.SetCursorUnlocked(true);
            AudioManager.Instance?.PauseHiss();
            AudioManager.Instance?.StartElectricHiss();

            foreach (ModuloBomba modulo in modulosAtivos)
            {
                if (modulo == null) continue;
                modulo.StopAllCoroutines();
                modulo.Inicializar();
                modulo.IniciarSessao();
                modulo.Interagir();
            }
        }

        void AtualizarCanvasCamera()
        {
            if (painelCanvas == null) return;

            Camera cam = PlayerController.Instance != null
                ? PlayerController.Instance.playerCamera
                : Camera.main;

            if (cam == null) return;
            if (painelCanvas.renderMode == RenderMode.WorldSpace || painelCanvas.renderMode == RenderMode.ScreenSpaceCamera)
                painelCanvas.worldCamera = cam;
        }

        public void Fechar(bool restoreControl = true, bool resumeBombLoop = false)
        {
            if (!IsOpen && painelCanvas != null && !painelCanvas.gameObject.activeSelf) return;

            foreach (ModuloBomba modulo in modulosAtivos)
            {
                if (modulo == null) continue;
                modulo.StopAllCoroutines();
                modulo.EncerrarSessao();
            }

            IsOpen = false;

            if (painelCanvas != null) painelCanvas.gameObject.SetActive(false);

            AudioManager.Instance?.StopElectricHiss();
            AudioManager.Instance?.StopTensionRising();
            AudioManager.Instance?.ResetRunState();

            if (resumeBombLoop && TimerController.Instance != null && TimerController.Instance.Running)
                AudioManager.Instance?.ResumeHiss();

            PlayerController.Instance?.SetCursorUnlocked(false);

            if (restoreControl && PlayerController.Instance != null)
                PlayerController.Instance.SetInputLocked(false);
        }

        public void DefinirAvancoAutomatico(bool habilitado)
        {
            avancoAutomaticoAoDesarmar = habilitado;
        }

        public void MostrarFalhaCritica()
        {
            if (statusGlobalTexto != null) statusGlobalTexto.text = "CRITICAL FAILURE";
            if (subtituloTexto != null) subtituloTexto.text = "SIGNAL LOST";
            if (rodapeTexto != null) rodapeTexto.text = "CORE DETONATION";
            if (overlayImagem != null) overlayImagem.color = new Color(0.16f, 0f, 0f, 0.98f);

            foreach (ModuloBomba modulo in modulosAtivos)
            {
                if (modulo is ModuloSincronizadorFrequencia sincronizador)
                    sincronizador.MostrarFalhaCritica();
            }
        }

        public void PrepararNovaTentativa()
        {
            if (rotinaConclusao != null)
            {
                StopCoroutine(rotinaConclusao);
                rotinaConclusao = null;
            }

            bloqueado = false;
            IsOpen = false;
            Strikes = 0;
            AtualizarTextoStrikes();

            foreach (ModuloBomba modulo in modulosAtivos)
            {
                if (modulo == null) continue;
                modulo.StopAllCoroutines();
                modulo.Resetar();
            }
        }

        DificuldadeBomba DeterminarDificuldade(int faseAtual)
        {
            if (GameManager.Instance != null)
                return GameManager.Instance.GetBombDifficulty(faseAtual);

            return faseAtual >= 4 ? DificuldadeBomba.Dificil : DificuldadeBomba.Facil;
        }

        void HandleStatusAtualizado(ModuloBomba modulo, string status)
        {
            if (!IsOpen || bloqueado || modulo == null) return;
            AtualizarStatusGlobal($"{modulo.NomeModulo}: {status}");
        }

        void HandleModuloFalhou(ModuloBomba modulo, string motivo)
        {
            if (!IsOpen || bloqueado || rotinaConclusao != null) return;

            // O gerenciador concentra toda a contagem de erros para manter
            // os modulos desacoplados da regra global de derrota.
            Strikes++;
            AtualizarTextoStrikes();
            AtualizarStatusGlobal($"{modulo.NomeModulo}: {motivo}");

            AudioManager.Instance?.PlayAlert();
            AudioManager.Instance?.PlayShock();
            CameraShake.Instance?.Impulse(0.18f);

            if (Strikes >= MaxStrikes)
            {
                rotinaConclusao = StartCoroutine(RotinaExplosaoPorStrikes());
                return;
            }

            modulo?.Resetar();
            modulo?.Interagir();
        }

        void HandleModuloResolvido(ModuloBomba modulo)
        {
            if (!IsOpen || bloqueado || rotinaConclusao != null) return;

            AudioManager.Instance?.PlaySuccess();
            AudioManager.Instance?.PlayConnect();
            AtualizarResumoProgresso();
            AtualizarStatusGlobal($"{modulo.NomeModulo}: RESOLVIDO");

            if (TodosOsModulosResolvidos())
                rotinaConclusao = StartCoroutine(RotinaBombDesarmada());
        }

        bool TodosOsModulosResolvidos()
        {
            if (modulosAtivos.Count == 0) return false;

            foreach (ModuloBomba modulo in modulosAtivos)
            {
                if (modulo == null || !modulo.Resolvido)
                    return false;
            }

            return true;
        }

        IEnumerator RotinaBombDesarmada()
        {
            bloqueado = true;
            float remaining = TimerController.Instance != null ? TimerController.Instance.Remaining : 0f;

            AtualizarStatusGlobal("TODOS OS MODULOS FORAM RESOLVIDOS");
            if (subtituloTexto != null) subtituloTexto.text = "BOMBA ESTAVEL";
            if (rodapeTexto != null) rodapeTexto.text = string.Empty;

            TimerController.Instance?.Stop();
            AudioManager.Instance?.StopTicking();
            AudioManager.Instance?.StopHiss();
            AudioManager.Instance?.StopElectricHiss();

            yield return new WaitForSecondsRealtime(0.8f);

            AudioManager.Instance?.PlaySuccess();
            AudioManager.Instance?.PlayRelief();

            Fechar(restoreControl: false);
            bombManager?.OnDefused();

            HudController hud = FindAnyObjectByType<HudController>();
            if (hud != null) hud.ShowPhaseComplete(remaining);

            if (!avancoAutomaticoAoDesarmar)
            {
                rotinaConclusao = null;
                yield break;
            }

            yield return new WaitForSecondsRealtime(2f);

            GameManager.Instance?.AdvancePhase(remaining);
        }

        IEnumerator RotinaExplosaoPorStrikes()
        {
            bloqueado = true;

            AtualizarTextoStrikes();
            AtualizarStatusGlobal("3 ERROS REGISTRADOS");
            if (subtituloTexto != null) subtituloTexto.text = "DETONACAO IMINENTE";
            if (rodapeTexto != null) rodapeTexto.text = string.Empty;
            if (overlayImagem != null) overlayImagem.color = new Color(0.08f, 0.01f, 0.01f, 0.97f);

            TimerController.Instance?.Stop();
            AudioManager.Instance?.StopElectricHiss();
            AudioManager.Instance?.StopHiss();
            AudioManager.Instance?.StopTicking();
            AudioManager.Instance?.PlayAlert();

            yield return new WaitForSecondsRealtime(0.75f);

            Fechar(restoreControl: false);

            Vector3 bombPos = bombManager != null ? bombManager.GetBombPosition() : Vector3.zero;
            Camera cam = PlayerController.Instance != null ? PlayerController.Instance.playerCamera : Camera.main;
            GameManager.Instance?.TriggerGameOver(bombPos, cam);
        }

        void AtualizarCabecalho()
        {
            if (tituloTexto != null) tituloTexto.text = "NUCLEO INTERNO";
            if (subtituloTexto != null)
                subtituloTexto.text = $"RESOLVA {modulosAtivos.Count:00} MODULOS ANTES DE {MaxStrikes} ERROS";
            if (dificuldadeTexto != null)
                dificuldadeTexto.text = $"DIFICULDADE  {BombDefinitions.ObterNome(DificuldadeAtual)}";
        }

        void AtualizarResumoProgresso()
        {
            if (subtituloTexto == null) return;

            int resolvidos = 0;
            foreach (ModuloBomba modulo in modulosAtivos)
            {
                if (modulo != null && modulo.Resolvido) resolvidos++;
            }

            subtituloTexto.text = $"MODULOS RESOLVIDOS  {resolvidos}/{modulosAtivos.Count}";
        }

        void AtualizarStatusGlobal(string texto)
        {
            if (statusGlobalTexto != null) statusGlobalTexto.text = texto;
        }

        void AtualizarTextoStrikes()
        {
            if (strikesTexto == null) return;

            strikesTexto.text = $"ERROS  {Strikes}/{MaxStrikes}";
            strikesTexto.color = Strikes switch
            {
                0 => new Color(0.78f, 0.94f, 0.84f),
                1 => new Color(1f, 0.86f, 0.48f),
                2 => new Color(1f, 0.58f, 0.34f),
                _ => new Color(1f, 0.32f, 0.32f),
            };
        }
    }
}
