using Microsoft.UI.Xaml;
using NamuWikiViewer.Windows.ViewModels;

namespace NamuWikiViewer.Windows;

public partial class App : Application
{
    private Window  _window;
    public static PreferenceViewModel GlobalPreferenceViewModel { get; } = new();

    public App() => InitializeComponent();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
