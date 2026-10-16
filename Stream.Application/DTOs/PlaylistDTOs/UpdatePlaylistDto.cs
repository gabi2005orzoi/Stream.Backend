using System;

namespace Stream.Application.DTOs.PlaylistDTOs
{
    public class UpdatePlaylistkDto
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
    }
}