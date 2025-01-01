using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static KMerUtils.DNAGraph.Info;
using KMerUtils.KMer;
using System.Numerics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualBasic;
using System.Runtime.CompilerServices;
using System.Collections.Frozen;

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

                //Console.WriteLine(Distance);
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

        public class DoublePathV2
        {
            record struct PathNode(
                ulong KMer,
                int Distance,
                bool IsSeed
            );

            int _LeftIndex;
            int _RightIndex;

            ulong _Difference;
            int _kMerLength;

            List<PathNode> _path = new();

            public (ulong, ulong) GetFirst()
            {
                return (_path[0].KMer, _path[0].KMer ^ (_Difference << ((_kMerLength - _LeftIndex) * 2)));
            }

            public (ulong, ulong) GetLast()
            {
                return (_path[^1].KMer, _path[^1].KMer ^ (_Difference << ((_kMerLength - _RightIndex) * 2)));
            }

            public DoublePathV2((ulong first, ulong second, int positionFromLeft) seed, int kMerLength)
            {
                _kMerLength = kMerLength;
                _RightIndex = (kMerLength - seed.positionFromLeft) * 2 - 2;
                _LeftIndex = _RightIndex;
                _Difference = seed.first ^ seed.second >>> (_kMerLength - _RightIndex);
                _path.Add(new PathNode(GetCanonUlong(seed.first, _LeftIndex), 0, true));
            }

            public ulong GetCanonUlong(ulong kmer, int position)
            {
                var otherkmer = kmer ^ (_Difference << ((_kMerLength - _RightIndex) * 2));
                return otherkmer > kmer ? kmer : otherkmer;
            }

            public bool TryAddRight(ulong kmer, int distance)
            {
                //AABBCC
                //BBCCDD
                if (distance > _RightIndex) return false;
                _RightIndex -= distance;
                _path.Add(new PathNode(GetCanonUlong(kmer, _RightIndex), distance, false));
                return true;
            }

            public bool TryAddLeft(ulong kmer, int distance)
            {
                if (distance > _LeftIndex) return false;
                _LeftIndex -= distance;
                _path.Insert(0, new PathNode(GetCanonUlong(kmer, _LeftIndex), distance, false));
                return true;
            }


        }

        public class DoublePath
        {
            readonly UInt128 _difference;
            UInt128 _path;

            //Left toVertices right
            int LeftIndex;
            int RightIndex;

            List<(ulong, int)> KMersOnPath = new();

            int DistanceJumped = 0;
            int KmerIn = 0;
            public int Size => DistanceJumped;
            readonly int _kMerLength;

            public DoublePath((ulong first, ulong second, int positionFromLeft) seed, int kMerLength)
            {
                //Console.WriteLine(eed.First.ToStringRepre(kMerLength));
                //Console.WriteLine(eed.second.ToStringRepre(kMerLength));
                //Console.WriteLine(eed.positionFromRigth);

                this._kMerLength = kMerLength;
                RightIndex = (kMerLength - seed.positionFromLeft) * 2 - 2;
                LeftIndex = RightIndex;
                _difference = (UInt128)(seed.first ^ seed.second) << RightIndex;


                _path = ((UInt128)seed.first << (RightIndex));

                if ((_path ^ _difference) > _path) _path = _path ^ _difference;

                KMersOnPath.Add((seed.first, 0));

            }

            public (ulong, ulong) GetLast()
            {
                UInt128 p1 = _path >>> (RightIndex * 2);
                UInt128 p2 = (_path ^ _difference) >>> (RightIndex * 2);
                return ((ulong)p1, (ulong)p2);
            }

            public (ulong, ulong) GetFirst()
            {
                UInt128 p1 = _path >> (LeftIndex);
                UInt128 p2 = (_path ^ _difference) >> (LeftIndex);
                return ((ulong)(p1), (ulong)(p2));
            }

            public bool TryAddRight(ulong kmer, int distance)
            {
                //__AABB____ + BBCC -- Distance 2
                //__AABBCC__
                if (distance > RightIndex) return false;

                UInt128 k = kmer;
                k <<= ((RightIndex - distance) * 2);
                _path |= k;
                RightIndex -= distance;
                DistanceJumped += distance;
                KmerIn++;
                KMersOnPath.Add((kmer, distance));

                return true;
            }

            public IEnumerable<ulong> GetPath()
            {
                var p = _path >>> (RightIndex * 2);
                for (int i = 0; i < DistanceJumped; i++)
                {
                    yield return (ulong)p;
                    yield return (ulong)(p ^ _difference);
                    p >>>= 2;
                }
            }

            public override string ToString()
            {
                var (x, y) = GetFirst();
                return $"KMER - {_path.ToStringRepre(_kMerLength * 2 - 1)}\n" +
                    $"DIFF - {_difference.ToStringRepre(_kMerLength * 2 - 1)}\n" +
                    $"{((ulong)x).ToStringRepre(32)} - {((ulong)y).ToStringRepre(32)}"
                    + string.Join('\n', KMersOnPath.Select(x => x.Item1.ToStringRepre(_kMerLength) + " " + x.Item2.ToString()));
            }
        }

        public record struct Seed(ulong First, ulong Difference, int positionFromRigth);

        public static IEnumerable<ulong> RecoverGraph<T>(T graph, int kMerLength, int seedMaxDistance, int distanceCutoff, int minDistance)
        where T : IEnumerable<ulong>
        {
            var vertices = graph.Concat(graph.Select(x => Utils.GetComplement(x, kMerLength))).ToHashSet();

            var seedsGroups = FindSeeds(vertices, kMerLength).Select(
                x => new Seed((x.first > x.second) ? x.first : x.second, x.first ^ x.second, x.positionDifference))
                .GroupBy(x => (x.Difference >> (x.positionFromRigth * 2), (x.First >> (x.positionFromRigth * 2)) & 0b11));
            ;


            //foreach (var seedsBase in seedsGroups)
            //{
            //    Console.WriteLine((seedsBase.Key.Item1.ToStringRepre(1), seedsBase.Key.Item2.ToStringRepre(1)));


            //    foreach (var seed in seedsBase)
            //    {
            //        Console.WriteLine((seed.First.ToStringRepre(kMerLength), seed.Difference.ToStringRepre(kMerLength), (seed.First ^ seed.Difference).ToStringRepre(kMerLength), seed.positionFromRigth));
            //    }

            //}

            //var allEdges = new List<List<(ulong Key, (ulong, int) Value)>>();

            var seededPaths = new List<List<Path>>();

            foreach (var seedsBase in seedsGroups)
            {

                //Console.WriteLine((seedsBase.Key.Item1.ToStringRepre(1), seedsBase.Key.Item2.ToStringRepre(1)));

                var fromVertices = seedsBase.Select(x => x.First).ToHashSet();
                var toVertices = seedsBase.Select(x => x.First).ToHashSet();
                var edges = new List<(ulong, (ulong, int))>();
                List<(ulong from, ulong to)> found;
                List<ulong> notFound;
                HashSet<ulong> endpoints = seedsBase.Select(x => x.First).ToHashSet();

                for (int i = minDistance; i <= distanceCutoff; i++)//seedMaxDistance; i++)
                {
                    Random r = new();
                    //var j = r.Next(minDistance, Math.Min(i, distanceCutoff));
                    var j = i;

                    (found, notFound) = FindVerticesInSetDistance(fromVertices, toVertices, j, kMerLength);


                    Random.Shared.Shuffle(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(found));

                    foreach (var (fromVertex, toVertex) in found)
                    {

                        if (fromVertices.Contains(fromVertex) && toVertices.Contains(toVertex))
                        {
                            edges.Add((fromVertex, (toVertex, j)));
                            fromVertices.Remove(fromVertex);
                            toVertices.Remove(toVertex);
                            endpoints.Remove(toVertex);
                        }
                    }



                }
                var seedsDic = seedsBase.ToFrozenDictionary(x => x.First);
                var dicEdges = edges.ToFrozenDictionary(x => x.Item1, x => x.Item2);
                seededPaths.Add(endpoints.Select(x =>
                {

                    var p = new Path(seedsDic[x], kMerLength);
                    var next = x;
                    while (true)
                    {
                        if (!dicEdges.TryGetValue(next, out var nextnext)) break;
                        next = nextnext.Item1;
                        if (p.AddSeed(seedsDic[next])) break;
                    }
                    return p;

                }).ToList());
                //foreach (var edge in edges) Console.WriteLine((edge.Key.ToStringRepre(kMerLength), edge.Value.Item1.ToStringRepre(kMerLength), edge.Value.Item2));

            }

            return seededPaths.SelectMany(x => x).SelectMany(x => x.GetPath()).Concat(graph).ToArray();

            //var dic = seedsGroups.SelectMany(x => x).ToDictionary(x => x.First);


            //return allEdges.SelectMany(x => x.Select(y => Info.GetPathFromAToB(y.Key, y.Value.Item1, kMerLength, y.Value.Item2)).SelectMany(x => x)).Concat(
            // allEdges.SelectMany(x => x.Select(y => Info.GetPathFromAToB(dic[y.Key].Difference ^ y.Key, dic[y.Value.Item1].Difference ^ y.Value.Item1, kMerLength, y.Value.Item2)).SelectMany(x => x))).ToArray();




            //var _seeds = seedsBase.ToDictionary(x => x.GetFirst().Item1, x => x);



            //vertices.ExceptWith(_seeds.Select(x => x.Value.GetFirst().Item1));
            //vertices.ExceptWith(_seeds.Select(x => x.Value.GetFirst().Item2));

            //Console.WriteLine(_seeds.Count());

            ////Merge _seeds
            //for (int i = minDistance; i <= seedMaxDistance; i++)
            //{
            //    Dictionary<ulong, ulong> removed = new();
            //    var (found, notFound) = FindVerticesInSetDistance(_seeds.Select(x => x.Value.GetFirst().Item1), _seeds.Select(x => x.Value.GetLast().Item1), i, kMerLength);
            //    foreach (var (fromVertices, toVertices) in found)
            //    {
            //        if (_seeds.TryGetValue(fromVertices, out var seed))
            //        {
            //            if (seed.TryAddRight(toVertices, i))
            //            {
            //                removed.Add(toVertices, fromVertices);
            //                _seeds.Remove(toVertices);
            //            }
            //        }
            //        else
            //        {
            //            ulong newFrom = fromVertices;
            //            while (removed.TryGetValue(newFrom, out var from2))
            //            {
            //                newFrom = from2;
            //            }

            //            if (_seeds[newFrom].TryAddRight(toVertices, i))
            //            {
            //                _seeds.Remove(toVertices);
            //            };


            //        }
            //    }

            //}

            //var dicSeed = _seeds.Values.Select(x => (x.GetFirst().Item2, x)).Concat(_seeds.Values.Select(x => (x.GetFirst().Item1, x)))
            //    .ToDictionary();

            ////Add the rest of the vertices
            //for (int i = minDistance; i <= distanceCutoff; i++)
            //{
            //    var (found, notFound) = FindVerticesInSetDistance(vertices, dicSeed.Keys, i, kMerLength);
            //    foreach (var (fromVertices, toVertices) in found)
            //    {
            //        if (dicSeed[fromVertices].TryAddRight(toVertices, i))
            //        {
            //            vertices.Remove(toVertices);
            //        }
            //    }
            //}

            //Console.WriteLine(_seeds.Count());

            //Console.WriteLine(_seeds.Average(x => x.Value.Size));

            //foreach (var seed in _seeds.Values)
            //{
            //    Console.WriteLine(seed);
            //}
        }

        public static int? LocateMutation(ulong a, ulong b)
        {
            var diff = a ^ b;
            if (diff == 0) return null;
            var position = ((BitOperations.TrailingZeroCount(diff)) / 2) * 2;
            if (diff > (11UL << position)) return null;
            else return position / 2;
        }

        public class Path
        {
            List<(ulong, int)> _seeds;
            List<(ulong, int)> _normalVertices;
            (ulong, ulong) _type;
            int _indexRigth;
            int _indexLeft;
            int _length;
            UInt128 _path;
            UInt128 _mask;
            UInt128 _difference;

            public Path(Seed start, int length)
            {
                //Console.WriteLine(start.First.ToStringRepre(31));
                //Console.WriteLine(start.Difference.ToStringRepre(31));
                //Console.WriteLine(start.positionFromRigth);

                _seeds = new List<(ulong, int)>([(start.First, start.positionFromRigth)]);
                _type = (start.First >> (start.positionFromRigth * 2), start.Difference >> (start.positionFromRigth * 2));
                _path = ((UInt128)start.First) << ((length - 1 - start.positionFromRigth) * 2);
                _mask = (((UInt128)0b1UL << ((length * 2))) - 1) << ((length - 1 - start.positionFromRigth) * 2);
                _difference = ((UInt128)start.Difference) << ((length - 1 - start.positionFromRigth) * 2);
                _indexRigth = start.positionFromRigth;
                _indexLeft = start.positionFromRigth + length;
                _length = length;

                //Console.WriteLine(_mask.ToStringRepre(length * 2 - 1));

            }

            public bool AddSeed(Seed next)
            {
                //Check if the seed is compatible
                var path2 = ((UInt128)next.First) << ((_length - 1 - next.positionFromRigth) * 2);
                var maskpath2 = ((((UInt128)0b1 << (_length * 2)) - 1)) << ((_length - 1 - next.positionFromRigth) * 2);


                //Console.WriteLine("A");
                //Console.WriteLine(path2.ToStringRepre(_length * 2 - 1));
                //Console.WriteLine(maskpath2.ToStringRepre(_length * 2 - 1));
                //Console.WriteLine(_path.ToStringRepre(_length * 2 - 1));
                //Console.WriteLine(_mask.ToStringRepre(_length * 2 - 1));



                var gmask = maskpath2 & _mask;

                //Console.WriteLine(gmask.ToStringRepre(_length * 2 - 1));

                if ((_path & gmask) != (path2 & gmask))
                {
                    return false;
                }

                _seeds.Add((next.First, next.positionFromRigth));

                _path |= path2;
                _mask |= maskpath2;

                _indexRigth = Math.Min(_indexRigth, next.positionFromRigth);
                _indexLeft = Math.Max(_indexLeft, next.positionFromRigth + _length);

                //Console.WriteLine(_path.ToStringRepre(_length * 2 - 1));
                //Console.WriteLine(_mask.ToStringRepre(_length * 2 - 1));

                return true;

            }

            public IEnumerable<ulong> GetPath()
            {
                if (_indexLeft - _length - _indexRigth > 0)
                {
                    Console.WriteLine(_path.ToStringRepre(_length * 2 - 1));
                    Console.WriteLine((_indexLeft, _indexRigth));
                    foreach (var x in _seeds)
                    {
                        Console.WriteLine((x.Item1.ToStringRepre(_length), 1));
                    }
                }
                for (int i = (_indexRigth); i <= (_indexLeft - _length); i++)
                {
                    if (_indexLeft - _length - _indexRigth > 0)
                        Console.WriteLine(((ulong)(_path >> ((_length - i - 1) * 2))).ToStringRepre(_length));
                    yield return ((ulong)(_path >>> ((_length - i - 1) * 2)));
                    yield return ((ulong)((_path ^ _difference) >>> ((_length - i - 1) * 2)));

                }
            }
        }

        public static IEnumerable<(ulong first, ulong second, int positionDifference)> FindSeeds<T>(T graph, int kMerLength) where T : IEnumerable<ulong>
        {
            const ulong rodeMaskFirst = 0b1100110011001100110011001100110011001100110011001100110011001100UL;
            const ulong rodeMaskSecond = 0b0011001100110011001100110011001100110011001100110011001100110011UL;

            Dictionary<ulong, List<ulong>> dicFirst = new(graph.Count());
            Dictionary<ulong, List<ulong>> dicSecond = new(graph.Count());

            (ulong, ulong, int) FAIL = (0UL, 0UL, -1);

            foreach (var kmer in graph)
            {
                var key = kmer;

                if (!dicFirst.ContainsKey(key & rodeMaskFirst))
                {
                    dicFirst.Add(key & rodeMaskFirst, new List<ulong>([kmer]));
                }
                else
                {
                    dicFirst[key & rodeMaskFirst].Add(kmer);
                }
                if (!dicSecond.ContainsKey(key & rodeMaskSecond))
                {
                    dicSecond.Add(key & rodeMaskSecond, new List<ulong>([kmer]));
                }
                else
                {
                    dicSecond[key & rodeMaskSecond].Add(kmer);
                }
            }


            return dicFirst.Concat(dicSecond).Where(x => x.Value.Count() == 2).Select(
                x =>
                {
                    var first = x.Value[0];
                    var second = x.Value[1];

                    var mutationLocation = LocateMutation(first, second);
                    if (mutationLocation == null) return FAIL;

                    return (first, second, (int)mutationLocation);
                }
                ).
                Where(x => FAIL != x);
            ;

        }

        public static ulong[] RecoverGraphCanonicalV3(ulong[] graph, int kMerLength, int distanceCutoff, int minDistance, bool addGraph = true)
        {
            IEnumerable<ulong> answer = new List<ulong>();

            var verticesTo = graph.Concat(graph.Select(x => Utils.GetComplement(x, kMerLength))).ToHashSet();

            var verticesFrom = verticesTo.ToList();
            for (int i = minDistance; i <= distanceCutoff; i++)
            {
                var (found, notFound) = FindVerticesInSetDistance2(verticesTo, verticesFrom, i, kMerLength);
                foreach (var (from, to) in found)
                {
                    answer = answer.Concat(GetPathFromAToB(from, to, kMerLength, i));
                }
                verticesFrom = notFound;
                verticesTo.ExceptWith(found.Select(x => x.to));

                //found.ForEach(x => Console.WriteLine((x.fromVertices.ToStringRepre(_kMerLength), x.toVertices.ToStringRepre(_kMerLength), i)));

                //if (i == distanceCutoff) Console.WriteLine(notFound.Count);
            }

            if (addGraph)
            {
                answer = answer.Concat(graph);
            }

            return answer.ToArray();
        }

        public static IEnumerable<ulong> FindPathFromLeftEndpoint(ulong endpoint, HashSet<ulong> vertices, int maxLength, int kMerLength)
        {
            for (int i = 0; i < maxLength; i++)
            {
                yield return endpoint;
                var answ = Utils.GetRightNeighbors(endpoint, kMerLength)
                .Where(x => vertices.Contains(x))
                .Select(x => (ulong?)x).FirstOrDefault();
                if (answ is null)
                {
                    break;
                }
                endpoint = (ulong)answ;
            }
        }
        public static IEnumerable<IEnumerable<ulong>> FindPaths(ulong[] graph, int kMerLength, int maxLength)
        {
            ulong[] graphWithComplement = graph.Concat(graph.Select(x => Utils.GetComplement(x, kMerLength))).ToArray();

            //Find left endpoints

            HashSet<ulong> graphWithComplementHash = new HashSet<ulong>(graphWithComplement);

            var leftEndpoints =
                graphWithComplementHash.Where(x =>
                Utils.GetCanonical(x, kMerLength) == x
                &
                Utils.GetLeftNeighbors(x, kMerLength).All(x => !graphWithComplementHash.Contains(x)));

            //Console.WriteLine($"A{leftEndpoints.Count()}");

            return leftEndpoints.Select(
                 x => FindPathFromLeftEndpoint(x, graphWithComplementHash, maxLength, kMerLength)
                 );

        }

        public static ulong[] RecoverGraphCanonicalV4(ulong[] graph, int kMerLength, int distanceCutoff, int minDistance, bool addGraph = true)
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

                //found.ForEach(x => Console.WriteLine((x.fromVertices.ToStringRepre(_kMerLength), x.toVertices.ToStringRepre(_kMerLength), i)));

                //if (i == distanceCutoff) Console.WriteLine(notFound.Count);
            }

            if (addGraph)
            {
                answer = answer.Concat(graph);
            }

            return answer.ToArray();
        }
    }
}
