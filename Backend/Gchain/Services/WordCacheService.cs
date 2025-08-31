using Gchain.Interfaces;
using Gchain.Models;
using Microsoft.Extensions.Options;

namespace Gchain.Services;

/// <summary>
/// Service for word dictionary and similarity caching using Redis
/// </summary>
public class WordCacheService : IWordCacheService
{
    private readonly IRedisService _redis;
    private readonly RedisSettings _settings;
    private readonly ILogger<WordCacheService> _logger;

    public WordCacheService(
        IRedisService redis,
        IOptions<RedisSettings> settings,
        ILogger<WordCacheService> logger
    )
    {
        _redis = redis;
        _settings = settings.Value;
        _logger = logger;
    }

    private string GetDictionaryKey(GameLanguage language) =>
        $"dict:{language.ToString().ToLowerInvariant()}";

    private string GetSimilarityKey(string word1, string word2, GameLanguage language)
    {
        // Ensure consistent ordering for similarity cache
        var words = new[] { word1.ToLowerInvariant(), word2.ToLowerInvariant() }
            .OrderBy(w => w)
            .ToArray();
        return $"similarity:{language.ToString().ToLowerInvariant()}:{words[0]}:{words[1]}";
    }

    private string GetWordFrequencyKey(GameLanguage language) =>
        $"freq:{language.ToString().ToLowerInvariant()}";

    private string GetDictionaryStatsKey(GameLanguage language) =>
        $"stats:dict:{language.ToString().ToLowerInvariant()}";

    public async Task<bool> IsWordInDictionaryAsync(string word, GameLanguage language)
    {
        try
        {
            var key = GetDictionaryKey(language);
            var normalizedWord = NormalizeWord(word);
            return await _redis.SetContainsAsync(key, normalizedWord);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to check if word {Word} exists in {Language} dictionary",
                word,
                language
            );
            return false;
        }
    }

    public async Task<bool> AddWordToDictionaryAsync(string word, GameLanguage language)
    {
        try
        {
            var key = GetDictionaryKey(language);
            var normalizedWord = NormalizeWord(word);
            var added = await _redis.SetAddAsync(key, normalizedWord);

            if (added)
            {
                await UpdateDictionaryStatsAsync(language);
                _logger.LogDebug("Added word {Word} to {Language} dictionary", word, language);
            }

            return added;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to add word {Word} to {Language} dictionary",
                word,
                language
            );
            return false;
        }
    }

    public async Task<int> AddWordsToDictionaryAsync(
        IEnumerable<string> words,
        GameLanguage language
    )
    {
        try
        {
            var key = GetDictionaryKey(language);
            var normalizedWords = words.Select(NormalizeWord).ToList();
            var addedCount = 0;

            foreach (var word in normalizedWords)
            {
                var added = await _redis.SetAddAsync(key, word);
                if (added)
                    addedCount++;
            }

            if (addedCount > 0)
            {
                await UpdateDictionaryStatsAsync(language);
                _logger.LogInformation(
                    "Added {Count} words to {Language} dictionary",
                    addedCount,
                    language
                );
            }

            return addedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add words to {Language} dictionary", language);
            return 0;
        }
    }

    public async Task<double?> GetCachedSimilarityAsync(
        string word1,
        string word2,
        GameLanguage language
    )
    {
        try
        {
            var key = GetSimilarityKey(word1, word2, language);
            var scoreStr = await _redis.GetAsync(key);

            if (string.IsNullOrEmpty(scoreStr))
                return null;

            if (double.TryParse(scoreStr, out var score))
                return score;

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to get cached similarity for {Word1} and {Word2} in {Language}",
                word1,
                word2,
                language
            );
            return null;
        }
    }

    public async Task<bool> CacheSimilarityAsync(
        string word1,
        string word2,
        double similarityScore,
        GameLanguage language
    )
    {
        try
        {
            var key = GetSimilarityKey(word1, word2, language);
            var expiry = TimeSpan.FromSeconds(_settings.SimilarityCacheTtlSeconds);

            var success = await _redis.SetAsync(key, similarityScore.ToString("F4"), expiry);

            if (success)
            {
                _logger.LogDebug(
                    "Cached similarity score {Score} for {Word1} and {Word2} in {Language}",
                    similarityScore,
                    word1,
                    word2,
                    language
                );
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to cache similarity for {Word1} and {Word2} in {Language}",
                word1,
                word2,
                language
            );
            return false;
        }
    }

    public async Task<List<string>> GetWordSuggestionsAsync(
        string partialWord,
        GameLanguage language,
        int maxSuggestions = 10
    )
    {
        try
        {
            var key = GetDictionaryKey(language);
            var allWords = await _redis.SetGetAllAsync(key);
            var normalizedPartial = NormalizeWord(partialWord).ToLowerInvariant();

            var suggestions = allWords
                .Where(word => word.ToLowerInvariant().StartsWith(normalizedPartial))
                .OrderBy(word => word.Length) // Prefer shorter words first
                .Take(maxSuggestions)
                .ToList();

            return suggestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to get word suggestions for {PartialWord} in {Language}",
                partialWord,
                language
            );
            return new List<string>();
        }
    }

    public async Task<DictionaryStats> GetDictionaryStatsAsync(GameLanguage language)
    {
        try
        {
            var statsKey = GetDictionaryStatsKey(language);
            var cachedStats = await _redis.GetAsync<DictionaryStats>(statsKey);

            if (cachedStats != null)
                return cachedStats;

            // Generate fresh stats
            var dictionaryKey = GetDictionaryKey(language);
            var allWords = await _redis.SetGetAllAsync(dictionaryKey);
            var mostFrequent = await GetMostFrequentWordsAsync(language, 10);

            var stats = new DictionaryStats
            {
                Language = language,
                TotalWords = allWords.Length,
                CachedSimilarities = 0, // Would need to count similarity cache keys
                LastUpdated = DateTime.UtcNow,
                MostFrequentWords = mostFrequent
            };

            // Cache stats for 1 hour
            await _redis.SetAsync(statsKey, stats, TimeSpan.FromHours(1));

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get dictionary stats for {Language}", language);
            return new DictionaryStats { Language = language, LastUpdated = DateTime.UtcNow };
        }
    }

    public async Task<int> ClearSimilarityCacheAsync(GameLanguage? language = null)
    {
        try
        {
            var pattern = language.HasValue
                ? $"similarity:{language.Value.ToString().ToLowerInvariant()}:*"
                : "similarity:*";

            var keys = await _redis.ScanKeysAsync(pattern);
            var deletedCount = 0;

            foreach (var key in keys)
            {
                var deleted = await _redis.DeleteAsync(key);
                if (deleted)
                    deletedCount++;
            }

            _logger.LogInformation(
                "Cleared {Count} similarity cache entries for language {Language}",
                deletedCount,
                language?.ToString() ?? "ALL"
            );

            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear similarity cache for {Language}", language);
            return 0;
        }
    }

    public async Task<int> PreloadCommonWordsAsync(GameLanguage language)
    {
        try
        {
            var commonWords = GetCommonWords(language);
            return await AddWordsToDictionaryAsync(commonWords, language);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to preload common words for {Language}", language);
            return 0;
        }
    }

    public async Task<List<string>> GetMostFrequentWordsAsync(
        GameLanguage language,
        int count = 100
    )
    {
        try
        {
            var key = GetWordFrequencyKey(language);
            var topWords = await _redis.SortedSetGetTopAsync(key, count);

            return topWords.Select(w => w.Value).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get most frequent words for {Language}", language);
            return new List<string>();
        }
    }

    public async Task<long> TrackWordUsageAsync(string word, GameLanguage language)
    {
        try
        {
            var key = GetWordFrequencyKey(language);
            var normalizedWord = NormalizeWord(word);

            return await _redis.IncrementAsync($"{key}:{normalizedWord}", 1);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to track word usage for {Word} in {Language}",
                word,
                language
            );
            return 0;
        }
    }

    private async Task UpdateDictionaryStatsAsync(GameLanguage language)
    {
        try
        {
            // Invalidate cached stats so they'll be regenerated on next request
            var statsKey = GetDictionaryStatsKey(language);
            await _redis.DeleteAsync(statsKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update dictionary stats for {Language}", language);
        }
    }

    private static string NormalizeWord(string word)
    {
        // Basic normalization - in a real implementation you'd want more sophisticated normalization
        // especially for Arabic text (remove diacritics, normalize characters)
        return word.Trim().ToLowerInvariant();
    }

    private static List<string> GetCommonWords(GameLanguage language)
    {
        // This would typically be loaded from a file or database
        // For now, return a small sample of common words
        return language switch
        {
            GameLanguage.EN
                => new List<string>
                {
                    "the",
                    "and",
                    "or",
                    "but",
                    "in",
                    "on",
                    "at",
                    "to",
                    "for",
                    "with",
                    "cat",
                    "dog",
                    "house",
                    "car",
                    "tree",
                    "book",
                    "water",
                    "fire",
                    "sun",
                    "moon",
                    "happy",
                    "sad",
                    "big",
                    "small",
                    "good",
                    "bad",
                    "fast",
                    "slow",
                    "hot",
                    "cold",
                    "red",
                    "blue",
                    "green",
                    "yellow",
                    "black",
                    "white",
                    "orange",
                    "purple",
                    "pink",
                    "brown"
                },
            GameLanguage.AR
                => new List<string>
                {
                    "في",
                    "من",
                    "إلى",
                    "على",
                    "مع",
                    "هذا",
                    "ذلك",
                    "التي",
                    "الذي",
                    "كان",
                    "قطة",
                    "كلب",
                    "بيت",
                    "سيارة",
                    "شجرة",
                    "كتاب",
                    "ماء",
                    "نار",
                    "شمس",
                    "قمر",
                    "سعيد",
                    "حزين",
                    "كبير",
                    "صغير",
                    "جيد",
                    "سيء",
                    "سريع",
                    "بطيء",
                    "حار",
                    "بارد",
                    "أحمر",
                    "أزرق",
                    "أخضر",
                    "أصفر",
                    "أسود",
                    "أبيض",
                    "برتقالي",
                    "بنفسجي",
                    "وردي",
                    "بني"
                },
            _ => new List<string>()
        };
    }
}
