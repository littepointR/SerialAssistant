using Avalonia.Headless.XUnit;
using SerialAssistant.ViewModels;
using Xunit;
using System.Linq;
using SerialAssistant.Models;

namespace SerialAssistant.Tests;

public class MainWindowViewModelTests
{
    [Fact]
    public void ViewModel_Initialization_SetsDefaults()
    {
        var vm = new MainWindowViewModel();

        Assert.Equal(115200, vm.SelectedBaudRate);
        Assert.Equal(8, vm.SelectedDataBits);
        Assert.Equal(System.IO.Ports.StopBits.One, vm.SelectedStopBits);
        Assert.Equal(System.IO.Ports.Parity.None, vm.SelectedParity);
        Assert.Equal(4096, vm.SelectedBufferSize);
        Assert.Equal("None", vm.SelectedLineEnding);
        
        Assert.Equal(3, vm.QuickSends.Count);
        Assert.Single(vm.JsScripts);
        Assert.Equal("Default Script", vm.JsScripts[0].Name);
    }

    [Fact]
    public void ViewModel_AddRemoveQuickSend()
    {
        var vm = new MainWindowViewModel();
        int initialCount = vm.QuickSends.Count;

        vm.AddQuickSendCommand.Execute(null);
        Assert.Equal(initialCount + 1, vm.QuickSends.Count);

        var addedItem = vm.QuickSends.Last();
        addedItem.RemoveCommand.Execute(null);

        Assert.Equal(initialCount, vm.QuickSends.Count);
    }

    [Fact]
    public void ViewModel_AddRemoveScript()
    {
        var vm = new MainWindowViewModel();
        int initialCount = vm.JsScripts.Count;

        vm.AddScriptCommand.Execute(null);
        Assert.Equal(initialCount + 1, vm.JsScripts.Count);

        var addedScript = vm.JsScripts.Last();
        vm.RemoveScriptCommand.Execute(addedScript);

        Assert.Equal(initialCount, vm.JsScripts.Count);
    }

    [Fact]
    public void ViewModel_ClearCommands_ResetsProperties()
    {
        var vm = new MainWindowViewModel();
        
        vm.ReceivedData = "Some Data";
        vm.ClearCommand.Execute(null);
        Assert.Equal("", vm.ReceivedData);

        // Test clearing counters
        vm.TxCount = 100;
        vm.RxCount = 200;
        vm.ClearCountersCommand.Execute(null);
        Assert.Equal(0, vm.TxCount);
        Assert.Equal(0, vm.RxCount);
    }

    [Fact]
    public void JsScript_ProcessData_ReturnsExpectedResult()
    {
        var scriptItem = new JsScriptItem
        {
            Name = "Test Script",
            ScriptContent = "function process(data) { return 'Prefix ' + data; }",
            IsEnabled = true
        };

        var result = scriptItem.ProcessData("Test");
        Assert.Equal("Prefix Test", result);
    }
}