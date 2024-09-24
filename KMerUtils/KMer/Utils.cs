using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static KMerUtils.KMer.Utils;

namespace KMerUtils.KMer
{
    public static class KMerExtension
    {
        public static ulong ToKMer(this string kMer)
        {
            return TranslateStringToUlong(kMer);
        }

        public static string ToStringRepre(this ulong kMer, int kMerLength)
        {
            return TranslateUlongToString(kMer, kMerLength);
        }

        public static ulong ToCanonical(this ulong kMer, int kMerLength)
        {
            return GetCanonical(kMer, kMerLength);
        }

        public static ulong ToComplement(this ulong kMer, int kMerLength)
        {
            return GetComplement(kMer, kMerLength);
        }
    }
    public static class Utils
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
                neighbors[i] = (kMer >> 2) | (i << (((int)kMerLength - 1) * 2));
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

}
public static class RandomExtensions
{
    public static ulong NextRandomUInt64(this Random random)
    {
        return (ulong)random.NextInt64();
    }
}

