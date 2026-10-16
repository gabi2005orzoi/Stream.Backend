using Microsoft.Extensions.DependencyInjection;
using Stream.Application.Interfaces;
using Stream.Application.Services;

namespace Stream.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(
            this IServiceCollection services)
        {
            services.AddScoped<IMusicProviderManager, MusicProviderManager>();

            services.AddScoped<ISearchService, SearchService>();

            services.AddScoped<ITrackMatcher, TrackMatcher>();

            services.AddScoped<ITrackResolver, TrackResolver>();

            services.AddScoped<IFavoriteService, FavoriteService>();

            services.AddScoped<IPlaylistService, PlaylistService>();

            services.AddScoped<IHistoryService, HistoryService>();
            
            services.AddScoped<IRecommendationService, RecommendationService>();

            services.AddScoped<IAuthService, AuthService>();

            return services;
        }
    }
}
