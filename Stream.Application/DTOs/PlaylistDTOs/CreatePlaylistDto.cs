using System;
using System.Collections.Generic;
using System.Text;

namespace Stream.Application.DTOs.PlaylistDTOs
{
    public class CreatePlaylistDto
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
    }
}
