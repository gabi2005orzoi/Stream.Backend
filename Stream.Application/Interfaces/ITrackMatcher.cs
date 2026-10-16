using Stream.Application.DTOs;
using Stream.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stream.Application.Interfaces
{
    public interface ITrackMatcher
    {
        Task<Track?> FindExistingTrackAsync(
            TrackCandidate candidate,
            CancellationToken cancellationToken);
    }
}
