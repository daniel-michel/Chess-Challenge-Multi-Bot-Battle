using System;
using Raylib_cs;
using static ChessChallenge.Application.UI.BasicComponents;
using static ChessChallenge.Application.UI.LayoutComponents;

namespace ChessChallenge.Application
{
    class AddBotPage : PageRoute
    {
        int selectScrollPosition = 0;
        int codeScrollPosition = 0;
        BotSourcer sourcer = new();

        SourceDescription? sourceDescription = null;
        string? code = null;
        bool? isAdded = null;

        public override void Show()
        {
            Padding(
                padding: 20,
                child: () => Column(
                    heightAt: i => i == 0 ? 40 : -1,
                    gap: 15,
                    children: new() {
                        () => TextButton("<- Back", () => Program.router.GoBack(), align: Align.CenterLeft),
                        () => Row(
                            gap: 10,
                            children: new() {
                                SourcerComponent,
                                DrawCode
                            }
                        )
                    }
                )
            );
        }

        public void SourcerComponent()
        {
            Column(
                heightAt: i => i == 0 ? 35 : -1,
                gap: 10,
                children: new() {
                    () => Row(
                        widthAt: i => i == 0 ? 35 : -1,
                        gap: 5,
                        children: new() {
                            () => TextButton("^", () => sourcer.GoUp()),
                            () => FittedText(sourcer.GetPath(), 30, align: Align.CenterLeft)
                        }
                    ),
                    () => Select(
                        childHeight: 40,
                        scrollPosition: ref selectScrollPosition,
                        children: sourcer.GetOptions().ConvertAll<Action>(option => () => {
                            FittedTextButton(
                                option,
                                align: Align.CenterLeft,
                                onClick: () => {
                                    if (sourcer.SelectOption(option, out SourceDescription? source) && source != null)
                                    {
                                        sourceDescription = source;
                                        code = source.GetFileContents();
                                        isAdded = BotManager.instance.HasBot(SyntaxTreeHashGenerator.GenerateHash(code));
                                        // BotManager.Instance.AddBot(source);
                                        // code = source.Code;
                                        // var bot = source.CreateBot();
                                    }
                                }
                            );
                        })
                    )
                }
            );
        }

        public void DrawCode()
        {
            Column(
                heightAt: i => i == 0 ? 35 : -1,
                children: new() {
                    () => {
                        if (isAdded == null)
                        {
                            return;
                        }
                        TextButton(
                            "Add: " + (isAdded == true ? "override" : "new"),
                            align: Align.Center,
                            onClick: () => {
                                if (sourceDescription != null && code != null)
                                {
                                    BotManager.instance.SetBot(new BotSource(sourceDescription));
                                }
                            }
                        );
                    },
                    () => Container(
                        color: new Color(10, 10, 10, 255),
                        child: () => {
                            if (code != null) TextArea(code, 30, ref codeScrollPosition);
                        }
                    )
                }
            );
        }
    }
}