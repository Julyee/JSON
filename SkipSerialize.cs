using System;

namespace Julyee
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class SkipSerialize : Attribute
    {
        public SkipSerialize()
        {
            // needed for compilation
        }
    }
}
