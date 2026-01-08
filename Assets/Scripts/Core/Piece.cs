namespace ChessBattle.Core
{
    [System.Serializable]
    public struct Piece
    {
        public PieceType Type;
        public TeamColor Team;

        public Piece(PieceType type, TeamColor team)
        {
            Type = type;
            Team = team;
        }

        public static Piece None => new Piece(PieceType.None, TeamColor.None);
        
        public bool IsColor(TeamColor color)
        {
            return Team == color;
        }
    }
}
