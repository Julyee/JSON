using System;
using System.IO;
using System.Text;

namespace Julyee.JSON
{
    public class Reader : StringReader
    {
        public Reader(string jsonString) : base(jsonString)
        {
            // default constructor
        }

        public string ReadString(StringBuilder builder = null)
        {
            if (builder == null)
            {
                builder = new StringBuilder();
            }

            for (char ch = GetNextCharacterSafe(); ch != '\"'; ch = GetNextCharacterSafe())
            {
                if (ch == '\\')
                {
                    builder.Append(ch);
                    ch = GetNextCharacterSafe();
                    builder.Append(ch);
                }
                else
                {
                    builder.Append(ch);
                }
            }
            return builder.ToString();
        }

        public string ReadValue(StringBuilder builder = null)
        {
            if (builder == null)
            {
                builder = new StringBuilder();
            }

            for (char ch = PeekNextCharacterSafe(); !IsControlCharacter(ch) && !char.IsWhiteSpace(ch); ch = PeekNextCharacterSafe())
            {
                Read();
                builder.Append(ch);
            }
            return builder.ToString();
        }

        public char GetNextControlCharacterSafe(bool returnOnOther = true)
        {
            char? ch = GetNextControlCharacter(returnOnOther);
            if (ch == null)
            {
                throw new Exception("Malformed JSON file. Unexpected EOF");
            }

            return (char)ch;
        }

        public char? GetNextControlCharacter(bool returnOnNonBlank = true)
        {
            bool skip = false;
            for (char? ch = GetNextCharacter(); ch != null; ch = GetNextCharacter())
            {
                if (skip)
                {
                    skip = false;
                    continue;
                }

                if (IsControlCharacter(ch) || (returnOnNonBlank && !char.IsWhiteSpace((char)ch)))
                {
                    return ch;
                }

                if (ch == '\\')
                {
                    skip = true;
                }
            }
            return null;
        }

        public char GetNextCharacterSafe()
        {
            char? ch = GetNextCharacter();
            if (ch == null)
            {
                throw new Exception("Malformed JSON file. Unexpected EOF");
            }
            return (char) ch;
        }

        public char? GetNextCharacter()
        {
            int ch = Read();
            if (ch == -1)
            {
                return null;
            }
            return Convert.ToChar(ch);
        }

        public char PeekNextCharacterSafe()
        {
            char? ch = PeekNextCharacter();
            if (ch == null)
            {
                throw new Exception("Malformed JSON file. Unexpected EOF");
            }
            return (char) ch;
        }

        public char? PeekNextCharacter()
        {
            int ch = Peek();
            if (ch == -1)
            {
                return null;
            }
            return Convert.ToChar(ch);
        }

        public static bool IsControlCharacter(char? ch)
        {
            return ch == '{' || ch == '}' ||
                   ch == '[' || ch == ']' ||
                   ch == ':' || ch == ',' ||
                   ch == '\"';
        }
    }
}