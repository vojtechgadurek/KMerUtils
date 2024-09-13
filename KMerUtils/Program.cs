using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;

namespace KMerUtils;
public class Program
{
    public static void Main(string[] args)
    {
        if (args.Length == 0)
            args = new string[] { "test-recovery", "31", "1000", "0.8", "42", "20", "100", "1" };

        if (args[0] == "prt-GRAPH")
        {

            int kMerLength = int.Parse(args[1]);
            var (nodes, edges) = DnaGraph.CreateDNAGraphCanonical(kMerLength);

            Console.WriteLine("digraph G {");
            foreach (var edge in edges)
            {
                Console.WriteLine($"\t{BasicKMerOperations.TranslateUlongToString(edge.Item1, kMerLength)} -> {BasicKMerOperations.TranslateUlongToString(edge.Item2, kMerLength)};");

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
                var (originalGraph, graphForRecovery) = DnaGraph.GenerateGraphForRecovery(kMerLength, nMutations, probability, random);
                Console.WriteLine($"Original graph length: {originalGraph.Sum(x => x.Length)} for recovery length: {graphForRecovery.Length}");


                var recovered = DnaGraph.RecoverGraphCanonicalV3(graphForRecovery, kMerLength, distanceCutoff, minDistance
                    );
                results.Add(DnaGraph.EvaluateRecovery(originalGraph.SelectMany(x => x).Select(x => BasicKMerOperations.GetCanonical(x, kMerLength)).ToArray(), recovered));
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

public static class StringKMerExtension
{
    public static ulong ToKMer(this string kMer)
    {
        return BasicKMerOperations.TranslateStringToUlong(kMer);

    }
}

public static class BasicKMerOperations
{
    /// <summary>
    /// Removes header from suffix
    /// </summary>
    /// <param name="kMer"></param>
    /// <returns></returns>
    public static ulong RemoveHeader(ulong kMer)
    {
        kMer >>>= 2;
        return kMer;
    }
    /// <summary>
    /// Add header to suffix 
    /// </summary>
    /// <param name="kMer"></param>
    /// <returns></returns>
    public static ulong AddHeader(ulong kMer)
    {
        return 0b11 | (kMer << 2);
    }
    /// <summary>
    /// Return the reverse complement of a suffix
    /// </summary>
    /// <param name="kMer"></param>
    /// <param name="kMerLength"></param>
    /// <returns></returns>
    public static ulong GetComplement(ulong kMer, int kMerLength)
    {
        ulong newKMer = 0;
        ulong mask = 0b11;
        for (ulong i = 0; (ulong)kMerLength > i; i++)
        {
            newKMer <<= 2;
            newKMer |= (0b11 - (kMer & mask));
            kMer >>= 2;
        }
        return newKMer;
    }
    /// <summary>
    /// Calculate reverse complement of a suffix and pick the canonical one
    /// </summary>
    /// <param name="value"></param>
    /// <param name="kMerLength"></param>
    /// <returns></returns>
    public static ulong GetCanonical(ulong value, int kMerLength)
    {
        //return value;
        ulong other = GetComplement(value, kMerLength);
        if (other > value) return value;
        return other;
    }
    /// <summary>
    /// Translate a symbol represented by a ulong to a char
    /// </summary>
    /// <param name="symbol"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static char TranslateUlongToChar(ulong symbol)
    {
        switch (symbol)
        {
            case 0: return 'A';
            case 1: return 'C';
            case 2: return 'G';
            case 3: return 'T';
            default: throw new ArgumentException("Invalid suffix");
        }
    }

    public static ulong TranslateCharToUlong(char symbol)
    {
        switch (symbol)
        {
            case 'A': return 0;
            case 'C': return 1;
            case 'G': return 2;
            case 'T': return 3;
            default: throw new ArgumentException("Invalid suffix");
        }
    }

    public static ulong TranslateStringToUlong(string kMer)
    {
        ulong value = 0;
        foreach (var c in kMer)
        {
            value <<= 2;
            value |= TranslateCharToUlong(c);
        }
        return value;
    }

    /// <summary>
    /// Returns the n-th character of a suffix
    /// </summary>
    /// <param name="kMer"></param>
    /// <param name="kMerLength"></param>
    /// <param name="position"></param>
    /// <returns></returns>
    public static ulong GetNthCharFromKMerLeft(ulong kMer, int kMerLength, int position)
    {
        ulong mask = 0b11UL;
        kMer >>>= ((kMerLength - position - 1) * 2);
        return kMer & mask;
    }

    /// <summary>
    /// Return string representation of a suffix
    /// </summary>
    /// <param name="kMer"></param>
    /// <param name="kMerLength"></param>
    /// <returns></returns>
    public static string TranslateUlongToString(ulong kMer, int kMerLength)
    {
        char[] chars = new char[kMerLength];
        for (int i = 0; i < kMerLength; i++)
        {
            chars[i] = TranslateUlongToChar(GetNthCharFromKMerLeft(kMer, kMerLength, i));
        }
        return new string(chars);
    }

    public static ulong[] GetRightNeighbors(ulong kMer, int kMerLength)
    {
        ulong[] neighbors = new ulong[4];
        ulong mask = (1UL << (int)(kMerLength * 2)) - 1;
        for (ulong i = 0; i < 4; i++)
        {
            neighbors[i] = ((kMer << 2) | i) & mask;
        }
        return neighbors;
    }
    public static ulong[] GetLeftNeighbors(ulong kMer, int kMerLength)
    {
        ulong[] neighbors = new ulong[4];

        for (ulong i = 0; i < 4; i++)
        {
            neighbors[i] = (kMer >> 2) | (i << ((int)kMerLength * 2));
        }
        return neighbors;
    }

    public static ulong GetLast(ulong kMer)
    {
        return kMer & 0b11;
    }

    public static ulong RightShift(ulong kMer)
    {
        return kMer >>> 2;
    }

    public static ulong Set(ulong kMer)
    {
        return kMer >>> 2;
    }
}

public static class KMerExtensions
{
    public static string ToStringRepre(this ulong kMer, int kMerLength)
    {
        return BasicKMerOperations.TranslateUlongToString(kMer, kMerLength);
    }

    public static ulong ToCanonical(this ulong kMer, int kMerLength)
    {
        return BasicKMerOperations.GetCanonical(kMer, kMerLength);
    }
}
public static class RandomExtensions
{
    public static ulong NextRandomUInt64(this Random random)
    {
        return (ulong)random.NextInt64();
    }
}

public static class DnaGraph
{
    /*
     AATTCCGG -> AATT ATTG TTGG TGGG GGCC GCCG CCGG
     */

    /*
    AATTCD
    GGAATT

    AATTCC
    GAATTC
    GGAATT

    AATTC.
    .GAATT

    ..AATT
    AATT..

    AATTTT ->
    AAATTT ->
    AAAATT
    */
    public static int FindLargestPrefixABeingSuffixB(ulong a, ulong b, int kMerLength)
    {
        ulong prefix = a;
        ulong suffix = b;

        ulong mask = (1UL << (int)(kMerLength * 2)) - 1;
        int counter = 0;
        while (prefix != suffix)
        {
            prefix >>>= 2;
            mask >>>= 2;
            suffix &= mask;
            counter++;
        }
        return counter;
    }

    public enum Direction
    {
        aToB,
        bToA,
        aToBComplement,
        BComplementToA,
    }
    public static (int Distance, Direction Direction) DetermineDistance(ulong a, ulong b, int kMerLength)
    {
        Direction direction = Direction.aToB;
        var distance = FindLargestPrefixABeingSuffixB(a, b, kMerLength);

        var baDistance = FindLargestPrefixABeingSuffixB(b, a, kMerLength);
        if (baDistance < distance)
        {
            distance = baDistance;
            direction = Direction.bToA;
        }


        var bComplement = BasicKMerOperations.GetComplement(b, kMerLength);

        var abComplementDistance = FindLargestPrefixABeingSuffixB(a, bComplement, kMerLength);

        if (abComplementDistance < distance)
        {
            distance = abComplementDistance;
            direction = Direction.aToBComplement;
        }
        var baComplementDistance = FindLargestPrefixABeingSuffixB(bComplement, a, kMerLength);
        if (baComplementDistance < distance)
        {
            distance = baComplementDistance;
            direction = Direction.BComplementToA;
        }

        return (distance, direction);
    }


    public static (int Distance, Direction Direction) DetermineDistanceDirected(ulong a, ulong b, int kMerLength)
    {
        Direction direction = Direction.aToB;
        var distance = FindLargestPrefixABeingSuffixB(a, b, kMerLength);

        var bComplement = BasicKMerOperations.GetComplement(b, kMerLength);

        var baComplementDistance = FindLargestPrefixABeingSuffixB(a, bComplement, kMerLength);
        if (baComplementDistance < distance)
        {
            distance = baComplementDistance;
            direction = Direction.aToBComplement;
        }

        return (distance, direction);
    }



    public static IEnumerable<ulong> GetPathFromAToB(ulong from, ulong to, int kMerLength, int distance)
    {
        ulong remainder = to >> ((kMerLength - distance) * 2);

        for (int i = 0; i < distance - 1; i++)
        {
            ulong last = BasicKMerOperations.GetLast(remainder);
            remainder >>>= 2;
            from = BasicKMerOperations.RightShift(from);
            from |= (last << (kMerLength * 2 - 2));
            yield return from;
        }
    }
    public static IEnumerable<ulong> FindShortestPath(ulong a, ulong b, int kMerLength)
    {

        var (distance, direction) = DetermineDistance(a, b, kMerLength);

        ulong bComplement = BasicKMerOperations.GetComplement(b, kMerLength);

        ulong from;
        ulong to;

        switch (direction)
        {
            case Direction.aToB:
                from = a;
                to = b;
                break;
            case Direction.bToA:
                from = b;
                to = a;
                break;
            case Direction.aToBComplement:
                from = a;
                to = bComplement;
                break;
            case Direction.BComplementToA:
                from = bComplement;
                to = a;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return GetPathFromAToB(from, to, kMerLength, distance);
    }

    public static (HashSet<ulong>, HashSet<(ulong, ulong)>) CreateDNAGraphCanonical(int kMerLength)
    {
        ulong nKMer = 1UL << (kMerLength * 2);
        HashSet<ulong> nodes = new HashSet<ulong>((int)nKMer);
        HashSet<(ulong, ulong)> edges = new HashSet<(ulong, ulong)>();
        for (ulong i = 0; i < nKMer; i++)
        {
            nodes.Add(BasicKMerOperations.GetCanonical(i, kMerLength));
            foreach (var neighbor in BasicKMerOperations.GetRightNeighbors(i, kMerLength))
            {
                edges.Add((BasicKMerOperations.GetCanonical(i, kMerLength), BasicKMerOperations.GetCanonical(neighbor, kMerLength)));
            }
            foreach (var neighbor in BasicKMerOperations.GetLeftNeighbors(i, kMerLength))
            {
                edges.Add((BasicKMerOperations.GetCanonical(i, kMerLength), BasicKMerOperations.GetCanonical(neighbor, kMerLength)));
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
            answer[i] = BasicKMerOperations.GetRightNeighbors(answer[i - 1], kMerLength)[random.Next(4)];
        }

        for (int i = 0; i < pathLength; i++)
        {
            answer[i] = BasicKMerOperations.GetCanonical(answer[i], kMerLength);
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

    public static (ulong kMer, int distance, Direction direction) FindClosestDirected(ulong kMer, int kMerLength, ulong[] graph, int minDistance)
    {
        ulong closest = 0;
        int closestDistance = int.MaxValue;
        Direction direction = 0;

        if (graph.Length < 2) throw new ArgumentException("Graph must have at least 2 vertices");

        foreach (var vertex in graph)
        {
            if (vertex == kMer)
            {
                continue;
            }
            var (distance, dir) = DetermineDistanceDirected(kMer, vertex, kMerLength);
            if (distance < closestDistance && distance > minDistance)
            {
                closest = vertex;
                closestDistance = distance;
                direction = dir;
            }
        }

        return (closest, closestDistance, direction);
    }

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
                closestKMer = BasicKMerOperations.GetComplement(closestKMer, kMerLength);
            }
            answer = answer.Concat(GetPathFromAToB(vertex, closestKMer, kMerLength, distance));
        }
        return answer.Concat(graph).Select(x => BasicKMerOperations.GetCanonical(x, kMerLength)).ToArray();
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
        return answer.Concat(graph).Select(x => BasicKMerOperations.GetCanonical(x, kMerLength)).ToArray();
    }

    public static ulong[] RecoverGraphCanonicalV3(ulong[] graph, int kMerLength, int distanceCutoff, int minDistance)
    {
        IEnumerable<ulong> answer = new List<ulong>();

        ulong[] verticesTo = graph.Concat(graph.Select(x => BasicKMerOperations.GetComplement(x, kMerLength))).ToArray();

        List<ulong> verticesFrom = new List<ulong>(graph);
        for (int i = minDistance; i <= distanceCutoff; i++)
        {
            var (found, notFound) = FindVerticesInSetDistance(verticesFrom, verticesTo, i, kMerLength);
            foreach (var (from, to) in found)
            {
                answer = answer.Concat(GetPathFromAToB(from, to, kMerLength, i));
            }
            verticesFrom = notFound;
        }
        return answer.Concat(graph).Select(x => BasicKMerOperations.GetCanonical(x, kMerLength)).ToArray();
    }

    public static (List<(ulong from, ulong to)> found, List<ulong> notfound) FindVerticesInSetDistance(List<ulong> verticesFrom, ulong[] verticesTo, int distance, int kMerLength)
    {


        //This implemenation is not entirely correct
        //more vertices may have same prefix
        //we do not care
        Dictionary<ulong, ulong> dic = new(verticesTo.Length);
        for (int i = 0; i < verticesTo.Length; i++)
        {
            var kmer = verticesTo[i];
            var key = kmer >>> (distance * 2);
            if (!dic.ContainsKey(key))
            {
                dic.Add(key, kmer);
            }
        }


        List<(ulong, ulong)> found = new();
        List<ulong> notFound = new();

        ulong mask = (1UL << (int)((kMerLength - distance) * 2)) - 1;
        foreach (var vertex in verticesFrom)
        {
            if (dic.TryGetValue(vertex & mask, out var value))
            {
                if (vertex != value)
                {
                    //We are not interested in finding the same vertex
                    found.Add((value, vertex));
                }
                else
                {
                    notFound.Add(vertex);
                }
            }
            else
            {
                notFound.Add(vertex);
            }
        }
        return (found, notFound);
    }

    public class DistanceFromClosestFinder
    {
        readonly Dictionary<ulong, ulong>[] _buckets;
        readonly int _maxDistance;
        readonly int _minDistance;
        readonly int _kMerLength;
        public DistanceFromClosestFinder(ulong[] vertices, int kMerLength, int maxDistance, int minDistance)
        {
            _maxDistance = maxDistance;
            _minDistance = minDistance;
            _kMerLength = kMerLength;


            _buckets = new Dictionary<ulong, ulong>[kMerLength];

            for (int i = 0; i < kMerLength; i++)
            {
                _buckets[i] = new Dictionary<ulong, ulong>();
            }

            for (int index = 0; index < vertices.Length; index++)
            {
                var kMer = vertices[index];
                var kMerComplement = BasicKMerOperations.GetComplement(kMer, kMerLength);
                var suffix = kMer;
                var suffixComplement = kMerComplement;

                ulong mask = (1UL << (int)((kMerLength - minDistance) * 2)) - 1;
                int counter = minDistance;
                while (counter <= maxDistance)
                {
                    if (!_buckets[counter].ContainsKey(suffix & mask))
                        _buckets[counter].Add(suffix & mask, kMer);
                    if (!_buckets[counter].ContainsKey(suffixComplement & mask))
                        _buckets[counter].Add(suffixComplement & mask, kMerComplement);
                    mask >>>= 2;

                    counter++;
                }
            }
        }

        public (ulong closest, int distance) FindClosest(ulong kMer)
        {
            for (int i = _minDistance; i <= _maxDistance; i++)
            {
                ulong mask = 1UL << (int)(_maxDistance * 2) - 1;
                ulong suffix = kMer;
                if (_buckets[i].ContainsKey(kMer & mask))
                {
                    return (_buckets[i][kMer], i);
                }
                mask >>>= 2;
            }
            return (0, -1);
        }


    }

    public static (ulong correct, ulong missing, ulong wrong) EvaluateRecovery(ulong[] originalGraph, ulong[] recoveredGraph)
    {
        ulong correct = 0;
        ulong missing = 0;
        ulong wrong = 0;

        HashSet<ulong> recoveredGraphHashSet = new(recoveredGraph);
        HashSet<ulong> originalGraphHashSet = new(originalGraph);

        foreach (var vertex in originalGraph)
        {
            if (recoveredGraphHashSet.Contains(vertex))
            {
                correct++;
            }
            else
            {
                missing++;
            }
        }

        foreach (var vertex in recoveredGraph)
        {
            if (!originalGraphHashSet.Contains(vertex))
            {
                wrong++;
            }
        }

        return (correct, missing, wrong);
    }

}