using System.Collections.Generic;

namespace Stream.Application.DTOs
{
    public class RecommendationResultDto
    {
        public string Title { get; set; } = "Recommended for You";
        public string Subtitle { get; set; } = "Based on your music taste";
        public bool IsPersonalized { get; set; }
        public List<TrackDto> Tracks { get; set; } = [];
    }
}
