using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using ParsableKeys = System.Collections.Generic.Dictionary<string, System.Reflection.MemberInfo>;

namespace Julyee.JSON.Serialization
{
    /// <summary>
    /// Parser static class used to deserialize objects, it calls the default constructor of the deserialized objects and
    /// sets their values through fields and properties. Cannot call non-default constructors.
    /// </summary>
    public static class Deserializer
    {
        /// <summary>
        /// This function parses the given JSON string and tries to deserialize an object of the given type.
        /// If the JSON string contains nested objects, this function looks for equivaent deserializable classes within
        /// the fields and properties of the original class.
        /// </summary>
        /// <param name="jsonString">The JSON string to parse.</param>
        /// <typeparam name="T">The root type to deserialize, must have the Serializable attribute.</typeparam>
        /// <returns>An instance of the specified type with the deserialized data.</returns>
        public static T Parse<T>(string jsonString)
        {
            Type type = typeof(T);

            T instance = default(T);
            object processing = null;
            ParsableKeys properties = null;
            Stack<object> objectStack = new Stack<object>();
            MemberInfo member = null;

            Parser.Parse(jsonString, (operation, content, depth) =>
            {

                if (depth == objectStack.Count)
                {
                    switch (operation)
                    {
                        case Parser.OperationType.Key:
                            if (properties.ContainsKey(content))
                            {
                                member = properties[content];
                            }
                            else
                            {
                                member = null;
                            }
                            break;

                        case Parser.OperationType.ObjecStart:
                            if (instance == null)
                            {
                                instance = (T) Activator.CreateInstance(type);
                                processing = instance;
                                properties = Serializable.GetSerializablePairs(type);
                                objectStack.Push(processing);
                            }
                            else if (member != null && processing != null && !_isList(processing))
                            {
                                Type objectType = _getMemberType(member);
                                if (objectType != null)
                                {
                                    object newObject = Activator.CreateInstance(objectType);
                                    _assignValue(processing, member, newObject);

                                    processing = newObject;
                                    properties = Serializable.GetSerializablePairs(objectType);
                                    objectStack.Push(processing);
                                    member = null;
                                }
                            }
                            else
                            {
                                if (processing == null)
                                {
                                    processing = _instantiateList(processing, member, objectStack);
                                    member = null;
                                }

                                if (_isList(processing))
                                {
                                    Type objectType = processing.GetType().GetGenericArguments()[0];
                                    if (objectType != null)
                                    {
                                        object newObject = Activator.CreateInstance(objectType);
                                        _addValue(processing, newObject);

                                        processing = newObject;
                                        properties = Serializable.GetSerializablePairs(objectType);
                                        objectStack.Push(processing);
                                        member = null;
                                    }
                                }
                            }
                            break;

                        case Parser.OperationType.ArrayStart:
                            Type processingType = processing.GetType();
                            if (!_isList(processingType) && member == null)
                            {
                                break;
                            }

                            if (_isList(processingType))
                            {
                                Type objectType = processing.GetType().GetGenericArguments()[0];
                                if (objectType != null)
                                {
                                    IList newList = (IList) Activator.CreateInstance(objectType);
                                    ((IList) processing).Add(newList);

                                    processing = newList;
                                    properties = null;
                                    objectStack.Push(processing);
                                }
                            }
                            else
                            {
                                processing = null;
                                properties = null;
                                objectStack.Push(processing);
                            }
                            break;

                        case Parser.OperationType.ObjectEnd:
                        case Parser.OperationType.ArrayEnd:
                            if (objectStack.Count > 0)
                            {
                                objectStack.Pop();
                                if (objectStack.Count > 0)
                                {
                                    processing = objectStack.Peek();
                                    if (processing != null && !_isList(processing))
                                    {
                                        properties = Serializable.GetSerializablePairs(processing.GetType());
                                    }
                                }
                                else
                                {
                                    processing = null;
                                }
                            }
                            break;

                        case Parser.OperationType.StringValue:
                            if (member != null)
                            {
                                _assignValue(processing, member, content);
                            }
                            else
                            {
                                if (processing == null)
                                {
                                    processing = _instantiateList(processing, member, objectStack);
                                    member = null;
                                }

                                if (_isList(processing))
                                {
                                    _addValue(processing, content);
                                }
                            }
                            break;

                        case Parser.OperationType.LiteralValue:
                            if (member != null)
                            {
                                _assignLiteralValue(processing, member, content);
                            }
                            else
                            {
                                if (processing == null)
                                {
                                    processing = _instantiateList(processing, member, objectStack);
                                    member = null;
                                }

                                if (_isList(processing))
                                {
                                    _addLiteralValue(processing, content);
                                }
                            }
                            break;
                    }
                }
            });

            return instance;
        }

        /// <summary>
        /// Internal method to assign a value to an instance of an object.
        /// </summary>
        /// <param name="instance">The instance of the object to which the value will be assigned.</param>
        /// <param name="member">The information of the instance's member which will hold the value.</param>
        /// <param name="value">The value to be assigned.</param>
        /// <typeparam name="T">The type of the value.</typeparam>
        private static void _assignValue<T>(object instance, MemberInfo member, T value)
        {
            MemberTypes type = member.MemberType;
            if (type == MemberTypes.Field)
            {
                FieldInfo field = (FieldInfo) member;
                field.SetValue(instance, value);
            }
            else if (type == MemberTypes.Property)
            {
                PropertyInfo property = (PropertyInfo) member;
                if (property.GetSetMethod() != null)
                {
                    property.SetValue(instance, value, null);
                }
            }
        }

        /// <summary>
        /// Private method to assign a JSON literal value (string, boolean, number, null)
        /// </summary>
        /// <param name="instance">The instance of the object to which the value will be assigned.</param>
        /// <param name="member">The information of the instance's member which will hold the value.</param>
        /// <param name="value">String representation of the vaue to be assigned.</param>
        /// <exception cref="Exception">Thrown if the passed string cannot be parsed as a literal value.</exception>
        private static void _assignLiteralValue(object instance, MemberInfo member, string value)
        {
            if (value == "null")
            {
                _assignValue<object>(instance, member, null);
            }
            else if (value == "true")
            {
                _assignValue(instance, member, true);
            }
            else if (value == "false")
            {
                _assignValue(instance, member, false);
            }
            else if (ParserTyped._IsInt(value))
            {
                /* cast to float if needed */
                if (_getMemberType(member) == typeof(float))
                {
                    _assignValue(instance, member, float.Parse(value));
                }
                else
                {
                    _assignValue(instance, member, int.Parse(value));
                }
            }
            else
            {
                float f;
                if (float.TryParse(value, out f))
                {
                    /* cast to int if needed */
                    if (_getMemberType(member) == typeof(int))
                    {
                        _assignValue(instance, member, (int) f);
                    }
                    else
                    {
                        _assignValue(instance, member, f);
                    }
                }
                else
                {
                    throw new Exception("Invalid literal value: " + value);
                }
            }
        }

        /// <summary>
        /// Private method that adds the given value to the specified list instance.
        /// </summary>
        /// <param name="instance">An instance of an object that implements IList</param>
        /// <param name="value">The value to add to the list</param>
        /// <typeparam name="T">The type of the value</typeparam>
        private static void _addValue<T>(object instance, T value)
        {
            IList list = (IList) instance;
            list.Add(value);
        }

        /// <summary>
        /// Private method that adds a literal value to the specified list instance.
        /// </summary>
        /// <param name="instance">An instance of an object that implements IList</param>
        /// <param name="value">String representation of a literal value.</param>
        /// <exception cref="Exception">Thrown if the passed string cannot be parsed as a literal value.</exception>
        private static void _addLiteralValue(object instance, string value)
        {

            if (value == "null")
            {
                _addValue<object>(instance, null);
            }
            else if (value == "true")
            {
                _addValue(instance, true);
            }
            else if (value == "false")
            {
                _addValue(instance, false);
            }
            if (ParserTyped._IsInt(value))
            {
                /* cast to float if needed */
                if (_getListItemType(instance) == typeof(float))
                {
                    _addValue(instance, float.Parse(value));
                }
                else
                {
                    _addValue(instance, int.Parse(value));
                }
            }
            else
            {
                float f;
                if (float.TryParse(value, out f))
                {
                    /* cast to int if needed */
                    if (_getListItemType(instance) == typeof(int))
                    {
                        _addValue(instance, (int) f);
                    }
                    else
                    {
                        _addValue(instance, f);
                    }
                }
                else
                {
                    throw new Exception("Invalid literal value: " + value);
                }
            }
        }

        /// <summary>
        /// Private method to get the type of an object's member based on its info.
        /// </summary>
        /// <param name="member">The member info from which the type will be extracted</param>
        /// <returns>The parsed type</returns>
        /// <exception cref="ArgumentException">Thrown if the specified memeber is not a field or a property.</exception>
        private static Type _getMemberType(MemberInfo member)
        {
            switch (member.MemberType)
            {
                case MemberTypes.Field:
                    return ((FieldInfo)member).FieldType;
                case MemberTypes.Property:
                    return ((PropertyInfo)member).PropertyType;
                default:
                    throw new ArgumentException("Input MemberInfo must be FieldInfo or Property Info");
            }
        }

        /// <summary>
        /// Idetifies the item type of a generic list.
        /// </summary>
        /// <param name="list">The list to extract the type from</param>
        /// <returns>The item type</returns>
        private static Type _getListItemType(object list)
        {
            return list.GetType().GetGenericArguments()[0];
        }

        /// <summary>
        /// Creates a list of the given generic type.
        /// </summary>
        /// <param name="genericListType">The generic type of the new list.</param>
        /// <returns>A new list</returns>
        private static IList _createList(Type genericListType)
        {
            IList instance = (IList)Activator.CreateInstance(genericListType);
            return instance;
        }

        /// <summary>
        /// Returns the type of a generic list containing items of the given type.
        /// </summary>
        /// <param name="type">The type of the items in the list.</param>
        /// <returns>A generic list type.</returns>
        private static Type _genericListType(Type type)
        {
            Type listType = typeof(List<>);
            return listType.MakeGenericType(type);
        }

        /// <summary>
        /// Checks if the given object is a generic list.
        /// </summary>
        /// <param name="obj">The object to check</param>
        /// <returns>Whether or not the object is a list</returns>
        private static bool _isList(object obj)
        {
            return _isList(obj.GetType());
        }

        /// <summary>
        /// Checks if the given type pertains to a generic list.
        /// </summary>
        /// <param name="type">The type to check</param>
        /// <returns>Whether or not the type is that of a generic list.</returns>
        private static bool _isList(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
        }

        /// <summary>
        /// Private method to instantiate and add a list to an object during JSON parsing.
        /// </summary>
        /// <param name="processing">The current object being processed by the parser</param>
        /// <param name="member">The current object member being processed by the parser</param>
        /// <param name="stack">The object stack of the parser.</param>
        /// <returns>The object that the parser should continue processing afterthe operation.</returns>
        /// <exception cref="ArgumentException">Thrown if nothing is being processed and no member is specified.</exception>
        private static object _instantiateList(object processing, MemberInfo member, Stack<object> stack)
        {
            if (processing == null)
            {
                if (member == null)
                {
                    throw new ArgumentException("Input MemberInfo cannot be null");
                }

                Type type = _getMemberType(member);
                while (_isList(type))
                {
                    type = type.GetGenericArguments()[0];
                }

                Type lastListType = _genericListType(type);
                processing = _createList(lastListType);
                stack.Pop();

                Stack<object> toPush = new Stack<object>();
                toPush.Push(processing);

                while (stack.Peek() == null)
                {
                    lastListType = _genericListType(lastListType);
                    IList newList = _createList(lastListType);
                    newList.Add(toPush.Peek());
                    toPush.Push(newList);
                    stack.Pop();
                }

                object propertyContainer = stack.Peek();
                object listToAssign = toPush.Peek();

                while (toPush.Count > 0)
                {
                    stack.Push(toPush.Pop());
                }

                _assignValue(propertyContainer, member, listToAssign);
            }

            return processing;
        }
    }
}