using System;
using System.Numerics;
using System.Collections.Generic;
using Raylib_cs;
using static ChessChallenge.Application.UI.ComponentUI;
using static ChessChallenge.Application.UI.LayoutComponents;

namespace ChessChallenge.Application.UI
{
    class BasicComponents
    {
        public static void Text(string text, int fontSize = 30, int padding = 5, Align align = Align.TopLeft)
        {
            AlignH alignH = HorizontalAlignOf(align);
            AlignV alignV = VerticalAlignOf(align);
            Vector2 boundSize = Raylib.MeasureTextEx(UIHelper.font, text, UIHelper.ScaleInt(fontSize), 1);
            Padding(
                padding: padding,
                child: () => AlignBox(
                    width: (int)boundSize.X,
                    height: (int)boundSize.Y,
                    align: align,
                    child: () => DrawClipped(() =>
                    {
                        Raylib.BeginShaderMode(UIHelper.shader);
                        Raylib.DrawTextEx(UIHelper.fontSdf, text, Vector2.Zero, UIHelper.ScaleInt(fontSize), 1, Color.WHITE);
                        Raylib.EndShaderMode();
                    })
                )
            );
        }
        public static void FittedText(string text, int fontSize, int padding = 5, Align align = Align.TopLeft)
        {
            Padding(
                padding: padding,
                child: () =>
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 boundSize = Raylib.MeasureTextEx(UIHelper.font, text, UIHelper.ScaleInt(fontSize), 1);
                        float scaleFactor = Math.Min(CurrentWidth / boundSize.X, CurrentHeight / boundSize.Y) * 0.98f;
                        if (scaleFactor >= 1)
                        {
                            break;
                        }
                        fontSize = (int)(fontSize * scaleFactor);
                    }
                    Text(text, fontSize, padding, align);
                }
            );
        }

        public static void Button(Action child, Action onClick)
        {

            Color normalCol = new(40, 40, 40, 255);
            Color hoverCol = new(3, 173, 252, 255);
            Color pressCol = new(2, 119, 173, 255);
            bool hover = IsMouseWithinCurrent();
            bool pressed = hover && Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT);
            Color col = hover ? (pressed ? pressCol : hoverCol) : normalCol;
            Color textCol = IsMouseWithinCurrent() ? Color.WHITE : new Color(180, 180, 180, 255);
            bool pressedThisFrame = pressed && Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT);
            Container(child, col);
            if (pressedThisFrame)
            {
                onClick();
            }
        }

        public static void TextButton(string text, Action onClick, int fontSize = 30, int padding = 5, Align align = Align.Center)
        {
            Button(
                () => Text(text, fontSize, padding, align),
                onClick
            );
        }

        public static void FittedTextButton(string text, Action onClick, int fontSize = 30, int padding = 5, Align align = Align.Center)
        {
            Button(
                () => FittedText(text, fontSize, padding, align),
                onClick
            );
        }

        public static void LabeledContainer(string label, Action child)
        {
            Column(
                heightAt: i => i == 0 ? 30 : -1,
                children: new() {
                    () => Text(label, 30, align: Align.CenterLeft),
                    child
                }
            );
        }

        public static void Select(List<Action> children, int childHeight, ref int scrollPosition)
        {
            if (IsMouseWithinCurrent())
            {
                scrollPosition -= (int)(Raylib.GetMouseWheelMoveV().Y * 20);
                scrollPosition = Math.Clamp(scrollPosition, 0, Math.Max(0, children.Count * childHeight - CurrentHeight));
            }

            int startIndex = scrollPosition / childHeight;
            int endIndex = (scrollPosition + CurrentHeight) / childHeight + 1;
            startIndex = Math.Clamp(startIndex, 0, children.Count);
            endIndex = Math.Clamp(endIndex, 0, children.Count);
            for (int i = startIndex; i < endIndex; i++)
            {
                Action child = children[i];
                int itemTop = i * childHeight - scrollPosition;
                Rectangle itemRect = new(0, itemTop, CurrentWidth, childHeight);

                WithinRelativeArea(itemRect, child);
            }
        }

        public static void TextArea(string text, int fontSize, ref int scrollPosition, int padding = 5)
        {
            Vector2 boundSize = Raylib.MeasureTextEx(UIHelper.font, text, UIHelper.ScaleInt(fontSize), 1) + 2 * new Vector2(padding, padding);
            if (IsMouseWithinCurrent())
            {
                scrollPosition -= (int)(Raylib.GetMouseWheelMoveV().Y * 20);
                scrollPosition = Math.Clamp(scrollPosition, 0, Math.Max(0, (int)boundSize.Y - CurrentHeight));
            }
            int scrollPos = scrollPosition;
            DrawClipped(() =>
            {
                Raylib.BeginShaderMode(UIHelper.shader);
                Raylib.DrawTextEx(UIHelper.fontSdf, text, new(padding, padding - scrollPos), UIHelper.ScaleInt(fontSize), 1, Color.WHITE);
                Raylib.EndShaderMode();
            });
        }
    }
}