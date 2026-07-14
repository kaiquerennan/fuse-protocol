using System.Collections.Generic;
using UnityEngine;

namespace LiveWire
{
    public partial class GameSceneBootstrap
    {
        enum LadoAbertoDaSala
        {
            Norte,
            Sul,
            Leste,
            Oeste
        }

        void BuildSchool()
        {
            Material pisoExternoMat = SceneBuildHelpers.MakeMat(new Color(0.58f, 0.62f, 0.66f), 0.18f);
            Material pisoCorredorMat = SceneBuildHelpers.MakeMat(new Color(0.82f, 0.84f, 0.8f), 0.08f);
            Material paredeMat = SceneBuildHelpers.MakeMat(new Color(0.92f, 0.9f, 0.84f), 0.05f);
            Material tetoMat = SceneBuildHelpers.MakeMat(new Color(0.97f, 0.97f, 0.95f), 0.02f);
            Material madeiraMat = SceneBuildHelpers.MakeMat(new Color(0.5f, 0.35f, 0.2f), 0.16f);
            Material metalMat = SceneBuildHelpers.MakeMat(new Color(0.6f, 0.67f, 0.72f), 0.62f);
            Material quadroMat = SceneBuildHelpers.MakeMat(new Color(0.09f, 0.28f, 0.17f), 0.1f);
            Material livroMat = SceneBuildHelpers.MakeMat(new Color(0.47f, 0.2f, 0.14f), 0.2f);
            Material azulejoMat = SceneBuildHelpers.MakeMat(new Color(0.86f, 0.92f, 0.94f), 0.04f);
            Material refeitorioMat = SceneBuildHelpers.MakeMat(new Color(0.75f, 0.7f, 0.64f), 0.1f);
            Material quadraMat = SceneBuildHelpers.MakeMat(new Color(0.22f, 0.49f, 0.34f), 0.12f);
            Material linhaQuadraMat = SceneBuildHelpers.MakeMat(new Color(0.97f, 0.98f, 0.98f), 0.02f);

            Transform root = new GameObject("Escola").transform;

            SceneBuildHelpers.CreateBox(
                "PisoExterno",
                new Vector3(0f, -0.1f, 0f),
                new Vector3(RoomHalfWidth * 2f, 0.2f, RoomHalfDepth * 2f),
                pisoExternoMat,
                root);

            SceneBuildHelpers.CreateBox(
                "CorredorPrincipal",
                new Vector3(0f, 0.01f, -0.75f),
                new Vector3(50f, 0.06f, 12f),
                pisoCorredorMat,
                root,
                collider: false);

            SceneBuildHelpers.CreateBox(
                "CorredorNorte",
                new Vector3(0f, 0.01f, 10f),
                new Vector3(8f, 0.06f, 18f),
                pisoCorredorMat,
                root,
                collider: false);

            CreatePerimeter(root, paredeMat);

            Transform sala = CreateRoom(
                root,
                "SalaDeAula",
                new Vector3(-18f, 0f, 9f),
                new Vector2(12f, 8.5f),
                LadoAbertoDaSala.Sul,
                SceneBuildHelpers.MakeMat(new Color(0.86f, 0.79f, 0.66f), 0.08f),
                paredeMat,
                tetoMat,
                new Color(0.77f, 0.38f, 0.22f));

            Transform biblioteca = CreateRoom(
                root,
                "Biblioteca",
                new Vector3(0f, 0f, 9f),
                new Vector2(12f, 8.5f),
                LadoAbertoDaSala.Sul,
                SceneBuildHelpers.MakeMat(new Color(0.7f, 0.58f, 0.44f), 0.08f),
                paredeMat,
                tetoMat,
                new Color(0.29f, 0.48f, 0.26f));

            Transform laboratorio = CreateRoom(
                root,
                "Laboratorio",
                new Vector3(18f, 0f, 9f),
                new Vector2(12f, 8.5f),
                LadoAbertoDaSala.Sul,
                SceneBuildHelpers.MakeMat(new Color(0.76f, 0.84f, 0.88f), 0.06f),
                paredeMat,
                tetoMat,
                new Color(0.19f, 0.46f, 0.67f));

            Transform banheiro = CreateRoom(
                root,
                "Banheiro",
                new Vector3(-18f, 0f, -10f),
                new Vector2(12f, 7.5f),
                LadoAbertoDaSala.Norte,
                azulejoMat,
                paredeMat,
                tetoMat,
                new Color(0.24f, 0.62f, 0.68f));

            Transform secretaria = CreateRoom(
                root,
                "Secretaria",
                new Vector3(0f, 0f, -10f),
                new Vector2(12f, 7.5f),
                LadoAbertoDaSala.Norte,
                SceneBuildHelpers.MakeMat(new Color(0.79f, 0.75f, 0.68f), 0.08f),
                paredeMat,
                tetoMat,
                new Color(0.76f, 0.45f, 0.16f));

            Transform refeitorio = CreateRoom(
                root,
                "Refeitorio",
                new Vector3(18f, 0f, -10f),
                new Vector2(13f, 7.5f),
                LadoAbertoDaSala.Norte,
                refeitorioMat,
                paredeMat,
                tetoMat,
                new Color(0.79f, 0.2f, 0.18f));

            PopulateCorridor(root, metalMat, madeiraMat);
            PopulateClassroom(sala, new Vector3(-18f, 0f, 9f), madeiraMat, metalMat, quadroMat, livroMat);
            PopulateLibrary(biblioteca, new Vector3(0f, 0f, 9f), madeiraMat, metalMat, livroMat, quadroMat);
            PopulateLaboratory(laboratorio, new Vector3(18f, 0f, 9f), metalMat, madeiraMat);
            PopulateBathroom(banheiro, new Vector3(-18f, 0f, -10f), azulejoMat, metalMat);
            PopulateSecretary(secretaria, new Vector3(0f, 0f, -10f), madeiraMat, metalMat);
            PopulateCafeteria(refeitorio, new Vector3(18f, 0f, -10f), madeiraMat, metalMat);
            PopulateCourt(root, new Vector3(0f, 0f, 22f), quadraMat, linhaQuadraMat, metalMat);

            Transform spawnRoot = new GameObject("BombSpawnPoints").transform;
            spawnRoot.SetParent(root, false);

            CreateBombSpawnPoint(spawnRoot, "sala_professor_01", LocalDaEscola.SalaDeAula, new Vector3(-18f, 0.08f, 11.8f), Quaternion.Euler(0f, 180f, 0f));
            CreateBombSpawnPoint(spawnRoot, "sala_fundo_01", LocalDaEscola.SalaDeAula, new Vector3(-21.5f, 0.08f, 7.4f), Quaternion.Euler(0f, 60f, 0f));
            CreateBombSpawnPoint(spawnRoot, "biblioteca_estante_01", LocalDaEscola.Biblioteca, new Vector3(-1.8f, 0.08f, 11.6f), Quaternion.Euler(0f, 180f, 0f));
            CreateBombSpawnPoint(spawnRoot, "biblioteca_leitura_01", LocalDaEscola.Biblioteca, new Vector3(2.4f, 0.08f, 7.2f), Quaternion.Euler(0f, -50f, 0f));
            CreateBombSpawnPoint(spawnRoot, "laboratorio_bancada_01", LocalDaEscola.Laboratorio, new Vector3(18.1f, 0.08f, 11.4f), Quaternion.Euler(0f, 180f, 0f));
            CreateBombSpawnPoint(spawnRoot, "laboratorio_armario_01", LocalDaEscola.Laboratorio, new Vector3(21.2f, 0.08f, 7.2f), Quaternion.Euler(0f, 135f, 0f));
            CreateBombSpawnPoint(spawnRoot, "banheiro_pia_01", LocalDaEscola.Banheiro, new Vector3(-20.8f, 0.08f, -11.8f), Quaternion.Euler(0f, 0f, 0f));
            CreateBombSpawnPoint(spawnRoot, "banheiro_cabine_01", LocalDaEscola.Banheiro, new Vector3(-16.2f, 0.08f, -8.2f), Quaternion.Euler(0f, 180f, 0f));
            CreateBombSpawnPoint(spawnRoot, "refeitorio_mesa_01", LocalDaEscola.Refeitorio, new Vector3(18f, 0.08f, -9.4f), Quaternion.Euler(0f, 180f, 0f));
            CreateBombSpawnPoint(spawnRoot, "refeitorio_janela_01", LocalDaEscola.Refeitorio, new Vector3(22f, 0.08f, -12f), Quaternion.Euler(0f, 120f, 0f));
            CreateBombSpawnPoint(spawnRoot, "quadra_bancada_01", LocalDaEscola.Quadra, new Vector3(-6.5f, 0.08f, 25.5f), Quaternion.Euler(0f, 90f, 0f));
            CreateBombSpawnPoint(spawnRoot, "quadra_lateral_01", LocalDaEscola.Quadra, new Vector3(7.8f, 0.08f, 18.6f), Quaternion.Euler(0f, -90f, 0f));
            CreateBombSpawnPoint(spawnRoot, "corredor_armarios_01", LocalDaEscola.Corredor, new Vector3(-11f, 0.08f, -0.8f), Quaternion.Euler(0f, 90f, 0f));
            CreateBombSpawnPoint(spawnRoot, "corredor_cruzamento_01", LocalDaEscola.Corredor, new Vector3(0f, 0.08f, 6.2f), Quaternion.Euler(0f, 180f, 0f));
            CreateBombSpawnPoint(spawnRoot, "secretaria_balcao_01", LocalDaEscola.Secretaria, new Vector3(-1f, 0.08f, -8.3f), Quaternion.Euler(0f, 0f, 0f));
            CreateBombSpawnPoint(spawnRoot, "secretaria_arquivo_01", LocalDaEscola.Secretaria, new Vector3(3.2f, 0.08f, -11.6f), Quaternion.Euler(0f, 180f, 0f));

            BuildSchoolLights(root);
        }

        void EnsureSchoolPhaseRuntime(BombManager bomb)
        {
            IntegracaoBombaFase integracao = FindAnyObjectByType<IntegracaoBombaFase>();
            if (integracao == null)
            {
                GameObject go = new GameObject("IntegracaoBombaFase");
                integracao = go.AddComponent<IntegracaoBombaFase>();
            }

            integracao.Configurar(bomb, localizarAutomaticamente: bomb == null);

            GerenciadorDeFase gerenciador = GerenciadorDeFase.Instance;
            if (gerenciador == null)
            {
                GameObject go = new GameObject("GerenciadorDeFase");
                gerenciador = go.AddComponent<GerenciadorDeFase>();
            }

            if (!gerenciador.TemFasesConfiguradas)
            {
                List<DadosDaFaseDaEscola> fases = escolaPrefabricada != null
                    ? CreatePhasesFromSceneSpawns()
                    : CreateDefaultSchoolPhases();

                gerenciador.ConfigurarCampanhaEmRuntime(
                    fases,
                    gerenciadorDeObjetivoEscola,
                    iniciarAgora: false);
            }

            if (!gerenciador.CampanhaAtiva)
                gerenciador.IniciarCampanha();
        }

        List<DadosDaFaseDaEscola> CreateDefaultSchoolPhases()
        {
            return new List<DadosDaFaseDaEscola>
            {
                CreateRuntimePhase("fase_01_sala", "Sala de Aula", LocalDaEscola.SalaDeAula, ModoDeObjetivoDaFase.Explicito, "", "sala_professor_01", "sala_fundo_01"),
                CreateRuntimePhase("fase_02_biblioteca", "Biblioteca", LocalDaEscola.Biblioteca, ModoDeObjetivoDaFase.Explicito, "", "biblioteca_estante_01", "biblioteca_leitura_01"),
                CreateRuntimePhase("fase_03_laboratorio", "Laboratorio", LocalDaEscola.Laboratorio, ModoDeObjetivoDaFase.Explicito, "", "laboratorio_bancada_01", "laboratorio_armario_01"),
                CreateRuntimePhase("fase_04_banheiro", "Banheiro", LocalDaEscola.Banheiro, ModoDeObjetivoDaFase.Explicito, "", "banheiro_pia_01", "banheiro_cabine_01"),
                CreateRuntimePhase("fase_05_refeitorio", "Refeitorio", LocalDaEscola.Refeitorio, ModoDeObjetivoDaFase.Explicito, "", "refeitorio_mesa_01", "refeitorio_janela_01"),
                CreateRuntimePhase("fase_06_quadra", "Quadra", LocalDaEscola.Quadra, ModoDeObjetivoDaFase.Explicito, "", "quadra_bancada_01", "quadra_lateral_01"),
                CreateRuntimePhase("fase_07_corredor", "Corredor", LocalDaEscola.Corredor, ModoDeObjetivoDaFase.Oculto, "", "corredor_armarios_01", "corredor_cruzamento_01"),
                CreateRuntimePhase("fase_08_secretaria", "Secretaria", LocalDaEscola.Secretaria, ModoDeObjetivoDaFase.Personalizado, "A secretaria esta silenciosa. Procure a bomba antes do sinal tocar.", "secretaria_balcao_01", "secretaria_arquivo_01")
            };
        }

        List<DadosDaFaseDaEscola> CreatePhasesFromSceneSpawns()
        {
            PontoDeSpawnDaBomba[] pontos = FindObjectsByType<PontoDeSpawnDaBomba>(FindObjectsInactive.Include);
            HashSet<LocalDaEscola> locaisPresentes = new();
            for (int i = 0; i < pontos.Length; i++)
            {
                if (pontos[i] != null)
                    locaisPresentes.Add(pontos[i].Local);
            }

            LocalDaEscola[] ordemPreferida =
            {
                LocalDaEscola.SalaDeAula,
                LocalDaEscola.Biblioteca,
                LocalDaEscola.Laboratorio,
                LocalDaEscola.Banheiro,
                LocalDaEscola.Refeitorio,
                LocalDaEscola.Quadra,
                LocalDaEscola.Corredor,
                LocalDaEscola.Secretaria,
                LocalDaEscola.Personalizado
            };

            List<DadosDaFaseDaEscola> fases = new();
            int contador = 1;
            for (int i = 0; i < ordemPreferida.Length; i++)
            {
                LocalDaEscola local = ordemPreferida[i];
                if (!locaisPresentes.Contains(local)) continue;

                string id = $"fase_{contador:00}_{local.ToString().ToLowerInvariant()}";
                string nome = FaseDaEscolaTexto.ObterNome(local);
                fases.Add(CreateRuntimePhase(id, nome, local, ModoDeObjetivoDaFase.Explicito, ""));
                contador++;
            }

            if (fases.Count == 0)
                Debug.LogError("EscolaPrefabricada detectada, mas nenhum PontoDeSpawnDaBomba foi encontrado na cena.", this);

            return fases;
        }

        DadosDaFaseDaEscola CreateRuntimePhase(
            string id,
            string nome,
            LocalDaEscola local,
            ModoDeObjetivoDaFase modo,
            string mensagemPersonalizada,
            params string[] idsDeSpawn)
        {
            DadosDaFaseDaEscola fase = ScriptableObject.CreateInstance<DadosDaFaseDaEscola>();
            fase.ConfigurarRuntime(
                id,
                nome,
                string.Empty,
                local,
                modo,
                mensagemPersonalizada,
                usarFiltroPorLocal: true,
                tempoSobrescrito: null,
                novosIdsDeSpawn: idsDeSpawn);
            return fase;
        }

        Transform CreateRoom(
            Transform parent,
            string nome,
            Vector3 centro,
            Vector2 tamanho,
            LadoAbertoDaSala ladoAberto,
            Material pisoMat,
            Material paredeMat,
            Material tetoMat,
            Color corDaPlaca)
        {
            const float espessuraParede = 0.35f;
            const float espessuraPiso = 0.18f;

            Transform root = new GameObject(nome).transform;
            root.SetParent(parent, false);

            SceneBuildHelpers.CreateBox("Floor", centro + new Vector3(0f, -espessuraPiso * 0.5f, 0f), new Vector3(tamanho.x, espessuraPiso, tamanho.y), pisoMat, root);
            SceneBuildHelpers.CreateBox("Ceiling", centro + new Vector3(0f, RoomHeight, 0f), new Vector3(tamanho.x, 0.14f, tamanho.y), tetoMat, root, collider: false);

            SceneBuildHelpers.CreateBox("Wall_W", centro + new Vector3(-tamanho.x * 0.5f, RoomHeight * 0.5f, 0f), new Vector3(espessuraParede, RoomHeight, tamanho.y), paredeMat, root);
            SceneBuildHelpers.CreateBox("Wall_E", centro + new Vector3(tamanho.x * 0.5f, RoomHeight * 0.5f, 0f), new Vector3(espessuraParede, RoomHeight, tamanho.y), paredeMat, root);

            if (ladoAberto == LadoAbertoDaSala.Norte || ladoAberto == LadoAbertoDaSala.Sul)
            {
                CreateWallWithDoorGap(root, centro, tamanho, RoomHeight, espessuraParede, ladoAberto, paredeMat);
                LadoAbertoDaSala ladoFechado = ladoAberto == LadoAbertoDaSala.Norte ? LadoAbertoDaSala.Sul : LadoAbertoDaSala.Norte;
                CreateSolidWall(root, centro, tamanho, ladoFechado, espessuraParede, paredeMat);
            }
            else
            {
                CreateWallWithDoorGap(root, centro, tamanho, RoomHeight, espessuraParede, ladoAberto, paredeMat);
                LadoAbertoDaSala ladoFechado = ladoAberto == LadoAbertoDaSala.Leste ? LadoAbertoDaSala.Oeste : LadoAbertoDaSala.Leste;
                CreateSolidWall(root, centro, tamanho, ladoFechado, espessuraParede, paredeMat);
            }

            CreateDoorPlate(root, centro, tamanho, ladoAberto, corDaPlaca);
            return root;
        }

        void CreatePerimeter(Transform parent, Material paredeMat)
        {
            SceneBuildHelpers.CreateBox("MuroNorte", new Vector3(0f, RoomHeight * 0.5f, RoomHalfDepth), new Vector3(RoomHalfWidth * 2f, RoomHeight, 0.45f), paredeMat, parent);
            SceneBuildHelpers.CreateBox("MuroSul_E", new Vector3(16.25f, RoomHeight * 0.5f, -RoomHalfDepth), new Vector3(23.5f, RoomHeight, 0.45f), paredeMat, parent);
            SceneBuildHelpers.CreateBox("MuroSul_W", new Vector3(-16.25f, RoomHeight * 0.5f, -RoomHalfDepth), new Vector3(23.5f, RoomHeight, 0.45f), paredeMat, parent);
            SceneBuildHelpers.CreateBox("MuroLeste", new Vector3(RoomHalfWidth, RoomHeight * 0.5f, 0f), new Vector3(0.45f, RoomHeight, RoomHalfDepth * 2f), paredeMat, parent);
            SceneBuildHelpers.CreateBox("MuroOeste", new Vector3(-RoomHalfWidth, RoomHeight * 0.5f, 0f), new Vector3(0.45f, RoomHeight, RoomHalfDepth * 2f), paredeMat, parent);
            SceneBuildHelpers.CreateBox("PilarEntrada_E", new Vector3(4f, RoomHeight * 0.5f, -RoomHalfDepth), new Vector3(0.55f, RoomHeight, 1.8f), paredeMat, parent);
            SceneBuildHelpers.CreateBox("PilarEntrada_W", new Vector3(-4f, RoomHeight * 0.5f, -RoomHalfDepth), new Vector3(0.55f, RoomHeight, 1.8f), paredeMat, parent);
            SceneBuildHelpers.CreateBox("TopoEntrada", new Vector3(0f, RoomHeight - 0.35f, -RoomHalfDepth), new Vector3(8.55f, 0.7f, 1.8f), paredeMat, parent, collider: false);
        }

        void CreateSolidWall(Transform parent, Vector3 centro, Vector2 tamanho, LadoAbertoDaSala lado, float espessura, Material paredeMat)
        {
            if (lado == LadoAbertoDaSala.Norte || lado == LadoAbertoDaSala.Sul)
            {
                float z = centro.z + (lado == LadoAbertoDaSala.Norte ? tamanho.y * 0.5f : -tamanho.y * 0.5f);
                SceneBuildHelpers.CreateBox($"Wall_{lado}", new Vector3(centro.x, RoomHeight * 0.5f, z), new Vector3(tamanho.x, RoomHeight, espessura), paredeMat, parent);
                return;
            }

            float x = centro.x + (lado == LadoAbertoDaSala.Leste ? tamanho.x * 0.5f : -tamanho.x * 0.5f);
            SceneBuildHelpers.CreateBox($"Wall_{lado}", new Vector3(x, RoomHeight * 0.5f, centro.z), new Vector3(espessura, RoomHeight, tamanho.y), paredeMat, parent);
        }

        void CreateWallWithDoorGap(Transform parent, Vector3 centro, Vector2 tamanho, float altura, float espessura, LadoAbertoDaSala lado, Material paredeMat)
        {
            float comprimento = lado == LadoAbertoDaSala.Norte || lado == LadoAbertoDaSala.Sul ? tamanho.x : tamanho.y;
            float deslocamento = lado == LadoAbertoDaSala.Norte || lado == LadoAbertoDaSala.Sul ? tamanho.y * 0.5f : tamanho.x * 0.5f;
            float larguraPorta = Mathf.Min(3.2f, comprimento * 0.44f);
            float comprimentoSegmento = (comprimento - larguraPorta) * 0.5f;
            float alturaPorta = 2.35f;
            float alturaLintel = Mathf.Max(0.5f, altura - alturaPorta);

            if (lado == LadoAbertoDaSala.Norte || lado == LadoAbertoDaSala.Sul)
            {
                float paredeZ = centro.z + (lado == LadoAbertoDaSala.Norte ? deslocamento : -deslocamento);
                SceneBuildHelpers.CreateBox("DoorWall_L", new Vector3(centro.x - (larguraPorta + comprimentoSegmento) * 0.5f, altura * 0.5f, paredeZ), new Vector3(comprimentoSegmento, altura, espessura), paredeMat, parent);
                SceneBuildHelpers.CreateBox("DoorWall_R", new Vector3(centro.x + (larguraPorta + comprimentoSegmento) * 0.5f, altura * 0.5f, paredeZ), new Vector3(comprimentoSegmento, altura, espessura), paredeMat, parent);
                SceneBuildHelpers.CreateBox("DoorLintel", new Vector3(centro.x, alturaPorta + alturaLintel * 0.5f, paredeZ), new Vector3(larguraPorta, alturaLintel, espessura), paredeMat, parent, collider: false);
                return;
            }

            float paredeX = centro.x + (lado == LadoAbertoDaSala.Leste ? deslocamento : -deslocamento);
            SceneBuildHelpers.CreateBox("DoorWall_B", new Vector3(paredeX, altura * 0.5f, centro.z - (larguraPorta + comprimentoSegmento) * 0.5f), new Vector3(espessura, altura, comprimentoSegmento), paredeMat, parent);
            SceneBuildHelpers.CreateBox("DoorWall_T", new Vector3(paredeX, altura * 0.5f, centro.z + (larguraPorta + comprimentoSegmento) * 0.5f), new Vector3(espessura, altura, comprimentoSegmento), paredeMat, parent);
            SceneBuildHelpers.CreateBox("DoorLintel", new Vector3(paredeX, alturaPorta + alturaLintel * 0.5f, centro.z), new Vector3(espessura, alturaLintel, larguraPorta), paredeMat, parent, collider: false);
        }

        void CreateDoorPlate(Transform parent, Vector3 centro, Vector2 tamanho, LadoAbertoDaSala lado, Color corDaPlaca)
        {
            Material placaMat = SceneBuildHelpers.MakeMat(corDaPlaca, 0.22f);
            Vector3 posicao = centro;
            Vector3 escala = new Vector3(2.5f, 0.5f, 0.18f);

            switch (lado)
            {
                case LadoAbertoDaSala.Norte:
                    posicao += new Vector3(0f, 3.15f, tamanho.y * 0.5f + 0.04f);
                    break;
                case LadoAbertoDaSala.Sul:
                    posicao += new Vector3(0f, 3.15f, -tamanho.y * 0.5f - 0.04f);
                    break;
                case LadoAbertoDaSala.Leste:
                    posicao += new Vector3(tamanho.x * 0.5f + 0.04f, 3.15f, 0f);
                    escala = new Vector3(0.18f, 0.5f, 2.5f);
                    break;
                case LadoAbertoDaSala.Oeste:
                    posicao += new Vector3(-tamanho.x * 0.5f - 0.04f, 3.15f, 0f);
                    escala = new Vector3(0.18f, 0.5f, 2.5f);
                    break;
            }

            SceneBuildHelpers.CreateBox("DoorPlate", posicao, escala, placaMat, parent, collider: false);
        }

        void PopulateCorridor(Transform parent, Material metalMat, Material madeiraMat)
        {
            Material quadroAvisoMat = SceneBuildHelpers.MakeMat(new Color(0.74f, 0.59f, 0.3f), 0.08f);
            Material armarioMat = SceneBuildHelpers.MakeMat(new Color(0.42f, 0.52f, 0.68f), 0.12f);

            for (int i = 0; i < 6; i++)
            {
                float x = -22f + i * 4.2f;
                SceneBuildHelpers.CreateBox($"Locker_E_{i}", new Vector3(x, 1.1f, 2.8f), new Vector3(1.6f, 2.2f, 0.45f), armarioMat, parent);
                SceneBuildHelpers.CreateBox($"Locker_W_{i}", new Vector3(x, 1.1f, -4.8f), new Vector3(1.6f, 2.2f, 0.45f), armarioMat, parent);
            }

            SceneBuildHelpers.CreateBox("MuralEntrada", new Vector3(-7f, 1.9f, -5.1f), new Vector3(3f, 1.6f, 0.1f), quadroAvisoMat, parent, collider: false);
            SceneBuildHelpers.CreateBox("BancoCorredor", new Vector3(9f, 0.45f, -4.9f), new Vector3(3f, 0.3f, 0.8f), madeiraMat, parent);
            SceneBuildHelpers.CreateBox("BancoBase", new Vector3(9f, 0.2f, -4.9f), new Vector3(2.5f, 0.25f, 0.45f), metalMat, parent);
        }

        void PopulateClassroom(Transform parent, Vector3 centro, Material madeiraMat, Material metalMat, Material quadroMat, Material livroMat)
        {
            SceneBuildHelpers.CreateBox("Quadro", centro + new Vector3(0f, 2f, 3.7f), new Vector3(4.2f, 2f, 0.12f), quadroMat, parent, collider: false);
            SceneBuildHelpers.CreateBox("MesaProfessor", centro + new Vector3(0f, 0.45f, 2.5f), new Vector3(2.2f, 0.9f, 1.1f), madeiraMat, parent);

            for (int linha = 0; linha < 2; linha++)
            {
                for (int coluna = 0; coluna < 3; coluna++)
                {
                    Vector3 pos = centro + new Vector3(-3f + coluna * 3f, 0.4f, 0.8f - linha * 2.4f);
                    SceneBuildHelpers.CreateBox($"Carteira_{linha}_{coluna}", pos, new Vector3(1.4f, 0.8f, 1f), madeiraMat, parent);
                    SceneBuildHelpers.CreateBox($"Cadeira_{linha}_{coluna}", pos + new Vector3(0f, -0.2f, -0.8f), new Vector3(1f, 0.4f, 0.8f), metalMat, parent);
                }
            }

            SceneBuildHelpers.CreateBox("EstanteLivros", centro + new Vector3(-4.5f, 1.1f, 3.1f), new Vector3(1f, 2.2f, 0.45f), madeiraMat, parent);
            SceneBuildHelpers.CreateBox("Pilhas", centro + new Vector3(4.2f, 0.72f, 1.8f), new Vector3(0.8f, 0.3f, 0.8f), livroMat, parent, collider: false);
        }

        void PopulateLibrary(Transform parent, Vector3 centro, Material madeiraMat, Material metalMat, Material livroMat, Material quadroMat)
        {
            for (int i = 0; i < 4; i++)
            {
                float x = -4f + i * 2.7f;
                SceneBuildHelpers.CreateBox($"Estante_{i}", centro + new Vector3(x, 1.25f, 2.2f), new Vector3(1.3f, 2.5f, 0.7f), madeiraMat, parent);
                SceneBuildHelpers.CreateBox($"Livros_{i}", centro + new Vector3(x, 1.55f, 2.25f), new Vector3(1.05f, 0.18f, 0.52f), livroMat, parent, collider: false);
            }

            SceneBuildHelpers.CreateBox("MesaLeitura", centro + new Vector3(-2.2f, 0.42f, -0.4f), new Vector3(2.6f, 0.84f, 1.2f), madeiraMat, parent);
            SceneBuildHelpers.CreateBox("MesaLeitura2", centro + new Vector3(2.2f, 0.42f, -0.4f), new Vector3(2.6f, 0.84f, 1.2f), madeiraMat, parent);
            SceneBuildHelpers.CreateBox("PainelBiblioteca", centro + new Vector3(0f, 1.8f, 3.75f), new Vector3(3.6f, 1.4f, 0.12f), quadroMat, parent, collider: false);
            SceneBuildHelpers.CreateBox("CarrinhoLivros", centro + new Vector3(4.2f, 0.55f, -2.4f), new Vector3(1.1f, 1.1f, 0.8f), metalMat, parent);
        }

        void PopulateLaboratory(Transform parent, Vector3 centro, Material metalMat, Material madeiraMat)
        {
            Material liquidoMat = SceneBuildHelpers.MakeMat(new Color(0.18f, 0.82f, 0.9f), 0.55f, true);

            SceneBuildHelpers.CreateBox("BancadaFundo", centro + new Vector3(0f, 0.55f, 3.3f), new Vector3(8.6f, 1.1f, 1.2f), metalMat, parent);
            SceneBuildHelpers.CreateBox("MesaCentral", centro + new Vector3(-2.1f, 0.55f, 0.2f), new Vector3(2.5f, 1.1f, 1.4f), madeiraMat, parent);
            SceneBuildHelpers.CreateBox("MesaCentral2", centro + new Vector3(2.1f, 0.55f, 0.2f), new Vector3(2.5f, 1.1f, 1.4f), madeiraMat, parent);
            SceneBuildHelpers.CreateBox("ArmarioQuimico", centro + new Vector3(4.6f, 1.3f, -2.5f), new Vector3(1.2f, 2.6f, 0.8f), metalMat, parent);

            for (int i = 0; i < 4; i++)
            {
                GameObject cilindro = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cilindro.name = $"Cilindro_{i}";
                cilindro.transform.SetParent(parent, false);
                cilindro.transform.position = centro + new Vector3(-3f + i * 2f, 1.22f, 3.2f);
                cilindro.transform.localScale = new Vector3(0.22f, 0.25f, 0.22f);
                cilindro.GetComponent<Renderer>().sharedMaterial = liquidoMat;
                Destroy(cilindro.GetComponent<Collider>());
            }
        }

        void PopulateBathroom(Transform parent, Vector3 centro, Material azulejoMat, Material metalMat)
        {
            Material divisoriaMat = SceneBuildHelpers.MakeMat(new Color(0.74f, 0.82f, 0.84f), 0.12f);

            SceneBuildHelpers.CreateBox("BalcaoPias", centro + new Vector3(-2.6f, 0.55f, -2.4f), new Vector3(3.6f, 1.1f, 0.9f), azulejoMat, parent);
            SceneBuildHelpers.CreateBox("Espelho", centro + new Vector3(-2.6f, 2f, -2.9f), new Vector3(3.2f, 1.8f, 0.08f), metalMat, parent, collider: false);

            for (int i = 0; i < 3; i++)
            {
                float x = -1f + i * 2f;
                SceneBuildHelpers.CreateBox($"Cabine_{i}", centro + new Vector3(x, 1.1f, 2f), new Vector3(1.5f, 2.2f, 2f), divisoriaMat, parent);
            }
        }

        void PopulateSecretary(Transform parent, Vector3 centro, Material madeiraMat, Material metalMat)
        {
            SceneBuildHelpers.CreateBox("Balcao", centro + new Vector3(0f, 0.6f, -0.7f), new Vector3(6.6f, 1.2f, 1.3f), madeiraMat, parent);
            SceneBuildHelpers.CreateBox("Arquivo_E", centro + new Vector3(-4.4f, 1.25f, 2f), new Vector3(1.1f, 2.5f, 0.9f), metalMat, parent);
            SceneBuildHelpers.CreateBox("Arquivo_D", centro + new Vector3(4.4f, 1.25f, 2f), new Vector3(1.1f, 2.5f, 0.9f), metalMat, parent);
            SceneBuildHelpers.CreateBox("MesaSecretaria", centro + new Vector3(2.3f, 0.42f, -2.1f), new Vector3(2.2f, 0.84f, 1.2f), madeiraMat, parent);
            SceneBuildHelpers.CreateBox("CadeiraAtendimento", centro + new Vector3(-1.8f, 0.38f, -2.3f), new Vector3(1f, 0.76f, 0.9f), metalMat, parent);
        }

        void PopulateCafeteria(Transform parent, Vector3 centro, Material madeiraMat, Material metalMat)
        {
            for (int i = 0; i < 3; i++)
            {
                float z = -2.1f + i * 2f;
                SceneBuildHelpers.CreateBox($"Mesa_{i}", centro + new Vector3(0f, 0.42f, z), new Vector3(5.2f, 0.84f, 0.9f), madeiraMat, parent);
                SceneBuildHelpers.CreateBox($"BancoN_{i}", centro + new Vector3(0f, 0.3f, z + 0.95f), new Vector3(5f, 0.6f, 0.45f), metalMat, parent);
                SceneBuildHelpers.CreateBox($"BancoS_{i}", centro + new Vector3(0f, 0.3f, z - 0.95f), new Vector3(5f, 0.6f, 0.45f), metalMat, parent);
            }

            SceneBuildHelpers.CreateBox("BalcaoBandejas", centro + new Vector3(4.5f, 0.62f, 2.4f), new Vector3(1.7f, 1.24f, 2.1f), metalMat, parent);
        }

        void PopulateCourt(Transform parent, Vector3 centro, Material quadraMat, Material linhaMat, Material metalMat)
        {
            Transform quadra = new GameObject("Quadra").transform;
            quadra.SetParent(parent, false);

            SceneBuildHelpers.CreateBox("PisoQuadra", centro + new Vector3(0f, -0.02f, 0f), new Vector3(24f, 0.14f, 11f), quadraMat, quadra);
            SceneBuildHelpers.CreateBox("LinhaCentral", centro + new Vector3(0f, 0.06f, 0f), new Vector3(0.18f, 0.02f, 10f), linhaMat, quadra, collider: false);
            SceneBuildHelpers.CreateBox("LinhaLateralN", centro + new Vector3(0f, 0.06f, 5f), new Vector3(23f, 0.02f, 0.12f), linhaMat, quadra, collider: false);
            SceneBuildHelpers.CreateBox("LinhaLateralS", centro + new Vector3(0f, 0.06f, -5f), new Vector3(23f, 0.02f, 0.12f), linhaMat, quadra, collider: false);

            SceneBuildHelpers.CreateBox("GradeN", centro + new Vector3(0f, 1.5f, 5.8f), new Vector3(24.4f, 3f, 0.12f), metalMat, quadra);
            SceneBuildHelpers.CreateBox("GradeS_E", centro + new Vector3(7.2f, 1.5f, -5.8f), new Vector3(10f, 3f, 0.12f), metalMat, quadra);
            SceneBuildHelpers.CreateBox("GradeS_W", centro + new Vector3(-7.2f, 1.5f, -5.8f), new Vector3(10f, 3f, 0.12f), metalMat, quadra);
            SceneBuildHelpers.CreateBox("GradeE", centro + new Vector3(12.2f, 1.5f, 0f), new Vector3(0.12f, 3f, 11.8f), metalMat, quadra);
            SceneBuildHelpers.CreateBox("GradeW", centro + new Vector3(-12.2f, 1.5f, 0f), new Vector3(0.12f, 3f, 11.8f), metalMat, quadra);

            SceneBuildHelpers.CreateBox("CestaN", centro + new Vector3(0f, 2.3f, 4.3f), new Vector3(2.2f, 1.2f, 0.12f), metalMat, quadra, collider: false);
            SceneBuildHelpers.CreateBox("CestaS", centro + new Vector3(0f, 2.3f, -4.3f), new Vector3(2.2f, 1.2f, 0.12f), metalMat, quadra, collider: false);
            SceneBuildHelpers.CreateBox("BancoQuadra", centro + new Vector3(-7.2f, 0.42f, 4.6f), new Vector3(3.4f, 0.84f, 0.8f), metalMat, quadra);
        }

        void CreateBombSpawnPoint(Transform parent, string identificador, LocalDaEscola local, Vector3 posicao, Quaternion rotacao)
        {
            GameObject spawn = new GameObject($"Spawn_{identificador}");
            spawn.transform.SetParent(parent, false);
            spawn.transform.SetPositionAndRotation(posicao, rotacao);

            PontoDeSpawnDaBomba ponto = spawn.AddComponent<PontoDeSpawnDaBomba>();
            ponto.ConfigurarRuntime(identificador, local, null, true, CorDoLocal(local));
        }

        Color CorDoLocal(LocalDaEscola local)
        {
            return local switch
            {
                LocalDaEscola.SalaDeAula => new Color(0.95f, 0.58f, 0.24f),
                LocalDaEscola.Banheiro => new Color(0.25f, 0.75f, 0.84f),
                LocalDaEscola.Biblioteca => new Color(0.34f, 0.72f, 0.36f),
                LocalDaEscola.Laboratorio => new Color(0.28f, 0.58f, 0.95f),
                LocalDaEscola.Refeitorio => new Color(0.94f, 0.34f, 0.24f),
                LocalDaEscola.Quadra => new Color(0.94f, 0.88f, 0.22f),
                LocalDaEscola.Corredor => new Color(0.88f, 0.6f, 0.94f),
                LocalDaEscola.Secretaria => new Color(0.96f, 0.72f, 0.24f),
                _ => new Color(1f, 0.35f, 0.15f),
            };
        }

        void BuildSchoolLights(Transform parent)
        {
            GameObject sol = new GameObject("SchoolSun");
            sol.transform.SetParent(parent, false);
            sol.transform.rotation = Quaternion.Euler(54f, -32f, 0f);
            Light luzSolar = sol.AddComponent<Light>();
            luzSolar.type = LightType.Directional;
            luzSolar.color = new Color(1f, 0.96f, 0.9f);
            luzSolar.intensity = 0.85f;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.66f, 0.7f);

            Vector3[] posicoes =
            {
                new Vector3(-18f, 3.4f, 9f),
                new Vector3(0f, 3.4f, 9f),
                new Vector3(18f, 3.4f, 9f),
                new Vector3(-18f, 3.4f, -10f),
                new Vector3(0f, 3.4f, -10f),
                new Vector3(18f, 3.4f, -10f),
                new Vector3(0f, 3.5f, -1f),
                new Vector3(0f, 3.5f, 10f),
                new Vector3(0f, 4.8f, 22f)
            };

            for (int i = 0; i < posicoes.Length; i++)
            {
                GameObject lamp = new GameObject($"SchoolLamp_{i}");
                lamp.transform.SetParent(parent, false);
                lamp.transform.position = posicoes[i];
                Light luz = lamp.AddComponent<Light>();
                luz.type = LightType.Point;
                luz.color = new Color(1f, 0.95f, 0.82f);
                luz.intensity = i == posicoes.Length - 1 ? 2.2f : 1.55f;
                luz.range = i == posicoes.Length - 1 ? 18f : 11f;
            }
        }
    }
}
