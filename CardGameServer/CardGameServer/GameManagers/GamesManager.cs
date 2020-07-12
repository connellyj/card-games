using CardGameServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CardGameServer.GameManagers
{
    public class GamesManager
    {
        // Map of game type to a map of game name to GameManager
        private Dictionary<string, Dictionary<string, GameManager>> GameNameMap;

        // Map of player uid to game name
        private Dictionary<string, string> PlayerGameNameMap;

        // Map of player uid to game type
        private Dictionary<string, string> PlayerGameTypeMap;

        // Static instance of this class
        private static GamesManager Instance;

        // Names of playable card games
        private static readonly string HEARTS = "Hearts";
        private static readonly string MIZERKA = "Mizerka";
        private static readonly string PINOCHLE = "Pinochle";

        // Map of game type to GameManager type
        private static readonly Dictionary<string, Type> GameManagerMap = new Dictionary<string, Type>()
        {
            { HEARTS, typeof(HeartsGameManager) },
            { MIZERKA, typeof(MizerkaGameManager) },
            { PINOCHLE, typeof(PinochleGameManager) }
        };

        public GamesManager()
        {
            GameNameMap = new Dictionary<string, Dictionary<string, GameManager>>
            {
                { HEARTS, new Dictionary<string, GameManager>() },
                { MIZERKA, new Dictionary<string, GameManager>() },
                { PINOCHLE, new Dictionary<string, GameManager>() }
            };
            PlayerGameNameMap = new Dictionary<string, string>();
            PlayerGameTypeMap = new Dictionary<string, string>();
        }

        public static GamesManager Get()
        {
            if (Instance == null)
            {
                Reset();
            }
            return Instance;
        }

        public static void Reset()
        {
            Instance = new GamesManager();
        }

        /// <summary>
        /// Get messages to send when a new connection appears
        /// </summary>
        /// <returns> List of messages to be sent </returns>
        public MessagePackets HandleNewConnection(string uid)
        {
            lock (this)
            {
                return new MessagePackets(new GameTypeMessage(GameManagerMap.Select(kvp => kvp.Key).ToArray()), uid);
            }
        }

        /// <summary>
        /// Get messages to send in response to a GameTypeMessage
        /// </summary>
        /// <param name="uid"> The uid of the player </param>
        /// <param name="gameTypeMessage"></param>
        /// <returns> List of messages to be sent </returns>
        public MessagePackets HandleGameTypes(string uid, GameTypeMessage gameTypeMessage)
        {
            lock (this)
            {
                if (GameManagerMap.ContainsKey(gameTypeMessage.ChosenGame))
                {
                    PlayerGameTypeMap.Add(uid, gameTypeMessage.ChosenGame);
                    return new MessagePackets(GetAvailableGames(gameTypeMessage.ChosenGame), uid);
                }
                else
                {
                    throw new Exception(gameTypeMessage.ChosenGame + " is not a valid game. Available games are: " + string.Join(", ", GameManagerMap.Keys));
                }
            }
        }

        /// <summary>
        /// Get messages to send in response to a JoinMessage
        /// </summary>
        /// <param name="uid"> The uid of the player </param>
        /// <param name="joinMessage"></param>
        /// <returns> List of messages to be sent </returns>
        public MessagePackets HandleJoin(string uid, JoinMessage joinMessage)
        {
            lock (this)
            {
                if (PlayerGameTypeMap.ContainsKey(uid))
                {
                    PlayerGameNameMap.Add(uid, joinMessage.GameName);

                    string gameType = PlayerGameTypeMap[uid];
                    if (GameNameMap[gameType].ContainsKey(joinMessage.GameName))
                    {
                        // The game exists, so join it
                        return GameNameMap[gameType][joinMessage.GameName].HandleJoin(uid, joinMessage);
                    }
                    else
                    {
                        MessagePackets messages = new MessagePackets();

                        // The game doesn't exist, so make a new one and join it
                        GameManager gm = (GameManager)Activator.CreateInstance(GameManagerMap[gameType]);
                        GameNameMap[gameType].Add(joinMessage.GameName, gm);
                        messages.Add(gm.HandleJoin(uid, joinMessage));

                        // Push available games update
                        messages.Add(GetAvailableGamesMessage(gameType));

                        return messages;
                    }
                }
                else
                {
                    throw new Exception("Game type not known for player " + uid);
                }
            }
        }

        /// <summary>
        /// Get messages to send in response to a player disconnecting
        /// </summary>
        /// <param name="playerId"> The uid of the player </param>
        /// <returns> List of messages to be sent </returns>
        public MessagePackets HandlePlayerDisconnect(string playerId)
        {
            lock (this)
            {
                MessagePackets messages = new MessagePackets();

                GameManager gm = GetGameManager(playerId);
                messages.Add(Do(gm, () => gm.HandleDisconnect(playerId)));
                messages.Add(RemovePlayer(playerId));

                return messages;
            }
        }

        /// <summary>
        /// Get messages to send in response to a RestartMessage
        /// </summary>
        /// <param name="playerId"> The uid of the player </param>
        /// <param name="restartMessage"></param>
        /// <returns> List of messages to be sent </returns>
        public MessagePackets HandleRestart(string playerId, RestartMessage restartMessage)
        {
            lock (this)
            {
                MessagePackets messages = new MessagePackets();

                GameManager gm = GetGameManager(playerId);
                messages.Add(Do(gm, () => gm.HandleRestart(restartMessage)));

                if (restartMessage.NewGame)
                {
                    RemovePlayer(playerId);
                }

                return messages;
            }
        }

        /// <summary>
        /// Get messages to send in response to a BidMessage
        /// </summary>
        /// <param name="playerId"> The uid of the player </param>
        /// <param name="bidMessage"></param>
        /// <returns> List of messages to be sent </returns>
        public MessagePackets HandleBid(string playerId, BidMessage bidMessage)
        {
            lock (this)
            {
                GameManager gm = GetGameManager(playerId);
                return Do(gm, () => gm.HandleBid(bidMessage));
            }
        }

        /// <summary>
        /// Get messages to send in response to a KittyMessage
        /// </summary>
        /// <param name="playerId"> The uid of the player </param>
        /// <param name="kittyMessage"></param>
        /// <returns> List of messages to be sent </returns>
        public MessagePackets HandleKitty(string playerId, KittyMessage kittyMessage)
        {
            lock (this)
            {
                GameManager gm = GetGameManager(playerId);
                return Do(gm, () => gm.HandleKitty(kittyMessage));
            }
        }

        /// <summary>
        /// Get messages to send in response to a TrumpMessage
        /// </summary>
        /// <param name="playerId"> The uid of the player </param>
        /// <param name="trumpMessage"></param>
        /// <returns> List of messages to be sent </returns>
        public MessagePackets HandleTrump(string playerId, TrumpMessage trumpMessage)
        {
            lock (this)
            {
                GameManager gm = GetGameManager(playerId);
                return Do(gm, () => gm.HandleTrump(trumpMessage));
            }
        }

        /// <summary>
        /// Get messages to send in response to a MeldMessage
        /// </summary>
        /// <param name="playerId"> The uid of the player </param>
        /// <param name="meldMessage"></param>
        /// <returns> List of messages to be sent </returns>
        public MessagePackets HandleMeld(string playerId, MeldMessage meldMessage)
        {
            lock (this)
            {
                GameManager gm = GetGameManager(playerId);
                return Do(gm, () => gm.HandleMeld(meldMessage));
            }
        }

        /// <summary>
        /// Get messages to send in response to a PassMessage
        /// </summary>
        /// <param name="playerId"> The uid of the player </param>
        /// <param name="passMessage"></param>
        /// <returns> List of messages to be sent </returns>
        public MessagePackets HandlePass(string playerId, PassMessage passMessage)
        {
            lock (this)
            {
                GameManager gm = GetGameManager(playerId);
                return Do(gm, () => gm.HandlePass(passMessage));
            }
        }

        /// <summary>
        /// Get messages to send in response to a TurnMessage
        /// </summary>
        /// <param name="playerId"> The uid of the player </param>
        /// <param name="turnMessage"></param>
        /// <returns> List of messages to be sent </returns>
        public MessagePackets HandleTurn(string playerId, TurnMessage turnMessage)
        {
            lock (this)
            {
                GameManager gm = GetGameManager(playerId);
                return Do(gm, () => gm.HandleTurn(turnMessage));
            }
        }

        private MessagePackets GetAvailableGamesMessage(string gameType)
        {
            return new MessagePackets(GetAvailableGames(gameType),
                PlayerGameTypeMap.Where(kvp => kvp.Value == gameType).Select(kvp => kvp.Key));
        }

        private AvailableGamesMessage GetAvailableGames(string gameType)
        {
            IEnumerable<string> games = GameNameMap[gameType]
                .Where(kvp => !kvp.Value.HasStarted())
                .Where(kvp => !kvp.Value.HasEnded())
                .Select(kvp => kvp.Key);
            return new AvailableGamesMessage(games.ToArray());
        }

        /// <summary>
        /// Remove the given player
        /// </summary>
        /// <param name="uid"> The uid of the player to remove </param>
        /// <returns> List of messages to be sent </returns>
        private MessagePackets RemovePlayer(string uid)
        {
            string gameType = PlayerGameTypeMap[uid];
            string gameName = PlayerGameNameMap[uid];
            PlayerGameTypeMap.Remove(uid);
            PlayerGameNameMap.Remove(uid);
            IEnumerable<string> playersWithSameType = PlayerGameTypeMap.Where(kvp => kvp.Value == gameType).Select(kvp => kvp.Key);
            IEnumerable<string> playersWithSameGame = PlayerGameNameMap.Where(kvp => kvp.Value == gameName).Select(kvp => kvp.Key);
            if (playersWithSameType.Intersect(playersWithSameGame).Count() == 0)
            {
                GameNameMap[gameType].Remove(gameName);
                return GetAvailableGamesMessage(gameType);
            }
            else
            {
                return new MessagePackets();
            }
        }

        /// <summary>
        /// Get the GameManager the given player is a part of.
        /// </summary>
        /// <param name="playerId"> The uid of the player </param>
        /// <returns> The GameManager or null if the player isn't mapped to a GameManager </returns>
        private GameManager GetGameManager(string playerId)
        {
            if (PlayerGameNameMap.ContainsKey(playerId) && PlayerGameNameMap.ContainsKey(playerId) && 
                GameNameMap[PlayerGameTypeMap[playerId]].ContainsKey(PlayerGameNameMap[playerId]))
            {
                return GameNameMap[PlayerGameTypeMap[playerId]][PlayerGameNameMap[playerId]];
            }
            else
            {
                return null;
            }
        }

        private MessagePackets Do(GameManager gm, Func<MessagePackets> todo)
        {
            if (gm != null)
            {
                return todo();
            }
            else
            {
                throw new Exception("The GameManager has not been created");
            }
        }
    }
}
