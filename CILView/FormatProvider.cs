using System;
using System.Text;

namespace CIL
{
    public interface IFormatProvider
    {
		string Int64ToHex(long int32);
		string Int32ToHex(int int32);
        string Int16ToHex(int int16);
        string Int8ToHex(int int8);
        string Argument(int ordinal);
        string EscapedString(string str);
        string Label(int offset);
        string MultipleLabels(int[] offsets);
        string SigByteArrayToString(byte[] sig);
        string Format(IInstruction ilInstruction);
    }

    public class DefaultFormatProvider : IFormatProvider
    {
		public virtual string Int64ToHex(long value)
		{
			return value.ToString("X16");
		}
        public virtual string Int32ToHex(int value)
        {
	        return value.ToString("X8");
        }

	    public virtual string Int16ToHex(int int16)
        {
            return int16.ToString("X4");
        }

        public virtual string Int8ToHex(int int8)
        {
            return int8.ToString("X2");
        }

        public virtual string Argument(int ordinal)
        {
            return string.Format("V_{0}", ordinal);
        }

        public virtual string Label(int offset)
        {
            return string.Format("IL_{0:x4}", offset);
        }

        public virtual string MultipleLabels(int[] offsets)
        {
            var sb = new StringBuilder();
            var length = offsets.Length;
            for (var i = 0; i < length; i++)
            {
	            sb.AppendFormat(i == 0 ? "(" : ", ");
	            sb.Append(Label(offsets[i]));
            }
	        sb.AppendFormat(")");
            return sb.ToString();
        }

        public virtual string EscapedString(string str)
        {
            var length = str.Length;
            var sb = new StringBuilder(length * 2);
            for (var i = 0; i < length; i++)
            {
	            var ch = str[i];
	            switch (ch)
	            {
		            case '\t':
			            sb.Append("\\t");
			            break;
		            case '\n':
			            sb.Append("\\n");
			            break;
		            case '\r':
			            sb.Append("\\r");
			            break;
		            case '\"':
			            sb.Append("\\\"");
			            break;
		            case '\\':
			            sb.Append("\\");
			            break;
		            default:
			            if (ch < 0x20 || ch >= 0x7f) sb.AppendFormat("\\u{0:x4}", (int)ch);
			            else sb.Append(ch);
			            break;
	            }
            }
	        return "\"" + sb + "\"";
        }

        public virtual string SigByteArrayToString(byte[] sig)
        {
            var sb = new StringBuilder();
            var length = sig.Length;
            for (var i = 0; i < length; i++)
            {
	            sb.AppendFormat(i == 0 ? "SIG [" : " ");
	            sb.Append(Int8ToHex(sig[i]));
            }
	        sb.AppendFormat("]");
            return sb.ToString();
        }

        public virtual string Format(IInstruction instruction)
        {
            string processed;
            try
            {
				processed = instruction.ProcessedOperand;
            }
            catch (Exception ex)
            {
                processed = "!" + EscapedString(ex.Message) + "!";
            }
            return String.Format("IL_{0:x4}: {1,-10} {2}",
				instruction.OffsetPosition,
				instruction.OpCode.Name,
                processed
            );
        }
    }
}