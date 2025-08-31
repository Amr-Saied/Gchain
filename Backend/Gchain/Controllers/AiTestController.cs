using Gchain.Interfaces;
using Gchain.Models;
using Microsoft.AspNetCore.Mvc;

namespace Gchain.Controllers;

/// <summary>
/// Controller to test AI and Redis integration
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AiTestController : ControllerBase
{
    private readonly ISemanticSimilarityService _semanticSimilarity;
    private readonly IWordCacheService _wordCache;
    private readonly IRedisService _redis;
    private readonly ILogger<AiTestController> _logger;

    public AiTestController(
        ISemanticSimilarityService semanticSimilarity,
        IWordCacheService wordCache,
        IRedisService redis,
        ILogger<AiTestController> logger
    )
    {
        _semanticSimilarity = semanticSimilarity;
        _wordCache = wordCache;
        _redis = redis;
        _logger = logger;
    }

    /// <summary>
    /// Test Hugging Face health
    /// </summary>
    [HttpGet("health")]
    public async Task<IActionResult> TestHealth()
    {
        try
        {
            var isHealthy = await _semanticSimilarity.IsHealthyAsync();
            return Ok(new { healthy = isHealthy, timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hugging Face health test failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Test all-MiniLM-L6-v2 model specifically
    /// </summary>
    [HttpGet("test-minilm")]
    public async Task<IActionResult> TestMiniLM()
    {
        try
        {
            var testWords = new[]
            {
                new { word1 = "cat", word2 = "dog" },
                new { word1 = "house", word2 = "home" },
                new { word1 = "car", word2 = "vehicle" },
                new { word1 = "happy", word2 = "joyful" },
                new { word1 = "ocean", word2 = "sea" }
            };

            var results = new List<object>();

            foreach (var test in testWords)
            {
                try
                {
                    var similarity = await _semanticSimilarity.ComputeSimilarityAsync(
                        test.word1,
                        test.word2,
                        GameLanguage.EN
                    );

                    results.Add(
                        new
                        {
                            word1 = test.word1,
                            word2 = test.word2,
                            similarity = Math.Round(similarity, 4),
                            expectedHigh = similarity > 0.5
                        }
                    );
                }
                catch (Exception ex)
                {
                    results.Add(
                        new
                        {
                            word1 = test.word1,
                            word2 = test.word2,
                            error = ex.Message
                        }
                    );
                }
            }

            return Ok(
                new
                {
                    model = "all-MiniLM-L6-v2",
                    testResults = results,
                    timestamp = DateTime.UtcNow
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MiniLM model test failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Test word similarity computation
    /// </summary>
    [HttpPost("test-similarity")]
    public async Task<IActionResult> TestSimilarity([FromBody] SimilarityTestRequest request)
    {
        try
        {
            var startTime = DateTime.UtcNow;

            // Test direct computation (no cache)
            var directSimilarity = await _semanticSimilarity.ComputeSimilarityAsync(
                request.Word1,
                request.Word2,
                request.Language
            );

            var computeTime = DateTime.UtcNow - startTime;

            // Test cached retrieval
            startTime = DateTime.UtcNow;
            var cachedSimilarity = await _semanticSimilarity.GetSimilarityAsync(
                request.Word1,
                request.Word2,
                request.Language
            );

            var cacheTime = DateTime.UtcNow - startTime;

            return Ok(
                new
                {
                    word1 = request.Word1,
                    word2 = request.Word2,
                    language = request.Language.ToString(),
                    directSimilarity,
                    cachedSimilarity,
                    computeTimeMs = computeTime.TotalMilliseconds,
                    cacheTimeMs = cacheTime.TotalMilliseconds,
                    isCached = Math.Abs(directSimilarity - cachedSimilarity) < 0.0001
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Similarity test failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Test word validation workflow
    /// </summary>
    [HttpPost("test-validation")]
    public async Task<IActionResult> TestValidation([FromBody] ValidationTestRequest request)
    {
        try
        {
            // First, add words to dictionary
            await _wordCache.AddWordToDictionaryAsync(request.TargetWord, request.Language);
            await _wordCache.AddWordToDictionaryAsync(request.GuessedWord, request.Language);

            // Test validation
            var result = await _semanticSimilarity.ValidateWordSimilarityAsync(
                request.TargetWord,
                request.GuessedWord,
                request.Language,
                request.Threshold
            );

            return Ok(
                new
                {
                    targetWord = request.TargetWord,
                    guessedWord = request.GuessedWord,
                    language = request.Language.ToString(),
                    threshold = request.Threshold,
                    result = new
                    {
                        isValid = result.IsValid,
                        similarityScore = result.SimilarityScore,
                        thresholdUsed = result.ThresholdUsed,
                        rejectionReason = result.RejectionReason,
                        wasCached = result.WasCached
                    }
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validation test failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Test word dictionary functionality
    /// </summary>
    [HttpGet("test-dictionary")]
    public async Task<IActionResult> TestDictionary(
        [FromQuery] GameLanguage language = GameLanguage.EN
    )
    {
        try
        {
            // Preload common words
            var preloadedCount = await _wordCache.PreloadCommonWordsAsync(language);

            // Get dictionary stats
            var stats = await _wordCache.GetDictionaryStatsAsync(language);

            // Test word suggestions
            var suggestions = await _wordCache.GetWordSuggestionsAsync("ca", language, 5);

            // Track some word usage
            await _wordCache.TrackWordUsageAsync("cat", language);
            await _wordCache.TrackWordUsageAsync("car", language);
            await _wordCache.TrackWordUsageAsync("cat", language); // Track again

            // Get most frequent words
            var frequentWords = await _wordCache.GetMostFrequentWordsAsync(language, 10);

            return Ok(
                new
                {
                    language = language.ToString(),
                    preloadedCount,
                    stats = new
                    {
                        totalWords = stats.TotalWords,
                        cachedSimilarities = stats.CachedSimilarities,
                        lastUpdated = stats.LastUpdated,
                        mostFrequentWords = stats.MostFrequentWords
                    },
                    suggestions,
                    frequentWords
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dictionary test failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Test cache management
    /// </summary>
    [HttpPost("test-cache-management")]
    public async Task<IActionResult> TestCacheManagement([FromQuery] GameLanguage? language = null)
    {
        try
        {
            // Add some similarity cache entries
            await _wordCache.CacheSimilarityAsync("dog", "cat", 0.75, GameLanguage.EN);
            await _wordCache.CacheSimilarityAsync("house", "home", 0.85, GameLanguage.EN);
            await _wordCache.CacheSimilarityAsync("كلب", "قطة", 0.70, GameLanguage.AR);

            // Get cached similarities
            var dogCatSimilarity = await _wordCache.GetCachedSimilarityAsync(
                "dog",
                "cat",
                GameLanguage.EN
            );
            var houseHomeSimilarity = await _wordCache.GetCachedSimilarityAsync(
                "house",
                "home",
                GameLanguage.EN
            );
            var arabicSimilarity = await _wordCache.GetCachedSimilarityAsync(
                "كلب",
                "قطة",
                GameLanguage.AR
            );

            // Clear cache
            var clearedCount = await _wordCache.ClearSimilarityCacheAsync(language);

            // Check if cache was cleared
            var dogCatAfterClear = await _wordCache.GetCachedSimilarityAsync(
                "dog",
                "cat",
                GameLanguage.EN
            );
            var arabicAfterClear = await _wordCache.GetCachedSimilarityAsync(
                "كلب",
                "قطة",
                GameLanguage.AR
            );

            return Ok(
                new
                {
                    beforeClear = new
                    {
                        dogCat = dogCatSimilarity,
                        houseHome = houseHomeSimilarity,
                        arabic = arabicSimilarity
                    },
                    clearedCount,
                    clearedLanguage = language?.ToString() ?? "ALL",
                    afterClear = new { dogCat = dogCatAfterClear, arabic = arabicAfterClear }
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache management test failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Test game scenario workflow
    /// </summary>
    [HttpPost("test-game-scenario")]
    public async Task<IActionResult> TestGameScenario()
    {
        try
        {
            var targetWord = "ocean";
            var playerGuesses = new[] { "sea", "water", "fish", "car", "mountain" };
            var language = GameLanguage.EN;

            // Preload dictionary
            await _wordCache.PreloadCommonWordsAsync(language);
            await _wordCache.AddWordToDictionaryAsync(targetWord, language);
            foreach (var guess in playerGuesses)
            {
                await _wordCache.AddWordToDictionaryAsync(guess, language);
            }

            var results = new List<object>();

            foreach (var guess in playerGuesses)
            {
                var validation = await _semanticSimilarity.ValidateWordSimilarityAsync(
                    targetWord,
                    guess,
                    language
                );

                // Track word usage
                await _wordCache.TrackWordUsageAsync(guess, language);

                results.Add(
                    new
                    {
                        guess,
                        isValid = validation.IsValid,
                        score = validation.SimilarityScore,
                        threshold = validation.ThresholdUsed,
                        reason = validation.RejectionReason,
                        wasCached = validation.WasCached
                    }
                );
            }

            return Ok(
                new
                {
                    targetWord,
                    language = language.ToString(),
                    results
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Game scenario test failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

/// <summary>
/// Request model for similarity testing
/// </summary>
public class SimilarityTestRequest
{
    public string Word1 { get; set; } = string.Empty;
    public string Word2 { get; set; } = string.Empty;
    public GameLanguage Language { get; set; } = GameLanguage.EN;
}

/// <summary>
/// Request model for validation testing
/// </summary>
public class ValidationTestRequest
{
    public string TargetWord { get; set; } = string.Empty;
    public string GuessedWord { get; set; } = string.Empty;
    public GameLanguage Language { get; set; } = GameLanguage.EN;
    public double? Threshold { get; set; }
}
