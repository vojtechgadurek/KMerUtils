using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static KMerUtils.DNAGraph.Info;
using KMerUtils.KMer;

namespace KMerUtils.DNAGraph
{
    public static partial class Recover
    {
        public static ulong[] RecoverGraphCanonical(ulong[] graph, int kMerLength, int distanceCutoff, int minDistance)
        {
            IEnumerable<ulong> answer = new List<ulong>();
            foreach (var vertex in graph)
            {
                var (closestKMer, distance, direction) = FindClosestDirected(vertex, kMerLength, graph, minDistance);
                if (distance > distanceCutoff)
                {
                    continue;
                }

                //Console.WriteLine(distance);
                if (direction == Direction.aToBComplement || direction == Direction.BComplementToA)
                {
                    closestKMer = Utils.GetComplement(closestKMer, kMerLength);
                }
                answer = answer.Concat(GetPathFromAToB(vertex, closestKMer, kMerLength, distance));
            }
            return answer.Concat(graph).Select(x => Utils.GetCanonical(x, kMerLength)).ToArray();
        }

        public static ulong[] RecoverGraphCanonicalV2(ulong[] graph, int kMerLength, int distanceCutoff, int minDistance)
        {
            IEnumerable<ulong> answer = new List<ulong>();

            var distanceFinder = new DistanceFromClosestFinder(graph, kMerLength, distanceCutoff, minDistance);


            foreach (var vertex in graph)
            {
                var (closestKMer, distance) = distanceFinder.FindClosest(vertex);
                Console.WriteLine(distance);


                answer = answer.Concat(GetPathFromAToB(vertex, closestKMer, kMerLength, distance));
            }
            return answer.Concat(graph).Select(x => Utils.GetCanonical(x, kMerLength)).ToArray();
        }

        public static ulong[] RecoverGraphCanonicalV3(ulong[] graph, int kMerLength, int distanceCutoff, int minDistance, bool addGraph = true)
        {
            IEnumerable<ulong> answer = new List<ulong>();

            ulong[] verticesTo = graph.Concat(graph.Select(x => Utils.GetComplement(x, kMerLength))).ToArray();

            List<ulong> verticesFrom = new List<ulong>(verticesTo);
            for (int i = minDistance; i <= distanceCutoff; i++)
            {
                var (found, notFound) = FindVerticesInSetDistance(verticesFrom, verticesTo, i, kMerLength);
                foreach (var (from, to) in found)
                {
                    answer = answer.Concat(GetPathFromAToB(from, to, kMerLength, i));
                }
                verticesFrom = notFound;

                //found.ForEach(x => Console.WriteLine((x.from.ToStringRepre(kMerLength), x.to.ToStringRepre(kMerLength), i)));

                if (i == distanceCutoff) Console.WriteLine(notFound.Count);
            }

            if (addGraph)
            {
                answer = answer.Concat(graph);
            }

            return answer.Select(x => Utils.GetCanonical(x, kMerLength)).ToArray();
        }
    }
}
