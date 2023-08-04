using System;
using System.Threading;
using System.Threading.Tasks;
using ChessChallenge.Chess;

namespace ChessChallenge.Application
{
    class NoMoveException : Exception
    {
        public NoMoveException(string message) : base(message)
        {
        }
    }

    class BotGameRunner
    {
        public readonly MoveGenerator moveGenerator = new();

        public readonly API.IChessBot whiteBot;
        public readonly API.IChessBot blackBot;
        public readonly ChessClock whiteClock;
        public readonly ChessClock blackClock;
        Board board;

        CancellationTokenSource cancellationTokenSource = new();


        public BotGameRunner(API.IChessBot whiteBot, API.IChessBot blackBot, Board board, ChessClock whiteClock, ChessClock blackClock)
        {
            this.whiteBot = whiteBot;
            this.blackBot = blackBot;
            this.board = board;
            this.whiteClock = whiteClock;
            this.blackClock = blackClock;
        }

        public BotGameRunner(API.IChessBot whiteBot, API.IChessBot blackBot, string startingFen, TimeControl whiteTimeControl, TimeControl blackTimeControl)
        {
            this.whiteBot = whiteBot;
            this.blackBot = blackBot;
            board = new Board();
            board.LoadPosition(startingFen);
            whiteClock = ChessClock.FromTimeControl(whiteTimeControl);
            blackClock = ChessClock.FromTimeControl(blackTimeControl);
        }

        public void Cancel()
        {
            cancellationTokenSource.Cancel();
        }

        public async Task<GameResult> Run()
        {
            while (true)
            {
                cancellationTokenSource.Token.ThrowIfCancellationRequested();
                GameResult result = Arbiter.GetGameState(board);
                if (result != GameResult.InProgress)
                {
                    return result;
                }
                var (botToMove, clock, opponentClock) =
                    board.IsWhiteToMove
                    ? (whiteBot, whiteClock, blackClock)
                    : (blackBot, blackClock, whiteClock);
                try
                {
                    Move botMove = await RunBotAsync(botToMove, clock, opponentClock, board);
                    if (!IsLegal(botMove))
                    {
                        return board.IsWhiteToMove ? GameResult.WhiteIllegalMove : GameResult.BlackIllegalMove;
                    }
                    board.MakeMove(botMove, false);
                }
                catch (TimeoutException)
                {
                    return board.IsWhiteToMove ? GameResult.WhiteTimeout : GameResult.BlackTimeout;
                }
                catch (NoMoveException)
                {
                    return board.IsWhiteToMove ? GameResult.WhiteIllegalMove : GameResult.BlackIllegalMove;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
        }

        bool IsLegal(Move givenMove)
        {
            var moves = moveGenerator.GenerateMoves(board);
            foreach (var legalMove in moves)
            {
                if (givenMove.Value == legalMove.Value)
                {
                    return true;
                }
            }
            return false;
        }

        private static Task<Move> RunBotAsync(API.IChessBot bot, ChessClock clock, ChessClock opponentClock, Board board)
        {
            API.Board apiBoard = new(board);
            API.Timer timer = ChessClock.GetAPITimer(clock, opponentClock);
            TimeSpan maxTime = clock.TimeToTimeout();
            API.Move? move = null;
            Thread thread = new(() =>
            {
                clock.StartTurn();
                move = bot.Think(apiBoard, timer);
                clock.EndTurn();
            });
            thread.Start();
            return Task.Run(() =>
            {
                thread.Join(maxTime + TimeSpan.FromSeconds(1));
                if (thread.IsAlive)
                {
                    thread.Abort();
                    throw new TimeoutException("Bot took too long to make a move and was terminated");
                }
                if (clock.IsTimeOut())
                {
                    throw new TimeoutException("Bot took too long to make a move and timed out");
                }
                if (move == null)
                {
                    throw new NoMoveException("Received no move from bot");
                }
                return new Move(move.Value.RawValue);
            });
        }
    }
}