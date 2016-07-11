using System;
using System.IO;
using System.Text;
using IEC60870.IE.Base;

namespace IEC60870.IE
{
    /// <summary>
    /// Information element "Binary Time 2a" = CP56Time2a
    /// </summary>
    /// <remarks>
    /// This binary time is defined in 6.8 of IEC 60870-5-4
    /// <code>
    ///         Bits |  8  |  7  |  6  |  5  |  4  |  3  |  2  |  1  |
    /// Octects      +-----+-----+-----+-----+-----+-----+-----+-----+
    ///              |                  Milliseconds                 |
    ///    1         |  7                                         0  |
    ///              +-----+-----+-----+-----+-----+-----+-----+-----+
    ///              |                  Milliseconds                 |
    ///    2         | 15                                         8  |  0..59999 ms
    ///              +-----+-----+-----+-----+-----+-----+-----+-----+
    ///              |     |     |            Minutes                |
    ///    3         |  IV | RES1|  5     4     3     2     1     0  |  0..59 min
    ///              +-----+-----+-----+-----+-----+-----+-----+-----+
    ///              |     |           |          Hours              |
    ///    4         |  SU |    RES2   |  4     3     2     1     0  |  0..23 h
    ///              +-----+-----+-----+-----+-----+-----+-----+-----+
    ///              |   Day of week   |       Day of month          |  1..31 days of month
    ///    5         |  2     1     0  |  4     3     2     1     0  |  1..7 days of week
    ///              +-----+-----+-----+-----+-----+-----+-----+-----+
    ///              |                       |       Months          |
    ///    6         |         RES3          |  3     2     1     0  |  1..12 months
    ///              +-----+-----+-----+-----+-----+-----+-----+-----+
    ///              |     |               Years                     |
    ///    7         | RES4|  6     5     4     3     2     1     0  |  0..99 years
    ///              +-----+-----+-----+-----+-----+-----+-----+-----+
    /// </code>
    /// </remarks>
    public class IeTime56 : InformationElement
    {
        private readonly byte[] value = new byte[7];

        public IeTime56(long timestamp, TimeZone timeZone, bool invalid)
        {
            var datetime = new DateTime(timestamp);
            var ms = datetime.Millisecond + 1000*datetime.Second;

            value[0] = (byte) ms;
            value[1] = (byte) (ms >> 8);
            value[2] = (byte) datetime.Minute;

            if (invalid)
            {
                value[2] |= 0x80;
            }
            value[3] = (byte) datetime.Hour;
            if (datetime.IsDaylightSavingTime())
            {
                value[3] |= 0x80;
            }
            value[4] = (byte) (datetime.Day + ((((int) datetime.DayOfWeek + 5)%7 + 1) << 5));
            value[5] = (byte) (datetime.Month);
            value[6] = (byte) (datetime.Year%100);
        }

        public IeTime56(DateTime dateTimeInUtc)
        {
            if(dateTimeInUtc.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("DateTime must be represented as UTC", "dateTimeInUtc");
            }

            var ms = dateTimeInUtc.Millisecond + 1000 * dateTimeInUtc.Second;

            value[0] = (byte)ms;
            value[1] = (byte)(ms >> 8);
            value[2] = (byte)dateTimeInUtc.Minute;
            value[3] = (byte)dateTimeInUtc.Hour;
            // UTC uses no DayligtSavingTime
            value[4] = (byte)(dateTimeInUtc.Day + (((int)dateTimeInUtc.DayOfWeek + 1) << 5));
            value[5] = (byte)(dateTimeInUtc.Month);
            value[6] = (byte)(dateTimeInUtc.Year % 100);
        }

        public IeTime56(long timestamp) : this(timestamp, TimeZone.CurrentTimeZone, false)
        {
        }

        public IeTime56(byte[] value)
        {
            for (var i = 0; i < 7; i++)
            {
                this.value[i] = value[i];
            }
        }

        public IeTime56(BinaryReader reader)
        {
            value = reader.ReadBytes(7);
        }

        public override int Encode(byte[] buffer, int i)
        {
            Array.Copy(value, 0, buffer, i, 7);
            return 7;
        }

        public long GetTimestamp(int startOfCentury, TimeZone timeZone)
        {
            var century = startOfCentury/100*100;
            if (value[6] < startOfCentury%100)
            {
                century += 100;
            }

            return -1;
        }

        public long GetTimestamp(int startOfCentury)
        {
            return GetTimestamp(startOfCentury, TimeZone.CurrentTimeZone);
        }

        public long GetTimestamp()
        {
            return GetTimestamp(1970, TimeZone.CurrentTimeZone);
        }

        public int GetMillisecond()
        {
            return ((value[0] & 0xff) + ((value[1] & 0xff) << 8))%1000;
        }

        public int GetSecond()
        {
            return ((value[0] & 0xff) + ((value[1] & 0xff) << 8))/1000;
        }

        public int GetMinute()
        {
            return value[2] & 0x3f;
        }

        public int GetHour()
        {
            return value[3] & 0x1f;
        }

        public int GetDayOfWeek()
        {
            return (value[4] & 0xe0) >> 5;
        }

        public int GetDayOfMonth()
        {
            return value[4] & 0x1f;
        }

        public int GetMonth()
        {
            return value[5] & 0x0f;
        }

        public int GetYear()
        {
            return value[6] & 0x7f;
        }

        public bool IsSummerTime()
        {
            return (value[3] & 0x80) == 0x80;
        }

        public void SetSummerTime()
        {
            value[3] |= 0x80;
        }

        public void UnsetSummerTime()
        {
            value[3] &= 0x7F;
        }

        public bool IsInvalid()
        {
            return (value[2] & 0x80) == 0x80;
        }

        public override string ToString()
        {
            var builder = new StringBuilder("Time56: ");
            AppendWithNumDigits(builder, GetDayOfMonth(), 2);
            builder.Append("-");
            AppendWithNumDigits(builder, GetMonth(), 2);
            builder.Append("-");
            AppendWithNumDigits(builder, GetYear(), 2);
            builder.Append(" ");
            AppendWithNumDigits(builder, GetHour(), 2);
            builder.Append(":");
            AppendWithNumDigits(builder, GetMinute(), 2);
            builder.Append(":");
            AppendWithNumDigits(builder, GetSecond(), 2);
            builder.Append(":");
            AppendWithNumDigits(builder, GetMillisecond(), 3);

            if (IsSummerTime())
            {
                builder.Append(" DST");
            }

            if (IsInvalid())
            {
                builder.Append(", invalid");
            }

            return builder.ToString();
        }

        private void AppendWithNumDigits(StringBuilder builder, int value, int numDigits)
        {
            var i = numDigits - 1;
            while (i < numDigits && value < Math.Pow(10, i))
            {
                builder.Append("0");
                i--;
            }
            builder.Append(value);
        }
    }
}