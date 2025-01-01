using KMerUtils.DNAGraph;
using KMerUtils.KMer;
using System.Diagnostics;
using System.Linq;
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

        if (args[0] == "measure-recovery")
        {
            int kMerLength = int.Parse(args[1]);
            int nMutations = int.Parse(args[2]);
            double probability = 0;
            int seed = int.Parse(args[4]);
            int distanceCutoff = int.Parse(args[5]);
            int nTests = int.Parse(args[6]);
            int minDistance = int.Parse(args[7]);

            double probabilityStep = 0.01;

            Console.WriteLine("Prob,Cor,Miss,Fail,Ratio,");
            while (probability < 1)
            {
                probability += probabilityStep;
                List<(ulong, ulong, ulong)> results = new();

                int length = 0;
                int lengthGraphForRecovery = 0;
                int lengthRecovered = 0;

                for (int i = 0; i < nTests; i++)
                {
                    Random random = new Random(seed + i);
                    var (originalGraph, graphForRecovery) = DNAGraph.Create.GenerateGraphForRecovery(kMerLength, nMutations, probability, random);
                    //Console.WriteLine($"Original graph _length: {originalGraph.Sum(x => x.Length)} for recovery _length: {graphForRecovery.Length}");
                    length += originalGraph.Sum(x => x.Item1.Length + x.Item2.Length);
                    lengthGraphForRecovery += graphForRecovery.Length;

                    var recovered = DNAGraph.Recover.RecoverGraphCanonicalV3(graphForRecovery, kMerLength, distanceCutoff, minDistance
                        );
                    results.Add(DNAGraph.Evaluate.EvaluateRecovery(originalGraph.SelectMany(x => x.Item1.Concat(x.Item2)).Select(x => Utils.GetCanonical(x, kMerLength)).ToArray(), recovered));

                    var hashRecovered = recovered.ToHashSet();
                    var hashGraphForRecovery = graphForRecovery.ToHashSet();

                    lengthRecovered += recovered.Length;

                    //DNAGraph.Recover.FindPaths(recovered, _kMerLength, 100).ForEach(x => Console.WriteLine(x.Count()));
                }

                var res = results.Aggregate((0UL, 0UL, 0UL), (acc, x) => (acc.Item1 + x.Item1, acc.Item2 + x.Item2, acc.Item3 + x.Item3));

                Console.WriteLine($"{probability},{(double)res.Item1 / length},{(double)res.Item2 / length},{(double)res.Item3 / length},{(double)lengthRecovered / lengthGraphForRecovery},");
            }

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
            bool doublePath = bool.Parse(args[8]);


            Stopwatch sw = new Stopwatch();
            sw.Start();

            //foreach (var vertex in graphForRecovery.Select(x => BasicKMerOperations.GetCanonical(x, _kMerLength)))
            //{
            //    Console.WriteLine(vertex.ToStringRepre(_kMerLength));
            //}

            //foreach (var _path in originalGraph)
            //{
            //    Console.WriteLine("Start of _path");
            //    foreach (var vertex in _path) Console.WriteLine(vertex.ToStringRepre(_kMerLength));
            //}


            List<(ulong, ulong, ulong)> results = new();
            List<int> forRecoverySize = new();



            for (int i = 0; i < nTests; i++)
            {
                Random random = new Random(seed + i);
                var (originalGraph, graphForRecovery) = DNAGraph.Create.GenerateGraphForRecovery(kMerLength, nMutations, probability, random, doublePath);
                //Console.WriteLine($"Original graph _length: {originalGraph.Sum(x => x.Item1.Length + x.Item2.Length)} for recovery _length: {graphForRecovery.Length}");


                var recovered = DNAGraph.Recover.RecoverGraph(graphForRecovery, kMerLength, distanceCutoff, distanceCutoff, minDistance).ToArray();

                Console.WriteLine(recovered.Select(x => Utils.GetCanonical(x, kMerLength)).Count());

                //var bad =
                //                    Recover.FindPaths(recovered, _kMerLength, 100)
                //                    .SelectMany(x => x)
                //                    .ToArray();
                //;
                //Console.WriteLine(recovered.Count());
                //Console.WriteLine(recovered.ToHashSet().Count());
                //Console.WriteLine(bad.Count());



                results.Add(DNAGraph.Evaluate
                    .EvaluateRecovery(
                        originalGraph
                            .SelectMany(x => x.Item1.Concat(x.Item2))
                            .Select(x => Utils.GetCanonical(x, kMerLength))
                            .ToArray()
                            , recovered.Concat(graphForRecovery).Select(x => Utils.GetCanonical(x, kMerLength))
                            .ToArray()));
                ;
                forRecoverySize.Add(graphForRecovery.Count());

                var hashRecovered = recovered.ToHashSet();
                var hashGraphForRecovery = graphForRecovery.ToHashSet();


                //DNAGraph.Recover.FindPaths(recovered, _kMerLength, 100).ForEach(x => Console.WriteLine(x.Count()));

                //originalGraph.Take(1)
                //    .Select(
                //        _path => KMerUtils.DNAGraph.Evaluate
                //        .EvaluatePathRecovery(_path, hashGraphForRecovery, hashRecovered)
                //        .Zip(_path.Select(x => x.ToCanonical(_kMerLength).ToStringRepre(_kMerLength))))
                //    .ForEach(x => Console.WriteLine(string.Join("\n", x)));


                //var _path = graphForRecovery
                //    ;

                //_path.Select(x => (x, DNAGraph.Info.FindClosestDirected(x, _kMerLength, _path, minDistance)))
                //    .ForEach(x => Console.WriteLine($"{x.x.ToStringRepre(_kMerLength)}, {x.Item2.kMer.ToStringRepre(_kMerLength)}, {x.Item2.distance}"));
                //;



            }

            var res = results.Aggregate((0UL, 0UL, 0UL), (acc, x) => (acc.Item1 + x.Item1, acc.Item2 + x.Item2, acc.Item3 + x.Item3));

            Console.WriteLine($"Begin with: {forRecoverySize.Average()} Correct: {(double)res.Item1 / nTests}, Missing: {(double)res.Item2 / nTests}, Wrong: {(double)res.Item3 / nTests}");


            //Console.WriteLine(
            //    $"original _length: {originalGraph.Sum(x => x.Length)} for recovery _length: {graphForRecovery.Length})"
            //);

            //foreach (var vertex in recovered)
            //{
            //    Console.WriteLine(vertex.ToStringRepre(_kMerLength));
            //}
            Console.WriteLine(
                $"Time elapsed: {sw.ElapsedMilliseconds} ms"
            );
        }
    }
}
