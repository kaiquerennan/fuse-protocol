using System.Collections;
using System;
using UnityEngine;

namespace LiveWire
{
    public class BombManager : MonoBehaviour, IInteractable
    {
        public static BombManager Instance { get; private set; }

        public Transform bombRoot;
        public Transform bombVisual;
        public Light bombLight;
        public Renderer bombRenderer;
        public Renderer sparkRenderer;
        public Light sparkLight;
        public Transform cameraZoomTarget;
        public Transform cameraLookTarget;
        public GerenciadorDeBomba gerenciadorDeBomba;

        const float ZoomInDuration = 1.0f;
        const float ZoomOutDuration = 1.0f;
        const float InteriorDistance = 0.95f;
        const float FocusFov = 42f;
        const float FocusNearClip = 0.01f;
        const float SurfaceClearance = 0.035f;
        public const float ProximityRadius = 2.5f;

        bool defused;
        bool focusing;
        float pulseClock;
        float cachedPlayerFov = -1f;
        float cachedPlayerNearClip = -1f;
        Coroutine cancelRoutine;
        Vector3 escalaInicialDaRaiz = Vector3.one;
        bool estadoInicialRegistrado;

        // Calculado por distância a cada consulta, em vez de eventos de trigger:
        // OnTriggerEnter não dispara quando a bomba é rearmada/teleportada com o
        // jogador já dentro do raio, deixando o prompt de desarme preso em "fora
        // de alcance" até sair e voltar.
        public bool PlayerInRange => JogadorDentroDoRaio(PlayerController.Instance, ObterPontoVisualDeFoco());
        public bool EstaDesarmada => defused;
        public bool IsFocused => focusing || (gerenciadorDeBomba != null && gerenciadorDeBomba.IsOpen);

        public event Action<BombManager> AoBombaDesarmada;
        public event Action<BombManager> AoSequenciaDeDesarmeConcluida;

        void Awake()
        {
            Instance = this;
            RegistrarEstadoInicial();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (defused) return;

            pulseClock += Time.deltaTime;
            float pulse = 0.5f + 0.5f * Mathf.Sin(pulseClock * 6f);

            if (bombLight != null)
            {
                bombLight.intensity = Mathf.Lerp(1.2f, 4.5f, pulse);
            }

            if (bombRenderer != null)
            {
                Color emissive = Color.Lerp(new Color(0.2f, 0.01f, 0.01f), new Color(0.8f, 0.08f, 0.06f), pulse);
                bombRenderer.material.SetColor("_EmissionColor", emissive);
                if (!bombRenderer.material.IsKeywordEnabled("_EMISSION"))
                    bombRenderer.material.EnableKeyword("_EMISSION");
            }

            if (sparkRenderer != null)
            {
                float flicker = 0.7f + 0.3f * Mathf.Sin(pulseClock * 22f + Mathf.Sin(pulseClock * 37f));
                Color sparkEm = new Color(1.6f, 1.0f, 0.25f) * flicker;
                sparkRenderer.material.SetColor("_EmissionColor", sparkEm);
                if (!sparkRenderer.material.IsKeywordEnabled("_EMISSION"))
                    sparkRenderer.material.EnableKeyword("_EMISSION");
                if (sparkLight != null) sparkLight.intensity = Mathf.Lerp(0.8f, 1.8f, flicker);
            }
        }

        static bool JogadorDentroDoRaio(PlayerController player, Vector3 pontoDaBomba)
        {
            if (player == null) return false;

            Vector3 pos = player.transform.position;
            float dx = pos.x - pontoDaBomba.x;
            float dz = pos.z - pontoDaBomba.z;
            if (dx * dx + dz * dz > ProximityRadius * ProximityRadius) return false;

            // Janela vertical curta: perto no plano mas um andar acima/abaixo
            // não conta como "perto da bomba".
            return Mathf.Abs(pos.y - pontoDaBomba.y) <= 1.8f;
        }

        public string GetPrompt() => "[ E ] DESARMAR";

        public void Interact(PlayerController player)
        {
            if (defused || focusing || gerenciadorDeBomba == null) return;
            if (gerenciadorDeBomba.IsOpen) return;
            if (!CanInteractFrom(player)) return;
            StartCoroutine(FocusIntoBomb(player));
        }

        public bool CanInteractNow() => !defused && !focusing && gerenciadorDeBomba != null && !gerenciadorDeBomba.IsOpen;

        public bool CanInteractFrom(PlayerController player)
        {
            if (!CanInteractNow() || player == null) return false;

            Vector3 target = ObterPontoVisualDeFoco();
            if (!JogadorDentroDoRaio(player, target)) return false;

            // Perto da bomba o desarme fica SEMPRE disponível. A única exceção
            // é uma parede entre o jogador e a bomba (jogador no cômodo ao
            // lado). Móveis, direção do olhar e altura da câmera não bloqueiam.
            return !ParedeEntre(player, target);
        }

        bool ParedeEntre(PlayerController player, Vector3 target)
        {
            Vector3 peito = player.transform.position + Vector3.up * 1.2f;
            if (ParedeNoCaminho(player, peito, target)) return HaParedeTambemPelaCamera(player, target);
            return false;
        }

        bool HaParedeTambemPelaCamera(PlayerController player, Vector3 target)
        {
            if (player.playerCamera == null) return true;
            return ParedeNoCaminho(player, player.playerCamera.transform.position, target);
        }

        bool ParedeNoCaminho(PlayerController player, Vector3 origin, Vector3 target)
        {
            Vector3 toTarget = target - origin;
            float distance = toTarget.magnitude;
            if (distance <= 0.01f) return false;

            // RaycastAll: um móvel na frente não pode esconder uma parede que
            // esteja logo atrás dele — verificamos todos os obstáculos até a bomba.
            RaycastHit[] hits = Physics.RaycastAll(origin, toTarget / distance, distance, ~0, QueryTriggerInteraction.Ignore);
            foreach (RaycastHit hit in hits)
            {
                Transform hitTransform = hit.collider.transform;
                if (hitTransform.IsChildOf(player.transform)) continue;
                if (hitTransform.IsChildOf(transform) || (bombVisual != null && hitTransform.IsChildOf(bombVisual))) continue;
                if (hit.collider.GetComponentInParent<BombManager>() == this) continue;

                // Só conta como parede o que for alto demais para ser mobiliário
                // (mesa, cadeira, banco ficam abaixo de ~1.6 m). Portas fechadas
                // e armários altos bloqueiam como paredes, o que é o esperado.
                if (hit.collider.bounds.size.y >= 1.6f) return true;
            }

            return false;
        }

        public void CancelMinigame()
        {
            if (defused || focusing || gerenciadorDeBomba == null || !gerenciadorDeBomba.IsOpen) return;
            if (cancelRoutine != null) return;
            cancelRoutine = StartCoroutine(CancelMinigameRoutine());
        }

        IEnumerator FocusIntoBomb(PlayerController player)
        {
            focusing = true;
            player.SetInputLocked(true);

            Camera cam = player.playerCamera;
            if (cam == null)
            {
                gerenciadorDeBomba.Abrir(GameManager.Instance?.CurrentPhase ?? 1);
                focusing = false;
                yield break;
            }

            Vector3 focusPoint = ObterPontoVisualDeFoco();

            Vector3 startPos = cam.transform.position;
            Quaternion startRot = cam.transform.rotation;
            float startFov = cam.fieldOfView;
            cachedPlayerFov = startFov;
            cachedPlayerNearClip = cam.nearClipPlane;

            cam.nearClipPlane = FocusNearClip;

            Vector3 endPos;
            if (cameraZoomTarget != null)
            {
                endPos = cameraZoomTarget.position;
            }
            else
            {
                Vector3 toBomb = focusPoint - startPos;
                toBomb.y *= 0.3f;
                Vector3 dir = toBomb.sqrMagnitude > 0.001f ? toBomb.normalized : cam.transform.forward;
                endPos = focusPoint - dir * InteriorDistance;
            }
            Quaternion endRot = Quaternion.LookRotation(focusPoint - endPos);

            player.DetachCameraForFocus();

            float elapsed = 0f;
            while (elapsed < ZoomInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float n = Mathf.Clamp01(elapsed / ZoomInDuration);
                float eased = 1f - Mathf.Pow(1f - n, 3f);
                cam.transform.position = Vector3.Lerp(startPos, endPos, eased);
                cam.transform.rotation = Quaternion.Slerp(startRot, endRot, eased);
                cam.fieldOfView = Mathf.Lerp(startFov, FocusFov, eased);
                yield return null;
            }
            cam.transform.position = endPos;
            cam.transform.rotation = endRot;
            cam.fieldOfView = FocusFov;
            cam.nearClipPlane = FocusNearClip;

            if (CameraShake.Instance != null)
            {
                CameraShake.Instance.CaptureCurrentBase();
                CameraShake.Instance.enabled = true;
            }

            gerenciadorDeBomba.Abrir(GameManager.Instance?.CurrentPhase ?? 1);
            focusing = false;
        }

        Vector3 ObterPontoVisualDeFoco()
        {
            Transform alvo = cameraLookTarget != null
                ? cameraLookTarget
                : (bombVisual != null ? bombVisual : transform);

            Renderer[] renderers = alvo.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return alvo.position + Vector3.up * 0.25f;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds.center;
        }

        IEnumerator CancelMinigameRoutine()
        {
            focusing = true;
            gerenciadorDeBomba.Fechar(restoreControl: false, resumeBombLoop: true);
            yield return ZoomOutFromBomb();
            focusing = false;
            cancelRoutine = null;
        }

        public IEnumerator ZoomOutFromBomb()
        {
            PlayerController player = PlayerController.Instance;
            if (player == null || player.playerCamera == null)
            {
                if (player != null && player.playerCamera != null && cachedPlayerNearClip > 0f)
                    player.playerCamera.nearClipPlane = cachedPlayerNearClip;
                player?.ReattachCamera();
                player?.SetInputLocked(false);
                yield break;
            }

            Camera cam = player.playerCamera;
            player.SetInputLocked(true);

            if (CameraShake.Instance != null)
            {
                CameraShake.Instance.CaptureCurrentBase();
                CameraShake.Instance.enabled = false;
            }

            Vector3 startPos = cam.transform.position;
            Quaternion startRot = cam.transform.rotation;
            float startFov = cam.fieldOfView;
            float startNear = cam.nearClipPlane;

            Transform origParent = player.transform;
            Vector3 targetWorldPos = origParent.TransformPoint(new Vector3(0f, 1.65f, 0f));
            Quaternion targetWorldRot = origParent.rotation * Quaternion.Euler(0f, 0f, 0f);
            float targetFov = cachedPlayerFov > 0f ? cachedPlayerFov : 72f;
            float targetNear = cachedPlayerNearClip > 0f ? cachedPlayerNearClip : 0.05f;

            float elapsed = 0f;
            while (elapsed < ZoomOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float n = Mathf.Clamp01(elapsed / ZoomOutDuration);
                float eased = 1f - Mathf.Pow(1f - n, 3f);
                cam.transform.position = Vector3.Lerp(startPos, targetWorldPos, eased);
                cam.transform.rotation = Quaternion.Slerp(startRot, targetWorldRot, eased);
                cam.fieldOfView = Mathf.Lerp(startFov, targetFov, eased);
                cam.nearClipPlane = Mathf.Lerp(startNear, targetNear, eased);
                yield return null;
            }
            cam.fieldOfView = targetFov;
            cam.nearClipPlane = targetNear;

            player.ReattachCamera();
            player.SetInputLocked(false);
        }

        public void OnDefused()
        {
            if (defused) return;
            defused = true;
            AoBombaDesarmada?.Invoke(this);
            StartCoroutine(DefusedSequence());
        }

        IEnumerator DefusedSequence()
        {
            AudioManager.Instance?.StopTicking();
            AudioManager.Instance?.StopHiss();

            yield return ZoomOutFromBomb();

            yield return new WaitForSecondsRealtime(0.3f);

            Vector3 fxPos = cameraZoomTarget != null
                ? cameraZoomTarget.position
                : (bombVisual != null ? bombVisual.position : transform.position + Vector3.up * 0.6f);
            SpawnElectricDisappear(fxPos);

            float elapsed = 0f;
            Vector3 startScale = bombRoot != null ? bombRoot.localScale : Vector3.one;
            Transform scaleTarget = bombRoot != null ? bombRoot : bombVisual;
            while (elapsed < 0.6f && scaleTarget != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / 0.6f;
                scaleTarget.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                yield return null;
            }
            if (scaleTarget != null) scaleTarget.gameObject.SetActive(false);
            if (bombLight != null) bombLight.enabled = false;
            if (sparkLight != null) sparkLight.enabled = false;
            AoSequenciaDeDesarmeConcluida?.Invoke(this);
        }

        void SpawnElectricDisappear(Vector3 position)
        {
            GameObject go = new GameObject("BombElectric");
            go.transform.position = position;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.8f;
            main.startLifetime = 0.6f;
            main.startSpeed = 6f;
            main.startSize = 0.2f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.4f, 0.9f, 1.3f), new Color(0.1f, 0.7f, 1.5f));
            main.maxParticles = 200;
            var emission = ps.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 120) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.4f;

            var r = go.GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            r.material = new Material(shader);
            r.material.color = new Color(0.4f, 0.9f, 1.4f);

            Destroy(go, 2f);
        }

        public Vector3 GetBombPosition()
        {
            if (bombVisual != null) return bombVisual.position;
            if (cameraZoomTarget != null) return cameraZoomTarget.position;
            return transform.position;
        }

        public void RearmarNoPonto(Transform pontoDeSpawn, bool posicaoManual = false)
        {
            RegistrarEstadoInicial();

            if (cancelRoutine != null)
            {
                StopCoroutine(cancelRoutine);
                cancelRoutine = null;
            }

            StopAllCoroutines();

            // Esse reset permite reutilizar a mesma cena com outro spawn
            // sem depender de reload completo entre fases.
            gameObject.SetActive(true);
            if (bombRoot != null) bombRoot.gameObject.SetActive(true);

            Transform escalaAlvo = bombRoot != null ? bombRoot : transform;
            escalaAlvo.localScale = escalaInicialDaRaiz;

            if (pontoDeSpawn != null)
            {
                transform.SetPositionAndRotation(pontoDeSpawn.position, pontoDeSpawn.rotation);
                if (!posicaoManual)
                    AjustarNoChaoDoSpawn();
            }

            if (bombLight != null) bombLight.enabled = true;
            if (sparkLight != null) sparkLight.enabled = true;

            defused = false;
            focusing = false;
            pulseClock = 0f;

            gerenciadorDeBomba?.PrepararNovaTentativa();
        }

        void AjustarNoChaoDoSpawn()
        {
            // Procura a superfície (mesa, bancada, prateleira, chão) mais próxima
            // logo abaixo da âncora de spawn. Os spawns ficam em y baixo e nem
            // sempre o XZ está exatamente sobre a tampa da mesa, então cobrimos
            // duas faixas: até ~1.5m acima do spawn (mesas/bancadas) e abaixo
            // dele (chão). Tetos e telhados ficam fora dessa janela e não viram
            // suporte por engano.
            Bounds bounds = ObterBoundsVisuais();
            Vector3 anchorPos = transform.position;

            // Começa o raio acima do topo atual da bomba pra nunca nascer dentro
            // da própria geometria, e estende bem pra baixo até o chão.
            float alturaOrigem = Mathf.Max(bounds.max.y, anchorPos.y) + 5f;
            Vector3 origin = new Vector3(anchorPos.x, alturaOrigem, anchorPos.z);
            float alcance = alturaOrigem - anchorPos.y + 30f;
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, alcance, ~0, QueryTriggerInteraction.Ignore);

            const float alturaMaxDeMobiliario = 1.5f;
            float limiteSuperior = anchorPos.y + alturaMaxDeMobiliario;

            // Preferimos uma superfície dentro da janela do mobiliário; se não
            // houver, caímos para qualquer superfície abaixo da bomba (o chão).
            bool achouProximo = false;
            float superficieProxima = 0f;
            bool achouAbaixo = false;
            float superficieAbaixo = 0f;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider col = hits[i].collider;
                if (col == null) continue;
                if (col.transform == transform || col.transform.IsChildOf(transform)) continue;
                if (Vector3.Dot(hits[i].normal, Vector3.up) < 0.7f) continue;

                float y = hits[i].point.y;

                if (y <= bounds.center.y && (!achouAbaixo || y > superficieAbaixo))
                {
                    achouAbaixo = true;
                    superficieAbaixo = y;
                }

                if (y <= limiteSuperior && (!achouProximo || y > superficieProxima))
                {
                    achouProximo = true;
                    superficieProxima = y;
                }
            }

            float superficieY;
            if (achouProximo) superficieY = superficieProxima;
            else if (achouAbaixo) superficieY = superficieAbaixo;
            else return; // Sem apoio embaixo: não levanta a bomba pro vazio.

            float deltaY = superficieY - bounds.min.y + SurfaceClearance;
            transform.position += Vector3.up * deltaY;
        }

        Bounds ObterBoundsVisuais()
        {
            Transform alvo = bombVisual != null ? bombVisual : transform;
            Renderer[] renderers = alvo.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return new Bounds(alvo.position, Vector3.one * 0.35f);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds;
        }

        void RegistrarEstadoInicial()
        {
            if (estadoInicialRegistrado) return;

            if (bombRoot == null) bombRoot = transform;
            escalaInicialDaRaiz = bombRoot != null ? bombRoot.localScale : transform.localScale;
            estadoInicialRegistrado = true;
        }
    }
}
