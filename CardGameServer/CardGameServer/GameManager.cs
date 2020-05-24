using CardGameServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CardGameServer
{
    public abstract class GameManager
    {
        // ********** Static member variables ********** //

        // Static map of game type to a map of game name to GameManager
        private static Dictionary<string, Dictionary<string, GameManager>> GameNameMap;

        // Static map of player uid to game name
        private static Dictionary<string, string> PlayerGameNameMap;

        // Static map of player uid to game type
        private static Dictionary<string, string> PlayerGameTypeMap;

        // The number of points required to end the game
        private static readonly int WINNING_POINTS = 100;

        // The number of players required to play
        private static readonly int MIN_PLAYERS = 4;

        // The number of cards in each players hand
        private static readonly int NUM_CARDS_IN_HAND = 13;

        // Static list of card suits
        private static readonly List<string> Suits = new List<string>() { "C", "D", "S", "H" };

        // Static list of card ranks
        private static readonly List<string> Ranks = new List<string>() { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };

        // Static map of game type to GameManager type
        private static readonly Dictionary<string, Type> GameManagerMap = new Dictionary<string, Type>()
        {
            { "Hearts", typeof(HeartsGameManager) },
            { "Pinochle", typeof(PinochleGameManager) }
        };

        // Static map of game type to game info
        private static readonly Dictionary<string, GameInfoMessage> GameInfoMap = new Dictionary<string, GameInfoMessage>()
        {
            { "Hearts", new GameInfoMessage(HeartsGameManager.MinPlayers()) },
            { "Pinochle", new GameInfoMessage(PinochleGameManager.MinPlayers()) }
        };


        // ********** Member variables ********** //

        // List of all players in the game
        private readonly List<Player> Players;

        // List of cards in the current trick
        private List<Card> CurTrick;

        // Index of the dealer
        private int Dealer;

        // Index of the leader of the current trick
        private int Leader;

        // Index of the current player
        private int CurPlayer;

        // Whether the game is over
        private bool IsGameOver;


        // ********** Static member functions ********** //

        /// <summary>
        /// Must be called before using the GameManager
        /// </summary>
        public static void StaticInitialize()
        {
            GameNameMap = new Dictionary<string, Dictionary<string, GameManager>>
            {
                { "Hearts", new Dictionary<string, GameManager>() },
                { "Pinochle", new Dictionary<string, GameManager>() }
            };
            PlayerGameNameMap = new Dictionary<string, string>();
            PlayerGameTypeMap = new Dictionary<string, string>();
        }

        /// <summary>
        /// Returns the mininum number of players required to start the game.
        /// </summary>
        /// <returns> Minimum number of players </returns>
        public static int MinPlayers()
        {
            lock (GameManagerMap)
            {
                return MIN_PLAYERS;
            }
        }

        /// <summary>
        /// Get all the possible game types
        /// </summary>
        /// <returns> All game types </returns>
        public static GameTypeMessage GetGameTypes()
        {
            lock (GameManagerMap)
            {
                return new GameTypeMessage(GameManagerMap.Select(kvp => kvp.Key).ToArray());
            }
        }

        /// <summary>
        /// Handle a GameTypesMessage.
        /// </summary>
        /// <param name="uid"> The uid of the player </param>
        /// <param name="gameTypeMessage"></param>
        public static void HandleGameTypes(string uid, GameTypeMessage gameTypeMessage)
        {
            lock (GameManagerMap)
            {
                // Add to game type map
                PlayerGameTypeMap.Add(uid, gameTypeMessage.ChosenGame);

                // Send AvailableGames and GameInfo messages
                Server.Instance().Send(GetAvailableGames(PlayerGameTypeMap[uid]), uid);
                Server.Instance().Send(GameInfoMap[PlayerGameTypeMap[uid]], uid);
            }
        }

        /// <summary>
        /// Handle a JoinMessage.
        /// </summary>
        /// <param name="uid"> The uid of the player </param>
        /// <param name="joinMessage"></param>
        public static void HandleJoin(string uid, JoinMessage joinMessage)
        {
            lock (GameManagerMap)
            {
                if (PlayerGameTypeMap.ContainsKey(uid))
                {
                    if (GameNameMap[PlayerGameTypeMap[uid]].ContainsKey(joinMessage.GameName))
                    {
                        // The game exists, so join it
                        GameNameMap[PlayerGameTypeMap[uid]][joinMessage.GameName].Join(joinMessage, uid);
                    }
                    else
                    {
                        // The game doesn't exist, so make a new one and join it
                        GameManager gm = (GameManager)Activator.CreateInstance(GameManagerMap[PlayerGameTypeMap[uid]]);
                        GameNameMap[PlayerGameTypeMap[uid]].Add(joinMessage.GameName, gm);
                        gm.Join(joinMessage, uid);

                        // Broadcast available games
                        BroadcastAvailableGames(PlayerGameTypeMap[uid]);
                    }
                }
                else
                {
                    Server.Instance().Send(new ErrorResponse("A GameTypeMessage must be sent before a JoinMessage."), uid);
                }
            }
        }

        /// <summary>
        /// Handle a player disconnect
        /// </summary>
        /// <param name="uid"> The uid of the player </param>
        public static void HandlePlayerDisconnect(string uid)
        {
            lock (GameManagerMap)
            {
                GameManager gm = GetGameManager(uid);
                if (gm != null)
                {
                    gm.HandleDisconnect(uid);
                }
            }
        }

        /// <summary>
        /// Handle a RestartMessage.
        /// </summary>
        /// <param name="playerUid"> The uid of the player </param>
        /// <param name="restartMessage"></param>
        public static void HandleRestart(string playerUid, RestartMessage restartMessage)
        {
            lock (GameManagerMap)
            {
                GameManager gm = GetGameManager(playerUid);
                if (gm != null)
                {
                    gm.HandleRestart(restartMessage);
                }
            }
        }

        /// <summary>
        /// Handle a BidMessage.
        /// </summary>
        /// <param name="playerId"> The uid of the player </param>
        /// <param name="bidMessage"></param>
        public static void HandleBid(string playerId, BidMessage bidMessage)
        {
            lock (GameManagerMap)
            {
                GameManager gm = GetGameManager(playerId);
                if (gm != null)
                {
                    gm.HandleBid(bidMessage);
                }
            }
        }

        /// <summary>
        /// Handle a KittyMessage.
        /// </summary>
        /// <param name="playerId"> The uid of the player </param>
        /// <param name="kittyMessage"></param>
        public static void HandleKitty(string playerId, KittyMessage kittyMessage)
        {
            lock (GameManagerMap)
            {
                GameManager gm = GetGameManager(playerId);
                if (gm != null)
                {
                    gm.HandleKitty(kittyMessage);
                }
            }
        }

        /// <summary>
        /// Handle a TrumpMessage
        /// </summary>
        /// <param name="playerId"> The uid of the player </param>
        /// <param name="trumpMessage"></param>
        public static void HandleTrump(string playerId, TrumpMessage trumpMessage)
        {
            lock (GameManagerMap)
            {
                GameManager gm = GetGameManager(playerId);
                if (gm != null)
                {
                    gm.HandleTrump(trumpMessage);
                }
            }
        }

        /// <summary>
        /// Handle a MeldMessage
        /// </summary>
        /// <param name="playerId"> The uid of the player </param>
        /// <param name="meldMessage"></param>
        public static void HandleMeld(string playerId, MeldMessage meldMessage)
        {
            lock (GameManagerMap)
            {
                GameManager gm = GetGameManager(playerId);
                if (gm != null)
                {
                    gm.HandleMeld(meldMessage);
                }
            }
        }

        /// <summary>
        /// Handle a PassMessage.
        /// </summary>
        /// <param name="playerId"> The uid of the player </param>
        /// <param name="passMessage"></param>
        public static void HandlePass(string playerId, PassMessage passMessage)
        {
            lock (GameManagerMap)
            {
                GameManager gm = GetGameManager(playerId);
                if (gm != null)
                {
                    gm.HandlePass(passMessage);
                }
            }
        }

        /// <summary>
        /// Handle a TurnMessage
        /// </summary>
        /// <param name="playerId"> The uid of the player </param>
        /// <param name="turnMessage"></param>
        public static void HandleTurn(string playerId, TurnMessage turnMessage)
        {
            lock (GameManagerMap)
            {
                GameManager gm = GetGameManager(playerId);
                if (gm != null)
                {
                    gm.HandleTurn(turnMessage);
                }
            }
        }

        private static void BroadcastAvailableGames(string gameType)
        {
            Server.Instance().Broadcast(GetAvailableGames(gameType),
                PlayerGameTypeMap.Where(kvp => kvp.Value == gameType).Select(kvp => kvp.Key));
        }

        private static AvailableGamesMessage GetAvailableGames(string gameType)
        {
            // Get games of given type that aren't full or over
            IEnumerable<string> games = GameNameMap[gameType]
                .Where(kvp => kvp.Value.GetNumPlayers() < kvp.Value.GetMinPlayers())
                .Where(kvp => !kvp.Value.IsGameOver)
                .Select(kvp => kvp.Key);
            return new AvailableGamesMessage(games.ToArray());
        }

        /// <summary>
        /// Remove the given GameManager
        /// </summary>
        /// <param name="gm"> The GameManager to remove </param>
        private static void RemoveGameManager(GameManager gm)
        {
            // Remove game manager
            string gameType = GameManagerMap.Where(kvp => kvp.Value == gm.GetType()).Select(kvp => kvp.Key).Single();
            string gameName = GameNameMap[gameType].Where(kvp => kvp.Value == gm).Select(kvp => kvp.Key).Single();
            GameNameMap[gameType].Remove(gameName);

            // Remove all players
            foreach (string p in PlayerGameNameMap.Where(kvp => kvp.Value == gameName).Select(kvp => kvp.Key).ToList())
            {
                RemovePlayer(p);
            }

            // Update available games
            BroadcastAvailableGames(gameType);
        }

        /// <summary>
        /// Remove the given player
        /// </summary>
        /// <param name="uid"> The uid of the player to remove </param>
        private static void RemovePlayer(string uid)
        {
            PlayerGameNameMap.Remove(uid);
            PlayerGameTypeMap.Remove(uid);
        }

        /// <summary>
        /// Get the GameManager the given player is a part of.
        /// </summary>
        /// <param name="playerId"> The uid of the player </param>
        /// <returns> The GameManager or null if the player isn't mapped to a GameManager </returns>
        private static GameManager GetGameManager(string playerId)
        {
            if (PlayerGameNameMap.ContainsKey(playerId) && PlayerGameNameMap.ContainsKey(playerId) && GameNameMap[PlayerGameTypeMap[playerId]].ContainsKey(PlayerGameNameMap[playerId]))
            {
                return GameNameMap[PlayerGameTypeMap[playerId]][PlayerGameNameMap[playerId]];
            }
            else
            {
                Server.Instance().Send(new ErrorResponse("A JoinMessage must be sent before any other message."), playerId);
                return null;
            }
        }


        // ********** Member functions ********** //

        /// <summary>
        /// Constructor
        /// </summary>
        public GameManager()
        {
            Players = new List<Player>();
            Reset();
        }

        /// <summary>
        /// Get the index of the current player.
        /// </summary>
        /// <returns> Index of the current player </returns>
        protected int GetCurrentPlayerIndex()
        {
            return CurPlayer;
        }

        /// <summary>
        /// Get the current player.
        /// </summary>
        /// <returns> The current player </returns>
        protected Player GetCurrentPlayer()
        {
            return Players[CurPlayer];
        }

        /// <summary>
        /// Get player at the given index.
        /// </summary>
        /// <returns> Player at the index </returns>
        protected Player GetPlayer(int index)
        {
            return Players[index];
        }

        /// <summary>
        /// Get all players.
        /// </summary>
        /// <returns> All players </returns>
        protected List<Player> GetPlayers()
        {
            return Players;
        }

        /// <summary>
        /// Get the number of players.
        /// </summary>
        /// <returns> Number of players in the game </returns>
        protected int GetNumPlayers()
        {
            return Players.Count;
        }

        /// <summary>
        /// Update CurPlayer.
        /// </summary>
        protected void NextPlayer()
        {
            CurPlayer = (CurPlayer + 1) % Players.Count;
        }

        /// <summary>
        /// Update Dealer.
        /// </summary>
        protected void NextDealer()
        {
            Dealer = (Dealer + 1) % Players.Count;
        }

        /// <summary>
        /// Broadcast the given message to all players.
        /// </summary>
        /// <param name="message"></param>
        protected void Broadcast(Message message)
        {
            Server.Instance().Broadcast(message, Players.Select(p => p.Uid));
        }

        /// <summary>
        /// Broadcast player scores to all players.
        /// </summary>
        /// <param name="playerName"> Name of the player whose score to broadcast </param>
        protected void BroadcastScore(string playerName)
        {
            Player player = Players.Where(p => p.Name == playerName).Single();
            Broadcast(new ScoreMessage(playerName, player.Score, player.Score - player.OldScore, player.MissedBidBy));
        }

        /// <summary>
        /// Starts a new trick. The provided leader is set as the current player. If isFirstRound, GetFirstTrickValidCards()
        /// is called to get the valid cards. Otherwise, the entire players hand is used.
        /// </summary>
        /// <param name="leader"> Index of the player who should lead the trick </param>
        /// <param name="isFirstTrick"> Whether this trick is the first of the round </param>
        protected void StartTrick(int leader, bool isFirstTrick=false)
        {
            Leader = leader;
            CurPlayer = leader;
            Player player = Players[CurPlayer];
            CurTrick = new List<Card>();
            Card[] validCards = isFirstTrick ? DoGetFirstTrickValidCards(player.Cards) : player.Cards.ToArray();
            Broadcast(new TurnMessage(player.Name, validCards, isFirstCard: true));
        }

        /// <summary>
        /// By default, shuffles a standard 52 card deck.
        /// Called when a round is started.
        /// Subclasses should override to enable different behaviors.
        /// </summary>
        /// <returns> The list of all cards, shuffled </returns>
        protected virtual Card[] DoShuffle()
        {
            return Deck.Shuffle(Suits, Ranks);
        }

        /// <summary>
        /// By default, do nothing.
        /// Called during dealing if there are leftover cards.
        /// Subclasses should override to enable different behaviors.
        /// </summary>
        /// <param name="cards"> The remaining cards that weren't dealt to players </param>
        protected virtual void DoDealExtraCards(IEnumerable<Card> cards)
        {
        }

        /// <summary>
        /// By default, when the round is started just start the first trick with the current player.
        /// Called when enough players have joined the game, or when the game is restarted.
        /// Subclasses should override to enable different behaviors.
        /// </summary>
        /// <param name="dealer"> The current dealer </param>
        protected virtual void DoStartRound(int dealer)
        {
            StartTrick(CurPlayer, true);
        }

        /// <summary>
        /// By default, return the entire hand
        /// Called when the first trick of a round is started
        /// Subclasses should override to enable different behaviors.
        /// </summary>
        /// <param name="hand"> The players cards </param>
        /// <returns> The valid cards that can be played </returns>
        protected virtual Card[] DoGetFirstTrickValidCards(List<Card> hand)
        {
            return hand.ToArray();
        }

        /// <summary>
        /// By default, return all cards with the same suit as the first card in the trick,
        /// or the entire hand if there are none.
        /// Called when a new player's turn starts if they aren't the first player in the trick.
        /// Subclasses should override to enable different behaviors.
        /// </summary>
        /// <param name="hand"> The players hand </param>
        /// <param name="trick"> The current trick </param>
        /// <param name="isFirstTrick"> Whether this trick is the first trick </param>
        /// <returns> The valid cards that can be played </returns>
        protected virtual Card[] DoGetValidCards(List<Card> hand, List<Card> trick, bool isFirstTrick)
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

        /// <summary>
        /// By default, the winning card is the highest card in the same suit as the first card in the trick.
        /// Called when a trick has as many cards as there are players in the game.
        /// Subclasses should override to enable different behaviors.
        /// </summary>
        /// <param name="trick"> The cards in the trick </param>
        /// <returns> The index of the winning card </returns>
        protected virtual int DoDecideTrick(List<Card> trick)
        {
            string suit = trick[0].Suit;
            return trick.IndexOf(trick.Where(c => c.Suit == suit).OrderBy(c => c.SortKey).Last());
        }

        /// <summary>
        /// By deafult, do nothing.
        /// Called when a trick has as many cards as there are players in the game.
        /// Subclasses should override to enable different behaviors.
        /// </summary>
        /// <param name="trick"> The completed trick </param>
        /// <param name="winningPlayer"> The index of the player that won the trick </param>
        protected virtual void DoScoreTrick(List<Card> trick, Player winningPlayer)
        {
        }

        /// <summary>
        /// By default, do nothing.
        /// Called when the last trick of the game has been completed.
        /// Subclasses should override to enable different behaviors.
        /// </summary>
        /// <param name="winningPlayer"> The index of the player who won the trick </param>
        protected virtual void DoLastTrick(int winningPlayer)
        {
        }

        /// <summary>
        /// By default, add the players secret score to overall score.
        /// Called when the scores are updated after the last trick in a round.
        /// Subclasses should override to enable different behaviors.
        /// </summary>
        protected virtual void DoUpdateScores()
        {
            foreach (Player p in GetPlayers())
            {
                p.Score += p.SecretScore;
            }
        }

        /// <summary>
        /// By default, return the player with the most points.
        /// Called when the winning point total is reached after a trick ends.
        /// Subclasses should override to enable different behaviors.
        /// </summary>
        /// <returns> The name of the winning player </returns>
        protected virtual string DoGetGameWinningPlayer()
        {
            return Players.OrderBy(p => p.Score).Last().Name;
        }

        /// <summary>
        /// By default, returns 100.
        /// Used to determine when the game should end.
        /// Subclasses should override to enable different behaviors.
        /// </summary>
        /// <returns> The points required to end the game </returns>
        protected virtual int GetWinningPointTotal()
        {
            return WINNING_POINTS;
        }

        /// <summary>
        /// By default, returns 4.
        /// Used to determine when the game should start.
        /// Subclasses should override to enable different behaviors.
        /// </summary>
        /// <returns> Number of players required to start the game </returns>
        protected virtual int GetMinPlayers()
        {
            return MinPlayers();
        }

        /// <summary>
        /// By default, returns 13.
        /// Used to deal cards.
        /// Subclasses should override to enable different behaviors.
        /// </summary>
        /// <returns> The number of cards in a players hand </returns>
        protected virtual int GetNumCardsInHand()
        {
            return NUM_CARDS_IN_HAND;
        }

        /// <summary>
        /// Handle a KittyMessage. By default, do nothing.
        /// Subclasses should override to enable different behaviors.
        /// </summary>
        /// <param name="kittyMessage"></param>
        protected virtual void HandleKitty(KittyMessage kittyMessage)
        {
            SendNotSupportedMessage(kittyMessage.ChoosingPlayer, "KittyMessage");
        }

        /// <summary>
        /// Handle a BidMessage. By default, do nothing.
        /// Subclasses should override to enable different behaviors.
        /// </summary>
        /// <param name="bidMessage"></param>
        protected virtual void HandleBid(BidMessage bidMessage)
        {
            SendNotSupportedMessage(bidMessage.PlayerName, "BidMessage");
        }

        /// <summary>
        /// Handle a TrumpMessage. By default, do nothing.
        /// Subclasses should override to enable different behaviors.
        /// </summary>
        /// <param name="trumpMessage"></param>
        protected virtual void HandleTrump(TrumpMessage trumpMessage)
        {
            SendNotSupportedMessage(trumpMessage.ChoosingPlayer, "TrumpMessage");
        }

        /// <summary>
        /// Handle a MeldMessage. By default, do nothing.
        /// Subclasses should override to enable different behaviors.
        /// </summary>
        /// <param name="meldMessage"></param>
        protected virtual void HandleMeld(MeldMessage meldMessage)
        {
            SendNotSupportedMessage(meldMessage.PlayerName, "MeldMessage");
        }

        /// <summary>
        /// Handle a PassMessage. By default, do nothing.
        /// Subclasses should override to enable different behaviors.
        /// </summary>
        /// <param name="passMessage"></param>
        protected virtual void HandlePass(PassMessage passMessage)
        {
            SendNotSupportedMessage(passMessage.PassingPlayer, "PassMessage");
        }

        /// <summary>
        /// Handle a DisconnectMessage.
        /// </summary>
        /// <param name="playerId"> The uid of the disconnected player </param>
        private void HandleDisconnect(string playerId)
        {
            // Broadcast message to all players
            Player player = Players.Where(p => p.Uid == playerId).Single();
            Broadcast(new DisconnectMessage(player.Name, !IsGameOver));

            // Remove the disconnected player
            RemovePlayer(player);
        }

        /// <summary>
        /// Handle a RestartMessage.
        /// </summary>
        /// <param name="restartMessage"></param>
        private void HandleRestart(RestartMessage restartMessage)
        {
            if (!restartMessage.NewGame)
            {
                // Restart the same game
                Reset();
                StartRound();
            }
            else
            {
                // Start a new game
                Player player = Players.Where(p => p.Name == restartMessage.PlayerName).Single();
                Server.Instance().Send(restartMessage, player.Uid);

                // Remove player from old game
                RemovePlayer(player);

                // Inform other players the player left the old game
                Broadcast(new DisconnectMessage(restartMessage.PlayerName, false));
            }
        }

        /// <summary>
        /// Join a player to the game
        /// </summary>
        /// <param name="message"> The received join message </param>
        /// <param name="uid"> The uid of the player joining </param>
        private void Join(JoinMessage message, string uid)
        {
            if (!Players.Any(p => p.Name == message.UserName))
            {
                // Handle full game
                if (Players.Count == GetMinPlayers())
                {
                    Server.Instance().Send(new JoinResponse(false, errorMessage: string.Format("The game '{0}' is full", message.GameName)), uid);
                    return;
                }

                // Send success response
                Server.Instance().Send(new JoinResponse(true, message.UserName), uid);

                // Inform new player of all existing players
                foreach (Player p in Players)
                {
                    Server.Instance().Send(new JoinMessage(p.Name, p.GameName, p.Order), uid);
                }

                // Create new player model
                Player player = new Player(message.GameName, message.UserName, uid, Players.Count);
                Players.Add(player);
                PlayerGameNameMap.Add(uid, message.GameName);
                message.Order = player.Order;

                // Broadcast new player to all players
                Broadcast(message);

                // There are enough players, so start the game
                if (Players.Count == GetMinPlayers())
                {
                    StartRound();
                }
            }
            else
            {
                // Handle user name already exists in game
                Server.Instance().Send(new JoinResponse(false, errorMessage: string.Format("The name '{0}' already exists in the game '{1}'", message.UserName, message.GameName)), uid);
            }
        }

        /// <summary>
        /// Start a round
        /// </summary>
        private void StartRound()
        {
            Deal();
            CurPlayer = (Dealer + 1) % Players.Count;
            DoStartRound(Dealer);
        }

        /// <summary>
        /// Deal cards to players
        /// </summary>
        private void Deal()
        {
            Card[] deck = DoShuffle();
            int idx = 0;
            foreach (Player p in Players)
            {
                p.Cards = deck.Skip(idx * GetNumCardsInHand()).Take(GetNumCardsInHand()).ToList();
                idx++;
                Server.Instance().Send(new StartMessage(p.Cards.ToArray()), p.Uid);
            }
            if (deck.Length > idx * GetNumCardsInHand())
            {
                DoDealExtraCards(deck.Skip(idx * GetNumCardsInHand()));
            }
        }

        /// <summary>
        /// Handle a TurnMessage.
        /// </summary>
        /// <param name="message"> The received turn message </param>
        private void HandleTurn(TurnMessage message)
        {
            // Broadcast turn start to all players
            Broadcast(message);

            // Add card to current trick, and remove from player's hand
            CurTrick.Add(message.Card);
            Players[CurPlayer].Cards.Remove(message.Card);

            // Update CurPlayer
            NextPlayer();
            Player player = Players[CurPlayer];

            // Handle all players have played
            if (CurPlayer == Leader)
            {
                // Broadcast trick end to all players
                Broadcast(new TrickMessage(player.Name));

                // Handle trick end
                int winningPlayer = (Leader + DoDecideTrick(CurTrick)) % Players.Count;
                Players[winningPlayer].TookATrick = true;
                DoScoreTrick(CurTrick, Players[winningPlayer]);

                // Handle last trick
                if (player.Cards.Count == 11)
                {
                    DoLastTrick(winningPlayer);
                    UpdateAndBroadcastAllScores();

                    // Handle game over
                    if (Players.Any(p => p.Score >= GetWinningPointTotal()))
                    {
                        Broadcast(new GameOverMessage(DoGetGameWinningPlayer()));
                        IsGameOver = true;
                    }

                    // Initiate next round
                    else
                    {
                        NextDealer();
                        StartRound();
                    }
                }

                // Initiate next trick
                else
                {
                    StartTrick(winningPlayer);
                }
            }

            // Handle next turn in trick
            else
            {
                bool isFirst = !Players.Any(p => p.TookATrick);
                Server.Instance().Send(new TurnMessage(player.Name, DoGetValidCards(player.Cards, CurTrick, isFirst)), player.Uid);
            }
        }

        /// <summary>
        /// Update all player scores and broadcast them to all players
        /// </summary>
        private void UpdateAndBroadcastAllScores()
        {
            foreach (Player p in Players)
            {
                p.OldScore = p.Score;
            }
            DoUpdateScores();
            foreach (Player p in Players)
            {
                p.ResetPerHandScores();
                BroadcastScore(p.Name);
            }
        }

        /// <summary>
        /// Reset this game
        /// </summary>
        private void Reset()
        {
            CurTrick = new List<Card>();
            Dealer = 0;
            Leader = 0;
            CurPlayer = 0;
            IsGameOver = false;
            foreach (Player p in Players)
            {
                p.ResetScores();
                p.Cards = null;
            }
        }

        /// <summary>
        /// Remove player from this game, and remove game if all players have been removed
        /// </summary>
        /// <param name="player"> Name of player to remove </param>
        private void RemovePlayer(Player player)
        {
            Players.Remove(player);
            RemovePlayer(player.Uid);
            if (Players.Count == 0)
            {
                RemoveGameManager(this);
            }
        }

        private void SendNotSupportedMessage(string playerName, string unsupportedName)
        {
            string uid = Players.Where(p => p.Name == playerName).Single().Uid;
            Server.Instance().Send(new ErrorResponse("This game does not support " + unsupportedName + "."), uid);
        }
    }
}
