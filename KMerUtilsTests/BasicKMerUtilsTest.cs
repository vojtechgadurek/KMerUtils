using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

using KMerUtils.KMer;

namespace KMerUtils
{

    public static class BasicTestData
    {
        public static (ulong KMer, string StringRepr)[] ShortValues = [

            (0b0000, "AA"),
            (0b0001, "AC"),
            (0b0010, "AG"),
            (0b0011, "AT"),
            (0b0100, "CA"),
            (0b0101, "CC"),
            (0b0110, "CG"),
            (0b0111, "CT"),
            (0b1000, "GA"),
            (0b1001, "GC"),
            (0b1010, "GG"),
            (0b1011, "GT"),
            (0b1100, "TA"),
            (0b1101, "TC"),
            (0b1110, "TG"),
            (0b1111, "TT"),




            ];
        public static (ulong KMer, string StringRepr)[] LongValues = [
            (0b00_00_00_00_00_00, "AAAAAA"),
            (0b00_00_00_00_00_11, "AAAAAT"),
            (0b00_00_00_00_11_11, "AAAATT"),
            (0b00_00_00_11_11_11, "AAATTT"),
            (0b00_00_11_11_11_11, "AATTTT"),
            (0b00_11_11_11_11_11, "ATTTTT"),
            (0b11_11_11_11_11_11, "TTTTTT"),
            (0b00_00_00_00_01_01, "AAAACC"),
            (0b00_00_00_00_10_10, "AAAAGG"),
            (0b00_00_00_00_11_11, "AAAATT"),
            (0b00_00_00_01_01_01, "AAACCC"),
            (0b00_00_00_00_11_01, "AAAATC"),
            (0b00_00_00_01_01_10, "AAACCG"),

            ];

        public static (string KMer, string Complement)[] KMerWithComplement = [
            ("A", "T"),
            ("C", "G"),
            ("AT", "AT"),
            ("TA", "TA"),
            ("AAT", "ATT"),
            ("CCG", "CGG"),
            ("AAAAGAAAAG", "CTTTTCTTTT")

            ];
    }
    public class BasicKMerUtilsTest
    {
        ITestOutputHelper _output;

        public BasicKMerUtilsTest(ITestOutputHelper output)
        {
            _output = output;
        }


        [Fact]
        public void TestTranslateUlongToString()
        {
            foreach (var item in BasicTestData.ShortValues.Concat(BasicTestData.LongValues))
            {
                _output.WriteLine(item.ToString());
                Assert.Equal(item.StringRepr, Utils.TranslateUlongToString(item.KMer, item.StringRepr.Length));
            }
        }

        [Fact]
        public void TestTranslateStringToUlong()
        {
            foreach (var item in BasicTestData.ShortValues.Concat(BasicTestData.LongValues))
            {
                _output.WriteLine(item.ToString());
                Assert.Equal(item.KMer, Utils.TranslateStringToUlong(item.StringRepr));
            }

        }

        [Fact]
        public void TestComplement()
        {
            foreach (var item in BasicTestData.KMerWithComplement)
            {
                _output.WriteLine(item.ToString());
                Assert.Equal(item.Item2, Utils.TranslateUlongToString(Utils.GetComplement(item.Item1.ToKMer(), item.Item1.Length), item.Item1.Length));
            }
        }
    }
}
