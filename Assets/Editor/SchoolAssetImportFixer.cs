using System.IO;
using UnityEditor;
using UnityEngine;

namespace LiveWireEditor
{
    [InitializeOnLoad]
    static class SchoolAssetImportFixer
    {
        const string RootPath = "Assets/school";
        const string FallbackMaterialPath = "Assets/school/material/Materials/school_urp_fallback.mat";
        const string LastRunKey = "LiveWire.SchoolAssetImportFixer.LastRun";
        const string CurrentVersion = "2";

        static SchoolAssetImportFixer()
        {
            EditorApplication.delayCall += RunOncePerVersion;
        }

        [MenuItem("LiveWire/Fix Imported School Assets")]
        public static void FixImportedAssets()
        {
            Material fallback = EnsureFallbackMaterial();
            ConvertSchoolMaterials();
            FixPrefabMaterialSlots(fallback);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static void RunOncePerVersion()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (SessionState.GetString(LastRunKey, string.Empty) == CurrentVersion)
                return;

            SessionState.SetString(LastRunKey, CurrentVersion);
            if (AssetDatabase.IsValidFolder(RootPath))
                FixImportedAssets();
        }

        static void ConvertSchoolMaterials()
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) return;

            string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { RootPath });
            for (int i = 0; i < materialGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(materialGuids[i]);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null || material.shader == urpLit)
                    continue;

                Texture mainTexture = material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : null;
                Color color = material.HasProperty("_Color") ? material.GetColor("_Color") : material.color;
                float smoothness = material.HasProperty("_Glossiness") ? material.GetFloat("_Glossiness") : 0.14f;

                material.shader = urpLit;
                material.color = color;
                if (material.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", color);
                if (mainTexture != null && material.HasProperty("_BaseMap"))
                    material.SetTexture("_BaseMap", mainTexture);
                if (material.HasProperty("_Smoothness"))
                    material.SetFloat("_Smoothness", smoothness);

                EditorUtility.SetDirty(material);
            }
        }

        static void FixPrefabMaterialSlots(Material fallback)
        {
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { RootPath });
            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                bool changed = false;

                Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
                for (int r = 0; r < renderers.Length; r++)
                {
                    Material[] materials = renderers[r].sharedMaterials;
                    Material rendererFallback = FirstValidMaterial(materials);

                    for (int m = 0; m < materials.Length; m++)
                    {
                        if (materials[m] == null)
                        {
                            materials[m] = rendererFallback != null ? rendererFallback : fallback;
                            changed = true;
                        }
                    }

                    if (changed)
                        renderers[r].sharedMaterials = materials;
                }

                if (changed)
                    PrefabUtility.SaveAsPrefabAsset(root, path);

                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static Material FirstValidMaterial(Material[] materials)
        {
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] != null)
                    return materials[i];
            }

            return null;
        }

        static Material EnsureFallbackMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(FallbackMaterialPath);
            if (material != null)
                return material;

            string directory = Path.GetDirectoryName(FallbackMaterialPath);
            if (!string.IsNullOrEmpty(directory) && !AssetDatabase.IsValidFolder(directory))
            {
                Directory.CreateDirectory(directory);
                AssetDatabase.Refresh();
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            material = new Material(shader)
            {
                name = "school_urp_fallback",
                color = new Color(0.62f, 0.56f, 0.48f)
            };

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", material.color);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0.12f);

            AssetDatabase.CreateAsset(material, FallbackMaterialPath);
            return material;
        }
    }
}
