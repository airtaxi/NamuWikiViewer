using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using NamuWikiViewer.Commons.Models;
using NamuWikiViewer.Windows;
using SocialServiceWorkerServiceRecordAutomation;
using System;
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

    [ObservableProperty]
    public partial bool HideNamuNewsCard { get; set; }

    [ObservableProperty]
    public partial bool HideRecentChangesCard { get; set; }

    [ObservableProperty]
    public partial bool HideRelatedSearchCard { get; set; }

    [ObservableProperty]
    public partial AppTheme Theme { get; set; }

    [ObservableProperty]
    public partial bool DisableWebViewCache { get; set; }

    [ObservableProperty]
    public partial double FontScale { get; set; }

    public int? BackStackDepthLimit
    {
        get => field;
        set
        {
            int? clamped = value;
            if (clamped.HasValue)
            {
                clamped = Math.Clamp(clamped.Value, Constants.MinBackStackDepth, Constants.MaxBackStackDepth);
            }

            if (SetProperty(ref field, clamped))
            {
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(UseBackStackDepthLimit)));
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(BackStackDepthLimitValue)));
            }
        }
    }

    public bool UseBackStackDepthLimit
    {
        get => BackStackDepthLimit.HasValue;
        set
        {
            if (value == UseBackStackDepthLimit) return;

            BackStackDepthLimit = value ? BackStackDepthLimit ?? Constants.DefaultBackStackDepth : null;
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(UseBackStackDepthLimit)));
        }
    }

    public double BackStackDepthLimitValue
    {
        get => BackStackDepthLimit ?? Constants.DefaultBackStackDepth;
        set => BackStackDepthLimit = (int)Math.Round(Math.Clamp(value, Constants.MinBackStackDepth, Constants.MaxBackStackDepth));
    }

    public ObservableCollection<PendingPage> PendingPages { get; }

    public ObservableCollection<PageHistory> PageHistories { get; }

    public ObservableCollection<PageHistory> ReversedPageHistories { get; }

    public ObservableCollection<AppTheme> Themes { get; } = new(Enum.GetValues<AppTheme>());

    public PreferenceViewModel()
    {
        var preference = Configuration.GetValue<Preference>("Preference") ?? new();

        // Initialize properties
        UsePageHistory = preference.UsePageHistory;
        HideWebViewScrollBar = preference.HideWebViewScrollBar;
        BlockAds = preference.BlockAds;
        HideNamuNewsCard = preference.HideNamuNewsCard;
        HideRecentChangesCard = preference.HideRecentChangesCard;
        HideRelatedSearchCard = preference.HideRelatedSearchCard;
        Theme = preference.Theme;
        DisableWebViewCache = preference.DisableWebViewCache;
        FontScale = preference.FontScale;
        BackStackDepthLimit = preference.BackStackDepthLimit ?? Constants.DefaultBackStackDepth;

        if (!preference.BackStackDepthLimit.HasValue)
        {
            UseBackStackDepthLimit = false;
        }

        PendingPages = new(preference.PendingPages ?? []);
        PendingPages.CollectionChanged += OnPendingPagesCollectionChanged;

        PageHistories = new(preference.PageHistories ?? []);
        PageHistories.CollectionChanged += OnPageHistoriesCollectionChanged;

        ReversedPageHistories = new(PageHistories.Reverse());

        Preference = preference;
    }

    public void SavePreference()
    {
        if (Preference == null) return;

        // Update preference model
        Preference.UsePageHistory = UsePageHistory;
        Preference.HideWebViewScrollBar = HideWebViewScrollBar;
        Preference.BlockAds = BlockAds;
        Preference.HideNamuNewsCard = HideNamuNewsCard;
        Preference.HideRecentChangesCard = HideRecentChangesCard;
        Preference.HideRelatedSearchCard = HideRelatedSearchCard;
        Preference.Theme = Theme;
        Preference.DisableWebViewCache = DisableWebViewCache;
        Preference.FontScale = FontScale;
        Preference.BackStackDepthLimit = BackStackDepthLimit;

        Configuration.SetValue("Preference", Preference);

        WeakReferenceMessenger.Default.Send(new ValueChangedMessage<Preference>(Preference));
    }

    private void ClearPageHistories()
    {
        PageHistories.Clear();
        Preference.PageHistories?.Clear();
        ReversedPageHistories.Clear();
        SavePreference();
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
                ClearPageHistories();
            }
        }

        SavePreference();
    }
}
