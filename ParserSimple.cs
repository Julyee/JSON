using System.Collections.Generic;

namespace Julyee.JSON
{
    /// <summary>
    /// Utility class that parses a JSON string into a tree of Dictionaries and Lists
    /// where all the values, including literal values, are parsed as strings.
    /// </summary>
    public class ParserSimple
    {
        /// <summary>
        /// Parses a JSON string and returns an object representing the root of the parsed tree.
        /// </summary>
        /// <param name="jsonString">The JSON string to parse.</param>
        /// <returns>Either a Dictionatyor a List representing the root of the parsed JSON structure</returns>
        public static object Parse(string jsonString)
        {
            object current = null;
            string key = null;
            Stack<object> stack = new Stack<object>();

            Parser.Parse(jsonString, (operation, content, depth) =>
            {
                switch (operation)
                {
                    case Parser.OperationType.ObjecStart:
                    {
                        Dictionary<string, object> value = new Dictionary<string, object>();
                        if (current != null)
                        {
                            key = _AddToCurrent(current, key, value);
                            stack.Push(current);
                        }
                        current = value;
                        break;
                    }

                    case Parser.OperationType.ArrayStart:
                    {
                        List<object> value = new List<object>();
                        if (current != null)
                        {
                            key = _AddToCurrent(current, key, value);
                            stack.Push(current);
                        }
                        current = value;
                        break;
                    }

                    case Parser.OperationType.ObjectEnd:
                    case Parser.OperationType.ArrayEnd:
                        if (stack.Count > 0)
                        {
                            current = stack.Pop();
                        }
                        break;

                    case Parser.OperationType.Key:
                        key = content;
                        break;

                    case Parser.OperationType.StringValue:
                    case Parser.OperationType.LiteralValue:
                        key = _AddToCurrent(current, key, content);
                        break;
                }
            });

            return current;
        }

        /// <summary>
        /// Adds a value to the current object, be it a dictionary or a list.
        /// </summary>
        /// <param name="current">The current object being processed.</param>
        /// <param name="key">The key of the value, can be null in which case it is assumed that current is a list</param>
        /// <param name="value">The value to add to the current object</param>
        /// <returns>Always returns null, useful to reset the key value to null once it has been added to the current object</returns>
        private static string _AddToCurrent(object current, string key, object value)
        {
            if (key != null)
            {
                ((Dictionary<string, object>) current).Add(key, value);
            }
            else
            {
                ((List<object>) current).Add(value);
            }

            return null;
        }
    }
}