using System;
using System.Reflection;

namespace Julyee
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class SerializeAlias : Attribute
    {
        private string m_alias;

        public SerializeAlias(string alias)
        {
            m_alias = alias;
        }

        public string GetAlias(MemberInfo info)
        {
            return m_alias != "" ? m_alias : info.Name;
        }
    }
}