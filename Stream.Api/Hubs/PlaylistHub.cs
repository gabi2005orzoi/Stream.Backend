using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Stream.Api.Hubs
{
    [Authorize]
    public class PlaylistHub : Hub
    {
        public async Task JoinPlaylist(string playlistId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"playlist-{playlistId}");
        }

        public async Task LeavePlaylist(string playlistId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"playlist-{playlistId}");
        }
    }
}
