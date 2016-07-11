using System;
using System.IO;
using IEC60870.IE.Base;

namespace IEC60870.IE
{
    /// <summary>
    /// Represents a normalized value (NVA) information element.
    /// </summary>
    public class IeNormalizedValue : InformationElement
    {
        public int Value { get; private set; }

        /// <summary>
        /// Normalized value is a value in the range from -1 to (1-1/(2^15))
        /// </summary>
        /// <remarks>
        /// This class represents value as an integer from -32768 to 32767 instead.
        /// In order to get the real normalized value you need to divide value by 32768.
        /// </remarks>
        /// <param name="value">value in the range -32768 to 32767</param>
        public IeNormalizedValue(short value)
        {
            Value = value;
        }

        public IeNormalizedValue(BinaryReader reader)
        {
            Value = reader.ReadByte() | (reader.ReadByte() << 8);
            Value -= 32768;
        }

        public static implicit operator decimal (IeNormalizedValue value)
        {
            return (decimal)(value.Value) / 32768;
        }

        public override int Encode(byte[] buffer, int i)
        {
            var temp = Value + 32768;
            buffer[i++] = (byte) temp;
            buffer[i] = (byte) (temp >> 8);

            return 2;
        }

        public override string ToString()
        {
            return "Normalized value: " + (double)Value / 32768;
        }
    }
}