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
    public partial bool UsePageHistory { get; set; }

    [ObservableProperty]
    public partial bool HideWebViewScrollBar { get; set; }

    [ObservableProperty]
    public partial bool BlockAds { get; set; }

    public ObservableCollection<PendingPage> PendingPages { get; }

    public ObservableCollection<PageHistory> PageHistories { get; }

    public ObservableCollection<PageHistory> ReversedPageHistories { get; }

    public PreferenceViewModel()
    {
        var preference = Configuration.GetValue<Preference>("Preference") ?? new();

        // Initialize properties
        UsePageHistory = preference.UsePageHistory;
        HideWebViewScrollBar = preference.HideWebViewScrollBar;
        BlockAds = preference.BlockAds;

        PendingPages = new(preference.PendingPages ?? []);
        PendingPages.CollectionChanged += OnPendingPagesCollectionChanged;

        PageHistories = new(preference.PageHistories ?? []);
        PageHistories.CollectionChanged += OnPageHistoriesCollectionChanged;

        ReversedPageHistories = new(PageHistories.Reverse());

        Preference = preference;
    }

    private void OnPageHistoriesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            foreach (PageHistory newItem in e.NewItems)
            {
                Preference.PageHistories ??= [];
                Preference.PageHistories.Add(newItem);
                ReversedPageHistories.Insert(0, newItem);
            }

            SavePreference();
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove)
        {
            foreach (PageHistory oldItem in e.OldItems)
            {
                ReversedPageHistories.Remove(oldItem);
                Preference.PageHistories?.Remove(oldItem);
            }

            SavePreference();
        }

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

        // Clear page history when UsePageHistory is disabled
        if (e.PropertyName == "UsePageHistory")
        {
            if (!UsePageHistory)
            {
                PageHistories.Clear();
                Preference.PageHistories?.Clear();
            }
        }

        SavePreference();
    }

    public void SavePreference()
    {
        if (Preference == null) return;

        // Update preference model
        Preference.UsePageHistory = UsePageHistory;
        Preference.HideWebViewScrollBar = HideWebViewScrollBar;
        Preference.BlockAds = BlockAds;

        Configuration.SetValue("Preference", Preference);

        WeakReferenceMessenger.Default.Send(new ValueChangedMessage<Preference>(Preference));
    }
}
