using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Tex.Builders.Map;

namespace Tex.Builders
{
	public interface IWrapper<TSource, TResult>
		where TSource : class
		where TResult : class
	{
		Type ResultType { get; set; }

		Type SourceType { get; set; }

		Wrapper<TResult, TSource>.CopyDelegate Build();
	}

	public class WrapperBase
	{
		//public delegate void CopyDelegate<TS, TR>(TS source, out TR result);
	}

	public class Wrapper<TResult, TSource> : IWrapper<TSource, TResult>
		where TSource : class
		where TResult : class
	{
		public delegate void CopyDelegate(TSource source, out TResult result);

		private readonly ParameterExpression _instance;
		private readonly ParameterExpression _result;
		private readonly ConstructorInfo _resultConsturctor;
		private readonly List<MapMember> _mapMembers;

		public Type ResultType { get; set; }

		public Type SourceType { get; set; }

		public Wrapper()
			: this(typeof(TResult).GetConstructor(Type.EmptyTypes))
		{
		}

		private Wrapper(ConstructorInfo ctor)
		{
			_mapMembers = new List<MapMember>();
			var sourceMembers = new List<MemberInfo>();
			var destinationMembers = new List<MemberInfo>();

			SourceType = typeof(TSource);
			ResultType = typeof(TResult);

			_result = Expression.Parameter(ResultType.MakeByRefType(), "result");
			_instance = Expression.Parameter(SourceType, "instance");

			sourceMembers.AddRange(SourceType.GetFields());
			destinationMembers.AddRange(ResultType.GetFields());
			sourceMembers.AddRange(SourceType.GetProperties().Where(x => x.CanRead));
			destinationMembers.AddRange(ResultType.GetProperties().Where(x => x.CanWrite));

			_resultConsturctor = ctor;
			if (_resultConsturctor == null)
			{
				throw new Exception("Default constructor cannot have parameters");
			}
			foreach (var member in destinationMembers)
			{
				var field = member as FieldInfo;
				var prop = member as PropertyInfo;
				Type type;
				if (field != null)
					type = field.FieldType;
				else if (prop != null)
					type = prop.PropertyType;
				else
					throw new Exception(string.Format("Wrong type of memeber : {0}", member.Name));

				var memberInfo = sourceMembers.FirstOrDefault(x => x.Name == member.Name && ((x is FieldInfo && ((FieldInfo)x).FieldType == type) || (x is PropertyInfo && ((PropertyInfo)x).PropertyType == type)));
				if (memberInfo != null)
				{
					_mapMembers.Add(new MapMember(memberInfo, member));
					continue;
				}
				_mapMembers.Add(new MapMember(member));
			}
		}

		public void MapFrom<T, TG>(Expression<Func<TResult, T>> resultMember, Expression<Func<TSource, TG>> sourceMember) where T : TG
		{
			var member = resultMember.Body as MemberExpression;
			if (member != null)
			{
				var map = _mapMembers.FirstOrDefault(x => x.MemberDestination == member.Member);
				if (map == null)
					throw new Exception(string.Format("Result parameter {0} is not member of {1}", member.Member, typeof(TResult)));
				map.MapExpression = sourceMember.Body;
				map.StateOfMapMember = StateOfMapMember.PrivateMap;
			}
		}

		public void WithResolver<T, TG>(Expression<Func<TResult, T>> resultMember, Func<TSource, TG> sourceResolver) where T : TG
		{
			//Niezle mozna wykorzystac
			//var dec = ExpressiveEngine.GetDecompiler();
			//var lmb = dec.Decompile(sourceResolver.Method);
			var member = resultMember.Body as MemberExpression;
			if (member != null)
			{
				var map = _mapMembers.FirstOrDefault(x => x.MemberDestination == member.Member);
				if (map == null)
					throw new Exception(string.Format("Result parameter {0} is not member of {1}", member.Member, typeof(TResult)));
				if (!sourceResolver.Method.IsStatic)
					throw new Exception(string.Format("Func resolver should be STATIC!"));

				var callExp = Expression.Call(sourceResolver.Method, _instance);
				map.MapExpression = callExp;
				map.StateOfMapMember = StateOfMapMember.PrivateMethodMap;
			}
		}

		public void Ignore<T>(Expression<Func<TResult, T>> resultMember)
		{
			var member = resultMember.Body as MemberExpression;
			if (member != null)
			{
				var map = _mapMembers.FirstOrDefault(x => x.MemberDestination == member.Member);
				if (map == null)
					return;

				map.StateOfMapMember = StateOfMapMember.Ignore;
			}
		}

		public CopyDelegate Build()
		{
			var exprList = new List<Expression>
			{
				Expression.Assign(_result, Expression.New(_resultConsturctor))
			};

			foreach (var mapMember in _mapMembers)
			{
				switch (mapMember.StateOfMapMember)
				{
					case StateOfMapMember.Default:
						exprList.Add(GenerateDefaultExpression(mapMember.MemberDestination, mapMember.MemberSource));
						break;

					case StateOfMapMember.PrivateMethodMap:
					case StateOfMapMember.PrivateMap:
						exprList.Add(GeneratePrivateExpression(mapMember.MemberDestination, mapMember.MapExpression));
						break;
				}
			}
			//exprList.Add(_result);

			Expression block = Expression.Block(exprList);
			if (block.CanReduce)
				block = block.Reduce();
			var result = Expression.Lambda<CopyDelegate>(block, _instance, _result).Compile();
			return result;
		}

		public Expression GenerateDefaultExpression(MemberInfo field, MemberInfo memberFrom)
		{
			var get = GetterExpression(memberFrom);
			var set = SetterExpression(field);
			var expr = Expression.Assign(set, get);
			return expr;
		}

		public Expression GeneratePrivateExpression(MemberInfo field, Expression getExpression)
		{
			var set = SetterExpression(field);

			var get = new RewriterVisitor(_instance).Visit(getExpression);
			var expr = Expression.Assign(set, get);
			return expr;
		}

		private Expression GetterExpression(MemberInfo memberInfo)
		{
			var setter = Expression.PropertyOrField(_instance, memberInfo.Name);
			return setter;
		}

		private Expression SetterExpression(MemberInfo memberInfo)
		{
			var setter = Expression.PropertyOrField(_result, memberInfo.Name);
			return setter;
		}

		private class RewriterVisitor : ExpressionVisitor
		{
			private readonly ParameterExpression _parameterExpression;

			public RewriterVisitor(ParameterExpression parameterExpression)
			{
				_parameterExpression = parameterExpression;
			}

			protected override Expression VisitParameter(ParameterExpression node)
			{
				return _parameterExpression;
			}
		}

		//private void PreProcess()
		//{
		//	var a = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("",new Version(1,1)), "", ModuleKind.Dll);

		//}
	}
}