using UnityEngine;
using ChessBattle.Core;

namespace ChessBattle.View
{
    [CreateAssetMenu(fileName = "ChessAssets", menuName = "Chess/Assets")]
    public class ChessAssets : ScriptableObject
    {
        [Header("Prefabs (White)")]
        public GameObject WhitePawn;
        public GameObject WhiteKnight;
        public GameObject WhiteBishop;
        public GameObject WhiteRook;
        public GameObject WhiteQueen;
        public GameObject WhiteKing;

        [Header("Prefabs (Black)")]
        public GameObject BlackPawn;
        public GameObject BlackKnight;
        public GameObject BlackBishop;
        public GameObject BlackRook;
        public GameObject BlackQueen;
        public GameObject BlackKing;

        [Header("Materials")]
        public Material SelectedSquareMat;
        public Material LegalMoveMat;
        public Material LastMoveMat;

        public GameObject GetPrefab(PieceType type, TeamColor color)
        {
            if (color == TeamColor.White)
            {
                switch (type)
                {
                    case PieceType.Pawn: return WhitePawn;
                    case PieceType.Knight: return WhiteKnight;
                    case PieceType.Bishop: return WhiteBishop;
                    case PieceType.Rook: return WhiteRook;
                    case PieceType.Queen: return WhiteQueen;
                    case PieceType.King: return WhiteKing;
                }
            }
            else
            {
                switch (type)
                {
                    case PieceType.Pawn: return BlackPawn;
                    case PieceType.Knight: return BlackKnight;
                    case PieceType.Bishop: return BlackBishop;
                    case PieceType.Rook: return BlackRook;
                    case PieceType.Queen: return BlackQueen;
                    case PieceType.King: return BlackKing;
                }
            }
            return null;
        }
    }
}
