using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Stream.Backend.Persistence;
using Microsoft.EntityFrameworkCore;
using Stream.Application.Interfaces;
using Stream.Infrastructure.Repositories;
using Stream.Infrastructure.Providers;
using YoutubeExplode;
using Stream.Infrastructure.Services;

namespace Stream.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext> (options =>
                options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection")));

            services.AddSingleton(sp =>
            {
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
                return new YoutubeClient(httpClient);
            });
            services.AddScoped<IMusicProvider, YoutubeProvider>();

            //services.AddHttpClient<JamendoProvider>();
            //services.AddScoped<IMusicProvider, JamendoProvider>();

            services.AddScoped<ITrackRepository, TrackRepository>();

            services.AddScoped<IProviderTrackRepository, ProviderTrackRepository>();
            
            services.AddScoped<IArtistRepository, ArtistRepository>();

            services.AddScoped<IFavoriteRepository, FavoriteRepository>();

            services.AddScoped<IUserRepository, UserRepository>();

            services.AddScoped<IPlaylistRepository, PlaylistRepository>();

            services.AddScoped<IHistoryRepository, HistoryRepository>();

            services.AddScoped<IPasswordHasher, PasswordHasher>();

            services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

            services.AddScoped<ICurrentUserService, CurrentUserService>();

            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

            return services;
        }
    }
}
