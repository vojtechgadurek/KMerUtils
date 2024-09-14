using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KMerUtils.DNAGraph
{
    public static class Evaluate
    {
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

        public enum RecoveryState
        {
            Given,
            Recovered,
            NotRecovered,
            Wrong
        }

        public static IEnumerable<RecoveryState> EvaluatePathRecovery(ulong[] originalGraph, HashSet<ulong> graphBeforeRecovery, HashSet<ulong> recoveredGraph)
        {
            return originalGraph
                .Select(
                    x => graphBeforeRecovery.Contains(x) ?
                        RecoveryState.Given :
                            recoveredGraph.Contains(x) ?
                               RecoveryState.Recovered :
                               RecoveryState.NotRecovered
                    );

        }
    }
}
