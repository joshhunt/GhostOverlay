using System;
using System.Collections.Generic;
using System.ComponentModel;
using GhostSharper.Models;

namespace GhostOverlay.Models
{
    interface ITrackable : INotifyPropertyChanged
    {
        bool IsCompleted { get; }
        string GroupByKey { get; }
        DestinyDisplayPropertiesDefinition DisplayProperties { get; }
        List<Objective> Objectives { get; set; }
        string Title { get; }
        Uri ImageUri { get; }
        TrackedEntry TrackedEntry { get; set; }
        string SortValue { get; }
        string Subtitle { get; }
        bool ShowDescription { get; }
        void NotifyPropertyChanged(string fieldName);
    }
}
