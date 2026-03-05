using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;

namespace SerialAssistant.ViewModels;

public class SeriesData
{
    public string Name { get; set; } = "";
    public string Color { get; set; } = "#0000FF";
    public ObservableCollection<double> Values { get; } = new();
}

public partial class PlotWindowViewModel : ViewModelBase
{
    private readonly Dictionary<string, SeriesData> _seriesData = new();
    private const int MaxPointsPerSeries = 1000;

    public PlotWindowViewModel()
    {
        Series = Array.Empty<ISeries>();
        // Add default series
        AddSeries("Series1", "#0000FF");
    }

    [ObservableProperty]
    private ISeries[] _series;

    public void AddSeries(string name, string color)
    {
        if (_seriesData.ContainsKey(name)) return;

        var data = new SeriesData { Name = name, Color = color };
        _seriesData[name] = data;

        var lineSeries = new LineSeries<double>
        {
            Values = data.Values,
            Fill = null,
            GeometrySize = 0,
            LineSmoothness = 0,
            Stroke = new SolidColorPaint(GetSKColor(color)) { StrokeThickness = 2 },
            Name = name
        };

        Series = _seriesData.Values
            .Select(s => new LineSeries<double>
            {
                Values = s.Values,
                Fill = null,
                GeometrySize = 0,
                LineSmoothness = 0,
                Stroke = new SolidColorPaint(GetSKColor(s.Color)) { StrokeThickness = 2 },
                Name = s.Name
            } as ISeries)
            .ToArray();
    }

    public void RemoveSeries(string name)
    {
        if (!_seriesData.ContainsKey(name)) return;

        _seriesData.Remove(name);

        Series = _seriesData.Values
            .Select(s => new LineSeries<double>
            {
                Values = s.Values,
                Fill = null,
                GeometrySize = 0,
                LineSmoothness = 0,
                Stroke = new SolidColorPaint(GetSKColor(s.Color)) { StrokeThickness = 2 },
                Name = s.Name
            } as ISeries)
            .ToArray();
    }

    public SeriesData? GetSeries(string name)
    {
        return _seriesData.GetValueOrDefault(name);
    }

    public void AddPointToSeries(string name, double value)
    {
        if (!_seriesData.TryGetValue(name, out var data))
        {
            AddSeries(name, "#0000FF");
            data = _seriesData[name];
        }

        data.Values.Add(value);
        if (data.Values.Count > MaxPointsPerSeries)
        {
            data.Values.RemoveAt(0);
        }
    }

    public void ClearSeries(string name)
    {
        if (_seriesData.TryGetValue(name, out var data))
        {
            data.Values.Clear();
        }
    }

    public void ClearAll()
    {
        foreach (var data in _seriesData.Values)
        {
            data.Values.Clear();
        }
    }

    public List<double> GetPoints(string name)
    {
        if (_seriesData.TryGetValue(name, out var data))
        {
            return data.Values.ToList();
        }
        return new List<double>();
    }

    public Dictionary<string, List<double>> GetAllSeriesData()
    {
        return _seriesData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Values.ToList());
    }

    public void AddPointsFromObject(JsonElement obj)
    {
        // Support format: {"series1": 25.5, "series2": 30.2}
        foreach (var prop in obj.EnumerateObject())
        {
            if (prop.Value.ValueKind == JsonValueKind.Number)
            {
                AddPointToSeries(prop.Name, prop.Value.GetDouble());
            }
        }
    }

    public void AddPointsFromArray(JsonElement array, string? defaultSeriesName = null)
    {
        // Support format: [{"temp": 25.5}, {"humidity": 60}]
        if (array.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in array.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Object)
                {
                    AddPointsFromObject(item);
                }
            }
        }
    }

    private static SKColor GetSKColor(string hexColor)
    {
        try
        {
            if (hexColor.StartsWith("#") && (hexColor.Length == 7 || hexColor.Length == 9))
            {
                var alpha = hexColor.Length == 9
                    ? Convert.ToByte(hexColor.Substring(1, 2), 16)
                    : (byte)255;
                var r = Convert.ToByte(hexColor.Substring(hexColor.Length - 6, 2), 16);
                var g = Convert.ToByte(hexColor.Substring(hexColor.Length - 4, 2), 16);
                var b = Convert.ToByte(hexColor.Substring(hexColor.Length - 2, 2), 16);
                return new SKColor(r, g, b, alpha);
            }
        }
        catch { }
        return SKColors.Blue;
    }

    // Legacy method for backward compatibility
    public void AddPoint(double value)
    {
        AddPointToSeries("Series1", value);
    }

    public void Clear()
    {
        ClearAll();
    }
}