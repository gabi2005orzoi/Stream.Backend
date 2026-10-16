using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Stream.Api.Hubs;
using Stream.Application.DTOs.PlaylistDTOs;
using Stream.Application.Interfaces;

namespace Stream.Api.Controllers
{
    [Authorize]
    [Route("api/playlists")]
    [ApiController]
    public class PlaylistController : ControllerBase
    {
        private readonly IPlaylistService playlistService;
        private readonly IHubContext<PlaylistHub> hubContext;

        public PlaylistController(IPlaylistService playlistService, IHubContext<PlaylistHub> hubContext)
        {
            this.playlistService = playlistService;
            this.hubContext = hubContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var playlists = await playlistService.GetAllFromCurrentUserAsync(cancellationToken);
            return Ok(playlists);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(
            [FromRoute] Guid id,
            CancellationToken cancellationToken)
        {
            var playlist = await playlistService.GetByIdAsync(id, cancellationToken);
            if (playlist == null)
                return NotFound(new { message = "Playlist not found" });

            return Ok(playlist);
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreatePlaylistDto dto,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { message = "Name is required" });

            var result = await playlistService.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(
            [FromRoute] Guid id,
            CancellationToken cancellationToken)
        {
            var deleted = await playlistService.DeleteAsync(id, cancellationToken);
            if (!deleted)
                return NotFound(new { message = "Playlist not found" });

            return Ok(new { message = "Playlist deleted" });
        }

        [HttpPost("{id:guid}/tracks/{trackId:guid}")]
        public async Task<IActionResult> AddTrack(
            [FromRoute] Guid id,
            [FromRoute] Guid trackId,
            CancellationToken cancellationToken)
        {
            var added = await playlistService.AddTrackAsync(id, trackId, cancellationToken);

            if (!added)
                return BadRequest(new {message = "Track already in playlist or playlist doesn't exist"});

            await hubContext.Clients.Group($"playlist-{id}")
                .SendAsync("PlaylistUpdated", new { playlistId = id, action = "TrackAdded", trackId }, cancellationToken);

            return Ok(new { message = "Track added to playlist" });
        }

        [HttpDelete("{id:guid}/tracks/{trackId:guid}")]
        public async Task<IActionResult> RemoveTrack(
            [FromRoute] Guid id,
            [FromRoute] Guid trackId,
            CancellationToken cancellationToken)
        {
            var removed = await playlistService.RemoveTrackAsync(id, trackId, cancellationToken);

            if (!removed)
                return NotFound(new { message = "Track not found in playlist" });

            await hubContext.Clients.Group($"playlist-{id}")
                .SendAsync("PlaylistUpdated", new { playlistId = id, action = "TrackRemoved", trackId }, cancellationToken);

            return Ok(new { message = "Track removed from playlist" });
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(
            [FromRoute] Guid id,
            [FromBody] UpdatePlaylistkDto dto,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { message = "Name is required" });
            
            var updated = await playlistService.UpdateAsync(id, dto, cancellationToken);

            if (!updated)
                return NotFound(new { message = "Playlist not found or unauthorized" });

            await hubContext.Clients.Group($"playlist-{id}")
                .SendAsync("PlaylistUpdated", new { playlistId = id, action = "PlaylistRenamed" }, cancellationToken);

            return Ok(new { message = "Playlist updated successfully" });
        }
    }
}
