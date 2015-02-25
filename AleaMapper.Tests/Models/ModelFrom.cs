using System;

namespace AleaMapper.Tests.Models
{
	public class ModelFrom
	{
		public int IntFrom;

		public string StringFrom { get; set; }

		public Type TypeFrom { get; set; }

		public bool BoolFrom { get; set; }

		public int? IntNullFrom { get; set; }

		public bool? BoolNullFrom { get; set; }
	}
	public class ModelTo
	{
		public int IntTo;

		public string StringTo { get; set; }

		public Type TypeTo { get; set; }

		public bool BoolTo { get; set; }

		public int? IntNullTo { get; set; }

		public bool? BoolNullTo { get; set; }
	}
}