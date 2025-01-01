using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using KMerUtils.KMer;
using FlashHash;
using FlashHash.SchemesAndFamilies;

namespace KMerUtils.DNAGraph
{
    public static class Create
    {
        public static (HashSet<ulong>, HashSet<(ulong, ulong)>) CreateDNAGraphCanonical(int kMerLength)
        {
            ulong nKMer = 1UL << (kMerLength * 2);
            HashSet<ulong> nodes = new HashSet<ulong>((int)nKMer);
            HashSet<(ulong, ulong)> edges = new HashSet<(ulong, ulong)>();
            for (ulong i = 0; i < nKMer; i++)
            {
                nodes.Add(Utils.GetCanonical(i, kMerLength));
                foreach (var neighbor in Utils.GetRightNeighbors(i, kMerLength))
                {
                    edges.Add((Utils.GetCanonical(i, kMerLength), Utils.GetCanonical(neighbor, kMerLength)));
                }
                foreach (var neighbor in Utils.GetLeftNeighbors(i, kMerLength))
                {
                    edges.Add((Utils.GetCanonical(i, kMerLength), Utils.GetCanonical(neighbor, kMerLength)));
                }
            }
            return (nodes, edges);
        }

        public static (ulong[], ulong[]) GenerateMutationOfRandomPath(ulong[] path)
        {
            ulong change = Random.Shared.NextRandomUInt64() % 3 + 1;
            ulong[] mutatedPath = new ulong[path.Length];
            for (int i = 0; i < path.Length; i++)
            {

                mutatedPath[i] = path[i] ^ change;
                change <<= 2;
                //Console.WriteLine(_path[i].ToStringRepre(31));
                //Console.WriteLine(mutatedPath[i].ToStringRepre(31));
            }
            return (path, mutatedPath);
        }
        public static ulong[] GenerateRandomPathOfFixedSize(int pathLength, int kMerLength, Random random)
        {
            ulong[] answer = new ulong[pathLength];
            ulong mask = (1UL << (kMerLength * 2)) - 1;
            answer[0] = random.NextRandomUInt64() & mask;
            for (int i = 1; i < pathLength; i++)
            {
                answer[i] = Utils.GetRightNeighbors(answer[i - 1], kMerLength)[random.Next(4)];
            }

            for (int i = 0; i < pathLength; i++)
            {
                answer[i] = Utils.GetCanonical(answer[i], kMerLength);
            }

            return answer;
        }

        public static (ulong[], ulong[])[] GenerateMutationGraph(int kMerLength, int nMutations, Random random, bool doublePath = true)
        {
            var x = Enumerable.Range(0, nMutations).
                Select(_ => GenerateRandomPathOfFixedSize(kMerLength, kMerLength, random));
            return doublePath ? x.Select(GenerateMutationOfRandomPath).ToArray() : x.Select((x) => (x, x)).ToArray();


        }

        public static IEnumerable<ulong> RemoveRandomlyVerticesFromPath(IEnumerable<ulong> path, double probability, Random random)
        {
            return path.Where(path => random.NextDouble() > probability);
        }

        public readonly static Func<ulong, ulong> HF = new TabulationFamily().GetScheme(1000, 0).Create().Compile();
        public static bool IsSyncMer(ulong kMer, int syncMerSize, int kMerLength)
        {
            ulong maximum = 0;
            ulong mask = (1UL << (syncMerSize * 2)) - 1;

            for (int i = 0; i < kMerLength - syncMerSize; i++)
            {
                maximum = Math.Max(maximum, HF(mask & (kMer >>> (i * 2))));
            }
            //Console.WriteLine(maximum);
            return (maximum == HF(mask & kMer) | (maximum == HF(kMer >> (kMerLength - syncMerSize))));
        }

        public static ((ulong[], ulong[])[] originalGraph, ulong[] graphForRecovery) GenerateGraphForRecovery(int kMerLength, int nMutations, double probability, Random random, bool doublePath = true)
        {
            (ulong[], ulong[])[] originalGraph = GenerateMutationGraph(kMerLength, nMutations, random, doublePath);


            ulong[] graphForRecovery = originalGraph.SelectMany(x => x.Item1.Concat(x.Item2)).Where(
                //kMer => IsSyncMer(kMer, Math.Min(kMerLength, (int)(kMerLength + 1 - 2 / (1 - probability))), _kMerLength)
                //x => IsSyncMer(x, 25, 31)
                _ => random.NextDouble() > probability
                ).ToArray();
            return (originalGraph, graphForRecovery);
        }
    }
}
