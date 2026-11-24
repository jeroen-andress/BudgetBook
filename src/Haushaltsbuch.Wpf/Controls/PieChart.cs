using System.Windows;
using System.Windows.Media;
using Haushaltsbuch.Presentation.ViewModels;

namespace Haushaltsbuch.Wpf.Controls;

/// <summary>
/// Schlankes Kreisdiagramm ohne externe Bibliothek. Zeichnet für jede <see cref="PieSliceViewModel"/>
/// ein Tortenstück; die Winkel ergeben sich aus dem Betrag relativ zur Summe.
/// </summary>
public sealed class PieChart : FrameworkElement
{
    public static readonly DependencyProperty SlicesProperty = DependencyProperty.Register(
        nameof(Slices), typeof(IReadOnlyList<PieSliceViewModel>), typeof(PieChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public IReadOnlyList<PieSliceViewModel>? Slices
    {
        get => (IReadOnlyList<PieSliceViewModel>?)GetValue(SlicesProperty);
        set => SetValue(SlicesProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize) =>
        new(double.IsInfinity(availableSize.Width) ? 240 : availableSize.Width,
            double.IsInfinity(availableSize.Height) ? 240 : availableSize.Height);

    protected override void OnRender(DrawingContext dc)
    {
        var slices = Slices;
        if (slices is null || slices.Count == 0) return;

        var total = (double)slices.Sum(s => s.Amount);
        if (total <= 0) return;

        var diameter = Math.Min(ActualWidth, ActualHeight) - 4;
        if (diameter <= 0) return;

        var radius = diameter / 2;
        var center = new Point(ActualWidth / 2, ActualHeight / 2);
        var separator = new Pen(Brushes.White, 1.5);
        separator.Freeze();

        // Nur ein Stück: ein Vollkreis (ein Bogen von 360° ist als Pfad nicht darstellbar)
        if (slices.Count == 1)
        {
            dc.DrawEllipse(ToBrush(slices[0].ColorHex), separator, center, radius, radius);
            return;
        }

        var startAngle = -90.0; // 12-Uhr-Position
        foreach (var slice in slices)
        {
            var sweep = (double)slice.Amount / total * 360.0;
            var endAngle = startAngle + sweep;

            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(center, isFilled: true, isClosed: true);
                ctx.LineTo(PointOnCircle(center, radius, startAngle), true, false);
                ctx.ArcTo(PointOnCircle(center, radius, endAngle), new Size(radius, radius), 0,
                    isLargeArc: sweep > 180, SweepDirection.Clockwise, true, false);
            }
            geometry.Freeze();

            dc.DrawGeometry(ToBrush(slice.ColorHex), separator, geometry);
            startAngle = endAngle;
        }
    }

    private static Point PointOnCircle(Point center, double radius, double angleDegrees)
    {
        var rad = angleDegrees * Math.PI / 180.0;
        return new Point(center.X + radius * Math.Cos(rad), center.Y + radius * Math.Sin(rad));
    }

    private static readonly Dictionary<string, Brush> BrushCache = new();

    private static Brush ToBrush(string hex)
    {
        if (BrushCache.TryGetValue(hex, out var cached)) return cached;
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return BrushCache[hex] = brush;
    }
}
