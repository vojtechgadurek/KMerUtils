using KMerUtils;
using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;
using Xunit.Abstractions;

namespace KMerUtilsTests
{
    public static class PathRecoveryTestData
    {

        public static (string a, string b, int lengthLargestPrefixABeingSuffixOfB)[] SuffixPrefixData = [
            ("AAAATAAAAT", "AAAATCCCCC", 10),
            ("AAAATCCCCC", "AAAATAAAAT", 5),
            ("AAAA", "TTTT", 4),
            ];

        public static (string a, string b, int Distance, DnaGraph.Direction[] possibleDirections)[] TwoVertices = [

            ("AAA", "AAA", 0, [DnaGraph.Direction.aToB, DnaGraph.Direction.bToA]),
            ("AAT","AAA", 1, [DnaGraph.Direction.aToB]),
            ("AAA","AAT", 1, [DnaGraph.Direction.bToA]),
            ("AAAATAAAAT", "AAAATCCCCC", 5, [DnaGraph.Direction.bToA]),
            ("AAA", "TTT", 0, [DnaGraph.Direction.aToBComplement, DnaGraph.Direction.BComplementToA])
            ];

        public static (string a, string b, string[] between, int distance)[] PathData = [
            ("AAA", "TTT", ["TAA", "TTA"], 3),
            ("AAA", "AAA", [], 0),
            ("AAA", "TAA", [], 1),
            ("AAAAAAAA", "TTTTTTTC", [ "CAAAAAAA", "TCAAAAAA", "TTCAAAAA", "TTTCAAAA", "TTTTCAAA", "TTTTTCAA", "TTTTTTCA"], 8),

            ];
    }
    public class PathRecoveryTests
    {
        ITestOutputHelper _output;
        public PathRecoveryTests(ITestOutputHelper output)
        {
            _output = output;
        }
        [Fact]
        public void TestDetermineDistanceAndDirection()
        {
            foreach (var item in PathRecoveryTestData.TwoVertices)
            {
                var answer = DnaGraph.DetermineDistance(item.a.ToKMer(), item.b.ToKMer(), item.a.Length);
                Assert.Equal(item.Distance, answer.Distance);
                Assert.Contains(answer.Direction, item.possibleDirections);
            }
        }

        [Fact]
        public void TestLargestPrefixABeingSuffixB()
        {
            foreach (var item in PathRecoveryTestData.SuffixPrefixData)
            {
                var answer = DnaGraph.FindLargestPrefixABeingSuffixB(item.a.ToKMer(), item.b.ToKMer(), item.a.Length);
                Assert.Equal(item.lengthLargestPrefixABeingSuffixOfB, answer);
            }
        }

        [Fact]
        void TestGetPathFromAToB()
        {
            foreach (var item in PathRecoveryTestData.PathData)
            {
                var a = item.a.ToKMer();
                var b = item.b.ToKMer();
                var answer = DnaGraph.GetPathFromAToB(a, b, item.a.Length, item.distance).ToArray();

                _output.WriteLine($"a: {item.a}, b: {item.b}, distance: {item.distance}, itemsOnPath {string.Join("-", item.between)}");
                Assert.Equal(item.between.Length, answer.Length);
                foreach (var innerItem in answer.Zip(item.between))
                {
                    Assert.Equal(innerItem.Second, BasicKMerOperations.TranslateUlongToString(innerItem.First, item.a.Length));
                }

            }
        }
    }
}