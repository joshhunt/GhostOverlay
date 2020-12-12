﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using GhostSharper.Models;

namespace GhostOverlay.Models
{
    public interface ITrackable : INotifyPropertyChanged
    {

        bool IsCompleted { get; }
        DestinyDisplayPropertiesDefinition DisplayProperties { get; }
        TrackableOwner Owner { get; set; }

        List<Objective> Objectives { get; set; }
        string Title { get; }
        Uri ImageUri { get; }
        TrackedEntry TrackedEntry { get; set; }
        string SortValue { get; }
        string Subtitle { get; }
        bool ShowDescription { get; }

        void UpdateTo(ITrackable item);
    }
}
