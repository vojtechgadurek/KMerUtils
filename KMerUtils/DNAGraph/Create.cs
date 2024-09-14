using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KMerUtils.KMer;


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

        public static ulong[][] GenerateMutationGraph(int kMerLength, int nMutations, Random random)
        {
            return Enumerable.Range(0, nMutations).Select(_ => GenerateRandomPathOfFixedSize(kMerLength, kMerLength, random)).ToArray();
        }

        public static IEnumerable<ulong> RemoveRandomlyVerticesFromPath(IEnumerable<ulong> path, double probability, Random random)
        {
            return path.Where(path => random.NextDouble() > probability);
        }

        public static (ulong[][] originalGraph, ulong[] graphForRecovery) GenerateGraphForRecovery(int kMerLength, int nMutations, double probability, Random random)
        {
            ulong[][] originalGraph = GenerateMutationGraph(kMerLength, nMutations, random);
            ulong[] graphForRecovery = originalGraph.SelectMany(x => RemoveRandomlyVerticesFromPath(x, probability, random)).ToArray();
            return (originalGraph, graphForRecovery);
        }
    }
}
