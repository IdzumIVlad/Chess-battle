using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using ChessBattle.View;

namespace ChessBattle.Editor
{
    public class HudSetupTool
    {
        [MenuItem("Tools/Create UI/Generate Game HUD")]
        public static void GenerateHUD()
        {
            // 1. Create Canvas
            GameObject canvasObj = new GameObject("GameHUD_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();

            // 2. Add GameHUD Component
            GameHUD hud = canvasObj.AddComponent<GameHUD>();

            // 3. Helper to create Text
            TextMeshProUGUI CreateText(GameObject parent, string name, string content, Vector2 pos, int size, Color color, FontStyles style = FontStyles.Normal)
            {
                GameObject obj = new GameObject(name);
                obj.transform.SetParent(parent.transform, false);
                TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
                tmp.text = content;
                tmp.fontSize = size;
                tmp.color = color;
                tmp.fontStyle = style;
                tmp.alignment = TextAlignmentOptions.Center;
                
                RectTransform rt = obj.GetComponent<RectTransform>();
                rt.anchoredPosition = pos;
                rt.sizeDelta = new Vector2(800, 200); // Increased from 400x100
                return tmp;
            }

            // 4. Top Bar (Turn Info)
            hud.TurnText = CreateText(canvasObj, "TurnText", "White's Turn", new Vector2(0, 450), 72, Color.white, FontStyles.Bold); // Size 48 -> 72
            hud.CheckText = CreateText(canvasObj, "CheckText", "CHECK!", new Vector2(0, 350), 100, Color.red, FontStyles.Bold); // Size 72 -> 100
            hud.CheckText.gameObject.SetActive(false);

            // 5. Player Panels (Avatars)
            GameObject CreatePanel(string name, Vector2 pos, Color color, out Image logoImg)
            {
                GameObject p = new GameObject(name);
                p.transform.SetParent(canvasObj.transform, false);
                Image img = p.AddComponent<Image>();
                img.color = new Color(0, 0, 0, 0.8f);
                
                RectTransform rt = p.GetComponent<RectTransform>();
                rt.anchoredPosition = pos;
                rt.sizeDelta = new Vector2(600, 240); // 300x120 -> 600x240
                
                // Avatar/Logo
                GameObject l = new GameObject("Logo");
                l.transform.SetParent(p.transform, false);
                logoImg = l.AddComponent<Image>();
                logoImg.color = Color.white; 
                
                RectTransform lrt = l.GetComponent<RectTransform>();
                lrt.anchoredPosition = new Vector2(-200, 0); // -100 -> -200
                lrt.sizeDelta = new Vector2(160, 160); // 80x80 -> 160x160
                
                return p;
            }

            Image wLogo, bLogo;
            hud.WhitePanel = CreatePanel("WhitePanel", new Vector2(-600, -400), Color.white, out wLogo); // Pos adjusted
            hud.WhiteLogo = wLogo;
            hud.WhiteNameText = CreateText(hud.WhitePanel, "Name", "Player (White)", new Vector2(80, 0), 48, Color.white); // 32 -> 48
            hud.WhiteNameText.alignment = TextAlignmentOptions.Left;
            
            hud.BlackPanel = CreatePanel("BlackPanel", new Vector2(600, -400), Color.black, out bLogo); 
            hud.BlackLogo = bLogo;
            hud.BlackNameText = CreateText(hud.BlackPanel, "Name", "AI (Black)", new Vector2(80, 0), 48, Color.white);
            hud.BlackNameText.alignment = TextAlignmentOptions.Left;


            // 6. Thought Bubbles
            GameObject CreateBubble(string name, Vector2 pos)
            {
                GameObject b = new GameObject(name);
                b.transform.SetParent(canvasObj.transform, false);
                Image img = b.AddComponent<Image>();
                img.color = new Color(1, 1, 1, 0.9f); 
                
                RectTransform rt = b.GetComponent<RectTransform>();
                rt.anchoredPosition = pos;
                rt.sizeDelta = new Vector2(600, 250); // 400x150 -> 600x250
                
                return b;
            }

            hud.WhiteBubble = CreateBubble("WhiteBubble", new Vector2(-600, -100)); // Adjusted pos
            TextMeshProUGUI wbTxt = CreateText(hud.WhiteBubble, "Text", "Thinking...", Vector2.zero, 36, Color.black); // 24 -> 36
            wbTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(560, 210);
            hud.WhiteBubbleText = wbTxt;

            hud.BlackBubble = CreateBubble("BlackBubble", new Vector2(600, -100));
            TextMeshProUGUI bbTxt = CreateText(hud.BlackBubble, "Text", "Thinking...", Vector2.zero, 36, Color.black);
            bbTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(560, 210);
            hud.BlackBubbleText = bbTxt;

            // Link to GameManager if found
            var gm = GameObject.FindFirstObjectByType<Game.GameManager>();
            if (gm != null)
            {
                gm.HUD = hud;
                EditorUtility.SetDirty(gm);
                Debug.Log("Assigned HUD to GameManager!");
            }
            
            Selection.activeGameObject = canvasObj;

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("HUD Generated!");
        }
    }
}
