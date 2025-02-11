﻿using Raylib_cs;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using ChessChallenge.API;
using ChessChallenge.Application.UI;

namespace ChessChallenge.Application
{
    static class Program
    {
        const bool hideRaylibLogs = true;
        public static Camera2D cam;
        public static ChallengeController controller = new();
        public static Router router = new();
        public static List<IChessBot> bots = new();
        public static BotBattle botBattle = new();

        public static void Main()
        {
            Vector2 loadedWindowSize = GetSavedWindowSize();
            int screenWidth = (int)loadedWindowSize.X;
            int screenHeight = (int)loadedWindowSize.Y;

            if (hideRaylibLogs)
            {
                unsafe
                {
                    Raylib.SetTraceLogCallback(&LogCustom);
                }
            }

            Raylib.SetConfigFlags(ConfigFlags.FLAG_WINDOW_RESIZABLE);
            // Setting min size causes an error:
            // dotnet: external/glfw/src/monitor.c:451: glfwGetVideoMode: Assertion `monitor != NULL' failed.
            // Raylib.SetWindowMinSize(800, 600);
            Raylib.InitWindow(screenWidth, screenHeight, "Chess Coding Challenge");
            Raylib.SetTargetFPS(60);

            UpdateCamera(screenWidth, screenHeight);

            router.AddPage("main", new MainPage());
            router.AddPage("add_bot", new AddBotPage());
            router.AddPage("manage_bots", new ManageBotsPage());
            router.AddPage("battle_statistics", new BattleStatisticsPage());
            router.GoToPage("main");

            Task runningBotBattle = botBattle.Run();

            while (!Raylib.WindowShouldClose())
            {
                Raylib.BeginDrawing();

                Raylib.ClearBackground(new Color(22, 22, 22, 255));
                ComponentUI.Start();

                router.GetCurrentPage().Show();

                Raylib.EndDrawing();
            }

            Raylib.CloseWindow();

            botBattle.Cancel();
            try
            {
                runningBotBattle.Wait();
            }
            catch (System.Exception)
            {
            }

            controller.Release();
            UIHelper.Release();
        }

        public static void SetWindowSize(Vector2 size)
        {
            Raylib.SetWindowSize((int)size.X, (int)size.Y);
            UpdateCamera((int)size.X, (int)size.Y);
            SaveWindowSize();
        }

        public static Vector2 ScreenToWorldPos(Vector2 screenPos) => Raylib.GetScreenToWorld2D(screenPos, cam);

        static void UpdateCamera(int screenWidth, int screenHeight)
        {
            cam = new Camera2D();
            cam.target = new Vector2(0, 15);
            cam.offset = new Vector2(screenWidth / 2f, screenHeight / 2f);
            cam.zoom = screenWidth / 1280f * 0.7f;
        }


        [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        private static unsafe void LogCustom(int logLevel, sbyte* text, sbyte* args)
        {
        }

        static Vector2 GetSavedWindowSize()
        {
            if (File.Exists(FileHelper.PrefsFilePath))
            {
                string prefs = File.ReadAllText(FileHelper.PrefsFilePath);
                if (!string.IsNullOrEmpty(prefs))
                {
                    if (prefs[0] == '0')
                    {
                        return Settings.ScreenSizeSmall;
                    }
                    else if (prefs[0] == '1')
                    {
                        return Settings.ScreenSizeBig;
                    }
                }
            }
            return Settings.ScreenSizeSmall;
        }

        static void SaveWindowSize()
        {
            Directory.CreateDirectory(FileHelper.AppDataPath);
            bool isBigWindow = Raylib.GetScreenWidth() > Settings.ScreenSizeSmall.X;
            File.WriteAllText(FileHelper.PrefsFilePath, isBigWindow ? "1" : "0");
        }
    }
}