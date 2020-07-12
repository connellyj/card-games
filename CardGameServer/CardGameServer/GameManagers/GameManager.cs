using CardGameServer.GameMechanics;
using CardGameServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CardGameServer.GameManagers
{
    public class GameManager
    {
        // ********** Static member variables ********** //

        // Static list of card suits
        protected static readonly List<string> Suits = new List<string>() { "C", "D", "S", "H" };

        // Static list of card ranks
        private static readonly List<string> Ranks = new List<string>() { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };

        // The number of points required to end the game
        private static readonly int WINNING_POINTS = 100;

        // The number of players required to play
        private static readonly int MIN_PLAYERS = 4;

        // The number of cards in each players hand
        private static readonly int NUM_CARDS_IN_HAND = 13;


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

        // Whether the game is started
        private bool IsStarted;

        // Whether the game is over
        private bool IsGameOver;

        private readonly TrickDecider Decider;


        /// <summary>
        /// Constructor
        /// </summary>
        public GameManager(TrickDecider m=null)
        {
            if (m == null)
            {
                Decider = new TrickDecider();
            }
            else
            {
                Decider = m;
            }
            Players = new List<Player>();
            Reset();
        }

        public bool HasStarted()
        {
            return IsStarted;
        }

        public bool HasEnded()
        {
            return IsGameOver;
        }

        public List<Player> GetPlayers()
        {
            return Players.Select(p => new Player(p)).ToList();
        }

        /// <summary>
        /// Get the index of the dealer.
        /// </summary>
        /// <returns> Index of the dealer </returns>
        public int GetDealerIndex()
        {
            return Dealer;
        }

        /// <summary>
        /// Get the index of the current player.
        /// </summary>
        /// <returns> Index of the current player </returns>
        public int GetCurrentPlayerIndex()
        {
            return CurPlayer;
        }

        /// <summary>
        /// Handle a DisconnectMessage.
        /// </summary>
        /// <param name="playerId"> The uid of the disconnected player </param>
        /// <returns> List of messages to be sent </returns>
        public MessagePackets HandleDisconnect(string playerId)
        {
            MessagePackets messages = new MessagePackets();

            Player player = Players.Where(p => p.Uid == playerId).SingleOrDefault();
            if (player != null)
            {
                messages.Add(DisconnectPlayer(player));
            }
            else
            {
                throw new Exception("Player " + playerId + " does not exist");
            }

            return messages;
        }

        /// <summary>
        /// Handle a RestartMessage.
        /// </summary>
        /// <param name="restartMessage"></param>
        /// <returns> List of messages to be sent </returns>
        public MessagePackets HandleRestart(RestartMessage restartMessage)
        {
            if (!restartMessage.NewGame)
            {
                // Restart the same game
                VerifyStarted();
                Reset();
                return StartRound();
            }
            else
            {
                MessagePackets messages = new MessagePackets();

                // Confirm restart request
                Player player = Players.Where(p => p.Name == restartMessage.PlayerName).Single();
                messages.Add(restartMessage, player.Uid);

                // Inform other players the player left the old game
                messages.Add(DisconnectPlayer(player));

                return messages;
            }
        }

        /// <summary>
        /// Handle a JoinMessage.
        /// </summary>
        /// <param name="message"> The received join message </param>
        /// <param name="playerId"> The uid of the player joining </param>
        /// <returns> List of messages to be sent </returns>
        public MessagePackets HandleJoin(string playerId, JoinMessage message)
        {
            // Verify game state
            VerifyNotOver();
            VerifyNotStarted();

            MessagePackets messages = new MessagePackets();

            if (Players.Any(p => p.Name == message.UserName))
            {
                // Handle user name already exists in game
                messages.Add(new JoinResponse(false, errorMessage: string.Format("The name '{0}' already exists in the game '{1}'", message.UserName, message.GameName)), playerId);
            }
            else
            {
                // Send success response
                messages.Add(new JoinResponse(true, message.UserName), playerId);

                // Inform new player of all existing players
                foreach (Player p in Players)
                {
                    messages.Add(new JoinMessage(p.Name, message.GameName, p.Order), playerId);
                }

                // Create new player model
                Player player = new Player(message.UserName, playerId, Players.Count);
                Players.Add(player);

                // Broadcast new player to all players with correct order
                message.Order = player.Order;
                messages.Add(GetBroadcastMessage(message));

                // There are enough players, so start the game
                if (Players.Count == DoGetMinPlayers())
                {
                    messages.Add(StartRound());
                }
            }

            return messages;
        }

        /// <summary>
        /// Handle a TurnMessage.
        /// </summary>
        /// <param name="message"> The received turn message </param>
        /// <returns> List of messages to be sent </returns>
        public MessagePackets HandleTurn(TurnMessage message)
        {
            // Verify game state
            VerifyGameRunning();
            VerifyPlayer(message.PlayerName);

            // Broadcast turn to all players
            MessagePackets messages = new MessagePackets();
            messages.Add(GetBroadcastMessage(message));

            // Add card to current trick, and remove from player's hand
            CurTrick.Add(message.Card);
            Players[CurPlayer].Cards.Remove(message.Card);

            // Update CurPlayer
            NextPlayer();

            // Handle all players have played
            if (CurPlayer == Leader)
            {
                // Decide and score trick
                int winningPlayer = DecideTrick();
                messages.Add(ScoreTrick(winningPlayer));

                // Broadcast trick to all players
                messages.Add(GetBroadcastMessage(new TrickMessage(Players[winningPlayer].Name)));

                // Handle last trick
                if (Players.All(p => p.Cards.Count == 0))
                {
                    DoLastTrick(winningPlayer);

                    // Update, send, and clear scores
                    UpdateScores();
                    messages.Add(GetScoreMessages());
                    ClearPerHandScores();

                    // Handle game over
                    if (DoShouldGameEnd())
                    {
                        messages.Add(EndGame());
                    }
                    // Initiate next hand
                    else
                    {
                        NextDealer();
                        messages.Add(StartRound());
                    }
                }
                // Initiate next trick
                else
                {
                    messages.Add(StartTrick(winningPlayer));
                }
            }
            // Handle next turn
            else
            {
                messages.Add(NextTurn());
            }

            return messages;
        }

        /// <summary>
        /// Handle a KittyMessage. By default, do nothing.
        /// Subclasses should override to enable different behaviors.
        /// </summary>
        /// <param name="kittyMessage"></param>
        /// <returns> List of messages to be sent </returns>
        public virtual MessagePackets HandleKitty(KittyMessage kittyMessage)
        {
            throw GetNotSupportedException("KittyMessage");
        }

        /// <summary>
        /// Handle a BidMessage. By default, do nothing.
        /// Subclasses should override to enable different behaviors.
        /// </summary>
        /// <param name="bidMessage"></param>
        /// <returns> List of messages to be sent </returns>
        public virtual MessagePackets HandleBid(BidMessage bidMessage)
        {
            throw GetNotSupportedException("BidMessage");
        }

        /// <summary>
        /// Handle a TrumpMessage. By default, do nothing.
        /// Subclasses should override to enable different behaviors.
        /// </summary>
        /// <param name="trumpMessage"></param>
        /// <returns> List of messages to be sent </returns>
        public virtual MessagePackets HandleTrump(TrumpMessage trumpMessage)
        {
            throw GetNotSupportedException("TrumpMessage");
        }

        /// <summary>
        /// Handle a MeldMessage. By default, do nothing.
        /// Subclasses should override to enable different behaviors.
        /// </summary>
        /// <param name="meldMessage"></param>
        /// <returns> List of messages to be sent </returns>
        public virtual MessagePackets HandleMeld(MeldMessage meldMessage)
        {
            throw GetNotSupportedException("MeldMessage");
        }

        /// <summary>
        /// Handle a PassMessage. By default, do nothing.
        /// Subclasses should override to enable different behaviors.
        /// </summary>
        /// <param name="passMessage"></param>
        /// <returns> List of messages to be sent </returns>
        public virtual MessagePackets HandlePass(PassMessage passMessage)
        {
            throw GetNotSupportedException("PassMessage");
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
        /// <returns> List of messages to be sent </returns>
        protected virtual MessagePackets DoStartRound()
        {
            MessagePackets messages = new MessagePackets();
            messages.Add(GetStartMessages());
            messages.Add(StartTrick(CurPlayer, true));
            return messages;
        }

        /// <summary>
        /// By deafult, do nothing.
        /// Called when a trick has as many cards as there are players in the game.
        /// Subclasses should override to enable different behaviors.
        /// </summary>
        /// <param name="trick"> The completed trick </param>
        /// <param name="winningPlayer"> The index of the player that won the trick </param>
        /// <returns> List of messages to be sent </returns>
        protected virtual MessagePackets DoScoreTrick(List<Card> trick, Player winningPlayer)
        {
            return new MessagePackets();
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
            foreach (Player p in GetPlayersMutable())
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
        /// By default, returns true when any player has 100 points.
        /// Used to determine when the game should end.
        /// Subclasses should override to enable different behaviors.
        /// </summary>
        /// <returns> Whether the game is over </returns>
        protected virtual bool DoShouldGameEnd()
        {
            return Players.Any(p => p.Score >= WINNING_POINTS);
        }

        /// <summary>
        /// By default, returns 4.
        /// Used to determine when the game should start.
        /// Subclasses should override to enable different behaviors.
        /// </summary>
        /// <returns> Number of players required to start the game </returns>
        protected virtual int DoGetMinPlayers()
        {
            return MIN_PLAYERS;
        }

        /// <summary>
        /// By default, returns 13.
        /// Used to deal cards.
        /// Subclasses should override to enable different behaviors.
        /// </summary>
        /// <returns> The number of cards in a players hand </returns>
        protected virtual int DoGetNumCardsInHand()
        {
            return NUM_CARDS_IN_HAND;
        }

        protected TrickDecider GetDecider()
        {
            return Decider;
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
        protected List<Player> GetPlayersMutable()
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
        /// <returns> A packet with all player ids and the given message </returns>
        protected MessagePackets GetBroadcastMessage(Message message)
        {
            return new MessagePackets(message, Players.Select(p => p.Uid));
        }

        protected MessagePackets GetStartMessages()
        {
            MessagePackets messages = new MessagePackets();
            foreach (Player p in Players)
            {
                messages.Add(new StartMessage(p.Cards.ToArray()), p.Uid);
            }
            return messages;
        }

        /// <summary>
        /// Starts a new trick. The provided leader is set as the current player. If isFirstRound, GetFirstTrickValidCards()
        /// is called to get the valid cards. Otherwise, the entire players hand is used.
        /// </summary>
        /// <param name="leader"> Index of the player who should lead the trick </param>
        /// <param name="isFirstTrick"> Whether this trick is the first of the round </param>
        /// <returns> List of messages to be sent </returns>
        protected MessagePackets StartTrick(int leader, bool isFirstTrick=false)
        {
            Leader = leader;
            CurPlayer = leader;
            Player player = Players[CurPlayer];
            CurTrick = new List<Card>();
            List<Card> validCards = isFirstTrick ? Decider.FirstTrickStartValidCards(player.Cards) : Decider.TrickStartValidCards(player.Cards);
            return GetBroadcastMessage(new TurnMessage(player.Name, validCards.ToArray(), isFirstCard: true));
        }

        protected void VerifyPlayer(string playerName)
        {
            if (!Players.Select(p => p.Name).Contains(playerName))
            {
                throw new Exception("Player " + playerName + " does not exist");
            }
            if (Players[CurPlayer].Name != playerName)
            {
                throw new Exception("Player " + playerName + " is not the current player");
            }
        }

        protected void VerifyStarted()
        {
            if (!IsStarted)
            {
                throw new Exception("The game hasn't met the criteria for starting");
            }
        }

        protected void VerifyNotStarted()
        {
            if (IsStarted)
            {
                throw new Exception("The game has already started");
            }
        }

        protected void VerifyNotOver()
        {
            if (IsGameOver)
            {
                throw new Exception("The game has already ended");
            }
        }

        protected void VerifyGameRunning()
        {
            VerifyStarted();
            VerifyNotOver();
        }

        private MessagePackets DisconnectPlayer(Player player)
        {
            Players.Remove(player);
            bool shouldDisable = IsStarted && !IsGameOver;
            IsGameOver = IsStarted;
            return GetBroadcastMessage(new DisconnectMessage(player.Name, shouldDisable));
        }

        /// <summary>
        /// Start a round
        /// </summary>
        /// <returns> List of messages to be sent </returns>
        private MessagePackets StartRound()
        {
            IsStarted = true;
            CurPlayer = (Dealer + 1) % Players.Count;
            Deal();
            return DoStartRound();
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
                p.Cards = deck.Skip(idx * DoGetNumCardsInHand()).Take(DoGetNumCardsInHand()).ToList();
                idx++;
            }
            if (deck.Length > idx * DoGetNumCardsInHand())
            {
                DoDealExtraCards(deck.Skip(idx * DoGetNumCardsInHand()));
            }
        }

        private int DecideTrick()
        {
            return (Leader + Decider.DecideTrick(CurTrick)) % Players.Count;
        }

        private MessagePackets ScoreTrick(int winningPlayer)
        {
            Players[winningPlayer].TricksTaken++;
            return DoScoreTrick(CurTrick, Players[winningPlayer]);
        }

        private MessagePackets NextTurn()
        {
            Player player = Players[CurPlayer];
            bool isFirst = !Players.Any(p => p.TricksTaken > 0);
            List<Card> validCards = isFirst ? Decider.FirstTrickValidCards(player.Cards, CurTrick) : Decider.ValidCards(player.Cards, CurTrick);
            return new MessagePackets(new TurnMessage(player.Name, validCards.ToArray()), player.Uid);
        }

        private void UpdateScores()
        {
            foreach (Player p in Players)
            {
                p.OldScore = p.Score;
            }
            DoUpdateScores();
        }

        /// <summary>
        /// Update all player scores and broadcast them to all players
        /// </summary>
        /// <returns> List of messages to be sent </returns>
        private MessagePackets GetScoreMessages()
        {
            MessagePackets messages = new MessagePackets();
            foreach (Player p in Players)
            {
                messages.Add(GetBroadcastMessage(new ScoreMessage(p.Name, p.Score, p.Score - p.OldScore, p.MissedBidBy)));
            }
            return messages;
        }

        private void ClearPerHandScores()
        {
            foreach (Player p in Players)
            {
                p.ResetPerHandScores();
            }
        }

        private MessagePackets EndGame()
        {
            IsGameOver = true;
            return GetBroadcastMessage(new GameOverMessage(DoGetGameWinningPlayer()));
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
            IsStarted = false;
            foreach (Player p in Players)
            {
                p.ResetScores();
                p.Cards = null;
            }
        }

        private Exception GetNotSupportedException(string unsupportedName)
        {
            return new Exception("This game does not support " + unsupportedName + ".");
        }
    }
}
