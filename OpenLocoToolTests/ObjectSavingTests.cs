﻿using NUnit.Framework;
using OpenLocoTool.DatFileParsing;
using OpenLocoTool.Headers;
using OpenLocoTool.Objects;
using OpenLocoToolCommon;

namespace OpenLocoToolTests
{
	[TestFixture]
	public class ObjectSavingTests
	{
		[Test]
		public void WriteLocoStruct()
		{
			// arrange
			const string testFile = "Q:\\Steam\\steamapps\\common\\Locomotion\\ObjData\\SIGC3.DAT";
			var fileSize = new FileInfo(testFile).Length;
			var logger = new Logger();
			var loaded = SawyerStreamReader.LoadFull(testFile);

			// load data in raw bytes for test
			ReadOnlySpan<byte> fullData = SawyerStreamReader.LoadBytesFromFile(testFile);

			// make openlocotool useful objects
			var s5Header = S5Header.Read(fullData[0..S5Header.StructLength]);
			var remainingData = fullData[S5Header.StructLength..];

			var objectHeader = ObjectHeader.Read(remainingData[0..ObjectHeader.StructLength]);
			remainingData = remainingData[ObjectHeader.StructLength..];

			var originalEncodedData = remainingData.ToArray();
			var decodedData = SawyerStreamReader.Decode(objectHeader.Encoding, originalEncodedData);
			remainingData = decodedData;

			var originalObjectData = decodedData[..ObjectAttributes.StructSize<TrainSignalObject>()];

			// act
			var bytes = ByteWriter.WriteLocoStruct(loaded.LocoObject.Object);

			// assert
			CollectionAssert.AreEqual(originalObjectData, bytes.ToArray());
		}

		[Test]
		public void Industry()
		{
			// arrange
			const string testFile = "Q:\\Steam\\steamapps\\common\\Locomotion\\ObjData\\BREWERY.DAT";
			var fileSize = new FileInfo(testFile).Length;
			var logger = new Logger();
			var loaded = SawyerStreamReader.LoadFull(testFile);

			// load data in raw bytes for test
			ReadOnlySpan<byte> fullData = SawyerStreamReader.LoadBytesFromFile(testFile);

			// make openlocotool useful objects
			var s5Header = S5Header.Read(fullData[0..S5Header.StructLength]);
			var remainingData = fullData[S5Header.StructLength..];

			var objectHeader = ObjectHeader.Read(remainingData[0..ObjectHeader.StructLength]);
			remainingData = remainingData[ObjectHeader.StructLength..];

			var originalEncodedData = remainingData.ToArray();
			var decodedData = SawyerStreamReader.Decode(objectHeader.Encoding, originalEncodedData);
			remainingData = decodedData;

			var originalObjectData = decodedData[..ObjectAttributes.StructSize<TrainSignalObject>()];

			// act
			var bytes = ByteWriter.WriteLocoStruct(loaded.LocoObject.Object);

			// assert
			CollectionAssert.AreEqual(originalObjectData, bytes.ToArray());
		}
	}
}