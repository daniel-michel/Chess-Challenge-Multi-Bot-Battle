using System;

namespace ChessChallenge.Application
{
    abstract record TimeControl { };
    record TimeControlInfinite : TimeControl { };
    record TimeControlFixed(TimeSpan Time) : TimeControl { };
    record TimeControlIncremental(TimeSpan Time, TimeSpan Increment) : TimeControlFixed(Time) { };
    record TimeControlDelay(TimeSpan Time, TimeSpan Delay) : TimeControlFixed(Time) { };

    abstract class ChessClock
    {
        public static ChessClock FromTimeControl(TimeControl timeControl)
        {
            return timeControl switch
            {
                TimeControlIncremental timeControlIncremental => new IncrementalChessClock(timeControlIncremental),
                TimeControlDelay timeControlDelay => new DelayChessClock(timeControlDelay),
                TimeControlFixed timeControlFixed => new FixedChessClock(timeControlFixed),
                TimeControlInfinite => new InfiniteChessClock(),
                _ => throw new ArgumentException("Unknown time control type")
            };
        }
        public static API.Timer GetAPITimer(ChessClock clock, ChessClock opponentClock)
        {
            int startingMilliseconds = clock switch
            {
                InfiniteChessClock => int.MaxValue,
                FixedChessClock fixedClock => (int)fixedClock.fixedTimeControl.Time.TotalMilliseconds,
                _ => throw new ArgumentException("Unknown time control type")
            };
            int incrementMilliseconds = clock switch
            {
                IncrementalChessClock incrementalClock => (int)incrementalClock.incrementalTimeControl.Increment.TotalMilliseconds,
                _ => 0
            };
            return new API.Timer(
                (int)clock.TimeLeft().TotalMilliseconds,
                (int)opponentClock.TimeLeft().TotalMilliseconds,
                startingMilliseconds,
                incrementMilliseconds
            );
        }

        public abstract void StartTurn();
        public abstract void EndTurn();
        public abstract void Reset();

        public abstract bool IsPaused();
        public abstract TimeSpan TimeLeft();
        public virtual TimeSpan TimeToTimeout() => TimeLeft();
        public bool IsTimeOut()
        {
            return TimeLeft() < TimeSpan.Zero;
        }
    }
    class InfiniteChessClock : ChessClock
    {
        public override bool IsPaused() => false;
        public override TimeSpan TimeLeft() => TimeSpan.FromHours(1);
        public override void StartTurn() { }
        public override void EndTurn() { }
        public override void Reset() { }
    }
    class FixedChessClock : ChessClock
    {
        public readonly TimeControlFixed fixedTimeControl;
        protected TimeSpan timeLeftBeforeTurn;
        protected DateTime turnStart;
        bool paused = true;

        public FixedChessClock(TimeControlFixed timeControl)
        {
            this.fixedTimeControl = timeControl;
            timeLeftBeforeTurn = timeControl.Time;
        }

        public override void Reset()
        {
            timeLeftBeforeTurn = fixedTimeControl.Time;
            paused = true;
        }

        public override bool IsPaused() => paused;
        public override TimeSpan TimeLeft() => paused ? timeLeftBeforeTurn : timeLeftBeforeTurn - (DateTime.Now - turnStart);

        public override void StartTurn()
        {
            if (!paused)
            {
                return;
            }
            paused = false;
            turnStart = DateTime.Now;
        }

        public override void EndTurn()
        {
            if (paused)
            {
                return;
            }
            timeLeftBeforeTurn = TimeLeft();
            paused = true;
        }
    }
    class IncrementalChessClock : FixedChessClock
    {
        public readonly TimeControlIncremental incrementalTimeControl;

        public IncrementalChessClock(TimeControlIncremental timeControl)
            : base(new TimeControlFixed(timeControl.Time))
        {
            incrementalTimeControl = timeControl;
        }
        public override void EndTurn()
        {
            if (IsPaused())
            {
                return;
            }
            base.EndTurn();
            if (!IsTimeOut())
            {
                timeLeftBeforeTurn += incrementalTimeControl.Increment;
            }
        }
    }
    class DelayChessClock : FixedChessClock
    {
        public readonly TimeControlDelay delayTimeControl;

        public DelayChessClock(TimeControlDelay timeControl)
            : base(new TimeControlFixed(timeControl.Time))
        {
            delayTimeControl = timeControl;
        }

        public override TimeSpan TimeToTimeout()
        {
            if (IsPaused())
            {
                return timeLeftBeforeTurn + delayTimeControl.Delay;
            }
            var timePassed = DateTime.Now - turnStart;
            return timeLeftBeforeTurn - timePassed + delayTimeControl.Delay;
        }

        public override TimeSpan TimeLeft()
        {
            if (IsPaused())
            {
                return timeLeftBeforeTurn;
            }
            var timePassed = DateTime.Now - turnStart;
            if (timePassed < delayTimeControl.Delay)
            {
                return timeLeftBeforeTurn;
            }
            return timeLeftBeforeTurn - (timePassed - delayTimeControl.Delay);
        }
    }
}