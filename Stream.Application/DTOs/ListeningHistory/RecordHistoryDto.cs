using System;
using System.Collections.Generic;
using System.Text;

namespace Stream.Application.DTOs.ListeningHistory
{
    public class RecordHistoryDto
    {
        public Guid TrackId { get; set; }
        public TimeSpan? Duration { get; set; }
        public bool Completed { get; set; }
    }
}
