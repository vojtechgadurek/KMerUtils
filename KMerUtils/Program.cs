using KMerUtils.KMer;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;

namespace KMerUtils;
public class Program
{
    public static void Main(string[] args)
    {
        if (args.Length == 0)
            args = new string[] { "test-recovery", "10", "10", "0.5", "42", "20", "1", "1" };

        if (args[0] == "prt-GRAPH")
        {

            int kMerLength = int.Parse(args[1]);
            var (nodes, edges) = DNAGraph.Create.CreateDNAGraphCanonical(kMerLength);

            Console.WriteLine("digraph G {");
            foreach (var edge in edges)
            {
                Console.WriteLine($"\t{Utils.TranslateUlongToString(edge.Item1, kMerLength)} -> {Utils.TranslateUlongToString(edge.Item2, kMerLength)};");

            }
            Console.WriteLine("}");
        }

        if (args[0] == "test-recovery")
        {
            int kMerLength = int.Parse(args[1]);
            int nMutations = int.Parse(args[2]);
            double probability = double.Parse(args[3]);
            int seed = int.Parse(args[4]);
            int distanceCutoff = int.Parse(args[5]);
            int nTests = int.Parse(args[6]);
            int minDistance = int.Parse(args[7]);


            Stopwatch sw = new Stopwatch();
            sw.Start();

            //foreach (var vertex in graphForRecovery.Select(x => BasicKMerOperations.GetCanonical(x, kMerLength)))
            //{
            //    Console.WriteLine(vertex.ToStringRepre(kMerLength));
            //}

            //foreach (var path in originalGraph)
            //{
            //    Console.WriteLine("Start of path");
            //    foreach (var vertex in path) Console.WriteLine(vertex.ToStringRepre(kMerLength));
            //}


            List<(ulong, ulong, ulong)> results = new();

            for (int i = 0; i < nTests; i++)
            {
                Random random = new Random(seed + i);
                var (originalGraph, graphForRecovery) = DNAGraph.Create.GenerateGraphForRecovery(kMerLength, nMutations, probability, random);
                Console.WriteLine($"Original graph length: {originalGraph.Sum(x => x.Length)} for recovery length: {graphForRecovery.Length}");


                var recovered = DNAGraph.Recover.RecoverGraphCanonicalV3(graphForRecovery, kMerLength, distanceCutoff, minDistance
                    );
                results.Add(DNAGraph.Evaluate.EvaluateRecovery(originalGraph.SelectMany(x => x).Select(x => Utils.GetCanonical(x, kMerLength)).ToArray(), recovered));

                var hashRecovered = recovered.ToHashSet();
                var hashGraphForRecovery = graphForRecovery.ToHashSet();



                //originalGraph.Take(1)
                //    .Select(
                //        path => KMerUtils.DNAGraph.Evaluate
                //        .EvaluatePathRecovery(path, hashGraphForRecovery, hashRecovered)
                //        .Zip(path.Select(x => x.ToCanonical(kMerLength).ToStringRepre(kMerLength))))
                //    .ForEach(x => Console.WriteLine(string.Join("\n", x)));


                //var path = graphForRecovery
                //    ;

                //path.Select(x => (x, DNAGraph.Info.FindClosestDirected(x, kMerLength, path, minDistance)))
                //    .ForEach(x => Console.WriteLine($"{x.x.ToStringRepre(kMerLength)}, {x.Item2.kMer.ToStringRepre(kMerLength)}, {x.Item2.distance}"));
                //;



            }

            var res = results.Aggregate((0UL, 0UL, 0UL), (acc, x) => (acc.Item1 + x.Item1, acc.Item2 + x.Item2, acc.Item3 + x.Item3));

            Console.WriteLine($"Correct: {(double)res.Item1 / nTests}, Missing: {(double)res.Item2 / nTests}, Wrong: {(double)res.Item3 / nTests}");


            //Console.WriteLine(
            //    $"original length: {originalGraph.Sum(x => x.Length)} for recovery length: {graphForRecovery.Length})"
            //);

            //foreach (var vertex in recovered)
            //{
            //    Console.WriteLine(vertex.ToStringRepre(kMerLength));
            //}
            Console.WriteLine(
                $"Time elapsed: {sw.ElapsedMilliseconds} ms"
            );
        }
    }
}
