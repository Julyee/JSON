using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Julyee.JSON
{
    /// <summary>
    /// Attribute class used to make classes serializable for use with the JSON parser.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class Serializable : Attribute
    {
        /// <summary>
        /// Private field that holds the value of the SerializeFields property.
        /// </summary>
        private bool m_serializeFields = true;

        /// <summary>
        /// Private field that holds the value of the SerializeProperties propery.
        /// </summary>
        private bool m_serializeProperties = true;

        /// <summary>
        /// Should the decorated class' fields be serialized.
        /// </summary>
        public bool SerializeFields
        {
            get { return m_serializeFields; }
            set { m_serializeFields = value; }
        }

        /// <summary>
        /// Should the decorated class' properties be serialized.
        /// </summary>
        public bool SerializeProperties
        {
            get { return m_serializeProperties; }
            set { m_serializeProperties = value; }
        }

        /// <summary>
        /// Checks if a class member can be serialized. Note that this method only verifies that the provided member
        /// does not contain the SkipSerialize attribute and does not check if the parent class has the Serializable
        /// attribute.
        /// </summary>
        /// <param name="info">The information of the member to check.</param>
        /// <returns>True if the member can be serialized, false otherwise</returns>
        private static bool CanBeSerialized(MemberInfo info)
        {
            return GetCustomAttribute(info, typeof(SkipSerialize)) == null;
        }

        /// <summary>
        /// Returns the serialization name for the given member. If the member has an instance of the SearializeAlias
        /// attribute, the alias is returned, otherwise the name of the attribute.
        /// </summary>
        /// <param name="info">The info of the member for which the serialization name will be obtained</param>
        /// <returns>A string containing the serialization name</returns>
        private static string GetSerializationName(MemberInfo info)
        {
            SerializeAlias aliasAttribute = (SerializeAlias)GetCustomAttribute(info, typeof(SerializeAlias));
            if (aliasAttribute != null)
            {
                return aliasAttribute.GetAlias(info);
            }
            return info.Name;
        }

        /// <summary>
        /// If the given type has the Serializable attribute, this function traverses it and returns a dictionary
        /// containing the serializable names of its members as the keys and the MemberInfo instances as the values.
        /// </summary>
        /// <param name="type">The type to check for serializable attributes.</param>
        /// <returns>A dictionary containing the serializable type members and their serializable names</returns>
        public static Dictionary<string, MemberInfo> GetSerializablePairs(Type type)
        {
            Dictionary<string, MemberInfo> result = new Dictionary<string, MemberInfo>();
            Serializable serializable = (Serializable)GetCustomAttribute(type, typeof(Serializable));
            if (serializable == null)
            {
                Debug.LogWarningFormat("{0} does not have the Julyee.JSON.Serializable attribute.", type.ToString());
            }
            else
            {
                if (serializable.SerializeFields)
                {
                    FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    for (int i = 0; i < fields.Length; ++i)
                    {
                        if (CanBeSerialized(fields[i]))
                        {
                            result.Add(GetSerializationName(fields[i]), fields[i]);
                        }
                    }
                }

                if (serializable.SerializeProperties)
                {
                    PropertyInfo[] properties = type.GetProperties();
                    for (int i = 0; i < properties.Length; ++i)
                    {
                        if (CanBeSerialized(properties[i]) && properties[i].GetSetMethod() != null)
                        {
                            result.Add(GetSerializationName(properties[i]), properties[i]);
                        }
                    }
                }
            }
            return result;
        }
    }
}