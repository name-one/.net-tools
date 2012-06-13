using System;

namespace InoSoft.Tools.Serialization
{
    internal abstract class ReferenceTypeSerializer : Serializer
    {
        internal override bool IsDataNullable
        {
            get { return true; }
            set { throw new InvalidOperationException(); }
        }
    }
}