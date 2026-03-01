using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using SerialAssistant.ViewModels;
using SerialAssistant.Views;
using Xunit;
using System.Linq;
using Avalonia.VisualTree;
using Avalonia.Interactivity;

namespace SerialAssistant.Tests;

public class AvaloniaUiTests
{
    [AvaloniaFact]
    public void MainWindow_CanBeInstantiated_WithViewModel()
    {
        var vm = new MainWindowViewModel();
        var window = new MainWindow
        {
            DataContext = vm
        };

        window.Show();

        // Check if RxTextBox and TxTextBox are initialized
        var rxTextBox = window.GetVisualDescendants().OfType<TextBox>().FirstOrDefault(x => x.Name == "RxTextBox");
        var txTextBox = window.GetVisualDescendants().OfType<TextBox>().FirstOrDefault(x => x.Name == "TxTextBox");

        Assert.NotNull(rxTextBox);
        Assert.NotNull(txTextBox);

        // Interact with the ViewModel directly to see if UI reflects (bindings check is tricky here without Avalonia timers pump, 
        // but we can check if data context updates work).
        vm.ReceivedData = "Hello from Test";
        
        Assert.Equal("Hello from Test", rxTextBox.Text);
    }
}