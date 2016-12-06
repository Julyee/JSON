using System;
using System.Reflection;

namespace Julyee.JSON
{
    /// <summary>
    /// Attribute used to provide a serialization alias for class fields and properties. Useful when writing/parsing JSON
    /// files from other tools which use non-standard names (like hyphenated names).
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class SerializeAlias : Attribute
    {
        /// <summary>
        /// Private field that holds this property/field serialization alias.
        /// </summary>
        private string m_alias;

        /// <summary>
        /// Constructor which takes a string representing the serialization alias of this class member.
        /// </summary>
        /// <param name="alias">The serialization alias</param>
        public SerializeAlias(string alias)
        {
            m_alias = alias;
        }

        /// <summary>
        /// If the serialization alias is not empty, this method returns it, otherwise it returns the member's original name.
        /// </summary>
        /// <param name="info">The member's info for which to get the serialization alias</param>
        /// <returns>A valid name to use during serialization.</returns>
        public string GetAlias(MemberInfo info)
        {
            return m_alias != "" ? m_alias : info.Name;
        }
    }
}