using KMerUtils.KMer;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static KMerUtils.KMer.Utils;

namespace KMerUtils.DNAGraph
{
    public static class Info
    {
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

                //Console.WriteLine($"{kMer.ToStringRepre(_kMerLength)} -> {vertex.ToStringRepre(_kMerLength)}: {distance} {dir}");

                if (distance < closestDistance && distance >= minDistance)
                {
                    closest = vertex;
                    closestDistance = distance;
                    direction = dir;
                }
            }

            return (closest, closestDistance, direction);
        }



        public static (List<(ulong from, ulong to)> found, List<ulong> notfound) FindVerticesInSetDistance<T1, T2>(T1 verticesFrom, T2 verticesTo, int distance, int kMerLength)
            where T1 : IEnumerable<ulong>
            where T2 : IEnumerable<ulong>
        {
            //Console.WriteLine(distance);


            //This implemenation is not entirely correct
            //more vertices may have same prefix
            //we do not care

            //AABBCC & __**** -> __BBCC
            //BBCCAA >> 2 -> __BBCC
            //AABBCCAA

            Dictionary<ulong, ulong> dic = new();
            foreach (var kmer in verticesTo)
            {
                var key = kmer >>> (distance * 2);

                //if (kmer == "GATATGGCAACAATATTATGTTTCCCGGATC".ToKMer())
                //    Console.WriteLine(key.ToStringRepre(_kMerLength));
                //if (kmer == "CCGGGAAACATAATATTGTTGCCATATCCGA".ToKMer())
                //    Console.WriteLine(key.ToStringRepre(_kMerLength));

                if (!dic.ContainsKey(key))
                {
                    dic.Add(key, kmer);
                }
            }


            List<(ulong, ulong)> found = new();
            List<ulong> notFound = new();


            ulong mask = (1UL << (int)((kMerLength - distance) * 2)) - 1;
            //Console.WriteLine("S");

            foreach (var vertex in verticesFrom)
            {
                //if (vertex == "CCGGGAAACATAATATTGTTGCCATATCCGA".ToKMer())
                //{
                //    //Console.WriteLine(value.ToStringRepre(_kMerLength) + " " + vertex.ToStringRepre(_kMerLength));
                //    Console.WriteLine((vertex & _mask).ToStringRepre(_kMerLength));
                //}

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
            //Console.WriteLine("E");

            return (found, notFound);
        }

        public static IEnumerable<ulong> FindShortestPath(ulong a, ulong b, int kMerLength)
        {

            var (distance, direction) = DetermineDistance(a, b, kMerLength);

            ulong bComplement = GetComplement(b, kMerLength);

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
                    var kMerComplement = Utils.GetComplement(kMer, kMerLength);
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


            var bComplement = Utils.GetComplement(b, kMerLength);

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

            var bComplement = Utils.GetComplement(b, kMerLength);

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
                ulong last = Utils.GetLast(remainder);
                remainder >>>= 2;
                from = Utils.RightShift(from);
                from |= (last << (kMerLength * 2 - 2));
                yield return from;
            }
        }


        public static (List<(ulong from, ulong to)> found, List<ulong> notfound) FindVerticesInSetDistance2<T1, T2>(T1 verticesTo, T2 verticesFrom, int distance, int kMerLength)
            where T1 : IEnumerable<ulong>
            where T2 : IEnumerable<ulong>
        {
            //Console.WriteLine(distance);


            //This implemenation is not entirely correct
            //more vertices may have same prefix
            //we do not care

            const ulong rodeMaskFirst = 0b1100110011001100110011001100110011001100110011001100110011001100UL;
            const ulong rodeMaskSecond = 0b0011001100110011001100110011001100110011001100110011001100110011UL;
            const int acceptedNumberMutations = 2;

            Dictionary<ulong, ulong> dicFirst = new();
            Dictionary<ulong, ulong> dicSecond = new();

            foreach (var kmer in verticesFrom)
            {
                var key = kmer >>> (distance * 2);

                //if (kmer == "GATATGGCAACAATATTATGTTTCCCGGATC".ToKMer())
                //    Console.WriteLine(key.ToStringRepre(_kMerLength));
                //if (kmer == "CCGGGAAACATAATATTGTTGCCATATCCGA".ToKMer())
                //    Console.WriteLine(key.ToStringRepre(_kMerLength));

                if (!dicFirst.ContainsKey(key & rodeMaskFirst))
                {
                    dicFirst.Add(key & rodeMaskFirst, kmer);
                }
                if (!dicSecond.ContainsKey(key & rodeMaskSecond))
                {
                    dicSecond.Add(key & rodeMaskSecond, kmer);
                }
            }


            List<(ulong, ulong)> found = new();
            List<ulong> notFound = new();


            ulong mask = (1UL << (int)((kMerLength - distance) * 2)) - 1;

            var count = 0;
            var countA = 0;
            var countB = 0;
            //Console.WriteLine("S");
            foreach (var vertex in verticesTo)
            {
                //if (vertex == "CCGGGAAACATAATATTGTTGCCATATCCGA".ToKMer())
                //{
                //    //Console.WriteLine(value.ToStringRepre(_kMerLength) + " " + vertex.ToStringRepre(_kMerLength));
                //    Console.WriteLine((vertex & _mask).ToStringRepre(_kMerLength));
                //}

                bool firstSucc = dicFirst.TryGetValue((vertex & mask) & rodeMaskFirst, out ulong valueFirst);
                bool secondSucc = dicSecond.TryGetValue((vertex & mask) & rodeMaskSecond, out ulong valueSecond);

                if (firstSucc == false && secondSucc == false) notFound.Add(vertex);


                if (firstSucc != secondSucc)
                {
                    countB++;
                    var succvalue = firstSucc ? valueFirst : valueSecond;
                    var succmask = (succvalue >>> (distance * 2)) ^ (vertex & mask);
                    //Console.WriteLine(succmask.ToStringRepre(_kMerLength));
                    //Console.WriteLine(BitOperations.PopCount(succmask));

                    if (succvalue == vertex) notFound.Add(vertex);
                    else if (succmask > 0 && BitOperations.PopCount(succmask) <= acceptedNumberMutations)
                    {
                        //find where the mutation is
                        count++;
                        BitOperations.TrailingZeroCount(succmask);
                        //Console.WriteLine((succvalue >>> (2 * distance)).ToStringRepre(_kMerLength));
                        //Console.WriteLine(vertex.ToStringRepre(_kMerLength));
                        //Console.WriteLine((vertex ^ succmask).ToStringRepre(_kMerLength));

                        //Console.WriteLine(succmask.ToStringRepre(_kMerLength));

                        found.Add((succvalue, vertex ^ succmask));
                        found.Add((succvalue ^ (succmask << (2 * distance)), vertex));

                    }
                    else notFound.Add(vertex);
                }

                if (firstSucc && secondSucc)
                {
                    countA++;
                    if (valueFirst != vertex) found.Add((valueFirst, vertex));
                    else if (valueSecond != vertex) found.Add((valueSecond, vertex));
                    else notFound.Add(vertex);
                }



            }
            //Console.WriteLine("E");
            //Console.WriteLine((countA, distance, "Same"));
            //Console.WriteLine((count, distance, "Mut"));
            //Console.WriteLine((countB, distance, "MutA"));


            return (found, notFound);
        }

    }
}
