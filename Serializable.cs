using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Julyee
{
    [AttributeUsage(AttributeTargets.Class)]
    public class Serializable : Attribute
    {
        public Serializable()
        {
            // default constructor
        }

        public static Dictionary<string, MemberInfo> GetSerializablePairs(Type serializable)
        {
            Dictionary<string, MemberInfo> result = new Dictionary<string, MemberInfo>();
            if (GetCustomAttribute(serializable, typeof(Serializable)) == null)
            {
                Debug.LogWarningFormat("{0} does not have the Julyee.Serializable attribute.", serializable.ToString());
            }
            else
            {
                FieldInfo[] fields = serializable.GetFields();
                for (int i = 0; i < fields.Length; ++i)
                {
                    if (CanBeSerialized(fields[i]))
                    {
                        result.Add(GetSerializationName(fields[i]), fields[i]);
                    }
                }

                PropertyInfo[] properties = serializable.GetProperties();
                for (int i = 0; i < properties.Length; ++i)
                {
                    if (CanBeSerialized(properties[i]))
                    {
                        result.Add(GetSerializationName(properties[i]), properties[i]);
                    }
                }
            }
            return result;
        }

        private static bool CanBeSerialized(MemberInfo info)
        {
            return GetCustomAttribute(info, typeof(SkipSerialize)) == null;
        }

        private static string GetSerializationName(MemberInfo info)
        {
            SerializeAlias aliasAttribute = (SerializeAlias)GetCustomAttribute(info, typeof(SerializeAlias));
            if (aliasAttribute != null)
            {
                return aliasAttribute.GetAlias(info);
            }
            return info.Name;
        }
    }
}