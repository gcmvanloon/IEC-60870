using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IEC60870.IE;
using Xunit;

namespace IEC60870.UnitTest.IE
{
    public class IeTime56Tests
    {
        [Fact]
        public void Month_should_be_set_correctly()
        {
            var date = new DateTime(2016, 1, 2);
            var sut = new IeTime56(date.Ticks);
            Assert.Equal(1, sut.GetMonth());
        }
    }
}
