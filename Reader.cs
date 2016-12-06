using System;
using System.IO;
using System.Text;

namespace Julyee.JSON
{
    /// <summary>
    /// Utility class that extends the functionality of StringReader to include JSON parsing specific methods.
    /// </summary>
    public class Reader : StringReader
    {
        /// <summary>
        /// Default constructor that simply forwards the json string to the base class.
        /// </summary>
        /// <param name="jsonString">JSON data as a string</param>
        public Reader(string jsonString) : base(jsonString)
        {
            // default constructor
        }

        /// <summary>
        /// Reads a the characters in the string buffer from the current head position up until the next non-escaped
        /// duoble quote instance ("). The result does not include the last character read but this function moves the
        /// head past it.
        /// </summary>
        /// <param name="builder">To avoid unnecessary allocations, a StringBuilder can be provided</param>
        /// <returns>The parsed string</returns>
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

        /// <summary>
        /// Reads a the characters in the string buffer from the current head position up until the next control character
        /// or white space. Does not include the last character read and does not move the head past it.
        /// </summary>
        /// <param name="builder">To avoid unnecessary memory allocations, a StringBuilder can be provided</param>
        /// <returns>The string resulting from the operation</returns>
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

        /// <summary>
        /// Gets the next character in the string buffer that is not considered white space. Throws an exception if the
        /// end of the string buffer is reached.
        /// </summary>
        /// <returns>The next non-white space character</returns>
        /// <exception cref="Exception">If the end of the string buffer is reached before a non-white space charcter is
        /// found, this exception is thrown</exception>
        public char GetNextCharacterNoWhiteSpaceSafe()
        {
            char? ch = GetNextCharacterNoWhiteSpace();
            if (ch == null)
            {
                throw new Exception("Malformed JSON file. Unexpected EOF");
            }

            return (char)ch;
        }

        /// <summary>
        /// Gets the next character in the string buffer that is not considered white space.
        /// </summary>
        /// <returns>The next non-white space character or null if the end of the string buffer is reached</returns>
        public char? GetNextCharacterNoWhiteSpace()
        {
            for (char? ch = GetNextCharacter(); ch != null; ch = GetNextCharacter())
            {
                if (!char.IsWhiteSpace((char)ch))
                {
                    return ch;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the next character in the string buffer and advances the head forward. Throws an exception if the head
        /// is at the end of the string buffer.
        /// </summary>
        /// <returns>The</returns>
        /// <exception cref="Exception">If the head is at the end of the string buffer this exception is thrown</exception>
        public char GetNextCharacterSafe()
        {
            char? ch = GetNextCharacter();
            if (ch == null)
            {
                throw new Exception("Malformed JSON file. Unexpected EOF");
            }
            return (char) ch;
        }

        /// <summary>
        /// Gets the next character in the string buffer and advances the head forward.
        /// </summary>
        /// <returns>The next character in the string or null if at the end of the string buffer</returns>
        public char? GetNextCharacter()
        {
            int ch = Read();
            if (ch == -1)
            {
                return null;
            }
            return Convert.ToChar(ch);
        }

        /// <summary>
        /// Returns the next character in the string buffer without moving the head forward. Throws an exception if the
        /// head is at the end of the string buffer.
        /// </summary>
        /// <returns>The next character in the string</returns>
        /// <exception cref="Exception">If the head is at the end of the string buffer this exception is thrown</exception>
        public char PeekNextCharacterSafe()
        {
            char? ch = PeekNextCharacter();
            if (ch == null)
            {
                throw new Exception("Malformed JSON file. Unexpected EOF");
            }
            return (char) ch;
        }

        /// <summary>
        /// Returns the next character in the string buffer without moving the head forward.
        /// </summary>
        /// <returns>The next character in the string or null if at the end of the string buffer.</returns>
        public char? PeekNextCharacter()
        {
            int ch = Peek();
            if (ch == -1)
            {
                return null;
            }
            return Convert.ToChar(ch);
        }

        /// <summary>
        /// Checks whether or not a character is a JSON control character
        /// </summary>
        /// <param name="ch">The character to test</param>
        /// <returns>True if the character is a control character, false otherwise</returns>
        public static bool IsControlCharacter(char? ch)
        {
            return ch == '{' || ch == '}' ||
                   ch == '[' || ch == ']' ||
                   ch == ':' || ch == ',' ||
                   ch == '\"';
        }
    }
}