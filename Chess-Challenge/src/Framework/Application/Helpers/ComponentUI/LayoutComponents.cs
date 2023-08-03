using System;
using System.Linq;
using System.Collections.Generic;
using static ChessChallenge.Application.UI.ComponentUI;
using static ChessChallenge.Application.RectangleHelper;
using Raylib_cs;

namespace ChessChallenge.Application.UI
{
    class LayoutComponents
    {
        public enum Align
        {
            TopLeft,
            TopCenter,
            TopRight,
            CenterLeft,
            Center,
            CenterRight,
            BottomLeft,
            BottomCenter,
            BottomRight
        }
        public enum AlignH
        {
            Left,
            Center,
            Right
        }
        public enum AlignV
        {
            Top,
            Center,
            Bottom
        }
        public static AlignH HorizontalAlignOf(Align align)
            => align switch
            {
                Align.TopLeft => AlignH.Left,
                Align.TopCenter => AlignH.Center,
                Align.TopRight => AlignH.Right,
                Align.CenterLeft => AlignH.Left,
                Align.Center => AlignH.Center,
                Align.CenterRight => AlignH.Right,
                Align.BottomLeft => AlignH.Left,
                Align.BottomCenter => AlignH.Center,
                Align.BottomRight => AlignH.Right,
                _ => throw new Exception("Invalid Align")
            };
        public static AlignV VerticalAlignOf(Align align)
            => align switch
            {
                Align.TopLeft => AlignV.Top,
                Align.TopCenter => AlignV.Top,
                Align.TopRight => AlignV.Top,
                Align.CenterLeft => AlignV.Center,
                Align.Center => AlignV.Center,
                Align.CenterRight => AlignV.Center,
                Align.BottomLeft => AlignV.Bottom,
                Align.BottomCenter => AlignV.Bottom,
                Align.BottomRight => AlignV.Bottom,
                _ => throw new Exception("Invalid Align")
            };

        public static void Column(List<Action> children, Func<int, int> heightAt, int gap = 0)
        {
            int currentY = 0;
            for (int i = 0; i < children.Count; i++)
            {
                int height = heightAt(i);
                if (height < 0)
                {
                    int count = children.Count - i;
                    height = CalculateUniformSize(CurrentHeight - currentY, count, gap);
                }
                WithinRelativeArea(new Rectangle(0, currentY, CurrentWidth, height), children[i]);
                currentY += height + gap;
            }
        }
        public static void Column(List<Action> children, List<float> relativeSizes, int gap = 0)
        {
            Column(children, RelativeSizeCalculator(relativeSizes, CurrentHeight, gap), gap);
        }
        public static void Column(List<Action> children, int gap = 0)
        {
            int componentHeight = CalculateUniformSize(CurrentHeight, children.Count, gap);
            Column(children, i => componentHeight, gap);
        }
        public static void Row(List<Action> children, Func<int, int> widthAt, int gap = 0)
        {
            int currentX = 0;
            for (int i = 0; i < children.Count; i++)
            {
                int width = widthAt(i);
                if (width < 0)
                {
                    int count = children.Count - i;
                    width = CalculateUniformSize(CurrentWidth - currentX, count, gap);
                }
                WithinRelativeArea(new Rectangle(currentX, 0, width, CurrentHeight), children[i]);
                currentX += width + gap;
            }
        }
        public static void Row(List<Action> children, List<float> relativeSizes, int gap = 0)
        {
            Row(children, RelativeSizeCalculator(relativeSizes, CurrentWidth, gap), gap);
        }
        public static void Row(List<Action> children, int gap = 0)
        {
            int componentWidth = CalculateUniformSize(CurrentWidth, children.Count, gap);
            Row(children, i => componentWidth, gap);
        }
        private static int CalculateUniformSize(int availableSpace, int componentCount, int gap)
        {
            return (availableSpace - (componentCount - 1) * gap) / componentCount;
        }
        private static Func<int, int> RelativeSizeCalculator(List<float> relativeSizes, int availableSpace, int gap)
        {
            float total = relativeSizes.Aggregate((a, b) => a + b);
            float factor = (availableSpace - (relativeSizes.Count - 1) * gap) / total;
            return i => i < relativeSizes.Count ? (int)(relativeSizes[i] * factor) : -1;
        }

        public static void Padding(Action child, int padding = 10)
        {
            WithinRelativeArea(Shrink(new Rectangle(0, 0, CurrentWidth, CurrentHeight), new(padding, padding)), child);
        }
        public static void Container(Action child, Color color, int padding = 0)
        {
            DrawClipped(() => Raylib.DrawRectangle(0, 0, CurrentWidth, CurrentHeight, color));
            Padding(child, padding);
        }
        public static void AlignBox(int width, int height, Align align, Action child)
        {
            AlignH alignH = HorizontalAlignOf(align);
            AlignV alignV = VerticalAlignOf(align);
            int offsetX = alignH == AlignH.Left ? 0 : (alignH == AlignH.Center ? (CurrentWidth - width) / 2 : CurrentWidth - width);
            int offsetY = alignV == AlignV.Top ? 0 : (alignV == AlignV.Center ? (CurrentHeight - height) / 2 : CurrentHeight - height);
            WithinRelativeArea(new Rectangle(offsetX, offsetY, width, height), child);
        }
    }
}