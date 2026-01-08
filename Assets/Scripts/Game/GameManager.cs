using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ChessBattle.Core;
using ChessBattle.View;

namespace ChessBattle.Game
{
    public class GameManager : MonoBehaviour
    {
        public BoardView BoardView;
        public Camera MainCamera;
        
        public enum GameMode { HumanVsHuman, AiVsAi }
        [Header("Game Settings")]
        public GameMode Mode = GameMode.HumanVsHuman;
        
        [Header("AI Configuration")]
        public ChessBattle.AI.LLMService AiService;
        public string WhitePersonality = "Aggressive Grandmaster";
        public string BlackPersonality = "Cautious Beginner";
        
        private ChessBoard _board;
        private MoveGenerator _moveGenerator; // Helper
        private BoardPosition _selectedSquare = new BoardPosition(-1, -1);
        private List<ChessMove> _legalMoves;
        private bool _isAiThinking = false;

        private void Start()
        {
            StartNewGame();
        }

        public void StartNewGame()
        {
            _board = new ChessBoard();
            _moveGenerator = new MoveGenerator();
            
            // Wire up view
            // Wire up view
            BoardView.Initialize(_board, OnSquareSelected);
            
            // Calculate initial moves
            _legalMoves = _moveGenerator.GenerateLegalMoves(_board);

            // Check AI Turn
            if (Mode == GameMode.AiVsAi)
            {
                StartCoroutine(AiTurnRoutine());
            }
        }

        private IEnumerator AiTurnRoutine()
        {
            if (_isAiThinking) yield break;
            _isAiThinking = true;

            // Wait a bit before move
            yield return new WaitForSeconds(1.0f);

            // 1. Prepare Data
            string fen = _board.GenerateFen();
            List<string> strMoves = new List<string>();
            foreach (var m in _legalMoves) strMoves.Add(m.ToString());

            // 2. Determine Personality
            string personality = _board.CurrentTurn == TeamColor.White ? WhitePersonality : BlackPersonality;
            Debug.Log($"[AI] Turn: {_board.CurrentTurn} ({personality}) thinking...");

            // 3. Request Move
            if (AiService == null)
            {
                 Debug.LogError("LLMService is not assigned in GameManager!");
                 _isAiThinking = false;
                 yield break;
            }

            var task = AiService.GetMoveAsync(fen, strMoves, personality);
            yield return new WaitUntil(() => task.IsCompleted);

            string moveStr = task.Result;
            Debug.Log($"[AI] Selected Move: {moveStr}");

            // 4. Parse & Execute
            ChessMove? selectedMove = null;
            foreach (var m in _legalMoves)
            {
                if (m.ToString() == moveStr)
                {
                    selectedMove = m;
                    break;
                }
            }

            if (selectedMove.HasValue)
            {
                BoardView.AnimateMove(selectedMove.Value, () => {
                   _board.MakeMove(selectedMove.Value);
                   BoardView.RefreshBoard();
                   _legalMoves = _moveGenerator.GenerateLegalMoves(_board);
                   
                   _isAiThinking = false;
                   
                   if (_legalMoves.Count == 0)
                   {
                       Debug.Log("Game Over!");
                   }
                   else
                   {
                       // Next Turn
                       StartCoroutine(AiTurnRoutine());
                   }
                });
            }
            else
            {
                Debug.LogError($"[AI] Returned illegal or unparseable move: {moveStr}. Retrying random...");
                // Fallback: Random move to unblock game
                var fallback = _legalMoves[Random.Range(0, _legalMoves.Count)];
                BoardView.AnimateMove(fallback, () => {
                   _board.MakeMove(fallback);
                   BoardView.RefreshBoard();
                   _legalMoves = _moveGenerator.GenerateLegalMoves(_board);
                   _isAiThinking = false;
                   StartCoroutine(AiTurnRoutine());
                });
            }
        }

        private void OnSquareSelected(BoardPosition pos)
        {
            if (Mode == GameMode.AiVsAi || _isAiThinking) return;

            if (_selectedSquare.IsValid())
            {
                // Try to move
                ChessMove? validMove = FindLegalMove(_selectedSquare, pos);
                if (validMove.HasValue)
                {
                    // Execute Move
                    BoardView.AnimateMove(validMove.Value, () => {
                        _board.MakeMove(validMove.Value);
                        _selectedSquare = new BoardPosition(-1, -1);
                        
                        // Sync visual state for edge cases (Castling/EnPassant)
                        BoardView.RefreshBoard();
                        
                        // Next Turn Logic
                        _legalMoves = _moveGenerator.GenerateLegalMoves(_board);
                        if (_legalMoves.Count == 0)
                        {
                             Debug.Log("Game Over! (Checkmate or Stalemate)");
                        }
                    });
                }
                else
                {
                    // Deselect or Select new
                    if (_board.GetPiece(pos).Team == _board.CurrentTurn)
                    {
                        _selectedSquare = pos;
                        // TODO: Highlight selected square
                        Debug.Log($"Selected {_board.CurrentTurn} Piece at {pos}");
                    }
                    else
                    {
                        _selectedSquare = new BoardPosition(-1, -1);
                    }
                }
            }
            else
            {
                // Select
                Piece p = _board.GetPiece(pos);
                if (p.Type != PieceType.None && p.Team == _board.CurrentTurn)
                {
                    _selectedSquare = pos;
                    Debug.Log($"Selected {p.Team} {p.Type} at {pos}");
                }
            }
        }

        private ChessMove? FindLegalMove(BoardPosition from, BoardPosition to)
        {
            foreach (var m in _legalMoves)
            {
                if (m.From.Equals(from) && m.To.Equals(to))
                {
                    // Handle Promotion Selection? For now auto-queen
                    // If m.PromotionType != None, we need to pick specific one. 
                    // MoveGenerator usually adds 4 moves for promotion. 
                    // We will return the Queen one by default if multiple match From/To.
                    if (m.PromotionType != PieceType.None && m.PromotionType != PieceType.Queen) continue;
                    
                    return m;
                }
            }
            return null;
        }
    }
}
