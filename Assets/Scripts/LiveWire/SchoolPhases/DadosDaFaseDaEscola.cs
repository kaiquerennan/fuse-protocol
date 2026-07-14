using System;
using UnityEngine;

namespace LiveWire
{
    [CreateAssetMenu(fileName = "DadosDaFaseDaEscola", menuName = "LiveWire/Fases/Dados Da Fase Da Escola")]
    public class DadosDaFaseDaEscola : ScriptableObject
    {
        [Header("Identificacao")]
        [SerializeField] string idDaFase = "fase_01";
        [SerializeField] string nomeDaFase = "Fase 01";
        [SerializeField] string nomeDaCena = "";

        [Header("Local")]
        [SerializeField] LocalDaEscola localPrincipal = LocalDaEscola.Biblioteca;
        [SerializeField] bool filtrarPorLocalPrincipal = true;
        [SerializeField] string[] idsDeSpawnValidos = Array.Empty<string>();

        [Header("Objetivo")]
        [SerializeField] ModoDeObjetivoDaFase modoDoObjetivo = ModoDeObjetivoDaFase.Explicito;
        [SerializeField] string mensagemPersonalizada = "";

        [Header("Tempo")]
        [SerializeField] bool sobrescreverTempoDaFase;
        [SerializeField] float tempoDaFase = 60f;

        public string IdDaFase => idDaFase;
        public string NomeDaFase => nomeDaFase;
        public string NomeDaCena => nomeDaCena;
        public LocalDaEscola LocalPrincipal => localPrincipal;
        public ModoDeObjetivoDaFase ModoDoObjetivo => modoDoObjetivo;
        public string MensagemPersonalizada => mensagemPersonalizada;

        public bool UsaCenaAtual => string.IsNullOrWhiteSpace(nomeDaCena);

        public bool PermitePonto(PontoDeSpawnDaBomba ponto)
        {
            if (ponto == null || !ponto.Disponivel) return false;

            // Primeiro filtra pelo local macro da escola. Se a lista de IDs
            // estiver vazia, qualquer ponto desse local pode ser usado.
            if (filtrarPorLocalPrincipal && ponto.Local != localPrincipal)
                return false;

            if (idsDeSpawnValidos == null || idsDeSpawnValidos.Length == 0)
                return true;

            string idDoPonto = ponto.Identificador;
            if (string.IsNullOrWhiteSpace(idDoPonto))
                return false;

            for (int i = 0; i < idsDeSpawnValidos.Length; i++)
            {
                if (string.Equals(idsDeSpawnValidos[i], idDoPonto, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public float ObterTempoDaFase(int numeroDaFase)
        {
            if (sobrescreverTempoDaFase)
                return tempoDaFase;

            if (GameManager.Instance != null)
                return GameManager.Instance.GetPhaseTime(numeroDaFase);

            return tempoDaFase;
        }

        public string ConstruirMensagemInicial()
        {
            if (!string.IsNullOrWhiteSpace(mensagemPersonalizada) && modoDoObjetivo == ModoDeObjetivoDaFase.Personalizado)
                return mensagemPersonalizada;

            return modoDoObjetivo switch
            {
                ModoDeObjetivoDaFase.Explicito => $"Encontre a bomba na {FaseDaEscolaTexto.ObterNome(localPrincipal)}.",
                ModoDeObjetivoDaFase.Oculto => "Encontre a bomba antes que o tempo acabe.",
                ModoDeObjetivoDaFase.Personalizado => string.IsNullOrWhiteSpace(mensagemPersonalizada)
                    ? "Encontre a bomba antes que seja tarde."
                    : mensagemPersonalizada,
                _ => "Encontre a bomba.",
            };
        }

        public void ConfigurarRuntime(
            string novoId,
            string novoNome,
            string novaCena,
            LocalDaEscola novoLocal,
            ModoDeObjetivoDaFase novoModo,
            string novaMensagemPersonalizada = "",
            bool usarFiltroPorLocal = true,
            float? tempoSobrescrito = null,
            params string[] novosIdsDeSpawn)
        {
            idDaFase = novoId;
            nomeDaFase = novoNome;
            nomeDaCena = novaCena;
            localPrincipal = novoLocal;
            modoDoObjetivo = novoModo;
            mensagemPersonalizada = novaMensagemPersonalizada;
            filtrarPorLocalPrincipal = usarFiltroPorLocal;
            idsDeSpawnValidos = novosIdsDeSpawn ?? Array.Empty<string>();

            sobrescreverTempoDaFase = tempoSobrescrito.HasValue;
            if (tempoSobrescrito.HasValue)
                tempoDaFase = tempoSobrescrito.Value;
        }
    }
}
