﻿using System.ComponentModel;
using OpenLocoTool.Headers;

namespace OpenLocoTool.DatFileParsing
{
	[TypeConverter(typeof(ExpandableObjectConverter))]
	public interface ILocoObject
	{
		ILocoStruct Object { get; set; }
		StringTable StringTable { get; set; }
		List<G1Element32>? G1Elements { get; set; }
	}
}
