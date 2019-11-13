using System;

namespace QB.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class PrimaryKeyColumnAttribute : Attribute
    {
        /// <summary>
        ///     The SQL name of the column
        /// </summary>
        public string Name { get; set; }

        public PrimaryKeyColumnAttribute()
        {
        }

        public PrimaryKeyColumnAttribute(string name)
        {
            this.Name = name;
        }
    }
}