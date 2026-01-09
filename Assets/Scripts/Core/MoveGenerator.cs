using System.Collections.Generic;
using UnityEngine;

namespace ChessBattle.Core
{
    public class MoveGenerator
    {
        private ChessBoard _board;

        // Offsets
        private static readonly int[,] KnightOffsets = { { 1, 2 }, { 1, -2 }, { -1, 2 }, { -1, -2 }, { 2, 1 }, { 2, -1 }, { -2, 1 }, { -2, -1 } };
        private static readonly int[,] KingOffsets = { { 0, 1 }, { 0, -1 }, { 1, 0 }, { -1, 0 }, { 1, 1 }, { 1, -1 }, { -1, 1 }, { -1, -1 } };
        private static readonly int[,] BishopOffsets = { { 1, 1 }, { 1, -1 }, { -1, 1 }, { -1, -1 } };
        private static readonly int[,] RookOffsets = { { 0, 1 }, { 0, -1 }, { 1, 0 }, { -1, 0 } };

        public List<ChessMove> GenerateLegalMoves(ChessBoard board)
        {
            _board = board;
            List<ChessMove> pseudoLegalMoves = GeneratePseudoLegalMoves();
            List<ChessMove> legalMoves = new List<ChessMove>();

            foreach (var move in pseudoLegalMoves)
            {
                if (IsMoveLegal(move))
                {
                    legalMoves.Add(move);
                }
            }

            return legalMoves;
        }

        public bool IsKingInCheck(ChessBoard board, TeamColor kingColor)
        {
            _board = board;
            BoardPosition kingPos = FindKing(board, kingColor);
            TeamColor attacker = (kingColor == TeamColor.White) ? TeamColor.Black : TeamColor.White;
            return IsSquareAttacked(board, kingPos, attacker);
        }

        private List<ChessMove> GeneratePseudoLegalMoves()
        {
            List<ChessMove> moves = new List<ChessMove>();

            for (int file = 0; file < 8; file++)
            {
                for (int rank = 0; rank < 8; rank++)
                {
                    Piece piece = _board.GetPiece(file, rank);
                    if (piece.Type != PieceType.None && piece.Team == _board.CurrentTurn)
                    {
                        GenerateMovesForPiece(file, rank, piece, moves);
                    }
                }
            }

            return moves;
        }

        private void GenerateMovesForPiece(int file, int rank, Piece piece, List<ChessMove> moves)
        {
            BoardPosition from = new BoardPosition(file, rank);

            switch (piece.Type)
            {
                case PieceType.Pawn:
                    GeneratePawnMoves(file, rank, piece.Team, moves);
                    break;
                case PieceType.Knight:
                    GenerateSteppingMoves(file, rank, KnightOffsets, piece.Team, moves);
                    break;
                case PieceType.King:
                    GenerateSteppingMoves(file, rank, KingOffsets, piece.Team, moves);
                    GenerateCastlingMoves(file, rank, piece.Team, moves);
                    break;
                case PieceType.Bishop:
                    GenerateSlidingMoves(file, rank, BishopOffsets, piece.Team, moves);
                    break;
                case PieceType.Rook:
                    GenerateSlidingMoves(file, rank, RookOffsets, piece.Team, moves);
                    break;
                case PieceType.Queen:
                    GenerateSlidingMoves(file, rank, BishopOffsets, piece.Team, moves);
                    GenerateSlidingMoves(file, rank, RookOffsets, piece.Team, moves);
                    break;
            }
        }

        private void GeneratePawnMoves(int file, int rank, TeamColor team, List<ChessMove> moves)
        {
            int direction = team == TeamColor.White ? 1 : -1;
            int startRank = team == TeamColor.White ? 1 : 6;
            int promotionRank = team == TeamColor.White ? 7 : 0;
            BoardPosition from = new BoardPosition(file, rank);

            // Forward 1
            if (IsEmpty(file, rank + direction))
            {
                AddPawnMove(from, new BoardPosition(file, rank + direction), promotionRank, moves);

                // Forward 2
                if (rank == startRank && IsEmpty(file, rank + (direction * 2)))
                {
                    AddPawnMove(from, new BoardPosition(file, rank + (direction * 2)), promotionRank, moves);
                }
            }

            // Captures
            TryPawnCapture(from, file - 1, rank + direction, team, moves, promotionRank);
            TryPawnCapture(from, file + 1, rank + direction, team, moves, promotionRank);
        }

        private void TryPawnCapture(BoardPosition from, int toFile, int toRank, TeamColor team, List<ChessMove> moves, int promotionRank)
        {
            BoardPosition to = new BoardPosition(toFile, toRank);
            if (!to.IsValid()) return;

            Piece target = _board.GetPiece(to);
            
            // Standard capture
            if (target.Type != PieceType.None && target.Team != team)
            {
                AddPawnMove(from, to, promotionRank, moves);
            }
            // En Passant
            else if (to.Equals(_board.EnPassantTarget))
            {
                AddPawnMove(from, to, promotionRank, moves);
            }
        }

        private void AddPawnMove(BoardPosition from, BoardPosition to, int promotionRank, List<ChessMove> moves)
        {
            if (to.Rank == promotionRank)
            {
                moves.Add(new ChessMove(from, to, PieceType.Queen));
                moves.Add(new ChessMove(from, to, PieceType.Rook));
                moves.Add(new ChessMove(from, to, PieceType.Bishop));
                moves.Add(new ChessMove(from, to, PieceType.Knight));
            }
            else
            {
                moves.Add(new ChessMove(from, to));
            }
        }

        private void GenerateSteppingMoves(int file, int rank, int[,] offsets, TeamColor team, List<ChessMove> moves)
        {
            BoardPosition from = new BoardPosition(file, rank);
            for (int i = 0; i < offsets.GetLength(0); i++)
            {
                int nextFile = file + offsets[i, 0];
                int nextRank = rank + offsets[i, 1];
                
                if (IsPositionValid(nextFile, nextRank))
                {
                    Piece target = _board.GetPiece(nextFile, nextRank);
                    if (target.Type == PieceType.None || target.Team != team)
                    {
                        moves.Add(new ChessMove(from, new BoardPosition(nextFile, nextRank)));
                    }
                }
            }
        }

        private void GenerateSlidingMoves(int file, int rank, int[,] offsets, TeamColor team, List<ChessMove> moves)
        {
            BoardPosition from = new BoardPosition(file, rank);
            for (int i = 0; i < offsets.GetLength(0); i++)
            {
                int dx = offsets[i, 0];
                int dy = offsets[i, 1];
                int nextFile = file + dx;
                int nextRank = rank + dy;

                while (IsPositionValid(nextFile, nextRank))
                {
                    Piece target = _board.GetPiece(nextFile, nextRank);
                    if (target.Type == PieceType.None)
                    {
                        moves.Add(new ChessMove(from, new BoardPosition(nextFile, nextRank)));
                    }
                    else
                    {
                        if (target.Team != team)
                        {
                            moves.Add(new ChessMove(from, new BoardPosition(nextFile, nextRank)));
                        }
                        break; // Blocked
                    }
                    nextFile += dx;
                    nextRank += dy;
                }
            }
        }

        private void GenerateCastlingMoves(int file, int rank, TeamColor team, List<ChessMove> moves)
        {
            // Usually assumes King is at default position (e1 for white) but let's be strict
            if (team == TeamColor.White)
            {
                if (_board.WhiteCastlingKingside && IsPathEmpty(5, 0, 6, 0) && !IsSquareAttacked(4, 0, TeamColor.Black) && !IsSquareAttacked(5, 0, TeamColor.Black))
                    moves.Add(new ChessMove(new BoardPosition(4, 0), new BoardPosition(6, 0)));
                if (_board.WhiteCastlingQueenside && IsPathEmpty(1, 0, 3, 0) && !IsSquareAttacked(4, 0, TeamColor.Black) && !IsSquareAttacked(3, 0, TeamColor.Black))
                    moves.Add(new ChessMove(new BoardPosition(4, 0), new BoardPosition(2, 0)));
            }
            else
            {
                if (_board.BlackCastlingKingside && IsPathEmpty(5, 7, 6, 7) && !IsSquareAttacked(4, 7, TeamColor.White) && !IsSquareAttacked(5, 7, TeamColor.White))
                    moves.Add(new ChessMove(new BoardPosition(4, 7), new BoardPosition(6, 7)));
                if (_board.BlackCastlingQueenside && IsPathEmpty(1, 7, 3, 7) && !IsSquareAttacked(4, 7, TeamColor.White) && !IsSquareAttacked(3, 7, TeamColor.White))
                    moves.Add(new ChessMove(new BoardPosition(4, 7), new BoardPosition(2, 7)));
            }
        }

        private bool IsMoveLegal(ChessMove move)
        {
            // 1. Simulate move
            Piece capturedPiece = _board.GetPiece(move.To); // Handle basic capture
            ChessBoard simBoard = CloneBoard(_board); // Better: We need a way to Undo or Clone efficiently. 
            // Cloning full board is expensive but safest for now. Optimization: MakeUndo
            
            simBoard.MakeMove(move);
            
            // 2. Check if King is attacked
            BoardPosition kingPos = FindKing(simBoard, _board.CurrentTurn); // Current turn is actually the one who moved
            
            // Note: After MakeMove, CurrentTurn flips. So we need to check if the 'previous' player (who just moved) is in check.
            // But MakeMove flips it, so simBoard.CurrentTurn is the OPPONENT.
            // We want to know if 'kingPos' (which belongs to the player who just moved) is attacked by simBoard.CurrentTurn (the opponent).
            
            // The FindKing should find the King of the player who MADE the move.
            // If _board.CurrentTurn was White, simBoard.CurrentTurn is Black.
            // We want to check if White King is under attack by Black.
            
            TeamColor playerWhoMoved = _board.CurrentTurn; 
            // WAIT: _board.MakeMove flips turn? Yes.
            // So if I call IsSquareAttacked on simBoard, I should check if 'playerWhoMoved' King is attacked by 'simBoard.CurrentTurn' (which is the opponent).
            
            return !IsSquareAttacked(simBoard, kingPos, simBoard.CurrentTurn);
        }

        private bool IsSquareAttacked(int file, int rank, TeamColor attackerColor)
        {
            return IsSquareAttacked(_board, new BoardPosition(file, rank), attackerColor);
        }

        private bool IsSquareAttacked(ChessBoard board, BoardPosition pos, TeamColor attackerColor)
        {
            // Iterate all pieces of attackerColor and see if they can hit 'pos'
            // Optimization: Look outwards from 'pos' like a Queen/Knight and see if we hit an enemy piece.
            
            // 1. Check Pawn attacks
            int pawnDir = attackerColor == TeamColor.White ? -1 : 1; // Pawns attack "backwards" relative to their move dir if we are looking from heavy piece POV? 
            // No, easier: iterate all squares? No slow.
            // Look from 'pos' for Knights
            foreach (var off in new int[,] { { 1, 2 }, { 1, -2 }, { -1, 2 }, { -1, -2 }, { 2, 1 }, { 2, -1 }, { -2, 1 }, { -2, -1 } }.Iterate())
            {
                 Piece p = board.GetPiece(pos.File + off[0], pos.Rank + off[1]);
                 if (p.Type == PieceType.Knight && p.Team == attackerColor) return true;
            }
            
            // Look for Sliding (Rook/Queen)
            foreach (var off in new int[,] { { 0, 1 }, { 0, -1 }, { 1, 0 }, { -1, 0 } }.Iterate())
            {
                int dist = 1;
                while(true)
                {
                    int nextFile = pos.File + off[0]*dist;
                    int nextRank = pos.Rank + off[1]*dist;
                    if (!IsPositionValid(nextFile, nextRank)) break;

                    Piece p = board.GetPiece(nextFile, nextRank);
                    if (p.Type == PieceType.None) { dist++; continue; }
                    if (p.Team == attackerColor && (p.Type == PieceType.Rook || p.Type == PieceType.Queen)) return true;
                    break;
                }
            }
            
            // Look for Sliding (Bishop/Queen)
            foreach (var off in new int[,] { { 1, 1 }, { 1, -1 }, { -1, 1 }, { -1, -1 } }.Iterate())
            {
                int dist = 1;
                while(true)
                {
                    int nextFile = pos.File + off[0]*dist;
                    int nextRank = pos.Rank + off[1]*dist;
                    if (!IsPositionValid(nextFile, nextRank)) break;

                    Piece p = board.GetPiece(nextFile, nextRank);
                    if (p.Type == PieceType.None) { dist++; continue; }
                    if (p.Team == attackerColor && (p.Type == PieceType.Bishop || p.Type == PieceType.Queen)) return true;
                    break;
                }
            }
            
            // Look for King
            foreach (var off in new int[,] { { 0, 1 }, { 0, -1 }, { 1, 0 }, { -1, 0 }, { 1, 1 }, { 1, -1 }, { -1, 1 }, { -1, -1 } }.Iterate())
            {
                 Piece p = board.GetPiece(pos.File + off[0], pos.Rank + off[1]);
                 if (p.Type == PieceType.King && p.Team == attackerColor) return true;
            }

            // Look for Pawns
            // If attacker is White, they are "below" (Rank 0->7). They attack upwards.
            // So if we are at 'pos', we look "downwards" to find White pawns.
            int attackDir = attackerColor == TeamColor.White ? -1 : 1; 
            Piece p1 = board.GetPiece(pos.File - 1, pos.Rank + attackDir);
            Piece p2 = board.GetPiece(pos.File + 1, pos.Rank + attackDir);
            if (p1.Type == PieceType.Pawn && p1.Team == attackerColor) return true;
            if (p2.Type == PieceType.Pawn && p2.Team == attackerColor) return true;

            return false;
        }

        private ChessBoard CloneBoard(ChessBoard original)
        {
            // Deep clone logic (or just create new from FEN?)
            ChessBoard newBoard = new ChessBoard();
            newBoard.LoadFromFen(original.GenerateFen()); // Inefficient but reliable for prototyping
            return newBoard;
        }

        private BoardPosition FindKing(ChessBoard board, TeamColor color)
        {
            for (int i = 0; i < 64; i++)
            {
                if (board.Squares[i].Type == PieceType.King && board.Squares[i].Team == color)
                {
                    return new BoardPosition(i % 8, i / 8);
                }
            }
            return new BoardPosition(-1, -1);
        }

        private bool IsPositionValid(int file, int rank) => file >= 0 && file < 8 && rank >= 0 && rank < 8;
        private bool IsEmpty(int file, int rank) => IsPositionValid(file, rank) && _board.GetPiece(file, rank).Type == PieceType.None;
        
        private bool IsPathEmpty(int fileStart, int rankStart, int fileEnd, int rankEnd)
        {
            // Horizontal check only for castling
             int dir = fileEnd > fileStart ? 1 : -1;
             for (int i = fileStart; i != fileEnd; i += dir)
             {
                 if (!_board.GetPiece(i, rankStart).IsColor(TeamColor.None) && i != fileStart) return false;
             }
             return true; // Simplified 
        }
    }
    
    // Helper extension
    public static class ArrayExt
    {
        public static IEnumerable<int[]> Iterate(this int[,] array)
        {
            for (int i = 0; i < array.GetLength(0); i++)
            {
                yield return new int[] { array[i, 0], array[i, 1] };
            }
        }
    }
}
