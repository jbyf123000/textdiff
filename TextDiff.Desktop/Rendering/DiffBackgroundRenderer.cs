using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using TextDiff.Desktop.Models;

namespace TextDiff.Desktop.Rendering;

public sealed class DiffBackgroundRenderer : IBackgroundRenderer
{
    private static readonly Brush ModifiedBrush = CreateFrozenBrush(Color.FromArgb(72, 255, 196, 89));
    private static readonly Brush LeftOnlyBrush = CreateFrozenBrush(Color.FromArgb(64, 244, 67, 54));
    private static readonly Brush RightOnlyBrush = CreateFrozenBrush(Color.FromArgb(72, 76, 175, 80));

    private IReadOnlyDictionary<int, DiffChangeKind> _lineChanges = new Dictionary<int, DiffChangeKind>();

    public KnownLayer Layer => KnownLayer.Selection;

    public void SetLineChanges(IReadOnlyDictionary<int, DiffChangeKind> lineChanges)
    {
        _lineChanges = lineChanges;
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (!textView.VisualLinesValid || _lineChanges.Count == 0)
        {
            return;
        }

        foreach (var visualLine in textView.VisualLines)
        {
            var documentLine = visualLine.FirstDocumentLine;
            if (!_lineChanges.TryGetValue(documentLine.LineNumber, out var changeKind))
            {
                continue;
            }

            var brush = GetBrush(changeKind);
            if (brush is null)
            {
                continue;
            }

            foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, documentLine, true))
            {
                drawingContext.DrawRectangle(
                    brush,
                    null,
                    new Rect(0, rect.Top, Math.Max(textView.ActualWidth, rect.Right), rect.Height));
            }
        }
    }

    private static Brush? GetBrush(DiffChangeKind changeKind)
    {
        return changeKind switch
        {
            DiffChangeKind.Modified => ModifiedBrush,
            DiffChangeKind.LeftOnly => LeftOnlyBrush,
            DiffChangeKind.RightOnly => RightOnlyBrush,
            _ => null
        };
    }

    private static Brush CreateFrozenBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
