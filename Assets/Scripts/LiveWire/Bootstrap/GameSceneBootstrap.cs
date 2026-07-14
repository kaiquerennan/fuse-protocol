using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LiveWire
{
    public partial class GameSceneBootstrap : MonoBehaviour
    {
        const float RoomHalfWidth = 28f;
        const float RoomHalfDepth = 30f;
        const float RoomHeight = 4.2f;

        GameManager gameManager;
        GerenciadorDeObjetivo gerenciadorDeObjetivoEscola;
        EscolaPrefabricada escolaPrefabricada;
        bool processandoTimeout;

        void Awake()
        {
            EnsureGlobals();
            escolaPrefabricada = FindAnyObjectByType<EscolaPrefabricada>();
            if (escolaPrefabricada == null)
                BuildSchool();
            SchoolAssetRuntimeFixer.FixScene();
            BombManager bomb = BuildBomb();
            PlayerController player = BuildPlayer();
            // Modo 3D: liga raycaster do mouse na camera do jogador. No modo
            // procedural (sem builder) o painel 2D continua usando GraphicRaycaster.
            if (LocalizarBuilder3D() != null)
                WireRaycaster3D(player.playerCamera);
            BuildHudAndMinigame(player.playerCamera, bomb);
            new GameObject("MenuDePausa").AddComponent<MenuDePausa>();
            EnsureSchoolPhaseRuntime(bomb);
            SchoolNavigationOptimizer.EnsureRuntimeHooks(player.transform, bomb);
            StartPhase(player);
        }

        void EnsureGlobals()
        {
            if (GameManager.Instance == null)
            {
                GameObject gm = new GameObject("GameManager");
                gm.AddComponent<GameManager>();
            }
            if (AudioManager.Instance == null)
            {
                GameObject am = new GameObject("AudioManager");
                am.AddComponent<AudioManager>();
            }
            gameManager = GameManager.Instance;
        }

        void BuildRoom()
        {
            Material wallMat = SceneBuildHelpers.MakeMat(new Color(0.08f, 0.08f, 0.1f), 0.2f);
            Material floorMat = SceneBuildHelpers.MakeMat(new Color(0.05f, 0.05f, 0.06f), 0.35f);
            Material ceilingMat = SceneBuildHelpers.MakeMat(new Color(0.04f, 0.04f, 0.05f), 0.1f);

            GameObject root = new GameObject("Room");

            SceneBuildHelpers.CreateBox("Floor", new Vector3(0f, 0f, 0f),
                new Vector3(RoomHalfWidth * 2f, 0.2f, RoomHalfDepth * 2f), floorMat, root.transform);

            SceneBuildHelpers.CreateBox("Ceiling", new Vector3(0f, RoomHeight, 0f),
                new Vector3(RoomHalfWidth * 2f, 0.2f, RoomHalfDepth * 2f), ceilingMat, root.transform);

            SceneBuildHelpers.CreateBox("Wall_N", new Vector3(0f, RoomHeight * 0.5f, RoomHalfDepth),
                new Vector3(RoomHalfWidth * 2f, RoomHeight, 0.4f), wallMat, root.transform);

            SceneBuildHelpers.CreateBox("Wall_S", new Vector3(0f, RoomHeight * 0.5f, -RoomHalfDepth),
                new Vector3(RoomHalfWidth * 2f, RoomHeight, 0.4f), wallMat, root.transform);

            SceneBuildHelpers.CreateBox("Wall_E", new Vector3(RoomHalfWidth, RoomHeight * 0.5f, 0f),
                new Vector3(0.4f, RoomHeight, RoomHalfDepth * 2f), wallMat, root.transform);

            SceneBuildHelpers.CreateBox("Wall_W", new Vector3(-RoomHalfWidth, RoomHeight * 0.5f, 0f),
                new Vector3(0.4f, RoomHeight, RoomHalfDepth * 2f), wallMat, root.transform);

            AddClutter(root.transform, wallMat, floorMat);
            AddLights(root.transform);
        }

        void AddClutter(Transform parent, Material wallMat, Material floorMat)
        {
            Material crateMat = SceneBuildHelpers.MakeMat(new Color(0.18f, 0.15f, 0.12f), 0.15f);
            Material pipeMat = SceneBuildHelpers.MakeMat(new Color(0.12f, 0.13f, 0.16f), 0.6f);

            Vector3[] cratePositions =
            {
                new(-5.5f, 0.75f, 4f),
                new(-5.5f, 2.2f, 4f),
                new(4.2f, 0.75f, -5f),
                new(3f, 0.75f, 5.5f),
                new(-3f, 0.75f, -6f)
            };

            foreach (var p in cratePositions)
            {
                SceneBuildHelpers.CreateBox("Crate", p, new Vector3(1.4f, 1.4f, 1.4f), crateMat, parent);
            }

            GameObject pipeGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pipeGO.name = "Pipe_H";
            pipeGO.transform.SetParent(parent);
            pipeGO.transform.position = new Vector3(0f, 3.8f, -RoomHalfDepth + 0.4f);
            pipeGO.transform.localScale = new Vector3(0.2f, RoomHalfWidth, 0.2f);
            pipeGO.transform.eulerAngles = new Vector3(0f, 0f, 90f);
            pipeGO.GetComponent<Renderer>().sharedMaterial = pipeMat;
            Destroy(pipeGO.GetComponent<Collider>());
        }

        void AddLights(Transform parent)
        {
            GameObject ambient = new GameObject("AmbientLight");
            ambient.transform.SetParent(parent);
            Light al = ambient.AddComponent<Light>();
            al.type = LightType.Directional;
            al.color = new Color(0.35f, 0.4f, 0.55f);
            al.intensity = 0.15f;
            ambient.transform.rotation = Quaternion.Euler(60f, 30f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.04f, 0.05f, 0.07f);

            Vector3[] lampPositions =
            {
                new(-4.5f, 3.6f, -4.5f),
                new(4.5f, 3.6f, 4.5f),
                new(0f, 3.6f, 0f)
            };

            foreach (var p in lampPositions)
            {
                GameObject lamp = new GameObject("PointLamp");
                lamp.transform.SetParent(parent);
                lamp.transform.position = p;
                Light l = lamp.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(0.9f, 0.85f, 0.6f);
                l.range = 9f;
                l.intensity = 1.4f;
            }
        }

        BombManager BuildBomb()
        {
            BombaFisicaBuilder builder3D = LocalizarBuilder3D();
            if (builder3D != null)
                return BuildBomb3D(builder3D);

            Vector3 bombPos = PickBombPosition();

            GameObject bombRoot = new GameObject("Bomb");
            bombRoot.transform.position = bombPos;
            bombRoot.tag = "Respawn";

            Material bodyMat = SceneBuildHelpers.MakeMat(new Color(0.045f, 0.045f, 0.05f), 0.18f);
            Material shellMat = SceneBuildHelpers.MakeMat(new Color(0.08f, 0.08f, 0.09f), 0.3f);
            Material metalMat = SceneBuildHelpers.MakeMat(new Color(0.22f, 0.21f, 0.2f), 0.72f);
            Material moduleMat = SceneBuildHelpers.MakeMat(new Color(0.1f, 0.095f, 0.11f), 0.42f);
            Material modulePlateMat = SceneBuildHelpers.MakeMat(new Color(0.19f, 0.19f, 0.2f), 0.68f);
            Material fuseMat = SceneBuildHelpers.MakeMat(new Color(0.18f, 0.15f, 0.11f), 0.16f);
            Material sparkMat = SceneBuildHelpers.MakeMat(new Color(1.4f, 0.85f, 0.2f), 0.8f, true);
            Material tapeMat = SceneBuildHelpers.MakeMat(new Color(0.18f, 0.17f, 0.16f), 0.05f);
            Material pulseMat = SceneBuildHelpers.MakeMat(new Color(0.72f, 0.08f, 0.06f), 0.55f, true);
            Material[] wireMats =
            {
                SceneBuildHelpers.MakeMat(new Color(0.42f, 0.08f, 0.08f), 0.28f),
                SceneBuildHelpers.MakeMat(new Color(0.07f, 0.18f, 0.48f), 0.28f),
                SceneBuildHelpers.MakeMat(new Color(0.08f, 0.32f, 0.14f), 0.28f),
                SceneBuildHelpers.MakeMat(new Color(0.55f, 0.46f, 0.08f), 0.28f)
            };
            Vector3 bodyCenter = new Vector3(0f, 0.62f, 0f);

            GameObject standRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            standRing.name = "BombStand";
            standRing.transform.SetParent(bombRoot.transform);
            standRing.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            standRing.transform.localScale = new Vector3(0.88f, 0.05f, 0.88f);
            standRing.GetComponent<Renderer>().sharedMaterial = metalMat;
            Destroy(standRing.GetComponent<Collider>());

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "BombBody";
            body.transform.SetParent(bombRoot.transform);
            body.transform.localPosition = bodyCenter;
            body.transform.localScale = new Vector3(1.18f, 1.08f, 1.18f);
            body.GetComponent<Renderer>().sharedMaterial = bodyMat;
            Destroy(body.GetComponent<Collider>());

            GameObject bodyPatch = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bodyPatch.name = "BodyPatch";
            bodyPatch.transform.SetParent(bombRoot.transform);
            bodyPatch.transform.localPosition = bodyCenter + new Vector3(-0.08f, -0.02f, 0.06f);
            bodyPatch.transform.localScale = new Vector3(1.14f, 1.0f, 1.14f);
            bodyPatch.GetComponent<Renderer>().sharedMaterial = shellMat;
            Destroy(bodyPatch.GetComponent<Collider>());

            GameObject seamBand = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            seamBand.name = "SeamBand";
            seamBand.transform.SetParent(bombRoot.transform);
            seamBand.transform.localPosition = bodyCenter + new Vector3(0f, 0.02f, 0f);
            seamBand.transform.localRotation = Quaternion.Euler(5f, 0f, -3f);
            seamBand.transform.localScale = new Vector3(1.13f, 0.04f, 1.13f);
            seamBand.GetComponent<Renderer>().sharedMaterial = shellMat;
            Destroy(seamBand.GetComponent<Collider>());

            GameObject tape = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tape.name = "DuctTape";
            tape.transform.SetParent(bombRoot.transform);
            tape.transform.localPosition = bodyCenter + new Vector3(-0.16f, 0.08f, 0.28f);
            tape.transform.localRotation = Quaternion.Euler(16f, 34f, 14f);
            tape.transform.localScale = new Vector3(0.56f, 0.13f, 0.22f);
            tape.GetComponent<Renderer>().sharedMaterial = tapeMat;
            Destroy(tape.GetComponent<Collider>());

            GameObject tape2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tape2.name = "DuctTape2";
            tape2.transform.SetParent(bombRoot.transform);
            tape2.transform.localPosition = bodyCenter + new Vector3(0.23f, -0.12f, -0.18f);
            tape2.transform.localRotation = Quaternion.Euler(-22f, -28f, 9f);
            tape2.transform.localScale = new Vector3(0.52f, 0.11f, 0.2f);
            tape2.GetComponent<Renderer>().sharedMaterial = tapeMat;
            Destroy(tape2.GetComponent<Collider>());

            Vector3[] boltOffsets =
            {
                bodyCenter + new Vector3(0.46f, 0.18f, 0.2f),
                bodyCenter + new Vector3(-0.42f, 0.12f, 0.28f),
                bodyCenter + new Vector3(0.16f, 0.24f, -0.42f),
                bodyCenter + new Vector3(-0.24f, -0.24f, 0.44f)
            };
            foreach (var bo in boltOffsets)
            {
                GameObject bolt = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bolt.name = "Bolt";
                bolt.transform.SetParent(bombRoot.transform);
                bolt.transform.localPosition = bo;
                bolt.transform.localScale = new Vector3(0.07f, 0.035f, 0.07f);
                bolt.GetComponent<Renderer>().sharedMaterial = metalMat;
                Destroy(bolt.GetComponent<Collider>());
            }

            GameObject fuse = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fuse.name = "FuseStem";
            fuse.transform.SetParent(bombRoot.transform);
            fuse.transform.localPosition = bodyCenter + new Vector3(0.02f, 0.56f, -0.03f);
            fuse.transform.localRotation = Quaternion.Euler(12f, 0f, -10f);
            fuse.transform.localScale = new Vector3(0.045f, 0.18f, 0.045f);
            fuse.GetComponent<Renderer>().sharedMaterial = fuseMat;
            Destroy(fuse.GetComponent<Collider>());

            GameObject fuseTipStem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fuseTipStem.name = "FuseTipStem";
            fuseTipStem.transform.SetParent(bombRoot.transform);
            fuseTipStem.transform.localPosition = bodyCenter + new Vector3(0.11f, 0.78f, 0.02f);
            fuseTipStem.transform.localRotation = Quaternion.Euler(26f, 16f, 8f);
            fuseTipStem.transform.localScale = new Vector3(0.03f, 0.11f, 0.03f);
            fuseTipStem.GetComponent<Renderer>().sharedMaterial = fuseMat;
            Destroy(fuseTipStem.GetComponent<Collider>());

            GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spark.name = "Spark";
            spark.transform.SetParent(fuseTipStem.transform);
            spark.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            spark.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);
            spark.GetComponent<Renderer>().sharedMaterial = sparkMat;
            Destroy(spark.GetComponent<Collider>());

            GameObject sparkLightGO = new GameObject("SparkLight");
            sparkLightGO.transform.SetParent(spark.transform);
            sparkLightGO.transform.localPosition = Vector3.zero;
            Light sparkLight = sparkLightGO.AddComponent<Light>();
            sparkLight.type = LightType.Point;
            sparkLight.color = new Color(1f, 0.75f, 0.3f);
            sparkLight.intensity = 1.4f;
            sparkLight.range = 1.6f;

            Vector3[] modulePositions =
            {
                bodyCenter + new Vector3(0.56f, 0.04f, 0.4f),
                bodyCenter + new Vector3(-0.6f, -0.02f, 0.34f),
                bodyCenter + new Vector3(0.08f, -0.18f, 0.62f)
            };
            Vector3[] moduleSizes =
            {
                new(0.28f, 0.22f, 0.16f),
                new(0.24f, 0.18f, 0.14f),
                new(0.32f, 0.14f, 0.12f)
            };
            Vector3[] moduleOffsets =
            {
                new(8f, -18f, 4f),
                new(-7f, 22f, -3f),
                new(12f, 4f, -8f)
            };
            Color[] ledColors =
            {
                new Color(1.2f, 0.15f, 0.1f),
                new Color(0.15f, 0.95f, 0.5f),
                new Color(1.2f, 0.8f, 0.2f)
            };

            Transform[] modules = new Transform[modulePositions.Length];
            for (int i = 0; i < modulePositions.Length; i++)
            {
                modules[i] = CreateBombModule(
                    bombRoot.transform,
                    bodyCenter,
                    $"Module_{i}",
                    modulePositions[i],
                    moduleSizes[i],
                    moduleOffsets[i],
                    moduleMat,
                    modulePlateMat,
                    metalMat,
                    SceneBuildHelpers.MakeMat(ledColors[i], 0.8f, true));
            }

            GameObject pulseLens = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pulseLens.name = "PulseLens";
            pulseLens.transform.SetParent(bombRoot.transform);
            pulseLens.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            pulseLens.transform.localScale = new Vector3(0.28f, 0.055f, 0.28f);
            pulseLens.GetComponent<Renderer>().sharedMaterial = pulseMat;
            Destroy(pulseLens.GetComponent<Collider>());

            Vector3[] moduleTargets =
            {
                bodyCenter + new Vector3(-0.18f, 0.2f, -0.34f),
                bodyCenter + new Vector3(0.18f, -0.1f, -0.44f),
                bodyCenter + new Vector3(0.12f, 0.36f, -0.24f)
            };
            Vector3[] moduleMidOffsets =
            {
                new(-0.08f, 0.18f, 0.12f),
                new(0.12f, 0.14f, 0.1f),
                new(0.02f, 0.22f, 0.08f)
            };

            for (int i = 0; i < modules.Length; i++)
            {
                Vector3 start = bombRoot.transform.InverseTransformPoint(modules[i].TransformPoint(new Vector3(0.05f, -0.02f, 0.12f)));
                Vector3 end = moduleTargets[i];
                Vector3 mid = (start + end) * 0.5f + moduleMidOffsets[i];
                SpawnCurvedWire(bombRoot.transform, start, mid, end, wireMats[i % wireMats.Length], 0.01f, 7);
            }

            Vector3 fuseLeadStart = bodyCenter + new Vector3(0.03f, 0.44f, -0.02f);
            Vector3 fuseLeadEnd = bodyCenter + new Vector3(-0.26f, 0.24f, -0.18f);
            Vector3 fuseLeadMid = (fuseLeadStart + fuseLeadEnd) * 0.5f + new Vector3(-0.04f, 0.12f, -0.1f);
            SpawnCurvedWire(bombRoot.transform, fuseLeadStart, fuseLeadMid, fuseLeadEnd, wireMats[3], 0.009f, 6);

            Vector3 wrapStart = bodyCenter + new Vector3(-0.38f, -0.08f, 0.24f);
            Vector3 wrapEnd = bodyCenter + new Vector3(0.34f, 0.14f, 0.2f);
            Vector3 wrapMid = (wrapStart + wrapEnd) * 0.5f + new Vector3(0f, 0.24f, 0.16f);
            SpawnCurvedWire(bombRoot.transform, wrapStart, wrapMid, wrapEnd, wireMats[1], 0.008f, 8);

            GameObject lightGO = new GameObject("BombLight");
            lightGO.transform.SetParent(bombRoot.transform);
            lightGO.transform.localPosition = new Vector3(0f, 0.16f, 0f);
            Light l = lightGO.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.18f, 0.12f);
            l.intensity = 3.5f;
            l.range = 5.5f;

            SphereCollider interact = bombRoot.AddComponent<SphereCollider>();
            interact.isTrigger = false;
            interact.radius = 1.02f;
            interact.center = bodyCenter;

            SphereCollider proximity = bombRoot.AddComponent<SphereCollider>();
            proximity.isTrigger = true;
            proximity.radius = BombManager.ProximityRadius;
            proximity.center = bodyCenter;

            Rigidbody proximityRb = bombRoot.AddComponent<Rigidbody>();
            proximityRb.isKinematic = true;
            proximityRb.useGravity = false;

            GameObject zoomTarget = new GameObject("ZoomTarget");
            zoomTarget.transform.SetParent(bombRoot.transform);
            zoomTarget.transform.localPosition = bodyCenter;

            BombManager mgr = bombRoot.AddComponent<BombManager>();
            mgr.bombRoot = bombRoot.transform;
            mgr.bombVisual = body.transform;
            mgr.bombLight = l;
            mgr.bombRenderer = pulseLens.GetComponent<Renderer>();
            mgr.sparkRenderer = spark.GetComponent<Renderer>();
            mgr.sparkLight = sparkLight;
            mgr.cameraZoomTarget = zoomTarget.transform;

            return mgr;
        }

        Transform CreateBombModule(Transform parent, Vector3 bodyCenter, string name, Vector3 localPosition, Vector3 localScale, Vector3 rotationOffset, Material shellMat, Material plateMat, Material detailMat, Material ledMat)
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = name;
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPosition;
            Vector3 outward = (localPosition - bodyCenter).normalized;
            if (outward.sqrMagnitude < 0.001f) outward = Vector3.forward;
            root.transform.localRotation = Quaternion.FromToRotation(Vector3.forward, outward) * Quaternion.Euler(rotationOffset);
            root.transform.localScale = localScale;
            root.GetComponent<Renderer>().sharedMaterial = shellMat;
            Destroy(root.GetComponent<Collider>());

            GameObject face = GameObject.CreatePrimitive(PrimitiveType.Cube);
            face.name = "Face";
            face.transform.SetParent(root.transform, false);
            face.transform.localPosition = new Vector3(0f, 0f, 0.42f);
            face.transform.localScale = new Vector3(0.74f, 0.62f, 0.18f);
            face.GetComponent<Renderer>().sharedMaterial = plateMat;
            Destroy(face.GetComponent<Collider>());

            GameObject led = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            led.name = "LED";
            led.transform.SetParent(root.transform, false);
            led.transform.localPosition = new Vector3(-0.26f, 0.18f, 0.56f);
            led.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);
            led.GetComponent<Renderer>().sharedMaterial = ledMat;
            Destroy(led.GetComponent<Collider>());

            GameObject lead = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lead.name = "Lead";
            lead.transform.SetParent(root.transform, false);
            lead.transform.localPosition = new Vector3(0.05f, -0.02f, 0.58f);
            lead.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            lead.transform.localScale = new Vector3(0.16f, 0.1f, 0.16f);
            lead.GetComponent<Renderer>().sharedMaterial = detailMat;
            Destroy(lead.GetComponent<Collider>());

            Vector3[] boltPositions =
            {
                new(-0.24f, -0.18f, 0.5f),
                new(0.22f, 0.16f, 0.48f)
            };

            foreach (var boltPos in boltPositions)
            {
                GameObject bolt = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bolt.name = "Bolt";
                bolt.transform.SetParent(root.transform, false);
                bolt.transform.localPosition = boltPos;
                bolt.transform.localScale = new Vector3(0.12f, 0.06f, 0.12f);
                bolt.GetComponent<Renderer>().sharedMaterial = detailMat;
                Destroy(bolt.GetComponent<Collider>());
            }

            return root.transform;
        }

        void SpawnCurvedWire(Transform parent, Vector3 start, Vector3 mid, Vector3 end, Material mat, float thickness, int segments)
        {
            Vector3 prev = start;
            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                Vector3 curr = QuadraticBezier(start, mid, end, t);
                SpawnCylinderBetween(parent, prev, curr, thickness, mat);
                prev = curr;
            }
        }

        static Vector3 QuadraticBezier(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            float u = 1f - t;
            return u * u * a + 2f * u * t * b + t * t * c;
        }

        static void SpawnCylinderBetween(Transform parent, Vector3 localA, Vector3 localB, float thickness, Material mat)
        {
            GameObject cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cyl.name = "WireSeg";
            cyl.transform.SetParent(parent, false);
            Vector3 dir = localB - localA;
            float len = dir.magnitude;
            if (len < 0.0001f) { Object.Destroy(cyl); return; }
            cyl.transform.localPosition = (localA + localB) * 0.5f;
            cyl.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
            cyl.transform.localScale = new Vector3(thickness, len * 0.5f, thickness);
            cyl.GetComponent<Renderer>().sharedMaterial = mat;
            Object.Destroy(cyl.GetComponent<Collider>());
        }

        Vector3 PickBombPosition()
        {
            return new Vector3(0f, 0.08f, -3.5f);
        }

        PlayerController BuildPlayer()
        {
            GameObject playerGO = new GameObject("Player");
            Vector3 spawnPos = new Vector3(0f, 1f, -RoomHalfDepth + 5f);
            Quaternion spawnRot = Quaternion.identity;
            if (escolaPrefabricada != null && escolaPrefabricada.PontoDeSpawnDoJogador != null)
            {
                spawnPos = escolaPrefabricada.PontoDeSpawnDoJogador.position;
                spawnRot = escolaPrefabricada.PontoDeSpawnDoJogador.rotation;
            }
            playerGO.transform.SetPositionAndRotation(spawnPos, spawnRot);

            CharacterController cc = playerGO.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            GameObject camGO = new GameObject("PlayerCamera", typeof(Camera), typeof(AudioListener));
            camGO.transform.SetParent(playerGO.transform);
            camGO.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            Camera cam = camGO.GetComponent<Camera>();
            cam.fieldOfView = 72f;
            cam.nearClipPlane = 0.05f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.01f, 0.01f, 0.02f);
            camGO.tag = "MainCamera";

            camGO.AddComponent<CameraShake>();

            PlayerController pc = playerGO.AddComponent<PlayerController>();
            pc.playerCamera = cam;

            Camera.main?.gameObject.SetActive(true);

            return pc;
        }

        void BuildHudAndMinigame(Camera playerCamera, BombManager bomb)
        {
            Canvas hudCanvas = SceneBuildHelpers.CreateCanvas("HUD", 5);
            SceneBuildHelpers.EnsureEventSystem();

            Text timer = SceneBuildHelpers.CreateText(hudCanvas.transform, "Timer", "60:00", 78,
                new Color(0.9f, 1f, 0.9f), TextAnchor.MiddleCenter,
                new Vector2(0f, -80f), new Vector2(600f, 120f),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            timer.fontStyle = FontStyle.Bold;

            Text phase = SceneBuildHelpers.CreateText(hudCanvas.transform, "Phase", "FASE 01", 32,
                new Color(0.6f, 1f, 0.9f), TextAnchor.MiddleLeft,
                new Vector2(40f, -40f), new Vector2(400f, 40f),
                new Vector2(0f, 1f), new Vector2(0f, 1f));
            phase.alignment = TextAnchor.UpperLeft;
            phase.rectTransform.pivot = new Vector2(0f, 1f);
            phase.rectTransform.anchoredPosition = new Vector2(32f, -24f);

            Text strikes = SceneBuildHelpers.CreateText(hudCanvas.transform, "Strikes", "ERROS 0/3", 28,
                new Color(0.78f, 0.94f, 0.84f), TextAnchor.MiddleRight,
                new Vector2(-32f, -28f), new Vector2(360f, 40f),
                new Vector2(1f, 1f), new Vector2(1f, 1f));
            strikes.alignment = TextAnchor.UpperRight;
            strikes.rectTransform.pivot = new Vector2(1f, 1f);
            strikes.rectTransform.anchoredPosition = new Vector2(-32f, -24f);

            Text prompt = SceneBuildHelpers.CreateText(hudCanvas.transform, "Prompt", "", 46,
                new Color(1f, 0.9f, 0.5f), TextAnchor.MiddleCenter,
                new Vector2(0f, 80f), new Vector2(800f, 70f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            prompt.fontStyle = FontStyle.Bold;

            Text message = SceneBuildHelpers.CreateText(hudCanvas.transform, "Message", "", 64,
                new Color(0.6f, 1f, 0.9f), TextAnchor.MiddleCenter,
                new Vector2(0f, 0f), new Vector2(1600f, 200f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            GameObject objectivePanelGO = new GameObject("ObjectivePanel", typeof(RectTransform), typeof(Image));
            RectTransform objectivePanelRect = (RectTransform)objectivePanelGO.transform;
            objectivePanelRect.SetParent(hudCanvas.transform, false);
            objectivePanelRect.anchorMin = new Vector2(0.5f, 1f);
            objectivePanelRect.anchorMax = new Vector2(0.5f, 1f);
            objectivePanelRect.pivot = new Vector2(0.5f, 1f);
            objectivePanelRect.anchoredPosition = new Vector2(0f, -148f);
            objectivePanelRect.sizeDelta = new Vector2(1080f, 112f);
            Image objectivePanel = objectivePanelGO.GetComponent<Image>();
            objectivePanel.sprite = SceneBuildHelpers.GetWhiteSprite();
            objectivePanel.color = new Color(0.06f, 0.12f, 0.11f, 0.82f);
            objectivePanel.raycastTarget = false;

            Text objectiveTitle = SceneBuildHelpers.CreateText(objectivePanelRect, "ObjectiveTitle", "", 34,
                new Color(0.92f, 1f, 0.95f), TextAnchor.MiddleCenter,
                new Vector2(0f, -16f), new Vector2(980f, 38f),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            objectiveTitle.fontStyle = FontStyle.Bold;

            Text objectiveSummary = SceneBuildHelpers.CreateText(objectivePanelRect, "ObjectiveSummary", "", 22,
                new Color(0.72f, 0.94f, 0.82f), TextAnchor.MiddleCenter,
                new Vector2(0f, -58f), new Vector2(960f, 30f),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));

            Image redTop = CreateBorder(hudCanvas.transform, "RedBorder_Top", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(4000f, 16f));
            Image redBottom = CreateBorder(hudCanvas.transform, "RedBorder_Bottom", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(4000f, 16f));
            Image redLeft = CreateBorder(hudCanvas.transform, "RedBorder_Left", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(16f, 2400f));
            Image redRight = CreateBorder(hudCanvas.transform, "RedBorder_Right", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(16f, 2400f));

            GameObject flashGO = new GameObject("FlashOverlay", typeof(RectTransform), typeof(Image));
            RectTransform flashRect = (RectTransform)flashGO.transform;
            flashRect.SetParent(hudCanvas.transform, false);
            flashRect.anchorMin = Vector2.zero;
            flashRect.anchorMax = Vector2.one;
            flashRect.offsetMin = Vector2.zero;
            flashRect.offsetMax = Vector2.zero;
            Image flashImg = flashGO.GetComponent<Image>();
            flashImg.sprite = SceneBuildHelpers.GetWhiteSprite();
            flashImg.color = new Color(0f, 0f, 0f, 0f);
            flashImg.raycastTarget = false;

            GameObject hudHost = new GameObject("HudController");
            HudController hud = hudHost.AddComponent<HudController>();
            hud.timerText = timer;
            hud.phaseText = phase;
            hud.strikesText = strikes;
            hud.promptText = prompt;
            hud.messageText = message;
            hud.redBorderTop = redTop;
            hud.redBorderBottom = redBottom;
            hud.redBorderLeft = redLeft;
            hud.redBorderRight = redRight;

            GameObject flashHost = new GameObject("UIFlasher");
            UIFlasher flasher = flashHost.AddComponent<UIFlasher>();
            flasher.flashImage = flashImg;

            GameObject objetivoHost = new GameObject("GerenciadorDeObjetivo");
            gerenciadorDeObjetivoEscola = objetivoHost.AddComponent<GerenciadorDeObjetivo>();
            gerenciadorDeObjetivoEscola.Configurar(objectiveTitle, objectiveSummary, true);

            GameObject timerGO = new GameObject("TimerController");
            timerGO.AddComponent<TimerController>();

            GerenciadorDeBomba gerenciador = BuildBombGameplay(bomb);
            if (bomb != null) bomb.gerenciadorDeBomba = gerenciador;
        }

        Image CreateBorder(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            Image img = go.GetComponent<Image>();
            img.sprite = SceneBuildHelpers.GetWhiteSprite();
            img.color = new Color(1f, 0.05f, 0.05f, 0f);
            img.raycastTarget = false;
            return img;
        }

        GerenciadorDeBomba BuildBombGameplay(BombManager bomb)
        {
            BombaFisicaBuilder builder3D = LocalizarBuilder3D();
            if (builder3D != null)
                return BuildBombGameplay3D(bomb, builder3D);

            Canvas painelCanvas = SceneBuildHelpers.CreateCanvas("BombPanelCanvas", 9);
            painelCanvas.gameObject.SetActive(false);

            Image overlay = BombUiFactory.CreatePanel(
                painelCanvas.transform,
                "Overlay",
                new Color(0.01f, 0.05f, 0.05f, 0.97f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            RectTransform overlayRT = overlay.rectTransform;
            overlayRT.offsetMin = Vector2.zero;
            overlayRT.offsetMax = Vector2.zero;

            Image frame = BombUiFactory.CreatePanel(
                overlay.transform,
                "Frame",
                new Color(0.03f, 0.08f, 0.08f, 0.98f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(1760f, 930f));

            BombUiFactory.CreateImage(
                frame.transform,
                "AccentTop",
                new Color(0.28f, 0.96f, 0.74f, 0.34f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, -10f),
                new Vector2(-56f, 6f),
                new Vector2(0.5f, 1f));

            Text title = BombUiFactory.CreateText(
                frame.transform,
                "Title",
                "NUCLEO INTERNO",
                48,
                new Color(0.9f, 1f, 0.96f),
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -44f),
                new Vector2(900f, 56f),
                new Vector2(0.5f, 1f));
            title.fontStyle = FontStyle.Bold;

            Text subtitle = BombUiFactory.CreateText(
                frame.transform,
                "Subtitle",
                string.Empty,
                24,
                new Color(0.68f, 0.92f, 0.84f),
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -92f),
                new Vector2(1100f, 34f),
                new Vector2(0.5f, 1f));

            Text difficulty = BombUiFactory.CreateText(
                frame.transform,
                "Difficulty",
                string.Empty,
                22,
                new Color(0.84f, 0.96f, 0.88f),
                TextAnchor.MiddleLeft,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(42f, -146f),
                new Vector2(340f, 32f),
                new Vector2(0f, 1f));

            Text strikes = BombUiFactory.CreateText(
                frame.transform,
                "PanelStrikes",
                string.Empty,
                22,
                new Color(1f, 0.86f, 0.48f),
                TextAnchor.MiddleRight,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-42f, -146f),
                new Vector2(340f, 32f),
                new Vector2(1f, 1f));

            Text status = BombUiFactory.CreateText(
                frame.transform,
                "StatusGlobal",
                string.Empty,
                22,
                new Color(1f, 0.9f, 0.46f),
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 52f),
                new Vector2(1280f, 36f),
                new Vector2(0.5f, 0f));

            Text footer = BombUiFactory.CreateText(
                frame.transform,
                "Footer",
                "ESC FECHA O PAINEL",
                20,
                new Color(0.72f, 0.9f, 0.82f),
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 22f),
                new Vector2(640f, 28f),
                new Vector2(0.5f, 0f));

            RectTransform modulesRoot = BombUiFactory.CreateRect(
                frame.transform,
                "ModulesRoot",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -14f),
                new Vector2(1680f, 660f));

            RectTransform sincronizadorSlot = BombUiFactory.CreateRect(
                modulesRoot,
                "SincronizadorSlot",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(1180f, 640f));

            GameObject gerenciadorGO = new GameObject("GerenciadorDeBomba");
            GerenciadorDeBomba gerenciador = gerenciadorGO.AddComponent<GerenciadorDeBomba>();
            gerenciador.Configurar(bomb, painelCanvas, frame.rectTransform, title, subtitle, status, difficulty, strikes, footer, overlay);

            ModuloSincronizadorFrequencia sincronizador = new GameObject("ModuloSincronizadorFrequencia").AddComponent<ModuloSincronizadorFrequencia>();
            sincronizador.transform.SetParent(gerenciadorGO.transform, false);
            sincronizador.Configurar(gerenciador, sincronizadorSlot, "SINCRONIZADOR DE ONDAS");

            return gerenciador;
        }

        WireMinigame BuildMinigame3D(BombManager bomb)
        {
            GameObject interior = new GameObject("BombInterior");
            if (bomb != null) interior.transform.SetParent(bomb.transform, false);
            interior.SetActive(false);

            Canvas minigameCanvas = SceneBuildHelpers.CreateCanvas("MinigameCanvas", 4);
            minigameCanvas.gameObject.SetActive(false);

            GameObject overlayGO = new GameObject("Overlay", typeof(RectTransform), typeof(Image));
            RectTransform overlayRT = (RectTransform)overlayGO.transform;
            overlayRT.SetParent(minigameCanvas.transform, false);
            overlayRT.anchorMin = Vector2.zero;
            overlayRT.anchorMax = Vector2.one;
            overlayRT.offsetMin = Vector2.zero;
            overlayRT.offsetMax = Vector2.zero;
            Image overlayImg = overlayGO.GetComponent<Image>();
            overlayImg.sprite = SceneBuildHelpers.GetWhiteSprite();
            overlayImg.color = new Color(0f, 0.02f, 0.01f, 0.92f);
            overlayImg.raycastTarget = false;

            GameObject gridGO = new GameObject("GridGlow", typeof(RectTransform), typeof(Image));
            RectTransform gridRT = (RectTransform)gridGO.transform;
            gridRT.SetParent(overlayRT, false);
            gridRT.anchorMin = Vector2.zero;
            gridRT.anchorMax = Vector2.one;
            gridRT.offsetMin = Vector2.zero;
            gridRT.offsetMax = Vector2.zero;
            Image gridImg = gridGO.GetComponent<Image>();
            gridImg.sprite = SceneBuildHelpers.GetWhiteSprite();
            gridImg.color = new Color(0.02f, 0.14f, 0.06f, 0.1f);
            gridImg.raycastTarget = false;

            GameObject panelTitleGO = new GameObject("PanelTitle", typeof(RectTransform), typeof(Text));
            RectTransform ptRT = (RectTransform)panelTitleGO.transform;
            ptRT.SetParent(overlayRT, false);
            ptRT.anchorMin = new Vector2(0.5f, 1f);
            ptRT.anchorMax = new Vector2(0.5f, 1f);
            ptRT.pivot = new Vector2(0.5f, 1f);
            ptRT.anchoredPosition = new Vector2(0f, -200f);
            ptRT.sizeDelta = new Vector2(1200f, 70f);
            Text pt = panelTitleGO.GetComponent<Text>();
            pt.font = HudController.GetMonoFont();
            pt.fontSize = 54;
            pt.alignment = TextAnchor.MiddleCenter;
            pt.color = new Color(0.7f, 1.2f, 0.85f);
            pt.fontStyle = FontStyle.Bold;
            pt.text = "";

            GameObject panelSubGO = new GameObject("PanelSubtitle", typeof(RectTransform), typeof(Text));
            RectTransform psRT = (RectTransform)panelSubGO.transform;
            psRT.SetParent(overlayRT, false);
            psRT.anchorMin = new Vector2(0.5f, 1f);
            psRT.anchorMax = new Vector2(0.5f, 1f);
            psRT.pivot = new Vector2(0.5f, 1f);
            psRT.anchoredPosition = new Vector2(0f, -266f);
            psRT.sizeDelta = new Vector2(1200f, 40f);
            Text ps = panelSubGO.GetComponent<Text>();
            ps.font = HudController.GetMonoFont();
            ps.fontSize = 26;
            ps.alignment = TextAnchor.MiddleCenter;
            ps.color = new Color(0.6f, 0.9f, 0.7f);
            ps.text = "";

            GameObject controlsGO = new GameObject("ControlsHint", typeof(RectTransform), typeof(Text));
            RectTransform cRT = (RectTransform)controlsGO.transform;
            cRT.SetParent(overlayRT, false);
            cRT.anchorMin = new Vector2(0.5f, 0f);
            cRT.anchorMax = new Vector2(0.5f, 0f);
            cRT.pivot = new Vector2(0.5f, 0f);
            cRT.anchoredPosition = new Vector2(0f, 78f);
            cRT.sizeDelta = new Vector2(1500f, 40f);
            Text ct = controlsGO.GetComponent<Text>();
            ct.font = HudController.GetMonoFont();
            ct.fontSize = 24;
            ct.alignment = TextAnchor.MiddleCenter;
            ct.color = new Color(0.72f, 0.96f, 0.82f, 0.9f);
            ct.text = "";

            GameObject listGO = new GameObject("WireList", typeof(RectTransform), typeof(Image));
            RectTransform listRT = (RectTransform)listGO.transform;
            listRT.SetParent(overlayRT, false);
            listRT.anchorMin = new Vector2(0f, 0.5f);
            listRT.anchorMax = new Vector2(0f, 0.5f);
            listRT.pivot = new Vector2(0f, 0.5f);
            listRT.anchoredPosition = new Vector2(80f, 0f);
            listRT.sizeDelta = new Vector2(340f, 620f);
            Image listBg = listGO.GetComponent<Image>();
            listBg.sprite = SceneBuildHelpers.GetWhiteSprite();
            listBg.color = new Color(0.015f, 0.04f, 0.02f, 0.85f);
            listBg.raycastTarget = false;

            GameObject listTitle = new GameObject("ListTitle", typeof(RectTransform), typeof(Text));
            RectTransform ltRT = (RectTransform)listTitle.transform;
            ltRT.SetParent(listRT, false);
            ltRT.anchorMin = new Vector2(0f, 1f);
            ltRT.anchorMax = new Vector2(1f, 1f);
            ltRT.pivot = new Vector2(0.5f, 1f);
            ltRT.anchoredPosition = new Vector2(0f, 22f);
            ltRT.sizeDelta = new Vector2(0f, 36f);
            Text lt = listTitle.GetComponent<Text>();
            lt.font = HudController.GetMonoFont();
            lt.fontSize = 24;
            lt.alignment = TextAnchor.MiddleCenter;
            lt.color = new Color(0.4f, 0.9f, 0.55f);
            lt.text = "FIOS";

            GameObject systemsGO = new GameObject("SystemsPanel", typeof(RectTransform), typeof(Image));
            RectTransform systemsRT = (RectTransform)systemsGO.transform;
            systemsRT.SetParent(overlayRT, false);
            systemsRT.anchorMin = new Vector2(1f, 0.5f);
            systemsRT.anchorMax = new Vector2(1f, 0.5f);
            systemsRT.pivot = new Vector2(1f, 0.5f);
            systemsRT.anchoredPosition = new Vector2(-80f, 0f);
            systemsRT.sizeDelta = new Vector2(360f, 620f);
            Image systemsBg = systemsGO.GetComponent<Image>();
            systemsBg.sprite = SceneBuildHelpers.GetWhiteSprite();
            systemsBg.color = new Color(0.015f, 0.04f, 0.02f, 0.85f);
            systemsBg.raycastTarget = false;

            GameObject systemsTitleGO = new GameObject("SystemsTitle", typeof(RectTransform), typeof(Text));
            RectTransform stRT = (RectTransform)systemsTitleGO.transform;
            stRT.SetParent(systemsRT, false);
            stRT.anchorMin = new Vector2(0f, 1f);
            stRT.anchorMax = new Vector2(1f, 1f);
            stRT.pivot = new Vector2(0.5f, 1f);
            stRT.anchoredPosition = new Vector2(0f, -18f);
            stRT.sizeDelta = new Vector2(0f, 36f);
            Text st = systemsTitleGO.GetComponent<Text>();
            st.font = HudController.GetMonoFont();
            st.fontSize = 24;
            st.alignment = TextAnchor.MiddleCenter;
            st.color = new Color(0.8f, 0.95f, 0.82f);
            st.text = "SUBSISTEMAS";

            GameObject systemsTextGO = new GameObject("SystemsText", typeof(RectTransform), typeof(Text));
            RectTransform sysTextRT = (RectTransform)systemsTextGO.transform;
            sysTextRT.SetParent(systemsRT, false);
            sysTextRT.anchorMin = new Vector2(0f, 1f);
            sysTextRT.anchorMax = new Vector2(1f, 1f);
            sysTextRT.pivot = new Vector2(0.5f, 1f);
            sysTextRT.anchoredPosition = new Vector2(0f, -64f);
            sysTextRT.sizeDelta = new Vector2(-28f, 270f);
            Text sysText = systemsTextGO.GetComponent<Text>();
            sysText.font = HudController.GetMonoFont();
            sysText.fontSize = 22;
            sysText.alignment = TextAnchor.UpperLeft;
            sysText.color = new Color(0.72f, 0.9f, 0.78f);
            sysText.horizontalOverflow = HorizontalWrapMode.Wrap;
            sysText.verticalOverflow = VerticalWrapMode.Overflow;
            sysText.text = "";

            GameObject instLabelGO = new GameObject("InstabilityLabel", typeof(RectTransform), typeof(Text));
            RectTransform instLabelRT = (RectTransform)instLabelGO.transform;
            instLabelRT.SetParent(systemsRT, false);
            instLabelRT.anchorMin = new Vector2(0f, 0f);
            instLabelRT.anchorMax = new Vector2(1f, 0f);
            instLabelRT.pivot = new Vector2(0.5f, 0f);
            instLabelRT.anchoredPosition = new Vector2(0f, 188f);
            instLabelRT.sizeDelta = new Vector2(-28f, 28f);
            Text instLabel = instLabelGO.GetComponent<Text>();
            instLabel.font = HudController.GetMonoFont();
            instLabel.fontSize = 20;
            instLabel.alignment = TextAnchor.MiddleLeft;
            instLabel.color = new Color(0.84f, 0.92f, 0.86f);
            instLabel.text = "PRESSAO";

            GameObject instBgGO = new GameObject("InstabilityBG", typeof(RectTransform), typeof(Image));
            RectTransform instBgRT = (RectTransform)instBgGO.transform;
            instBgRT.SetParent(systemsRT, false);
            instBgRT.anchorMin = new Vector2(0f, 0f);
            instBgRT.anchorMax = new Vector2(1f, 0f);
            instBgRT.pivot = new Vector2(0.5f, 0f);
            instBgRT.anchoredPosition = new Vector2(0f, 152f);
            instBgRT.sizeDelta = new Vector2(-28f, 18f);
            Image instBg = instBgGO.GetComponent<Image>();
            instBg.sprite = SceneBuildHelpers.GetWhiteSprite();
            instBg.color = new Color(0.06f, 0.08f, 0.1f, 0.95f);
            instBg.raycastTarget = false;

            GameObject instFillGO = new GameObject("InstabilityFill", typeof(RectTransform), typeof(Image));
            RectTransform instFillRT = (RectTransform)instFillGO.transform;
            instFillRT.SetParent(instBgRT, false);
            instFillRT.anchorMin = new Vector2(0f, 0f);
            instFillRT.anchorMax = new Vector2(1f, 1f);
            instFillRT.offsetMin = instFillRT.offsetMax = Vector2.zero;
            Image instFill = instFillGO.GetComponent<Image>();
            instFill.sprite = SceneBuildHelpers.GetWhiteSprite();
            instFill.type = Image.Type.Filled;
            instFill.fillMethod = Image.FillMethod.Horizontal;
            instFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            instFill.fillAmount = 0f;
            instFill.color = new Color(0.25f, 0.9f, 0.35f, 1f);
            instFill.raycastTarget = false;

            GameObject utilityGO = new GameObject("UtilityText", typeof(RectTransform), typeof(Text));
            RectTransform utilRT = (RectTransform)utilityGO.transform;
            utilRT.SetParent(systemsRT, false);
            utilRT.anchorMin = new Vector2(0f, 0f);
            utilRT.anchorMax = new Vector2(1f, 0f);
            utilRT.pivot = new Vector2(0.5f, 0f);
            utilRT.anchoredPosition = new Vector2(0f, 90f);
            utilRT.sizeDelta = new Vector2(-28f, 54f);
            Text utilText = utilityGO.GetComponent<Text>();
            utilText.font = HudController.GetMonoFont();
            utilText.fontSize = 20;
            utilText.alignment = TextAnchor.UpperLeft;
            utilText.color = new Color(0.84f, 0.92f, 0.86f);
            utilText.text = "";

            GameObject feedGO = new GameObject("FeedText", typeof(RectTransform), typeof(Text));
            RectTransform feedRT = (RectTransform)feedGO.transform;
            feedRT.SetParent(systemsRT, false);
            feedRT.anchorMin = new Vector2(0f, 0f);
            feedRT.anchorMax = new Vector2(1f, 0f);
            feedRT.pivot = new Vector2(0.5f, 0f);
            feedRT.anchoredPosition = new Vector2(0f, 18f);
            feedRT.sizeDelta = new Vector2(-28f, 54f);
            Text feed = feedGO.GetComponent<Text>();
            feed.font = HudController.GetMonoFont();
            feed.fontSize = 22;
            feed.alignment = TextAnchor.UpperLeft;
            feed.color = new Color(1f, 0.88f, 0.42f);
            feed.text = "";

            GameObject challengeGO = new GameObject("ChallengeArea", typeof(RectTransform));
            RectTransform challRT = (RectTransform)challengeGO.transform;
            challRT.SetParent(overlayRT, false);
            challRT.anchorMin = new Vector2(0.5f, 0.5f);
            challRT.anchorMax = new Vector2(0.5f, 0.5f);
            challRT.pivot = new Vector2(0.5f, 0.5f);
            challRT.anchoredPosition = new Vector2(140f, -40f);
            challRT.sizeDelta = new Vector2(1200f, 720f);

            GameObject minigameHost = new GameObject("WireMinigame");
            WireMinigame mg = minigameHost.AddComponent<WireMinigame>();
            mg.bombRoot = bomb != null ? bomb.transform : null;
            mg.interiorRoot = interior.transform;
            mg.minigameCanvas = minigameCanvas;
            mg.overlayRoot = overlayRT;
            mg.wireListRoot = listRT;
            mg.challengeRoot = challRT;
            mg.panelTitle = pt;
            mg.panelSubtitle = ps;
            mg.controlsText = ct;
            mg.subsystemStatusText = sysText;
            mg.utilityText = utilText;
            mg.feedText = feed;
            mg.instabilityFill = instFill;
            mg.vignetteImage = overlayImg;
            return mg;
        }

        void StartPhase(PlayerController player)
        {
            TimerController timerController = TimerController.Instance;
            if (timerController != null)
            {
                timerController.OnTimeout -= HandleTimeout;
                timerController.OnTimeout += HandleTimeout;
            }

            if (GerenciadorDeFase.Instance != null && GerenciadorDeFase.Instance.CampanhaAtiva)
                return;

            int phase = gameManager.CurrentPhase;
            float time = gameManager.GetPhaseTime(phase);

            processandoTimeout = false;
            AudioManager.Instance?.ResetRunState();
            timerController?.Begin(time);
            AudioManager.Instance?.StartHiss();
            AudioManager.Instance?.SetHissIntensity(0f);
        }

        void HandleTimeout()
        {
            if (processandoTimeout) return;
            processandoTimeout = true;

            if (GerenciadorDeBomba.Instance != null && GerenciadorDeBomba.Instance.IsOpen)
            {
                StartCoroutine(HandleTimeoutComFalhaCritica());
                return;
            }

            if (GameManager.Instance == null) return;
            Vector3 bombPos = BombManager.Instance != null ? BombManager.Instance.GetBombPosition() : Vector3.zero;
            Camera cam = PlayerController.Instance != null ? PlayerController.Instance.playerCamera : Camera.main;
            GameManager.Instance.TriggerGameOver(bombPos, cam);
        }

        IEnumerator HandleTimeoutComFalhaCritica()
        {
            GerenciadorDeBomba.Instance?.MostrarFalhaCritica();
            AudioManager.Instance?.PlayAlert();
            CameraShake.Instance?.Impulse(0.65f);

            yield return new WaitForSecondsRealtime(1.1f);

            if (GameManager.Instance == null) yield break;
            Vector3 bombPos = BombManager.Instance != null ? BombManager.Instance.GetBombPosition() : Vector3.zero;
            Camera cam = PlayerController.Instance != null ? PlayerController.Instance.playerCamera : Camera.main;
            GameManager.Instance.TriggerGameOver(bombPos, cam);
        }

        void OnDestroy()
        {
            if (TimerController.Instance != null)
            {
                TimerController.Instance.OnTimeout -= HandleTimeout;
            }
        }
    }
}
