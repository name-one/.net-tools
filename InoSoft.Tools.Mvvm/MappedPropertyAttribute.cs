using System;

namespace InoSoft.Tools.Mvvm
{
    [AttributeUsage(AttributeTargets.Property)]
    public class MappedPropertyAttribute : Attribute
    {
        public MappedPropertyAttribute(string sourceName, string template)
        {
            SourceName = sourceName;
            Template = template;
        }

        public MappedPropertyAttribute(string sourceName)
            : this(sourceName, null)
        {
        }

        public MappedPropertyAttribute()
            : this(null, null)
        {
        }

        public string SourceName { get; set; }

        public string Template { get; set; }
    }
}