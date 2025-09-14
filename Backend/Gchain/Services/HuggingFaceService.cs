using System.Text;
using System.Text.Json;
using Gchain.Interfaces;
using Gchain.Models;
using Microsoft.Extensions.Options;

namespace Gchain.Services;

/// <summary>
/// Service for semantic similarity computation using Hugging Face API
/// Core functionality: Word validation during gameplay using semantic similarity
/// </summary>
public class HuggingFaceService : ISemanticSimilarityService
{
    private readonly HttpClient _httpClient;
    private readonly IWordCacheService _wordCache;
    private readonly HuggingFaceSettings _settings;
    private readonly ILogger<HuggingFaceService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public HuggingFaceService(
        HttpClient httpClient,
        IWordCacheService wordCache,
        IOptions<HuggingFaceSettings> settings,
        ILogger<HuggingFaceService> logger
    )
    {
        _httpClient = httpClient;
        _wordCache = wordCache;
        _settings = settings.Value;
        _logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.ApiToken}");
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
    }

    /// <summary>
    /// Core method: Compute semantic similarity between two words using Hugging Face API
    /// Used during gameplay to validate player word guesses
    /// </summary>
    public async Task<double> ComputeSimilarityAsync(
        string word1,
        string word2,
        GameLanguage language
    )
    {
        try
        {
            var model = GetModelForLanguage(language);
            var url = $"{_settings.BaseUrl}/models/{model}";

            // Ensure model is loaded before making requests
            await EnsureModelLoadedAsync(model);

            // Use the sentence_similarity format: {"inputs": {"source_sentence": "word1", "sentences": ["word2"]}}
            var requestBody = new
            {
                inputs = new { source_sentence = word1, sentences = new[] { word2 } }
            };

            var jsonContent = JsonSerializer.Serialize(requestBody, _jsonOptions);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            _logger.LogInformation(
                "Sending sentence_similarity request to {Url} with body: {Body}",
                url,
                jsonContent
            );

            var response = await SendWithRetryAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Success! Response: {Response}", responseContent);

                // Parse the similarity score directly
                var similarityScores = JsonSerializer.Deserialize<double[]>(
                    responseContent,
                    _jsonOptions
                );

                if (similarityScores != null && similarityScores.Length > 0)
                {
                    var similarity = similarityScores[0]; // First (and only) similarity score

                    _logger.LogDebug(
                        "Computed similarity between '{Word1}' and '{Word2}': {Score}",
                        word1,
                        word2,
                        similarity
                    );

                    return similarity;
                }
                else
                {
                    _logger.LogWarning(
                        "Failed to parse similarity response for words: {Word1}, {Word2}",
                        word1,
                        word2
                    );
                    return _settings.EnableFallback ? ComputeFallbackSimilarity(word1, word2) : 0.0;
                }
            }
            else
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning(
                    "Hugging Face API returned {StatusCode}: {ReasonPhrase} for words: {Word1}, {Word2}. Response: {Response}",
                    response.StatusCode,
                    response.ReasonPhrase,
                    word1,
                    word2,
                    responseContent
                );

                // If sentence_similarity fails, use fallback
                return _settings.EnableFallback ? ComputeFallbackSimilarity(word1, word2) : 0.0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error computing similarity between '{Word1}' and '{Word2}'",
                word1,
                word2
            );

            if (_settings.EnableFallback)
            {
                return ComputeFallbackSimilarity(word1, word2);
            }

            throw;
        }
    }

    /// <summary>
    /// Get similarity with caching - used by the game engine
    /// </summary>
    public async Task<double> GetSimilarityAsync(string word1, string word2, GameLanguage language)
    {
        try
        {
            // Check cache first
            var cachedSimilarity = await _wordCache.GetCachedSimilarityAsync(
                word1,
                word2,
                language
            );
            if (cachedSimilarity.HasValue)
            {
                _logger.LogDebug(
                    "Found cached similarity for '{Word1}' and '{Word2}': {Score}",
                    word1,
                    word2,
                    cachedSimilarity.Value
                );
                return cachedSimilarity.Value;
            }

            // Compute using AI model
            var similarity = await ComputeSimilarityAsync(word1, word2, language);

            // Cache the result
            await _wordCache.CacheSimilarityAsync(word1, word2, similarity, language);

            return similarity;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to get similarity between '{Word1}' and '{Word2}'",
                word1,
                word2
            );
            return 0.0;
        }
    }

    /// <summary>
    /// Core game method: Validate if a player's word guess is correct
    /// Used during each turn to determine if the word is semantically similar enough
    /// </summary>
    public async Task<WordValidationResult> ValidateWordSimilarityAsync(
        string targetWord,
        string guessedWord,
        GameLanguage language,
        double? threshold = null
    )
    {
        try
        {
            var thresholdValue = threshold ?? _settings.DefaultSimilarityThreshold;

            // Check if word exists in dictionary first
            var wordExists = await _wordCache.IsWordInDictionaryAsync(guessedWord, language);
            if (!wordExists)
            {
                return new WordValidationResult
                {
                    IsValid = false,
                    SimilarityScore = 0.0,
                    ThresholdUsed = thresholdValue,
                    TargetWord = targetWord,
                    GuessedWord = guessedWord,
                    Language = language,
                    RejectionReason = "Word not found in dictionary",
                    WasCached = false
                };
            }

            // Check cache first
            var cachedSimilarity = await _wordCache.GetCachedSimilarityAsync(
                targetWord,
                guessedWord,
                language
            );
            var wasCached = cachedSimilarity.HasValue;

            var similarity = wasCached
                ? cachedSimilarity.Value
                : await ComputeSimilarityAsync(targetWord, guessedWord, language);

            // Cache if not cached
            if (!wasCached)
            {
                await _wordCache.CacheSimilarityAsync(
                    targetWord,
                    guessedWord,
                    similarity,
                    language
                );
            }

            var isValid = similarity >= thresholdValue;

            return new WordValidationResult
            {
                IsValid = isValid,
                SimilarityScore = similarity,
                ThresholdUsed = thresholdValue,
                TargetWord = targetWord,
                GuessedWord = guessedWord,
                Language = language,
                RejectionReason = isValid
                    ? null
                    : $"Similarity score {similarity:F3} below threshold {thresholdValue:F3}",
                WasCached = wasCached
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to validate word similarity for '{TargetWord}' vs '{GuessedWord}'",
                targetWord,
                guessedWord
            );

            return new WordValidationResult
            {
                IsValid = false,
                SimilarityScore = 0.0,
                ThresholdUsed = threshold ?? _settings.DefaultSimilarityThreshold,
                TargetWord = targetWord,
                GuessedWord = guessedWord,
                Language = language,
                RejectionReason = "Error occurred during validation",
                WasCached = false
            };
        }
    }

    /// <summary>
    /// Health check for the Hugging Face service
    /// Used by the application's health check system
    /// </summary>
    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            // Test with a simple similarity check
            var testSimilarity = await ComputeSimilarityAsync("cat", "dog", GameLanguage.EN);
            return testSimilarity >= 0.0; // Should return a valid score
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hugging Face health check failed");
            return false;
        }
    }

    /// <summary>
    /// Get the appropriate model for the specified language
    /// </summary>
    private string GetModelForLanguage(GameLanguage language)
    {
        return _settings.EnglishModel;
    }

    /// <summary>
    /// Send HTTP request with retry logic for reliability
    /// </summary>
    private async Task<HttpResponseMessage> SendWithRetryAsync(string url, StringContent content)
    {
        HttpResponseMessage response = null!;

        for (int attempt = 1; attempt <= _settings.MaxRetries; attempt++)
        {
            try
            {
                response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    return response;
                }

                // If it's a rate limit (429) or server error (5xx), retry
                if (
                    response.StatusCode == System.Net.HttpStatusCode.TooManyRequests
                    || ((int)response.StatusCode >= 500 && (int)response.StatusCode < 600)
                )
                {
                    if (attempt < _settings.MaxRetries)
                    {
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // Exponential backoff
                        _logger.LogWarning(
                            "API request failed with {StatusCode}, retrying in {Delay}s (attempt {Attempt}/{MaxRetries})",
                            response.StatusCode,
                            delay.TotalSeconds,
                            attempt,
                            _settings.MaxRetries
                        );

                        await Task.Delay(delay);
                        continue;
                    }
                }

                return response;
            }
            catch (Exception ex) when (attempt < _settings.MaxRetries)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                _logger.LogWarning(
                    ex,
                    "API request failed, retrying in {Delay}s (attempt {Attempt}/{MaxRetries})",
                    delay.TotalSeconds,
                    attempt,
                    _settings.MaxRetries
                );

                await Task.Delay(delay);
            }
        }

        return response
            ?? throw new HttpRequestException("Failed to get response after all retries");
    }

    /// <summary>
    /// Ensures the model is loaded and ready for inference
    /// </summary>
    private async Task EnsureModelLoadedAsync(string model)
    {
        try
        {
            var url = $"{_settings.BaseUrl}/models/{model}";

            // Check if model is ready by sending a simple request
            var testRequest = new { inputs = "test" };
            var jsonContent = JsonSerializer.Serialize(testRequest, _jsonOptions);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);

            if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
            {
                _logger.LogInformation(
                    "Model {Model} is loading, waiting for it to be ready...",
                    model
                );

                // Wait for model to load (this can take a few minutes for large models)
                var maxWaitTime = TimeSpan.FromMinutes(_settings.MaxModelLoadWaitMinutes);
                var startTime = DateTime.UtcNow;

                while (DateTime.UtcNow - startTime < maxWaitTime)
                {
                    await Task.Delay(TimeSpan.FromSeconds(_settings.ModelLoadCheckIntervalSeconds));

                    var healthCheck = await _httpClient.PostAsync(url, content);
                    if (healthCheck.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Model {Model} is now ready!", model);
                        return;
                    }
                }

                throw new TimeoutException(
                    $"Model {model} failed to load within {maxWaitTime.TotalMinutes} minutes"
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking model loading status for {Model}", model);
            // Continue anyway - the model might already be loaded
        }
    }

    /// <summary>
    /// Fallback similarity computation using Levenshtein distance
    /// Used when Hugging Face API is unavailable
    /// </summary>
    private static double ComputeFallbackSimilarity(string word1, string word2)
    {
        // Simple fallback using Levenshtein distance
        var distance = ComputeLevenshteinDistance(
            word1.ToLowerInvariant(),
            word2.ToLowerInvariant()
        );
        var maxLength = Math.Max(word1.Length, word2.Length);

        if (maxLength == 0)
            return 1.0;

        var similarity = 1.0 - (double)distance / maxLength;
        return Math.Max(0.0, similarity);
    }

    /// <summary>
    /// Compute Levenshtein distance between two strings
    /// </summary>
    private static int ComputeLevenshteinDistance(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1))
            return s2?.Length ?? 0;
        if (string.IsNullOrEmpty(s2))
            return s1.Length;

        var matrix = new int[s1.Length + 1, s2.Length + 1];

        for (int i = 0; i <= s1.Length; i++)
            matrix[i, 0] = i;

        for (int j = 0; j <= s2.Length; j++)
            matrix[0, j] = j;

        for (int i = 1; i <= s1.Length; i++)
        {
            for (int j = 1; j <= s2.Length; j++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost
                );
            }
        }

        return matrix[s1.Length, s2.Length];
    }
}
