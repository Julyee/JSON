using System;

namespace Julyee.JSON
{
    /// <summary>
    /// Utility class that parses a JSON string into a tree of Dictionaries and Lists
    /// where primitive values are boxed.
    /// </summary>
    public static class ParserTyped
    {
        /// <summary>
        /// Parses a JSON string and returns an object representing the root of the parsed tree.
        /// </summary>
        /// <param name="jsonString">The JSON string to parse.</param>
        /// <returns>Either a Dictionatyor a List representing the root of the parsed JSON structure</returns>
        public static object Parse(string jsonString)
        {
            return ParserSimple._Parse(jsonString, _ParseValue);
        }

        /// <summary>
        /// Parses a string as a value.
        /// </summary>
        /// <param name="operation">The operation type that should be used to determine the value of the string.</param>
        /// <param name="value">The string from which the value will be parsed.</param>
        /// <returns>The parsed value.</returns>
        /// <exception cref="Exception">If no value can be parsed according to the operation type, this exceptions is thrown.</exception>
        private static object _ParseValue(Parser.OperationType operation, string value)
        {
            if (operation == Parser.OperationType.StringValue)
            {
                return value;
            }
            else if (value == "null")
            {
                return null;
            }
            else if (value == "true")
            {
                return true;
            }
            else if (value == "false")
            {
                return false;
            }
            else if (_IsInt(value))
            {
                return int.Parse(value);
            }
            else
            {
                float f;
                if (float.TryParse(value, out f))
                {
                    return f;
                }
                else
                {
                    throw new Exception("Invalid literal value: " + value);
                }

            }
        }

        /// <summary>
        /// Checks if the string represents an integer by making sure it only contains numeric characters.
        /// </summary>
        /// <param name="candidate">The string to check.</param>
        /// <returns>Wheter the string is an integer or not</returns>
        public static bool _IsInt(string candidate)
        {
            foreach (char c in candidate)
            {
                if (c < '0' || c > '9')
                    return false;
            }
            return !string.IsNullOrEmpty(candidate);
        }
    }
}