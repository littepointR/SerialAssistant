using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Timers;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jint;
using SerialAssistant.Models;
using SerialAssistant.Views;
using Avalonia;
using Avalonia.Markup.Xaml.Styling;

namespace SerialAssistant.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private SerialPort? _serialPort;
    private readonly System.Timers.Timer _frameTimer;
    private readonly List<byte> _rxBuffer = new();
    private PlotWindow? _plotWindow;
    private PlotWindowViewModel? _plotViewModel;
    private HelpWindow? _helpWindow;
    private int _lineCounter = 1;

    [ObservableProperty]
    private ObservableCollection<string> _availablePorts = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    private string _selectedPort = "";

    [ObservableProperty]
    private ObservableCollection<int> _baudRates = new() { 4800, 9600, 19200, 38400, 57600, 115200 };

    [ObservableProperty]
    private int _selectedBaudRate = 115200;

    [ObservableProperty]
    private ObservableCollection<int> _dataBitsList = new() { 5, 6, 7, 8 };

    [ObservableProperty]
    private int _selectedDataBits = 8;

    [ObservableProperty]
    private ObservableCollection<StopBits> _stopBitsList = new(Enum.GetValues<StopBits>());

    [ObservableProperty]
    private StopBits _selectedStopBits = StopBits.One;

    [ObservableProperty]
    private ObservableCollection<Parity> _parityList = new(Enum.GetValues<Parity>());

    [ObservableProperty]
    private Parity _selectedParity = Parity.None;

    [ObservableProperty]
    private string _receivedData = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    private string _dataToSend = "";

    [ObservableProperty]
    private string _logData = "";

    [ObservableProperty]
    private bool _isHexReceive;

    [ObservableProperty]
    private bool _isHexSend;

    [ObservableProperty]
    private bool _showTimestamp;

    [ObservableProperty]
    private bool _autoFrameBreak = true;

    [ObservableProperty]
    private int _frameBreakTimeoutMs = 50;

    [ObservableProperty]
    private bool _pauseUpdate;

    [ObservableProperty]
    private bool _autoScroll = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    private bool _isConnected;

    [ObservableProperty]
    private int _txCount = 0;

    [ObservableProperty]
    private int _rxCount = 0;

    [ObservableProperty]
    private int _rxSelectionLength = 0;

    [ObservableProperty]
    private int _txSelectionLength = 0;

    [ObservableProperty]
    private int _rxSelectionCharCount = 0;

    [ObservableProperty]
    private int _rxSelectionHexCount = 0;

    [ObservableProperty]
    private int _txSelectionCharCount = 0;

    [ObservableProperty]
    private int _txSelectionHexCount = 0;

    [ObservableProperty]
    private ObservableCollection<string> _sendHistory = new();

    [ObservableProperty]
    private string _selectedSendHistory = "";

    [ObservableProperty]
    private ObservableCollection<QuickSendItem> _quickSends = new();

    [ObservableProperty]
    private ObservableCollection<JsScriptItem> _jsScripts = new();

    public ObservableCollection<string> Encodings { get; } = new() { "UTF-8", "ASCII", "GBK", "UTF-16" };
    [ObservableProperty] private string _selectedEncoding = "UTF-8";

    public ObservableCollection<int> BufferSizes { get; } = new() { 1024, 4096, 8192, 16384, 65536 };
    [ObservableProperty] private int _selectedBufferSize = 4096;

    public ObservableCollection<int> FontSizes { get; } = new() { 10, 11, 12, 13, 14, 16, 18, 20, 24 };
    [ObservableProperty] private int _selectedFontSize = 13;

    public ObservableCollection<string> LineEndings { get; } = new() { "None", "\\r\\n", "\\n", "\\r" };
    [ObservableProperty] private string _selectedLineEnding = "None";

    [ObservableProperty] private bool _showLineNumbers;

    public ObservableCollection<string> Themes { get; } = new() { "Auto", "Light", "Dark" };
    [ObservableProperty] private string _selectedTheme = "Auto";

    public ObservableCollection<string> Languages { get; } = new() { "简体中文", "English" };
    [ObservableProperty] private string _selectedLanguage = "简体中文";

    public ObservableCollection<string> ThemeColors { get; } = new() { "Teal", "Blue", "Purple", "Amber", "Red", "Green" };
    [ObservableProperty] private string _selectedThemeColor = "Teal";

    public string ConnectButtonText => IsConnected ? "Disconnect" : "Connect";

    public MainWindowViewModel()
    {
        _frameTimer = new Timer();
        _frameTimer.Elapsed += FrameTimer_Elapsed;

        RefreshPorts();

        for (int i = 1; i <= 3; i++)
        {
            QuickSends.Add(new QuickSendItem(OnQuickSend, OnRemoveQuickSend) { Name = $"Cmd {i}" });
        }

        JsScripts.Add(new JsScriptItem
        {
            Name = "Default Script",
            SendDataAction = (data) => Dispatcher.UIThread.Post(() => SendData(data, false)),
            LogAction = (msg) => Dispatcher.UIThread.Post(() => AppendLogData(msg)),
            PlotViewModel = _plotViewModel
        });
    }

    partial void OnSelectedThemeChanged(string value)
    {
        var app = Application.Current;
        if (app != null)
        {
            var targetTheme = value switch
            {
                "Light" => Avalonia.Styling.ThemeVariant.Light,
                "Dark" => Avalonia.Styling.ThemeVariant.Dark,
                _ => Avalonia.Styling.ThemeVariant.Default
            };
            app.RequestedThemeVariant = targetTheme;
        }
    }

    partial void OnSelectedLanguageChanged(string value)
    {
        var app = Application.Current;
        if (app != null)
        {
            string uri = value == "English" ? "avares://SerialAssistant/Assets/Lang/en-US.axaml" : "avares://SerialAssistant/Assets/Lang/zh-CN.axaml";
            app.Resources.MergedDictionaries[0] = new ResourceInclude(new Uri("avares://SerialAssistant/App.axaml"))
            {
                Source = new Uri(uri)
            };
        }
    }

    partial void OnSelectedThemeColorChanged(string value)
    {
        var app = Application.Current;
        if (app != null)
        {
            var materialTheme = app.Styles.OfType<Material.Styles.Themes.MaterialTheme>().FirstOrDefault();
            if (materialTheme != null)
            {
                if (Enum.TryParse<Material.Colors.PrimaryColor>(value, out var color))
                {
                    // Update PrimaryColor property dynamically
                    materialTheme.PrimaryColor = color;
                }
            }
        }
    }

    partial void OnSelectedBufferSizeChanged(int value)
    {
        if (_serialPort != null)
        {
            try
            {
                _serialPort.ReadBufferSize = value;
            }
            catch (Exception ex)
            {
                AppendLogData($"Failed to change buffer size: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private void AddQuickSend()
    {
        QuickSends.Add(new QuickSendItem(OnQuickSend, OnRemoveQuickSend) { Name = $"Cmd {QuickSends.Count + 1}" });
    }

    private void OnRemoveQuickSend(QuickSendItem item)
    {
        QuickSends.Remove(item);
    }

    [RelayCommand]
    private void AddScript()
    {
        JsScripts.Add(new JsScriptItem
        {
            Name = $"New Script {JsScripts.Count + 1}",
            SendDataAction = (data) => Dispatcher.UIThread.Post(() => SendData(data, false)),
            LogAction = (msg) => Dispatcher.UIThread.Post(() => AppendLogData(msg)),
            PlotViewModel = _plotViewModel
        });
    }

    [RelayCommand]
    private void RemoveScript(JsScriptItem item)
    {
        item.IsEnabled = false; // Stops timer if running
        JsScripts.Remove(item);
    }

    partial void OnSelectedSendHistoryChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            DataToSend = value;
        }
    }

    partial void OnFrameBreakTimeoutMsChanged(int value)
    {
        if (value > 0)
        {
            _frameTimer.Interval = value;
        }
    }

    private void AppendLogData(string data)
    {
        LogData += $"[{DateTime.Now:HH:mm:ss}] {data}{Environment.NewLine}";
    }

    [RelayCommand]
    private void RefreshPorts()
    {
        AvailablePorts.Clear();
        var ports = SerialPort.GetPortNames();
        foreach (var port in ports)
        {
            AvailablePorts.Add(port);
        }

        if (AvailablePorts.Any())
        {
            SelectedPort = AvailablePorts.First();
        }
        AppendLogData("Refreshed available ports.");
    }

    private bool CanConnect() => !string.IsNullOrEmpty(SelectedPort);

    [RelayCommand(CanExecute = nameof(CanConnect))]
    private void Connect()
    {
        if (IsConnected)
        {
            try
            {
                _frameTimer.Stop();
                _serialPort?.Close();
                _serialPort?.Dispose();
                _serialPort = null;
                IsConnected = false;
                OnPropertyChanged(nameof(ConnectButtonText));
                AppendLogData($"Disconnected from {SelectedPort}.");
            }
            catch (Exception ex)
            {
                AppendLogData($"Error closing port: {ex.Message}");
            }
        }
        else
        {
            try
            {
                _serialPort = new SerialPort(SelectedPort, SelectedBaudRate, SelectedParity, SelectedDataBits, SelectedStopBits);
                _serialPort.ReadBufferSize = SelectedBufferSize;
                _serialPort.DataReceived += SerialPort_DataReceived;
                _serialPort.Open();
                IsConnected = true;
                _frameTimer.Interval = FrameBreakTimeoutMs > 0 ? FrameBreakTimeoutMs : 50;
                OnPropertyChanged(nameof(ConnectButtonText));
                AppendLogData($"Connected to {SelectedPort} at {SelectedBaudRate} baud.");
            }
            catch (Exception ex)
            {
                AppendLogData($"Error opening port: {ex.Message}");
            }
        }
    }

    private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        if (_serialPort is { IsOpen: true })
        {
            try
            {
                int bytesToRead = _serialPort.BytesToRead;
                if (bytesToRead == 0) return;

                byte[] buffer = new byte[bytesToRead];
                int bytesRead = _serialPort.Read(buffer, 0, bytesToRead);

                if (bytesRead > 0)
                {
                    Dispatcher.UIThread.Post(() => RxCount += bytesRead);

                    if (AutoFrameBreak)
                    {
                        lock (_rxBuffer)
                        {
                            _rxBuffer.AddRange(buffer.Take(bytesRead));
                        }
                        _frameTimer.Stop();
                        _frameTimer.Start();
                    }
                    else
                    {
                        ProcessReceivedData(buffer.Take(bytesRead).ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Post(() => AppendLogData($"Error reading data: {ex.Message}"));
            }
        }
    }

    private void FrameTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        _frameTimer.Stop();
        byte[] data;
        lock (_rxBuffer)
        {
            if (_rxBuffer.Count == 0) return;
            data = _rxBuffer.ToArray();
            _rxBuffer.Clear();
        }

        ProcessReceivedData(data);
    }

    private Encoding GetCurrentEncoding()
    {
        try
        {
            return Encoding.GetEncoding(SelectedEncoding);
        }
        catch
        {
            return Encoding.UTF8;
        }
    }

    private void ProcessReceivedData(byte[] buffer)
    {
        if (PauseUpdate) return;

        Dispatcher.UIThread.Post(() => {
            string output = "";
            if (IsHexReceive)
            {
                output = BitConverter.ToString(buffer).Replace("-", " ") + " ";
            }
            else
            {
                output = GetCurrentEncoding().GetString(buffer);
            }

            // Optional JS processing
            foreach (var script in JsScripts)
            {
                output = script.ProcessData(output);
            }

            // Attempt to parse for plotting
            if (_plotViewModel != null && double.TryParse(output.Trim(), out double val))
            {
                _plotViewModel.AddPoint(val);
            }

            string prefix = "";
            if (ShowLineNumbers)
            {
                prefix += $"[{_lineCounter++:D4}] ";
            }
            if (ShowTimestamp)
            {
                prefix += $"[{DateTime.Now:HH:mm:ss.fff}] ";
            }

            output = prefix + output;

            if (AutoFrameBreak)
            {
                output += Environment.NewLine;
            }

            ReceivedData += output;
        });
    }

    private bool CanSend() => IsConnected && !string.IsNullOrEmpty(DataToSend);

    [RelayCommand(CanExecute = nameof(CanSend))]
    private void Send()
    {
        string toSend = DataToSend;
        if (SelectedLineEnding != "None" && !IsHexSend)
        {
            string ending = SelectedLineEnding switch
            {
                "\\r\\n" => "\r\n",
                "\\n" => "\n",
                "\\r" => "\r",
                _ => ""
            };
            toSend += ending;
        }

        SendData(toSend, IsHexSend);

        if (!SendHistory.Contains(DataToSend))
        {
            SendHistory.Insert(0, DataToSend);
            if (SendHistory.Count > 20)
            {
                SendHistory.RemoveAt(20);
            }
        }
    }

    private void OnQuickSend(QuickSendItem item)
    {
        if (string.IsNullOrEmpty(item.Data)) return;
        string toSend = item.Data;
        if (SelectedLineEnding != "None" && !item.IsHex)
        {
            string ending = SelectedLineEnding switch
            {
                "\\r\\n" => "\r\n",
                "\\n" => "\n",
                "\\r" => "\r",
                _ => ""
            };
            toSend += ending;
        }
        SendData(toSend, item.IsHex);
    }

    private void SendData(string data, bool isHex)
    {
        if (_serialPort is { IsOpen: true })
        {
            try
            {
                if (isHex)
                {
                    string hex = data.Replace(" ", "").Replace("\r", "").Replace("\n", "");
                    if (hex.Length % 2 != 0)
                    {
                        AppendLogData("Send Error: Invalid HEX format. Length must be even.");
                        return;
                    }

                    byte[] buffer = new byte[hex.Length / 2];
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        buffer[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
                    }

                    _serialPort.Write(buffer, 0, buffer.Length);
                    TxCount += buffer.Length;
                    AppendLogData($"Sent {buffer.Length} bytes (HEX).");
                }
                else
                {
                    byte[] buffer = GetCurrentEncoding().GetBytes(data);
                    _serialPort.Write(buffer, 0, buffer.Length);
                    TxCount += buffer.Length;
                    AppendLogData($"Sent {buffer.Length} bytes (Text).");
                }
            }
            catch (Exception ex)
            {
                AppendLogData($"Error sending data: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private void OpenPlotter()
    {
        if (_plotWindow == null || !_plotWindow.IsVisible)
        {
            _plotViewModel = new PlotWindowViewModel();

            // Pass plot view model to all scripts
            foreach (var script in JsScripts)
            {
                script.PlotViewModel = _plotViewModel;
            }

            _plotWindow = new PlotWindow
            {
                DataContext = _plotViewModel
            };
            _plotWindow.Closed += (s, e) => { _plotWindow = null; _plotViewModel = null; };
            _plotWindow.Show();
        }
        else
        {
            _plotWindow.Activate();
        }
    }

    [RelayCommand]
    private void OpenHelp()
    {
        if (_helpWindow == null || !_helpWindow.IsVisible)
        {
            _helpWindow = new HelpWindow();
            _helpWindow.Closed += (s, e) => { _helpWindow = null; };
            _helpWindow.Show();
        }
        else
        {
            _helpWindow.Activate();
        }
    }

    public void UpdateRxSelectionStats(string? selectedText, int selectionLength)
    {
        RxSelectionLength = selectionLength;
        if (string.IsNullOrEmpty(selectedText))
        {
            RxSelectionCharCount = 0;
            RxSelectionHexCount = 0;
        }
        else
        {
            RxSelectionCharCount = selectedText.Length;
            // Calculate hex count: each char in hex string represents 4 bits
            // When in HEX mode, count the hex characters and divide by 2 (2 chars = 1 byte)
            var hexChars = selectedText.Replace(" ", "").Replace("\r", "").Replace("\n", "").Replace("-", "");
            RxSelectionHexCount = hexChars.Length / 2;
        }
    }

    public void UpdateTxSelectionStats(string? selectedText, int selectionLength)
    {
        TxSelectionLength = selectionLength;
        if (string.IsNullOrEmpty(selectedText))
        {
            TxSelectionCharCount = 0;
            TxSelectionHexCount = 0;
        }
        else
        {
            TxSelectionCharCount = selectedText.Length;
            var hexChars = selectedText.Replace(" ", "").Replace("\r", "").Replace("\n", "").Replace("-", "");
            TxSelectionHexCount = hexChars.Length / 2;
        }
    }

    [RelayCommand]
    private void Clear()
    {
        ReceivedData = "";
        _lineCounter = 1;
    }

    [RelayCommand]
    private void ClearLog()
    {
        LogData = "";
    }

    [RelayCommand]
    private void ClearCounters()
    {
        TxCount = 0;
        RxCount = 0;
    }
}