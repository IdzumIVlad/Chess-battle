using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ChessBattle.Game;

namespace ChessBattle.Editor
{
    public class AssetImportTool
    {
        [MenuItem("Tools/Assets/Setup Logos")]
        public static void SetupLogos()
        {
            AssetDatabase.Refresh();
            
            string pathOpenAI = "Assets/Art/Logos/openai_logo.png";
            string pathGrok = "Assets/Art/Logos/grok_logo.png";

            SetupSprite(pathOpenAI);
            SetupSprite(pathGrok);

            // Assign to GameManager
            GameManager gm = GameObject.FindFirstObjectByType<GameManager>();
            if (gm != null)
            {
                gm.LogoOpenAI = AssetDatabase.LoadAssetAtPath<Sprite>(pathOpenAI);
                gm.LogoGrok = AssetDatabase.LoadAssetAtPath<Sprite>(pathGrok);
                EditorUtility.SetDirty(gm);
                Debug.Log("Assigned Logos to GameManager!");
            }
            else
            {
                Debug.LogError("GameManager not found in scene!");
            }
        }

        private static void SetupSprite(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.alphaIsTransparency = true;
                    importer.SaveAndReimport();
                    Debug.Log($"Converted {path} to Sprite.");
                }
            }
            else
            {
                Debug.LogError($"Could not load importer for {path}");
            }
        }
    }
}
