using System;
using System.Numerics;
using Raylib_cs;

namespace ChessChallenge.Application
{
    class RectangleHelper
    {
        public static Rectangle Intersection(Rectangle rectA, Rectangle rectB)
        {
            float x1 = Math.Max(rectA.x, rectB.x);
            float y1 = Math.Max(rectA.y, rectB.y);
            float x2 = Math.Min(rectA.x + rectA.width, rectB.x + rectB.width);
            float y2 = Math.Min(rectA.y + rectA.height, rectB.y + rectB.height);
            return new Rectangle(x1, y1, x2 - x1, y2 - y1);
        }
        public static bool Inside(Rectangle area, Vector2 point)
        {
            return point.X >= area.x && point.Y >= area.y && point.X < area.x + area.width && point.Y < area.y + area.height;
        }
        public static Rectangle Offset(Rectangle rect, Vector2 offset)
        {
            return new Rectangle(rect.x + offset.X, rect.y + offset.Y, rect.width, rect.height);
        }
        public static Rectangle Shrink(Rectangle rect, Vector2 shrink)
        {
            return new Rectangle(rect.x + shrink.X, rect.y + shrink.Y, rect.width - 2 * shrink.X, rect.height - 2 * shrink.Y);
        }
    }
}