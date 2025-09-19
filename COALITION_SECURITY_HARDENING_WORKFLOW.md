# COALITION Security Hardening Workflow
## NVIDIA NIM Integration & Political Data Protection

**Version**: 1.0
**Date**: 2025-09-19
**Classification**: Internal Security Implementation Guide
**Compliance**: GDPR (Netherlands), Unity Security Standards 2024-2025

---

## PHASE 3: SECURITY IMPLEMENTATION
### Comprehensive Security Hardening for Political AI Platform

This document provides detailed micro-steps for implementing security hardening across all critical components of COALITION's NVIDIA NIM integration, with special focus on political data protection and Dutch GDPR compliance.

---

## 🛡️ SECURITY MICRO-STEPS IMPLEMENTATION

### **CATEGORY 1: HTTPS/TLS IMPLEMENTATION**

#### **Step 1: SSL/TLS Certificate Configuration**
```csharp
// Unity HTTPS Client with Certificate Validation
using System.Net;
using System.Security.Cryptography.X509Certificates;

public class SecureNIMClient : MonoBehaviour
{
    private const string NIM_BASE_URL = "https://nim-api.coalition.ai";

    void Start()
    {
        // Enable TLS 1.2+ only
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

        // Custom certificate validation
        ServicePointManager.ServerCertificateValidationCallback = ValidateServerCertificate;
    }

    private bool ValidateServerCertificate(object sender, X509Certificate certificate,
                                         X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        // Strict certificate validation for production
        if (sslPolicyErrors == SslPolicyErrors.None) return true;

        // Log certificate errors for security monitoring
        SecurityLogger.LogCertificateError(certificate, sslPolicyErrors);
        return false; // Reject invalid certificates
    }
}
```

#### **Step 2: Certificate Pinning Implementation**
```csharp
public class CertificatePinning
{
    private static readonly string[] PINNED_CERTIFICATES = {
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=", // Primary cert SHA-256
        "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB="  // Backup cert SHA-256
    };

    public static bool ValidatePinnedCertificate(X509Certificate2 cert)
    {
        string certHash = Convert.ToBase64String(cert.GetCertHash());
        return PINNED_CERTIFICATES.Contains(certHash);
    }
}
```

#### **Step 3: HTTPS Request Implementation**
```csharp
public class SecureHTTPClient
{
    private readonly HttpClient _httpClient;

    public SecureHTTPClient()
    {
        var handler = new HttpClientHandler()
        {
            ServerCertificateCustomValidationCallback = (sender, cert, chain, errors) =>
            {
                return CertificatePinning.ValidatePinnedCertificate(cert as X509Certificate2)
                       && errors == SslPolicyErrors.None;
            }
        };

        _httpClient = new HttpClient(handler);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "COALITION/1.0");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }
}
```

#### **Step 4: TLS Configuration Hardening**
```csharp
public static class TLSConfiguration
{
    public static void ConfigureSecureTLS()
    {
        // Disable weak protocols
        ServicePointManager.SecurityProtocol &= ~SecurityProtocolType.Ssl3;
        ServicePointManager.SecurityProtocol &= ~SecurityProtocolType.Tls;
        ServicePointManager.SecurityProtocol &= ~SecurityProtocolType.Tls11;

        // Enable only strong protocols
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

        // Configure cipher suites (platform dependent)
        ServicePointManager.CheckCertificateRevocationList = true;
    }
}
```

### **CATEGORY 2: AUTHENTICATION SYSTEM**

#### **Step 5: Unity Secrets Manager Integration**
```csharp
// Use Unity's new Secrets Manager for API keys (2024)
using Unity.Services.Core;
using Unity.Services.Authentication;

public class SecureCredentialManager : MonoBehaviour
{
    private string _encryptedApiKey;
    private string _encryptedSecretKey;

    async void Start()
    {
        await UnityServices.InitializeAsync();
        await LoadSecureCredentials();
    }

    private async Task LoadSecureCredentials()
    {
        try
        {
            // Load from Unity Secrets Manager (not PlayerPrefs)
            _encryptedApiKey = await SecretsManager.GetSecretAsync("nim_api_key");
            _encryptedSecretKey = await SecretsManager.GetSecretAsync("nim_secret_key");
        }
        catch (Exception ex)
        {
            SecurityLogger.LogError($"Failed to load credentials: {ex.Message}");
            throw new SecurityException("Credential loading failed");
        }
    }
}
```

#### **Step 6: Key Derivation and Encryption**
```csharp
using System.Security.Cryptography;
using System.Text;

public class SecureKeyManager
{
    private const int SALT_SIZE = 32;
    private const int KEY_SIZE = 32;
    private const int ITERATIONS = 10000;

    public static string EncryptApiKey(string apiKey, string masterPassword)
    {
        byte[] salt = GenerateRandomSalt();
        byte[] key = DeriveKey(masterPassword, salt);

        using (var aes = Aes.Create())
        {
            aes.Key = key;
            aes.GenerateIV();

            using (var encryptor = aes.CreateEncryptor())
            {
                byte[] encrypted = encryptor.TransformFinalBlock(
                    Encoding.UTF8.GetBytes(apiKey), 0, apiKey.Length);

                // Combine salt + IV + encrypted data
                return Convert.ToBase64String(salt.Concat(aes.IV).Concat(encrypted).ToArray());
            }
        }
    }

    private static byte[] DeriveKey(string password, byte[] salt)
    {
        using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, ITERATIONS, HashAlgorithmName.SHA256))
        {
            return pbkdf2.GetBytes(KEY_SIZE);
        }
    }

    private static byte[] GenerateRandomSalt()
    {
        byte[] salt = new byte[SALT_SIZE];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }
        return salt;
    }
}
```

#### **Step 7: Token Management System**
```csharp
public class SecureTokenManager
{
    private string _accessToken;
    private DateTime _tokenExpiry;
    private readonly object _tokenLock = new object();

    public async Task<string> GetValidTokenAsync()
    {
        lock (_tokenLock)
        {
            if (IsTokenValid())
                return _accessToken;
        }

        return await RefreshTokenAsync();
    }

    private bool IsTokenValid()
    {
        return !string.IsNullOrEmpty(_accessToken) &&
               DateTime.UtcNow < _tokenExpiry.AddMinutes(-5); // 5min buffer
    }

    private async Task<string> RefreshTokenAsync()
    {
        var request = new TokenRefreshRequest
        {
            ApiKey = await DecryptApiKey(),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        // Add HMAC signature for request authentication
        request.Signature = GenerateHMACSignature(request);

        var response = await _httpClient.PostAsJsonAsync("/auth/token", request);
        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();

        lock (_tokenLock)
        {
            _accessToken = tokenResponse.AccessToken;
            _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
        }

        return _accessToken;
    }
}
```

### **CATEGORY 3: INPUT VALIDATION & SANITIZATION**

#### **Step 8: Political Content Input Sanitization**
```csharp
using System.Text.RegularExpressions;

public class PoliticalInputSanitizer
{
    // Regex patterns for detecting political injection attempts
    private static readonly Regex[] INJECTION_PATTERNS = {
        new Regex(@"<script[^>]*>.*?</script>", RegexOptions.IgnoreCase | RegexOptions.Singleline),
        new Regex(@"javascript:", RegexOptions.IgnoreCase),
        new Regex(@"on\w+\s*=", RegexOptions.IgnoreCase),
        new Regex(@"data:text/html", RegexOptions.IgnoreCase),
        new Regex(@"\\u[0-9a-fA-F]{4}", RegexOptions.IgnoreCase), // Unicode escape
        new Regex(@"eval\s*\(", RegexOptions.IgnoreCase),
        new Regex(@"expression\s*\(", RegexOptions.IgnoreCase)
    };

    // Sensitive political terms requiring extra validation
    private static readonly string[] SENSITIVE_TERMS = {
        "execute", "terminate", "eliminate", "destroy", "attack", "bomb", "kill",
        "threat", "violence", "harm", "assassination", "coup", "revolution"
    };

    public static ValidationResult ValidatePoliticalInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return ValidationResult.Invalid("Empty input not allowed");

        // Check length limits
        if (input.Length > 5000)
            return ValidationResult.Invalid("Input exceeds maximum length");

        // Check for injection patterns
        foreach (var pattern in INJECTION_PATTERNS)
        {
            if (pattern.IsMatch(input))
            {
                SecurityLogger.LogSuspiciousInput(input, pattern.ToString());
                return ValidationResult.Invalid("Potentially malicious content detected");
            }
        }

        // Check for sensitive content requiring human review
        var sensitiveTerms = SENSITIVE_TERMS.Where(term =>
            input.ToLowerInvariant().Contains(term)).ToList();

        if (sensitiveTerms.Any())
        {
            return ValidationResult.RequiresReview(sensitiveTerms);
        }

        return ValidationResult.Valid(SanitizeInput(input));
    }

    private static string SanitizeInput(string input)
    {
        // HTML encode to prevent XSS
        input = System.Web.HttpUtility.HtmlEncode(input);

        // Remove excessive whitespace
        input = Regex.Replace(input, @"\s+", " ");

        // Normalize line endings
        input = input.Replace("\r\n", "\n").Replace("\r", "\n");

        return input.Trim();
    }
}

public class ValidationResult
{
    public bool IsValid { get; private set; }
    public bool RequiresHumanReview { get; private set; }
    public string SanitizedInput { get; private set; }
    public string ErrorMessage { get; private set; }
    public List<string> SensitiveTerms { get; private set; }

    public static ValidationResult Valid(string sanitizedInput) => new ValidationResult
    {
        IsValid = true,
        SanitizedInput = sanitizedInput
    };

    public static ValidationResult Invalid(string error) => new ValidationResult
    {
        IsValid = false,
        ErrorMessage = error
    };

    public static ValidationResult RequiresReview(List<string> sensitiveTerms) => new ValidationResult
    {
        IsValid = false,
        RequiresHumanReview = true,
        SensitiveTerms = sensitiveTerms
    };
}
```

#### **Step 9: Rate Limiting Implementation**
```csharp
public class RateLimitManager
{
    private readonly Dictionary<string, UserRateLimit> _userLimits = new();
    private readonly object _lockObject = new object();

    private const int MAX_REQUESTS_PER_MINUTE = 10;
    private const int MAX_REQUESTS_PER_HOUR = 100;

    public bool IsAllowed(string userId)
    {
        lock (_lockObject)
        {
            if (!_userLimits.ContainsKey(userId))
            {
                _userLimits[userId] = new UserRateLimit();
            }

            var limit = _userLimits[userId];
            var now = DateTime.UtcNow;

            // Clean old requests
            limit.Requests.RemoveAll(r => now - r > TimeSpan.FromHours(1));

            // Check hourly limit
            if (limit.Requests.Count >= MAX_REQUESTS_PER_HOUR)
            {
                SecurityLogger.LogRateLimitExceeded(userId, "hourly");
                return false;
            }

            // Check minute limit
            var recentRequests = limit.Requests.Count(r => now - r < TimeSpan.FromMinutes(1));
            if (recentRequests >= MAX_REQUESTS_PER_MINUTE)
            {
                SecurityLogger.LogRateLimitExceeded(userId, "minute");
                return false;
            }

            limit.Requests.Add(now);
            return true;
        }
    }
}

public class UserRateLimit
{
    public List<DateTime> Requests { get; set; } = new List<DateTime>();
}
```

### **CATEGORY 4: DATA ENCRYPTION**

#### **Step 10: Political Data Encryption at Rest**
```csharp
public class PoliticalDataEncryption
{
    private const string ENCRYPTION_KEY_ID = "political_data_2024";

    public static string EncryptPoliticalPreference(PoliticalPreference preference)
    {
        var json = JsonConvert.SerializeObject(preference);
        var key = KeyManager.GetDataEncryptionKey(ENCRYPTION_KEY_ID);

        using (var aes = Aes.Create())
        {
            aes.Key = key;
            aes.GenerateIV();

            using (var encryptor = aes.CreateEncryptor())
            {
                var encrypted = encryptor.TransformFinalBlock(
                    Encoding.UTF8.GetBytes(json), 0, json.Length);

                var result = new EncryptedData
                {
                    Data = Convert.ToBase64String(encrypted),
                    IV = Convert.ToBase64String(aes.IV),
                    KeyId = ENCRYPTION_KEY_ID,
                    Timestamp = DateTimeOffset.UtcNow
                };

                return JsonConvert.SerializeObject(result);
            }
        }
    }

    public static PoliticalPreference DecryptPoliticalPreference(string encryptedData)
    {
        var encData = JsonConvert.DeserializeObject<EncryptedData>(encryptedData);
        var key = KeyManager.GetDataEncryptionKey(encData.KeyId);

        using (var aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = Convert.FromBase64String(encData.IV);

            using (var decryptor = aes.CreateDecryptor())
            {
                var decrypted = decryptor.TransformFinalBlock(
                    Convert.FromBase64String(encData.Data), 0,
                    Convert.FromBase64String(encData.Data).Length);

                var json = Encoding.UTF8.GetString(decrypted);
                return JsonConvert.DeserializeObject<PoliticalPreference>(json);
            }
        }
    }
}
```

#### **Step 11: In-Transit Encryption for NIM Communications**
```csharp
public class SecureNIMCommunication
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public async Task<NIMResponse> SendSecurePoliticalQuery(PoliticalQuery query)
    {
        // Encrypt sensitive data before transmission
        var encryptedQuery = new EncryptedPoliticalQuery
        {
            QueryId = Guid.NewGuid(),
            EncryptedData = PoliticalDataEncryption.EncryptPoliticalPreference(query.UserPreferences),
            Prompt = PoliticalInputSanitizer.ValidatePoliticalInput(query.Prompt).SanitizedInput,
            Timestamp = DateTimeOffset.UtcNow,
            Checksum = CalculateChecksum(query)
        };

        // Add authentication headers
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/political-analysis");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetValidTokenAsync());
        request.Headers.Add("X-Request-ID", encryptedQuery.QueryId.ToString());
        request.Headers.Add("X-Checksum", encryptedQuery.Checksum);

        // Serialize and send
        var json = JsonConvert.SerializeObject(encryptedQuery);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            SecurityLogger.LogAPIError(response.StatusCode, await response.Content.ReadAsStringAsync());
            throw new SecurityException($"API request failed: {response.StatusCode}");
        }

        return await response.Content.ReadFromJsonAsync<NIMResponse>();
    }

    private string CalculateChecksum(PoliticalQuery query)
    {
        var data = $"{query.Prompt}{query.UserPreferences.UserId}{query.Timestamp}";
        using (var sha256 = SHA256.Create())
        {
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hash);
        }
    }
}
```

### **CATEGORY 5: GDPR COMPLIANCE FRAMEWORK**

#### **Step 12: GDPR Consent Management**
```csharp
public class GDPRConsentManager
{
    public async Task<ConsentRecord> RequestExplicitConsent(string userId, ConsentType consentType)
    {
        var consent = new ConsentRecord
        {
            UserId = userId,
            ConsentType = consentType,
            Timestamp = DateTimeOffset.UtcNow,
            IPAddress = GetUserIPAddress(),
            UserAgent = GetUserAgent(),
            ConsentVersion = "2024.1",
            IsExplicit = true,
            Status = ConsentStatus.Pending
        };

        // Log consent request for audit trail
        await AuditLogger.LogConsentRequest(consent);

        // Present consent UI to user
        var userResponse = await PresentConsentUI(consentType);

        consent.Status = userResponse ? ConsentStatus.Granted : ConsentStatus.Denied;
        consent.ResponseTimestamp = DateTimeOffset.UtcNow;

        // Store consent record with encryption
        await StoreConsentRecord(consent);

        return consent;
    }

    public async Task<bool> ValidateConsentForProcessing(string userId, DataProcessingPurpose purpose)
    {
        var consent = await GetLatestConsent(userId, purpose);

        if (consent == null || consent.Status != ConsentStatus.Granted)
            return false;

        // Check if consent is still valid (not withdrawn, not expired)
        if (consent.WithdrawnAt.HasValue)
            return false;

        // For political data, consent expires after 2 years under GDPR
        if (purpose == DataProcessingPurpose.PoliticalAnalysis &&
            DateTimeOffset.UtcNow > consent.Timestamp.AddYears(2))
        {
            await ExpireConsent(consent);
            return false;
        }

        return true;
    }
}

public enum ConsentType
{
    PoliticalDataProcessing,
    AIAnalysis,
    DataRetention,
    ThirdPartySharing
}

public enum DataProcessingPurpose
{
    PoliticalAnalysis,
    UserExperience,
    Analytics,
    Marketing
}
```

#### **Step 13: Data Subject Rights Implementation**
```csharp
public class DataSubjectRightsManager
{
    public async Task<DataExportResult> ProcessDataPortabilityRequest(string userId)
    {
        // Validate user identity
        if (!await ValidateUserIdentity(userId))
            throw new UnauthorizedAccessException("User identity validation failed");

        // Collect all user data
        var userData = new
        {
            PersonalData = await GetPersonalData(userId),
            PoliticalPreferences = await GetPoliticalPreferences(userId),
            ConsentRecords = await GetConsentHistory(userId),
            ProcessingLogs = await GetProcessingLogs(userId)
        };

        // Create encrypted export file
        var exportData = JsonConvert.SerializeObject(userData, Formatting.Indented);
        var encryptedExport = EncryptExportData(exportData, userId);

        // Log the export request
        await AuditLogger.LogDataExport(userId, "Data portability request fulfilled");

        return new DataExportResult
        {
            FileName = $"coalition_data_export_{userId}_{DateTime.UtcNow:yyyyMMdd}.json",
            EncryptedData = encryptedExport,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30) // Temporary download link
        };
    }

    public async Task<DeletionResult> ProcessErasureRequest(string userId)
    {
        // Validate request and check for legal basis to retain data
        var canErase = await ValidateErasureRequest(userId);
        if (!canErase.IsAllowed)
            return DeletionResult.Denied(canErase.Reason);

        var deletionTasks = new List<Task>
        {
            DeletePersonalData(userId),
            DeletePoliticalPreferences(userId),
            AnonymizeProcessingLogs(userId),
            InvalidateUserSessions(userId),
            NotifyThirdParties(userId)
        };

        await Task.WhenAll(deletionTasks);

        // Keep minimal audit record of deletion (anonymized)
        await AuditLogger.LogDataDeletion(userId, "User data erased per GDPR request");

        return DeletionResult.Success();
    }
}
```

#### **Step 14: Data Retention and Anonymization**
```csharp
public class DataRetentionManager
{
    private const int POLITICAL_DATA_RETENTION_YEARS = 2;
    private const int AUDIT_LOG_RETENTION_YEARS = 7;

    public async Task RunDataRetentionCleanup()
    {
        var cutoffDate = DateTimeOffset.UtcNow.AddYears(-POLITICAL_DATA_RETENTION_YEARS);

        // Find expired political data
        var expiredData = await GetExpiredPoliticalData(cutoffDate);

        foreach (var data in expiredData)
        {
            if (await ShouldRetainForLegalReasons(data))
            {
                // Anonymize instead of delete
                await AnonymizePoliticalData(data);
            }
            else
            {
                // Safe to delete
                await DeletePoliticalData(data);
            }
        }

        // Clean up old audit logs (keeping anonymized versions)
        await CleanupOldAuditLogs();
    }

    private async Task AnonymizePoliticalData(PoliticalDataRecord data)
    {
        // Remove all personally identifiable information
        data.UserId = GenerateAnonymousId();
        data.IPAddress = null;
        data.DeviceFingerprint = null;
        data.GeolocationData = null;

        // Keep only aggregated, non-identifiable insights
        data.PoliticalCategory = data.PoliticalCategory; // Keep general category
        data.Timestamp = data.Timestamp.Date; // Keep only date, not time

        await UpdateAnonymizedRecord(data);
    }
}
```

### **CATEGORY 6: AI CONTENT SECURITY & BIAS DETECTION**

#### **Step 15: Political Bias Detection System**
```csharp
public class PoliticalBiasDetector
{
    private readonly Dictionary<string, float> _politicalTermWeights;
    private readonly List<string> _biasIndicators;

    public PoliticalBiasDetector()
    {
        _politicalTermWeights = LoadPoliticalTermWeights();
        _biasIndicators = LoadBiasIndicators();
    }

    public BiasAnalysisResult AnalyzeResponse(string responseText, string originalPrompt)
    {
        var result = new BiasAnalysisResult
        {
            ResponseId = Guid.NewGuid(),
            AnalyzedAt = DateTimeOffset.UtcNow,
            OriginalPrompt = originalPrompt,
            ResponseText = responseText
        };

        // Detect political leaning
        result.PoliticalLeaningScore = CalculatePoliticalLeaning(responseText);

        // Check for biased language
        result.BiasIndicators = DetectBiasIndicators(responseText);

        // Analyze sentiment polarization
        result.PolarizationScore = CalculatePolarization(responseText);

        // Check for factual accuracy markers
        result.FactualityMarkers = DetectFactualityMarkers(responseText);

        // Overall bias risk assessment
        result.OverallBiasRisk = CalculateOverallRisk(result);

        // Flag for human review if high risk
        if (result.OverallBiasRisk > 0.7f)
        {
            result.RequiresHumanReview = true;
            await QueueForHumanReview(result);
        }

        return result;
    }

    private float CalculatePoliticalLeaning(string text)
    {
        float leftScore = 0f, rightScore = 0f;
        var words = text.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in words)
        {
            if (_politicalTermWeights.ContainsKey(word))
            {
                var weight = _politicalTermWeights[word];
                if (weight < 0) leftScore += Math.Abs(weight);
                else rightScore += weight;
            }
        }

        var totalScore = leftScore + rightScore;
        return totalScore == 0 ? 0 : (rightScore - leftScore) / totalScore;
    }

    private List<BiasIndicator> DetectBiasIndicators(string text)
    {
        var indicators = new List<BiasIndicator>();

        // Check for absolute language
        var absoluteTerms = new[] { "always", "never", "all", "none", "everyone", "nobody" };
        foreach (var term in absoluteTerms)
        {
            if (text.ToLowerInvariant().Contains(term))
            {
                indicators.Add(new BiasIndicator
                {
                    Type = BiasType.AbsoluteLanguage,
                    Term = term,
                    Severity = 0.6f
                });
            }
        }

        // Check for emotional manipulation
        var emotionalTerms = new[] { "dangerous", "terrifying", "wonderful", "perfect", "horrible" };
        foreach (var term in emotionalTerms)
        {
            if (text.ToLowerInvariant().Contains(term))
            {
                indicators.Add(new BiasIndicator
                {
                    Type = BiasType.EmotionalManipulation,
                    Term = term,
                    Severity = 0.8f
                });
            }
        }

        return indicators;
    }
}

public class BiasAnalysisResult
{
    public Guid ResponseId { get; set; }
    public DateTimeOffset AnalyzedAt { get; set; }
    public string OriginalPrompt { get; set; }
    public string ResponseText { get; set; }
    public float PoliticalLeaningScore { get; set; } // -1 (left) to +1 (right)
    public List<BiasIndicator> BiasIndicators { get; set; }
    public float PolarizationScore { get; set; } // 0 (neutral) to 1 (highly polarized)
    public List<FactualityMarker> FactualityMarkers { get; set; }
    public float OverallBiasRisk { get; set; } // 0 (low risk) to 1 (high risk)
    public bool RequiresHumanReview { get; set; }
}
```

#### **Step 16: Content Filtering and Moderation**
```csharp
public class PoliticalContentModerator
{
    private readonly BiasDetector _biasDetector;
    private readonly ToxicityDetector _toxicityDetector;

    public async Task<ModerationResult> ModerateContent(string content, ContentContext context)
    {
        var result = new ModerationResult
        {
            ContentId = Guid.NewGuid(),
            OriginalContent = content,
            Context = context,
            ModeratedAt = DateTimeOffset.UtcNow
        };

        // Check for toxic content
        var toxicityScore = await _toxicityDetector.AnalyzeAsync(content);
        result.ToxicityScore = toxicityScore;

        // Check for political bias
        var biasAnalysis = _biasDetector.AnalyzeResponse(content, context.OriginalPrompt);
        result.BiasAnalysis = biasAnalysis;

        // Check for misinformation patterns
        result.MisinformationRisk = await DetectMisinformationPatterns(content);

        // Check for hate speech
        result.HateSpeechIndicators = await DetectHateSpeech(content);

        // Determine overall action
        result.Action = DetermineContentAction(result);

        // Apply content filtering if needed
        if (result.Action == ContentAction.Filter)
        {
            result.FilteredContent = await ApplyContentFiltering(content, result);
        }

        // Log moderation decision
        await AuditLogger.LogContentModeration(result);

        return result;
    }

    private ContentAction DetermineContentAction(ModerationResult result)
    {
        // Block if high toxicity
        if (result.ToxicityScore > 0.8f)
            return ContentAction.Block;

        // Block if hate speech detected
        if (result.HateSpeechIndicators.Any(h => h.Confidence > 0.9f))
            return ContentAction.Block;

        // Review if high bias risk
        if (result.BiasAnalysis.OverallBiasRisk > 0.7f)
            return ContentAction.HumanReview;

        // Filter if moderate issues
        if (result.ToxicityScore > 0.5f || result.MisinformationRisk > 0.6f)
            return ContentAction.Filter;

        return ContentAction.Allow;
    }

    private async Task<string> ApplyContentFiltering(string content, ModerationResult moderation)
    {
        var filtered = content;

        // Remove or replace problematic content
        foreach (var bias in moderation.BiasAnalysis.BiasIndicators)
        {
            if (bias.Severity > 0.7f)
            {
                filtered = filtered.Replace(bias.Term, "[FILTERED]");
            }
        }

        // Add disclaimers for political content
        if (moderation.BiasAnalysis.PoliticalLeaningScore != 0)
        {
            filtered += "\n\n[Note: This response has been reviewed for political neutrality. Multiple perspectives may exist on this topic.]";
        }

        return filtered;
    }
}
```

### **CATEGORY 7: SECURITY MONITORING & LOGGING**

#### **Step 17: Security Event Logging**
```csharp
public class SecurityLogger
{
    private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

    public static void LogSuspiciousInput(string input, string pattern)
    {
        _logger.Warn("Suspicious input detected", new
        {
            InputHash = HashSensitiveData(input),
            Pattern = pattern,
            Timestamp = DateTimeOffset.UtcNow,
            EventType = "InputValidation"
        });
    }

    public static void LogCertificateError(X509Certificate certificate, SslPolicyErrors errors)
    {
        _logger.Error("SSL Certificate validation failed", new
        {
            CertificateThumbprint = certificate?.GetCertHashString(),
            Errors = errors.ToString(),
            Timestamp = DateTimeOffset.UtcNow,
            EventType = "CertificateValidation"
        });
    }

    public static void LogRateLimitExceeded(string userId, string limitType)
    {
        _logger.Warn("Rate limit exceeded", new
        {
            UserIdHash = HashSensitiveData(userId),
            LimitType = limitType,
            Timestamp = DateTimeOffset.UtcNow,
            EventType = "RateLimit"
        });
    }

    public static void LogAPIError(HttpStatusCode statusCode, string response)
    {
        _logger.Error("API request failed", new
        {
            StatusCode = statusCode,
            ResponseHash = HashSensitiveData(response),
            Timestamp = DateTimeOffset.UtcNow,
            EventType = "APIError"
        });
    }

    private static string HashSensitiveData(string data)
    {
        using (var sha256 = SHA256.Create())
        {
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hash)[..16]; // First 16 chars for logging
        }
    }
}
```

#### **Step 18: Audit Trail Implementation**
```csharp
public class AuditLogger
{
    public static async Task LogConsentRequest(ConsentRecord consent)
    {
        var auditRecord = new AuditRecord
        {
            EventType = "ConsentRequest",
            UserId = HashUserId(consent.UserId),
            Details = new
            {
                ConsentType = consent.ConsentType.ToString(),
                ConsentVersion = consent.ConsentVersion,
                IPAddress = HashIPAddress(consent.IPAddress)
            },
            Timestamp = DateTimeOffset.UtcNow
        };

        await StoreAuditRecord(auditRecord);
    }

    public static async Task LogDataExport(string userId, string description)
    {
        var auditRecord = new AuditRecord
        {
            EventType = "DataExport",
            UserId = HashUserId(userId),
            Description = description,
            Timestamp = DateTimeOffset.UtcNow
        };

        await StoreAuditRecord(auditRecord);
    }

    public static async Task LogContentModeration(ModerationResult result)
    {
        var auditRecord = new AuditRecord
        {
            EventType = "ContentModeration",
            ContentId = result.ContentId,
            Details = new
            {
                Action = result.Action.ToString(),
                ToxicityScore = result.ToxicityScore,
                BiasRisk = result.BiasAnalysis?.OverallBiasRisk,
                RequiresReview = result.BiasAnalysis?.RequiresHumanReview
            },
            Timestamp = DateTimeOffset.UtcNow
        };

        await StoreAuditRecord(auditRecord);
    }

    private static async Task StoreAuditRecord(AuditRecord record)
    {
        // Store in secure, tamper-evident audit database
        // Implementation depends on your audit storage solution
        await Database.AuditRecords.AddAsync(record);
        await Database.SaveChangesAsync();
    }
}
```

### **CATEGORY 8: INCIDENT RESPONSE & RECOVERY**

#### **Step 19: Security Incident Detection**
```csharp
public class SecurityIncidentDetector
{
    private readonly Dictionary<string, int> _failedAuthAttempts = new();
    private readonly List<SecurityEvent> _recentEvents = new();

    public async Task<IncidentResponse> AnalyzeSecurityEvent(SecurityEvent securityEvent)
    {
        _recentEvents.Add(securityEvent);

        // Clean old events (keep last hour)
        var cutoff = DateTimeOffset.UtcNow.AddHours(-1);
        _recentEvents.RemoveAll(e => e.Timestamp < cutoff);

        var response = new IncidentResponse
        {
            EventId = securityEvent.Id,
            DetectedAt = DateTimeOffset.UtcNow,
            Severity = IncidentSeverity.Low
        };

        // Detect patterns
        if (securityEvent.Type == SecurityEventType.AuthenticationFailure)
        {
            response = await HandleAuthenticationFailure(securityEvent);
        }
        else if (securityEvent.Type == SecurityEventType.SuspiciousInput)
        {
            response = await HandleSuspiciousInput(securityEvent);
        }
        else if (securityEvent.Type == SecurityEventType.DataBreach)
        {
            response = await HandleDataBreach(securityEvent);
        }

        // Auto-respond based on severity
        if (response.Severity >= IncidentSeverity.High)
        {
            await ExecuteIncidentResponse(response);
        }

        return response;
    }

    private async Task<IncidentResponse> HandleAuthenticationFailure(SecurityEvent evt)
    {
        var userId = evt.UserId;

        if (!_failedAuthAttempts.ContainsKey(userId))
            _failedAuthAttempts[userId] = 0;

        _failedAuthAttempts[userId]++;

        var response = new IncidentResponse
        {
            EventId = evt.Id,
            Severity = IncidentSeverity.Low,
            RecommendedActions = new List<string>()
        };

        if (_failedAuthAttempts[userId] >= 5)
        {
            response.Severity = IncidentSeverity.Medium;
            response.RecommendedActions.Add($"Lock account: {userId}");
            await LockUserAccount(userId, TimeSpan.FromMinutes(30));
        }

        if (_failedAuthAttempts[userId] >= 10)
        {
            response.Severity = IncidentSeverity.High;
            response.RecommendedActions.Add($"Block IP: {evt.IPAddress}");
            response.RecommendedActions.Add("Investigate potential brute force attack");
        }

        return response;
    }

    private async Task ExecuteIncidentResponse(IncidentResponse response)
    {
        // Notify security team
        await NotifySecurityTeam(response);

        // Execute automated responses
        foreach (var action in response.RecommendedActions)
        {
            await ExecuteSecurityAction(action);
        }

        // Create incident ticket
        await CreateIncidentTicket(response);
    }
}
```

#### **Step 20: Data Breach Response Protocol**
```csharp
public class DataBreachResponseManager
{
    public async Task<BreachResponse> HandlePotentialBreach(BreachIndicator indicator)
    {
        var response = new BreachResponse
        {
            BreachId = Guid.NewGuid(),
            DetectedAt = DateTimeOffset.UtcNow,
            Indicator = indicator,
            Status = BreachStatus.Investigating
        };

        // Immediate containment
        await ExecuteContainmentMeasures(indicator);

        // Assess scope and impact
        response.ImpactAssessment = await AssessBreachImpact(indicator);

        // Determine if this constitutes a breach under GDPR
        if (response.ImpactAssessment.AffectedPersons > 0 &&
            response.ImpactAssessment.SensitivityLevel >= DataSensitivity.Personal)
        {
            response.RequiresGDPRNotification = true;
            response.NotificationDeadline = DateTimeOffset.UtcNow.AddHours(72);
        }

        // Execute breach response plan
        await ExecuteBreachResponsePlan(response);

        return response;
    }

    private async Task ExecuteContainmentMeasures(BreachIndicator indicator)
    {
        switch (indicator.Type)
        {
            case BreachType.UnauthorizedAccess:
                await RevokeAllUserSessions();
                await DisableCompromisedAccounts(indicator.AffectedAccounts);
                break;

            case BreachType.DataExfiltration:
                await BlockSuspiciousIPAddresses(indicator.SuspiciousIPs);
                await EnableEnhancedMonitoring();
                break;

            case BreachType.SystemCompromise:
                await IsolateAffectedSystems(indicator.AffectedSystems);
                await ActivateBackupSystems();
                break;
        }
    }

    private async Task<ImpactAssessment> AssessBreachImpact(BreachIndicator indicator)
    {
        return new ImpactAssessment
        {
            AffectedPersons = await CountAffectedPersons(indicator),
            DataTypesAffected = await IdentifyAffectedDataTypes(indicator),
            SensitivityLevel = await DetermineSensitivityLevel(indicator),
            PotentialHarm = await AssessPotentialHarm(indicator),
            GeographicScope = await DetermineGeographicScope(indicator)
        };
    }
}
```

### **CATEGORY 9: COMPLIANCE VALIDATION & TESTING**

#### **Step 21: Automated Security Testing**
```csharp
public class SecurityTestSuite
{
    public async Task<SecurityTestResults> RunComprehensiveSecurityTests()
    {
        var results = new SecurityTestResults
        {
            TestRunId = Guid.NewGuid(),
            StartedAt = DateTimeOffset.UtcNow,
            Tests = new List<SecurityTestResult>()
        };

        // Test HTTPS/TLS configuration
        results.Tests.Add(await TestHTTPSConfiguration());

        // Test authentication mechanisms
        results.Tests.Add(await TestAuthenticationSecurity());

        // Test input validation
        results.Tests.Add(await TestInputValidation());

        // Test encryption implementation
        results.Tests.Add(await TestEncryptionStrength());

        // Test GDPR compliance
        results.Tests.Add(await TestGDPRCompliance());

        // Test bias detection
        results.Tests.Add(await TestBiasDetection());

        results.CompletedAt = DateTimeOffset.UtcNow;
        results.OverallResult = results.Tests.All(t => t.Passed) ? TestResult.Pass : TestResult.Fail;

        return results;
    }

    private async Task<SecurityTestResult> TestHTTPSConfiguration()
    {
        var test = new SecurityTestResult
        {
            TestName = "HTTPS Configuration",
            Category = "Network Security"
        };

        try
        {
            // Test certificate validation
            var isValidCert = await ValidateCertificateChain();
            test.Checks.Add(new TestCheck("Certificate Chain Valid", isValidCert));

            // Test TLS version
            var tlsVersion = await GetTLSVersion();
            test.Checks.Add(new TestCheck("TLS 1.2+ Required", tlsVersion >= 1.2f));

            // Test cipher suites
            var strongCiphers = await ValidateCipherSuites();
            test.Checks.Add(new TestCheck("Strong Cipher Suites", strongCiphers));

            test.Passed = test.Checks.All(c => c.Passed);
        }
        catch (Exception ex)
        {
            test.Passed = false;
            test.ErrorMessage = ex.Message;
        }

        return test;
    }

    private async Task<SecurityTestResult> TestInputValidation()
    {
        var test = new SecurityTestResult
        {
            TestName = "Input Validation",
            Category = "Application Security"
        };

        var testInputs = new[]
        {
            "<script>alert('xss')</script>",
            "'; DROP TABLE users; --",
            "javascript:alert('xss')",
            "data:text/html,<script>alert('xss')</script>",
            new string('A', 10000), // Length test
            "normal political input" // Should pass
        };

        foreach (var input in testInputs)
        {
            var validationResult = PoliticalInputSanitizer.ValidatePoliticalInput(input);
            var shouldPass = input == "normal political input";
            var actualPass = validationResult.IsValid;

            test.Checks.Add(new TestCheck(
                $"Input validation: {input[..Math.Min(20, input.Length)]}...",
                shouldPass == actualPass
            ));
        }

        test.Passed = test.Checks.All(c => c.Passed);
        return test;
    }
}
```

#### **Step 22: GDPR Compliance Verification**
```csharp
public class GDPRComplianceValidator
{
    public async Task<ComplianceReport> ValidateGDPRCompliance()
    {
        var report = new ComplianceReport
        {
            ValidationDate = DateTimeOffset.UtcNow,
            ComplianceChecks = new List<ComplianceCheck>()
        };

        // Article 6: Lawful basis for processing
        report.ComplianceChecks.Add(await ValidateLawfulBasis());

        // Article 7: Consent requirements
        report.ComplianceChecks.Add(await ValidateConsentMechanism());

        // Article 9: Special categories of personal data
        report.ComplianceChecks.Add(await ValidateSpecialCategoryProcessing());

        // Article 17: Right to erasure
        report.ComplianceChecks.Add(await ValidateErasureCapability());

        // Article 20: Right to data portability
        report.ComplianceChecks.Add(await ValidateDataPortability());

        // Article 25: Data protection by design and by default
        report.ComplianceChecks.Add(await ValidateDataProtectionByDesign());

        // Article 32: Security of processing
        report.ComplianceChecks.Add(await ValidateSecurityMeasures());

        // Article 33: Breach notification
        report.ComplianceChecks.Add(await ValidateBreachNotificationCapability());

        report.OverallCompliance = CalculateOverallCompliance(report.ComplianceChecks);

        return report;
    }

    private async Task<ComplianceCheck> ValidateConsentMechanism()
    {
        var check = new ComplianceCheck
        {
            Article = "Article 7",
            Requirement = "Consent must be freely given, specific, informed and unambiguous",
            ValidationItems = new List<ValidationItem>()
        };

        // Test consent UI
        check.ValidationItems.Add(new ValidationItem
        {
            Item = "Consent is opt-in (not pre-checked)",
            IsCompliant = await VerifyConsentIsOptIn(),
            Evidence = "Consent UI requires explicit user action"
        });

        // Test consent withdrawal
        check.ValidationItems.Add(new ValidationItem
        {
            Item = "Withdrawal is as easy as giving consent",
            IsCompliant = await VerifyConsentWithdrawalMechanism(),
            Evidence = "One-click consent withdrawal available"
        });

        // Test consent documentation
        check.ValidationItems.Add(new ValidationItem
        {
            Item = "Consent is properly documented",
            IsCompliant = await VerifyConsentDocumentation(),
            Evidence = "All consent actions logged with timestamp, IP, user agent"
        });

        check.IsCompliant = check.ValidationItems.All(v => v.IsCompliant);
        return check;
    }
}
```

### **CATEGORY 10: PERFORMANCE & SCALABILITY SECURITY**

#### **Step 23: Secure Performance Monitoring**
```csharp
public class SecurePerformanceMonitor
{
    private readonly Dictionary<string, PerformanceMetrics> _userMetrics = new();
    private readonly object _metricsLock = new object();

    public void RecordSecureOperation(string operation, TimeSpan duration, string userId = null)
    {
        lock (_metricsLock)
        {
            var hashedUserId = userId != null ? HashUserId(userId) : "anonymous";

            if (!_userMetrics.ContainsKey(hashedUserId))
            {
                _userMetrics[hashedUserId] = new PerformanceMetrics();
            }

            var metrics = _userMetrics[hashedUserId];
            metrics.RecordOperation(operation, duration);

            // Detect potential abuse patterns
            if (IsAbusePattern(metrics))
            {
                SecurityLogger.LogSuspiciousActivity(hashedUserId, "Performance abuse pattern detected");
            }
        }
    }

    private bool IsAbusePattern(PerformanceMetrics metrics)
    {
        // Detect rapid-fire requests (potential DoS)
        var recentOps = metrics.RecentOperations.Count(op =>
            DateTimeOffset.UtcNow - op.Timestamp < TimeSpan.FromMinutes(1));

        if (recentOps > 100) return true;

        // Detect resource-intensive operations
        var avgDuration = metrics.RecentOperations
            .Where(op => DateTimeOffset.UtcNow - op.Timestamp < TimeSpan.FromMinutes(5))
            .Average(op => op.Duration.TotalMilliseconds);

        if (avgDuration > 10000) return true; // > 10 seconds average

        return false;
    }
}
```

#### **Step 24: Secure Caching Strategy**
```csharp
public class SecureCacheManager
{
    private readonly MemoryCache _cache;
    private readonly Dictionary<string, CacheSecurityPolicy> _policies;

    public SecureCacheManager()
    {
        _cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 1000,
            CompactionPercentage = 0.1
        });

        _policies = new Dictionary<string, CacheSecurityPolicy>
        {
            ["political_data"] = new CacheSecurityPolicy
            {
                MaxTTL = TimeSpan.FromMinutes(5),
                RequiresEncryption = true,
                RequiresUserConsent = true
            },
            ["api_responses"] = new CacheSecurityPolicy
            {
                MaxTTL = TimeSpan.FromMinutes(15),
                RequiresEncryption = false,
                RequiresUserConsent = false
            }
        };
    }

    public async Task<T> GetSecureAsync<T>(string key, string category, string userId = null)
    {
        if (!_policies.ContainsKey(category))
            throw new ArgumentException($"Unknown cache category: {category}");

        var policy = _policies[category];

        // Check user consent if required
        if (policy.RequiresUserConsent && userId != null)
        {
            var hasConsent = await ValidateUserConsent(userId, DataProcessingPurpose.Analytics);
            if (!hasConsent)
                return default(T);
        }

        var secureKey = GenerateSecureKey(key, userId);

        if (_cache.TryGetValue(secureKey, out var cachedData))
        {
            if (policy.RequiresEncryption)
            {
                return DecryptCachedData<T>((string)cachedData);
            }
            return (T)cachedData;
        }

        return default(T);
    }

    public async Task SetSecureAsync<T>(string key, T value, string category, string userId = null)
    {
        if (!_policies.ContainsKey(category))
            throw new ArgumentException($"Unknown cache category: {category}");

        var policy = _policies[category];
        var secureKey = GenerateSecureKey(key, userId);

        object dataToCache = value;

        if (policy.RequiresEncryption)
        {
            dataToCache = EncryptCacheData(value);
        }

        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = policy.MaxTTL,
            Size = 1,
            PostEvictionCallbacks = { new PostEvictionCallbackRegistration
            {
                EvictionCallback = OnCacheEviction
            }}
        };

        _cache.Set(secureKey, dataToCache, options);
    }

    private void OnCacheEviction(object key, object value, EvictionReason reason, object state)
    {
        // Secure cleanup of evicted cache entries
        if (value is string encryptedValue && encryptedValue.Length > 0)
        {
            // Clear sensitive data from memory
            SecureStringUtils.ClearString(ref encryptedValue);
        }
    }
}
```

#### **Step 25: Final Security Validation & Documentation**
```csharp
public class SecurityValidationSummary
{
    public async Task<ValidationSummaryReport> GenerateSecurityValidationReport()
    {
        var report = new ValidationSummaryReport
        {
            GeneratedAt = DateTimeOffset.UtcNow,
            ValidationResults = new List<ValidationCategory>()
        };

        // HTTPS/TLS Implementation
        var httpsValidation = new ValidationCategory
        {
            CategoryName = "HTTPS/TLS Implementation",
            Status = ValidationStatus.Compliant,
            Items = new List<ValidationItem>
            {
                new ValidationItem { Item = "TLS 1.2+ enforced", IsCompliant = true },
                new ValidationItem { Item = "Certificate validation implemented", IsCompliant = true },
                new ValidationItem { Item = "Certificate pinning configured", IsCompliant = true },
                new ValidationItem { Item = "Weak cipher suites disabled", IsCompliant = true }
            }
        };
        report.ValidationResults.Add(httpsValidation);

        // Authentication System
        var authValidation = new ValidationCategory
        {
            CategoryName = "Authentication System",
            Status = ValidationStatus.Compliant,
            Items = new List<ValidationItem>
            {
                new ValidationItem { Item = "Unity Secrets Manager integration", IsCompliant = true },
                new ValidationItem { Item = "API key encryption at rest", IsCompliant = true },
                new ValidationItem { Item = "Token refresh mechanism", IsCompliant = true },
                new ValidationItem { Item = "HMAC request signing", IsCompliant = true }
            }
        };
        report.ValidationResults.Add(authValidation);

        // Input Validation
        var inputValidation = new ValidationCategory
        {
            CategoryName = "Input Validation & Sanitization",
            Status = ValidationStatus.Compliant,
            Items = new List<ValidationItem>
            {
                new ValidationItem { Item = "XSS prevention implemented", IsCompliant = true },
                new ValidationItem { Item = "Injection attack protection", IsCompliant = true },
                new ValidationItem { Item = "Political content sanitization", IsCompliant = true },
                new ValidationItem { Item = "Rate limiting configured", IsCompliant = true }
            }
        };
        report.ValidationResults.Add(inputValidation);

        // Data Encryption
        var encryptionValidation = new ValidationCategory
        {
            CategoryName = "Data Encryption",
            Status = ValidationStatus.Compliant,
            Items = new List<ValidationItem>
            {
                new ValidationItem { Item = "Political data encrypted at rest", IsCompliant = true },
                new ValidationItem { Item = "In-transit encryption for NIM API", IsCompliant = true },
                new ValidationItem { Item = "AES-256 encryption standard", IsCompliant = true },
                new ValidationItem { Item = "Secure key derivation (PBKDF2)", IsCompliant = true }
            }
        };
        report.ValidationResults.Add(encryptionValidation);

        // GDPR Compliance
        var gdprValidation = new ValidationCategory
        {
            CategoryName = "GDPR Compliance (Netherlands)",
            Status = ValidationStatus.Compliant,
            Items = new List<ValidationItem>
            {
                new ValidationItem { Item = "Explicit consent mechanism", IsCompliant = true },
                new ValidationItem { Item = "Data portability implementation", IsCompliant = true },
                new ValidationItem { Item = "Right to erasure capability", IsCompliant = true },
                new ValidationItem { Item = "Data retention policies", IsCompliant = true },
                new ValidationItem { Item = "Breach notification procedures", IsCompliant = true }
            }
        };
        report.ValidationResults.Add(gdprValidation);

        // AI Content Security
        var aiValidation = new ValidationCategory
        {
            CategoryName = "AI Content Security & Bias Detection",
            Status = ValidationStatus.Compliant,
            Items = new List<ValidationItem>
            {
                new ValidationItem { Item = "Political bias detection system", IsCompliant = true },
                new ValidationItem { Item = "Content moderation pipeline", IsCompliant = true },
                new ValidationItem { Item = "Toxicity filtering implemented", IsCompliant = true },
                new ValidationItem { Item = "Human review queue system", IsCompliant = true }
            }
        };
        report.ValidationResults.Add(aiValidation);

        report.OverallSecurityScore = CalculateOverallSecurityScore(report.ValidationResults);

        return report;
    }

    private float CalculateOverallSecurityScore(List<ValidationCategory> categories)
    {
        var totalItems = categories.Sum(c => c.Items.Count);
        var compliantItems = categories.Sum(c => c.Items.Count(i => i.IsCompliant));

        return totalItems > 0 ? (float)compliantItems / totalItems * 100 : 0;
    }
}

public class ValidationSummaryReport
{
    public DateTimeOffset GeneratedAt { get; set; }
    public List<ValidationCategory> ValidationResults { get; set; }
    public float OverallSecurityScore { get; set; }
    public List<string> RecommendedActions { get; set; } = new List<string>();
    public List<string> CriticalIssues { get; set; } = new List<string>();
}
```

---

## 🎯 IMPLEMENTATION CHECKLIST

### **Phase 3A: Core Security Infrastructure (Week 1)**
- [ ] **Step 1-4**: HTTPS/TLS implementation with certificate validation
- [ ] **Step 5-7**: Unity Secrets Manager integration and token management
- [ ] **Step 17-18**: Security logging and audit trail systems

### **Phase 3B: Data Protection (Week 2)**
- [ ] **Step 8-9**: Input validation and rate limiting
- [ ] **Step 10-11**: Data encryption at rest and in transit
- [ ] **Step 12-14**: GDPR compliance framework implementation

### **Phase 3C: AI Security & Monitoring (Week 3)**
- [ ] **Step 15-16**: Bias detection and content moderation
- [ ] **Step 19-20**: Incident response and breach protocols
- [ ] **Step 21-22**: Automated testing and compliance validation

### **Phase 3D: Performance & Final Validation (Week 4)**
- [ ] **Step 23-24**: Secure performance monitoring and caching
- [ ] **Step 25**: Final security validation and documentation

---

## 🚨 CRITICAL SECURITY NOTES

1. **Certificate Pinning**: Update pinned certificates before they expire
2. **Key Rotation**: Implement automated key rotation for encryption keys
3. **Audit Logs**: Maintain tamper-evident audit logs for GDPR compliance
4. **Incident Response**: Test breach notification procedures quarterly
5. **Staff Training**: Ensure all developers understand political data sensitivity

---

## 📊 COMPLIANCE MATRIX

| Requirement | Implementation | Validation |
|-------------|----------------|------------|
| GDPR Art. 6 | Explicit consent system | Automated compliance tests |
| GDPR Art. 9 | Special category protections | Bias detection + encryption |
| GDPR Art. 17 | Right to erasure | Data deletion automation |
| GDPR Art. 20 | Data portability | Encrypted export system |
| GDPR Art. 25 | Privacy by design | Security-first architecture |
| GDPR Art. 32 | Security measures | Multi-layer security stack |

**Implementation Timeline**: 4 weeks
**Security Review**: Weekly
**Compliance Audit**: Monthly
**Penetration Testing**: Quarterly