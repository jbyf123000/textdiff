namespace TextDiff.Desktop.Models;

public enum DiffChangeKind
{
    Same,
    Modified,
    LeftOnly,
    RightOnly
}

public sealed record DiffResult(
    IReadOnlyDictionary<int, DiffChangeKind> LeftLineChanges,
    IReadOnlyDictionary<int, DiffChangeKind> RightLineChanges,
    int SameCount,
    int ModifiedCount,
    int LeftOnlyCount,
    int RightOnlyCount);
