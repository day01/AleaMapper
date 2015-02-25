using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Security;

namespace CIL
{
	public interface ITokenResolver
	{
		FieldInfo AsField(int token);

		MemberInfo AsMember(int token);

		MethodBase AsMethod(int token);

		byte[] AsSignature(int token);

		string AsString(int token);

		Type AsType(int token);
	}
	
	public class DynamicScopeTokenResolver : ITokenResolver
	{
		private static readonly FieldInfo GenmethFi1, GenmethFi2;
		private static readonly Type GenMethodInfoType;
		private static readonly FieldInfo ScopeFi;
		private static readonly PropertyInfo SIndexer;
		private static readonly FieldInfo VarargFi1, VarargFi2;
		private static readonly Type VarArgMethodType;
		private readonly object _scope = null;

		static DynamicScopeTokenResolver()
		{
			const BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;

			var dynamicGeneratorType = Type.GetType("System.Reflection.Emit.DynamicILGenerator");
			var dynamicScopeType = Type.GetType("System.Reflection.Emit.DynamicScope");
			VarArgMethodType = Type.GetType("System.Reflection.Emit.VarArgMethod");
			GenMethodInfoType = Type.GetType("System.Reflection.Emit.GenericMethodInfo");

			if (dynamicGeneratorType == null || dynamicScopeType == null || VarArgMethodType == null | GenMethodInfoType == null)
				throw new VerificationException("Version of Emit actual using is not supported by this viewer");

			SIndexer = dynamicScopeType.GetProperty("Item", bindingFlags);
			ScopeFi = dynamicGeneratorType.GetField("m_scope", bindingFlags);

			VarargFi1 = VarArgMethodType.GetField("m_method", bindingFlags);
			VarargFi2 = VarArgMethodType.GetField("m_signature", bindingFlags);

			GenmethFi1 = GenMethodInfoType.GetField("m_method", bindingFlags);
			GenmethFi2 = GenMethodInfoType.GetField("m_context", bindingFlags);
		}

		public DynamicScopeTokenResolver(DynamicMethod dm)
		{
			_scope = ScopeFi.GetValue(dm.GetILGenerator());
		}

		internal object this[int token]
		{
			get { return SIndexer.GetValue(_scope, new object[] { token }); }
		}

		public FieldInfo AsField(int token)
		{
			return FieldInfo.GetFieldFromHandle((RuntimeFieldHandle)this[token]);
		}

		public MemberInfo AsMember(int token)
		{
			if ((token & 0x02000000) == 0x02000000)
				return AsType(token);
			if ((token & 0x06000000) == 0x06000000)
				return AsMethod(token);
			if ((token & 0x04000000) == 0x04000000)
				return AsField(token);

			Debug.Assert(false, string.Format("unexpected token type: {0:x8}", token));
			return null;
		}

		public MethodBase AsMethod(int token)
		{
			if (this[token] is DynamicMethod)
				return this[token] as DynamicMethod;

			if (this[token] is RuntimeMethodHandle)
				return MethodBase.GetMethodFromHandle((RuntimeMethodHandle)this[token]);

			if (this[token].GetType() == GenMethodInfoType)
				return MethodBase.GetMethodFromHandle(
					(RuntimeMethodHandle)GenmethFi1.GetValue(this[token]),
					(RuntimeTypeHandle)GenmethFi2.GetValue(this[token]));

			if (this[token].GetType() == VarArgMethodType)
				return (MethodInfo)VarargFi1.GetValue(this[token]);

			Debug.Assert(false, string.Format("unexpected type: {0}", this[token].GetType()));
			return null;
		}

		public byte[] AsSignature(int token)
		{
			return this[token] as byte[];
		}

		public String AsString(int token)
		{
			return this[token] as string;
		}

		public Type AsType(int token)
		{
			return Type.GetTypeFromHandle((RuntimeTypeHandle)this[token]);
		}
	}
}