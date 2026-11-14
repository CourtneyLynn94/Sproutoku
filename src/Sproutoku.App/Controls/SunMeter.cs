using Microsoft.Maui.Controls.Shapes;
using Sproutoku.App.Resources.Styles;

namespace Sproutoku.App.Controls;

/// <summary>
/// The suns in the sky band: a bright sun for each one left, a pale dashed one for each spent.
/// A gentle arc: with more than one sun, the outer suns sit a little lower. Spending a sun fades it out, rightmost first.
/// </summary>
public sealed class SunMeter : HorizontalStackLayout {
    public static readonly BindableProperty StartingSunsProperty = BindableProperty.Create(
        nameof(StartingSuns), typeof(int), typeof(SunMeter), 0,
        propertyChanged: (view, _, _) => ((SunMeter)view).Build());

    public static readonly BindableProperty SunsLeftProperty = BindableProperty.Create(
        nameof(SunsLeft), typeof(int), typeof(SunMeter), 0,
        propertyChanged: (view, _, _) => ((SunMeter)view).Show(animate: true));

    // The bright sun in each slot, left to right.
    private readonly List<View> _brightSuns = [];

    public SunMeter() {
        Spacing = 8;
    }

    /// <summary>
    /// How many sun slots there are: the garden's starting suns.
    /// </summary>
    public int StartingSuns {
        get => (int)GetValue(StartingSunsProperty);
        set => SetValue(StartingSunsProperty, value);
    }

    /// <summary>
    /// How many suns are still bright.
    /// </summary>
    public int SunsLeft {
        get => (int)GetValue(SunsLeftProperty);
        set => SetValue(SunsLeftProperty, value);
    }

    // Each slot stacks a bright sun on top of a spent one (pale, dashed outline).
    private void Build() {
        Children.Clear();
        _brightSuns.Clear();

        int suns = StartingSuns;
        for (int i = 0; i < suns; i++) {
            var spent = new Ellipse {
                Fill = new SolidColorBrush(Palette.SpentSun),
                Stroke = new SolidColorBrush(Palette.Ink),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 3, 3 },
            };
            var bright = new Ellipse {
                Fill = new SolidColorBrush(Palette.Sun),
                Stroke = new SolidColorBrush(Palette.Ink),
                StrokeThickness = 2,
            };

            Children.Add(new Grid {
                WidthRequest = 44,
                HeightRequest = 44,
                TranslationY = suns > 1 && (i == 0 || i == suns - 1) ? 6 : 0,
                Children = { spent, bright },
            });
            _brightSuns.Add(bright);
        }

        Show(animate: false);
    }

    // As many bright suns as are left.
    private void Show(bool animate) {
        for (int i = 0; i < _brightSuns.Count; i++) {
            var bright = _brightSuns[i];
            double target = i < SunsLeft ? 1 : 0;
            if (bright.Opacity == target) continue;

            if (animate) {
                _ = bright.FadeToAsync(target, 300);
            } else {
                bright.Opacity = target;
            }
        }
    }
}
