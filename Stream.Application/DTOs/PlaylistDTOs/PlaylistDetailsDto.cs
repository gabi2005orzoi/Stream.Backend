using System;
using System.Collections.Generic;
using System.Text;

namespace Stream.Application.DTOs.PlaylistDTOs
{
    public class PlaylistDetailsDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsOwner { get; set; }
        public IReadOnlyList<PlaylistTrackDto> Tracks { get; set; } = [];
    }
}
