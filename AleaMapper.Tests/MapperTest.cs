using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AleaMapper.Tests.Models;
using AleaMapper.Tests.Models.EmitMapperModels;
using AutoMapper;
using NUnit.Framework;
using Tex.Builders;
using Mapper = Tex.Builders.Mapper;

namespace AleaMapper.Tests
{
	[TestFixture]
	public class MapperTest
	{
		[Test]
		public void MapperTimeTest()
		{
			const int hm = 100000;
			var from = new ModelFrom();
			var sw = new Stopwatch();
			sw.Start();
			AutoMapper.Mapper.CreateMap<ModelFrom, ModelTo>()
				.ForMember(x => x.BoolNullTo, opt => opt.MapFrom(x => x.BoolNullFrom))
				.ForMember(x => x.BoolTo, opt => opt.MapFrom(x => x.BoolFrom))
				.ForMember(x => x.IntNullTo, opt => opt.MapFrom(x => x.IntNullFrom))
				.ForMember(x => x.IntTo, opt => opt.MapFrom(x => x.BoolFrom ? 1 : 70))
				.ForMember(x => x.StringTo, opt => opt.MapFrom(x => x.StringFrom))
				.ForMember(x => x.TypeTo, opt => opt.MapFrom(x => x.TypeFrom));
			AutoMapper.Mapper.CreateMap<BenchSource, BenchDestination>();

			sw.Stop();
			Console.WriteLine("AutoMapper prepare test Time in ms : {0} ms", sw.ElapsedMilliseconds);

			sw.Reset();
			sw.Start();
			Mapper.CreateMap<ModelTo, ModelFrom>()
				.ForMember(x => x.BoolNullTo, opt => opt.MapFrom(x => x.BoolNullFrom))
				.ForMember(x => x.BoolTo, opt => opt.MapFrom(x => x.BoolFrom))
				.ForMember(x => x.IntNullTo, opt => opt.MapFrom(x => x.IntNullFrom))
				.ForMember(x => x.IntTo, opt => opt.MapFrom(x => x.BoolFrom ? 1 : 70))
				.ForMember(x => x.StringTo, opt => opt.MapFrom(x => x.StringFrom))
				.ForMember(x => x.TypeTo, opt => opt.MapFrom(x => x.TypeFrom));
			Mapper.CreateMap<BenchDestination, BenchSource>();
			Mapper.Build();
			sw.Stop();
			Console.WriteLine("AleaMapper prepare test Time in ms : {0} ms", sw.ElapsedMilliseconds);
			sw.Reset();
			sw.Start();

			for (int i = 0; i < hm; i++)
				AutoMapper.Mapper.Map<ModelTo>(from);
			sw.Stop();
			Console.WriteLine("AutoMapper Test on {1} pcs Time in ms : {0}ms\n Time on one pcs : {2} Ticks", sw.ElapsedMilliseconds, hm, (sw.ElapsedTicks / hm));

			sw.Reset();
			sw.Start();
			for (int i = 0; i < hm; i++)
				HandmadeMap(from);
			sw.Stop();
			Console.WriteLine("Handmade Test on {1} pcs Time in ms : {0}ms\n Time on one pcs : {2} Ticks", sw.ElapsedMilliseconds, hm, (sw.ElapsedTicks / hm));

			sw.Reset();
			sw.Start();
			for (int i = 0; i < hm; i++)
				Mapper.Map<ModelTo, ModelFrom>(from);
			sw.Stop();
			Console.WriteLine("AleaMapper Test on {1} pcs Time in ms : {0}ms\n Time on one pcs : {2} Ticks", sw.ElapsedMilliseconds, hm, (sw.ElapsedTicks / hm));

			sw.Reset();

			Func<ModelFrom, ModelTo> dd;
			sw.Start();
			for (int i = 0; i < hm; i++)
				dd = Mapper.Map<ModelTo, ModelFrom>();
			sw.Stop();
			Console.WriteLine("AleaMapper Inline getter Test on {1} pcs Time in ms : {0}ms\n Time on one pcs :  {2} Ticks", sw.ElapsedMilliseconds, hm, (sw.ElapsedTicks / hm));

			sw.Reset();
			var del = Mapper.Map<ModelTo, ModelFrom>();
			sw.Start();
			for (int i = 0; i < hm; i++)
				del(from);
			sw.Stop();
			Console.WriteLine("AleaMapper Inline Test on {1} pcs Time in ms : {0}ms\n Time on one pcs : {2} Ticks", sw.ElapsedMilliseconds, hm, (sw.ElapsedTicks / hm));


			#region EmitMapper part test

			sw.Reset();
			sw.Start();
			var bench = new BenchSource();
			for (int i = 0; i < hm; i++)
				Mapper.Map<BenchDestination, BenchSource>(bench);
			sw.Stop();
			Console.WriteLine("AleaMapper Test on {1} pcs Time in ms : {0}ms\n Time on one pcs : {2} Ticks", sw.ElapsedMilliseconds, hm, (sw.ElapsedTicks / hm));

			#endregion
		}

		private ModelTo HandmadeMap(ModelFrom m)
		{
			var a = new ModelTo();
			a.BoolNullTo = m.BoolNullFrom;
			a.BoolTo = m.BoolFrom;
			a.IntNullTo = m.IntNullFrom;
			if (m.BoolFrom)
			{
				a.IntTo = 1;
			}
			else
			{
				a.IntTo = 70;
			}
			a.StringTo = m.StringFrom;
			a.TypeTo = m.TypeFrom;
			return a;
		}
	}
}
