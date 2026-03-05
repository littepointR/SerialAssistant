using CommunityToolkit.Mvvm.ComponentModel;
using Jint;
using SerialAssistant.ViewModels;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Timers;

namespace SerialAssistant.Models;

public partial class JsScriptItem : ObservableObject
{
    private readonly System.Timers.Timer _timer;
    private Engine? _engine;

    public JsScriptItem()
    {
        _timer = new System.Timers.Timer();
        _timer.Elapsed += Timer_Elapsed;
    }

    [ObservableProperty]
    private string _name = "New Script";

    [ObservableProperty]
    private string _scriptContent = @"function process(data) {
  // Parse ""temp:25.5"" format
  var parts = data.split(':');
  if (parts.length === 2) {
    var seriesName = parts[0].trim();
    var value = parseFloat(parts[1].trim());
    if (!isNaN(value)) {
      chart.addSeries(seriesName, '#FF0000');
      chart.addPoint(seriesName, value);
    }
  }
  return data;
}

// function onTimer() { return 'hello'; }";

    [ObservableProperty]
    private bool _isEnabled = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TimerSettingsVisible))]
    private bool _isTimerEnabled = false;

    [ObservableProperty]
    private int _timerIntervalMs = 1000;

    public bool TimerSettingsVisible => IsTimerEnabled;

    public Action<string>? SendDataAction { get; set; }
    public Action<string>? LogAction { get; set; }

    public PlotWindowViewModel? PlotViewModel { get; set; }

    private PlotWindowViewModel? GetPlotViewModel()
    {
        return PlotViewModel;
    }

    private void RegisterChartApi(Engine engine)
    {
        // chart.addSeries(name, color)
        engine.SetValue("chart", new
        {
            addSeries = new Action<string, string>((name, color) =>
            {
                GetPlotViewModel()?.AddSeries(name, color);
            }),

            removeSeries = new Action<string>(name =>
            {
                GetPlotViewModel()?.RemoveSeries(name);
            }),

            addPoint = new Action<string, double>((name, value) =>
            {
                GetPlotViewModel()?.AddPointToSeries(name, value);
            }),

            getPoints = new Func<string, List<double>>(name =>
            {
                return GetPlotViewModel()?.GetPoints(name) ?? new List<double>();
            }),

            clear = new Action<string>(name =>
            {
                GetPlotViewModel()?.ClearSeries(name);
            }),

            clearAll = new Action(() =>
            {
                GetPlotViewModel()?.ClearAll();
            })
        });
    }

    partial void OnIsEnabledChanged(bool value)
    {
        UpdateTimerState();
    }

    partial void OnIsTimerEnabledChanged(bool value)
    {
        UpdateTimerState();
    }

    partial void OnTimerIntervalMsChanged(int value)
    {
        if (value > 0)
        {
            _timer.Interval = value;
        }
    }

    private void UpdateTimerState()
    {
        if (IsEnabled && IsTimerEnabled && TimerIntervalMs > 0)
        {
            _timer.Interval = TimerIntervalMs;
            _timer.Start();
        }
        else
        {
            _timer.Stop();
        }
    }

    private void Timer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        if (!IsEnabled || !IsTimerEnabled) return;

        try
        {
            if (_engine == null)
            {
                _engine = new Engine();
                RegisterChartApi(_engine);
            }

            _engine.Execute(ScriptContent);
            var onTimerFunc = _engine.GetValue("onTimer");

            if (onTimerFunc.IsObject())
            {
                var result = _engine.Invoke("onTimer");
                if (!result.IsUndefined() && !result.IsNull())
                {
                    SendDataAction?.Invoke(result.AsString());
                }
            }
        }
        catch (Exception ex)
        {
            LogAction?.Invoke($"Script '{Name}' Timer Error: {ex.Message}");
        }
    }

    public string ProcessData(string data)
    {
        if (!IsEnabled) return data;

        try
        {
            if (_engine == null)
            {
                _engine = new Engine();
                RegisterChartApi(_engine);
            }

            _engine.Execute(ScriptContent);
            var processFunc = _engine.GetValue("process");

            if (processFunc.IsObject())
            {
                var result = _engine.Invoke("process", data);

                // Check if result contains data to plot
                if (!result.IsUndefined() && !result.IsNull())
                {
                    var resultStr = result.ToString();

                    // Try to parse as JSON for batch plotting
                    if (resultStr.TrimStart().StartsWith("{") || resultStr.TrimStart().StartsWith("["))
                    {
                        try
                        {
                            using var doc = JsonDocument.Parse(resultStr);
                            var plotVm = GetPlotViewModel();

                            if (doc.RootElement.ValueKind == JsonValueKind.Object)
                            {
                                // Check if it's an array format like {temp: 25.5, humidity: 60}
                                // or object format like {temp: [1,2,3], humidity: [4,5,6]}
                                foreach (var prop in doc.RootElement.EnumerateObject())
                                {
                                    if (prop.Value.ValueKind == JsonValueKind.Number)
                                    {
                                        // Simple format: {seriesName: value}
                                        plotVm?.AddPointToSeries(prop.Name, prop.Value.GetDouble());
                                    }
                                    else if (prop.Value.ValueKind == JsonValueKind.Array)
                                    {
                                        // Array format: {seriesName: [1,2,3]}
                                        foreach (var item in prop.Value.EnumerateArray())
                                        {
                                            if (item.ValueKind == JsonValueKind.Number)
                                            {
                                                plotVm?.AddPointToSeries(prop.Name, item.GetDouble());
                                            }
                                        }
                                    }
                                }
                            }
                            else if (doc.RootElement.ValueKind == JsonValueKind.Array)
                            {
                                // Array format: [{temp: 25.5}, {humidity: 60}]
                                foreach (var item in doc.RootElement.EnumerateArray())
                                {
                                    if (item.ValueKind == JsonValueKind.Object)
                                    {
                                        foreach (var prop in item.EnumerateObject())
                                        {
                                            if (prop.Value.ValueKind == JsonValueKind.Number)
                                            {
                                                plotVm?.AddPointToSeries(prop.Name, prop.Value.GetDouble());
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (JsonException)
                        {
                            // Not valid JSON, return as string
                        }
                    }

                    return resultStr;
                }
            }
        }
        catch (Exception ex)
        {
            LogAction?.Invoke($"Script '{Name}' Process Error: {ex.Message}");
        }

        return data;
    }
}
