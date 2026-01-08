using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ChessBattle.Core
{
    public class ChessBoard
    {
        public Piece[] Squares { get; private set; } // 64 Squares, 0 = a1, 7 = h1, 63 = h8
        public TeamColor CurrentTurn { get; private set; }
        
        // Castling Rights
        public bool WhiteCastlingKingside { get; set; }
        public bool WhiteCastlingQueenside { get; set; }
        public bool BlackCastlingKingside { get; set; }
        public bool BlackCastlingQueenside { get; set; }

        public BoardPosition EnPassantTarget { get; set; } // The square behind the pawn that just moved two steps

        public int HalfMoveClock { get; private set; } // For 50 move rule
        public int FullMoveNumber { get; private set; }

        public ChessBoard()
        {
            Squares = new Piece[64];
            EnPassantTarget = new BoardPosition(-1, -1);
            InitializeStandardGame();
        }

        public void InitializeStandardGame()
        {
            LoadFromFen("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
        }

        public void LoadFromFen(string fen)
        {
            // Reset board
            for (int i = 0; i < 64; i++) Squares[i] = Piece.None;

            string[] parts = fen.Split(' ');
            string placement = parts[0];
            string turn = parts[1];
            string castling = parts[2];
            string enPassant = parts[3];
            
            // 1. Placement
            int rank = 7;
            int file = 0;

            foreach (char c in placement)
            {
                if (c == '/')
                {
                    rank--;
                    file = 0;
                }
                else if (char.IsDigit(c))
                {
                    file += (int)char.GetNumericValue(c);
                }
                else
                {
                    TeamColor color = char.IsUpper(c) ? TeamColor.White : TeamColor.Black;
                    PieceType type = CharToPieceType(char.ToLower(c));
                    SetPiece(file, rank, new Piece(type, color));
                    file++;
                }
            }

            // 2. Turn
            CurrentTurn = turn == "w" ? TeamColor.White : TeamColor.Black;

            // 3. Castling
            WhiteCastlingKingside = castling.Contains("K");
            WhiteCastlingQueenside = castling.Contains("Q");
            BlackCastlingKingside = castling.Contains("k");
            BlackCastlingQueenside = castling.Contains("q");

            // 4. En Passant
            if (enPassant != "-")
            {
                int epFile = enPassant[0] - 'a';
                int epRank = enPassant[1] - '1';
                EnPassantTarget = new BoardPosition(epFile, epRank);
            }
            else
            {
                EnPassantTarget = new BoardPosition(-1, -1);
            }
        }

        public string GenerateFen()
        {
            StringBuilder sb = new StringBuilder();

            for (int rank = 7; rank >= 0; rank--)
            {
                int emptyCount = 0;
                for (int file = 0; file < 8; file++)
                {
                    Piece p = GetPiece(file, rank);
                    if (p.Type == PieceType.None)
                    {
                        emptyCount++;
                    }
                    else
                    {
                        if (emptyCount > 0)
                        {
                            sb.Append(emptyCount);
                            emptyCount = 0;
                        }
                        char c = PieceTypeToChar(p.Type);
                        sb.Append(p.Team == TeamColor.White ? char.ToUpper(c) : c);
                    }
                }
                if (emptyCount > 0) sb.Append(emptyCount);
                if (rank > 0) sb.Append('/');
            }

            sb.Append(" ");
            sb.Append(CurrentTurn == TeamColor.White ? "w" : "b");
            sb.Append(" ");
            
            string castling = "";
            if (WhiteCastlingKingside) castling += "K";
            if (WhiteCastlingQueenside) castling += "Q";
            if (BlackCastlingKingside) castling += "k";
            if (BlackCastlingQueenside) castling += "q";
            if (castling == "") castling = "-";
            sb.Append(castling);

            sb.Append(" ");
            if (EnPassantTarget.IsValid())
            {
                sb.Append(EnPassantTarget.ToString());
            }
            else
            {
                sb.Append("-");
            }
            
            // TODO: Add HalfMove and FullMove counters
            sb.Append(" 0 1"); 

            return sb.ToString();
        }

        public Piece GetPiece(int file, int rank)
        {
            if (file < 0 || file >= 8 || rank < 0 || rank >= 8) return Piece.None;
            return Squares[rank * 8 + file];
        }
        
        public Piece GetPiece(BoardPosition pos) => GetPiece(pos.File, pos.Rank);

        public void SetPiece(int file, int rank, Piece piece)
        {
            Squares[rank * 8 + file] = piece;
        }
        
        public void SetPiece(BoardPosition pos, Piece piece) => SetPiece(pos.File, pos.Rank, piece);

        // Basic move execution (No validation here, assumes move is valid)
        public void MakeMove(ChessMove move)
        {
            Piece piece = GetPiece(move.From);
            
            // Handle En Passant Capture
            if (piece.Type == PieceType.Pawn && move.To.Equals(EnPassantTarget))
            {
                // Remove the pawn that was captured (it's "behind" the target square)
                int captureRank = move.To.Rank + (piece.Team == TeamColor.White ? -1 : 1);
                SetPiece(move.To.File, captureRank, Piece.None);
            }

            // Handle Castling
            if (piece.Type == PieceType.King && Mathf.Abs(move.To.File - move.From.File) == 2)
            {
                // Kingside
                if (move.To.File == 6)
                {
                    Piece rook = GetPiece(7, move.From.Rank);
                    SetPiece(7, move.From.Rank, Piece.None);
                    SetPiece(5, move.From.Rank, rook);
                }
                // Queenside
                else if (move.To.File == 2)
                {
                    Piece rook = GetPiece(0, move.From.Rank);
                    SetPiece(0, move.From.Rank, Piece.None);
                    SetPiece(3, move.From.Rank, rook);
                }
            }
            
            // Handle Promotion
            if (move.PromotionType != PieceType.None)
            {
                piece.Type = move.PromotionType;
            }

            // Update Board
            SetPiece(move.To, piece);
            SetPiece(move.From, Piece.None);

            // Update State (Turn, etc.)
            CurrentTurn = CurrentTurn == TeamColor.White ? TeamColor.Black : TeamColor.White;
            
            // Update En Passant Target
            EnPassantTarget = new BoardPosition(-1, -1);
            if (piece.Type == PieceType.Pawn && Mathf.Abs(move.To.Rank - move.From.Rank) == 2)
            {
                int epRank = (move.From.Rank + move.To.Rank) / 2;
                EnPassantTarget = new BoardPosition(move.From.File, epRank);
            }

            // Update Castling Rights (Naive implementation: if King or Rook moves/captured, lose rights)
            // TODO: More robust check
            if (piece.Type == PieceType.King)
            {
                if (piece.Team == TeamColor.White) { WhiteCastlingKingside = false; WhiteCastlingQueenside = false; }
                else { BlackCastlingKingside = false; BlackCastlingQueenside = false; }
            }
        }

        private PieceType CharToPieceType(char c)
        {
            switch (c)
            {
                case 'p': return PieceType.Pawn;
                case 'n': return PieceType.Knight;
                case 'b': return PieceType.Bishop;
                case 'r': return PieceType.Rook;
                case 'q': return PieceType.Queen;
                case 'k': return PieceType.King;
                default: return PieceType.None;
            }
        }

        private char PieceTypeToChar(PieceType type)
        {
            switch (type)
            {
                case PieceType.Pawn: return 'p';
                case PieceType.Knight: return 'n';
                case PieceType.Bishop: return 'b';
                case PieceType.Rook: return 'r';
                case PieceType.Queen: return 'q';
                case PieceType.King: return 'k';
                default: return ' ';
            }
        }
    }
}
