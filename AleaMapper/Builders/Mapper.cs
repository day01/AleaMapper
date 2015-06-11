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
			var key = new KeyValuePair<Type, Type>(wrap.ResultType, wrap.SourceType);
			if (WrapperDict.ContainsKey(key))
			{
				throw new Exception(string.Format("Map with type {0} on type {1} exist!\nPlease try do not add the same type of map.", key.Key.FullName, key.Value.FullName));
			}
			WrapperDict.Add(key, wrap);
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

		private delegate void CopyDelegate<TResult, in TSource>(TSource source, out TResult result);

		public static TResult Map<TResult, TSource>(TSource source) where TResult : class where TSource :class
		{
			Delegate del;
			if (DelegateDict.TryGetValue(new KeyValuePair<Type, Type>(typeof(TResult), typeof(TSource)), out del))
			{
				var dyn = del as Wrapper<TResult,TSource>.CopyDelegate;
				if(dyn == null)
					throw new Exception(string.Format("Delegate error on source type {0} and result type : {1}", typeof(TSource), typeof(TResult)));
				TResult result;
				dyn(source, out result);
				return result;
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
			throw new Exception(string.Format("Map with {0} - {1} not exist. At First implement map or set default settings", typeof(TResult), typeof(TSource)));
		}

		public static void Build()
		{
			foreach (var wrapDict in WrapperDict)
			{
				var key = wrapDict.Key;
				dynamic obj = wrapDict.Value;
				var del = obj.Build() as Delegate;
				if (DelegateDict.ContainsKey(key))
				{
					throw new Exception(string.Format("Map with type {0} on type {1} exist!\nPlease try do not add the same type of map.", key.Key.FullName, key.Value.FullName));
				}
				DelegateDict.Add(key, del);
			}
			WrapperDict.Clear();
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

		public void WithResolver(Func<TSource, TG> resolver)
		{
			_wrapper.WithResolver(_resultMember, resolver);
		}
	}
}
