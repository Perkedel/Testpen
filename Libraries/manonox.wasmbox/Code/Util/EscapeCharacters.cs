using System.Globalization;
using System.Text;

namespace WasmBox.Util.EscapeCharacters;

/// <summary>
///   Extension methods for <see cref="string"/>.
/// </summary>
public static class StringExtensions
{
    public static string Escape( this string s )
    {
        var sb = new StringBuilder();
        foreach ( var c in s )
        {
            switch ( c )
            {
                case '\n':
                    sb.Append( @"\n" );
                    break;
                case '\t':
                    sb.Append( @"\t" );
                    break;
                case '\r':
                    sb.Append( @"\r" );
                    break;
                case '\f':
                    sb.Append( @"\f" );
                    break;
                case '\b':
                    sb.Append( @"\b" );
                    break;
                case '\\':
                    sb.Append( @"\\" );
                    break;
                case '"':
                    sb.Append( @"\""" );
                    break;
                case '\0':
                    sb.Append( @"\0" );
                    break;
                case '\a':
                    sb.Append( @"\a" );
                    break;
                case '\v':
                    sb.Append( @"\v" );
                    break;
                default:
                    if ( c < ' ' || c > '~' )
                    {
                        if (c <= 0xff)
                        {
                            sb.AppendFormat(CultureInfo.InvariantCulture, @"\x{0:x2}", (int)c);
                        }
                        else
                        {
                            sb.AppendFormat(CultureInfo.InvariantCulture, @"\u{0:x4}", (int)c);
                        }
                    }
                    else
                    {
                        sb.Append( c );
                    }
                    break;
            }
        }
        return sb.ToString();
    }

    public static string UnEscape( this string s )
    {
        var sb = new StringBuilder();
        for ( var i = 0; i < s.Length; i++ )
        {
            if ( s[i] != '\\' )
            {
                sb.Append( s[i] );
                continue;
            }

            if (i + 1 >= s.Length) throw new FormatException("Invalid escape sequence: trailing backslash.");

            i++; // Consume backslash
            switch ( s[i] )
            {
                case 'n': sb.Append( '\n' ); break;
                case 't': sb.Append( '\t' ); break;
                case 'r': sb.Append( '\r' ); break;
                case 'f': sb.Append( '\f' ); break;
                case 'b': sb.Append( '\b' ); break;
                case 'a': sb.Append( '\a' ); break;
                case 'v': sb.Append( '\v' ); break;
                case '0': sb.Append( '\0' ); break;
                case '\\': sb.Append( '\\' ); break;
                case '"': sb.Append( '"' ); break;
                case 'c':
                    if (i + 1 >= s.Length) throw new FormatException("Invalid escape sequence: \\c at end of string.");
                    i++;
                    sb.Append((char)(s[i] & 0x1f));
                    break;
                case 'x':
                    var hexEnd = i + 1;
                    while (hexEnd < s.Length && hexEnd < i + 5 && "0123456789abcdefABCDEF".IndexOf(s[hexEnd]) != -1)
                    {
                        hexEnd++;
                    }
                    if (hexEnd == i + 1) throw new FormatException("Invalid escape sequence: \\x with no hex digits.");
                    
                    var hexValue = s.Substring(i + 1, hexEnd - (i + 1));
                    sb.Append((char)int.Parse(hexValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                    i = hexEnd - 1;
                    break;
                case 'u':
                    if (i + 4 >= s.Length) throw new FormatException("Invalid escape sequence: \\u with less than 4 hex digits.");
                    var uhex = s.Substring(i + 1, 4);
                    for(var j=0; j<4; j++) {
                        if ("0123456789abcdefABCDEF".IndexOf(uhex[j]) == -1) {
                            throw new FormatException("Invalid escape sequence: \\u with non-hex characters.");
                        }
                    }
                    sb.Append((char)ushort.Parse(uhex, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                    i += 4;
                    break;
                default:
                    throw new FormatException($"Invalid escape character: '{s[i]}'");
            }
        }

        return sb.ToString();
    }
}