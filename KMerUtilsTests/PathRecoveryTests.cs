using KMerUtils;
using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;
using Xunit.Abstractions;
using KMerUtils.KMer;
using KMerUtils;
using KMerUtils.DNAGraph;


namespace KMerUtilsTests
{
    public static class PathRecoveryTestData
    {

        public static (string a, string b, int lengthLargestPrefixABeingSuffixOfB)[] SuffixPrefixData = [
            ("AAAATAAAAT", "AAAATCCCCC", 10),
            ("AAAATCCCCC", "AAAATAAAAT", 5),
            ("AAAA", "TTTT", 4),
            ];

        public static (string a, string b, int Distance, KMerUtils.DNAGraph.Info.Direction[] possibleDirections)[] TwoVertices = [
            ("AAA", "AAA", 0, [KMerUtils.DNAGraph.Info.Direction.aToB, KMerUtils.DNAGraph.Info.Direction.bToA]),
            ("AAT","AAA", 1, [KMerUtils.DNAGraph.Info.Direction.aToB]),
            ("AAA","AAT", 1, [KMerUtils.DNAGraph.Info.Direction.bToA]),
            ("AAAATAAAAT", "AAAATCCCCC", 5, [KMerUtils.DNAGraph.Info.Direction.bToA]),
            ("AAA", "TTT", 0, [KMerUtils.DNAGraph.Info.Direction.aToBComplement, KMerUtils.DNAGraph.Info.Direction.BComplementToA]),
            ("GACAGAAGAC", "AGACAGAAGA", 1, [Info.Direction.aToB])
            ];

        public static (string a, string b, string[] between, int distance)[] PathData = [
            ("AAA", "TTT", ["TAA", "TTA"], 3),
            ("AAA", "AAA", [], 0),
            ("AAA", "TAA", [], 1),
            ("AAAAAAAA", "TTTTTTTC", [ "CAAAAAAA", "TCAAAAAA", "TTCAAAAA", "TTTCAAAA", "TTTTCAAA", "TTTTTCAA", "TTTTTTCA"], 8),
            ];

        public static (string a, string b, string[] between, int distance, int maxDistance)[] CannonicalPathData = [
            ("AAA", "TAA", [], 1,1),
            ("AAA", "TTT", [], 0,3),
            ("AAA", "AAA", [], 0,3),
            ("AAAAAAAA", "TTTTTTTC", [], 1, 8),

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
                var answer = KMerUtils.DNAGraph.Info.DetermineDistance(item.a.ToKMer(), item.b.ToKMer(), item.a.Length);
                Assert.Equal(item.Distance, answer.Distance);
                Assert.Contains(answer.Direction, item.possibleDirections);
            }
        }

        [Fact]
        public void TestLargestPrefixABeingSuffixB()
        {
            foreach (var item in PathRecoveryTestData.SuffixPrefixData)
            {
                var answer = KMerUtils.DNAGraph.Info.FindLargestPrefixABeingSuffixB(item.a.ToKMer(), item.b.ToKMer(), item.a.Length);
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
                var answer = KMerUtils.DNAGraph.Info.GetPathFromAToB(a, b, item.a.Length, item.distance).ToArray();

                _output.WriteLine($"a: {item.a}, b: {item.b}, distance: {item.distance}, itemsOnPath {string.Join("-", item.between)}");
                Assert.Equal(item.between.Length, answer.Length);
                foreach (var innerItem in answer.Zip(item.between))
                {
                    Assert.Equal(innerItem.Second, Utils.TranslateUlongToString(innerItem.First, item.a.Length));
                }

            }
        }

        [Fact]

        void TestPathRecovery()
        {
            foreach (var item in PathRecoveryTestData.CannonicalPathData)
            {
                var a = item.a.ToKMer();
                var b = item.b.ToKMer();
                var kMerLength = item.a.Length;

                var between = item.between.Prepend(item.a).Append(item.b).Select(x => Utils.GetCanonical(x.ToKMer(), kMerLength).ToStringRepre(kMerLength)).ToArray();

                var answer = KMerUtils.DNAGraph.Recover.RecoverGraphCanonical([a, b], kMerLength, item.maxDistance, 0)

                    .Select(x => Utils.GetCanonical(x, kMerLength))
                    .ToArray();

                _output.WriteLine($"a: {item.a}, b: {item.b}, distance: {item.distance}, itemsOnPath {string.Join("-", between)}, recovered: {string.Join("-", answer.Select(x => x.ToStringRepre(kMerLength)))}");
                Assert.Equal(between.Length, answer.Length);
                foreach (var innerItem in answer.Zip(between))
                {
                    Assert.Equal(innerItem.Second, Utils.TranslateUlongToString(innerItem.First, kMerLength));
                }

            }
        }
    }
}