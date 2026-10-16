using System;
using System.Collections.Generic;
using System.Text;

namespace Stream.Application.DTOs
{
    public class ProviderTrackDto
    {
        public string Provider { get; set; } = null!;
        public string ExternalId { get; set; } = null!;
        public string? ExternalUrl { get; set; }
    }
}
    