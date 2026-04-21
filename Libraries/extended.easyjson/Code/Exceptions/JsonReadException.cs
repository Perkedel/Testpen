using System;
using System.Collections.Generic;
using System.Text.Json;

namespace EasyJson.Exceptions;

public class JsonReadException : Exception
{
    public JsonReadException(in Utf8JsonReader reader, string message) : base($"Error reading json: {message}. Position: {/*reader.Position*/"position"}.") // TODO: add readerInfo
    {

    }

    public static JsonReadException NothingToRead(in Utf8JsonReader reader) => new(reader, "Nothing to read.");
    public static JsonReadException UnexpectedToken(in Utf8JsonReader reader) => new(reader, $"Token {reader.TokenType} is not expected.");
    public static JsonReadException UnexpectedToken(in Utf8JsonReader reader, JsonTokenType expectedTokenType) => new(reader, $"Token {reader.TokenType} is not expected. Expected {expectedTokenType}.");
    public static JsonReadException UnexpectedToken(in Utf8JsonReader reader, params IEnumerable<JsonTokenType> expectedTokenTypes) => new(reader, $"Token {reader.TokenType} is not expected. Expected one of {string.Join(", ", expectedTokenTypes)}.");
    public static JsonReadException TokenNotPresent(in Utf8JsonReader reader, JsonTokenType expectedTokenType) => new(reader, $"Token {expectedTokenType} not present.");
}