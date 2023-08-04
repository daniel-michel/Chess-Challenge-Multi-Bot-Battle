using System;
using System.Linq;
using static ChessChallenge.Application.UI.BasicComponents;
using static ChessChallenge.Application.UI.LayoutComponents;

namespace ChessChallenge.Application
{
    class ManageBotsPage : PageRoute
    {
        private int inactiveScrollPosition = 0;
        private int activeScrollPosition = 0;
        public override void Show()
        {
            Padding(
                padding: 20,
                child: () => Column(
                    heightAt: i => i == 0 ? 40 : -1,
                    gap: 15,
                    children: new() {
                        () => Row(
                            gap: 10,
                            children: new() {
                                () => TextButton(
                                    "<- Back",
                                    align: Align.CenterLeft,
                                    onClick: () => Program.router.GoBack()
                                ),
                                () => TextButton(
                                    "Add Bot",
                                    () => Program.router.GoToPage("add_bot")
                                )
                            }
                        ),
                        () => Row(
                            gap: 10,
                            children: new() {
                                () => LabeledContainer(
                                    label: "Inactive Bots",
                                    child: () => Select(
                                        childHeight: 40,
                                        scrollPosition: ref inactiveScrollPosition,
                                        children: BotManager.instance.bots.Values.Where(bot => !Program.botBattle.HasBot(bot.hash)).Select<BotSource, Action>(bot => () => {
                                            FittedTextButton(
                                                bot.Info.Identifier,
                                                align: Align.CenterLeft,
                                                onClick: () => Program.botBattle.AddBot(bot)
                                            );
                                        }).ToList()
                                    )
                                ),
                                () => LabeledContainer(
                                    label: "Active Bots",
                                    child: () => Select(
                                        childHeight: 40,
                                        scrollPosition: ref activeScrollPosition,
                                        children: BotManager.instance.bots.Values.Where(bot => Program.botBattle.HasBot(bot.hash)).Select<BotSource, Action>(bot => () => {
                                            FittedTextButton(
                                                bot.Info.Identifier,
                                                align: Align.CenterLeft,
                                                onClick: () => Program.botBattle.RemoveBot(bot.hash)
                                            );
                                        }).ToList()
                                    )
                                )
                            }
                        )
                    }
                )
            );
        }
    }
}