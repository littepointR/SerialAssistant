using Avalonia.Controls;
using Avalonia.Input;
using SerialAssistant.ViewModels;
using System.Linq;
using System.Text.RegularExpressions;

namespace SerialAssistant.Views;

public partial class MainWindow : Window
{
    private TextBox? _rxTextBox;
    private TextBox? _txTextBox;

    public MainWindow()
    {
        InitializeComponent();
        
        _rxTextBox = this.FindControl<TextBox>("RxTextBox");
        _txTextBox = this.FindControl<TextBox>("TxTextBox");

        if (_rxTextBox != null)
        {
            _rxTextBox.PointerReleased += TextBox_SelectionChanged;
            _rxTextBox.KeyUp += TextBox_SelectionChanged;
            _rxTextBox.PropertyChanged += RxTextBox_PropertyChanged;
        }

        if (_txTextBox != null)
        {
            _txTextBox.PointerReleased += TextBox_SelectionChanged;
            _txTextBox.KeyUp += TextBox_SelectionChanged;
        }
    }

    private void RxTextBox_PropertyChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name == nameof(TextBox.Text) && DataContext is MainWindowViewModel vm)
        {
            if (vm.AutoScroll && _rxTextBox != null)
            {
                _rxTextBox.CaretIndex = _rxTextBox.Text?.Length ?? 0;
            }
        }
    }

    private void TextBox_SelectionChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;

        if (sender == _rxTextBox && _rxTextBox != null)
        {
            vm.RxSelectionLength = CalculateActualLength(_rxTextBox.SelectedText, vm.IsHexReceive);
        }
        else if (sender == _txTextBox && _txTextBox != null)
        {
            vm.TxSelectionLength = CalculateActualLength(_txTextBox.SelectedText, vm.IsHexSend);
        }
    }

    private int CalculateActualLength(string? selectedText, bool isHex)
    {
        if (string.IsNullOrEmpty(selectedText)) return 0;

        if (isHex)
        {
            // Remove spaces, newlines, etc.
            var cleanHex = Regex.Replace(selectedText, @"\s+", "");
            return cleanHex.Length / 2;
        }

        return selectedText.Length;
    }
}