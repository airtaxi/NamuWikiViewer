using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using NamuWikiViewer.Commons.Models;
using SocialServiceWorkerServiceRecordAutomation;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace NamuWikiViewer.Windows.ViewModels;

public partial class PreferenceViewModel : ObservableObject
{
    public Preference Preference { get; }

    [ObservableProperty]
    public partial bool HideWebViewScrollBar { get; set; }

    [ObservableProperty]
    public partial bool BlockAds { get; set; }

    public ObservableCollection<PendingPage> PendingPages { get; }

    public PreferenceViewModel()
    {
        Preference = Configuration.GetValue<Preference>("Preference") ?? new();

        // Initialize properties
        HideWebViewScrollBar = Preference.HideWebViewScrollBar;

        PendingPages = new(Preference.PendingPages ?? []);
        PendingPages.CollectionChanged += OnPendingPagesCollectionChanged;
    }

    private void OnPendingPagesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            foreach (PendingPage newItem in e.NewItems)
            {
                Preference.PendingPages ??= [];
                Preference.PendingPages.Add(newItem);
            }
            SavePreference();
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove)
        {
            foreach (PendingPage oldItem in e.OldItems)
            {
                Preference.PendingPages?.Remove(oldItem);
            }
            SavePreference();
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        SavePreference();
    }

    public void SavePreference()
    {
        if (Preference == null) return;

        // Update preference model
        Preference.HideWebViewScrollBar = HideWebViewScrollBar;
        Preference.BlockAds = BlockAds;

        Configuration.SetValue("Preference", Preference);

        WeakReferenceMessenger.Default.Send(new ValueChangedMessage<Preference>(Preference));
    }
}
