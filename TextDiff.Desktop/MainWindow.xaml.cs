using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.AvalonEdit;
using Microsoft.Win32;
using TextDiff.Desktop.Rendering;
using TextDiff.Desktop.Services;

namespace TextDiff.Desktop;

public partial class MainWindow : Window
{
    private readonly DiffBackgroundRenderer _leftBackgroundRenderer = new();
    private readonly DiffBackgroundRenderer _rightBackgroundRenderer = new();
    private bool _isSynchronizingScroll;

    public MainWindow()
    {
        InitializeComponent();
        AutoCompareCheckBox.Checked += AutoCompareCheckBox_Changed;
        AutoCompareCheckBox.Unchecked += AutoCompareCheckBox_Changed;
        WordWrapCheckBox.Checked += WordWrapCheckBox_Changed;
        WordWrapCheckBox.Unchecked += WordWrapCheckBox_Changed;
        ConfigureEditor(LeftEditor);
        ConfigureEditor(RightEditor);
        CompareAndRefresh();
    }

    private void ConfigureEditor(TextEditor editor)
    {
        editor.AllowDrop = true;
        editor.Options.ConvertTabsToSpaces = false;
        editor.Options.EnableHyperlinks = false;
        editor.Options.HighlightCurrentLine = true;
        editor.Options.IndentationSize = 4;
        editor.TextArea.IndentationStrategy = null;

        editor.TextChanged += Editor_TextChanged;
        editor.TextArea.Caret.PositionChanged += Caret_PositionChanged;
        editor.PreviewDragOver += Editor_PreviewDragOver;
        editor.Drop += Editor_Drop;
    }

    private async void OpenLeftFile_Click(object sender, RoutedEventArgs e)
    {
        await OpenFileIntoEditorAsync(LeftEditor).ConfigureAwait(true);
    }

    private async void OpenRightFile_Click(object sender, RoutedEventArgs e)
    {
        await OpenFileIntoEditorAsync(RightEditor).ConfigureAwait(true);
    }

    private void PasteLeft_Click(object sender, RoutedEventArgs e)
    {
        PasteClipboardIntoEditor(LeftEditor);
    }

    private void PasteRight_Click(object sender, RoutedEventArgs e)
    {
        PasteClipboardIntoEditor(RightEditor);
    }

    private void SwapSides_Click(object sender, RoutedEventArgs e)
    {
        (LeftEditor.Text, RightEditor.Text) = (RightEditor.Text, LeftEditor.Text);
        CompareAndRefresh();
    }

    private void CompareNow_Click(object sender, RoutedEventArgs e)
    {
        CompareAndRefresh();
    }

    private void ClearAll_Click(object sender, RoutedEventArgs e)
    {
        LeftEditor.Clear();
        RightEditor.Clear();
        CompareAndRefresh();
    }

    private void AutoCompareCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (AutoCompareCheckBox.IsChecked == true)
        {
            CompareAndRefresh();
        }
    }

    private void WordWrapCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        var wrap = WordWrapCheckBox.IsChecked == true;
        LeftEditor.WordWrap = wrap;
        RightEditor.WordWrap = wrap;
    }

    private void Editor_TextChanged(object? sender, EventArgs e)
    {
        UpdateMeta();

        if (AutoCompareCheckBox.IsChecked == true)
        {
            CompareAndRefresh();
        }
    }

    private void Caret_PositionChanged(object? sender, EventArgs e)
    {
        if (CursorTextBlock is null)
        {
            return;
        }

        CursorTextBlock.Text =
            $"左侧 Ln {LeftEditor.TextArea.Caret.Line}, Col {LeftEditor.TextArea.Caret.Column} | 右侧 Ln {RightEditor.TextArea.Caret.Line}, Col {RightEditor.TextArea.Caret.Column}";
    }

    private void Editor_PreviewDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetDataPresent(DataFormats.UnicodeText)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private async void Editor_Drop(object sender, DragEventArgs e)
    {
        if (sender is not TextEditor editor)
        {
            return;
        }

        if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
        {
            editor.Text = await File.ReadAllTextAsync(files[0]).ConfigureAwait(true);
            return;
        }

        if (e.Data.GetData(DataFormats.UnicodeText) is string text)
        {
            editor.Text = text;
        }
    }

    private async Task OpenFileIntoEditorAsync(TextEditor editor)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Text files|*.txt;*.json;*.xml;*.yaml;*.yml;*.md;*.log;*.csv|All files|*.*",
            Title = "选择要载入的文本文件"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        editor.Text = await File.ReadAllTextAsync(dialog.FileName).ConfigureAwait(true);
    }

    private void PasteClipboardIntoEditor(TextEditor editor)
    {
        if (Clipboard.ContainsText())
        {
            editor.Text = Clipboard.GetText();
        }
    }

    private void CompareAndRefresh()
    {
        if (LeftEditor is null || RightEditor is null || SummaryTextBlock is null)
        {
            return;
        }

        UpdateMeta();

        var diffResult = DiffEngine.Compare(LeftEditor.Text, RightEditor.Text);
        _leftBackgroundRenderer.SetLineChanges(diffResult.LeftLineChanges);
        _rightBackgroundRenderer.SetLineChanges(diffResult.RightLineChanges);

        LeftEditor.TextArea.TextView.Redraw();
        RightEditor.TextArea.TextView.Redraw();

        SummaryTextBlock.Text =
            $"相同 {diffResult.SameCount} 行 | 修改 {diffResult.ModifiedCount} 行 | 左侧独有 {diffResult.LeftOnlyCount} 行 | 右侧独有 {diffResult.RightOnlyCount} 行";
    }

    private void UpdateMeta()
    {
        if (LeftMetaTextBlock is null || RightMetaTextBlock is null || LeftEditor is null || RightEditor is null)
        {
            return;
        }

        LeftMetaTextBlock.Text = $"{CountLines(LeftEditor.Text)} 行 | {LeftEditor.Text.Length} 字符";
        RightMetaTextBlock.Text = $"{CountLines(RightEditor.Text)} 行 | {RightEditor.Text.Length} 字符";
    }

    private static int CountLines(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        return text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n').Length;
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);

        LeftEditor.TextArea.TextView.BackgroundRenderers.Add(_leftBackgroundRenderer);
        RightEditor.TextArea.TextView.BackgroundRenderers.Add(_rightBackgroundRenderer);

        LeftEditor.TextArea.TextView.ScrollOffsetChanged += LeftTextView_ScrollOffsetChanged;
        RightEditor.TextArea.TextView.ScrollOffsetChanged += RightTextView_ScrollOffsetChanged;

        CompareAndRefresh();
    }

    private void LeftTextView_ScrollOffsetChanged(object? sender, EventArgs e)
    {
        if (_isSynchronizingScroll)
        {
            return;
        }

        _isSynchronizingScroll = true;
        RightEditor.ScrollToVerticalOffset(LeftEditor.VerticalOffset);
        RightEditor.ScrollToHorizontalOffset(LeftEditor.HorizontalOffset);
        _isSynchronizingScroll = false;
    }

    private void RightTextView_ScrollOffsetChanged(object? sender, EventArgs e)
    {
        if (_isSynchronizingScroll)
        {
            return;
        }

        _isSynchronizingScroll = true;
        LeftEditor.ScrollToVerticalOffset(RightEditor.VerticalOffset);
        LeftEditor.ScrollToHorizontalOffset(RightEditor.HorizontalOffset);
        _isSynchronizingScroll = false;
    }
}
