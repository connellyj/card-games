using PinochleServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PinochleServer
{
    class GameManager
    {
        public static readonly int MIN_PLAYERS = 3;
        public static readonly int NUM_CARDS = 15;
        public static readonly int NUM_KITTY_CARDS = 3;
        public static readonly int MIN_BID = 20;
        public static readonly int ACES_AROUND = 10;
        public static readonly int KINGS_AROUND = 8;
        public static readonly int QUEENS_AROUND = 6;
        public static readonly int JACKS_AROUND = 4;
        public static readonly int DOUBLE_AROUND_MULTIPLIER = 10;
        public static readonly int MARRIAGE = 2;
        public static readonly int TRUMP_MARRIAGE = 4;
        public static readonly int PINOCHLE = 4;
        public static readonly int DOUBLE_PINOCHLE = 30;
        public static readonly int TRUMP_NINE = 1;
        public static readonly int TRUMP_RUN = 15;

        private readonly List<Player> Players;
        private List<int> PassedPlayers;
        private List<Card> CurTrick;
        private Card[] Kitty;
        private string Trump;
        private int Dealer;
        private int LastBidder;
        private int Leader;
        private int CurPlayer;
        private int NumMeld;
        private int CurBid;

        private static Dictionary<string, GameManager> GameMap;
        private static Dictionary<string, string> PlayerMap;

        public GameManager()
        {
            Players = new List<Player>();
            PassedPlayers = new List<int>();
            CurTrick = new List<Card>();
            Dealer = 0;
            NumMeld = 0;
            CurBid = MIN_BID;
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
                GameManager gm = new GameManager();
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
            // deal cards
            Card[] deck = Deck.Shuffle();
            int idx = 0;
            foreach (Player p in Players)
            {
                p.Cards = deck.Skip(idx * NUM_CARDS).Take(NUM_CARDS).ToList();
                idx++;
                Server.Instance().Send(new StartMessage(p.Cards.ToArray()), p.Uid);
            }
            Kitty = deck.Skip(idx * NUM_CARDS).Take(NUM_KITTY_CARDS).ToArray();

            // initiate first turn
            CurPlayer = (Dealer + 1) % Players.Count;
            StartBid(Players[CurPlayer]);
            SendMeldPoints();
        }

        private void StartBid(Player player)
        {
            LastBidder = Dealer;
            Server.Instance().Send(new BidMessage(player.Name, MIN_BID), player.Uid);
        }

        private void HandleBid(BidMessage message)
        {
            // broadcast bid to all players
            Broadcast(message);

            // parse bid
            CurBid = message.Bid;
            if (message.Bid != 0)
            {
                LastBidder = CurPlayer;
            }
            else
            {
                CurBid = message.CurBid;
                PassedPlayers.Add(CurPlayer);
            }

            // get next non-passed player
            NextPlayer();
            while (PassedPlayers.Contains(CurPlayer) && CurPlayer != LastBidder)
            {
                NextPlayer();
            }

            // initiate next turn
            if (CurPlayer == LastBidder)
            {
                Broadcast(new BidMessage(Players[CurPlayer].Name, CurBid, CurBid));
                PassedPlayers = new List<int>();
                StartKitty(CurPlayer);
            }
            else
            {
                Player player = Players[CurPlayer];
                Server.Instance().Send(new BidMessage(player.Name, CurBid), player.Uid);
            }
        }

        private void StartKitty(int player)
        {
            Broadcast(new KittyMessage(Kitty, Players[player].Name));
        }

        private void HandleKitty(KittyMessage message)
        {
            // update player model
            Player player = Players[CurPlayer];
            player.Cards.AddRange(Kitty);
            foreach (Card c in message.Kitty)
            {
                player.Cards.Remove(c);
                if (TrickDecider.IsPoint(c))
                {
                    player.SecretScore++;
                }
            }

            // initiate next turn
            StartTrump(CurPlayer);
        }

        private void StartTrump(int player)
        {
            Server.Instance().Send(new TrumpMessage(Players[player].Name), Players[CurPlayer].Uid);
        }

        private void HandleTrump(TrumpMessage message)
        {
            // broadcast to all players
            Broadcast(message);

            Trump = message.TrumpSuit;

            // initiate next turn
            StartMeld(message.TrumpSuit);
        }

        private void SendMeldPoints()
        {
            MeldPointsMessage meldPoints = new MeldPointsMessage(ACES_AROUND, KINGS_AROUND, QUEENS_AROUND, JACKS_AROUND, 
                DOUBLE_AROUND_MULTIPLIER, MARRIAGE, TRUMP_MARRIAGE, PINOCHLE, DOUBLE_PINOCHLE, TRUMP_NINE, TRUMP_RUN);
            Broadcast(meldPoints);
        }

        private void StartMeld(string trump)
        {
            foreach (Player p in Players)
            {
                MeldCounter counter = new MeldCounter(p.Cards, trump);
                p.MeldScore = counter.TotalMeld();
                MeldMessage message = new MeldMessage(p.Name, trump)
                {
                    AcesAround = counter.AcesAround(),
                    KingsAround = counter.KingsAround(),
                    QueensAround = counter.QueensAround(),
                    JacksAround = counter.JacksAround(),
                    TrumpNine = counter.Nines(),
                    Run = counter.Runs(),
                    ClubsMarriage = counter.ClubsMarriage(),
                    DiamondsMarriage = counter.DiamondsMarriage(),
                    SpadesMarriage = counter.SpadesMarriage(),
                    HeartsMarriage = counter.HeartsMarriage(),
                    Pinochle = counter.Pinochle()
                };
                Server.Instance().Send(message, p.Uid);
            }
        }

        private void HandleMeld(MeldMessage message)
        {
            // broadcast to all players
            Broadcast(message);
            BroadcastScore(message.PlayerName, true);

            NumMeld++;

            // initiate next turn
            if (NumMeld == Players.Count)
            {
                NumMeld = 0;
                StartTurn(LastBidder);
            }
        }

        private void StartTurn(int leader)
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
                int winningPlayer = (Leader + TrickDecider.WinningCard(CurTrick, Trump)) % Players.Count;
                Players[winningPlayer].TookATrick = true;
                foreach (Card c in CurTrick)
                {
                    if (TrickDecider.IsPoint(c))
                    {
                        Players[winningPlayer].SecretScore++;
                    }
                }

                if (player.Cards.Count == 0)
                {
                    // point for last trick
                    Players[winningPlayer].SecretScore++;

                    // start new hand
                    UpdateAndBroadcastAllScores();

                    if (Players.Any(p => p.Score >= 100))
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
                Card[] validCards = TrickDecider.ValidCards(player.Cards, CurTrick, Trump).ToArray();
                Server.Instance().Send(new TurnMessage(player.Name, validCards), player.Uid);
            }
        }

        private void NextPlayer()
        {
            CurPlayer = (CurPlayer + 1) % Players.Count;
        }

        private void Broadcast(Message message)
        {
            Server.Instance().Broadcast(message, Players.Select(p => p.Uid));
        }

        private void UpdateAndBroadcastAllScores()
        {
            Player biddingPlayer = Players[LastBidder];
            if (biddingPlayer.SecretScore + biddingPlayer.MeldScore < CurBid)
            {
                biddingPlayer.Score -= CurBid;
                biddingPlayer.SecretScore = 0;
                biddingPlayer.MeldScore = 0;
            }
            foreach (Player p in Players)
            {
                if (!p.TookATrick)
                {
                    p.MeldScore = 0;
                }
                p.Score += p.SecretScore;
                p.Score += p.MeldScore;
                p.SecretScore = 0;
                p.MeldScore = 0;
                p.TookATrick = false;
                BroadcastScore(p.Name, false);
            }
        }

        private void BroadcastScore(string playerName, bool includeMeld)
        {
            Player player = Players.Where(p => p.Name == playerName).Single();
            Broadcast(new ScoreMessage(playerName, includeMeld ? player.Score + player.MeldScore : player.Score));
        }
    }
}
