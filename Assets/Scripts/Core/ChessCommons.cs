using System;
using UnityEngine;

namespace ChessBattle.Core
{
    public enum TeamColor
    {
        White = 0,
        Black = 1,
        None = 2
    }

    public enum PieceType
    {
        None = 0,
        Pawn = 1,
        Knight = 2,
        Bishop = 3,
        Rook = 4,
        Queen = 5,
        King = 6
    }

    [Serializable]
    public struct BoardPosition : IEquatable<BoardPosition>
    {
        public int Rank; // 0-7 (Rows)
        public int File; // 0-7 (Cols)

        public BoardPosition(int file, int rank)
        {
            File = file;
            Rank = rank;
        }

        public bool IsValid()
        {
            return File >= 0 && File < 8 && Rank >= 0 && Rank < 8;
        }

        public override string ToString()
        {
            return $"{(char)('a' + File)}{Rank + 1}";
        }
        
        public bool Equals(BoardPosition other)
        {
            return Rank == other.Rank && File == other.File;
        }
    }

    public struct ChessMove
    {
        public BoardPosition From;
        public BoardPosition To;
        public PieceType PromotionType;

        public ChessMove(BoardPosition from, BoardPosition to, PieceType promotion = PieceType.None)
        {
            From = from;
            To = to;
            PromotionType = promotion;
        }

        public override string ToString()
        {
            string move = $"{From}{To}";
            if (PromotionType != PieceType.None)
            {
                // Simple promotion notation (e.g. e7e8q)
                switch(PromotionType)
                {
                    case PieceType.Queen: move += "q"; break;
                    case PieceType.Rook: move += "r"; break;
                    case PieceType.Bishop: move += "b"; break;
                    case PieceType.Knight: move += "n"; break;
                }
            }
            return move;
        }
    }
}
