using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LiveWire;

namespace LiveWireEditor
{
    public static class MontadorDeEscola
    {
        const string MenuRoot = "LiveWire/Escola/";
        const string EscolaRootName = "EscolaMontada";

        // Backrooms (estrutura)
        const string PathFloor = "Assets/LoafbrrAssets/BackroomsLikeAssetRe/prefab/Floor/BR_Floor_3x3.prefab";
        const string PathWall = "Assets/LoafbrrAssets/BackroomsLikeAssetRe/prefab/Wall/BR_Wall_A_3x3.prefab";
        const string PathWallCrack = "Assets/LoafbrrAssets/BackroomsLikeAssetRe/prefab/Wall/BR_Wall_A_3x3_Crck_A.prefab";
        const string PathWallDoor = "Assets/LoafbrrAssets/BackroomsLikeAssetRe/prefab/Wall/BR_Wall_A_3x3_Door_A.prefab";

        // School props
        const string PathSchoolBoard = "Assets/school/Prefabs/props/board.prefab";
        const string PathSchoolBoardAlt = "Assets/school/Prefabs/props/board2.prefab";
        const string PathSchoolTablePro = "Assets/school/Prefabs/props/table1.prefab";
        const string PathSchoolTableAluno = "Assets/school/Prefabs/props/table2.prefab";
        const string PathSchoolTableLong = "Assets/school/Prefabs/props/table3.prefab";
        const string PathSchoolDoor = "Assets/school/Prefabs/props/a door.prefab";
        const string PathSchoolDoorAlt = "Assets/school/Prefabs/props/a door1.prefab";
        const string PathSchoolLaptop = "Assets/school/Prefabs/props/a laptop.prefab";
        const string PathSchoolChair = "Assets/school/Prefabs/props/chair.prefab";
        const string PathSchoolChair2 = "Assets/school/Prefabs/props/chair1.prefab";
        const string PathSchoolLocker = "Assets/school/Prefabs/props/locker.prefab";
        const string PathSchoolLocker1 = "Assets/school/Prefabs/props/locker_1.prefab";
        const string PathSchoolLocker2 = "Assets/school/Prefabs/props/locker_2.prefab";
        const string PathSchoolRack = "Assets/school/Prefabs/props/rack.prefab";
        const string PathSchoolRack2 = "Assets/school/Prefabs/props/rack1.prefab";
        const string PathSchoolBook = "Assets/school/Prefabs/props/book1.prefab";
        const string PathSchoolBook2 = "Assets/school/Prefabs/props/book7.prefab";
        const string PathSchoolBook3 = "Assets/school/Prefabs/props/book12.prefab";
        const string PathSchoolComputer = "Assets/school/Prefabs/props/computer.prefab";
        const string PathSchoolComputer2 = "Assets/school/Prefabs/props/computer2.prefab";
        const string PathSchoolShowcase = "Assets/school/Prefabs/props/showcase.prefab";
        const string PathSchoolProjector = "Assets/school/Prefabs/props/projector.prefab";
        const string PathSchoolTray = "Assets/school/Prefabs/props/tray.prefab";
        const string PathSchoolSpeaker = "Assets/school/Prefabs/props/speaker.prefab";
        const string PathSchoolFire = "Assets/school/Prefabs/props/fire.prefab";
        const string PathSchoolJalousie = "Assets/school/Prefabs/props/jalousie.prefab";
        const string PathSchoolSheet = "Assets/school/Prefabs/props/sheet.prefab";
        const string PathSchoolSheet2 = "Assets/school/Prefabs/props/sheet2.prefab";
        const string PathSchoolBus = "Assets/school/Prefabs/props/bus.prefab";
        const string PathSchoolRoad = "Assets/school/Prefabs/road/doroga.prefab";
        const string PathSchoolRoadCross = "Assets/school/Prefabs/road/doroga — krest.prefab";
        const string PathSchoolOutdoorFloor = "Assets/school/Prefabs/road/floor.prefab";

        // MarpaStudio Office (secretaria + utilitarios)
        const string PathOfficeDesk = "Assets/MarpaStudio/Built-In/Prefabs/DeskBase .prefab";
        const string PathOfficeChair = "Assets/MarpaStudio/Built-In/Prefabs/OfficeChair.prefab";
        const string PathOfficeMonitor = "Assets/MarpaStudio/Built-In/Prefabs/PCScreen.prefab";
        const string PathOfficeKeyboard = "Assets/MarpaStudio/Built-In/Prefabs/KeyBoard.prefab";
        const string PathOfficeMouse = "Assets/MarpaStudio/Built-In/Prefabs/Mouse.prefab";
        const string PathOfficePhone = "Assets/MarpaStudio/Built-In/Prefabs/Phone.prefab";
        const string PathOfficeLamp = "Assets/MarpaStudio/Built-In/Prefabs/DeskLamp.prefab";
        const string PathOfficeArchive = "Assets/MarpaStudio/Built-In/Prefabs/Archive Cabin.prefab";
        const string PathOfficeCloset = "Assets/MarpaStudio/Built-In/Prefabs/Closet.prefab";
        const string PathOfficeClock = "Assets/MarpaStudio/Built-In/Prefabs/Clock.prefab";
        const string PathOfficeFoto = "Assets/MarpaStudio/Built-In/Prefabs/FotoFrameSmall.prefab";
        const string PathOfficeBinder = "Assets/MarpaStudio/Built-In/Prefabs/Binder.prefab";
        const string PathOfficeTrash = "Assets/MarpaStudio/Built-In/Prefabs/TrashCan.prefab";
        const string PathOfficeExitSign = "Assets/MarpaStudio/Built-In/Prefabs/ExitSign.prefab";
        const string PathOfficeSmoke = "Assets/MarpaStudio/Built-In/Prefabs/SmokeDetector.prefab";
        const string PathOfficeFireAlarm = "Assets/MarpaStudio/Built-In/Prefabs/FireAlarm.prefab";

        const float TileSize = 3f;
        const float WallHeight = 3f;
        const float HalfTile = TileSize * 0.5f;

        [System.Flags]
        enum Lados { Nenhum = 0, Norte = 1, Sul = 2, Leste = 4, Oeste = 8, Todos = Norte | Sul | Leste | Oeste }

        [MenuItem(MenuRoot + "Construir Escola Pre-Fabricada")]
        public static void Construir()
        {
            GameObject existing = GameObject.Find(EscolaRootName);
            if (existing != null)
            {
                bool ok = EditorUtility.DisplayDialog(
                    "Reconstruir escola?",
                    $"Ja existe um GameObject '{EscolaRootName}'. Quer apaga-lo e reconstruir do zero?",
                    "Sim, reconstruir",
                    "Cancelar");
                if (!ok) return;
                Object.DestroyImmediate(existing);
            }

            GameObject root = new GameObject(EscolaRootName);
            Undo.RegisterCreatedObjectUndo(root, "Construir Escola");

            BuildCorredor(root.transform);

            // 4 salas norte (6x6 cada). Centro em z=+4.5.
            // Linha de salas x=-9, -3, +3, +9.
            // Pula parede sul (lado do corredor). Pula oeste se ja foi construida pela sala vizinha.
            BuildSala(root.transform, "SalaDeAula", new Vector3(-9f, 0f, 4.5f), 2, 2, Lados.Norte | Lados.Leste | Lados.Oeste);
            BuildSala(root.transform, "SalaDeAula_02", new Vector3(-3f, 0f, 4.5f), 2, 2, Lados.Norte | Lados.Leste);
            BuildSala(root.transform, "Biblioteca", new Vector3(3f, 0f, 4.5f), 2, 2, Lados.Norte | Lados.Leste);
            BuildSala(root.transform, "Laboratorio", new Vector3(9f, 0f, 4.5f), 2, 2, Lados.Norte | Lados.Leste);

            // 3 salas sul. Pula parede norte (lado do corredor).
            BuildSala(root.transform, "Banheiro", new Vector3(-9f, 0f, -4.5f), 2, 2, Lados.Sul | Lados.Leste | Lados.Oeste);
            BuildSala(root.transform, "Secretaria", new Vector3(-3f, 0f, -4.5f), 2, 2, Lados.Sul | Lados.Leste);
            BuildSala(root.transform, "Refeitorio", new Vector3(3f, 0f, -4.5f), 2, 2, Lados.Sul | Lados.Leste);
            // Tampa o vao sul-leste com uma parede extra (nao tem sala em x=+9 sul)
            PlaceWall(root.transform, AssetDatabase.LoadAssetAtPath<GameObject>(PathWall), new Vector3(6f, 0f, -1.5f - 6f), 0f);
            PlaceWall(root.transform, AssetDatabase.LoadAssetAtPath<GameObject>(PathWall), new Vector3(9f, 0f, -1.5f - 6f), 0f);
            PlaceWall(root.transform, AssetDatabase.LoadAssetAtPath<GameObject>(PathWall), new Vector3(12f, 0f, -1.5f - 6f), 0f);

            // Decoracao de cada sala (props dos pacotes school + MarpaStudio)
            DecorarSalaDeAula(root.transform, new Vector3(-9f, 0f, 4.5f), espelhar: false);
            DecorarSalaDeAula(root.transform, new Vector3(-3f, 0f, 4.5f), espelhar: true);
            DecorarBiblioteca(root.transform, new Vector3(3f, 0f, 4.5f));
            DecorarLaboratorio(root.transform, new Vector3(9f, 0f, 4.5f));
            DecorarBanheiro(root.transform, new Vector3(-9f, 0f, -4.5f));
            DecorarSecretaria(root.transform, new Vector3(-3f, 0f, -4.5f));
            DecorarRefeitorio(root.transform, new Vector3(3f, 0f, -4.5f));
            DecorarCorredor(root.transform);
            DecorarExterior(root.transform);

            BuildLuzes(root.transform);
            BuildMarcadorESpawns(root.transform);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = root;
            Debug.Log($"[{EscolaRootName}] Escola construida. Salve a cena (Ctrl+S) e de Play.");
        }

        [MenuItem(MenuRoot + "Limpar Escola")]
        public static void Limpar()
        {
            GameObject existing = GameObject.Find(EscolaRootName);
            if (existing == null) return;
            Undo.DestroyObjectImmediate(existing);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        // ===================== CORREDOR =====================
        static void BuildCorredor(Transform parent)
        {
            GameObject floor = AssetDatabase.LoadAssetAtPath<GameObject>(PathFloor);
            GameObject wall = AssetDatabase.LoadAssetAtPath<GameObject>(PathWall);
            GameObject wallCrack = AssetDatabase.LoadAssetAtPath<GameObject>(PathWallCrack) ?? wall;

            if (floor == null || wall == null)
            {
                Debug.LogError("Prefabs Backrooms nao encontrados. Confira os paths em MontadorDeEscola.");
                return;
            }

            Transform corredor = new GameObject("Corredor").transform;
            corredor.SetParent(parent, false);

            // 9 tiles em z=0, x = -12..+12 step 3 (27m)
            for (int i = 0; i < 9; i++)
            {
                float x = -12f + i * TileSize;
                PlaceFloor(corredor, floor, new Vector3(x, 0f, 0f));
                PlaceCeiling(corredor, floor, new Vector3(x, WallHeight, 0f));
            }

            // Norte: portas em x = -9, -3, +3, +9 (4 salas norte)
            HashSet<int> portasNorte = new() { -9, -3, 3, 9 };
            HashSet<int> portasSul = new() { -9, -3, 3 };

            for (int i = 0; i < 9; i++)
            {
                float x = -12f + i * TileSize;
                int xi = Mathf.RoundToInt(x);

                if (portasNorte.Contains(xi))
                    PlaceDoorOpening(corredor, new Vector3(x, 0f, HalfTile), 180f, norte: true);
                else
                    PlaceWall(corredor, i % 4 == 1 ? wallCrack : wall, new Vector3(x, 0f, HalfTile), 180f);

                if (portasSul.Contains(xi))
                    PlaceDoorOpening(corredor, new Vector3(x, 0f, -HalfTile), 0f, norte: false);
                else
                    PlaceWall(corredor, i % 4 == 2 ? wallCrack : wall, new Vector3(x, 0f, -HalfTile), 0f);
            }

            // Tampa as pontas leste/oeste
            PlaceWall(corredor, wall, new Vector3(13.5f, 0f, 0f), -90f);
            PlaceWall(corredor, wall, new Vector3(-13.5f, 0f, 0f), 90f);
        }

        // ===================== ESTRUTURA DE SALA =====================
        static void BuildSala(Transform parent, string nome, Vector3 centro, int tilesX, int tilesZ, Lados ladosParaConstruir)
        {
            GameObject floor = AssetDatabase.LoadAssetAtPath<GameObject>(PathFloor);
            GameObject wall = AssetDatabase.LoadAssetAtPath<GameObject>(PathWall);
            GameObject wallCrack = AssetDatabase.LoadAssetAtPath<GameObject>(PathWallCrack) ?? wall;

            Transform sala = new GameObject(nome).transform;
            sala.SetParent(parent, false);

            float startX = centro.x - (tilesX - 1) * 0.5f * TileSize;
            float startZ = centro.z - (tilesZ - 1) * 0.5f * TileSize;

            for (int ix = 0; ix < tilesX; ix++)
                for (int iz = 0; iz < tilesZ; iz++)
                {
                    Vector3 pos = new Vector3(startX + ix * TileSize, 0f, startZ + iz * TileSize);
                    PlaceFloor(sala, floor, pos);
                    PlaceCeiling(sala, floor, new Vector3(pos.x, WallHeight, pos.z));
                }

            float halfX = tilesX * 0.5f * TileSize;
            float halfZ = tilesZ * 0.5f * TileSize;

            if ((ladosParaConstruir & Lados.Norte) != 0)
                for (int ix = 0; ix < tilesX; ix++)
                {
                    GameObject p = (ix == tilesX / 2) ? wallCrack : wall;
                    PlaceWall(sala, p, new Vector3(startX + ix * TileSize, 0f, centro.z + halfZ), 180f);
                }

            if ((ladosParaConstruir & Lados.Sul) != 0)
                for (int ix = 0; ix < tilesX; ix++)
                    PlaceWall(sala, wall, new Vector3(startX + ix * TileSize, 0f, centro.z - halfZ), 0f);

            if ((ladosParaConstruir & Lados.Leste) != 0)
                for (int iz = 0; iz < tilesZ; iz++)
                    PlaceWall(sala, wall, new Vector3(centro.x + halfX, 0f, startZ + iz * TileSize), -90f);

            if ((ladosParaConstruir & Lados.Oeste) != 0)
                for (int iz = 0; iz < tilesZ; iz++)
                {
                    GameObject p = (iz == tilesZ / 2) ? wallCrack : wall;
                    PlaceWall(sala, p, new Vector3(centro.x - halfX, 0f, startZ + iz * TileSize), 90f);
                }
        }

        // ===================== HELPERS DE INSTANCIACAO =====================
        static void PlaceFloor(Transform parent, GameObject prefab, Vector3 worldPos)
        {
            if (prefab == null) return;
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            inst.transform.position = worldPos;
            AddBoxColliderFromMesh(inst);
        }

        static void PlaceCeiling(Transform parent, GameObject prefab, Vector3 worldPos)
        {
            if (prefab == null) return;
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            inst.transform.position = worldPos;
            inst.transform.rotation = Quaternion.Euler(180f, 0f, 0f);
            inst.name = inst.name.Replace("Floor", "Ceiling");
        }

        // Paredes usam MeshCollider para que recortes (porta, janela) sejam realmente
        // atravessaveis pelo player. BoxCollider tampava a porta inteira.
        static void PlaceWall(Transform parent, GameObject prefab, Vector3 worldPos, float yawDegrees)
        {
            if (prefab == null) return;
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            inst.transform.SetPositionAndRotation(worldPos, Quaternion.Euler(0f, yawDegrees, 0f));
            AddMeshColliderFromMesh(inst);
        }

        static void PlaceDoorOpening(Transform parent, Vector3 worldPos, float yawDegrees, bool norte)
        {
            GameObject marker = new GameObject("Porta_Larga_SemBloqueio");
            marker.transform.SetParent(parent, false);
            marker.transform.SetPositionAndRotation(worldPos, Quaternion.Euler(0f, yawDegrees, 0f));

            // A parede inteira e omitida aqui de proposito: a abertura fica com
            // um tile completo (3m), suficiente para o CharacterController.
            float visualYaw = norte ? 180f : 0f;
            float side = norte ? 1f : -1f;
            PlaceProp(parent, PathSchoolDoor, worldPos + new Vector3(-1.18f, 0f, 0.16f * side), visualYaw + 75f, collider: false, maxDim: 2.25f);
            PlaceProp(parent, PathSchoolDoorAlt, worldPos + new Vector3(1.18f, 0f, 0.16f * side), visualYaw - 75f, collider: false, maxDim: 2.25f);
        }

        static void AddBoxColliderFromMesh(GameObject go)
        {
            MeshFilter mf = go.GetComponentInChildren<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return;
            BoxCollider col = go.AddComponent<BoxCollider>();
            Bounds local = mf.sharedMesh.bounds;
            col.center = local.center;
            col.size = local.size;
        }

        static void AddMeshColliderFromMesh(GameObject go)
        {
            MeshFilter mf = go.GetComponentInChildren<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return;
            MeshCollider col = go.AddComponent<MeshCollider>();
            col.sharedMesh = mf.sharedMesh;
            col.convex = false;
        }

        // Instancia um prop e (opcionalmente) adiciona BoxCollider. Tambem clamp de
        // escala caso o prefab venha com unidade muito grande.
        static GameObject PlaceProp(Transform parent, string path, Vector3 pos, float yawDeg = 0f, bool collider = true, float maxDim = 4f)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) { Debug.LogWarning($"Prop nao encontrado: {path}"); return null; }
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            inst.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yawDeg, 0f));

            Renderer[] renderers = inst.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds combined = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) combined.Encapsulate(renderers[i].bounds);
                float maior = Mathf.Max(combined.size.x, combined.size.y, combined.size.z);
                if (maior > maxDim && maior > 0.0001f)
                {
                    float fator = maxDim / maior;
                    inst.transform.localScale = inst.transform.localScale * fator;
                    // Recalcular bounds apos escala
                    combined = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++) combined.Encapsulate(renderers[i].bounds);
                }

                if (collider && inst.GetComponentInChildren<Collider>() == null)
                {
                    BoxCollider c = inst.AddComponent<BoxCollider>();
                    Vector3 localCenter = inst.transform.InverseTransformPoint(combined.center);
                    Vector3 localSize = combined.size;
                    if (inst.transform.lossyScale.x != 0) localSize.x /= inst.transform.lossyScale.x;
                    if (inst.transform.lossyScale.y != 0) localSize.y /= inst.transform.lossyScale.y;
                    if (inst.transform.lossyScale.z != 0) localSize.z /= inst.transform.lossyScale.z;
                    c.center = localCenter;
                    c.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
                }
            }
            return inst;
        }

        // ===================== DECORACOES POR SALA =====================
        static void DecorarSalaDeAula(Transform parent, Vector3 centro, bool espelhar)
        {
            float sgn = espelhar ? -1f : 1f;
            float yawQuadro = 180f; // quadro vira para o sul (interior da sala)

            // Quadro na parede norte
            PlaceProp(parent, PathSchoolBoard, centro + new Vector3(0f, 1.4f, 2.6f), yawQuadro, collider: false, maxDim: 4f);
            PlaceProp(parent, PathSchoolBoardAlt, centro + new Vector3(-2.1f, 1.15f, 2.55f), yawQuadro, collider: false, maxDim: 1.4f);

            // Mesa do professor centralizada na frente (sul do quadro)
            PlaceProp(parent, PathSchoolTablePro, centro + new Vector3(0f, 0f, 1.55f), yawQuadro, maxDim: 2.2f);
            PlaceProp(parent, PathSchoolLaptop, centro + new Vector3(-0.35f, 0.86f, 1.45f), yawQuadro + 8f, collider: false, maxDim: 0.8f);
            PlaceProp(parent, PathSchoolSheet, centro + new Vector3(0.55f, 0.84f, 1.42f), yawQuadro - 12f, collider: false, maxDim: 0.45f);

            // Cadeira do professor atras da mesa
            PlaceProp(parent, PathSchoolChair, centro + new Vector3(0.1f, 0f, 2.18f), yawQuadro + 5f, maxDim: 1.15f);

            // Carteiras laterais, com corredor central livre da porta ate a bomba/mesa.
            float[] colunas = { -1.85f, 1.85f };
            for (int linha = 0; linha < 2; linha++)
            {
                for (int coluna = 0; coluna < colunas.Length; coluna++)
                {
                    Vector3 carteiraPos = centro + new Vector3(colunas[coluna], 0f, 0.1f - linha * 1.55f);
                    PlaceProp(parent, PathSchoolTableAluno, carteiraPos, yawQuadro, maxDim: 1.55f);
                    PlaceProp(parent, PathSchoolChair2, carteiraPos + new Vector3(0f, 0f, -0.65f), yawQuadro, maxDim: 1.2f);
                }
            }

            // Locker no canto
            PlaceProp(parent, PathSchoolLocker, centro + new Vector3(sgn * 2.4f, 0f, 2.4f), 90f * (espelhar ? -1f : 1f), maxDim: 2.4f);

            // Objetos pequenos dao vida a sala sem bloquear a entrada.
            PlaceProp(parent, PathSchoolBook, centro + new Vector3(-1.85f, 0.82f, 0.1f), -14f, collider: false, maxDim: 0.38f);
            PlaceProp(parent, PathSchoolBook2, centro + new Vector3(1.85f, 0.82f, -1.45f), 22f, collider: false, maxDim: 0.38f);
            PlaceProp(parent, PathSchoolBook3, centro + new Vector3(-1.85f, 0.82f, -1.45f), 8f, collider: false, maxDim: 0.38f);
            PlaceProp(parent, PathSchoolSheet2, centro + new Vector3(1.85f, 0.83f, 0.08f), -18f, collider: false, maxDim: 0.42f);
            PlaceProp(parent, PathSchoolTray, centro + new Vector3(0.85f, 0.82f, 1.55f), -8f, collider: false, maxDim: 0.45f);
            PlaceProp(parent, PathSchoolSpeaker, centro + new Vector3(-2.45f, 2.35f, 2.55f), 180f, collider: false, maxDim: 0.45f);
            PlaceProp(parent, PathSchoolJalousie, centro + new Vector3(2.65f, 1.55f, 0.9f), -90f, collider: false, maxDim: 1.6f);
            PlaceProp(parent, PathSchoolRack, centro + new Vector3(-2.55f, 0f, -1.8f), 90f, maxDim: 2.15f);

            // Detector de fumaca / alarme no teto
            PlaceProp(parent, PathOfficeSmoke, centro + new Vector3(0f, 2.95f, 0f), 0f, collider: false, maxDim: 0.6f);
        }

        static void DecorarBiblioteca(Transform parent, Vector3 centro)
        {
            // 4 estantes contra parede leste/oeste
            for (int i = 0; i < 2; i++)
            {
                float z = -1f + i * 2f;
                PlaceProp(parent, PathSchoolRack, centro + new Vector3(2.4f, 0f, z), -90f, maxDim: 3f);
                PlaceProp(parent, PathSchoolRack2, centro + new Vector3(-2.4f, 0f, z), 90f, maxDim: 3f);
            }

            // Pilhas de livros sobre as estantes
            PlaceProp(parent, PathSchoolBook, centro + new Vector3(2.2f, 1.2f, -1f), 30f, collider: false, maxDim: 0.6f);
            PlaceProp(parent, PathSchoolBook2, centro + new Vector3(-2.2f, 1.2f, 1f), -20f, collider: false, maxDim: 0.6f);
            PlaceProp(parent, PathSchoolBook3, centro + new Vector3(2.2f, 1.2f, 1f), 10f, collider: false, maxDim: 0.6f);

            // Mesa de leitura central com cadeiras
            PlaceProp(parent, PathSchoolTableLong, centro + new Vector3(0f, 0f, 0f), 90f);
            PlaceProp(parent, PathSchoolChair, centro + new Vector3(0f, 0f, 0.8f), 0f);
            PlaceProp(parent, PathSchoolChair, centro + new Vector3(0f, 0f, -0.8f), 180f);

            // Livros sobre a mesa
            PlaceProp(parent, PathSchoolBook2, centro + new Vector3(-0.4f, 0.8f, 0f), 15f, collider: false, maxDim: 0.4f);
            PlaceProp(parent, PathSchoolBook3, centro + new Vector3(0.5f, 0.8f, 0.1f), -10f, collider: false, maxDim: 0.4f);
        }

        static void DecorarLaboratorio(Transform parent, Vector3 centro)
        {
            // Bancada com computadores na parede norte
            PlaceProp(parent, PathSchoolTablePro, centro + new Vector3(-1.2f, 0f, 1.6f), 180f);
            PlaceProp(parent, PathSchoolTablePro, centro + new Vector3(1.2f, 0f, 1.6f), 180f);
            PlaceProp(parent, PathSchoolComputer, centro + new Vector3(-1.2f, 0.85f, 1.4f), 180f, collider: false, maxDim: 0.9f);
            PlaceProp(parent, PathSchoolComputer2, centro + new Vector3(1.2f, 0.85f, 1.4f), 180f, collider: false, maxDim: 0.9f);

            // Showcase no canto
            PlaceProp(parent, PathSchoolShowcase, centro + new Vector3(-2.2f, 0f, -1.5f), 90f, maxDim: 2.5f);

            // Projector pendurado (ou no teto)
            PlaceProp(parent, PathSchoolProjector, centro + new Vector3(0f, 2.6f, 0.5f), 0f, collider: false, maxDim: 1.2f);

            // Mesa central com cadeiras
            PlaceProp(parent, PathSchoolTableLong, centro + new Vector3(0f, 0f, -0.8f), 0f);
            PlaceProp(parent, PathSchoolChair2, centro + new Vector3(-0.7f, 0f, -1.5f), 0f);
            PlaceProp(parent, PathSchoolChair2, centro + new Vector3(0.7f, 0f, -1.5f), 0f);

            // Extintor no canto
            PlaceProp(parent, "Assets/MarpaStudio/Built-In/Prefabs/Fireextinguisher.prefab", centro + new Vector3(2.3f, 0f, 2.2f), 0f, collider: false, maxDim: 0.8f);
        }

        static void DecorarBanheiro(Transform parent, Vector3 centro)
        {
            // Banheiro propositadamente vazio para gerar tensao com a luz piscante.
            // Apenas alguns elementos pequenos.
            PlaceProp(parent, PathOfficeTrash, centro + new Vector3(2.2f, 0f, 2.2f), 0f, maxDim: 0.8f);
            PlaceProp(parent, PathOfficeTrash, centro + new Vector3(-2.2f, 0f, -2.2f), 0f, maxDim: 0.8f);

            // Espelho fake (showcase rotacionado contra parede)
            PlaceProp(parent, PathSchoolShowcase, centro + new Vector3(0f, 0f, -2.4f), 0f, maxDim: 2.5f);
        }

        static void DecorarSecretaria(Transform parent, Vector3 centro)
        {
            // Mesa de escritorio com PC e telefone
            PlaceProp(parent, PathOfficeDesk, centro + new Vector3(0f, 0f, -1.4f), 0f, maxDim: 2.5f);
            PlaceProp(parent, PathOfficeMonitor, centro + new Vector3(-0.4f, 0.8f, -1.5f), 10f, collider: false, maxDim: 0.7f);
            PlaceProp(parent, PathOfficeKeyboard, centro + new Vector3(-0.4f, 0.78f, -1.1f), 0f, collider: false, maxDim: 0.6f);
            PlaceProp(parent, PathOfficeMouse, centro + new Vector3(0.0f, 0.78f, -1.1f), 0f, collider: false, maxDim: 0.2f);
            PlaceProp(parent, PathOfficePhone, centro + new Vector3(0.7f, 0.78f, -1.3f), -30f, collider: false, maxDim: 0.4f);
            PlaceProp(parent, PathOfficeLamp, centro + new Vector3(-0.9f, 0.78f, -1.7f), 0f, collider: false, maxDim: 0.7f);
            PlaceProp(parent, PathOfficeFoto, centro + new Vector3(0.4f, 0.78f, -1.7f), -10f, collider: false, maxDim: 0.4f);
            PlaceProp(parent, PathOfficeBinder, centro + new Vector3(-0.6f, 0.78f, -1.9f), 0f, collider: false, maxDim: 0.4f);

            // Cadeira do escritorio atras da mesa
            PlaceProp(parent, PathOfficeChair, centro + new Vector3(0f, 0f, -0.5f), 180f, maxDim: 1.6f);

            // Arquivo encostado na parede
            PlaceProp(parent, PathOfficeArchive, centro + new Vector3(2.2f, 0f, 1.6f), -90f, maxDim: 2.5f);
            PlaceProp(parent, PathOfficeArchive, centro + new Vector3(2.2f, 0f, 0.4f), -90f, maxDim: 2.5f);

            // Armario no canto oeste
            PlaceProp(parent, PathOfficeCloset, centro + new Vector3(-2.2f, 0f, 1.6f), 90f, maxDim: 2.5f);

            // Relogio na parede
            PlaceProp(parent, PathOfficeClock, centro + new Vector3(0f, 2.4f, 2.6f), 0f, collider: false, maxDim: 0.6f);

            // Lixeira ao lado da mesa
            PlaceProp(parent, PathOfficeTrash, centro + new Vector3(1.4f, 0f, -1.7f), 0f, maxDim: 0.8f);
        }

        static void DecorarRefeitorio(Transform parent, Vector3 centro)
        {
            // 2 mesas longas paralelas com cadeiras dos dois lados
            for (int i = 0; i < 2; i++)
            {
                float x = -1.2f + i * 2.4f;
                PlaceProp(parent, PathSchoolTableLong, centro + new Vector3(x, 0f, 0f), 0f);
                PlaceProp(parent, PathSchoolChair, centro + new Vector3(x, 0f, 0.9f), 0f);
                PlaceProp(parent, PathSchoolChair, centro + new Vector3(x, 0f, -0.9f), 180f);
                PlaceProp(parent, PathSchoolChair2, centro + new Vector3(x - 0.6f, 0f, 0.9f), 0f);
                PlaceProp(parent, PathSchoolChair2, centro + new Vector3(x + 0.6f, 0f, -0.9f), 180f);
                PlaceProp(parent, PathSchoolTray, centro + new Vector3(x, 0.78f, 0f), 0f, collider: false, maxDim: 0.5f);
            }

            // Speaker no canto
            PlaceProp(parent, PathSchoolSpeaker, centro + new Vector3(0f, 2.5f, 2.6f), 180f, collider: false, maxDim: 0.7f);
        }

        static void DecorarCorredor(Transform parent)
        {
            Transform corredorDeco = new GameObject("CorredorDecoracao").transform;
            corredorDeco.SetParent(parent, false);

            // Lockers nas pontas do corredor (entre as portas)
            float[] xLockers = { -12f, 0f, 12f };
            foreach (float x in xLockers)
            {
                PlaceProp(corredorDeco, PathSchoolLocker1, new Vector3(x, 0f, 1.1f), 180f, maxDim: 2.4f);
                PlaceProp(corredorDeco, PathSchoolLocker2, new Vector3(x, 0f, -1.1f), 0f, maxDim: 2.4f);
            }

            PlaceProp(corredorDeco, PathSchoolLocker, new Vector3(-6f, 0f, 1.1f), 180f, maxDim: 2.4f);
            PlaceProp(corredorDeco, PathSchoolLocker, new Vector3(6f, 0f, -1.1f), 0f, maxDim: 2.4f);

            // Placas de saida em ambas as pontas do corredor
            PlaceProp(corredorDeco, PathOfficeExitSign, new Vector3(13f, 2.5f, 0f), -90f, collider: false, maxDim: 0.8f);
            PlaceProp(corredorDeco, PathOfficeExitSign, new Vector3(-13f, 2.5f, 0f), 90f, collider: false, maxDim: 0.8f);

            // Alarme de incendio
            PlaceProp(corredorDeco, PathOfficeFireAlarm, new Vector3(-3f, 2.2f, 1.4f), 180f, collider: false, maxDim: 0.4f);
            PlaceProp(corredorDeco, PathOfficeFireAlarm, new Vector3(3f, 2.2f, -1.4f), 0f, collider: false, maxDim: 0.4f);

            // Detectores de fumaca espalhados
            for (int i = -2; i <= 2; i++)
                PlaceProp(corredorDeco, PathOfficeSmoke, new Vector3(i * 6f, 2.95f, 0f), 0f, collider: false, maxDim: 0.5f);
        }

        static void DecorarExterior(Transform parent)
        {
            Transform exterior = new GameObject("ExteriorEscola").transform;
            exterior.SetParent(parent, false);

            for (int x = -2; x <= 2; x++)
            {
                PlaceProp(exterior, PathSchoolOutdoorFloor, new Vector3(x * 3f, -0.03f, -11.2f), 0f, collider: false, maxDim: 3.2f);
                PlaceProp(exterior, PathSchoolRoad, new Vector3(x * 3f, -0.02f, -14.2f), 90f, collider: false, maxDim: 3.2f);
            }

            PlaceProp(exterior, PathSchoolRoadCross, new Vector3(-9f, -0.02f, -14.2f), 90f, collider: false, maxDim: 3.2f);
            PlaceProp(exterior, PathSchoolRoadCross, new Vector3(9f, -0.02f, -14.2f), 90f, collider: false, maxDim: 3.2f);
            PlaceProp(exterior, PathSchoolBus, new Vector3(-9f, 0f, -16.6f), 90f, collider: false, maxDim: 5.8f);

            // Pequenos objetos funcionais perto da entrada, fora da rota jogavel.
            PlaceProp(exterior, PathSchoolLocker, new Vector3(11.6f, 0f, -10.7f), -90f, maxDim: 2.2f);
            PlaceProp(exterior, PathOfficeTrash, new Vector3(12.6f, 0f, -8.8f), 0f, maxDim: 0.7f);
            PlaceProp(exterior, PathOfficeExitSign, new Vector3(0f, 2.5f, -1.42f), 0f, collider: false, maxDim: 0.8f);
        }

        // ===================== ILUMINACAO =====================
        static void BuildLuzes(Transform parent)
        {
            Transform luzes = new GameObject("Luzes").transform;
            luzes.SetParent(parent, false);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.04f, 0.05f, 0.06f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.02f, 0.025f, 0.035f);
            RenderSettings.fogStartDistance = 6f;
            RenderSettings.fogEndDistance = 30f;

            // Corredor: 5 lampadas, 2 piscam
            for (int i = -2; i <= 2; i++)
            {
                bool pisca = (i == -1 || i == 2);
                Color cor = i == 2 ? new Color(1f, 0.55f, 0.4f) : new Color(1f, 0.85f, 0.55f);
                CriarPointLight(luzes, $"Corredor_Lampada_{i}", new Vector3(i * 6f, 2.7f, 0f), cor, 0.8f, 6.5f, pisca);
            }

            // Lampada vermelha de aviso ao fundo
            CriarPointLight(luzes, "AvisoVermelho_Leste", new Vector3(13f, 2.5f, 0f), new Color(1f, 0.18f, 0.12f), 0.45f, 4f, false);
            CriarPointLight(luzes, "AvisoVermelho_Oeste", new Vector3(-13f, 2.5f, 0f), new Color(1f, 0.18f, 0.12f), 0.45f, 4f, false);

            // Salas norte
            CriarPointLight(luzes, "SalaDeAula_Lampada", new Vector3(-9f, 2.7f, 4.5f), new Color(1f, 0.9f, 0.7f), 1f, 7f, false);
            CriarPointLight(luzes, "SalaDeAula_02_Lampada", new Vector3(-3f, 2.7f, 4.5f), new Color(1f, 0.9f, 0.7f), 0.85f, 7f, true);
            CriarPointLight(luzes, "Biblioteca_Lampada", new Vector3(3f, 2.7f, 4.5f), new Color(0.9f, 0.85f, 1f), 0.8f, 7f, false);
            CriarPointLight(luzes, "Laboratorio_Lampada", new Vector3(9f, 2.7f, 4.5f), new Color(0.6f, 0.95f, 0.85f), 0.85f, 7f, false);

            // Salas sul
            CriarPointLight(luzes, "Banheiro_Lampada", new Vector3(-9f, 2.7f, -4.5f), new Color(0.7f, 0.9f, 1f), 0.55f, 7f, true);
            CriarPointLight(luzes, "Secretaria_Lampada", new Vector3(-3f, 2.7f, -4.5f), new Color(1f, 0.85f, 0.6f), 0.95f, 7f, false);
            CriarPointLight(luzes, "Refeitorio_Lampada", new Vector3(3f, 2.7f, -4.5f), new Color(1f, 0.75f, 0.55f), 0.85f, 7f, false);
        }

        static void CriarPointLight(Transform parent, string nome, Vector3 pos, Color cor, float intensidade, float range, bool piscar)
        {
            GameObject go = new GameObject(nome);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            Light l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = cor;
            l.intensity = intensidade;
            l.range = range;
            l.shadows = LightShadows.Soft;
            if (piscar) go.AddComponent<LuzPiscando>();
        }

        // ===================== MARCADOR + SPAWNS =====================
        static void BuildMarcadorESpawns(Transform parent)
        {
            GameObject markerGO = new GameObject("EscolaPrefabricada");
            markerGO.transform.SetParent(parent, false);
            EscolaPrefabricada marker = markerGO.AddComponent<EscolaPrefabricada>();

            GameObject spawnGO = new GameObject("PlayerSpawn");
            spawnGO.transform.SetParent(parent, false);
            spawnGO.transform.SetPositionAndRotation(new Vector3(0f, 1f, 0f), Quaternion.identity);

            SerializedObject so = new SerializedObject(marker);
            SerializedProperty prop = so.FindProperty("pontoDeSpawnDoJogador");
            if (prop != null)
            {
                prop.objectReferenceValue = spawnGO.transform;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            Transform spawnRoot = new GameObject("BombSpawnPoints").transform;
            spawnRoot.SetParent(parent, false);

            CriarSpawnDeBomba(spawnRoot, "sala_de_aula_mesa", LocalDaEscola.SalaDeAula, new Vector3(-9f, 0.85f, 6.1f), Quaternion.Euler(0f, 180f, 0f), new Color(0.95f, 0.58f, 0.24f));
            CriarSpawnDeBomba(spawnRoot, "sala_de_aula_carteira", LocalDaEscola.SalaDeAula, new Vector3(-7.5f, 0.85f, 3f), Quaternion.Euler(0f, 0f, 0f), new Color(0.95f, 0.58f, 0.24f));
            CriarSpawnDeBomba(spawnRoot, "sala_de_aula_02_mesa", LocalDaEscola.SalaDeAula, new Vector3(-3f, 0.85f, 6.1f), Quaternion.Euler(0f, 180f, 0f), new Color(0.95f, 0.58f, 0.24f));

            CriarSpawnDeBomba(spawnRoot, "biblioteca_mesa", LocalDaEscola.Biblioteca, new Vector3(3f, 0.85f, 4.5f), Quaternion.Euler(0f, 90f, 0f), new Color(0.34f, 0.72f, 0.36f));
            CriarSpawnDeBomba(spawnRoot, "biblioteca_estante", LocalDaEscola.Biblioteca, new Vector3(5.2f, 0.1f, 5.5f), Quaternion.Euler(0f, -90f, 0f), new Color(0.34f, 0.72f, 0.36f));

            CriarSpawnDeBomba(spawnRoot, "laboratorio_bancada", LocalDaEscola.Laboratorio, new Vector3(7.8f, 0.85f, 6.1f), Quaternion.Euler(0f, 180f, 0f), new Color(0.28f, 0.58f, 0.95f));
            CriarSpawnDeBomba(spawnRoot, "laboratorio_mesa", LocalDaEscola.Laboratorio, new Vector3(9f, 0.85f, 3.7f), Quaternion.Euler(0f, 0f, 0f), new Color(0.28f, 0.58f, 0.95f));

            CriarSpawnDeBomba(spawnRoot, "banheiro_canto", LocalDaEscola.Banheiro, new Vector3(-10.5f, 0.1f, -2.7f), Quaternion.Euler(0f, 0f, 0f), new Color(0.25f, 0.75f, 0.84f));
            CriarSpawnDeBomba(spawnRoot, "banheiro_pia", LocalDaEscola.Banheiro, new Vector3(-9f, 0.85f, -6.9f), Quaternion.Euler(0f, 0f, 0f), new Color(0.25f, 0.75f, 0.84f));

            CriarSpawnDeBomba(spawnRoot, "secretaria_mesa", LocalDaEscola.Secretaria, new Vector3(-3f, 0.85f, -5.9f), Quaternion.Euler(0f, 0f, 0f), new Color(0.96f, 0.72f, 0.24f));
            CriarSpawnDeBomba(spawnRoot, "secretaria_arquivo", LocalDaEscola.Secretaria, new Vector3(-1.2f, 0.1f, -3f), Quaternion.Euler(0f, -90f, 0f), new Color(0.96f, 0.72f, 0.24f));

            CriarSpawnDeBomba(spawnRoot, "refeitorio_mesa1", LocalDaEscola.Refeitorio, new Vector3(1.8f, 0.85f, -4.5f), Quaternion.Euler(0f, 90f, 0f), new Color(0.94f, 0.34f, 0.24f));
            CriarSpawnDeBomba(spawnRoot, "refeitorio_mesa2", LocalDaEscola.Refeitorio, new Vector3(4.2f, 0.85f, -4.5f), Quaternion.Euler(0f, -90f, 0f), new Color(0.94f, 0.34f, 0.24f));

            CriarSpawnDeBomba(spawnRoot, "corredor_armarios_oeste", LocalDaEscola.Corredor, new Vector3(-12f, 0.1f, 0.8f), Quaternion.Euler(0f, 180f, 0f), new Color(0.88f, 0.6f, 0.94f));
            CriarSpawnDeBomba(spawnRoot, "corredor_centro", LocalDaEscola.Corredor, new Vector3(0f, 0.1f, 0f), Quaternion.Euler(0f, 90f, 0f), new Color(0.88f, 0.6f, 0.94f));
            CriarSpawnDeBomba(spawnRoot, "corredor_armarios_leste", LocalDaEscola.Corredor, new Vector3(12f, 0.1f, -0.8f), Quaternion.Euler(0f, 0f, 0f), new Color(0.88f, 0.6f, 0.94f));
        }

        static void CriarSpawnDeBomba(Transform parent, string id, LocalDaEscola local, Vector3 pos, Quaternion rot, Color cor)
        {
            GameObject go = new GameObject($"Spawn_{id}");
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(pos, rot);
            PontoDeSpawnDaBomba ponto = go.AddComponent<PontoDeSpawnDaBomba>();
            ponto.ConfigurarRuntime(id, local, null, true, cor);
        }
    }
}
