using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace CIL
{
	public static class Extension
	{
		public static byte[] PublicBakeByteArray(this ILGenerator il)
		{
			return Reader.MethodBakeByteArray.Invoke(il, null) as byte[];
		}
	}

	public class Reader : IEnumerable<IInstruction>
	{
		public static readonly MethodInfo MethodBakeByteArray = typeof(ILGenerator).GetMethod("BakeByteArray", BindingFlags.NonPublic | BindingFlags.Instance);
		private const int OpCodeByteLimit = 0x100;
		private static readonly FieldInfo FiLen = typeof(ILGenerator).GetField("m_length", BindingFlags.NonPublic | BindingFlags.Instance);
		private static readonly FieldInfo FiStream = typeof(ILGenerator).GetField("m_ILStream", BindingFlags.NonPublic | BindingFlags.Instance);
		private static readonly Dictionary<int, OpCode> OneByteOpCodes = new Dictionary<int, OpCode>();
		private static readonly OpCode[] TwoByteOpCodes = new OpCode[OpCodeByteLimit];
		private readonly Byte[] _byteArray;
		private readonly ITokenResolver _resolver;
		private int _position;

		static Reader()
		{
			var opCodes =
				typeof (OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
					.Select(fi => (OpCode) fi.GetValue(null))
					.ToList();

			foreach (var opCode in opCodes)
			{
				if (opCode.Value < OpCodeByteLimit)
				{
					OneByteOpCodes[opCode.Value] = opCode;
				}
				else if ((opCode.Value & 0xff00) == 0xfe00)
				{
					TwoByteOpCodes[opCode.Value & 0xff] = opCode;
				}
			}
		}

		public string ThrowUp
		{
			get
			{
				var list = this.ToList();
				var ret = list.Aggregate("", (current, instruction) => current + (instruction.ToString() + "\n"));
				return ret;
			}
		}

		public Reader(DynamicMethod dynamicMethod)
		{
			_resolver = new DynamicScopeTokenResolver(dynamicMethod);
			var ilgen = dynamicMethod.GetILGenerator();

			FixupSuccess = false;
			try
			{
				_byteArray = ilgen.PublicBakeByteArray();
				FixupSuccess = true;
			}
			catch (TargetInvocationException)
			{
				var length = (int)FiLen.GetValue(ilgen);
				_byteArray = new byte[length];
				Array.Copy((byte[])FiStream.GetValue(ilgen), _byteArray, length);
			}
			_position = 0;
		}

		public bool FixupSuccess { get; private set; }

		public IEnumerator<IInstruction> GetEnumerator()
		{
			while (_position < _byteArray.Length)
				yield return Next();

			_position = 0;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		private IInstruction Next()
		{
			var offset = _position;
			OpCode opCode;
			int token;

			// read first 1 or 2 bytes as opCode
			var code = ReadByte();
			if (code != 0xFE)
			{
				opCode = OneByteOpCodes[code];
			}
			else
			{
				code = ReadByte();
				opCode = TwoByteOpCodes[code];
			}

			switch (opCode.OperandType)
			{
				case OperandType.InlineNone:
					return new InlineNoneInstruction(_resolver, offset, opCode);

				//The operand is an 8-bit integer branch target.
				case OperandType.ShortInlineBrTarget:
					var shortDelta = ReadSByte();
					return new ShortInlineBrTargetInstruction(_resolver, offset, opCode, shortDelta);

				//The operand is a 32-bit integer branch target.
				case OperandType.InlineBrTarget:
					var delta = ReadInt32();
					return new InlineBrTargetInstruction(_resolver, offset, opCode, delta);

				//The operand is an 8-bit integer: 001F  ldc.i4.s, FE12  unaligned.
				case OperandType.ShortInlineI:
					var int8 = ReadByte();
					return new ShortInlineIInstruction(_resolver, offset, opCode, int8);

				//The operand is a 32-bit integer.
				case OperandType.InlineI:
					var int32 = ReadInt32();
					return new InlineIInstruction(_resolver, offset, opCode, int32);

				//The operand is a 64-bit integer.
				case OperandType.InlineI8:
					var int64 = ReadInt64();
					return new InlineI8Instruction(_resolver, offset, opCode, int64);

				//The operand is a 32-bit IEEE floating point number.
				case OperandType.ShortInlineR:
					var float32 = ReadSingle();
					return new ShortInlineRInstruction(_resolver, offset, opCode, float32);

				//The operand is a 64-bit IEEE floating point number.
				case OperandType.InlineR:
					var float64 = ReadDouble();
					return new InlineRInstruction(_resolver, offset, opCode, float64);

				//The operand is an 8-bit integer containing the ordinal of a local variable or an argument
				case OperandType.ShortInlineVar:
					var index8 = ReadByte();
					return new ShortInlineVarInstruction(_resolver, offset, opCode, index8);

				//The operand is 16-bit integer containing the ordinal of a local variable or an argument.
				case OperandType.InlineVar:
					var index16 = ReadUInt16();
					return new InlineVarInstruction(_resolver, offset, opCode, index16);

				//The operand is a 32-bit metadata string token.
				case OperandType.InlineString:
					token = ReadInt32();
					return new InlineStringInstruction(_resolver, offset, opCode, token);

				//The operand is a 32-bit metadata signature token.
				case OperandType.InlineSig:
					token = ReadInt32();
					return new InlineSigInstruction(_resolver, offset, opCode, token);

				//The operand is a 32-bit metadata token.
				case OperandType.InlineMethod:
					token = ReadInt32();
					return new InlineMethodInstruction(_resolver, offset, opCode, token);

				//The operand is a 32-bit metadata token.
				case OperandType.InlineField:
					token = ReadInt32();
					return new InlineFieldInstruction(_resolver, offset, opCode, token);

				//The operand is a 32-bit metadata token.
				case OperandType.InlineType:
					token = ReadInt32();
					return new InlineTypeInstruction(_resolver, offset, opCode, token);

				//The operand is a FieldRef, MethodRef, or TypeRef token.
				case OperandType.InlineTok:
					token = ReadInt32();
					return new InlineTokInstruction(_resolver, offset, opCode, token);

				//The operand is the 32-bit integer argument to a switch instruction.
				case OperandType.InlineSwitch:
					var cases = ReadInt32();
					var deltas = new Int32[cases];
					for (var i = 0; i < cases; i++) deltas[i] = ReadInt32();
					return new InlineSwitchInstruction(_resolver, offset, opCode, deltas);

				default:
					throw new BadImageFormatException("unexpected OperandType " + opCode.OperandType);
			}
		}

		private Byte ReadByte()
		{
			return _byteArray[_position++];
		}

		private Double ReadDouble()
		{
			_position += 8; return BitConverter.ToDouble(_byteArray, _position - 8);
		}

		private Int32 ReadInt32()
		{
			_position += 4; return BitConverter.ToInt32(_byteArray, _position - 4);
		}

		private Int64 ReadInt64()
		{
			_position += 8; return BitConverter.ToInt64(_byteArray, _position - 8);
		}

		private SByte ReadSByte()
		{
			return (SByte)ReadByte();
		}

		private Single ReadSingle()
		{
			_position += 4; return BitConverter.ToSingle(_byteArray, _position - 4);
		}

		private UInt16 ReadUInt16()
		{
			_position += 2; return BitConverter.ToUInt16(_byteArray, _position - 2);
		}

		private UInt32 ReadUInt32()
		{
			_position += 4; return BitConverter.ToUInt32(_byteArray, _position - 4);
		}

		private UInt64 ReadUInt64()
		{
			_position += 8; return BitConverter.ToUInt64(_byteArray, _position - 8);
		}
	}
}