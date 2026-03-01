using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Timers;
using CommunityToolkit.Mvvm.Input;
using Jint;

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
    private string _scriptContent = "function process(data) {\n  return data;\n}\n\n// function onTimer() { return \"hello\"; }";

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
            // Optionally auto-disable on error
            // IsTimerEnabled = false; 
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
            }

            _engine.Execute(ScriptContent);
            var processFunc = _engine.GetValue("process");
            
            if (processFunc.IsObject())
            {
                var result = _engine.Invoke("process", data);
                return result.AsString();
            }
        }
        catch (Exception ex)
        {
            LogAction?.Invoke($"Script '{Name}' Process Error: {ex.Message}");
        }

        return data;
    }
}