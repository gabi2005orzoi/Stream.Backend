using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Stream.Application.Services
{
    public static class TrackTitleSanitizer
    {
        private static readonly Regex TagInBracketsRegex = new Regex(
            @"[\(\[\{](?i:(official\s*(music)?\s*video|official\s*audio|visuali[sz]er|lyrics?(\s*video)?|live([^\)\]\}]*)|remix([^\)\]\}]*)|club\s*mix|extended\s*mix|acoustic([^\)\]\}]*)|slowed([^\)\]\}]*)|cover([^\)\]\}]*)|instrumental|karaoke|tribute|audio|hd|hq|4k|remaster([^\)\]\}]*)|version|edit|explicit|clean|prod\.[^\)\]\}]*|feat\.[^\)\]\}]*|ft\.[^\)\]\}]*))[\)\]\}]",
            RegexOptions.Compiled);

        private static readonly Regex TrailingTagRegex = new Regex(
            @"(?i:\s*[\-–—\|/~:]\s*(official\s*(music)?\s*video|official\s*audio|visuali[sz]er|lyrics?(\s*video)?|live([^\-–—\|/~:]*)|remix([^\-–—\|/~:]*)|acoustic([^\-–—\|/~:]*)|slowed([^\-–—\|/~:]*)|cover([^\-–—\|/~:]*)|instrumental|karaoke|audio|remaster([^\-–—\|/~:]*)|version|edit|clean|explicit|4k|hd).*$)",
            RegexOptions.Compiled);

        private static readonly Regex FeaturingRegex = new Regex(
            @"\b(ft\.|feat\.|featuring)\s+[^(\[\-]*(?=$|[(\[\-])",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex CleanPunctuationRegex = new Regex(
            @"[^\w\s]",
            RegexOptions.Compiled);

        private static readonly Regex MultipleSpacesRegex = new Regex(
            @"\s+",
            RegexOptions.Compiled);

        private static readonly string[] VariationKeywords = new[]
        {
            "remix", "acoustic", "live", "slowed", "reverb", "cover", "karaoke",
            "instrumental", "tribute", "mashup", "parody", "sped up", "nightcore",
            "8d audio", "bass boosted", "piano version", "orchestral", "unplugged",
            "stripped", "radio edit", "club mix", "extended mix", "guitar cover",
            "piano cover", "metal cover", "rock cover"
        };

        private static readonly string[] CompilationKeywords = new[]
        {
            "full album", "album complete", "discography", "greatest hits compilation",
            "best of compilation", "1 hour", "2 hours", "3 hours", "10 hours", "hour loop",
            "megamix", "mashup mix", "dj mix", "continuous mix", "jukebox", "all songs playlist",
            "bass boosted", "8d audio", "nightcore", "tik tok remix", "slowed reverb"
        };

        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "a", "an", "the", "and", "or", "in", "on", "at", "to", "for", "of", "with", "by", "from"
        };

        public static string Normalize(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return string.Empty;

            var cleaned = TagInBracketsRegex.Replace(title, " ");
            cleaned = TrailingTagRegex.Replace(cleaned, " ");
            cleaned = FeaturingRegex.Replace(cleaned, " ");
            cleaned = CleanPunctuationRegex.Replace(cleaned, " ");
            cleaned = MultipleSpacesRegex.Replace(cleaned, " ").Trim();

            return cleaned.ToLowerInvariant();
        }

        public static HashSet<string> GetCoreWords(string normalizedTitle)
        {
            if (string.IsNullOrWhiteSpace(normalizedTitle))
                return [];

            return normalizedTitle
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 1 && !StopWords.Contains(w))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        public static bool IsCompilationOrNoise(string title, TimeSpan? duration)
        {
            if (string.IsNullOrWhiteSpace(title))
                return true;

            var lower = title.ToLowerInvariant();
            foreach (var kw in CompilationKeywords)
            {
                if (lower.Contains(kw))
                    return true;
            }

            if (!IsReasonableDuration(duration))
                return true;

            return false;
        }

        public static bool IsSameSongOrVariation(string candidateTitle, string originalTitle)
        {
            if (string.IsNullOrWhiteSpace(candidateTitle) || string.IsNullOrWhiteSpace(originalTitle))
                return false;

            var candNorm = Normalize(candidateTitle);
            var origNorm = Normalize(originalTitle);

            if (string.IsNullOrEmpty(candNorm) || string.IsNullOrEmpty(origNorm))
                return false;

            // Direct equality
            if (candNorm == origNorm)
                return true;

            var origWords = GetCoreWords(origNorm);
            var candWords = GetCoreWords(candNorm);

            if (origWords.Count > 0 && candWords.Count > 0)
            {
                // Exact set equality
                if (origWords.SetEquals(candWords))
                    return true;

                // If candidate contains all core words of original (e.g., orig: "Yellow", cand: "Yellow Live In Brazil")
                if (origWords.IsSubsetOf(candWords))
                {
                    var extraWords = candWords.Except(origWords).ToList();
                    var candLower = candidateTitle.ToLowerInvariant();
                    var origLower = originalTitle.ToLowerInvariant();

                    // If candidate contains variation terms while original didn't
                    var hasVariationKeyword = VariationKeywords.Any(kw => candLower.Contains(kw) && !origLower.Contains(kw));
                    if (hasVariationKeyword)
                        return true;

                    // If the only extra words are few (e.g. 1-2 words like "live", "remix", "audio")
                    if (extraWords.Count <= 2 && (candNorm.Contains(origNorm) || origNorm.Contains(candNorm)))
                        return true;
                }

                // If original is a subset of candidate and original has at least 2 core words
                if (origWords.Count >= 2 && origWords.IsSubsetOf(candWords))
                {
                    return true;
                }

                // Reverse: If candidate is subset of original (e.g. original was "In The End" and candidate is "In The End")
                if (candWords.Count >= 2 && candWords.IsSubsetOf(origWords))
                {
                    return true;
                }
            }

            // Substring check for meaningful length phrases
            if (origNorm.Length >= 4 && candNorm.Contains(origNorm))
            {
                var candLower = candidateTitle.ToLowerInvariant();
                var origLower = originalTitle.ToLowerInvariant();
                if (VariationKeywords.Any(kw => candLower.Contains(kw) && !origLower.Contains(kw)))
                    return true;
            }

            return false;
        }

        public static bool IsReasonableDuration(TimeSpan? duration)
        {
            if (!duration.HasValue || duration.Value <= TimeSpan.FromSeconds(30))
                return false;

            // Reject tracks shorter than 30 seconds or longer than 8.5 minutes (which are usually full albums or DJ sets)
            return duration.Value >= TimeSpan.FromSeconds(30) && duration.Value <= TimeSpan.FromMinutes(8.5);
        }
    }
}
