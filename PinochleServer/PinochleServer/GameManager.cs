using CardGameServer.Models;
using System.Collections.Generic;
using System.Linq;

namespace CardGameServer
{
    public abstract class GameManager
    {
        public static readonly int MIN_PLAYERS = 3; // TODO move to protected method
        public static readonly int NUM_CARDS = 15;

        private readonly List<Player> Players;
        private List<Card> CurTrick;
        private int Dealer;
        private int Leader;
        private int CurPlayer;

        private static Dictionary<string, GameManager> GameMap;
        private static Dictionary<string, string> PlayerMap;

        public GameManager()
        {
            Players = new List<Player>();
            CurTrick = new List<Card>();
            Dealer = 0;
        }

        public static void Init()
        {
            GameMap = new Dictionary<string, GameManager>();
            PlayerMap = new Dictionary<string, string>();
        }

        public static List<Message> HandleNewConnection()
        {
            return new List<Message>() {
                new AvailableGamesMessage(GameMap.Keys.ToArray()),
                new GameInfoMessage(MIN_PLAYERS)
            };
        }

        public static void HandleJoin(JoinMessage message, string uid, string messageHash)
        {
            if (GameMap.ContainsKey(message.GameName))
            {
                GameMap[message.GameName].Join(message, uid, messageHash);
            }
            else
            {
                GameManager gm = new PinochleGameManager();
                GameMap.Add(message.GameName, gm);
                Server.Instance().Broadcast(new AvailableGamesMessage(GameMap.Keys.ToArray()));
                gm.Join(message, uid, messageHash);
            }
        }

        public static void HandleBid(string playerId, BidMessage message)
        {
            GameManager gm = GetGameManager(playerId);
            if (gm != null)
            {
                gm.HandleBid(message);
            }
        }

        public static void HandleKitty(string playerId, KittyMessage message)
        {
            GameManager gm = GetGameManager(playerId);
            if (gm != null)
            {
                gm.HandleKitty(message);
            }
        }

        public static void HandleTrump(string playerId, TrumpMessage message)
        {
            GameManager gm = GetGameManager(playerId);
            if (gm != null)
            {
                gm.HandleTrump(message);
            }
        }

        public static void HandleMeld(string playerId, MeldMessage message)
        {
            GameManager gm = GetGameManager(playerId);
            if (gm != null)
            {
                gm.HandleMeld(message);
            }
        }

        public static void HandleTurn(string playerId, TurnMessage message)
        {
            GameManager gm = GetGameManager(playerId);
            if (gm != null)
            {
                gm.HandleTurn(message);
            }
        }

        private static void HandleGameOver(GameManager gm)
        {
            string gameName = GameMap.Where(kvp => kvp.Value == gm).Select(kvp => kvp.Key).Single();
            GameMap.Remove(gameName);
            foreach (string p in PlayerMap.Where(kvp => kvp.Value == gameName).Select(kvp => kvp.Key).ToList())
            {
                PlayerMap.Remove(p);
            }
        }

        private static GameManager GetGameManager(string playerId)
        {
            if (PlayerMap.ContainsKey(playerId) && GameMap.ContainsKey(PlayerMap[playerId]))
            {
                return GameMap[PlayerMap[playerId]];
            }
            else
            {
                return null;
            }
        }

        private void Join(JoinMessage message, string uid, string messageHash)
        {
            if (!Players.Any(p => p.Name == message.UserName))
            {
                // handle full game
                if (Players.Count == MIN_PLAYERS)
                {
                    Server.Instance().Send(new Response(false, messageHash, string.Format("The game '{0}' is full", message.GameName)), uid);
                    return;
                }

                // send success response
                Server.Instance().Send(new Response(true, messageHash), uid);

                // inform new player of all existing players
                foreach (Player p in Players)
                {
                    Server.Instance().Send(new JoinMessage(p.Name, p.GameName, p.Order), uid);
                }

                // create new player model
                Player player = new Player(message.GameName, message.UserName, uid, Players.Count);
                Players.Add(player);
                PlayerMap.Add(uid, message.GameName);
                message.Order = player.Order;

                // broadcast to all players
                Broadcast(message);

                // there are enough players, so start the game
                if (Players.Count == MIN_PLAYERS)
                {
                    StartRound();
                }
            }
            else
            {
                Server.Instance().Send(new Response(false, messageHash, 
                    string.Format("The name '{0}' already exists in the game '{1}'", message.UserName, message.GameName)), uid);
            }
        }

        private void StartRound()
        {
            Deal();
            CurPlayer = (Dealer + 1) % Players.Count;
            DoStartRound(CurPlayer, Dealer);
        }

        private void Deal()
        {
            Card[] deck = Deck.Shuffle();
            int idx = 0;
            foreach (Player p in Players)
            {
                p.Cards = deck.Skip(idx * NUM_CARDS).Take(NUM_CARDS).ToList();
                idx++;
                Server.Instance().Send(new StartMessage(p.Cards.ToArray()), p.Uid);
            }
            if (deck.Length > idx * NUM_CARDS)
            {
                DealExtraCards(deck.Skip(idx * NUM_CARDS));
            }
        }

        protected void StartTurn(int leader)
        {
            Leader = leader;
            CurPlayer = leader;
            Player player = Players[CurPlayer];
            CurTrick = new List<Card>();
            Server.Instance().Send(new TurnMessage(player.Name, player.Cards.ToArray()), player.Uid);
        }

        private void HandleTurn(TurnMessage message)
        {
            // broadcast to all players
            Broadcast(message);

            CurTrick.Add(message.Card);
            Players[CurPlayer].Cards.Remove(message.Card);

            NextPlayer();
            Player player = Players[CurPlayer];
            if (CurPlayer == Leader)
            {
                // broadcast trick end to all players
                Broadcast(new TrickMessage(player.Name));

                // update points
                int winningPlayer = (Leader + DoDecideTrick(CurTrick)) % Players.Count;
                Players[winningPlayer].TookATrick = true;
                DoTrick(CurTrick, Players[winningPlayer]);

                if (player.Cards.Count == 0)
                {
                    DoLastTrick(winningPlayer);

                    // start new hand
                    UpdateAndBroadcastAllScores();

                    if (Players.Any(p => p.Score >= GetWinningPointTotal()))
                    {
                        Broadcast(new GameOverMessage(Players.OrderBy(p => p.Score).Last().Name));
                        HandleGameOver(this);
                    }
                    else
                    {
                        Dealer++;
                        StartRound();
                    }
                }
                else
                {
                    // initiate next trick
                    StartTurn(winningPlayer);
                }
            }
            else
            {
                // initiate next turn
                Server.Instance().Send(new TurnMessage(player.Name, GetValidCards(player.Cards, CurTrick)), player.Uid);
            }
        }

        protected void NextPlayer()
        {
            CurPlayer = (CurPlayer + 1) % Players.Count;
        }

        protected void Broadcast(Message message)
        {
            Server.Instance().Broadcast(message, Players.Select(p => p.Uid));
        }

        private void UpdateAndBroadcastAllScores()
        {
            DoUpdateScores();
            foreach (Player p in Players)
            {
                p.TookATrick = false;
                BroadcastScore(p.Name);
            }
        }
            
        protected void BroadcastScore(string playerName, bool includeMeld=false)
        {
            Player player = Players.Where(p => p.Name == playerName).Single();
            Broadcast(new ScoreMessage(playerName, includeMeld ? player.Score + player.MeldScore : player.Score));
        }

        protected int GetCurrentPlayerIndex()
        {
            return CurPlayer;
        }

        protected Player GetCurrentPlayer()
        {
            return Players[CurPlayer];
        }

        protected Player GetPlayer(int index)
        {
            return Players[index];
        }

        protected List<Player> GetPlayers()
        {
            return Players;
        }

        protected int GetNumPlayers()
        {
            return Players.Count;
        }

        protected virtual void DoStartRound(int curPlayer, int dealer)
        {
            StartTurn(curPlayer);
        }

        protected virtual int DoDecideTrick(List<Card> trick)
        {
            string suit = trick[0].Suit;
            return trick.IndexOf(trick.Where(c => c.Suit == suit).OrderBy(c => c).Last());
        }

        protected virtual void DoTrick(List<Card> trick, Player winningPlayer)
        {
            // Do nothing by default
        }

        protected virtual void DoLastTrick(int winningPlayer)
        {
            // Do nothing by default
        }

        protected virtual void DoUpdateScores()
        {

        }

        protected virtual Card[] GetValidCards(List<Card> hand, List<Card> trick)
        {
            return hand.ToArray();
        }

        protected virtual int GetWinningPointTotal()
        {
            return 100;
        }

        protected virtual void DealExtraCards(IEnumerable<Card> cards)
        {
            // Do nothing by default
        }

        protected virtual void HandleKitty(KittyMessage kittyMessage)
        {
            // Do nothing by default
        }

        protected virtual void HandleBid(BidMessage bidMessage)
        {
            // Do nothing by default
        }

        protected virtual void HandleTrump(TrumpMessage trumpMessage)
        {
            // Do nothing by default
        }

        protected virtual void HandleMeld(MeldMessage meldMessage)
        {
            // Do nothing by default
        }
    }
}
