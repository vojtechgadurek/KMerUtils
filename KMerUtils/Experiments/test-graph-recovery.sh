using System;
path="/workspaces/codespaces-blank/KmerUtils/KMerUtils/bin/Debug/net8.0/KMerUtils"
# path {kmerlength} {nMutations} {probabilityStart} {seed} {distanceCutoff} {nTests} {minDistance} {doublePath} {probabilityStep}
probabilityStart="0"
probabilityStep="0.01"
numberTests="1"

doublePath="false true"
cuttoff="5 10 20"

for i in $doublePath; do
    for j in $cuttoff; do
        $path "measure-recovery" "31" "100" $probabilityStart "42" $j $numberTests "0" $i $probabilityStep > "Results/31-100-0-42-"$j"-10-1-0-"$i"-0.01.txt"
    done
done
