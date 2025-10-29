using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages content filtering for player names and text responses.
/// Filters banned words, profanity, and checks for duplicate answers.
/// </summary>
public class ContentFilterManager : MonoBehaviour
{
    public static ContentFilterManager Instance;

    [Header("Banned Words Settings")]
    public string bannedWordsFilePath = "bannedwords"; // In Resources folder
    private HashSet<string> bannedWords = new HashSet<string>();

    [Header("Filter Settings")]
    public bool enableProfanityFilter = true;
    public bool caseSensitive = false;
    public string replacementCharacter = "*";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadBannedWords();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // === BANNED WORDS LOADING ===

    void LoadBannedWords()
    {
        bannedWords.Clear();

        // Try to load from Resources
        TextAsset bannedWordsFile = Resources.Load<TextAsset>(bannedWordsFilePath);

        if (bannedWordsFile != null)
        {
            // Parse file - each line is a banned word
            string[] lines = bannedWordsFile.text.Split('\n');

            foreach (string line in lines)
            {
                string word = line.Trim();
                if (!string.IsNullOrEmpty(word) && !word.StartsWith("#")) // Skip comments
                {
                    bannedWords.Add(caseSensitive ? word : word.ToLower());
                }
            }

            Debug.Log($"ContentFilterManager: Loaded {bannedWords.Count} banned words");
        }
        else
        {
            Debug.LogWarning("ContentFilterManager: Could not load banned words file from Resources/" + bannedWordsFilePath);
            LoadDefaultBannedWords();
        }
    }

    void LoadDefaultBannedWords()
    {
        // Basic profanity list as fallback
        string[] defaultBannedWords = new string[]
        {
            "fuck", "shit", "damn", "ass", "bitch", "bastard", "cunt", "dick",
            "pussy", "cock", "nigger", "nigga", "fag", "faggot", "retard",
            // Add more as needed
        };

        foreach (string word in defaultBannedWords)
        {
            bannedWords.Add(caseSensitive ? word : word.ToLower());
        }

        Debug.Log($"ContentFilterManager: Loaded {bannedWords.Count} default banned words");
    }

    // === CONTENT VALIDATION ===

    /// <summary>
    /// Check if text contains any banned words
    /// </summary>
    public bool ContainsBannedWords(string text)
    {
        if (!enableProfanityFilter || string.IsNullOrEmpty(text))
        {
            return false;
        }

        string textToCheck = caseSensitive ? text : text.ToLower();

        // Split text into words
        string[] words = textToCheck.Split(new char[] { ' ', '\t', '\n', '\r', '.', ',', '!', '?', ';', ':' },
            System.StringSplitOptions.RemoveEmptyEntries);

        // Check each word
        foreach (string word in words)
        {
            if (bannedWords.Contains(word))
            {
                return true;
            }
        }

        // Also check for banned words as substrings (catches "f*ck" as "fck" etc)
        foreach (string bannedWord in bannedWords)
        {
            if (textToCheck.Contains(bannedWord))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Filter out banned words from text by replacing with asterisks
    /// </summary>
    public string FilterBannedWords(string text)
    {
        if (!enableProfanityFilter || string.IsNullOrEmpty(text))
        {
            return text;
        }

        string filteredText = text;
        string textToCheck = caseSensitive ? text : text.ToLower();

        foreach (string bannedWord in bannedWords)
        {
            if (textToCheck.Contains(bannedWord))
            {
                // Create replacement string of equal length
                string replacement = new string(replacementCharacter[0], bannedWord.Length);

                // Replace with case-insensitive regex if needed
                if (caseSensitive)
                {
                    filteredText = filteredText.Replace(bannedWord, replacement);
                }
                else
                {
                    // Case-insensitive replacement
                    int index = 0;
                    while ((index = textToCheck.IndexOf(bannedWord, index)) != -1)
                    {
                        filteredText = filteredText.Substring(0, index) + replacement +
                                      filteredText.Substring(index + bannedWord.Length);
                        textToCheck = filteredText.ToLower();
                        index += replacement.Length;
                    }
                }
            }
        }

        return filteredText;
    }

    /// <summary>
    /// Validate player name (length, characters, banned words)
    /// </summary>
    public ValidationResult ValidatePlayerName(string name)
    {
        ValidationResult result = new ValidationResult();

        // Check empty
        if (string.IsNullOrWhiteSpace(name))
        {
            result.isValid = false;
            result.errorMessage = "Name cannot be empty";
            return result;
        }

        // Check length
        if (name.Length < 2)
        {
            result.isValid = false;
            result.errorMessage = "Name must be at least 2 characters";
            return result;
        }

        if (name.Length > 20)
        {
            result.isValid = false;
            result.errorMessage = "Name must be 20 characters or less";
            return result;
        }

        // Check for banned words
        if (ContainsBannedWords(name))
        {
            result.isValid = false;
            result.errorMessage = "Name contains inappropriate language";
            return result;
        }

        result.isValid = true;
        result.sanitizedText = name.Trim();
        return result;
    }

    /// <summary>
    /// Validate text answer (length, banned words, duplicates)
    /// </summary>
    public ValidationResult ValidateAnswer(string answer, List<string> existingAnswers = null)
    {
        ValidationResult result = new ValidationResult();

        // Check empty
        if (string.IsNullOrWhiteSpace(answer))
        {
            result.isValid = false;
            result.errorMessage = "Answer cannot be empty";
            return result;
        }

        // Trim and normalize
        string trimmedAnswer = answer.Trim();

        // Check length
        if (trimmedAnswer.Length < 1)
        {
            result.isValid = false;
            result.errorMessage = "Answer is too short";
            return result;
        }

        if (trimmedAnswer.Length > 100)
        {
            result.isValid = false;
            result.errorMessage = "Answer must be 100 characters or less";
            return result;
        }

        // Check for banned words
        if (ContainsBannedWords(trimmedAnswer))
        {
            result.isValid = false;
            result.errorMessage = "Answer contains inappropriate language";
            return result;
        }

        // Check for duplicates (case-insensitive)
        if (existingAnswers != null && existingAnswers.Count > 0)
        {
            foreach (string existing in existingAnswers)
            {
                if (string.Equals(trimmedAnswer, existing, System.StringComparison.OrdinalIgnoreCase))
                {
                    result.isValid = false;
                    result.errorMessage = "This answer has already been submitted";
                    result.isDuplicate = true;
                    return result;
                }
            }
        }

        result.isValid = true;
        result.sanitizedText = trimmedAnswer;
        return result;
    }

    // === UTILITY ===

    public void AddBannedWord(string word)
    {
        if (!string.IsNullOrEmpty(word))
        {
            bannedWords.Add(caseSensitive ? word : word.ToLower());
        }
    }

    public void RemoveBannedWord(string word)
    {
        if (!string.IsNullOrEmpty(word))
        {
            bannedWords.Remove(caseSensitive ? word : word.ToLower());
        }
    }

    public bool IsBannedWord(string word)
    {
        if (string.IsNullOrEmpty(word))
        {
            return false;
        }

        return bannedWords.Contains(caseSensitive ? word : word.ToLower());
    }
}

// === DATA STRUCTURES ===

[System.Serializable]
public class ValidationResult
{
    public bool isValid = true;
    public string errorMessage = "";
    public string sanitizedText = "";
    public bool isDuplicate = false;
}
