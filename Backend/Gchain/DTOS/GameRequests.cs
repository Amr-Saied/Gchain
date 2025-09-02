namespace Gchain.DTOS
{
    public class CreateGameRequest
    {
        public string Language { get; set; } = "English";
        public int MaxLives { get; set; } = 3;
        public int TurnTimeLimit { get; set; } = 30; // seconds
        public int RoundsToWin { get; set; } = 2;
    }

    public class JoinTeamRequest
    {
        public int GameSessionId { get; set; }
        public int TeamId { get; set; }
    }

    public class LeaveGameRequest
    {
        public int GameSessionId { get; set; }
    }
}
