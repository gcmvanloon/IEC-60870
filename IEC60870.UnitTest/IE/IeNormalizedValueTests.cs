using System;
using System.Collections.Generic;
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
    }
}
