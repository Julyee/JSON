using System;
using System.Text;

namespace Julyee.JSON
{
    /// <summary>
    /// Super simple and fast sequential JSON parser. Uses only static methods.
    /// </summary>
    public class Parser
    {
        /// <summary>
        /// Enum describing the possible operations found during parsing.
        /// </summary>
        public enum OperationType
        {
            /// <summary>
            /// Object opening tag found: "{"
            /// </summary>
            ObjecStart,
            /// <summary>
            /// Object closing tag found: "}"
            /// </summary>
            ObjectEnd,
            /// <summary>
            /// Array opening tag found: "["
            /// </summary>
            ArrayStart,
            /// <summary>
            /// Array closing tag found: "]"
            /// </summary>
            ArrayEnd,
            /// <summary>
            /// Key in a key:value pair found
            /// </summary>
            Key,
            /// <summary>
            /// An arbitrary string value found, either as part of a key:value pair or as an array member
            /// </summary>
            StringValue,
            /// <summary>
            /// A literal value found, It could be a boolean, null or a number
            /// </summary>
            LiteralValue,
            /// <summary>
            /// Sibbling tag found: ","
            /// </summary>
            AppendSibling
        }

        /// <summary>
        /// Delegate definition of the function required as a parser handler. This delegate will be called every time
        /// the parser finds and successfully parses a new operation in the provided JSON string.
        /// </summary>
        /// <param name="operation">The operation type found by the parser, see OperationType for more info</param>
        /// <param name="content">A string containing the content found during the operation parsing, can be a control character</param>
        /// <param name="depth">How many levels into the JSON hierarchy was the operation performed</param>
        public delegate void ParseHandler(OperationType operation, string content, int depth);

        /// <summary>
        /// Parses a JSON string sequentially and invokes the provided handler on every operation performed.
        /// </summary>
        /// <param name="jsonString">The JSON data to parse</param>
        /// <param name="handler">Delegate to be invoked for the operations performed</param>
        /// <exception cref="Exception">If an error is encountered while parsing the JSON data this exception is thrown</exception>
        public static void Parse(string jsonString, ParseHandler handler)
        {
            Reader reader = new Reader(jsonString);
            StringBuilder builder = new StringBuilder();

            char? startCharacter = reader.GetNextCharacterNoWhiteSpace();

            if (startCharacter == '{')
            {
                ParseObject(reader, builder, handler, 0);
            }
            else if (startCharacter == '[')
            {
                ParseArray(reader, builder, handler, 0);
            }
            else
            {
                throw new Exception("Julyee.Parser the provided string is malformed JSON.");
            }
        }

        /// <summary>
        /// Given the position of the provided reader, an object is parsed. Will be called recursively if needed.
        /// </summary>
        /// <param name="reader">The Julyee.JSON.Reader to use to parse the object</param>
        /// <param name="builder">In order to avoid unecessary allocations, a StringBuilder must be provided</param>
        /// <param name="handler">Delegate to be invoked for the operations performed</param>
        /// <param name="depth">The depth at which this object was found</param>
        /// <exception cref="Exception">If an error is encountered while parsing the JSON data this exception is thrown</exception>
        private static void ParseObject(Reader reader, StringBuilder builder, ParseHandler handler, int depth)
        {
            handler(OperationType.ObjecStart, "{", depth);

            char controlChar = reader.GetNextCharacterNoWhiteSpaceSafe();

            while (controlChar != '}')
            {
                if (controlChar != '\"')
                {
                    throw new Exception("Malformed JSON. Object keys must be enclosed by quotes (\").");
                }

                string key = reader.ReadString(builder);
                handler(OperationType.Key, key, depth);

                controlChar = reader.GetNextCharacterNoWhiteSpaceSafe();
                if (controlChar != ':')
                {
                    throw new Exception("Malformed JSON. Expected \":\" after \"" + key + "\".");
                }
                builder.Length = 0;

                controlChar = reader.GetNextCharacterNoWhiteSpaceSafe();
                if (Reader.IsControlCharacter(controlChar))
                {
                    if (controlChar == '{')
                    {
                        ParseObject(reader, builder, handler, depth + 1);
                    }
                    else if (controlChar == '[')
                    {
                        ParseArray(reader, builder, handler, depth + 1);
                    }
                    else if (controlChar == '\"')
                    {
                        string value = reader.ReadString(builder);
                        handler(OperationType.StringValue, value, depth);
                    }
                }
                else
                {
                    builder.Append(controlChar);
                    string value = reader.ReadValue(builder);
                    handler(OperationType.LiteralValue, value, depth);
                }

                builder.Length = 0;
                controlChar = reader.GetNextCharacterNoWhiteSpaceSafe();

                if (controlChar == ',')
                {
                    controlChar = reader.GetNextCharacterNoWhiteSpaceSafe();
                    handler(OperationType.AppendSibling, ",", depth);
                }
                else if (controlChar != '}')
                {
                    throw new Exception("Malformed JSON. Expected delimeter in object.");
                }
            }

            handler(OperationType.ObjectEnd, "}", depth);
        }

        /// <summary>
        /// Given the position of the provided reader, an array is parsed. Will be called recursively if needed.
        /// </summary>
        /// <param name="reader">The Julyee.JSON.Reader to use to parse the object</param>
        /// <param name="builder">In order to avoid unecessary allocations, a StringBuilder must be provided</param>
        /// <param name="handler">Delegate to be invoked for the operations performed</param>
        /// <param name="depth">The depth at which this object was found</param>
        /// <exception cref="Exception">If an error is encountered while parsing the JSON data this exception is thrown</exception>
        private static void ParseArray(Reader reader, StringBuilder builder, ParseHandler handler, int depth)
        {
            handler(OperationType.ArrayStart, "[", depth);

            char controlChar = reader.GetNextCharacterNoWhiteSpaceSafe();

            while (controlChar != ']')
            {
                if (Reader.IsControlCharacter(controlChar))
                {
                    if (controlChar == '{')
                    {
                        ParseObject(reader, builder, handler, depth + 1);
                    }
                    else if (controlChar == '[')
                    {
                        ParseArray(reader, builder, handler, depth + 1);
                    }
                    else if (controlChar == '\"')
                    {
                        string value = reader.ReadString(builder);
                        handler(OperationType.StringValue, value, depth);
                    }
                }
                else
                {
                    builder.Append(controlChar);
                    string value = reader.ReadValue(builder);
                    handler(OperationType.LiteralValue, value, depth);
                }

                builder.Length = 0;
                controlChar = reader.GetNextCharacterNoWhiteSpaceSafe();

                if (controlChar == ',')
                {
                    controlChar = reader.GetNextCharacterNoWhiteSpaceSafe();
                    handler(OperationType.AppendSibling, ",", depth);
                }
                else if (controlChar != ']')
                {
                    throw new Exception("Malformed JSON. Expected delimeter in array.");
                }
            }

            handler(OperationType.ArrayEnd, "]", depth);
        }
    }
}