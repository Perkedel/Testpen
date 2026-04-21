using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using WasmBox.Pixie.Markup;

namespace WasmBox.Pixie.Terminal.Devices {
    /// <summary>
    /// A style manager implementation that uses ANSI control codes to
    /// style output.
    /// </summary>
    public sealed class AnsiStyleManager : StyleManager {
        /// <summary>
        /// Creates an ANSI style manager from a text writer.
        /// </summary>
        /// <param name="writer">A text writer to which control sequences are written.</param>
        // public AnsiStyleManager(TextWriter writer)
        //     : this(
        //         writer,
        //         ConsoleStyle.ToPixieColor(Console.ForegroundColor, Colors.White),
        //         ConsoleStyle.ToPixieColor(Console.BackgroundColor, Colors.Black))
        // { }

        /// <summary>
        /// Creates an ANSI style manager from a text writer, a default foreground
        /// and a default background color.
        /// </summary>
        /// <param name="writer">A text writer to which control sequences are written.</param>
        /// <param name="defaultForegroundColor">The default foreground color.</param>
        /// <param name="defaultBackgroundColor">The default background color.</param>
        public AnsiStyleManager(
            TextWriter writer,
            Color defaultForegroundColor,
            Color defaultBackgroundColor) {
            this.Writer = writer;
            this.defaultForegroundColor = defaultForegroundColor;
            this.defaultBackgroundColor = defaultBackgroundColor;

            this.styleStack = new Stack<AnsiStyle>();
            this.styleStack.Push(
                new AnsiStyle(
                    default( Color? ),
                    default( Color? ),
                    TextDecoration.None));
        }

        private Stack<AnsiStyle> styleStack;

        private Color defaultForegroundColor;
        private Color defaultBackgroundColor;

        /// <summary>
        /// Gets the writer to which ANSI control codes are written.
        /// </summary>
        /// <returns>A text writer.</returns>
        public TextWriter Writer { get; private set; }

        private AnsiStyle CurrentStyle => styleStack.Peek();

        /// <inheritdoc/>
        public override void PushForegroundColor(Color color) {
            var curStyle = CurrentStyle;
            PushStyle(
                new AnsiStyle(
                    Over(color, curStyle.ForegroundColor, defaultForegroundColor),
                    curStyle.BackgroundColor,
                    curStyle.Decoration));
        }

        /// <inheritdoc/>
        public override void PushBackgroundColor(Color color) {
            var curStyle = CurrentStyle;
            PushStyle(
                new AnsiStyle(
                    curStyle.ForegroundColor,
                    Over(color, curStyle.BackgroundColor, defaultBackgroundColor),
                    curStyle.Decoration));
        }

        /// <inheritdoc/>
        public override void PushDecoration(
            TextDecoration decoration,
            Func<TextDecoration, TextDecoration, TextDecoration> updateDecoration) {
            var curStyle = CurrentStyle;
            PushStyle(
                new AnsiStyle(
                    curStyle.ForegroundColor,
                    curStyle.BackgroundColor,
                    updateDecoration(curStyle.Decoration, decoration)));
        }

        private void PushStyle(AnsiStyle style) {
            style.Apply(Writer, CurrentStyle);
            styleStack.Push(style);
        }

        /// <inheritdoc/>
        public override void PopStyle() {
            var popped = styleStack.Pop();
            CurrentStyle.Apply(Writer, popped);
        }

        private static Color? Over(
            Color top,
			Color? bottom,
            Color defaultBottom) {
            if (top.Alpha == 0.0) {
                return bottom;
            }
            else {
                return top.Over(bottom.GetValueOrDefault(defaultBottom));
            }
        }
    }

    internal enum AnsiControlCode : byte {
        Reset = 0,
        Bold = 1,
        Faint = 2,
        Italic = 3,
        Underline = 4,
        BlinkSlow = 5,
        BlinkFast = 6,
        Strikethrough = 9,

        ForegroundBlack = 30,
        ForegroundRed = 31,
        ForegroundGreen = 32,
        ForegroundYellow = 33,
        ForegroundBlue = 34,
        ForegroundMagenta = 35,
        ForegroundCyan = 36,
        ForegroundWhite = 37,

        BackgroundBlack = 40,
        BackgroundRed = 41,
        BackgroundGreen = 42,
        BackgroundYellow = 43,
        BackgroundBlue = 44,
        BackgroundMagenta = 45,
        BackgroundCyan = 46,
        BackgroundWhite = 47,
    }

    internal sealed class AnsiStyle {
        public AnsiStyle(
			Color? foregroundColor,
			Color? backgroundColor,
            TextDecoration decoration) {
            this.ForegroundColor = foregroundColor;
            this.BackgroundColor = backgroundColor;
            this.Decoration = decoration;
        }

        public Color? ForegroundColor { get; private set; }

        public Color? BackgroundColor { get; private set; }

        public TextDecoration Decoration { get; private set; }

        private void WriteControlSequence(
            TextWriter writer,
            IEnumerable<AnsiControlCode> commands) {
            writer.Write("\x1b[");
            bool first = false;
            foreach (var item in commands) {
                if (first) {
                    first = false;
                }
                else {
                    writer.Write(';');
                }
                writer.Write((int)item);
            }
            writer.Write('m');
        }

        private void Apply(TextWriter writer) {
            var commands = new List<AnsiControlCode>();

            // Always reset first.
            commands.Add(AnsiControlCode.Reset);

            // Write the foreground color.
            if (ForegroundColor.HasValue) {
                bool isFaint;
                commands.Add(ToForegroundColor(
                    ConsoleStyle.ToConsoleColor(ForegroundColor.Value),
                    out isFaint));

                if (isFaint) {
                    commands.Add(AnsiControlCode.Faint);
                }
            }

            // Write the background color.
            if (BackgroundColor.HasValue) {
                commands.Add(
                    ToBackgroundColor(
                        ConsoleStyle.ToConsoleColor(BackgroundColor.Value)));
            }

            // Apply decorations
            if (HasDecoration(TextDecoration.Bold)) {
                commands.Add(AnsiControlCode.Bold);
            }
            if (HasDecoration(TextDecoration.Italic)) {
                commands.Add(AnsiControlCode.Italic);
            }
            if (HasDecoration(TextDecoration.Underline)) {
                commands.Add(AnsiControlCode.Underline);
            }
            if (HasDecoration(TextDecoration.Strikethrough)) {
                commands.Add(AnsiControlCode.Strikethrough);
            }

            WriteControlSequence(writer, commands);
        }

        private bool HasDecoration(TextDecoration decor) {
            return (Decoration & decor) == decor;
        }

        /// <summary>
        /// Applies this style, given a previous style.
        /// </summary>
        public void Apply(TextWriter writer, AnsiStyle style) {
            if (Decoration != style.Decoration
                || !QuantizedColorEquals(ForegroundColor, style.ForegroundColor)
                || !QuantizedColorEquals(BackgroundColor, style.BackgroundColor)) {
                Apply(writer);
            }
        }

        private static AnsiControlCode ToForegroundColor(int color, out bool isFaint) {
            switch (color) {
                case 0:
                    isFaint = false;
                    return AnsiControlCode.ForegroundBlack;
                case 9:
                    isFaint = false;
                    return AnsiControlCode.ForegroundBlue;
                case 11:
                    isFaint = false;
                    return AnsiControlCode.ForegroundCyan;
                case 10:
                    isFaint = false;
                    return AnsiControlCode.ForegroundGreen;
                case 13:
                    isFaint = false;
                    return AnsiControlCode.ForegroundMagenta;
                case 12:
                    isFaint = false;
                    return AnsiControlCode.ForegroundRed;
                case 15:
                    isFaint = false;
                    return AnsiControlCode.ForegroundWhite;
                case 14:
                    isFaint = false;
                    return AnsiControlCode.ForegroundYellow;

                case 7:
                    isFaint = true;
                    return AnsiControlCode.ForegroundWhite;
                case 1:
                    isFaint = true;
                    return AnsiControlCode.ForegroundBlue;
                case 3:
                    isFaint = true;
                    return AnsiControlCode.ForegroundCyan;
                case 8:
                    isFaint = true;
                    return AnsiControlCode.ForegroundBlack;
                case 2:
                    isFaint = true;
                    return AnsiControlCode.ForegroundGreen;
                case 5:
                    isFaint = true;
                    return AnsiControlCode.ForegroundMagenta;
                case 4:
                    isFaint = true;
                    return AnsiControlCode.ForegroundRed;
                case 6:
                    isFaint = true;
                    return AnsiControlCode.ForegroundYellow;

                default:
                    throw new NotSupportedException("Unsupported color " + color);
            }
        }

        private static AnsiControlCode ToBackgroundColor(int color) {
            bool isFaint;
            return ToForegroundColor(color, out isFaint) + 10;
        }

        private static bool QuantizedColorEquals(Color first, Color second) {
            return ConsoleStyle.ToConsoleColor(first) == ConsoleStyle.ToConsoleColor(second);
        }

        private static bool QuantizedColorEquals(
			Color? first,
			Color? second ) {
            if (first.HasValue) {
                if (second.HasValue) {
                    return QuantizedColorEquals(first.Value, second.Value);
                }
                else {
                    return false;
                }
            }
            else {
                return !second.HasValue;
            }
        }
    }
}