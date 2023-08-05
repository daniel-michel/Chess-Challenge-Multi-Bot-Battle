using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChessChallenge.Chess;

namespace ChessChallenge.Application
{
    class OneSideGameResult
    {
        public string botHash;
        public int winsWithWhite;
        public int winsWithBlack;
        public int drawsWithWhite;
        public int TotalWins { get => winsWithWhite + winsWithBlack; }
    }
    class GameResults
    {
        public OneSideGameResult firstBot;
        public OneSideGameResult secondBot;

        public int TotalGamesWithWhiteFirstBot
        {
            get => firstBot.winsWithWhite + firstBot.drawsWithWhite + secondBot.winsWithBlack;
        }
        public int TotalGamesWithWhiteSecondBot
        {
            get => secondBot.winsWithWhite + secondBot.drawsWithWhite + firstBot.winsWithBlack;
        }
        public int TotalDraws { get => firstBot.drawsWithWhite + secondBot.drawsWithWhite; }
        public int TotalWinsWithWhite { get => firstBot.winsWithWhite + secondBot.winsWithWhite; }
        public int TotalWinsWithBlack { get => firstBot.winsWithBlack + secondBot.winsWithBlack; }
        public int TotalGames { get => TotalDraws + TotalWinsWithWhite + TotalWinsWithBlack; }
    }
    class BotBattle
    {
        public static (string, string) SortHashes(string firstBotHash, string secondBotHash)
        {
            return string.Compare(firstBotHash, secondBotHash) < 0
                ? (firstBotHash, secondBotHash)
                : (secondBotHash, firstBotHash);
        }
        public static string GetTwoBotHash(string firstBotHash, string secondBotHash)
        {
            var (first, second) = SortHashes(firstBotHash, secondBotHash);
            return $"{first}:{second}";
        }

        public Dictionary<string, BotSource> bots = new();
        public Dictionary<string, GameResults> results = new();

        List<(Task<GameResult> task, BotGameRunner gameRunner, string whiteBotHash, string blackBotHash)> runningGames = new();

        bool running = false;
        int maxConcurrentGames = 8;

        public int MaxConcurrentGames
        {
            get => maxConcurrentGames;
            set
            {
                maxConcurrentGames = value;
                StartGames();
            }
        }

        CancellationTokenSource cancellationTokenSource = new();

        public void AddBot(BotSource botSource)
        {
            lock (bots)
            {
                bots.Add(botSource.hash, botSource);
                StartGames();
            }
        }
        public void RemoveBot(string hash)
        {
            lock (bots)
            {
                bots.Remove(hash);
            }
        }
        public bool HasBot(string hash)
        {
            lock (bots)
            {
                return bots.ContainsKey(hash);
            }
        }

        public void AddResult(string whiteHash, string blackHash, GameResult result)
        {
            string combinedHash = GetTwoBotHash(whiteHash, blackHash);
            var (first, second) = SortHashes(whiteHash, blackHash);
            if (!results.ContainsKey(combinedHash))
            {
                results.Add(combinedHash, new GameResults()
                {
                    firstBot = new OneSideGameResult() { botHash = first },
                    secondBot = new OneSideGameResult() { botHash = second }
                });
            }
            bool firstBotIsWhite = first == whiteHash;
            var gameResults = results[combinedHash];
            var (whiteBotResults, blackBotResults) = firstBotIsWhite ? (gameResults.firstBot, gameResults.secondBot) : (gameResults.secondBot, gameResults.firstBot);
            if (Arbiter.IsDrawResult(result))
            {
                whiteBotResults.drawsWithWhite++;
                return;
            }
            if (Arbiter.IsBlackWinsResult(result))
            {
                blackBotResults.winsWithBlack++;
                return;
            }
            if (Arbiter.IsWhiteWinsResult(result))
            {
                whiteBotResults.winsWithWhite++;
                return;
            }
            throw new Exception("Unknown result");
        }

        public void Cancel()
        {
            cancellationTokenSource.Cancel();
            lock (runningGames)
            {
                foreach (var (_, gameRunner, _, _) in runningGames)
                {
                    gameRunner.Cancel();
                }
            }
        }

        public async Task Run()
        {
            if (running)
            {
                return;
            }
            running = true;
            await StartGames();
            while (true)
            {
                while (runningGames.Count == 0)
                {
                    await Task.Delay(100);
                }
                cancellationTokenSource.Token.ThrowIfCancellationRequested();
                var finishedGameTask = await Task.WhenAny(runningGames.Select(game => game.task));
                string whiteHash;
                string blackHash;
                lock (runningGames)
                {
                    var finishedGame = runningGames.Find(game => game.task == finishedGameTask);
                    var (task, runner, whiteBot, blackBot) = finishedGame;
                    whiteHash = whiteBot;
                    blackHash = blackBot;
                    runningGames.Remove(finishedGame);
                }
                var result = await finishedGameTask;
                AddResult(whiteHash, blackHash, result);
                await StartGames();
            }
        }

        public async Task StartGames()
        {
            if (!running)
            {
                return;
            }

            int currentlyRunningGames = runningGames.Count;
            int gamesToStart = maxConcurrentGames - currentlyRunningGames;
            if (gamesToStart <= 0)
            {
                return;
            }
            var leastPlayedCombinations = await Task.Run(() => LeastPlayedCombination(gamesToStart));
            if (leastPlayedCombinations.Count == 0)
            {
                return;
            }
            List<(BotGameRunner gameRunner, string whiteHash, string blackHash)> gameRunners = new();
            for (int i = 0; i < gamesToStart; i++)
            {
                int index = i % leastPlayedCombinations.Count;
                var (whiteBot, blackBot, _) = leastPlayedCombinations[index];
                TimeControl timeControl = new TimeControlFixed(TimeSpan.FromMinutes(1));
                BotGameRunner gameRunner = await GetGameRunner(whiteBot, blackBot, FenUtility.StartPositionFEN, timeControl, timeControl);
                gameRunners.Add((gameRunner, whiteBot.hash, blackBot.hash));
            }
            bool countChanged = false;
            lock (runningGames)
            {
                if (runningGames.Count != currentlyRunningGames)
                {
                    countChanged = true;
                }
                else
                {
                    foreach (var gameRunner in gameRunners)
                    {
                        runningGames.Add((gameRunner.gameRunner.Run(), gameRunner.gameRunner, gameRunner.whiteHash, gameRunner.blackHash));
                    }
                }
            }
            if (countChanged)
            {
                Console.WriteLine("Count of running games changed, restarting");
                await StartGames();
            }
        }

        public List<(BotSource white, BotSource black, int played)> LeastPlayedCombination(int count)
        {
            List<(BotSource white, BotSource black, int played)> leastPlayedCombinations = new();
            lock (bots)
            {
                foreach (var (whiteHash, whiteBot) in bots)
                {
                    foreach (var (blackHash, blackBot) in bots)
                    {
                        if (whiteHash == blackHash)
                        {
                            continue;
                        }
                        string combinedHash = GetTwoBotHash(whiteHash, blackHash);
                        var (firstBotHash, _) = SortHashes(whiteHash, blackHash);
                        var (firstCount, secondCount) = results.ContainsKey(combinedHash)
                            ? (
                                results[combinedHash].TotalGamesWithWhiteFirstBot,
                                results[combinedHash].TotalGamesWithWhiteSecondBot
                              )
                            : (0, 0);
                        int gameCount = firstBotHash == whiteHash ? firstCount : secondCount;
                        bool add = leastPlayedCombinations.Count < count;
                        if (!add)
                        {
                            int maxPlayedIndex = -1;
                            for (int i = 0; i < leastPlayedCombinations.Count; i++)
                            {
                                if (leastPlayedCombinations[i].played > gameCount && (maxPlayedIndex < 0 || leastPlayedCombinations[i].played > leastPlayedCombinations[maxPlayedIndex].played))
                                {
                                    maxPlayedIndex = i;
                                }
                            }
                            if (maxPlayedIndex >= 0)
                            {
                                leastPlayedCombinations.RemoveAt(maxPlayedIndex);
                                add = true;
                            }
                        }
                        if (add)
                        {
                            leastPlayedCombinations.Add((whiteBot, blackBot, gameCount));
                        }
                    }
                }
            }
            return leastPlayedCombinations;
        }

        public async Task<BotGameRunner> GetGameRunner(BotSource whiteBot, BotSource blackBot, string startFen, TimeControl whiteTimeControl, TimeControl blackTimeControl)
        {
            API.IChessBot whiteChessBot = await whiteBot.CreateBot();
            API.IChessBot blackChessBot = await blackBot.CreateBot();
            BotGameRunner gameRunner = new(whiteChessBot, blackChessBot, startFen, whiteTimeControl, blackTimeControl);
            return gameRunner;
        }
    }
}