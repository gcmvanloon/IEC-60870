using System.IO;
using IEC60870.IE.Base;

namespace IEC60870.IE
{
    /// <summary>
    /// Represents a scaled value (SVA) information element.
    /// </summary>
    public class IeScaledValue : InformationElement
    {
        public int Value { get; private set; }

        /// <summary>
        /// Scaled value is a 16 bit integer (short) in the range from -32768 to 32767
        /// </summary>
        /// <param name="value">value in the range -32768 to 32767</param>
        public IeScaledValue(short value)
        {
            Value = value;
        }

        public IeScaledValue(BinaryReader reader)
        {
            Value = reader.ReadByte() | (reader.ReadByte() << 8);
        }

        public override int Encode(byte[] buffer, int i)
        {
            buffer[i++] = (byte)Value;
            buffer[i] = (byte)(Value >> 8);

            return 2;
        }

        public override string ToString()
        {
            return "Scaled value: " + Value;
        }
    }
}