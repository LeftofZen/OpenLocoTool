﻿
using System.ComponentModel;
using OpenLocoTool.DatFileParsing;
using OpenLocoTool.Headers;

namespace OpenLocoTool.Objects
{
    [TypeConverter(typeof(ExpandableObjectConverter))]
	public record WaterObject() : ILocoStruct
	{
		public ObjectType ObjectType => ObjectType.water;
		public int ObjectStructSize => 0xE;
		public static ILocoStruct Read(ReadOnlySpan<byte> data) => throw new NotImplementedException();
		public ReadOnlySpan<byte> Write() => throw new NotImplementedException();
	}
}
