using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ChessBattle.Editor
{
    public class VisualSetupTool : EditorWindow
    {
        [MenuItem("Tools/Make It Pretty (PostFX + Lights)")]
        public static void MakeItPretty()
        {
            SetupPostProcessing();
            SetupLighting();
            SetupCamera();
            SetupPostProcessing();
            SetupLighting();
            SetupCamera();
            SetupMaterials();
            Debug.Log("Scene Beautified! Tweaking keys: Volume -> Global Volume");
        }

        [MenuItem("Tools/Apply Fancy Materials Only")]
        public static void SetupMaterials()
        {
            // 1. Create/Load Materials
            string matDir = "Assets/Materials";
            if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");

            Material whiteMat = AssetDatabase.LoadAssetAtPath<Material>(matDir + "/ChessWhite.mat");
            if (whiteMat == null)
            {
                whiteMat = new Material(Shader.Find("Universal Render Pipeline/Lit")); 
                if (whiteMat.shader.name == "Hidden/InternalErrorShader") whiteMat.shader = Shader.Find("Standard");
                
                // Reduced brightness to prevent blowout (was 0.95)
                whiteMat.SetColor("_BaseColor", new Color(0.85f, 0.82f, 0.75f)); 
                whiteMat.SetFloat("_Smoothness", 0.25f); // Reduced smoothness to reduce specular highlights
                AssetDatabase.CreateAsset(whiteMat, matDir + "/ChessWhite.mat");
            }
            else
            {
                // Update existing material values
                whiteMat.SetColor("_BaseColor", new Color(0.85f, 0.82f, 0.75f));
                whiteMat.SetFloat("_Smoothness", 0.25f);
                EditorUtility.SetDirty(whiteMat);
            }

            Material blackMat = AssetDatabase.LoadAssetAtPath<Material>(matDir + "/ChessBlack.mat");
            if (blackMat == null)
            {
                blackMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (blackMat.shader.name == "Hidden/InternalErrorShader") blackMat.shader = Shader.Find("Standard");

                blackMat.SetColor("_BaseColor", new Color(0.15f, 0.15f, 0.17f)); // Charcoal
                blackMat.SetFloat("_Smoothness", 0.35f);
                AssetDatabase.CreateAsset(blackMat, matDir + "/ChessBlack.mat");
            }
            else
            {
                blackMat.SetColor("_BaseColor", new Color(0.15f, 0.15f, 0.17f));
                blackMat.SetFloat("_Smoothness", 0.35f);
                EditorUtility.SetDirty(blackMat);
            }

            // 2. Find all pieces in Scene and Apply
            GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int count = 0;
            foreach (var obj in allObjects)
            {
                if (obj.name.StartsWith("Piece_") || obj.name.Contains("Corrected")) 
                {
                    // Parsing Name: Piece_X_Y
                    string[] parts = obj.name.Split('_');
                    if (parts.Length >= 3)
                    {
                        if (int.TryParse(parts[2], out int y))
                        {
                            Material target = (y <= 1) ? whiteMat : blackMat;
                            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
                            foreach (var r in renderers) r.sharedMaterial = target;
                            count++;
                        }
                    }
                }
            }
            Debug.Log($"Applied Materials to {count} pieces.");
        }

        private static void SetupPostProcessing()
        {
            // 1. Find or Create Volume
            Volume volume = FindFirstObjectByType<Volume>();
            if (volume == null)
            {
                GameObject volObj = new GameObject("Global Volume");
                volume = volObj.AddComponent<Volume>();
                volume.isGlobal = true;
            }

            // 2. Ensure Profile
            if (volume.sharedProfile == null)
            {
                volume.sharedProfile = ScriptableObject.CreateInstance<VolumeProfile>();
                volume.sharedProfile.name = "Chess_Cinematic_Profile";
                AssetDatabase.CreateAsset(volume.sharedProfile, "Assets/Settings/Chess_Cinematic_Profile.asset");
                EditorUtility.SetDirty(volume);
            }

            // 3. Add Overrides
            // Bloom
            if (!volume.sharedProfile.TryGet(out Bloom bloom))
            {
                bloom = volume.sharedProfile.Add<Bloom>(true);
            }
            bloom.intensity.Override(0.5f); // Reduced from 1.5
            bloom.threshold.Override(0.95f);
            bloom.scatter.Override(0.7f);

            // Tonemapping
            if (!volume.sharedProfile.TryGet(out Tonemapping tone))
            {
                tone = volume.sharedProfile.Add<Tonemapping>(true);
            }
            tone.mode.Override(TonemappingMode.ACES);

            // Color Adjustments
            if (!volume.sharedProfile.TryGet(out ColorAdjustments colorAdj))
            {
                colorAdj = volume.sharedProfile.Add<ColorAdjustments>(true);
            }
            colorAdj.postExposure.Override(0.0f); // Reduced from 0.2
            colorAdj.contrast.Override(10f); // Reduced slightly
            colorAdj.saturation.Override(10f); // Reduced slightly

            // Vignette
            if (!volume.sharedProfile.TryGet(out Vignette vig))
            {
                vig = volume.sharedProfile.Add<Vignette>(true);
            }
            vig.intensity.Override(0.4f);
            vig.smoothness.Override(0.3f);
            
            EditorUtility.SetDirty(volume.sharedProfile);
            AssetDatabase.SaveAssets();
        }

        private static void SetupLighting()
        {
            // Disable existing Directional Light if any, or tune it
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    if (l.gameObject.name == "Directional Light" || l.gameObject.name == "Key Light")
                    {
                        // Use this as Key Light
                         l.color = new Color(1.0f, 0.95f, 0.8f); // Warm sun
                         l.intensity = 1.0f; // Reduced from 1.5
                         l.transform.rotation = Quaternion.Euler(50, -30, 0);
                         l.shadows = LightShadows.Soft;
                         l.gameObject.name = "Key Light";
                         // Ensure we don't return early so we can check/create Rim Light too? 
                         // Actually the previous logic returned, which means it wouldn't create Rim Light if Key existed.
                         // Let's remove the return to allow Rim Light creation/check.
                         // But we need to break loop if we found the key light to simply proceed.
                         // For simplicity, let's keep the return but check Rim Light inside here or outside?
                         // The original code returned. Let's stick to the previous structure but create Rim Light if missing.
                         
                         EnsureRimLight();
                         return; 
                    }
                }
            }

            // If no light found, create new Key Light
            GameObject keyInfo = new GameObject("Key Light");
            Light key = keyInfo.AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = new Color(1.0f, 0.95f, 0.8f);
            key.intensity = 1.0f;
            key.shadows = LightShadows.Soft;
            keyInfo.transform.rotation = Quaternion.Euler(50, -30, 0);

            EnsureRimLight();
        }

        private static void EnsureRimLight()
        {
             GameObject rimObj = GameObject.Find("Rim Light");
             if (rimObj == null)
             {
                rimObj = new GameObject("Rim Light");
                Light rim = rimObj.AddComponent<Light>();
                rim.type = LightType.Directional;
                rim.color = new Color(0.4f, 0.6f, 1.0f); // Cool blue
                rim.intensity = 0.5f; // Reduced from 0.8
                rim.shadows = LightShadows.None;
                rimObj.transform.rotation = Quaternion.Euler(40, 150, 0);
             }
             else
             {
                Light rim = rimObj.GetComponent<Light>();
                rim.intensity = 0.5f; // Update intensity
             }
        }

        private static void SetupCamera()
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                // Add Orbit Script if missing
                if (cam.GetComponent<ChessBattle.View.CameraOrbit>() == null)
                {
                    cam.gameObject.AddComponent<ChessBattle.View.CameraOrbit>();
                }
                
                // Color
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.15f, 0.15f, 0.18f); // Dark Slate Grey

                // Enable Post Processing (URP)
                var camData = cam.GetComponent<UniversalAdditionalCameraData>();
                if (camData == null)
                {
                    camData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
                }
                camData.renderPostProcessing = true;
                camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                camData.antialiasingQuality = AntialiasingQuality.High;
                
                EditorUtility.SetDirty(cam);
            }
        }
    }
}
