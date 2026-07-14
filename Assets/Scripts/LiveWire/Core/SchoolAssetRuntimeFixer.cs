using System.Collections.Generic;
using UnityEngine;

namespace LiveWire
{
    public static class SchoolAssetRuntimeFixer
    {
        static readonly Dictionary<Material, Material> convertedMaterials = new();
        static Material fallbackMaterial;

        public static void FixScene()
        {
            Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include);
            for (int i = 0; i < renderers.Length; i++)
                FixRenderer(renderers[i]);

            SnapLoosePropsToSupport();
        }

        static void FixRenderer(Renderer renderer)
        {
            if (renderer == null) return;

            Material[] materials = renderer.sharedMaterials;
            bool changed = false;
            Material rendererFallback = null;

            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null)
                    continue;

                Material fixedMaterial = FixMaterial(materials[i]);
                if (fixedMaterial != materials[i])
                {
                    materials[i] = fixedMaterial;
                    changed = true;
                }

                if (rendererFallback == null)
                    rendererFallback = fixedMaterial;
            }

            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] != null)
                    continue;

                materials[i] = rendererFallback != null ? rendererFallback : GetFallbackMaterial();
                changed = true;
            }

            if (changed)
                renderer.sharedMaterials = materials;
        }

        static Material FixMaterial(Material source)
        {
            if (source == null)
                return GetFallbackMaterial();

            Shader shader = source.shader;
            if (shader != null && shader.name.StartsWith("Universal Render Pipeline/"))
                return source;

            if (convertedMaterials.TryGetValue(source, out Material cached) && cached != null)
                return cached;

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null)
                return source;

            Material converted = new Material(urpLit)
            {
                name = $"{source.name}_URP_Runtime"
            };

            Color color = Color.white;
            if (source.HasProperty("_BaseColor"))
                color = source.GetColor("_BaseColor");
            else if (source.HasProperty("_Color"))
                color = source.GetColor("_Color");

            converted.color = color;
            if (converted.HasProperty("_BaseColor"))
                converted.SetColor("_BaseColor", color);

            Texture mainTexture = null;
            if (source.HasProperty("_BaseMap"))
                mainTexture = source.GetTexture("_BaseMap");
            else if (source.HasProperty("_MainTex"))
                mainTexture = source.GetTexture("_MainTex");

            if (mainTexture != null && converted.HasProperty("_BaseMap"))
                converted.SetTexture("_BaseMap", mainTexture);

            if (source.HasProperty("_Glossiness") && converted.HasProperty("_Smoothness"))
                converted.SetFloat("_Smoothness", source.GetFloat("_Glossiness"));
            else if (converted.HasProperty("_Smoothness"))
                converted.SetFloat("_Smoothness", 0.18f);

            convertedMaterials[source] = converted;
            return converted;
        }

        static Material GetFallbackMaterial()
        {
            if (fallbackMaterial != null)
                return fallbackMaterial;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            fallbackMaterial = new Material(shader)
            {
                name = "SchoolAssetFallback_Runtime",
                color = new Color(0.62f, 0.56f, 0.48f)
            };

            if (fallbackMaterial.HasProperty("_BaseColor"))
                fallbackMaterial.SetColor("_BaseColor", fallbackMaterial.color);
            if (fallbackMaterial.HasProperty("_Smoothness"))
                fallbackMaterial.SetFloat("_Smoothness", 0.12f);

            return fallbackMaterial;
        }

        static void SnapLoosePropsToSupport()
        {
            Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);
            HashSet<Transform> processed = new();

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null) continue;

                Transform root = FindLoosePropRoot(renderer.transform);
                if (root == null || processed.Contains(root)) continue;
                processed.Add(root);

                SnapRootToSupport(root);
            }
        }

        static Transform FindLoosePropRoot(Transform transform)
        {
            Transform current = transform;
            Transform candidate = null;

            while (current != null)
            {
                string name = current.name.ToLowerInvariant();
                if (IsStructuralName(name) || current.GetComponent<BombManager>() != null || current.GetComponent<PlayerController>() != null)
                    break;

                if (IsLoosePropName(name))
                    candidate = current;

                current = current.parent;
            }

            return candidate;
        }

        static bool IsLoosePropName(string name)
        {
            return name.Contains("laptop")
                || name.Contains("notebook")
                || name.Contains("computer")
                || name.Contains("keyboard")
                || name.Contains("mouse")
                || name.Contains("book")
                || name.Contains("binder")
                || name.Contains("phone")
                || name.Contains("lamp")
                || name.Contains("projector")
                || name.Contains("speaker")
                || name.Contains("tray");
        }

        static bool IsStructuralName(string name)
        {
            return name.Contains("wall")
                || name.Contains("floor")
                || name.Contains("ceiling")
                || name.Contains("door")
                || name.Contains("window")
                || name.Contains("sala")
                || name.Contains("biblioteca")
                || name.Contains("laboratorio")
                || name.Contains("banheiro")
                || name.Contains("secretaria")
                || name.Contains("refeitorio")
                || name.Contains("corredor")
                || name.Contains("quadra")
                || name.Contains("spawn")
                || name.Contains("bomb")
                || name.Contains("hud")
                || name.Contains("camera");
        }

        static void SnapRootToSupport(Transform root)
        {
            if (!TryGetBounds(root, out Bounds bounds)) return;
            if (bounds.size.y < 0.03f || bounds.size.y > 1.2f) return;
            if (bounds.size.x > 2.2f || bounds.size.z > 2.2f) return;

            Bounds support;
            if (!TryFindSupport(root, bounds, out support))
                return;

            float delta = support.max.y - bounds.min.y + 0.015f;
            if (Mathf.Abs(delta) > 8f) return;

            Vector3 offset = Vector3.zero;
            if (Mathf.Abs(delta) >= 0.01f)
                offset.y = delta;

            // Mantém o objeto inteiro dentro do contorno horizontal do suporte
            // (a mesa). Sem isso, props soltos como notebooks ficam pendurados
            // pra fora da borda quando o snap original só ajusta Y.
            const float margem = 0.02f;
            float minX = support.min.x + bounds.extents.x + margem;
            float maxX = support.max.x - bounds.extents.x - margem;
            float minZ = support.min.z + bounds.extents.z + margem;
            float maxZ = support.max.z - bounds.extents.z - margem;

            if (minX <= maxX)
            {
                float alvoX = Mathf.Clamp(bounds.center.x, minX, maxX);
                offset.x = alvoX - bounds.center.x;
            }
            if (minZ <= maxZ)
            {
                float alvoZ = Mathf.Clamp(bounds.center.z, minZ, maxZ);
                offset.z = alvoZ - bounds.center.z;
            }

            if (offset.sqrMagnitude < 0.0001f) return;
            root.position += offset;
        }

        static bool TryFindSupport(Transform root, Bounds bounds, out Bounds support)
        {
            support = default;
            bool found = false;

            // Lança o raio de bem acima do objeto para garantir cobertura em qualquer altitude.
            Vector3 origin = new Vector3(bounds.center.x, bounds.min.y + 0.08f, bounds.center.z);
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 15f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; hits != null && i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.collider == null) continue;
                if (hit.collider.transform.IsChildOf(root)) continue;
                if (Vector3.Dot(hit.normal, Vector3.up) < 0.45f) continue;

                Bounds candidate = hit.collider.bounds;
                if (!found || candidate.max.y > support.max.y)
                {
                    support = candidate;
                    found = true;
                }
            }

            if (found) return true;

            Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || renderer.transform.IsChildOf(root)) continue;
                if (renderer.GetComponentInParent<Canvas>() != null) continue;

                Bounds candidate = renderer.bounds;
                if (candidate.size.x < 0.35f || candidate.size.z < 0.35f) continue;
                if (bounds.center.x < candidate.min.x - 0.1f || bounds.center.x > candidate.max.x + 0.1f) continue;
                if (bounds.center.z < candidate.min.z - 0.1f || bounds.center.z > candidate.max.z + 0.1f) continue;
                if (candidate.max.y > bounds.min.y + 0.3f) continue;
                if (candidate.max.y < bounds.min.y - 6f) continue;

                if (!found || candidate.max.y > support.max.y)
                {
                    support = candidate;
                    found = true;
                }
            }

            return found;
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

            return true;
        }
    }
}
