using UnityEngine;
using UnityEngine.UI;

namespace LiveWire
{
    // Caminho 3D do bootstrap. Quando uma BombaFisicaBuilder existe na cena
    // (arrastada pelo level designer), substituimos a montagem procedural por
    // prefabs reais e ligamos o raycaster ao Player Camera.
    public partial class GameSceneBootstrap
    {
        BombaFisicaBuilder fisica3DCache;

        BombaFisicaBuilder LocalizarBuilder3D()
        {
            if (fisica3DCache != null) return fisica3DCache;
            fisica3DCache = FindAnyObjectByType<BombaFisicaBuilder>();
            return fisica3DCache;
        }

        BombManager BuildBomb3D(BombaFisicaBuilder builder)
        {
            GameObject bombRoot = builder.gameObject;
            bombRoot.tag = "Respawn";

            builder.Construir(bombRoot.transform);

            Transform foco = builder.AncoraZoom != null
                ? builder.AncoraZoom
                : (builder.Painel != null ? builder.Painel : bombRoot.transform);

            // Collider de interacao "olhe pra mim e aperte E". Mantemos um
            // raio compativel com o procedural pra reusar o IsPlayer/Trigger.
            bool temColliderDeMira = false;
            SphereCollider[] sphereColliders = bombRoot.GetComponents<SphereCollider>();
            for (int i = 0; i < sphereColliders.Length; i++)
            {
                if (!sphereColliders[i].isTrigger)
                {
                    temColliderDeMira = true;
                    break;
                }
            }

            if (!temColliderDeMira)
            {
                SphereCollider interact = bombRoot.AddComponent<SphereCollider>();
                interact.isTrigger = false;
                interact.radius = 0.6f;
            }

            SphereCollider proximity = bombRoot.AddComponent<SphereCollider>();
            proximity.isTrigger = true;
            proximity.radius = BombManager.ProximityRadius;

            Rigidbody rb = bombRoot.GetComponent<Rigidbody>();
            if (rb == null) rb = bombRoot.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            BombManager mgr = bombRoot.GetComponent<BombManager>();
            if (mgr == null) mgr = bombRoot.AddComponent<BombManager>();
            mgr.bombRoot = bombRoot.transform;
            mgr.bombVisual = builder.Chassis != null ? builder.Chassis : bombRoot.transform;
            mgr.bombLight = builder.LedPrincipal;
            mgr.cameraZoomTarget = foco;
            mgr.cameraLookTarget = builder.Chassis != null ? builder.Chassis : bombRoot.transform;

            return mgr;
        }

        GerenciadorDeBomba BuildBombGameplay3D(BombManager bomb, BombaFisicaBuilder builder)
        {
            Canvas painelCanvas = CriarCanvasBombMundo(builder);
            RectTransform frame = CriarPainelBombMundo(painelCanvas.transform);
            Text statusGlobal = CriarTextoStatusMundo(frame);
            Text strikes = CriarTextoStrikesMundo(frame);
            Text subtitulo = CriarTextoSubtituloMundo(frame);
            Text rodape = CriarTextoRodapeMundo(frame);

            RectTransform sincronizadorSlot = CriarSlotModuloMundo(frame, "SincronizadorSlot", Vector2.zero);

            GameObject gerenciadorGO = new GameObject("GerenciadorDeBomba");
            gerenciadorGO.transform.SetParent(builder.transform, false);

            GerenciadorDeBomba gerenciador = gerenciadorGO.AddComponent<GerenciadorDeBomba>();
            gerenciador.Configurar(bomb, painelCanvas, frame, null, subtitulo, statusGlobal, null, strikes, rodape, null);

            ModuloSincronizadorFrequencia sincronizador = new GameObject("ModuloSincronizadorFrequencia").AddComponent<ModuloSincronizadorFrequencia>();
            sincronizador.transform.SetParent(gerenciadorGO.transform, false);
            sincronizador.Configurar(gerenciador, sincronizadorSlot, "SINCRONIZADOR DE ONDAS");

            return gerenciador;
        }

        Canvas CriarCanvasBombMundo(BombaFisicaBuilder builder)
        {
            GameObject go = new GameObject("BombaPainelMundo", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;

            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(1920f, 1080f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            return canvas;
        }

        RectTransform CriarPainelBombMundo(Transform parent)
        {
            Image frame = BombUiFactory.CreatePanel(
                parent,
                "FrameIndustrialMundo",
                new Color(0.08f, 0.085f, 0.075f, 0.98f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(1180f, 760f));
            frame.raycastTarget = false;
            RetroElectroUi.TryApplySprite(frame, RetroElectroUi.BrushedMetal, Image.Type.Tiled);
            RectTransform rt = frame.rectTransform;
            rt.pivot = new Vector2(0.5f, 0.5f);

            BombUiFactory.CreatePanel(
                frame.transform,
                "LinhaAlertaSuperior",
                new Color(1f, 0.13f, 0.08f, 0.72f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, -10f),
                new Vector2(-32f, 8f),
                new Vector2(0.5f, 1f)).raycastTarget = false;

            return rt;
        }

        Text CriarTextoStatusMundo(Transform parent)
        {
            return BombUiFactory.CreateText(
                parent,
                "StatusGlobalMundo",
                string.Empty,
                20,
                new Color(1f, 0.88f, 0.42f),
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 34f),
                new Vector2(860f, 30f));
        }

        Text CriarTextoSubtituloMundo(Transform parent)
        {
            return BombUiFactory.CreateText(
                parent,
                "SubtituloMundo",
                string.Empty,
                18,
                new Color(0.68f, 0.96f, 0.78f),
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -34f),
                new Vector2(820f, 30f));
        }

        Text CriarTextoStrikesMundo(Transform parent)
        {
            return BombUiFactory.CreateText(
                parent,
                "StrikesMundo",
                string.Empty,
                18,
                new Color(1f, 0.72f, 0.42f),
                TextAnchor.MiddleRight,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-34f, -34f),
                new Vector2(180f, 30f),
                new Vector2(1f, 1f));
        }

        Text CriarTextoRodapeMundo(Transform parent)
        {
            return BombUiFactory.CreateText(
                parent,
                "RodapeMundo",
                "ESC FECHA O PAINEL",
                15,
                new Color(0.65f, 0.86f, 0.76f),
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 6f),
                new Vector2(360f, 22f));
        }

        RectTransform CriarSlotModuloMundo(Transform parent, string nome, Vector2 anchoredPosition)
        {
            Image slot = BombUiFactory.CreatePanel(
                parent,
                nome,
                new Color(0.035f, 0.045f, 0.04f, 0.92f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                anchoredPosition,
                new Vector2(1100f, 640f));
            return slot.rectTransform;
        }

        void WireRaycaster3D(Camera playerCamera)
        {
            if (playerCamera == null) return;
            if (playerCamera.GetComponent<BombaInteractionRaycaster>() != null) return;

            BombaInteractionRaycaster ray = playerCamera.gameObject.AddComponent<BombaInteractionRaycaster>();
            ray.focusCamera = playerCamera;
            ray.maxDistance = 5f;
            ray.hitMask = ~0;
        }
    }
}
