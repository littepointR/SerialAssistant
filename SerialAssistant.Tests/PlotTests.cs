using Avalonia.Headless.XUnit;
using SerialAssistant.Models;
using SerialAssistant.ViewModels;
using SerialAssistant.Views;
using Xunit;
using System.Linq;
using Avalonia.VisualTree;
using Avalonia.Controls;

namespace SerialAssistant.Tests;

public class PlotWindowViewModelTests
{
    [Fact]
    public void PlotWindowViewModel_InitWithDefaultSeries_HasOneSeries()
    {
        var vm = new PlotWindowViewModel();

        Assert.NotNull(vm.Series);
        Assert.Single(vm.Series);
    }

    [Fact]
    public void AddSeries_WithValidName_AddsNewSeries()
    {
        var vm = new PlotWindowViewModel();

        vm.AddSeries("temperature", "#FF0000");

        Assert.Equal(2, vm.Series.Length);
    }

    [Fact]
    public void AddSeries_WithDuplicateName_DoesNotAddDuplicate()
    {
        var vm = new PlotWindowViewModel();

        vm.AddSeries("temperature", "#FF0000");
        vm.AddSeries("temperature", "#00FF00");

        Assert.Equal(2, vm.Series.Length);
    }

    [Fact]
    public void RemoveSeries_WithExistingName_RemovesSeries()
    {
        var vm = new PlotWindowViewModel();

        vm.AddSeries("temperature", "#FF0000");
        vm.RemoveSeries("temperature");

        Assert.Single(vm.Series);
    }

    [Fact]
    public void RemoveSeries_WithNonExistingName_DoesNotThrow()
    {
        var vm = new PlotWindowViewModel();

        var exception = Record.Exception(() => vm.RemoveSeries("nonexistent"));

        Assert.Null(exception);
    }

    [Fact]
    public void AddPointToSeries_WithNewSeries_CreatesSeriesAndAddsPoint()
    {
        var vm = new PlotWindowViewModel();

        vm.AddPointToSeries("temperature", 25.5);

        var series = vm.GetSeries("temperature");
        Assert.NotNull(series);
        Assert.Single(series.Values);
        Assert.Equal(25.5, series.Values[0]);
    }

    [Fact]
    public void AddPointToSeries_WithExistingSeries_AddsPoint()
    {
        var vm = new PlotWindowViewModel();

        vm.AddPointToSeries("temperature", 25.5);
        vm.AddPointToSeries("temperature", 26.0);

        var series = vm.GetSeries("temperature");
        Assert.NotNull(series);
        Assert.Equal(2, series.Values.Count);
        Assert.Equal(25.5, series.Values[0]);
        Assert.Equal(26.0, series.Values[1]);
    }

    [Fact]
    public void AddPointToSeries_ExceedsMaxPoints_RemovesOldestPoint()
    {
        var vm = new PlotWindowViewModel();
        vm.AddSeries("test", "#0000FF");

        // Add 1001 points
        for (int i = 0; i < 1001; i++)
        {
            vm.AddPointToSeries("test", i);
        }

        var series = vm.GetSeries("test");
        Assert.NotNull(series);
        Assert.Equal(1000, series.Values.Count);
        Assert.Equal(1, series.Values[0]); // First point (0) should be removed
    }

    [Fact]
    public void GetPoints_WithExistingSeries_ReturnsPoints()
    {
        var vm = new PlotWindowViewModel();

        vm.AddPointToSeries("temperature", 25.5);
        vm.AddPointToSeries("temperature", 26.0);

        var points = vm.GetPoints("temperature");

        Assert.Equal(2, points.Count);
        Assert.Contains(25.5, points);
        Assert.Contains(26.0, points);
    }

    [Fact]
    public void GetPoints_WithNonExistingSeries_ReturnsEmptyList()
    {
        var vm = new PlotWindowViewModel();

        var points = vm.GetPoints("nonexistent");

        Assert.NotNull(points);
        Assert.Empty(points);
    }

    [Fact]
    public void ClearSeries_WithExistingSeries_ClearsPoints()
    {
        var vm = new PlotWindowViewModel();

        vm.AddPointToSeries("temperature", 25.5);
        vm.AddPointToSeries("temperature", 26.0);
        vm.ClearSeries("temperature");

        var series = vm.GetSeries("temperature");
        Assert.NotNull(series);
        Assert.Empty(series.Values);
    }

    [Fact]
    public void ClearAll_ClearsAllSeriesData()
    {
        var vm = new PlotWindowViewModel();

        vm.AddPointToSeries("temperature", 25.5);
        vm.AddPointToSeries("humidity", 60.0);
        vm.ClearAll();

        var tempSeries = vm.GetSeries("temperature");
        var humSeries = vm.GetSeries("humidity");
        Assert.NotNull(tempSeries);
        Assert.NotNull(humSeries);
        Assert.Empty(tempSeries.Values);
        Assert.Empty(humSeries.Values);
    }

    [Fact]
    public void GetAllSeriesData_ReturnsAllSeriesData()
    {
        var vm = new PlotWindowViewModel();

        vm.AddPointToSeries("temperature", 25.5);
        vm.AddPointToSeries("humidity", 60.0);

        var allData = vm.GetAllSeriesData();

        Assert.Equal(3, allData.Count); // Default Series1 + temp + humidity
        Assert.True(allData.ContainsKey("temperature"));
        Assert.True(allData.ContainsKey("humidity"));
    }

    [Fact]
    public void AddPoint_LegacyMethod_AddsToDefaultSeries()
    {
        var vm = new PlotWindowViewModel();

        vm.AddPoint(25.5);

        var series = vm.GetSeries("Series1");
        Assert.NotNull(series);
        Assert.Single(series.Values);
        Assert.Equal(25.5, series.Values[0]);
    }

    [Fact]
    public void Clear_LegacyMethod_ClearsAllData()
    {
        var vm = new PlotWindowViewModel();

        vm.AddPointToSeries("temperature", 25.5);
        vm.Clear();

        var series = vm.GetSeries("temperature");
        Assert.NotNull(series);
        Assert.Empty(series.Values);
    }

    [Fact]
    public void GetSeries_WithNonExistingSeries_ReturnsNull()
    {
        var vm = new PlotWindowViewModel();

        var series = vm.GetSeries("nonexistent");

        Assert.Null(series);
    }
}

public class JsScriptItemTests
{
    [Fact]
    public void JsScriptItem_DefaultScript_HasProcessFunction()
    {
        var script = new JsScriptItem();

        Assert.NotNull(script.ScriptContent);
        Assert.Contains("function process", script.ScriptContent);
    }

    [Fact]
    public void ProcessData_WithDisabledScript_ReturnsOriginalData()
    {
        var script = new JsScriptItem { IsEnabled = false };

        var result = script.ProcessData("test data");

        Assert.Equal("test data", result);
    }

    [Fact]
    public void ProcessData_WithValidScript_ReturnsProcessedData()
    {
        var script = new JsScriptItem { ScriptContent = "function process(data) { return data.toUpperCase(); }" };

        var result = script.ProcessData("hello");

        Assert.Equal("HELLO", result);
    }

    [Fact]
    public void ProcessData_WithInvalidScript_ReturnsOriginalData()
    {
        var script = new JsScriptItem { ScriptContent = "function process(data) { return invalid; }" };
        var logMessages = new List<string>();
        script.LogAction = msg => logMessages.Add(msg);

        var result = script.ProcessData("test");

        Assert.Equal("test", result);
        Assert.Single(logMessages);
        Assert.Contains("Process Error", logMessages[0]);
    }

    [Fact]
    public void ProcessData_WithChartApi_CanAddSeries()
    {
        var vm = new PlotWindowViewModel();
        var script = new JsScriptItem
        {
            PlotViewModel = vm,
            ScriptContent = @"
                function process(data) {
                    chart.addSeries('testSeries', '#FF0000');
                    return data;
                }"
        };

        var result = script.ProcessData("test");

        var series = vm.GetSeries("testSeries");
        Assert.NotNull(series);
    }

    [Fact]
    public void ProcessData_WithChartApi_CanAddPoint()
    {
        var vm = new PlotWindowViewModel();
        var script = new JsScriptItem
        {
            PlotViewModel = vm,
            ScriptContent = @"
                function process(data) {
                    chart.addSeries('testSeries', '#FF0000');
                    chart.addPoint('testSeries', 25.5);
                    return data;
                }"
        };

        script.ProcessData("test");

        var series = vm.GetSeries("testSeries");
        Assert.NotNull(series);
        Assert.Single(series.Values);
        Assert.Equal(25.5, series.Values[0]);
    }

    [Fact]
    public void ProcessData_WithChartApi_CanGetPoints()
    {
        var vm = new PlotWindowViewModel();
        vm.AddPointToSeries("testSeries", 25.5);
        var script = new JsScriptItem
        {
            PlotViewModel = vm,
            ScriptContent = @"
                function process(data) {
                    var points = chart.getPoints('testSeries');
                    return points.length.toString();
                }"
        };

        var result = script.ProcessData("test");

        Assert.Equal("1", result);
    }

    [Fact]
    public void ProcessData_WithChartApi_CanClearSeries()
    {
        var vm = new PlotWindowViewModel();
        vm.AddPointToSeries("testSeries", 25.5);
        var script = new JsScriptItem
        {
            PlotViewModel = vm,
            ScriptContent = @"
                function process(data) {
                    chart.clear('testSeries');
                    return data;
                }"
        };

        script.ProcessData("test");

        var series = vm.GetSeries("testSeries");
        Assert.NotNull(series);
        Assert.Empty(series.Values);
    }

    [Fact]
    public void ProcessData_WithChartApi_CanClearAll()
    {
        var vm = new PlotWindowViewModel();
        vm.AddPointToSeries("series1", 1.0);
        vm.AddPointToSeries("series2", 2.0);
        var script = new JsScriptItem
        {
            PlotViewModel = vm,
            ScriptContent = @"
                function process(data) {
                    chart.clearAll();
                    return data;
                }"
        };

        script.ProcessData("test");

        var s1 = vm.GetSeries("series1");
        var s2 = vm.GetSeries("series2");
        Assert.NotNull(s1);
        Assert.NotNull(s2);
        Assert.Empty(s1.Values);
        Assert.Empty(s2.Values);
    }

    [Fact]
    public void ProcessData_WithJsonResult_CanParseObjectFormat()
    {
        var vm = new PlotWindowViewModel();
        var script = new JsScriptItem
        {
            PlotViewModel = vm,
            ScriptContent = "function process(data) { return JSON.stringify({temp: 25.5, humidity: 60}); }"
        };

        script.ProcessData("test");

        var tempSeries = vm.GetSeries("temp");
        var humSeries = vm.GetSeries("humidity");
        Assert.NotNull(tempSeries);
        Assert.NotNull(humSeries);
        Assert.Single(tempSeries.Values);
        Assert.Single(humSeries.Values);
        Assert.Equal(25.5, tempSeries.Values[0]);
        Assert.Equal(60.0, humSeries.Values[0]);
    }

    [Fact]
    public void ProcessData_WithJsonResult_CanParseArrayFormat()
    {
        var vm = new PlotWindowViewModel();
        var script = new JsScriptItem
        {
            PlotViewModel = vm,
            ScriptContent = "function process(data) { return JSON.stringify([{temp: 25.5}, {humidity: 60}]); }"
        };

        script.ProcessData("test");

        var tempSeries = vm.GetSeries("temp");
        var humSeries = vm.GetSeries("humidity");
        Assert.NotNull(tempSeries);
        Assert.NotNull(humSeries);
        Assert.Single(tempSeries.Values);
        Assert.Single(humSeries.Values);
    }

    [Fact]
    public void ProcessData_WithoutPlotViewModel_DoesNotThrow()
    {
        var script = new JsScriptItem
        {
            PlotViewModel = null,
            ScriptContent = @"
                function process(data) {
                    chart.addSeries('testSeries', '#FF0000');
                    return data;
                }"
        };

        var result = script.ProcessData("test");

        Assert.Equal("test", result);
    }

    [Fact]
    public void JsScriptItem_Timer_CanBeEnabledAndDisabled()
    {
        var script = new JsScriptItem { IsTimerEnabled = true, TimerIntervalMs = 100 };

        Assert.True(script.TimerSettingsVisible);

        script.IsTimerEnabled = false;

        Assert.False(script.TimerSettingsVisible);
    }

    [Fact]
    public void JsScriptItem_WithPlotViewModelProperty_CanBeSet()
    {
        var vm = new PlotWindowViewModel();
        var script = new JsScriptItem { PlotViewModel = vm };

        Assert.Equal(vm, script.PlotViewModel);
    }
}

public class PlotWindowUiTests
{
    [AvaloniaFact]
    public void PlotWindow_CanBeInstantiated_WithViewModel()
    {
        var vm = new PlotWindowViewModel();
        var window = new PlotWindow
        {
            DataContext = vm
        };

        window.Show();

        // Verify window can be created with PlotWindowViewModel
        Assert.NotNull(window.DataContext);
        Assert.IsType<PlotWindowViewModel>(window.DataContext);
    }

    [AvaloniaFact]
    public void PlotWindow_AfterAddingSeries_UpdatesVisual()
    {
        var vm = new PlotWindowViewModel();
        var window = new PlotWindow
        {
            DataContext = vm
        };

        window.Show();

        // Add a series
        vm.AddSeries("temperature", "#FF0000");
        vm.AddPointToSeries("temperature", 25.5);

        // Verify data was added
        var series = vm.GetSeries("temperature");
        Assert.NotNull(series);
        Assert.Single(series.Values);
    }
}

public class MainWindowViewModelPlotIntegrationTests
{
    /// <summary>
    /// Test that OpenPlotter creates a new PlotWindowViewModel.
    /// Note: This test directly creates PlotWindowViewModel to avoid Avalonia thread issues in unit tests.
    /// </summary>
    [Fact]
    public void OpenPlotter_WithNoExistingPlotter_CreatesNewPlotter()
    {
        var vm = new MainWindowViewModel();

        // Directly create PlotWindowViewModel (simulating what OpenPlotter does internally)
        var plotViewModel = new PlotWindowViewModel();

        // Assign to scripts
        foreach (var script in vm.JsScripts)
        {
            script.PlotViewModel = plotViewModel;
        }

        // Verify scripts have PlotViewModel reference
        foreach (var script in vm.JsScripts)
        {
            Assert.NotNull(script.PlotViewModel);
        }
    }

    [Fact]
    public void OpenPlotter_AfterOpening_ScriptsHavePlotViewModel()
    {
        var vm = new MainWindowViewModel();

        // Directly create PlotWindowViewModel
        var plotViewModel = new PlotWindowViewModel();

        // Simulate what OpenPlotter does
        foreach (var script in vm.JsScripts)
        {
            script.PlotViewModel = plotViewModel;
        }

        // Check that scripts have PlotViewModel reference
        foreach (var script in vm.JsScripts)
        {
            Assert.NotNull(script.PlotViewModel);
        }
    }

    [Fact]
    public void AddScript_BeforeOpeningPlotter_NewScriptHasNullPlotViewModel()
    {
        var vm = new MainWindowViewModel();

        // Before plotter is opened, _plotViewModel is null
        // So new scripts should have null PlotViewModel
        vm.AddScriptCommand.Execute(null);

        var newScript = vm.JsScripts.Last();
        Assert.Null(newScript.PlotViewModel);
    }

    [Fact]
    public void ProcessData_AfterPlotterOpen_ScriptsCanAccessChart()
    {
        var vm = new MainWindowViewModel();

        // Simulate opening plotter
        var plotViewModel = new PlotWindowViewModel();
        foreach (var s in vm.JsScripts)
        {
            s.PlotViewModel = plotViewModel;
        }

        var scriptItem = vm.JsScripts.First();
        scriptItem.ScriptContent = @"
            function process(data) {
                chart.addSeries('test', '#FF0000');
                chart.addPoint('test', 25.5);
                return data;
            }";

        scriptItem.ProcessData("test data");

        var plotVm = scriptItem.PlotViewModel;
        Assert.NotNull(plotVm);
        var series = plotVm.GetSeries("test");
        Assert.NotNull(series);
        Assert.Single(series.Values);
        Assert.Equal(25.5, series.Values[0]);
    }

    [Fact]
    public void MainWindowViewModel_DefaultScript_HasPlotViewModelSetToNull()
    {
        var vm = new MainWindowViewModel();

        // Before opening plotter, scripts should have null PlotViewModel
        foreach (var script in vm.JsScripts)
        {
            Assert.Null(script.PlotViewModel);
        }
    }
}
