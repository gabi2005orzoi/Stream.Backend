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
                var handler = new HttpClientHandler();

                // 1. Optional proxy support (e.g. residential/datacenter proxy for Render)
                var proxyUrl = configuration["YOUTUBE_PROXY"] ?? configuration["Youtube:Proxy"] ?? Environment.GetEnvironmentVariable("HTTP_PROXY");
                if (!string.IsNullOrWhiteSpace(proxyUrl))
                {
                    handler.Proxy = new System.Net.WebProxy(proxyUrl);
                    handler.UseProxy = true;
                }

                // 2. Optional cookie support to bypass YouTube bot detection on cloud servers
                var cookieString = configuration["YOUTUBE_COOKIES"] 
                    ?? configuration["Youtube:Cookies"] 
                    ?? configuration["Youtube__Cookies"]
                    ?? Environment.GetEnvironmentVariable("YOUTUBE_COOKIES")
                    ?? Environment.GetEnvironmentVariable("Youtube__Cookies");
                if (!string.IsNullOrWhiteSpace(cookieString))
                {
                    handler.UseCookies = true;
                    var cookieContainer = new System.Net.CookieContainer();
                    foreach (var rawCookie in cookieString.Split(';', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var parts = rawCookie.Trim().Split('=', 2);
                        if (parts.Length == 2)
                        {
                            try
                            {
                                cookieContainer.Add(new System.Net.Cookie(parts[0].Trim(), parts[1].Trim(), "/", ".youtube.com"));
                            }
                            catch
                            {
                                // Ignore malformed cookie tokens
                            }
                        }
                    }
                    handler.CookieContainer = cookieContainer;
                }

                var httpClient = new HttpClient(handler);
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
                return new YoutubeClient(httpClient);
            });
            // Invidious Provider (Alternative YouTube scraper/API via decentralized instances)
            services.AddHttpClient<InvidiousProvider>(client =>
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
                client.Timeout = TimeSpan.FromSeconds(10);
            });
            services.AddScoped<InvidiousProvider>();
            services.AddScoped<IMusicProvider>(sp => sp.GetRequiredService<InvidiousProvider>());

            // Cobalt Provider (Audio/Video extraction service)
            services.AddHttpClient<CobaltProvider>(client =>
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
                client.Timeout = TimeSpan.FromSeconds(10);
            });
            services.AddScoped<CobaltProvider>();
            services.AddScoped<IMusicProvider>(sp => sp.GetRequiredService<CobaltProvider>());

            services.AddScoped<IMusicProvider, YoutubeProvider>();

            // Audius Provider (Free, open music streaming API, ideal fallback for cloud hosting)
            services.AddHttpClient<AudiusProvider>(client =>
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
                client.Timeout = TimeSpan.FromSeconds(15);
            });
            services.AddScoped<IMusicProvider, AudiusProvider>();

            // Jamendo Provider (Works if ClientId is configured in appsettings/env)
            services.AddHttpClient<JamendoProvider>();
            services.AddScoped<IMusicProvider, JamendoProvider>();

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
