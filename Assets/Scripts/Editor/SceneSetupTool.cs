using UnityEngine;
using UnityEditor;
using ChessBattle.Game;
using ChessBattle.View;

namespace ChessBattle.Editor
{
    public class SceneSetupTool : EditorWindow
    {
        [MenuItem("Tools/Setup Chess Scene")]
        public static void SetupScene()
        {
            SetupGameManager();
            SetupAssets();
            SpawnPiecesInEditor();
            Debug.Log("Chess Battle Scene Setup Complete!");
        }

        [MenuItem("Tools/Spawn Pieces Only")]
        public static void SpawnPiecesOnly()
        {
            SetupAssets(); // Ensure assets loaded
            SpawnPiecesInEditor();
        }

        [MenuItem("Tools/Switch to High Poly Assets")]
        public static void SwitchToHighPoly()
        {
            string basePath = "Assets/Chess MEGA-pack/prefabs/pieces/HighPoly/";
            
            // Reload Asset
            string assetPath = "Assets/Settings/ChessAssets_LowPoly.asset";
            ChessAssets assets = AssetDatabase.LoadAssetAtPath<ChessAssets>(assetPath);
            if (assets == null)
            {
                Debug.LogError("Setup Chess Scene first!");
                return;
            }

            // White = Base Name (e.g. pawnHighPoly.prefab)
            assets.WhitePawn = LoadPrefab(basePath + "pawnHighPoly.prefab");
            assets.WhiteKnight = LoadPrefab(basePath + "knightHighPoly.prefab");
            assets.WhiteBishop = LoadPrefab(basePath + "bishopHighPoly.prefab");
            assets.WhiteRook = LoadPrefab(basePath + "rookHighPoly.prefab");
            assets.WhiteQueen = LoadPrefab(basePath + "queenHighPoly.prefab");
            assets.WhiteKing = LoadPrefab(basePath + "KingHighPoly.prefab"); // Note 'King' capitalization in file list

            // Black = " 1" Suffix (e.g. pawnHighPoly 1.prefab)
            assets.BlackPawn = LoadPrefab(basePath + "pawnHighPoly 1.prefab");
            assets.BlackKnight = LoadPrefab(basePath + "knightHighPoly 1.prefab");
            assets.BlackBishop = LoadPrefab(basePath + "bishopHighPoly 1.prefab");
            assets.BlackRook = LoadPrefab(basePath + "rookHighPoly 1.prefab");
            assets.BlackQueen = LoadPrefab(basePath + "queenHighPoly 1.prefab");
            assets.BlackKing = LoadPrefab(basePath + "KingHighPoly 1.prefab");

            EditorUtility.SetDirty(assets);
            AssetDatabase.SaveAssets();

            Debug.Log("Switched to High Poly Assets! Respawning...");
            SpawnPiecesInEditor();
        }

        [MenuItem("Tools/Process & Assign NEW FBX Assets")]
        public static void ProcessNewAssetsAndAssign()
        {
            // 1. Ensure Output Directory
            string outputDir = "Assets/Prefabs/ProcessedChess";
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder(outputDir)) AssetDatabase.CreateFolder("Assets/Prefabs", "ProcessedChess");

            // 2. Process Files
            // Structure: Assets/Low Poly Chess/Black/Bishop.fbx
            string sourceRoot = "Assets/Low Poly Chess/";
            string[] teams = new[] { "White", "Black" };
            string[] pieces = new[] { "Pawn", "Rook", "Knight", "Bishop", "Queen", "King" };

            ChessAssets assets = AssetDatabase.LoadAssetAtPath<ChessAssets>("Assets/Settings/ChessAssets_LowPoly.asset");
            if (assets == null)
            {
                Debug.LogError("Assets config not found!");
                return;
            }

            foreach (var team in teams)
            {
                foreach (var piece in pieces)
                {
                    string fbxPath = $"{sourceRoot}{team}/{piece}.fbx";
                    GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
                    
                    if (fbx == null)
                    {
                        Debug.LogWarning($"FBX not found at {fbxPath}");
                        continue;
                    }

                    // Create Wrapper
                    GameObject wrapper = new GameObject($"{team}_{piece}_Corrected");
                    GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
                    model.transform.SetParent(wrapper.transform);
                    model.transform.localPosition = Vector3.zero;
                    
                    // Fix Rotation: FBX imports often needed -90 on X
                    model.transform.localRotation = Quaternion.Euler(-90, 0, 0); 
                    wrapper.transform.localScale = Vector3.one * 0.5f;

                    // Save as Prefab
                    string prefabPath = $"{outputDir}/{team}_{piece}.prefab";
                    GameObject newPrefab = PrefabUtility.SaveAsPrefabAsset(wrapper, prefabPath);
                    
                    DestroyImmediate(wrapper);

                    // Assign to Assets
                    if (team == "White")
                    {
                         if (piece == "Pawn") assets.WhitePawn = newPrefab;
                         if (piece == "Knight") assets.WhiteKnight = newPrefab;
                         if (piece == "Bishop") assets.WhiteBishop = newPrefab;
                         if (piece == "Rook") assets.WhiteRook = newPrefab;
                         if (piece == "Queen") assets.WhiteQueen = newPrefab;
                         if (piece == "King") assets.WhiteKing = newPrefab;
                    }
                    else
                    {
                         if (piece == "Pawn") assets.BlackPawn = newPrefab;
                         if (piece == "Knight") assets.BlackKnight = newPrefab;
                         if (piece == "Bishop") assets.BlackBishop = newPrefab;
                         if (piece == "Rook") assets.BlackRook = newPrefab;
                         if (piece == "Queen") assets.BlackQueen = newPrefab;
                         if (piece == "King") assets.BlackKing = newPrefab;
                    }
                }
            }
            
            EditorUtility.SetDirty(assets);
            AssetDatabase.SaveAssets();
            Debug.Log("Processed all FBX assets (Geometry Only) and assigned them! Spawning...");
            SpawnPiecesInEditor();
        }

        private static void SetupGameManager()
        {
            // 1. Find or Create GameManager
            GameManager gm = FindFirstObjectByType<GameManager>();
            if (gm == null)
            {
                GameObject gmObj = new GameObject("GameManager");
                gm = gmObj.AddComponent<GameManager>();
                Debug.Log("Created GameManager");
            }

            // 2. Find or Create BoardView
            BoardView view = FindFirstObjectByType<BoardView>();
            if (view == null)
            {
                GameObject viewObj = new GameObject("BoardView");
                view = viewObj.AddComponent<BoardView>();
                viewObj.transform.position = Vector3.zero; // default
                Debug.Log("Created BoardView");
            }
            
            // Link them
            gm.BoardView = view;
            
            // Link Camera
            if (gm.MainCamera == null)
            {
                gm.MainCamera = Camera.main;
            }
            
            // Try to align BoardView to existing boardHighPoly if present
            GameObject boardMesh = GameObject.Find("boardHighPoly");
            if (boardMesh != null)
            {
                 // Usually board mesh pivot is center, so we might need to adjust.
                 // Assuming user placed board at 0,0,0
                 // BoardView.BoardOrigin usually wants corner a1.
                 // We will leave it at 0,0,0 for now and let user adjust.
                 view.transform.position = boardMesh.transform.position; // Just sync position
                 view.BoardOrigin = view.transform;
            }
            else
            {
                 view.BoardOrigin = view.transform;
            }
        }

        private static void SetupAssets()
        {
            // Create or Load Asset Config
            string assetPath = "Assets/Settings/ChessAssets_LowPoly.asset";
            ChessAssets assets = AssetDatabase.LoadAssetAtPath<ChessAssets>(assetPath);
            
            if (assets == null)
            {
                // Ensure directory exists
                if (!AssetDatabase.IsValidFolder("Assets/Settings"))
                {
                    AssetDatabase.CreateFolder("Assets", "Settings");
                }
                
                assets = ScriptableObject.CreateInstance<ChessAssets>();
                AssetDatabase.CreateAsset(assets, assetPath);
                Debug.Log("Created new ChessAssets config at " + assetPath);
            }
            
            // Populate Prefabs
            // Path: Assets/Chess MEGA-pack/prefabs/pieces/LowPoly1/
            string basePath = "Assets/Chess MEGA-pack/prefabs/pieces/LowPoly1/";
            
            // White (using default "pawnLowPoly1.prefab")
            assets.WhitePawn = LoadPrefab(basePath + "pawnLowPoly1 white.prefab");
            assets.WhiteKnight = LoadPrefab(basePath + "knightLowPoly1 white.prefab");
            assets.WhiteBishop = LoadPrefab(basePath + "bishopLowPoly1 white.prefab");
            assets.WhiteRook = LoadPrefab(basePath + "rookLowPoly1 white.prefab");
            assets.WhiteQueen = LoadPrefab(basePath + "queenLowPoly1 white.prefab");
            assets.WhiteKing = LoadPrefab(basePath + "kingLowPoly1 white.prefab");
            
            assets.BlackPawn = LoadPrefab(basePath + "pawnLowPoly1 black.prefab");
            assets.BlackKnight = LoadPrefab(basePath + "knightLowPoly1 black.prefab");
            assets.BlackBishop = LoadPrefab(basePath + "bishopLowPoly black.prefab");
            assets.BlackRook = LoadPrefab(basePath + "rookLowPoly1 black.prefab");
            assets.BlackQueen = LoadPrefab(basePath + "queenLowPoly1 black.prefab");
            assets.BlackKing = LoadPrefab(basePath + "kingLowPoly black.prefab");

            EditorUtility.SetDirty(assets);
            AssetDatabase.SaveAssets();

            // Assign to BoardView
            BoardView view = FindFirstObjectByType<BoardView>();
            if (view != null)
            {
                view.Assets = assets;
                EditorUtility.SetDirty(view);
                Debug.Log("[SceneSetupTool] Assigned Assets to BoardView.");
            }
            else
            {
                Debug.LogError("[SceneSetupTool] Could not find BoardView to assign assets!");
            }
        }

        private static GameObject LoadPrefab(string path)
        {
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) Debug.LogWarning($"[SceneSetupTool] Failed to load prefab at: {path}");
            return go;
        }

        private static void SpawnPiecesInEditor()
        {
            BoardView view = FindFirstObjectByType<BoardView>();
            if (view == null)
            {
                Debug.LogError("No BoardView found in scene!");
                return;
            }

            if (view.Assets == null)
            {
                 Debug.LogError("BoardView has no Assets assigned!");
                 return;
            }

            // Clear existing children
            while (view.transform.childCount > 0)
            {
                DestroyImmediate(view.transform.GetChild(0).gameObject);
            }

            // Use logic board to determine placement
            ChessBattle.Core.ChessBoard board = new ChessBattle.Core.ChessBoard();
            
            int spawnedCount = 0;
            for (int i = 0; i < 64; i++)
            {
                ChessBattle.Core.Piece p = board.Squares[i];
                if (p.Type != ChessBattle.Core.PieceType.None)
                {
                    GameObject prefab = view.Assets.GetPrefab(p.Type, p.Team);
                    if (prefab != null)
                    {
                        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, view.transform);
                        spawnedCount++;
                        
                        ChessBattle.Core.BoardPosition pos = new ChessBattle.Core.BoardPosition(i % 8, i / 8);
                        
                        // Calculate Position (Need internal helper or duplicate logic)
                        // Assuming BoardView logic: Origin + (File*Size, 0, Rank*Size)
                        // Let's assume view.BoardOrigin is view.transform if null
                        Vector3 origin = view.BoardOrigin != null ? view.BoardOrigin.position : view.transform.position;
                        Vector3 worldPos = origin + new Vector3(pos.File * view.SquareSize, 0, pos.Rank * view.SquareSize);
                        
                        go.transform.position = worldPos;
                        
                        // Rotation
                        if (p.Team == ChessBattle.Core.TeamColor.Black)
                        {
                            go.transform.rotation = Quaternion.Euler(0, 180, 0);
                        }

                        // Naming Convention for BoardView to find it later
                        go.name = $"Piece_{pos.File}_{pos.Rank}";
                    }
                    else
                    {
                        Debug.LogWarning($"[SceneSetupTool] Prefab missing for {p.Team} {p.Type}");
                    }
                }
            }
            Debug.Log($"[SceneSetupTool] Spawned {spawnedCount} pieces in Editor.");
            
            EditorUtility.SetDirty(view);
        }
    }
}
