using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace IRacingSmartPlug.Behaviors;

/// <summary>
/// Attached behavior that smoothly animates a panel's height open/closed based
/// on a bound bool. Use on the inline editor container:
///   beh:ExpandBehavior.IsOpen="{Binding IsEditing}"
/// The element should default to Visibility="Collapsed" and ClipToBounds="True".
///
/// All layout-touching work is deferred to a dispatcher callback so it never runs
/// during item generation or a layout pass (which can crash).
/// </summary>
public static class ExpandBehavior
{
    public static bool? GetIsOpen(DependencyObject o) => (bool?)o.GetValue(IsOpenProperty);
    public static void SetIsOpen(DependencyObject o, bool? v) => o.SetValue(IsOpenProperty, v);

    // Nullable default so the callback always fires on the first bind.
    public static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.RegisterAttached("IsOpen", typeof(bool?), typeof(ExpandBehavior),
            new PropertyMetadata(null, OnIsOpenChanged));

    private static readonly TimeSpan OpenDur = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan CloseDur = TimeSpan.FromMilliseconds(170);

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement fe) return;
        bool open = (bool?)e.NewValue == true;
        fe.Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() => Apply(fe, open)));
    }

    private static void Apply(FrameworkElement fe, bool open)
    {
        // Superseded by a newer toggle before we got here.
        if ((GetIsOpen(fe) == true) != open) return;

        if (!fe.IsLoaded)
        {
            fe.Visibility = open ? Visibility.Visible : Visibility.Collapsed;
            fe.Height = open ? double.NaN : 0;
            if (open)
            {
                void once(object? s, RoutedEventArgs a)
                {
                    fe.Loaded -= once;
                    if (GetIsOpen(fe) == true) AnimateOpen(fe);
                }
                fe.Loaded += once;
            }
            return;
        }

        if (open) AnimateOpen(fe);
        else AnimateClose(fe);
    }

    private static void AnimateOpen(FrameworkElement fe)
    {
        fe.BeginAnimation(FrameworkElement.HeightProperty, null);
        fe.Visibility = Visibility.Visible;
        fe.Height = double.NaN;
        fe.UpdateLayout();
        double to = fe.ActualHeight;
        if (to <= 0)
        {
            fe.Height = double.NaN;
            return;
        }
        fe.Height = 0;

        var anim = new DoubleAnimation(0, to, OpenDur)
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        anim.Completed += (_, _) =>
        {
            if (GetIsOpen(fe) == true)
            {
                fe.BeginAnimation(FrameworkElement.HeightProperty, null);
                fe.Height = double.NaN;
            }
        };
        fe.BeginAnimation(FrameworkElement.HeightProperty, anim);
    }

    private static void AnimateClose(FrameworkElement fe)
    {
        double from = fe.ActualHeight;
        fe.BeginAnimation(FrameworkElement.HeightProperty, null);
        if (from <= 0)
        {
            fe.Height = 0;
            fe.Visibility = Visibility.Collapsed;
            return;
        }
        fe.Height = from;

        var anim = new DoubleAnimation(from, 0, CloseDur)
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        anim.Completed += (_, _) =>
        {
            if (GetIsOpen(fe) != true)
            {
                fe.BeginAnimation(FrameworkElement.HeightProperty, null);
                fe.Height = 0;
                fe.Visibility = Visibility.Collapsed;
            }
        };
        fe.BeginAnimation(FrameworkElement.HeightProperty, anim);
    }
}
