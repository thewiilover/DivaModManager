using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DivaModManager.Avalonia.Dialogs;

public partial class TextInputDialog : Window
{
    public string? Result { get; private set; }

    public TextInputDialog()
    {
        InitializeComponent();
    }

    public TextInputDialog(string title, string prompt) : this()
    {
        Title = title;
        if (this.FindControl<TextBlock>("PromptTextBlock") is TextBlock promptBlock)
            promptBlock.Text = prompt;

        // Wire up button handlers
        if (this.FindControl<Button>("OKButton") is Button okBtn)
            okBtn.Click += OK_Click;
        if (this.FindControl<Button>("CancelButton") is Button cancelBtn)
            cancelBtn.Click += Cancel_Click;
    }

    private void OK_Click(object? sender, RoutedEventArgs e)
    {
        if (this.FindControl<TextBox>("InputTextBox") is TextBox input)
        {
            Result = input.Text;
        }
        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Result = null;
        Close(false);
    }
}

