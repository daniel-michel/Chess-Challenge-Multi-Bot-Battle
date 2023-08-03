using System;
using System.Numerics;
using System.Collections.Generic;
using Raylib_cs;
using static ChessChallenge.Application.RectangleHelper;

namespace ChessChallenge.Application.UI
{
    class ComponentUI
    {
        static readonly Stack<Rectangle> componentAreaStack = new();
        static readonly Stack<Rectangle> componentClipStack = new();

        public static int CurrentWidth { get => (int)componentAreaStack.Peek().width; }
        public static int CurrentHeight { get => (int)componentAreaStack.Peek().height; }
        public static int CurrentX { get => (int)componentAreaStack.Peek().x; }
        public static int CurrentY { get => (int)componentAreaStack.Peek().y; }
        public static Vector2 CurrentPosition { get => new(CurrentX, CurrentY); }

        private static Camera2D drawCamera = new();

        public static Vector2 GetRelativeMousePosition()
        {
            return Raylib.GetMousePosition() - CurrentPosition;
        }

        public static bool IsMouseWithinCurrent()
        {
            return Inside(componentClipStack.Peek(), Raylib.GetMousePosition());
        }

        public static void Start()
        {
            Rectangle renderArea = new(0, 0, Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
            componentAreaStack.Clear();
            componentClipStack.Clear();
            componentAreaStack.Push(renderArea);
            componentClipStack.Push(renderArea);
        }

        public static void WithinRelativeArea(Rectangle area, Action child)
        {
            Rectangle previousArea = componentAreaStack.Peek();
            Vector2 previousPosition = new(previousArea.x, previousArea.y);
            Rectangle componentArea = Offset(area, previousPosition);
            Rectangle clippedArea = Intersection(previousArea, componentArea);
            componentAreaStack.Push(componentArea);
            componentClipStack.Push(clippedArea);
            child();
            componentAreaStack.Pop();
            componentClipStack.Pop();
        }
        public static void WithinArea(Rectangle area, Action child)
        {
            Rectangle previousArea = componentAreaStack.Peek();
            Rectangle clippedArea = Intersection(previousArea, area);
            componentAreaStack.Push(area);
            componentClipStack.Push(clippedArea);
            child();
            componentAreaStack.Pop();
            componentClipStack.Pop();
        }

        public static void DrawClipped(Action draw)
        {
            drawCamera.zoom = 1;
            drawCamera.offset = CurrentPosition;
            Raylib.BeginMode2D(drawCamera);
            Rectangle clipArea = componentClipStack.Peek();
            Raylib.BeginScissorMode((int)clipArea.x, (int)clipArea.y, (int)clipArea.width, (int)clipArea.height);
            draw();
            Raylib.EndScissorMode();
            Raylib.EndMode2D();
        }
    }
}