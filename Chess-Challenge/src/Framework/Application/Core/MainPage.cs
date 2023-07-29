using Raylib_cs;

namespace ChessChallenge.Application
{
    class MainPage : PageRoute
    {
        public override void Show()
        {
            Raylib.BeginMode2D(Program.cam);

            Program.controller.Update();
            Program.controller.Draw();

            Raylib.EndMode2D();

            Program.controller.DrawOverlay();
        }
    }
}