using System.Collections.Generic;
using UnityEngine;

namespace LiveWire
{
    public class IntegracaoBombaFase : MonoBehaviour
    {
        [SerializeField] BombManager bombManager;
        [SerializeField] bool localizarBombManagerAutomaticamente = true;
        [SerializeField] bool incluirPontosInativos = true;
        [SerializeField] bool desativarAvancoAutomaticoDaBomba = true;
        [SerializeField] bool aguardarAnimacaoDeDesarme = true;

        GerenciadorDeFase gerenciadorDeFase;
        DadosDaFaseDaEscola faseAtual;
        PontoDeSpawnDaBomba pontoAtual;

        public BombManager BombManager => bombManager;
        public PontoDeSpawnDaBomba PontoAtual => pontoAtual;

        public void Configurar(BombManager bomba, bool localizarAutomaticamente = false)
        {
            bombManager = bomba;
            localizarBombManagerAutomaticamente = localizarAutomaticamente;
        }

        public bool PrepararFase(GerenciadorDeFase gerenciador, DadosDaFaseDaEscola fase, out PontoDeSpawnDaBomba pontoSelecionado)
        {
            pontoSelecionado = null;
            gerenciadorDeFase = gerenciador;
            faseAtual = fase;

            if (!GarantirBombManager())
                return false;

            if (desativarAvancoAutomaticoDaBomba && bombManager.gerenciadorDeBomba != null)
                bombManager.gerenciadorDeBomba.DefinirAvancoAutomatico(false);

            List<PontoDeSpawnDaBomba> pontosValidos = EncontrarPontosValidos(fase);
            if (pontosValidos.Count == 0)
            {
                Debug.LogError($"Nenhum ponto de spawn valido foi encontrado para a fase '{fase?.NomeDaFase}'.", this);
                return false;
            }

            pontoAtual = pontosValidos[Random.Range(0, pontosValidos.Count)];
            pontoSelecionado = pontoAtual;

            ConfigurarBombaNoPonto(pontoAtual);
            InscreverEventosDaBomba();
            return true;
        }

        void ConfigurarBombaNoPonto(PontoDeSpawnDaBomba ponto)
        {
            Transform ancora = ponto != null ? ponto.TransformDeSpawn : null;
            if (bombManager == null || ancora == null) return;

            bombManager.RearmarNoPonto(ancora, ponto != null && ponto.PosicaoManual);
        }

        List<PontoDeSpawnDaBomba> EncontrarPontosValidos(DadosDaFaseDaEscola fase)
        {
            PontoDeSpawnDaBomba[] pontos = incluirPontosInativos
                ? FindObjectsByType<PontoDeSpawnDaBomba>(FindObjectsInactive.Include)
                : FindObjectsByType<PontoDeSpawnDaBomba>(FindObjectsInactive.Exclude);

            // A fase escolhe sempre entre pontos previamente configurados
            // no cenario, nunca por coordenada aleatoria solta.
            List<PontoDeSpawnDaBomba> validos = new();
            for (int i = 0; i < pontos.Length; i++)
            {
                if (fase == null || fase.PermitePonto(pontos[i]))
                    validos.Add(pontos[i]);
            }

            return validos;
        }

        void InscreverEventosDaBomba()
        {
            if (bombManager == null) return;

            bombManager.AoBombaDesarmada -= HandleBombDesarmada;
            bombManager.AoSequenciaDeDesarmeConcluida -= HandleAnimacaoDeDesarmeConcluida;

            if (aguardarAnimacaoDeDesarme)
                bombManager.AoSequenciaDeDesarmeConcluida += HandleAnimacaoDeDesarmeConcluida;
            else
                bombManager.AoBombaDesarmada += HandleBombDesarmada;
        }

        void HandleBombDesarmada(BombManager bomba)
        {
            gerenciadorDeFase?.NotificarBombaDesarmada(this, faseAtual, pontoAtual);
        }

        void HandleAnimacaoDeDesarmeConcluida(BombManager bomba)
        {
            gerenciadorDeFase?.NotificarBombaDesarmada(this, faseAtual, pontoAtual);
        }

        bool GarantirBombManager()
        {
            if (bombManager != null) return true;

            if (!localizarBombManagerAutomaticamente)
                return false;

            bombManager = FindAnyObjectByType<BombManager>();
            if (bombManager == null)
            {
                Debug.LogError("IntegracaoBombaFase nao encontrou um BombManager na cena.", this);
                return false;
            }

            return true;
        }

        void OnDestroy()
        {
            if (bombManager == null) return;
            bombManager.AoBombaDesarmada -= HandleBombDesarmada;
            bombManager.AoSequenciaDeDesarmeConcluida -= HandleAnimacaoDeDesarmeConcluida;
        }
    }
}
