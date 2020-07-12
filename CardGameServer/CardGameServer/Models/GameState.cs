using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardGameServer.Models
{
    public class GameState
    {
        public IReadOnlyList<Player> Players { get; }
        public IReadOnlyList<Card> CurTrick { get; }
        public int Dealer { get; }
        public int Leader { get; }
        public int CurPlayer { get; }
        public bool IsGameOver { get; }
        public bool IsStarted { get; }

        public GameState()
        {
            Players = null;
            CurTrick = null;
            Dealer = -1;
            Leader = -1;
            CurPlayer = -1;
            IsGameOver = false;
            IsStarted = false;
        }

        public GameState(IReadOnlyList<Player> players, IReadOnlyList<Card> curTrick, int dealer, int leader, int curPlayer, bool isGameOver, bool isStarted)
        {
            Players = players;
            CurTrick = curTrick;
            Dealer = dealer;
            Leader = leader;
            CurPlayer = curPlayer;
            IsGameOver = isGameOver;
            IsStarted = isStarted;
        }
    }
}
