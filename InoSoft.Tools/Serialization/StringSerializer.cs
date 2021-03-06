﻿using System;
using System.IO;

namespace InoSoft.Tools.Serialization
{
    internal class StringSerializer : ReferenceTypeSerializer
    {
        internal override void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)DataType.String);
        }

        internal override bool IsCompatibleWithType(Type type)
        {
            return type == typeof(string);
        }

        internal override void SerializeDataSpecific(object obj, BinaryWriter writer)
        {
            writer.Write((string)obj);
        }

        internal override object DeserializeDataSpecific(Type type, BinaryReader reader)
        {
            return reader.ReadString();
        }
    }
}