using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.ObjectModel;

namespace SerialAssistant.ViewModels;

public partial class PlotWindowViewModel : ViewModelBase
{
    private readonly ObservableCollection<double> _chartValues;

    public PlotWindowViewModel()
    {
        _chartValues = new ObservableCollection<double>();
        
        Series = new ISeries[]
        {
            new LineSeries<double>
            {
                Values = _chartValues,
                Fill = null,
                GeometrySize = 0,
                LineSmoothness = 0,
                Stroke = new SolidColorPaint(SKColors.Blue) { StrokeThickness = 2 }
            }
        };
    }

    [ObservableProperty]
    private ISeries[] _series;

    public void AddPoint(double value)
    {
        _chartValues.Add(value);
        if (_chartValues.Count > 1000)
        {
            _chartValues.RemoveAt(0);
        }
    }

    public void Clear()
    {
        _chartValues.Clear();
    }
}