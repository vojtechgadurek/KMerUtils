using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;

namespace KMerUtils;
public class Program
{
    public static void Main(string[] args)
    {
        if (args[0] == "prt-GRAPH")
        {

            int kMerLength = int.Parse(args[1]);
            var (nodes, edges) = DnaGraph.CreateDNAGraph(kMerLength);

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

            Random random = new Random(seed);

            var (originalGraph, graphForRecovery) = DnaGraph.GenerateGraphForRecovery(kMerLength, nMutations, probability, random);

            Console.WriteLine(
                $"original length: {originalGraph.Sum(x => x.Length)} for recovery length: {graphForRecovery.Length})"
            );

            var recovered = DnaGraph.RecoverGraph(graphForRecovery, kMerLength, 14);
            Console.WriteLine(
                DnaGraph.EvaluateRecovery(originalGraph.SelectMany(x => x).Select(BasicKMerOperations.GetCanonical).ToArray(), recovered)
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
    /// Removes header from kMer
    /// </summary>
    /// <param name="kMer"></param>
    /// <returns></returns>
    public static ulong RemoveHeader(ulong kMer)
    {
        kMer >>>= 2;
        return kMer;
    }
    /// <summary>
    /// Add header to kMer 
    /// </summary>
    /// <param name="kMer"></param>
    /// <returns></returns>
    public static ulong AddHeader(ulong kMer)
    {
        return 0b11 | (kMer << 2);
    }
    /// <summary>
    /// Return the reverse complement of a kMer
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
    /// Calculate reverse complement of a kMer and pick the canonical one
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
            default: throw new ArgumentException("Invalid kMer");
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
            default: throw new ArgumentException("Invalid kMer");
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
    /// Returns the n-th character of a kMer
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
    /// Return string representation of a kMer
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

    public static (HashSet<ulong>, HashSet<(ulong, ulong)>) CreateDNAGraph(int kMerLength)
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

    public static (ulong kMer, int distance, Direction direction) FindClosestDirected(ulong kMer, int kMerLength, ulong[] graph)
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
            if (distance < closestDistance)
            {
                closest = vertex;
                closestDistance = distance;
                direction = dir;
            }
        }

        return (closest, closestDistance, direction);
    }

    public static ulong[] RecoverGraph(ulong[] graph, int kMerLength, int distanceCutoff)
    {
        IEnumerable<ulong> answer = new List<ulong>();
        foreach (var vertex in graph)
        {
            var (closestKMer, distance, direction) = FindClosestDirected(vertex, kMerLength, graph);
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
        return answer.Concat(graph).Select(BasicKMerOperations.GetCanonical).ToArray();
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