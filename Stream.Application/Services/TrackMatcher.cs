using Stream.Application.DTOs;
using Stream.Application.Interfaces;
using Stream.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stream.Application.Services
{
    public class TrackMatcher : ITrackMatcher
    {
        private readonly ITrackRepository trackRepository;

        public TrackMatcher(ITrackRepository trackRepository)
        {
            this.trackRepository = trackRepository;
        }

        public async Task<Track?> FindExistingTrackAsync(TrackCandidate candidate, CancellationToken cancellationToken)
        {
            return await trackRepository.FindByTitleAndArtistAsync(
                candidate.Title,
                candidate.Artist,
                cancellationToken);
        }
    }
}
