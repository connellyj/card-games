namespace CardGameServer
{
    public class HeartsGameManager : GameManager
    {
        public static int MinPlayers()
        {
            return 4;
        }

        protected override int GetMinPlayers()
        {
            return MinPlayers();
        }
    }
}
