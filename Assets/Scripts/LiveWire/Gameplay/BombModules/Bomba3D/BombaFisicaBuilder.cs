using UnityEngine;

namespace LiveWire
{
    // Monta a bomba 3D a partir de prefabs arrastados no Inspector. Substitui a
    // geracao procedural feita em GameSceneBootstrap.BuildBomb. Pode ser usado
    // como prefab solto na scene ou instanciado em runtime pelo bootstrap.
    public class BombaFisicaBuilder : MonoBehaviour
    {
        [Header("Prefabs visuais (arraste no Inspector)")]
        [Tooltip("Chassis da bomba (ex: Stylized Explosives/Bomb1.prefab)")]
        [SerializeField] GameObject chassisPrefab;
        [Tooltip("Painel sci-fi 'aberto' (ex: Dzeruza/MinimalScifiPack/...)")]
        [SerializeField] GameObject painelPrefab;
        [Tooltip("Vista 3D pra fios (com componente VistaFios3D)")]
        [SerializeField] GameObject vistaFiosPrefab;
        [Tooltip("Vista 3D pro botao (com componente VistaBotao3D)")]
        [SerializeField] GameObject vistaBotaoPrefab;
        [Tooltip("Vista 3D pra memoria (com componente VistaMemoria3D)")]
        [SerializeField] GameObject vistaMemoriaPrefab;

        [Header("Posicionamento do chassis")]
        [SerializeField] Vector3 chassisOffset = Vector3.zero;
        [SerializeField] Vector3 chassisRotation = Vector3.zero;
        [SerializeField] Vector3 chassisScale = Vector3.one;

        [Header("Posicionamento do painel")]
        [SerializeField] Vector3 painelOffset = new Vector3(0f, 0.6f, 0.4f);
        [SerializeField] Vector3 painelRotation = Vector3.zero;
        [SerializeField] Vector3 painelScale = Vector3.one;

        [Header("Slots de modulo (relativo ao painel)")]
        [SerializeField] Vector3 slotFiosOffset = new Vector3(-0.35f, 0.05f, 0f);
        [SerializeField] Vector3 slotBotaoOffset = new Vector3(0f, 0.05f, 0f);
        [SerializeField] Vector3 slotMemoriaOffset = new Vector3(0.35f, 0.05f, 0f);

        [Header("Camera e foco")]
        [Tooltip("Distancia da camera quando o jogador entra em foco. Mantem o painel preenchendo o frame.")]
        [SerializeField] Vector3 zoomOffset = new Vector3(0f, 0.28f, -1.15f);

        [Header("Iluminacao")]
        [SerializeField] Color corLedAlerta = new Color(1f, 0.18f, 0.12f);
        [SerializeField] float ledIntensidade = 2.4f;
        [SerializeField] float ledRange = 5f;

        public Transform Raiz { get; private set; }
        public Transform Chassis { get; private set; }
        public Transform Painel { get; private set; }
        public Transform SlotFios { get; private set; }
        public Transform SlotBotao { get; private set; }
        public Transform SlotMemoria { get; private set; }
        public Transform AncoraStatusGlobal { get; private set; }
        public Transform AncoraZoom { get; private set; }
        public Light LedPrincipal { get; private set; }

        public Color CorLedAlerta => corLedAlerta;

        // Constroi a parte fisica e devolve referencias pros slots. Deve ser
        // chamado uma unica vez por instancia de bomba.
        public void Construir(Transform raiz)
        {
            Raiz = raiz != null ? raiz : transform;

            if (chassisPrefab != null)
            {
                GameObject chassisGO = Instantiate(chassisPrefab, Raiz);
                chassisGO.name = "Chassis";
                chassisGO.transform.localPosition = chassisOffset;
                chassisGO.transform.localRotation = Quaternion.Euler(chassisRotation);
                chassisGO.transform.localScale = chassisScale;
                Chassis = chassisGO.transform;
            }

            if (painelPrefab != null)
            {
                GameObject painelGO = Instantiate(painelPrefab, Raiz);
                painelGO.name = "Painel";
                painelGO.transform.localPosition = painelOffset;
                painelGO.transform.localRotation = Quaternion.Euler(painelRotation);
                painelGO.transform.localScale = painelScale;
                Painel = painelGO.transform;
            }
            else
            {
                Painel = CriarPainelAnchor(Raiz);
            }

            Transform pai = Painel != null ? Painel : (Chassis != null ? Chassis : Raiz);

            SlotFios = CriarSlot(pai, "SlotFios", slotFiosOffset);
            SlotBotao = CriarSlot(pai, "SlotBotao", slotBotaoOffset);
            SlotMemoria = CriarSlot(pai, "SlotMemoria", slotMemoriaOffset);
            AncoraStatusGlobal = CriarSlot(pai, "AncoraStatus", new Vector3(0f, 0.45f, 0f));
            AncoraZoom = CriarSlot(pai, "AncoraZoom", zoomOffset);

            GameObject ledGO = new GameObject("LedPrincipal");
            ledGO.transform.SetParent(pai, false);
            ledGO.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            LedPrincipal = ledGO.AddComponent<Light>();
            LedPrincipal.type = LightType.Point;
            LedPrincipal.color = corLedAlerta;
            LedPrincipal.intensity = ledIntensidade;
            LedPrincipal.range = ledRange;
        }

        public VistaModulo3D InstanciarVistaFios(ModuloFios modulo) =>
            InstanciarVista(vistaFiosPrefab, SlotFios, modulo);

        public VistaModulo3D InstanciarVistaBotao(ModuloBotao modulo) =>
            InstanciarVista(vistaBotaoPrefab, SlotBotao, modulo);

        public VistaModulo3D InstanciarVistaMemoria(ModuloMemoria modulo) =>
            InstanciarVista(vistaMemoriaPrefab, SlotMemoria, modulo);

        VistaModulo3D InstanciarVista(GameObject prefab, Transform slot, ModuloBomba modulo)
        {
            if (prefab == null || slot == null || modulo == null) return null;

            GameObject go = Instantiate(prefab, slot);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;

            VistaModulo3D vista = go.GetComponent<VistaModulo3D>();
            if (vista != null) vista.Vincular(modulo);
            return vista;
        }

        Transform CriarSlot(Transform pai, string nome, Vector3 localPos)
        {
            GameObject go = new GameObject(nome);
            go.transform.SetParent(pai, false);
            go.transform.localPosition = localPos;
            return go.transform;
        }

        Transform CriarPainelAnchor(Transform raiz)
        {
            GameObject painelRoot = new GameObject("PainelAnchor");
            painelRoot.transform.SetParent(raiz, false);
            painelRoot.transform.localPosition = painelOffset;
            painelRoot.transform.localRotation = Quaternion.Euler(painelRotation);
            painelRoot.transform.localScale = painelScale;

            return painelRoot.transform;
        }
    }
}
