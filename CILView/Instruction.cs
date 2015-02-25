using System;
using System.Reflection;
using System.Reflection.Emit;

namespace CIL
{
	public interface IInstruction
	{
		int OffsetPosition { get; }

		OpCode OpCode { get; }

		string ProcessedOperand { get; }

		string RawOperand { get; }

		IFormatProvider FormatProvider { get; }
	}

	public class InlineBrTargetInstruction : Instruction<int>
	{
		public InlineBrTargetInstruction(ITokenResolver resolver, int offsetPosition, OpCode opCode, int value)
			: base(resolver, offsetPosition, opCode, value)
		{
		}

		public override String ProcessedOperand { get { return FormatProvider.Label(TargetOffset); } }

		public override String RawOperand { get { return FormatProvider.Int32ToHex(InternalValue); } }

		public int TargetOffset { get { return OffsetPosition + InternalValue + 1 + 4; } }
	}

	public class InlineFieldInstruction : Instruction<int>
	{
		public InlineFieldInstruction(ITokenResolver resolver, int offsetPosition, OpCode opCode, int value)
			: base(resolver, offsetPosition, opCode, value)
		{
		}

		public FieldInfo Field
		{
			get { return Resolver.AsField(InternalValue); }
		}

		public override String ProcessedOperand { get { return Field + "/" + Field.DeclaringType; } }

		public override String RawOperand { get { return FormatProvider.Int32ToHex(InternalValue); } }
	}

	public class InlineI8Instruction : Instruction<long>
	{
		public InlineI8Instruction(ITokenResolver resolver, int offsetPosition, OpCode opCode, long value)
			: base(resolver, offsetPosition, opCode, value)
		{
		}

		public override string ProcessedOperand { get { return InternalValue.ToString(); } }

		public override string RawOperand { get { return FormatProvider.Int64ToHex(InternalValue); } }
	}

	public class InlineIInstruction : Instruction<int>
	{
		public InlineIInstruction(ITokenResolver resolver, int offsetPosition, OpCode opCode, int value)
			: base(resolver, offsetPosition, opCode, value)
		{ }

		public override string ProcessedOperand { get { return InternalValue.ToString(); } }

		public override String RawOperand { get { return FormatProvider.Int32ToHex(InternalValue); } }
	}

	public class InlineMethodInstruction : Instruction<int>
	{
		public InlineMethodInstruction(ITokenResolver resolver, int offsetPosition, OpCode opCode, int value)
			: base(resolver, offsetPosition, opCode, value)
		{
		}

		public MethodBase Method
		{
			get { return Resolver.AsMethod(InternalValue); }
		}

		public override string ProcessedOperand
		{
			get
			{
				var meth = Method.ToString();
				meth = meth.Replace(meth.Split(' ')[0] + " ", "");
				return Method.DeclaringType + "::" + meth;
			}
		}

		public override string RawOperand { get { return FormatProvider.Int32ToHex(InternalValue); } }
	}

	public class InlineNoneInstruction : Instruction<object>
	{
		public InlineNoneInstruction(ITokenResolver resolver, int offsetPosition, OpCode opCode)
			: base(resolver, offsetPosition, opCode) { }
	}

	public class InlineRInstruction : Instruction<double>
	{
		public InlineRInstruction(ITokenResolver resolver, int offsetPosition, OpCode opCode, double value)
			: base(resolver, offsetPosition, opCode, value)
		{
		}

		public override string RawOperand { get { return InternalValue.ToString(); } }
	}

	public class InlineSigInstruction : Instruction<int>
	{
		private byte[] _signature;

		public InlineSigInstruction(ITokenResolver resolver, int offsetPosition, OpCode opCode, int value)
			: base(resolver, offsetPosition, opCode, value)
		{
		}

		public override string ProcessedOperand { get { return FormatProvider.SigByteArrayToString(Signature); } }

		public override string RawOperand { get { return FormatProvider.Int32ToHex(InternalValue); } }

		public byte[] Signature
		{
			get { return _signature ?? (_signature = Resolver.AsSignature(InternalValue)); }
		}
	}

	public class InlineStringInstruction : Instruction<int>
	{
		public InlineStringInstruction(ITokenResolver resolver, int offsetPosition, OpCode opCode, int value)
			: base(resolver, offsetPosition, opCode, value)
		{
		}

		public override string ProcessedOperand { get { return FormatProvider.EscapedString(Resolver.AsString(InternalValue)); } }

		public override string RawOperand { get { return FormatProvider.Int32ToHex(InternalValue); } }
	}

	public class InlineSwitchInstruction : Instruction<int[]>
	{
		private int[] m_targetOffsets;

		public InlineSwitchInstruction(ITokenResolver resolver, int offsetPosition, OpCode opCode, int[] value)
			: base(resolver, offsetPosition, opCode, value)
		{
		}

		public override string ProcessedOperand { get { return FormatProvider.MultipleLabels(TargetOffsets); } }

		public override string RawOperand { get { return "..."; } }

		public int[] TargetOffsets
		{
			get
			{
				if (m_targetOffsets != null) return m_targetOffsets;
				var cases = InternalValue.Length;
				var itself = 1 + 4 + 4 * cases;
				m_targetOffsets = new int[cases];
				for (var i = 0; i < cases; i++)
					m_targetOffsets[i] = OffsetPosition + InternalValue[i] + itself;
				return m_targetOffsets;
			}
		}
	}

	public class InlineTokInstruction : Instruction<int>
	{
		public InlineTokInstruction(ITokenResolver resolver, int offsetPosition, OpCode opCode, int value)
			: base(resolver, offsetPosition, opCode, value)
		{
		}

		public MemberInfo Member
		{
			get { return Resolver.AsMember(InternalValue); }
		}

		public override string ProcessedOperand { get { return Member + "/" + Member.DeclaringType; } }

		public override string RawOperand { get { return FormatProvider.Int32ToHex(InternalValue); } }
	}

	public class InlineTypeInstruction : Instruction<int>
	{
		public InlineTypeInstruction(ITokenResolver resolver, int offsetPosition, OpCode opCode, int value)
			: base(resolver, offsetPosition, opCode, value)
		{
		}

		public override string ProcessedOperand { get { return Type.ToString(); } }

		public override string RawOperand { get { return FormatProvider.Int32ToHex(InternalValue); } }

		public Type Type
		{
			get { return Resolver.AsType(InternalValue); }
		}
	}

	public class InlineVarInstruction : Instruction<int>
	{
		public InlineVarInstruction(ITokenResolver resolver, int offsetPosition, OpCode opCode, int value)
			: base(resolver, offsetPosition, opCode, value)
		{
		}

		public override string ProcessedOperand { get { return FormatProvider.Argument(InternalValue); } }

		public override string RawOperand { get { return FormatProvider.Int16ToHex(InternalValue); } }
	}

	public abstract class Instruction<T> : IInstruction
	{
		public IFormatProvider FormatProvider { get; private set; }

		internal ITokenResolver Resolver;

		internal Instruction(ITokenResolver resolver, int offsetPosition, OpCode opCode)
		{
			Resolver = resolver;
			OffsetPosition = offsetPosition;
			OpCode = opCode;
			FormatProvider = new DefaultFormatProvider();
		}

		internal Instruction(ITokenResolver resolver, int offsetPosition, OpCode opCode, T value)
			: this(resolver, offsetPosition, opCode)
		{
			InternalValue = value;
		}

		public T InternalValue { get; private set; }

		public int OffsetPosition { get; private set; }

		public OpCode OpCode { get; private set; }

		public virtual string ProcessedOperand { get { return RawOperand; } }

		public virtual string RawOperand { get { return string.Empty; } }

		public void SetFormatProvider(IFormatProvider formatProvider)
		{
			FormatProvider = formatProvider;
		}

		public override string ToString()
		{
			return FormatProvider.Format(this);
		}
	}

	public class ShortInlineBrTargetInstruction : Instruction<int>
	{
		public ShortInlineBrTargetInstruction(ITokenResolver resolver, int offsetPosition, OpCode opCode, int value)
			: base(resolver, offsetPosition, opCode, value)
		{
		}

		public override string ProcessedOperand { get { return FormatProvider.Label(TargetOffset); } }

		public override string RawOperand { get { return FormatProvider.Int8ToHex(InternalValue); } }

		public int TargetOffset { get { return OffsetPosition + InternalValue + 1 + 1; } }
	}

	public class ShortInlineIInstruction : Instruction<byte>
	{
		public ShortInlineIInstruction(ITokenResolver resolver, int offsetPosition, OpCode opCode, byte value)
			: base(resolver, offsetPosition, opCode, value)
		{
		}

		public override string ProcessedOperand { get { return InternalValue.ToString(); } }

		public override string RawOperand { get { return FormatProvider.Int8ToHex(InternalValue); } }
	}

	public class ShortInlineRInstruction : Instruction<float>
	{
		public ShortInlineRInstruction(ITokenResolver resolver, int offsetPosition, OpCode opCode, float value)
			: base(resolver, offsetPosition, opCode, value)
		{
		}

		public override string RawOperand { get { return InternalValue.ToString(); } }
	}

	public class ShortInlineVarInstruction : Instruction<byte>
	{
		public ShortInlineVarInstruction(ITokenResolver resolver, int offsetPosition, OpCode opCode, byte value)
			: base(resolver, offsetPosition, opCode, value)
		{
		}

		public override string ProcessedOperand { get { return FormatProvider.Argument(InternalValue); } }

		public override string RawOperand { get { return FormatProvider.Int8ToHex(InternalValue); } }
	}
}