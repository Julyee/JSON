using System;
using System.Text;

namespace Julyee.JSON
{
    public class Parser
    {
        public enum Operationtype
        {
            ObjecStart,
            ObjectEnd,
            ArrayStart,
            ArrayEnd,
            Key,
            StringValue,
            LiteralValue,
            AppendSibling
        }

        public delegate void ParseHandler(Operationtype operation, string content, int depth);

        public static void Parse(string jsonString, ParseHandler handler)
        {
            Reader reader = new Reader(jsonString);
            StringBuilder builder = new StringBuilder();

            char? startCharacter = reader.GetNextControlCharacter();

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

        internal static void ParseObject(Reader reader, StringBuilder builder, ParseHandler handler, int depth)
        {
            handler(Operationtype.ObjecStart, "{", depth);

            char controlChar = reader.GetNextControlCharacterSafe();

            while (controlChar != '}')
            {
                if (controlChar != '\"')
                {
                    throw new Exception("Malformed JSON. Object keys must be enclosed by quotes (\").");
                }

                string key = reader.ReadString(builder);
                handler(Operationtype.Key, key, depth);

                controlChar = reader.GetNextControlCharacterSafe();
                if (controlChar != ':')
                {
                    throw new Exception("Malformed JSON. Expected \":\" after \"" + key + "\".");
                }
                builder.Length = 0;

                controlChar = reader.GetNextControlCharacterSafe();
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
                        handler(Operationtype.StringValue, value, depth);
                    }
                }
                else
                {
                    builder.Append(controlChar);
                    string value = reader.ReadValue(builder);
                    handler(Operationtype.LiteralValue, value, depth);
                }

                builder.Length = 0;
                controlChar = reader.GetNextControlCharacterSafe();

                if (controlChar == ',')
                {
                    controlChar = reader.GetNextControlCharacterSafe();
                    handler(Operationtype.AppendSibling, ",", depth);
                }
                else if (controlChar != '}')
                {
                    throw new Exception("Malformed JSON. Expected delimeter in object.");
                }
            }

            handler(Operationtype.ObjectEnd, "}", depth);
        }

        internal static void ParseArray(Reader reader, StringBuilder builder, ParseHandler handler, int depth)
        {
            handler(Operationtype.ArrayStart, "[", depth);

            char controlChar = reader.GetNextControlCharacterSafe();

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
                        handler(Operationtype.StringValue, value, depth);
                    }
                }
                else
                {
                    builder.Append(controlChar);
                    string value = reader.ReadValue(builder);
                    handler(Operationtype.LiteralValue, value, depth);
                }

                builder.Length = 0;
                controlChar = reader.GetNextControlCharacterSafe();

                if (controlChar == ',')
                {
                    controlChar = reader.GetNextControlCharacterSafe();
                    handler(Operationtype.AppendSibling, ",", depth);
                }
                else if (controlChar != ']')
                {
                    throw new Exception("Malformed JSON. Expected delimeter in array.");
                }
            }

            handler(Operationtype.ArrayEnd, "]", depth);
        }
    }
}