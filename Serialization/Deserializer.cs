using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using ParsableKeys = System.Collections.Generic.Dictionary<string, System.Reflection.MemberInfo>;

namespace Julyee.JSON.Serialization
{
    public static class Deserializer
    {
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
                            else if (member != null)
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
                                    processing = _instantiateList<object>(processing, member, objectStack);
                                    member = null;
                                }

                                if (processing.GetType() == typeof(List<>))
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
                            if (processingType != typeof(List<>) && member == null)
                            {
                                break;
                            }

                            if (processingType == typeof(List<>))
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
                                    if (processing != null && processing.GetType() != typeof(List<>))
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
                                    processing = _instantiateList<object>(processing, member, objectStack);
                                    member = null;
                                }

                                if (processing.GetType() == typeof(List<>))
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
                                    processing = _instantiateList<object>(processing, member, objectStack);
                                    member = null;
                                }

                                if (processing.GetType() == typeof(List<>))
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
                _assignValue(instance, member, int.Parse(value));
            }
            else
            {
                float f;
                if (float.TryParse(value, out f))
                {
                    _assignValue(instance, member, f);
                }
                else
                {
                    throw new Exception("Invalid literal value: " + value);
                }
            }
        }

        private static void _addValue<T>(object instance, T value)
        {
            List<T> list = (List<T>) instance;
            list.Add(value);
        }

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
            else if (ParserTyped._IsInt(value))
            {
                _addValue(instance, int.Parse(value));
            }
            else
            {
                float f;
                if (float.TryParse(value, out f))
                {
                    _addValue(instance, f);
                }
                else
                {
                    throw new Exception("Invalid literal value: " + value);
                }
            }
        }

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

        private static IList _createList(Type genericListType)
        {
            IList instance = (IList)Activator.CreateInstance(genericListType);
            return instance;
        }

        private static Type _getGenericListType(Type type)
        {
            Type listType = typeof(List<>);
            return listType.MakeGenericType(type);
        }

        private static object _instantiateList<T>(object processing, MemberInfo member, Stack<object> stack)
        {
            if (processing == null)
            {
                if (member == null)
                {
                    throw new ArgumentException("Input MemberInfo cannot be null");
                }

                Type lastListType = _getGenericListType(typeof(T));
                processing = _createList(lastListType);
                stack.Pop();

                Stack<object> toPush = new Stack<object>();
                toPush.Push(processing);

                while (stack.Peek() == null)
                {
                    lastListType = _getGenericListType(lastListType);
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