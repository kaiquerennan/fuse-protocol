using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LiveWire
{
    public class PontoDeSpawnDaBomba : MonoBehaviour
    {
        [SerializeField] string identificador = "spawn_01";
        [SerializeField] LocalDaEscola local = LocalDaEscola.Biblioteca;
        [SerializeField] Transform ancoraDeSpawn;
        [SerializeField] bool disponivel = true;
        [Tooltip("Quando ligado, a bomba aparece exatamente nesta posição e ignora o auto-snap em superfície. Use para ancorar a bomba manualmente sobre uma mesa.")]
        [SerializeField] bool posicaoManual = false;
        [SerializeField] Color gizmoColor = new(1f, 0.35f, 0.15f, 1f);

        public string Identificador => identificador;
        public LocalDaEscola Local => local;
        public bool Disponivel => disponivel;
        public bool PosicaoManual => posicaoManual;
        public Transform TransformDeSpawn => ancoraDeSpawn != null ? ancoraDeSpawn : transform;

        public void ConfigurarRuntime(
            string novoIdentificador,
            LocalDaEscola novoLocal,
            Transform novaAncora = null,
            bool novoDisponivel = true,
            Color? novaCorDeGizmo = null)
        {
            identificador = novoIdentificador;
            local = novoLocal;
            ancoraDeSpawn = novaAncora;
            disponivel = novoDisponivel;

            if (novaCorDeGizmo.HasValue)
                gizmoColor = novaCorDeGizmo.Value;
        }

        void OnDrawGizmos()
        {
            Transform ponto = TransformDeSpawn;
            if (ponto == null) return;

            Gizmos.color = disponivel ? gizmoColor : new Color(0.4f, 0.4f, 0.4f, 0.7f);
            Gizmos.DrawWireSphere(ponto.position, 0.35f);
            Gizmos.DrawLine(ponto.position, ponto.position + ponto.forward * 0.8f);

#if UNITY_EDITOR
            Handles.Label(ponto.position + Vector3.up * 0.45f, $"{identificador}\n{FaseDaEscolaTexto.ObterNomeEmCaixaAlta(local)}");
#endif
        }
    }
}
