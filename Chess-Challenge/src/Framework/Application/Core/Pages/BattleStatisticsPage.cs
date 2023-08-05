using System;
using System.Collections.Generic;
using System.Linq;
using Raylib_cs;
using static ChessChallenge.Application.UI.BasicComponents;
using static ChessChallenge.Application.UI.LayoutComponents;

namespace ChessChallenge.Application
{
    class BattleStatisticsPage : PageRoute
    {
        public override void Show()
        {
            Column(
                gap: 10,
                heightAt: i => i == 0 ? 40 : -1,
                children: new()
                {
                    () => TextButton("<- Back", () => Program.router.GoBack()),
                    () => DrawResultTable()
                }
            );
        }

        public void DrawResultTable()
        {
            List<BotSource> bots = Program.botBattle.bots.Values.ToList();
            List<string> botHashes = bots.ConvertAll(bot => bot.hash);

            List<Action> botComponents = bots.ConvertAll<Action>(bot => () => DrawBot(bot));
            var botComponentsWithEmptyFirst = new List<Action>(botComponents);
            botComponentsWithEmptyFirst.Insert(0, () => { });
            Action headerRow = () => Row(
                children: botComponentsWithEmptyFirst
            );
            List<Action> rows = bots.ConvertAll<Action>(bot => () =>
            {
                Action headerCell = () => DrawBot(bot);
                List<Action> cells = bots.ConvertAll<Action>(secondBot => () =>
                {
                    string twoBotHash = BotBattle.GetTwoBotHash(bot.hash, secondBot.hash);
                    if (Program.botBattle.results.TryGetValue(twoBotHash, out GameResults? results) && results != null)
                    {
                        var (firstHash, _) = BotBattle.SortHashes(bot.hash, secondBot.hash);
                        var (first, second) = firstHash == bot.hash ? (results.firstBot, results.secondBot) : (results.secondBot, results.firstBot);
                        DrawResultCell(results, first, second);
                    }
                    else
                    {
                        FittedText("N/A", 30, align: Align.Center);
                    }
                });
                cells.Insert(0, headerCell);
                Row(
                    gap: 5,
                    children: cells
                );
            });

            List<Action> allRows = new List<Action>(rows);
            allRows.Insert(0, headerRow);

            Column(
                gap: 5,
                children: allRows
            );
        }

        public void DrawBot(BotSource bot)
        {
            FittedText(
                bot.Info.Identifier,
                30,
                align: Align.Center
            );
        }

        public void DrawResultCell(GameResults results, OneSideGameResult first, OneSideGameResult second)
        {
            Column(
                heightAt: i => i == 0 ? 10 : -1,
                children: new()
                {
                    () =>  HorizontalBarSectionChart(
                        new()
                        {
                            (first.TotalWins, Color.GREEN),
                            (results.TotalDraws, Color.GRAY),
                            (second.TotalWins, Color.RED)
                        }
                    ),
                    () => FittedText(
                        $"+{first.TotalWins} ={results.TotalDraws} -{second.TotalWins}",
                        30,
                        align: Align.Center
                    )
                }
            );
        }
    }
}