using EasyJson.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace EasyJson;

public static class Utf8JsonReaderExtensions
{
    /// <summary>
    /// Represents a method that reads a JSON property from the specified reader using the provided property name.
    /// </summary>
    /// <remarks>
    /// This delegate is typically used in custom JSON deserialization scenarios to process specific
    /// properties as they are encountered in the JSON payload. The implementation is responsible for advancing the
    /// reader as appropriate.
    /// </remarks>
    /// <param name="reader">A reference to the <see cref="Utf8JsonReader"/> used to read the JSON data. The reader should be positioned at
    /// the property value to be read.</param>
    /// <param name="propertyName">A read-only span of bytes containing the name of the property to be read from the JSON data.</param>
    public delegate void ReadPropertyDelegate(ref Utf8JsonReader reader, ReadOnlySpan<byte> propertyName);

    /// <summary>
    /// Represents a method that processes an entry in a JSON array during deserialization.
    /// </summary>
    /// <remarks>
    /// Use this delegate to define custom logic for reading or handling individual elements when
    /// deserializing a JSON array. The <paramref name="reader"/> advances as the entry is read. The delegate does not
    /// return a value; any results should be handled within the delegate implementation.
    /// </remarks>
    /// <param name="reader">A reference to the <see cref="Utf8JsonReader"/> positioned at the start of the array entry to be read.</param>
    /// <param name="index">The zero-based index of the current array entry being processed.</param>
    public delegate void ReadArrayEntryDelegate(ref Utf8JsonReader reader, int index);


    /// <summary>
    /// Verifies that the current token of the specified <see cref="Utf8JsonReader"/> matches the expected <see
    /// cref="JsonTokenType"/>. Throws an exception if the token does not match.
    /// </summary>
    /// <remarks>
    /// Use this method to enforce that the reader is positioned at a specific token type before
    /// proceeding with further JSON parsing. If the token does not match, a <see cref="JsonReadException"/> is thrown
    /// to indicate an unexpected token.
    /// </remarks>
    /// <param name="reader">The <see cref="Utf8JsonReader"/> instance to check. The reader must be positioned at a token to validate.</param>
    /// <param name="tokenType">The expected <see cref="JsonTokenType"/> to assert against the reader's current token.</param>
    public static void AssertToken(this ref Utf8JsonReader reader, JsonTokenType tokenType)
    {
        if(reader.TokenType != tokenType)
            throw JsonReadException.UnexpectedToken(reader, tokenType);
    }

    /// <summary>
    /// Verifies that the current token of the specified <see cref="Utf8JsonReader"/> matches the expected <see
    /// cref="JsonTokenType"/>. Throws an exception if the token does not match.
    /// </summary>
    /// <remarks>
    /// Use this method to enforce that the reader is positioned at a specific token type before
    /// proceeding with further JSON parsing. If the token does not match, a <see cref="JsonReadException"/> is thrown
    /// to indicate an unexpected token.
    /// </remarks>
    /// <param name="reader">The <see cref="Utf8JsonReader"/> instance to check. The reader must be positioned at a token to validate.</param>
    /// <param name="tokenTypes">Enumerable of expected <see cref="JsonTokenType"/>s to assert against the reader's current token.</param>
    public static void AssertToken(this ref Utf8JsonReader reader, params IEnumerable<JsonTokenType> tokenTypes)
    {
        if(!tokenTypes.Contains(reader.TokenType))
            throw JsonReadException.UnexpectedToken(reader, tokenTypes);
    }

    /// <summary>
    /// Advances the reader to the next JSON token and returns its type, or throws an exception if no more tokens are
    /// available.
    /// </summary>
    /// <remarks>
    /// This method throws an exception if the reader cannot advance because there are no more tokens
    /// to read. Use this method when you expect additional tokens and want to enforce strict parsing.
    /// </remarks>
    /// <param name="reader">The reader to advance. Must be positioned within a valid JSON payload.</param>
    /// <returns>The type of the next JSON token in the input data.</returns>
    public static JsonTokenType ReadOrThrow(this ref Utf8JsonReader reader)
    {
        if(!reader.Read())
            throw JsonReadException.NothingToRead(reader);
        return reader.TokenType;
    }

    /// <summary>
    /// Reads the next JSON token from the specified reader and ensures it matches one of the allowed token types.
    /// </summary>
    /// <remarks>
    /// This method advances the reader and validates the token type. If the token type is not in the
    /// allowed set, a <see cref="JsonReadException"/> is thrown. This is useful for enforcing strict token expectations
    /// when parsing JSON data.
    /// </remarks>
    /// <param name="reader">The reader from which to read the next JSON token. The reader must be positioned at a valid location for
    /// reading.</param>
    /// <param name="allowedTokenTypes">A set of token types that are considered valid. The method throws an exception if the next token does not match
    /// any of these types.</param>
    /// <returns>The type of the JSON token that was read. The returned value is guaranteed to be one of the allowed token types.</returns>
    public static JsonTokenType ReadTokenOrThrow(this ref Utf8JsonReader reader, params IEnumerable<JsonTokenType> allowedTokenTypes)
    {
        var token = reader.ReadOrThrow();
        if(!allowedTokenTypes.Contains(token))
            throw JsonReadException.UnexpectedToken(reader, allowedTokenTypes);
        return token;
    }

    #region simple read methods

    /// <summary>
    /// Reads a unsigned byte value from next JSON token in the reader.
    /// </summary>
    /// <remarks>
    /// Throws an exception if read token is not a number or if the value cannot be represented as an unsigned byte.
    /// </remarks>
    /// <param name="reader">A reference to the <see cref="Utf8JsonReader"/>.</param>
    /// <returns>Unsigned byte value represented by the read JSON number token.</returns>
    public static byte ReadByte(this ref Utf8JsonReader reader)
    {
        reader.ReadTokenOrThrow(JsonTokenType.Number);
        return reader.GetByte();
    }

    /// <summary>
    /// Reads a 16-bit signed integer value from next JSON token in the reader.
    /// </summary>
    /// <remarks>
    /// Throws an exception if read token is not a number or if the value cannot be represented as a 16-bit signed integer.
    /// </remarks>
    /// <param name="reader">A reference to the <see cref="Utf8JsonReader"/>.</param>
    /// <returns>The 16-bit signed integer value represented by the read JSON number token.</returns>
    public static short ReadInt16(this ref Utf8JsonReader reader)
    {
        reader.ReadTokenOrThrow(JsonTokenType.Number);
        return reader.GetInt16();
    }

    /// <summary>
    /// Reads a 32-bit signed integer value from next JSON token in the reader.
    /// </summary>
    /// <remarks>
    /// Throws an exception if read token is not a number or if the value cannot be represented as a 32-bit signed integer.
    /// </remarks>
    /// <param name="reader">A reference to the <see cref="Utf8JsonReader"/>.</param>
    /// <returns>The 32-bit signed integer value represented by the read JSON number token.</returns>
    public static int ReadInt32(this ref Utf8JsonReader reader)
    {
        reader.ReadTokenOrThrow(JsonTokenType.Number);
        return reader.GetInt32();
    }

    /// <summary>
    /// Reads a 64-bit signed integer value from next JSON token in the reader.
    /// </summary>
    /// <remarks>
    /// Throws an exception if read token is not a number or if the value cannot be represented as a 64-bit signed integer.
    /// </remarks>
    /// <param name="reader">A reference to the <see cref="Utf8JsonReader"/>.</param>
    /// <returns>The 64-bit signed integer value represented by the read JSON number token.</returns>
    public static long ReadInt64(this ref Utf8JsonReader reader)
    {
        reader.ReadTokenOrThrow(JsonTokenType.Number);
        return reader.GetInt64();
    }

    /// <summary>
    /// Reads a signed byte value from next JSON token in the reader.
    /// </summary>
    /// <remarks>
    /// Throws an exception if read token is not a number or if the value cannot be represented as a signed byte.
    /// </remarks>
    /// <param name="reader">A reference to the <see cref="Utf8JsonReader"/>.</param>
    /// <returns>Signed byte value represented by the read JSON number token.</returns>
    public static sbyte ReadSByte(this ref Utf8JsonReader reader)
    {
        reader.ReadTokenOrThrow(JsonTokenType.Number);
        return reader.GetSByte();
    }

    /// <summary>
    /// Reads a 16-bit unsigned integer value from next JSON token in the reader.
    /// </summary>
    /// <remarks>
    /// Throws an exception if read token is not a number or if the value cannot be represented as a 16-bit unsigned integer.
    /// </remarks>
    /// <param name="reader">A reference to the <see cref="Utf8JsonReader"/>.</param>
    /// <returns>The 16-bit unsigned integer value represented by the read JSON number token.</returns>
    public static ushort ReadUInt16(this ref Utf8JsonReader reader)
    {
        reader.ReadTokenOrThrow(JsonTokenType.Number);
        return reader.GetUInt16();
    }

    /// <summary>
    /// Reads a 32-bit unsigned integer value from next JSON token in the reader.
    /// </summary>
    /// <remarks>
    /// Throws an exception if read token is not a number or if the value cannot be represented as a 32-bit unsigned integer.
    /// </remarks>
    /// <param name="reader">A reference to the <see cref="Utf8JsonReader"/>.</param>
    /// <returns>The 32-bit unsigned integer value represented by the read JSON number token.</returns>
    public static uint ReadUInt32(this ref Utf8JsonReader reader)
    {
        reader.ReadTokenOrThrow(JsonTokenType.Number);
        return reader.GetUInt32();
    }

    /// <summary>
    /// Reads a 64-bit unsigned integer value from next JSON token in the reader.
    /// </summary>
    /// <remarks>
    /// Throws an exception if read token is not a number or if the value cannot be represented as a 64-bit unsigned integer.
    /// </remarks>
    /// <param name="reader">A reference to the <see cref="Utf8JsonReader"/>.</param>
    /// <returns>The 64-bit unsigned integer value represented by the read JSON number token.</returns>
    public static ulong ReadUInt64(this ref Utf8JsonReader reader)
    {
        reader.ReadTokenOrThrow(JsonTokenType.Number);
        return reader.GetUInt64();
    }

    /// <summary>
    /// Reads a single-precision floating-point number value from next JSON token in the reader.
    /// </summary>
    /// <remarks>
    /// Throws an exception if read token is not a number or if the value cannot be represented as a
    /// single-precision floating-point number.
    /// </remarks>
    /// <param name="reader">A reference to the <see cref="Utf8JsonReader"/>.</param>
    /// <returns>The single-precision floating-point number value represented by the read JSON number token.</returns>
    public static float ReadSingle(this ref Utf8JsonReader reader)
    {
        reader.ReadTokenOrThrow(JsonTokenType.Number);
        return reader.GetSingle();
    }

    /// <summary>
    /// Reads a double-precision floating-point number value from next JSON token in the reader.
    /// </summary>
    /// <remarks>
    /// Throws an exception if read token is not a number or if the value cannot be represented as a
    /// double-precision floating-point number.
    /// </remarks>
    /// <param name="reader">A reference to the <see cref="Utf8JsonReader"/>.</param>
    /// <returns>The double-precision floating-point number value represented by the read JSON number token.</returns>
    public static double ReadDouble(this ref Utf8JsonReader reader)
    {
        reader.ReadTokenOrThrow(JsonTokenType.Number);
        return reader.GetDouble();
    }

    /// <summary>
    /// Reads a decimal number value from next JSON token in the reader.
    /// </summary>
    /// <remarks>
    /// Throws an exception if read token is not a number or if the value cannot be represented as a decimal number.
    /// </remarks>
    /// <param name="reader">A reference to the <see cref="Utf8JsonReader"/>.</param>
    /// <returns>The decimal number value represented by the read JSON number token.</returns>
    public static decimal ReadDecimal(this ref Utf8JsonReader reader)
    {
        reader.ReadTokenOrThrow(JsonTokenType.Number);
        return reader.GetDecimal();
    }

    /// <summary>
    /// Reads a boolean value from next JSON token in the reader.
    /// </summary>
    /// <remarks>
    /// Throws an exception if read token is not a JsonTokenType.True or JsonTokenType.False.
    /// </remarks>
    /// <param name="reader">A reference to the <see cref="Utf8JsonReader"/>.</param>
    /// <returns>The boolean value represented by the read JSON token.</returns>
    public static bool ReadBoolean(this ref Utf8JsonReader reader)
    {
        reader.ReadTokenOrThrow(JsonTokenType.True, JsonTokenType.False);
        return reader.GetBoolean();
    }

    /// <summary>
    /// Reads a <see cref="DateTime"/> value from next JSON token in the reader.
    /// </summary>
    /// <remarks>
    /// Throws an exception if read token is not a <see cref="JsonTokenType.String"/>.
    /// </remarks>
    /// <param name="reader">A reference to the <see cref="Utf8JsonReader"/>.</param>
    /// <returns>The <see cref="DateTime"/> value represented by the read JSON token.</returns>
    public static DateTime ReadDateTime(this ref Utf8JsonReader reader)
    {
        reader.ReadTokenOrThrow(JsonTokenType.String);
        return reader.GetDateTime();
    }

    /// <summary>
    /// Reads a <see cref="DateTimeOffset"/> value from next JSON token in the reader.
    /// </summary>
    /// <remarks>
    /// Throws an exception if read token is not a <see cref="JsonTokenType.String"/>.
    /// </remarks>
    /// <param name="reader">A reference to the <see cref="Utf8JsonReader"/>.</param>
    /// <returns>The <see cref="DateTimeOffset"/> value represented by the read JSON token.</returns>
    public static DateTimeOffset ReadDateTimeOffset(this ref Utf8JsonReader reader)
    {
        reader.ReadTokenOrThrow(JsonTokenType.String);
        return reader.GetDateTimeOffset();
    }

    /// <summary>
    /// Reads a <see cref="Guid"/> value from next JSON token in the reader.
    /// </summary>
    /// <remarks>
    /// Throws an exception if read token is not a <see cref="JsonTokenType.String"/>.
    /// </remarks>
    /// <param name="reader">A reference to the <see cref="Utf8JsonReader"/>.</param>
    /// <returns>The <see cref="Guid"/> value represented by the read JSON token.</returns>
    public static Guid ReadGuid(this ref Utf8JsonReader reader)
    {
        reader.ReadTokenOrThrow(JsonTokenType.String);
        return reader.GetGuid();
    }

    /// <summary>
    /// Reads a string value from next JSON token in the reader.
    /// </summary>
    /// <remarks>
    /// Throws an exception if read token is not a <see cref="JsonTokenType.String"/> or <see cref="JsonTokenType.Null"/>.
    /// </remarks>
    /// <param name="reader">A reference to the <see cref="Utf8JsonReader"/>.</param>
    /// <returns>The string value represented by the read JSON token.</returns>
    public static string? ReadString(this ref Utf8JsonReader reader)
    {
        reader.ReadTokenOrThrow(JsonTokenType.String, JsonTokenType.Null);
        return reader.GetString();
    }
    #endregion

    /// <summary>
    /// Reads all properties of the current JSON object and invokes a delegate for each property encountered.
    /// </summary>
    /// <remarks>
    /// The method expects the reader to be positioned before <see cref="JsonTokenType.PropertyName"/>.
    /// After execution, the reader is positioned at the EndObject token. The delegate can process or
    /// skip the property value; if it does not advance the reader, the method will skip the value automatically.
    /// <br/><br/>Property name can be compared to string like this: propertyName.SequenceEqual("Name"u8);
    /// </remarks>
    /// <param name="reader">A reference to the <see cref="Utf8JsonReader"/> positioned at the start of the object.
    /// The reader is advanced as properties are read.</param>
    /// <param name="readPropertyAction">A delegate that is called for each property in the object. Receives the reader
    /// positioned at the property value and the property name as a <see cref="ReadOnlySpan{byte}"/>.</param>
    public static void ReadObjectProperties(this ref Utf8JsonReader reader, ReadPropertyDelegate readPropertyAction)
    {
        while(reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            var propertyName = reader.ValueSpan;

            var startDepth = reader.CurrentDepth;
            readPropertyAction(ref reader, propertyName);

            if(startDepth == reader.CurrentDepth)
                reader.Skip();
        }

        reader.AssertToken(JsonTokenType.EndObject);
    }

    /// <summary>
    /// Reads a JSON object from the current position of the reader and invokes the specified delegate for each property
    /// encountered.
    /// </summary>
    /// <remarks>
    /// The method expects the reader to be positioned before <see cref="JsonTokenType.StartObject"/>.
    /// After execution, the reader is positioned at the EndObject token. The delegate can process or
    /// skip the property value; if it does not advance the reader, the method will skip the value automatically.
    /// </remarks>
    /// <param name="reader">A reference to the <see cref="Utf8JsonReader"/> positioned at the start of the object.
    /// The reader is advanced as properties are read.</param>
    /// <param name="readPropertyAction">A delegate that is called for each property in the object. Receives the reader
    /// positioned at the property value and the property name as a <see cref="ReadOnlySpan{byte}"/>.</param>
    public static void ReadObject(this ref Utf8JsonReader reader, ReadPropertyDelegate readPropertyAction)
    {
        reader.ReadTokenOrThrow(JsonTokenType.StartObject);
        reader.ReadObjectProperties(readPropertyAction);
    }

    /// <summary>
    /// Reads each entry in a JSON array and invokes the specified delegate for each element.
    /// </summary>
    /// <remarks>
    /// The method expects the reader to be positioned at <see cref="JsonTokenType.StartArray"/> or before any array entry.
    /// After execution, the reader is positioned at the end of the array.
    /// The delegate can process each entry as needed; if the delegate does not advance the reader, the method
    /// will skip the current value automatically.
    /// </remarks>
    /// <param name="reader">A reference to the <see cref="Utf8JsonReader"/> positioned at <see cref="JsonTokenType.StartArray"/>
    /// or before any array entry. The reader advances as array entries are processed.</param>
    /// <param name="readArrayEntryAction">A delegate that is called for each array entry.
    /// Receives the reader and the zero-based index of the current entry.</param>
    public static void ReadArrayEntries(this ref Utf8JsonReader reader, ReadArrayEntryDelegate readArrayEntryAction)
    {
        int index = 0;

        while(reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            var startDepth = reader.CurrentDepth;
            readArrayEntryAction(ref reader, index);

            if(startDepth == reader.CurrentDepth)
                reader.Skip();

            index++;
        }

        reader.AssertToken(JsonTokenType.EndArray);
    }

    /// <summary>
    /// Reads JSON array and invokes the specified delegate for each element.
    /// </summary>
    /// <remarks>
    /// The method expects the reader to be positioned at <see cref="JsonTokenType.StartArray"/>. After execution,
    /// the reader is positioned at the end of the array.
    /// The delegate can process each entry as needed; if the delegate does not advance the reader, the method
    /// will skip the current value automatically.
    /// </remarks>
    /// <param name="reader">A reference to the <see cref="Utf8JsonReader"/> positioned at <see cref="JsonTokenType.StartArray"/>
    /// or before any array entry. The reader advances as array entries are processed.</param>
    /// <param name="readArrayEntryAction">A delegate that is called for each array entry.
    /// Receives the reader and the zero-based index of the current entry.</param>
    public static void ReadArray(this ref Utf8JsonReader reader, ReadArrayEntryDelegate readArrayEntryAction)
    {
        reader.ReadTokenOrThrow(JsonTokenType.StartArray);
        reader.ReadArrayEntries(readArrayEntryAction);
    }

    /// <summary>
    /// Reads each entry in a JSON array and deserializes each entry using default <see cref="JsonSerializer.Deserialize"/> behaviour.
    /// </summary>
    /// <param name="reader">A reference to the <see cref="Utf8JsonReader"/> positioned at <see cref="JsonTokenType.StartArray"/>
    /// or before any array entry. The reader advances as array entries are processed.</param>
    /// <param name="options">Options provided to <see cref="JsonSerializer.Deserialize"/>.</param>
    public static List<T?> ReadArrayEntries<T>(this ref Utf8JsonReader reader, JsonSerializerOptions? options = null)
    {
        List<T?> result = [];

        while(reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            var value = JsonSerializer.Deserialize<T>(ref reader, options);
            result.Add(value);
        }

        reader.AssertToken(JsonTokenType.EndArray);

        return [.. result];
    }

    /// <summary>
    /// Reads JSON array and reads each entry in a JSON array and deserializes each entry using default <see cref="JsonSerializer.Deserialize"/> behaviour.
    /// </summary>
    /// <param name="reader">A reference to the <see cref="Utf8JsonReader"/> positioned at <see cref="JsonTokenType.StartArray"/>
    /// or before any array entry. The reader advances as array entries are processed.</param>
    /// <param name="options">Options provided to <see cref="JsonSerializer.Deserialize"/>.</param>
    public static List<T?> ReadArray<T>(this ref Utf8JsonReader reader, JsonSerializerOptions? options = null)
    {
        reader.ReadTokenOrThrow(JsonTokenType.StartArray);
        return reader.ReadArrayEntries<T>(options);
    }


    /// <summary>
    /// Reads next json token as <see cref="T"/>.
    /// </summary>
    /// <remarks>
    /// The method expects the reader to be positioned before first token of <see cref="T"/>.
    /// </remarks>
    /// <param name="reader">A reference to the <see cref="Utf8JsonReader"/> positioned at <see cref="JsonTokenType.StartArray"/>
    /// or before any array entry. The reader advances as array entries are processed.</param>
    /// <param name="options">Options provided to <see cref="JsonSerializer.Deserialize"/>.</param>
    public static T? Read<T>(this ref Utf8JsonReader reader, JsonSerializerOptions? options = null)
    {
        reader.ReadOrThrow();
        return JsonSerializer.Deserialize<T>(ref reader, options);
    }

    /// <summary>
    /// Reads next json token as <see cref="T"/>.
    /// </summary>
    /// <remarks>
    /// The method expects the reader to be positioned at first token of <see cref="T"/>.
    /// </remarks>
    /// <param name="reader">A reference to the <see cref="Utf8JsonReader"/> positioned at <see cref="JsonTokenType.StartArray"/>
    /// or before any array entry. The reader advances as array entries are processed.</param>
    /// <param name="options">Options provided to <see cref="JsonSerializer.Deserialize"/>.</param>
    public static T? Get<T>(this ref Utf8JsonReader reader, JsonSerializerOptions? options = null)
    {
        return JsonSerializer.Deserialize<T>(ref reader, options);
    }
}
