using System;
using System.Linq.Expressions;
using System.Reflection.Emit;
using AleaMapper.Tests.Models;
using CIL;
using NUnit.Framework;
using Tex.Builders;

namespace AleaMapper.Tests
{
	[TestFixture]
	public class WrapperTest
	{
		[Test]
		public void TestExpressionInstace()
		{
			Mapper.CreateMap<ModelTo, ModelFrom>()
				.ForMember(x => x.IntTo, y => y.MapFrom(x => x.BoolFrom ? 1 : 70))
				.ForMember(x => x.StringTo, opt => opt.Ignore());
			
			Mapper.Build();
			var test = new ModelFrom();
			test.IntFrom = 7;
			test.StringFrom = " Testujem ! ";
			var a = Mapper.Map<ModelTo, ModelFrom>(test);

		}
	}
}