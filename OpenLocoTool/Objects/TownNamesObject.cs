﻿
using System.ComponentModel;
using OpenLocoTool.DatFileParsing;
using OpenLocoTool.Headers;

namespace OpenLocoTool.Objects
{
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[LocoStructSize(0x04)]
	public record TownNamesUnk(
		[property: LocoStructOffset(0x00)] uint8_t Count,
		[property: LocoStructOffset(0x01)] uint8_t Fill,
		[property: LocoStructOffset(0x02)] uint16_t Offset
	) : ILocoStruct;

	[TypeConverter(typeof(ExpandableObjectConverter))]
	[LocoStructSize(0x1A)]
	[LocoStructType(ObjectType.TownNames)]
	[LocoStringTable("Name")]
	public class TownNamesObject(TownNamesUnk[] unks)
		: ILocoStruct, ILocoStructVariableData
	{
		//[property: LocoStructOffset(0x00), LocoString, Browsable(false)] string_id Name,
		[LocoStructOffset(0x02), LocoArrayLength(6)] public TownNamesUnk[] unks { get; set; } = unks;

		byte[] tempUnkVariableData;

		public ReadOnlySpan<byte> Load(ReadOnlySpan<byte> remainingData)
		{
			// town names is interesting - loco has not RE'd the the whole object and there are no graphics, so we just
			// skip the rest of the data/object
			tempUnkVariableData = remainingData.ToArray();
			remainingData = remainingData[remainingData.Length..];

			return remainingData;
		}

		public ReadOnlySpan<byte> Save()
			=> tempUnkVariableData;
	}
}
