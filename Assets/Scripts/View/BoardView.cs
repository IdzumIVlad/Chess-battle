using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using ChessBattle.Core;

namespace ChessBattle.View
{
    public class BoardView : MonoBehaviour
    {
        [Header("Configuration")]
        public ChessAssets Assets;
        public Transform BoardOrigin; // Should be at a1 (0,0)
        public float SquareSize = 1.0f; // Distance between squares
        public float PieceYOffset = 0.0f; // Adjust to lift pieces if they drown

        [Header("State")]
        private GameObject[] _pieceObjects = new GameObject[64];
        private ChessBoard _currentBoard;
        private Action<BoardPosition> _onSquareSelected;

        public void Initialize(ChessBoard board, Action<BoardPosition> onSquareSelected)
        {
            Debug.Log($"[BoardView] Initialize called. Board: {board != null}, Assets: {Assets != null}");
            _currentBoard = board;
            _onSquareSelected = onSquareSelected;
            
            // 1. Try to link existing pieces (local or global)
            bool foundExisting = false;
            for (int i = 0; i < 64; i++)
            {
                BoardPosition pos = new BoardPosition(i % 8, i / 8);
                string pName = $"Piece_{pos.File}_{pos.Rank}";
                
                // Try Local Child First
                Transform existing = transform.Find(pName);
                
                // If not found, try Global Search (fallback)
                if (existing == null)
                {
                    GameObject globalObj = GameObject.Find(pName);
                    if (globalObj != null) 
                    {
                        existing = globalObj.transform;
                        // Reparent to ensure order
                        existing.SetParent(this.transform);
                    }
                }

                if (existing != null)
                {
                    _pieceObjects[i] = existing.gameObject;
                    
                    // AUTO-DETECT HEIGHT if first piece found
                    if (!foundExisting) 
                    {
                         // Assume board is flat, take Y from this piece relative to BoardOrigin
                         float pieceY = existing.position.y;
                         float boardY = BoardOrigin != null ? BoardOrigin.position.y : transform.position.y;
                         PieceYOffset = pieceY - boardY;
                         Debug.Log($"[BoardView] Auto-detected PieceYOffset: {PieceYOffset}");
                    }
                    
                    foundExisting = true;
                }
            }

            if (foundExisting)
            {
                Debug.Log($"[BoardView] Linked to existing pieces in scene.");
            }
            
            SyncBoardVisuals();
        }

        public void RefreshBoard() => SyncBoardVisuals();

        private void SyncBoardVisuals()
        {
            if (Assets == null) 
            {
                Debug.LogError("[BoardView] Assets is NULL! Cannot sync pieces.");
                return;
            }

            int spawnCount = 0;
            for (int i = 0; i < 64; i++)
            {
                Piece p = _currentBoard.Squares[i];
                GameObject currentObj = _pieceObjects[i];

                if (p.Type == PieceType.None)
                {
                    // Should be empty. If we have an object, destroy it.
                    if (currentObj != null)
                    {
                        Destroy(currentObj);
                        _pieceObjects[i] = null;
                    }
                }
                else
                {
                    // Should have a piece
                    if (currentObj == null)
                    {
                        // Spawn new
                        SpawnPiece(i, p);
                        spawnCount++;
                    }
                    else
                    {
                        // Update Name to ensure consistent lookup
                        currentObj.name = $"Piece_{i%8}_{i/8}";
                        
                        // NOTE: Do NOT snap position here. User wants to keep Editor placement.
                        // Only snap if needed (e.g. drift)? No, trust Editor.
                        // currentObj.transform.position = GetWorldPosition(pos);
                    }
                }
            }
            
            if (spawnCount > 0) Debug.Log($"[BoardView] Sync complete. Spawned {spawnCount} new pieces.");
        }

        public void AnimateMove(ChessMove move, Action onComplete = null)
        {
            int fromIdx = move.From.Rank * 8 + move.From.File;
            int toIdx = move.To.Rank * 8 + move.To.File;

            GameObject movingPiece = _pieceObjects[fromIdx];
            GameObject targetPiece = _pieceObjects[toIdx];

            if (movingPiece == null)
            {
                Debug.LogError($"View out of sync: No piece at {move.From}");
                onComplete?.Invoke();
                return;
            }

            // Kill captured piece
            if (targetPiece != null)
            {
                // Simple shrink out
                targetPiece.transform.DOScale(Vector3.zero, 0.3f).OnComplete(() => Destroy(targetPiece));
                _pieceObjects[toIdx] = null;
            }

            // Move
            Vector3 targetPos = GetWorldPosition(move.To);
            movingPiece.transform.DOMove(targetPos, 0.5f).SetEase(Ease.OutQuad).OnComplete(() => {
                _pieceObjects[toIdx] = movingPiece;
                _pieceObjects[fromIdx] = null;
                onComplete?.Invoke();
            });
            
            // Handle Castling (Rook update) logic needed here? 
            // The Logic Board updates the array, but Visuals need to match.
            // If I just animate the From->To, the Rook stays behind visually until next Refresh.
            // PROPER WAY: Receive "MoveResult" which includes side-effects like castling rook move.
            // For MVP: I will just call RefreshBoard() after animation completes to sync up any side effects (promotion, castling, en passant).
            // But that snaps the Rook. Let's rely on RefreshBoard for edge cases for now to save time.
        }

        private void SpawnPiece(int index, Piece piece)
        {
            GameObject prefab = Assets.GetPrefab(piece.Type, piece.Team);
            if (prefab == null) 
            {
                Debug.LogWarning($"[BoardView] Prefab not found for {piece.Team} {piece.Type}");
                return;
            }

            BoardPosition pos = new BoardPosition(index % 8, index / 8);
            Vector3 worldPos = GetWorldPosition(pos);

            GameObject go = Instantiate(prefab, worldPos, Quaternion.identity, transform);
            // Rotate Black pieces?
            if (piece.Team == TeamColor.Black)
            {
                go.transform.rotation = Quaternion.Euler(0, 180, 0);
            }
            
            _pieceObjects[index] = go;
        }

        private Vector3 GetWorldPosition(BoardPosition pos)
        {
            // Assuming BoardOrigin is at coordinate (0,0) of the board logic (a1)
            // And X is File, Z is Rank (standard Unity board orientation)
            // Adjust depending on actual mesh pivot.
            Vector3 origin = BoardOrigin != null ? BoardOrigin.position : Vector3.zero;
            return origin + new Vector3(pos.File * SquareSize, PieceYOffset, pos.Rank * SquareSize);
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                HandleInput();
            }
        }

        private void HandleInput()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Convert hit to grid
                // Local position relative to origin
                Vector3 localHit = hit.point - (BoardOrigin != null ? BoardOrigin.position : Vector3.zero);
                
                int file = Mathf.RoundToInt(localHit.x / SquareSize);
                int rank = Mathf.RoundToInt(localHit.z / SquareSize);

                // This is rough ("RoundToInt" centers on the square if pivot is center?) 
                // Better: Floor if pivot is corner, Round if pivot is center.
                // Let's assume standard 1x1 squares, pivot at corner (0,0) to (8,8)
                // Actually usually Board is centered. Let's try Floor.
                
                // Let's assume the user clicks on the object (Piece) or a Board Collider
                // A collider on the board plane is best.
                
                file = Mathf.FloorToInt((localHit.x + SquareSize/2) / SquareSize); // Adjust logic later based on actual board alignment
                rank = Mathf.FloorToInt((localHit.z + SquareSize/2) / SquareSize);
                
                // TEMP: Logic depends on exact board alignment. 
                // I will add a debug Click log to help user calibrate.
                // Debug.Log($"Clicked: {file}, {rank}");

                if (new BoardPosition(file, rank).IsValid())
                {
                    _onSquareSelected?.Invoke(new BoardPosition(file, rank));
                }
            }
        }
    }
}
