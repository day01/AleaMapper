using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AleaMapper.Tests.Models.EmitMapperModels
{
	public class BenchSource
	{
		public class Int1
		{
			public string str1 = "1";
			public string str2 = null;
			public int i = 10;
		}

		public class Int2
		{
			public Int1 i1 = new Int1();
			public Int1 i2 = new Int1();
			public Int1 i3 = new Int1();
			public Int1 i4 = new Int1();
			public Int1 i5 = new Int1();
			public Int1 i6 = new Int1();
			public Int1 i7 = new Int1();
		}

		public Int2 i1 = new Int2();
		public Int2 i2 = new Int2();
		public Int2 i3 = new Int2();
		public Int2 i4 = new Int2();
		public Int2 i5 = new Int2();
		public Int2 i6 = new Int2();
		public Int2 i7 = new Int2();
		public Int2 i8 = new Int2();

		public int n2;
		public int n3;
		public int n4;
		public int n5;
		public int n6;
		public int n7;
		public int n8;
		public int n9;

		public string s1 = "1";
		public string s2 = "2";
		public string s3 = "3";
		public string s4 = "4";
		public string s5 = "5";
		public string s6 = "6";
		public string s7 = "7";

	}

	public class BenchDestination
	{
		public class Int1
		{
			public string str1;
			public string str2;
			public int i;
		}

		public class Int2
		{
			public Int1 i1;
			public Int1 i2;
			public Int1 i3;
			public Int1 i4;
			public Int1 i5;
			public Int1 i6;
			public Int1 i7;
		}

		public Int2 i1 { get; set; }
		public Int2 i2 { get; set; }
		public Int2 i3 { get; set; }
		public Int2 i4 { get; set; }
		public Int2 i5 { get; set; }
		public Int2 i6 { get; set; }
		public Int2 i7 { get; set; }
		public Int2 i8 { get; set; }

		public int n2 = 2;
		public int n3 = 3;
		public int n4 = 4;
		public int n5 = 5;
		public int n6 = 6;
		public int n7 = 7;
		public int n8 = 8;
		public int n9 = 9;

		public string s1;
		public string s2;
		public string s3;
		public string s4;
		public string s5;
		public string s6;
		public string s7;
	}

}
