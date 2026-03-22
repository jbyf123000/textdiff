using TextDiff.Desktop.Models;

namespace TextDiff.Desktop.Services;

public static class DiffEngine
{
    public static DiffResult Compare(string? leftText, string? rightText)
    {
        var leftLines = SplitLines(leftText);
        var rightLines = SplitLines(rightText);
        var operations = BuildOperations(leftLines, rightLines);

        var leftChanges = new Dictionary<int, DiffChangeKind>();
        var rightChanges = new Dictionary<int, DiffChangeKind>();
        var sameCount = 0;
        var modifiedCount = 0;
        var leftOnlyCount = 0;
        var rightOnlyCount = 0;
        var leftLineNumber = 1;
        var rightLineNumber = 1;

        for (var index = 0; index < operations.Count; index++)
        {
            if (operations[index].Kind == DiffChangeKind.Same)
            {
                sameCount++;
                leftLineNumber++;
                rightLineNumber++;
                continue;
            }

            var leftBlockLength = 0;
            var rightBlockLength = 0;
            while (index < operations.Count && operations[index].Kind != DiffChangeKind.Same)
            {
                if (operations[index].Kind == DiffChangeKind.LeftOnly)
                {
                    leftBlockLength++;
                }
                else if (operations[index].Kind == DiffChangeKind.RightOnly)
                {
                    rightBlockLength++;
                }

                index++;
            }

            var pairCount = Math.Min(leftBlockLength, rightBlockLength);

            for (var offset = 0; offset < pairCount; offset++)
            {
                leftChanges[leftLineNumber] = DiffChangeKind.Modified;
                rightChanges[rightLineNumber] = DiffChangeKind.Modified;
                modifiedCount++;
                leftLineNumber++;
                rightLineNumber++;
            }

            for (var offset = pairCount; offset < leftBlockLength; offset++)
            {
                leftChanges[leftLineNumber] = DiffChangeKind.LeftOnly;
                leftOnlyCount++;
                leftLineNumber++;
            }

            for (var offset = pairCount; offset < rightBlockLength; offset++)
            {
                rightChanges[rightLineNumber] = DiffChangeKind.RightOnly;
                rightOnlyCount++;
                rightLineNumber++;
            }

            index--;
        }

        return new DiffResult(leftChanges, rightChanges, sameCount, modifiedCount, leftOnlyCount, rightOnlyCount);
    }

    private static IReadOnlyList<string> SplitLines(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Array.Empty<string>();
        }

        return text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');
    }

    private static List<DiffOperation> BuildOperations(IReadOnlyList<string> leftLines, IReadOnlyList<string> rightLines)
    {
        var lcs = BuildLcsTable(leftLines, rightLines);
        var operations = new List<DiffOperation>();
        var leftIndex = 0;
        var rightIndex = 0;

        while (leftIndex < leftLines.Count && rightIndex < rightLines.Count)
        {
            if (leftLines[leftIndex] == rightLines[rightIndex])
            {
                operations.Add(new DiffOperation(DiffChangeKind.Same));
                leftIndex++;
                rightIndex++;
            }
            else if (lcs[leftIndex + 1, rightIndex] >= lcs[leftIndex, rightIndex + 1])
            {
                operations.Add(new DiffOperation(DiffChangeKind.LeftOnly));
                leftIndex++;
            }
            else
            {
                operations.Add(new DiffOperation(DiffChangeKind.RightOnly));
                rightIndex++;
            }
        }

        while (leftIndex < leftLines.Count)
        {
            operations.Add(new DiffOperation(DiffChangeKind.LeftOnly));
            leftIndex++;
        }

        while (rightIndex < rightLines.Count)
        {
            operations.Add(new DiffOperation(DiffChangeKind.RightOnly));
            rightIndex++;
        }

        return operations;
    }

    private static int[,] BuildLcsTable(IReadOnlyList<string> leftLines, IReadOnlyList<string> rightLines)
    {
        var lcs = new int[leftLines.Count + 1, rightLines.Count + 1];

        for (var leftIndex = leftLines.Count - 1; leftIndex >= 0; leftIndex--)
        {
            for (var rightIndex = rightLines.Count - 1; rightIndex >= 0; rightIndex--)
            {
                lcs[leftIndex, rightIndex] = leftLines[leftIndex] == rightLines[rightIndex]
                    ? lcs[leftIndex + 1, rightIndex + 1] + 1
                    : Math.Max(lcs[leftIndex + 1, rightIndex], lcs[leftIndex, rightIndex + 1]);
            }
        }

        return lcs;
    }

    private sealed record DiffOperation(DiffChangeKind Kind);
}
