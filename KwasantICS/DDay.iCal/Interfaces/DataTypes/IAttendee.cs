﻿using System;
using System.Collections.Generic;

namespace KwasantICS.DDay.iCal.Interfaces.DataTypes
{
    public interface IAttendee :
        IEncodableDataType
    {
        Uri SentBy { get; set; }
        string CommonName { get; set; }
        Uri DirectoryEntry { get; set; }
        string Type { get; set; }
        IList<string> Members { get; }
        string Role { get; set; }
        string ParticipationStatus { get; set; }
        bool RSVP { get; set; }
        IList<string> DelegatedTo { get; }
        IList<string> DelegatedFrom { get; }
        Uri Value { get; set; }
    }
}
