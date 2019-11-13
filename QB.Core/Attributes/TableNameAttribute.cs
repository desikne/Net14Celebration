using System;

namespace QB.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class TableNameAttribute : Attribute
    {
        public string Value { get; set; }
    }
}