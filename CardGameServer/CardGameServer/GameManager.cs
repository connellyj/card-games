﻿using CardGameServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CardGameServer
{
    public abstract class GameManager
    {
        private readonly List<Player> Players;
        private List<Card> CurTrick;
        private int Dealer;
        private int Leader;
        private int CurPlayer;

        private static Dictionary<string, Dictionary<string, GameManager>> GameNameMap;
        private static Dictionary<string, string> PlayerGameNameMap;
        private static Dictionary<string, string> PlayerGameTypeMap;

        private static readonly List<string> Suits = new List<string>() { "C", "D", "S", "H" };
        private static readonly List<string> Ranks = new List<string>() { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };

        private static readonly Dictionary<string, Type> GameManagerMap = new Dictionary<string, Type>()
        {
            { "Hearts", typeof(HeartsGameManager) },
            { "Pinochle", typeof(PinochleGameManager) }
        };

        private static readonly Dictionary<string, int> MinPlayerMap = new Dictionary<string, int>()
        {
            { "Hearts", HeartsGameManager.MinPlayers() },
            { "Pinochle", PinochleGameManager.MinPlayers() }
        };

        public GameManager()
        {
            Players = new List<Player>();
            CurTrick = new List<Card>();
            Dealer = 0;
        }

        public static void Init()
        {
            GameNameMap = new Dictionary<string, Dictionary<string, GameManager>>
            {
                { "Hearts", new Dictionary<string, GameManager>() },
                { "Pinochle", new Dictionary<string, GameManager>() }
            };
            PlayerGameNameMap = new Dictionary<string, string>();
            PlayerGameTypeMap = new Dictionary<string, string>();
        }

        public static GameTypeMessage GetGameTypes()
        {
            return new GameTypeMessage(GameManagerMap.Select(kvp => kvp.Key).ToArray());
        }

        public static void HandleGameTypes(string uid, GameTypeMessage gameTypeMessage)
        {
            PlayerGameTypeMap.Add(uid, gameTypeMessage.ChosenGame);
            Server.Instance().Send(new AvailableGamesMessage(GameNameMap[PlayerGameTypeMap[uid]].Keys.ToArray()), uid);
            Server.Instance().Send(new GameInfoMessage(MinPlayerMap[PlayerGameTypeMap[uid]]), uid);
        }

        public static void HandleJoin(string uid, JoinMessage message)
        {
            if (PlayerGameTypeMap.ContainsKey(uid))
            {
                if (GameNameMap[PlayerGameTypeMap[uid]].ContainsKey(message.GameName))
                {
                    GameNameMap[PlayerGameTypeMap[uid]][message.GameName].Join(message, uid);
                }
                else
                {
                    GameManager gm = (GameManager)Activator.CreateInstance(GameManagerMap[PlayerGameTypeMap[uid]]);
                    GameNameMap[PlayerGameTypeMap[uid]].Add(message.GameName, gm);
                    Server.Instance().Broadcast(new AvailableGamesMessage(GameNameMap.Keys.ToArray()));
                    gm.Join(message, uid);
                }
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

        public static void HandlePass(string playerId, PassMessage message)
        {
            GameManager gm = GetGameManager(playerId);
            if (gm != null)
            {
                gm.HandlePass(message);
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
            string gameType = GameManagerMap.Where(kvp => kvp.Value == gm.GetType()).Select(kvp => kvp.Key).Single();
            string gameName = GameNameMap[gameType].Where(kvp => kvp.Value == gm).Select(kvp => kvp.Key).Single();
            GameNameMap.Remove(gameName);
            foreach (string p in PlayerGameNameMap.Where(kvp => kvp.Value == gameName).Select(kvp => kvp.Key).ToList())
            {
                PlayerGameNameMap.Remove(p);
                PlayerGameTypeMap.Remove(p);
            }
        }

        private static GameManager GetGameManager(string playerId)
        {
            if (PlayerGameNameMap.ContainsKey(playerId) && PlayerGameNameMap.ContainsKey(playerId) && GameNameMap[PlayerGameTypeMap[playerId]].ContainsKey(PlayerGameNameMap[playerId]))
            {
                return GameNameMap[PlayerGameTypeMap[playerId]][PlayerGameNameMap[playerId]];
            }
            else
            {
                return null;
            }
        }

        private void Join(JoinMessage message, string uid)
        {
            if (!Players.Any(p => p.Name == message.UserName))
            {
                // handle full game
                if (Players.Count == GetMinPlayers())
                {
                    Server.Instance().Send(new JoinResponse(false, errorMessage: string.Format("The game '{0}' is full", message.GameName)), uid);
                    return;
                }

                // send success response
                Server.Instance().Send(new JoinResponse(true, message.UserName), uid);

                // inform new player of all existing players
                foreach (Player p in Players)
                {
                    Server.Instance().Send(new JoinMessage(p.Name, p.GameName, p.Order), uid);
                }

                // create new player model
                Player player = new Player(message.GameName, message.UserName, uid, Players.Count);
                Players.Add(player);
                PlayerGameNameMap.Add(uid, message.GameName);
                message.Order = player.Order;

                // broadcast to all players
                Broadcast(message);

                // there are enough players, so start the game
                if (Players.Count == GetMinPlayers())
                {
                    StartRound();
                }
            }
            else
            {
                Server.Instance().Send(new JoinResponse(false, errorMessage: string.Format("The name '{0}' already exists in the game '{1}'", message.UserName, message.GameName)), uid);
            }
        }

        private void StartRound()
        {
            Deal();
            CurPlayer = DoSetStartingPlayer();
            DoStartRound(Dealer);
        }

        private void Deal()
        {
            Card[] deck = DealCards();
            int idx = 0;
            foreach (Player p in Players)
            {
                p.Cards = deck.Skip(idx * GetNumCardsInHand()).Take(GetNumCardsInHand()).ToList();
                idx++;
                Server.Instance().Send(new StartMessage(p.Cards.ToArray()), p.Uid);
            }
            if (deck.Length > idx * GetNumCardsInHand())
            {
                DealExtraCards(deck.Skip(idx * GetNumCardsInHand()));
            }
        }

        protected void StartTurn(int leader, bool isFirstRound=false)
        {
            Leader = leader;
            CurPlayer = leader;
            Player player = Players[CurPlayer];
            CurTrick = new List<Card>();
            Card[] validCards = isFirstRound ? GetFirstRoundValidCards(player.Cards) : player.Cards.ToArray();
            Server.Instance().Send(new TurnMessage(player.Name, validCards), player.Uid);
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
                        Broadcast(new GameOverMessage(GetGameWinningPlayer()));
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
                bool isFirst = !Players.Any(p => p.TookATrick);
                Server.Instance().Send(new TurnMessage(player.Name, GetValidCards(player.Cards, CurTrick, isFirst)), player.Uid);
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

        protected virtual int DoSetStartingPlayer()
        {
            return (Dealer + 1) % Players.Count;
        }

        protected virtual void DoStartRound(int dealer)
        {
            StartTurn(CurPlayer, true);
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
            foreach (Player p in GetPlayers())
            {
                p.Score += p.SecretScore;
                p.SecretScore = 0;
            }
        }

        protected virtual Card[] GetFirstRoundValidCards(List<Card> hand)
        {
            return hand.ToArray();
        }

        protected virtual Card[] GetValidCards(List<Card> hand, List<Card> trick, bool isFirstTrick)
        {
            string suit = trick[0].Suit;
            Card[] followSuit = hand.Where(c => c.Suit == suit).ToArray();
            if (followSuit.Length > 0)
            {
                return followSuit;
            }
            else
            {
                return hand.ToArray();
            }
        }

        protected abstract string GetGameWinningPlayer();

        protected virtual int GetWinningPointTotal()
        {
            return 100;
        }

        protected virtual int GetMinPlayers()
        {
            return 4;
        }

        protected virtual int GetNumCardsInHand()
        {
            return 13;
        }

        protected virtual Card[] DealCards()
        {
            return Deck.Shuffle(Suits, Ranks);
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

        protected virtual void HandlePass(PassMessage passMessage)
        {
            // Do nothing by default
        }
    }
}