namespace Gchain.Models;

/// <summary>
/// Configuration settings for Hugging Face API integration
/// </summary>
public class HuggingFaceSettings
{
    /// <summary>
    /// Hugging Face API token
    /// </summary>
    public string ApiToken { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for Hugging Face Inference API
    /// </summary>
    public string BaseUrl { get; set; } = "https://api-inference.huggingface.co";

    /// <summary>
    /// Model for English semantic similarity
    /// </summary>
    public string EnglishModel { get; set; } = "sentence-transformers/all-MiniLM-L6-v2";

    /// <summary>
    /// Model for Arabic semantic similarity
    /// </summary>
    public string ArabicModel { get; set; } = "aubmindlab/bert-base-arabertv02";

    /// <summary>
    /// Default similarity threshold for word validation
    /// </summary>
    public double DefaultSimilarityThreshold { get; set; } = 0.65;

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum number of retries for failed requests
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Enable fallback to basic string similarity if AI model fails
    /// </summary>
    public bool EnableFallback { get; set; } = true;

    /// <summary>
    /// Maximum wait time for model loading in minutes
    /// </summary>
    public int MaxModelLoadWaitMinutes { get; set; } = 5;

    /// <summary>
    /// Check interval for model loading status in seconds
    /// </summary>
    public int ModelLoadCheckIntervalSeconds { get; set; } = 10;

    /// <summary>
    /// Enable model loading checks
    /// </summary>
    public bool EnableModelLoadingChecks { get; set; } = true;

    /// <summary>
    /// Maximum input length for the model (all-MiniLM-L6-v2 supports up to 256 tokens)
    /// </summary>
    public int MaxInputLength { get; set; } = 256;
}
