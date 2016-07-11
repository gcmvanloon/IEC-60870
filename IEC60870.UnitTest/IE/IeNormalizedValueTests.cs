using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IEC60870.IE;
using Xunit;

namespace IEC60870.UnitTest.IE
{
    public class IeNormalizedValueTests
    {
        [Fact]
        public void MinValue_represents_minus_1()
        {
            var sut = new IeNormalizedValue(short.MinValue);
            Assert.Equal("Normalized value: -1", sut.ToString());
        }

        [Fact]
        public void MaxValue_represents_close_to_1()
        {
            var sut = new IeNormalizedValue(short.MaxValue);
            Assert.Equal("Normalized value: 0,999969482421875", sut.ToString());
        }

        [Fact]
        public void Zero_represents_0()
        {
            var sut = new IeNormalizedValue(0);
            Assert.Equal("Normalized value: 0", sut.ToString());
        }

        [Theory]
        [InlineData(short.MaxValue, 1)]
        [InlineData(short.MinValue, -1)]
        [InlineData(0, 0)]
        [InlineData(short.MaxValue / 2, 0.5)]
        [InlineData(short.MaxValue / 4, 0.25)]
        [InlineData(short.MinValue / 2, -0.5)]
        [InlineData(short.MinValue / 10, -0.1)]
        public void IeNormalizedValue_implicit_convert_to_decimal(short value, decimal expected)
        {
            var sut = new IeNormalizedValue(value);
            Assert.Equal((decimal)expected, sut, 2);
        }

        [Fact]
        public void All_16_bits_on_represents_1()
        {
            var stream = new MemoryStream(new Byte[] { 0xFF, 0xFF });
            var reader = new BinaryReader(stream);
            var sut = new IeNormalizedValue(reader);

            Assert.Equal(1.0m, sut, 2);
        }

        [Fact]
        public void All_16_bits_off_represents_minus_1()
        {
            var stream = new MemoryStream(new Byte[] { 0x00, 0x00 });
            var reader = new BinaryReader(stream);
            var sut = new IeNormalizedValue(reader);

            Assert.Equal(-1.0m, sut, 2);
        }

        [Fact]
        public void Decode()
        {
            var buffer = new byte[2];
            var sut = new IeNormalizedValue(short.MaxValue);
            sut.Encode(buffer, 0);

            Assert.Equal(new byte[] { 0xFF, 0xFF }, buffer);
        }
    }
}
