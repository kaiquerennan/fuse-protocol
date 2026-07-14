using UnityEngine;

namespace LiveWire
{
    public class EscolaPrefabricada : MonoBehaviour
    {
        [Header("Spawn do Jogador")]
        [SerializeField] Transform pontoDeSpawnDoJogador;

        [Header("Iluminacao")]
        [Tooltip("Se marcado, o bootstrap nao toca nas luzes/ambient. Deixe ligado quando a cena ja tem iluminacao propria.")]
        [SerializeField] bool usarIluminacaoDaCena = true;

        public Transform PontoDeSpawnDoJogador => pontoDeSpawnDoJogador;
        public bool UsarIluminacaoDaCena => usarIluminacaoDaCena;
    }
}
