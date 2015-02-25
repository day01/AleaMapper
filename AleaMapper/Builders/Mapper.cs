using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;

namespace Tex.Builders
{
	public static class Mapper
	{
		private static readonly Dictionary<KeyValuePair<Type, Type>, Delegate> DelegateDict = new Dictionary<KeyValuePair<Type, Type>, Delegate>();
		private static readonly Dictionary<KeyValuePair<Type, Type>, object> WrapperDict = new Dictionary<KeyValuePair<Type, Type>, object>();

		public static Wrapper<TResult, TSource> CreateMap<TResult, TSource>()
			where TSource : class
			where TResult : class
		{
			var wrap = new Wrapper<TResult, TSource>();
			WrapperDict.Add(new KeyValuePair<Type, Type>(wrap.ResultType, wrap.SourceType), wrap);
			return wrap;
		}

		public static Wrapper<TResult, TSource> ForMember<TResult, TSource, T>(this Wrapper<TResult, TSource> wrap, Expression<Func<TResult, T>> resultMember, Expression<Action<MapFunction<TResult, TSource, T, T>>> methodCall)
			where TSource : class
			where TResult : class
		{
			var method = methodCall.Compile();
			var func = new MapFunction<TResult, TSource, T, T>(wrap, resultMember);
			method.Invoke(func);
			return wrap;
		}

		public static TResult Map<TResult, TSource>(TSource source) where TResult : class
		{
			Delegate del;
			if (DelegateDict.TryGetValue(new KeyValuePair<Type, Type>(typeof (TResult), typeof (TSource)), out del))
			{
				var dyn = del as Func<TSource, TResult>;
				var res =  dyn(source);
				return res;
			}
			throw new Exception(string.Format("Map with {0} - {1} not exist. At First imeplment map or set default settings", typeof(TResult), typeof(TSource)));
		}
		public static Func<TSource, TResult> Map<TResult, TSource>() where TResult : class
		{
			Delegate del;
			if (DelegateDict.TryGetValue(new KeyValuePair<Type, Type>(typeof(TResult), typeof(TSource)), out del))
			{
				return del as Func<TSource, TResult>;
			}
			throw new Exception(string.Format("Map with {0} - {1} not exist. At First imeplment map or set default settings", typeof(TResult), typeof(TSource)));
		}

		public static void Build()
		{
			foreach (var wrapDict in WrapperDict)
			{
				var key = wrapDict.Key;
				var del =
					typeof (IWrapper).GetMethod("Build").MakeGenericMethod(new[] {key.Value, key.Key}).Invoke(wrapDict.Value, null) as
						Delegate;
				DelegateDict.Add(key, del);
			}
		}
	}

	public class MapFunction<TResult, TSource, T, TG>
		where TSource : class
		where TResult : class
		where T : TG
	{
		private readonly Wrapper<TResult, TSource> _wrapper;
		private readonly Expression<Func<TResult, T>> _resultMember;

		public MapFunction(Wrapper<TResult, TSource> wrapper, Expression<Func<TResult, T>> resultMember)
		{
			_wrapper = wrapper;
			_resultMember = resultMember;
		}
		public void MapFrom(Expression<Func<TSource, TG>> sourceMember)
		{
			_wrapper.MapFrom(_resultMember, sourceMember);
		}
		
		public void Ignore()
		{
			_wrapper.Ignore(_resultMember);
		}
	}
}
