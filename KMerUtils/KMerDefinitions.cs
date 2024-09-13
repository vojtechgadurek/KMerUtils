using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KMerUtils
{
    //ToDo add type safety to KMer to easily see, whether they have header, if they are canonical, etc.

    public interface IConstant<T>
    {
        public T Get();
    }
    public struct CI32_31 : IConstant<int>
    {
        public int Get() => 31;
    }

    public struct CBool_true : IConstant<bool>
    {
        public bool Get() => true;
    }

    public struct CBool_false : IConstant<bool>
    {
        public bool Get() => false;
    }

    record struct KMer<TLength, TCanonic>
        where TLength : struct, IConstant<int>
        where TCanonic : struct, IConstant<bool>
    {
        public ulong Value;
        public static readonly ulong LengthMask = (1UL << (default(TLength).Get() * 2)) - 1UL;
        public KMer(ulong value)
        {
            Value = value;
        }
    }
}
