using System.Collections.Generic;
using UnityEngine;

namespace LiveWire
{
    public static class SchoolNavigationOptimizer
    {
        const float PathHalfWidth = 0.95f;
        const float DoorClearRadius = 1.25f;

        static Transform playerTransform;
        static BombManager trackedBomb;
        static bool subscribed;

        public static void EnsureRuntimeHooks(Transform player, BombManager bomb)
        {
            playerTransform = player;
            trackedBomb = bomb;

            OpenAccessToBomb();

            GerenciadorDeFase fases = GerenciadorDeFase.Instance;
            if (fases == null || subscribed)
                return;

            fases.AoIniciarFase += HandlePhaseStarted;
            subscribed = true;
        }

        static void HandlePhaseStarted(DadosDaFaseDaEscola fase, int numeroDaFase, PontoDeSpawnDaBomba ponto)
        {
            OpenAccessToBomb();
        }

        static void OpenAccessToBomb()
        {
            if (playerTransform == null || trackedBomb == null)
                return;

            Vector3 start = playerTransform.position;
            Vector3 end = trackedBomb.GetBombPosition();
            start.y = 0f;
            end.y = 0f;

            if ((end - start).sqrMagnitude < 0.25f)
                return;

            // O caminho real ate a bomba nao e uma reta: o jogador anda pelo
            // corredor e entra pela porta da sala. Uma reta corredor->bomba corta
            // na diagonal e ignora o vao de entrada, deixando props bloquearem a
            // porta. Por isso limpamos ao longo de uma polilinha que inclui a
            // porta da sala da bomba como ponto de passagem, garantindo que a
            // entrada NUNCA fique bloqueada.
            List<Vector3> caminho = new() { start };
            if (TentarAcharPortaDaSala(end, out Vector3 pontoPorta))
                caminho.Add(pontoPorta);
            caminho.Add(end);

            ClearDoorColliders(caminho);

            HashSet<Transform> moved = new();
            Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);
            for (int i = 0; i < renderers.Length; i++)
            {
                Transform root = FindMovablePropRoot(renderers[i].transform);
                if (root == null || moved.Contains(root))
                    continue;

                if (!TryGetBounds(root, out Bounds bounds))
                    continue;

                Vector3 center = bounds.center;
                center.y = 0f;

                float distanceToPath = DistancePointToPolyline(center, caminho, out float t);
                bool blocksPath = t > 0.04f && t < 0.96f && distanceToPath < PathHalfWidth + Mathf.Min(bounds.extents.x, bounds.extents.z);
                bool blocksBombAccess = Vector3.Distance(center, end) < DoorClearRadius + Mathf.Max(bounds.extents.x, bounds.extents.z);

                if (!blocksPath && !blocksBombAccess)
                    continue;

                DisablePropColliders(root);
                moved.Add(root);
            }
        }

        // Acha o marcador de porta (abertura sem bloqueio deixada pelo montador)
        // mais proximo da bomba: e a entrada da sala onde ela esta. So vale a
        // pena desviar por ela quando a bomba esta dentro de uma sala (fora da
        // faixa do corredor, |z| > 2.5); para bombas no proprio corredor a reta
        // direta ja basta.
        static bool TentarAcharPortaDaSala(Vector3 bomba, out Vector3 ponto)
        {
            ponto = Vector3.zero;
            if (Mathf.Abs(bomba.z) <= 2.5f)
                return false;

            Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude);
            float melhor = float.MaxValue;
            bool achou = false;
            for (int i = 0; i < transforms.Length; i++)
            {
                if (!transforms[i].name.ToLowerInvariant().Contains("porta_larga"))
                    continue;

                Vector3 p = transforms[i].position;
                p.y = 0f;
                float d = (p - bomba).sqrMagnitude;
                if (d < melhor)
                {
                    melhor = d;
                    ponto = p;
                    achou = true;
                }
            }
            return achou;
        }

        static void ClearDoorColliders(List<Vector3> caminho)
        {
            Collider[] colliders = Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null || collider.isTrigger)
                    continue;

                string hierarchyName = GetHierarchyName(collider.transform);
                if (!LooksLikeDoorBlocker(hierarchyName))
                    continue;

                Vector3 center = collider.bounds.center;
                center.y = 0f;

                float distance = DistancePointToPolyline(center, caminho, out float t);
                if (t > 0.03f && t < 0.97f && distance < 1.65f)
                    collider.enabled = false;
            }
        }

        static Transform FindMovablePropRoot(Transform transform)
        {
            Transform current = transform;
            Transform candidate = null;

            while (current != null)
            {
                string name = current.name.ToLowerInvariant();
                if (IsStructuralName(name) || current.GetComponent<BombManager>() != null || current.GetComponent<PlayerController>() != null)
                    break;

                if (IsMovablePropName(name))
                    candidate = current;

                current = current.parent;
            }

            return candidate;
        }

        static bool TryGetBounds(Transform root, out Bounds bounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(false);
            if (renderers.Length == 0)
            {
                bounds = default;
                return false;
            }

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds.size.y > 0.08f && bounds.size.y < 4.5f;
        }

        static void DisablePropColliders(Transform prop)
        {
            // Empurrar mobília lateralmente fazia várias cadeiras vizinhas
            // colidirem entre si (várias acabavam deslocadas para o mesmo lado
            // e empilhadas). Mantemos a posição original e só desligamos os
            // colliders que bloqueariam o caminho até a bomba.
            Collider[] colliders = prop.GetComponentsInChildren<Collider>(false);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] == null || colliders[i].isTrigger) continue;
                colliders[i].enabled = false;
            }
        }

        // Menor distancia do ponto a uma polilinha, com t normalizado (0..1) da
        // posicao mais proxima ao longo do comprimento total. Usado para liberar
        // o caminho em "L" corredor -> porta -> bomba.
        static float DistancePointToPolyline(Vector3 point, List<Vector3> pts, out float tGlobal)
        {
            tGlobal = 0f;
            if (pts == null || pts.Count == 0)
                return float.MaxValue;
            if (pts.Count == 1)
                return Vector3.Distance(point, pts[0]);

            float comprimentoTotal = 0f;
            for (int i = 0; i < pts.Count - 1; i++)
                comprimentoTotal += Vector3.Distance(pts[i], pts[i + 1]);

            if (comprimentoTotal < 0.0001f)
                return Vector3.Distance(point, pts[0]);

            float melhor = float.MaxValue;
            float acumulado = 0f;
            for (int i = 0; i < pts.Count - 1; i++)
            {
                float segLen = Vector3.Distance(pts[i], pts[i + 1]);
                float d = DistancePointToSegment(point, pts[i], pts[i + 1], out float tSeg);
                if (d < melhor)
                {
                    melhor = d;
                    tGlobal = (acumulado + tSeg * segLen) / comprimentoTotal;
                }
                acumulado += segLen;
            }
            return melhor;
        }

        static float DistancePointToSegment(Vector3 point, Vector3 a, Vector3 b, out float t)
        {
            Vector3 ab = b - a;
            float lengthSq = ab.sqrMagnitude;
            if (lengthSq < 0.0001f)
            {
                t = 0f;
                return Vector3.Distance(point, a);
            }

            t = Mathf.Clamp01(Vector3.Dot(point - a, ab) / lengthSq);
            Vector3 closest = a + ab * t;
            return Vector3.Distance(point, closest);
        }

        static bool IsMovablePropName(string name)
        {
            return name.Contains("chair")
                || name.Contains("table")
                || name.Contains("desk")
                || name.Contains("carteira")
                || name.Contains("cadeira")
                || name.Contains("locker")
                || name.Contains("rack")
                || name.Contains("book")
                || name.Contains("computer")
                || name.Contains("showcase")
                || name.Contains("archive")
                || name.Contains("closet")
                || name.Contains("trash")
                || name.Contains("tray")
                || name.Contains("projector")
                || name.Contains("speaker")
                || name.Contains("binder")
                || name.Contains("phone")
                || name.Contains("mouse")
                || name.Contains("keyboard")
                || name.Contains("lamp");
        }

        static bool LooksLikeDoorBlocker(string name)
        {
            return name.Contains("door")
                || name.Contains("porta")
                || name.Contains("wall_a_3x3_door")
                || name.Contains("wall_b_3x3_door")
                || name.Contains("wall_a_3x2_door")
                || name.Contains("wall_b_3x2_door");
        }

        static string GetHierarchyName(Transform transform)
        {
            string result = transform.name.ToLowerInvariant();
            Transform current = transform.parent;
            while (current != null)
            {
                result += "/" + current.name.ToLowerInvariant();
                current = current.parent;
            }

            return result;
        }

        static bool IsStructuralName(string name)
        {
            return name.Contains("wall")
                || name.Contains("floor")
                || name.Contains("ceiling")
                || name.Contains("door")
                || name.Contains("corredor")
                || name.Contains("sala")
                || name.Contains("biblioteca")
                || name.Contains("laboratorio")
                || name.Contains("banheiro")
                || name.Contains("secretaria")
                || name.Contains("refeitorio")
                || name.Contains("spawn")
                || name.Contains("bomb")
                || name.Contains("hud")
                || name.Contains("camera");
        }
    }
}
