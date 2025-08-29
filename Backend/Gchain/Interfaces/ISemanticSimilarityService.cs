using Gchain.Models;

namespace Gchain.Interfaces;

/// <summary>
/// Interface for semantic similarity computation using external AI models
/// </summary>
public interface ISemanticSimilarityService
{
    /// <summary>
    /// Compute semantic similarity between two words
    /// </summary>
    /// <param name="word1">First word</param>
    /// <param name="word2">Second word</param>
    /// <param name="language">Language context</param>
    /// <returns>Similarity score between 0.0 and 1.0</returns>
    Task<double> ComputeSimilarityAsync(string word1, string word2, GameLanguage language);

    /// <summary>
    /// Get semantic similarity with caching
    /// This method checks cache first, then computes if needed
    /// </summary>
    /// <param name="word1">First word</param>
    /// <param name="word2">Second word</param>
    /// <param name="language">Language context</param>
    /// <returns>Similarity score between 0.0 and 1.0</returns>
    Task<double> GetSimilarityAsync(string word1, string word2, GameLanguage language);

    /// <summary>
    /// Validate if a word is semantically related to a target word
    /// </summary>
    /// <param name="targetWord">The target word (current game word)</param>
    /// <param name="guessedWord">The word guessed by player</param>
    /// <param name="language">Language context</param>
    /// <param name="threshold">Minimum similarity threshold (default from config)</param>
    /// <returns>Validation result with score and decision</returns>
    Task<WordValidationResult> ValidateWordSimilarityAsync(
        string targetWord,
        string guessedWord,
        GameLanguage language,
        double? threshold = null
    );

    /// <summary>
    /// Check if the service is healthy and can connect to AI models
    /// </summary>
    Task<bool> IsHealthyAsync();
}

/// <summary>
/// Result of word similarity validation
/// </summary>
public class WordValidationResult
{
    public bool IsValid { get; set; }
    public double SimilarityScore { get; set; }
    public double ThresholdUsed { get; set; }
    public string TargetWord { get; set; } = string.Empty;
    public string GuessedWord { get; set; } = string.Empty;
    public GameLanguage Language { get; set; }
    public string? RejectionReason { get; set; }
    public bool WasCached { get; set; }
}
