/*
    Copyright 2022 Jeffrey Sharp

    Permission to use, copy, modify, and distribute this software for any
    purpose with or without fee is hereby granted, provided that the above
    copyright notice and this permission notice appear in all copies.

    THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
    WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
    MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
    ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
    WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
    ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
    OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
*/

using System.Text;

namespace WasmBox.Util.Unindent;

/// <summary>
///   Extension methods for <see cref="string"/>.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    ///   Removes indentation from the specified string.
    /// </summary>
    /// <param name="s">
    ///   The string from which to remove indentation.
    /// </param>
    /// <param name="tabStop">
    ///   The count of columns between tab stops.  Must be a positive number.
    /// </param>
    /// <returns>
    ///   <para>
    ///     A string with content of <paramref name="s"/>, but unindented: the
    ///     leading space, if any, common to all non-blank lines is removed
    ///     from all lines (blank or not).  This method also removes one
    ///     leading line-ending and all trailing space from 
    ///     <paramref name="s"/>, if present.
    ///   </para>
    ///   <para>
    ///     <em>Space</em> is any mixture of the space (<c>U+0020</c>) and tab
    ///     (<c>U+0009</c>) characters.  Each tab advances to the next tab
    ///     stop. Tab stops are <paramref name="tabStop"/> spaces apart.  If a
    ///     tab in <paramref name="s"/> jumps past the indentation, this method
    ///     replaces the tab with spaces to preserve alignment.
    ///   </para>
    ///   <para>
    ///     Lines end via any mixture of the carriage return (<c>U+000D</c>)
    ///     and line feed (<c>U+000A</c>) characters.  A line is <em>blank</em>
    ///     if it contains no characters other than spaces or tabs.
    ///   </para>
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="s"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///   <paramref name="tabStop"/> is zero or negative.
    /// </exception>
    public static string Unindent(this string s, int tabStop = 8)
    {
        if (s is null)
            throw new ArgumentNullException(nameof(s));
        if (tabStop < 1)
            throw new ArgumentOutOfRangeException(nameof(tabStop));

        // Ignore one leading EOL and any trailing space
        var start = s.LengthOfLeadingEol();
        var limit = s.LengthExcludingTrailingSpace();

        // Compute indent width and count
        var (indent, count) = s.GetIndentWidthAndCount(start, limit, tabStop);

        // Shortcut for simple cases
        if (indent == 0 || count == 0)
            return s.Substring(start, limit - start);

        // Build unindented string
        return s.UnindentCore(start, limit, indent, tabStop);
    }

    private static (int, int) GetIndentWidthAndCount(
        this string s,
        int         index,
        int         limit,
        int         tabStop)
    {
        var column = 0;             // column offset from BOL
        var indent = int.MaxValue;  // width of one indent
        var count  = 0;             // count of indents

        while (index < limit)
        {
            switch (s[index++])
            {
                case ' ':
                    // Advance to next column
                    column++;
                    continue;

                case '\t':
                    // Advance to next tab stop
                    column += tabStop - column % tabStop;
                    continue;

                case '\r':
                case '\n':
                    // Empty line => do not consider indent
                    column = 0;
                    continue;

                default:
                    // Non-empty line => ident found
                    indent = Math.Min(indent, column);
                    count++;

                    // Skip to next potential indent
                    index  = s.IndexOfNextBol(index, limit);
                    column = 0;
                    continue;
            }
        }

        return (indent, count);
    }

    private static string UnindentCore(
        this string s,
        int         index,
        int         limit,
        int         indent,
        int         tabStop)
    {
        var result = new StringBuilder(limit - index);

        do
        {
            index = s.SkipIndent          (index, indent, tabStop, result);
            index = s.CopyUntilNextIndent (index, limit,           result);
        }
        while (index < limit);

        return result.ToString();
    }

    private static int SkipIndent(
        this string   s,
        int           index,
        int           indent,
        int           tabStop,
        StringBuilder result)
    {
        var column = 0; // column offset from BOL

        for (;;)
        {
            switch (s[index])
            {
                case ' ':
                    // Consume character
                    index++;

                    // Advance to next column
                    column++;

                    // Check if more indent remains
                    if (column < indent)
                        continue;

                    // Indent has been skipped
                    return index;

                case '\t':
                    // Consume character
                    index++;

                    // Advance to next tab stop
                    column += tabStop - column % tabStop;

                    // Check if more indent remains
                    if (column < indent)
                        continue;

                    // Add make-up spaces if tab jumped past indent
                    for (var i = column - indent; i > 0; i--)
                        result.Append(' ');

                    // Indent has been skipped
                    return index;

                default:
                    // This must be an early EOL, due to checks elsewhere.
                    // Consider any indent as having been skipped.
                    return index;
            }
        }
    }

    private static int CopyUntilNextIndent(
        this string   s,
        int           index,
        int           limit,
        StringBuilder result)
    {
        var bol = s.IndexOfNextBol(index, limit);

        result.Append(s, index, bol - index);

        return bol;
    }

    private static int LengthOfLeadingEol(this string s)
    {
        if (s.Length == 0)
            return 0;

        switch (s[0])
        {
            case '\r': break; // 1 or 2
            case '\n': return 1;
            default:   return 0;
        }

        if (s.Length == 1)
            return 1;

        switch (s[1])
        {
            case '\n': return 2;
            default:   return 1;
        }
    }

    private static int LengthExcludingTrailingSpace(this string s)
    {
        var index = s.Length;

        while (--index >= 0)
            if (!s[index].IsSpace())
                break;

        return index + 1;
    }

    private static int IndexOfNextEol(this string s, int index, int limit)
    {
        for (; index < limit; index++)
            if (s[index].IsEol())
                return index;

        return limit;
    }

    private static int IndexOfNextBol(this string s, int index, int limit)
    {
        index = s.IndexOfNextEol(index, limit);

        for (; index < limit; index++)
            if (!s[index].IsEol())
                return index;

        return limit;
    }

    private static bool IsSpace(this char c)
    {
        return c == ' ' || c == '\t';
    }

    private static bool IsEol(this char c)
    {
        return c == '\r' || c == '\n';
    }
}