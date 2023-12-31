﻿using OpenLocoTool.DatFileParsing;
using OpenLocoTool.Headers;

namespace OpenLocoTool
{
	public static class SObjectManager
	{
		static readonly Dictionary<ObjectType, List<ILocoObject>> Objects = [];

		static SObjectManager()
		{
			foreach (var v in Enum.GetValues(typeof(ObjectType)))
			{
				Objects.Add((ObjectType)v, []);
			}
		}

		public static List<T> Get<T>(ObjectType type)
			where T : ILocoStruct => Objects[type].Select(a => a.Object).Cast<T>().ToList();

		public static void Add<T>(T obj) where T : ILocoObject
			=> Objects[ObjectAttributes.ObjectType(obj.Object)].Add(obj);
	}

	// unused for now
	//public class ObjectManager
	//{
	//	readonly Dictionary<ObjectType, List<ILocoObject>> Objects = new();

	//	public ObjectManager()
	//	{
	//		foreach (var v in Enum.GetValues(typeof(ObjectType)))
	//		{
	//			Objects.Add((ObjectType)v, new List<ILocoObject>());
	//		}
	//	}

	//	public List<T> Get<T>(ObjectType type)
	//		where T : ILocoStruct => Objects[type].Select(a => a.Object).Cast<T>().ToList();

	//	public void Add<T>(T obj) where T : ILocoObject
	//		=> Objects[obj.S5Header.ObjectType].Add(obj);
	//}
}
