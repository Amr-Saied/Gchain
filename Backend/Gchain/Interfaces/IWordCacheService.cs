using Gchain.Models;

namespace Gchain.Interfaces;

/// <summary>
/// Interface for word dictionary and similarity caching
/// </summary>
public interface IWordCacheService
{
    /// <summary>
    /// Check if a word exists in the dictionary for a given language
    /// </summary>
    /// <param name="word">Word to check</param>
    /// <param name="language">Language to check in</param>
    /// <returns>True if word exists in dictionary</returns>
    Task<bool> IsWordInDictionaryAsync(string word, GameLanguage language);

    /// <summary>
    /// Add word to dictionary
    /// </summary>
    /// <param name="word">Word to add</param>
    /// <param name="language">Language to add to</param>
    /// <returns>True if word was added successfully</returns>
    Task<bool> AddWordToDictionaryAsync(string word, GameLanguage language);

    /// <summary>
    /// Add multiple words to dictionary
    /// </summary>
    /// <param name="words">Words to add</param>
    /// <param name="language">Language to add to</param>
    /// <returns>Number of words added successfully</returns>
    Task<int> AddWordsToDictionaryAsync(IEnumerable<string> words, GameLanguage language);

    /// <summary>
    /// Get cached similarity score between two words
    /// </summary>
    /// <param name="word1">First word</param>
    /// <param name="word2">Second word</param>
    /// <param name="language">Language context</param>
    /// <returns>Cached similarity score if exists, null otherwise</returns>
    Task<double?> GetCachedSimilarityAsync(string word1, string word2, GameLanguage language);

    /// <summary>
    /// Cache similarity score between two words
    /// </summary>
    /// <param name="word1">First word</param>
    /// <param name="word2">Second word</param>
    /// <param name="similarityScore">Similarity score to cache</param>
    /// <param name="language">Language context</param>
    /// <returns>True if cached successfully</returns>
    Task<bool> CacheSimilarityAsync(
        string word1,
        string word2,
        double similarityScore,
        GameLanguage language
    );

    /// <summary>
    /// Get word suggestions based on partial input
    /// </summary>
    /// <param name="partialWord">Partial word input</param>
    /// <param name="language">Language to search in</param>
    /// <param name="maxSuggestions">Maximum number of suggestions to return</param>
    /// <returns>List of word suggestions</returns>
    Task<List<string>> GetWordSuggestionsAsync(
        string partialWord,
        GameLanguage language,
        int maxSuggestions = 10
    );

    /// <summary>
    /// Get dictionary statistics
    /// </summary>
    /// <param name="language">Language to get stats for</param>
    /// <returns>Dictionary statistics</returns>
    Task<DictionaryStats> GetDictionaryStatsAsync(GameLanguage language);

    /// <summary>
    /// Clear similarity cache (for maintenance)
    /// </summary>
    /// <param name="language">Language to clear cache for, null for all languages</param>
    /// <returns>Number of entries cleared</returns>
    Task<int> ClearSimilarityCacheAsync(GameLanguage? language = null);

    /// <summary>
    /// Preload common words into cache
    /// </summary>
    /// <param name="language">Language to preload</param>
    /// <returns>Number of words preloaded</returns>
    Task<int> PreloadCommonWordsAsync(GameLanguage language);

    /// <summary>
    /// Get most frequently used words
    /// </summary>
    /// <param name="language">Language to get words for</param>
    /// <param name="count">Number of words to return</param>
    /// <returns>List of most frequent words</returns>
    Task<List<string>> GetMostFrequentWordsAsync(GameLanguage language, int count = 100);

    /// <summary>
    /// Track word usage frequency
    /// </summary>
    /// <param name="word">Word that was used</param>
    /// <param name="language">Language context</param>
    /// <returns>New usage count</returns>
    Task<long> TrackWordUsageAsync(string word, GameLanguage language);

    /// <summary>
    /// Get a random word for the game
    /// </summary>
    /// <param name="language">Language to get word from</param>
    /// <returns>Random word</returns>
    Task<string> GetRandomWordAsync(GameLanguage language);

    /// <summary>
    /// Validate word association with current game word
    /// </summary>
    /// <param name="currentWord">Current game word</param>
    /// <param name="guessedWord">Word to validate</param>
    /// <param name="language">Language context</param>
    /// <returns>Similarity score and validation result</returns>
    Task<(double similarityScore, bool isValid)> ValidateWordAssociationAsync(
        string currentWord,
        string guessedWord,
        GameLanguage language
    );
}

/// <summary>
/// Statistics about a language dictionary
/// </summary>
public class DictionaryStats
{
    public GameLanguage Language { get; set; }
    public int TotalWords { get; set; }
    public int CachedSimilarities { get; set; }
    public DateTime LastUpdated { get; set; }
    public List<string> MostFrequentWords { get; set; } = new List<string>();
}
