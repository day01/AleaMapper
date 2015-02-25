using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Tex.Builders.Map;

namespace Tex.Builders
{
	public interface IWrapper
	{
		Type ResultType { get; set; }
		Type SourceType { get; set; }
		Func<TSource, TResult> Build<TSource, TResult>();
	}

	public class Wrapper<TResult, TSource>  : IWrapper
		where TSource : class
		where TResult : class
	{
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

			_result = Expression.Parameter(ResultType, "result");
			_instance = Expression.Parameter(SourceType, "instance");

			sourceMembers.AddRange(SourceType.GetFields());
			destinationMembers.AddRange(ResultType.GetFields());
			sourceMembers.AddRange(SourceType.GetProperties());
			destinationMembers.AddRange(ResultType.GetProperties());

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
					throw new Exception(string.Format("Result parameter is not member of {0}", typeof(TResult)));
				map.MapExpression = sourceMember.Body;
				map.StateOfMapMember = StateOfMapMember.PrivateMap;
			}
		}


		public void Ignore<T>(Expression<Func<TResult, T>> resultMember)
		{
			var member = resultMember.Body as MemberExpression;
			if (member != null)
			{
				var map = _mapMembers.FirstOrDefault(x => x.MemberDestination == member.Member);
				if (map == null)
					throw new Exception(string.Format("Result parameter is not member of {0}", typeof(TResult)));

				map.StateOfMapMember = StateOfMapMember.Ignore;
			}
		}
		Func<TS, TR> IWrapper.Build<TS, TR>()
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
					case StateOfMapMember.PrivateMap:
						exprList.Add(GeneratePrivateExpression(mapMember.MemberDestination, mapMember.MapExpression));
						break;
				}
			}
			exprList.Add(_result);

			Expression block = Expression.Block(new[] { _result }, exprList);
			if (block.CanReduce)
				block = block.Reduce();
			var result =  Expression.Lambda<Func<TS, TR>>(block, _instance).Compile();
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
	}
}
