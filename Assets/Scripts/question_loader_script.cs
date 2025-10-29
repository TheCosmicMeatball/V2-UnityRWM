using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Netcode;

public class QuestionLoader : MonoBehaviour
{
    private GameManager gameManager;
    [Header("Question File Paths")]
    public string standardQuestionsPath = "questions";
    public string playerQuestionsPath = "playerqs";
    public string pictureQuestionsPath = "picqs";
    public string bonusQuestionsPath = "bonusquestions";

    [Header("Bonus Question Settings")]
    [SerializeField]
    private int bonusQuestionsPerRound = 4;

    private bool hasLoadedQuestions = false;
    private bool subscribedToShuffleSeed = false;

    void Awake()
    {
        gameManager = GameManager.Instance;

        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        if (gameManager == null)
        {
            Debug.LogError("QuestionLoader could not find an active GameManager. Questions will not be loaded.");
            return;
        }

        TryLoadQuestionsWithSeed();
    }

    void TryLoadQuestionsWithSeed()
    {
        if (hasLoadedQuestions)
        {
            return;
        }

        bool networkAvailable = NetworkManager.Singleton != null;
        bool isServer = networkAvailable && NetworkManager.Singleton.IsServer;
        bool isClientOnly = networkAvailable && NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer;

        if (isClientOnly)
        {
            int seedFromServer = gameManager.questionShuffleSeed.Value;

            if (seedFromServer == 0)
            {
                Debug.Log("[QuestionLoader] Waiting for server shuffle seed before loading questions.");
                if (!subscribedToShuffleSeed)
                {
                    gameManager.questionShuffleSeed.OnValueChanged += OnShuffleSeedChanged;
                    subscribedToShuffleSeed = true;
                }
                return;
            }

            LoadAllQuestionsWithSeed(seedFromServer);
            return;
        }

        int seed = GenerateSeedForLocalSession(isServer);
        LoadAllQuestionsWithSeed(seed);
    }

    int GenerateSeedForLocalSession(bool isServer)
    {
        int seed = Environment.TickCount;

        if (isServer && gameManager != null)
        {
            seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            if (seed == 0)
            {
                seed = 1;
            }

            gameManager.questionShuffleSeed.Value = seed;
        }
        else if (seed == 0)
        {
            seed = 1;
        }

        return seed;
    }

    void LoadAllQuestionsWithSeed(int seed)
    {
        if (hasLoadedQuestions)
        {
            return;
        }

        hasLoadedQuestions = true;

        ShuffleUtility.SeedRandom(seed);
        Debug.Log($"[QuestionLoader] Loading questions using shuffle seed {seed}");

        // Load standard questions
        LoadStandardQuestions();

        // Load player questions
        LoadPlayerQuestions();

        // Load picture questions
        LoadPictureQuestions();

        // Load bonus questions
        LoadBonusQuestions();

        Debug.Log("All questions loaded and shuffled successfully");

        if (subscribedToShuffleSeed)
        {
            gameManager.questionShuffleSeed.OnValueChanged -= OnShuffleSeedChanged;
            subscribedToShuffleSeed = false;
        }
    }

    void OnShuffleSeedChanged(int previousValue, int newValue)
    {
        if (hasLoadedQuestions || newValue == 0)
        {
            return;
        }

        LoadAllQuestionsWithSeed(newValue);
    }

    void OnDestroy()
    {
        if (subscribedToShuffleSeed && gameManager != null)
        {
            gameManager.questionShuffleSeed.OnValueChanged -= OnShuffleSeedChanged;
            subscribedToShuffleSeed = false;
        }
    }

    void LoadStandardQuestions()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(standardQuestionsPath);
        
        if (jsonFile == null)
        {
            Debug.LogError("Could not find questions.json at Resources/" + standardQuestionsPath);
            return;
        }
        
        // Parse the array format: [{"QUESTION": ..., "CORRECT ANSWER": ..., "ROBOT ANSWER": ...}]
        QuestionArrayWrapper wrapper = new QuestionArrayWrapper();
        wrapper.questions = ParseQuestionArray(jsonFile.text);
        
        if (wrapper.questions != null && wrapper.questions.Count > 0)
        {
            // Shuffle the questions to ensure varied playthrough order
            ShuffleUtility.Shuffle(wrapper.questions);
            gameManager.standardQuestions = wrapper.questions;
            Debug.Log("Loaded and shuffled " + wrapper.questions.Count + " standard questions");

            // Log first 3 questions for verification (helps confirm shuffle is working)
            if (wrapper.questions.Count >= 3)
            {
                Debug.Log($"  First 3 questions: 1) {wrapper.questions[0].questionText.Substring(0, System.Math.Min(50, wrapper.questions[0].questionText.Length))}... " +
                         $"2) {wrapper.questions[1].questionText.Substring(0, System.Math.Min(50, wrapper.questions[1].questionText.Length))}... " +
                         $"3) {wrapper.questions[2].questionText.Substring(0, System.Math.Min(50, wrapper.questions[2].questionText.Length))}...");
            }
        }
        else
        {
            Debug.LogError("Failed to parse standard questions JSON");
        }
    }
    
    void LoadPlayerQuestions()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(playerQuestionsPath);

        if (jsonFile == null)
        {
            Debug.LogError("Could not find playerqs.json at Resources/" + playerQuestionsPath);
            return;
        }

        QuestionArrayWrapper wrapper = new QuestionArrayWrapper();
        wrapper.questions = ParsePlayerQuestionArray(jsonFile.text);

        if (wrapper.questions != null && wrapper.questions.Count > 0)
        {
            // Shuffle the questions to ensure varied playthrough order
            ShuffleUtility.Shuffle(wrapper.questions);
            gameManager.playerQuestions = wrapper.questions;
            Debug.Log("Loaded and shuffled " + wrapper.questions.Count + " player questions");
        }
        else
        {
            Debug.LogError("Failed to parse player questions JSON");
        }
    }
    
    void LoadPictureQuestions()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(pictureQuestionsPath);

        if (jsonFile == null)
        {
            Debug.LogError("Could not find picqs.json at Resources/" + pictureQuestionsPath);
            return;
        }

        List<Question> pictureQuestionList = ParsePictureQuestionArray(jsonFile.text);

        if (pictureQuestionList != null && pictureQuestionList.Count > 0)
        {
            // Shuffle the questions to ensure varied playthrough order
            ShuffleUtility.Shuffle(pictureQuestionList);
            gameManager.pictureQuestions = pictureQuestionList;
            Debug.Log("Loaded and shuffled " + pictureQuestionList.Count + " picture questions");
        }
        else
        {
            Debug.LogError("Failed to parse picture questions JSON");
        }
    }
    
    void LoadBonusQuestions()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(bonusQuestionsPath);

        if (jsonFile == null)
        {
            Debug.LogError("Could not find bonusquestions.json at Resources/" + bonusQuestionsPath);
            return;
        }

        // Wrap the array in an object: {"bonusQuestions": [...]}
        string wrappedJson = "{\"bonusQuestions\":" + jsonFile.text + "}";

        RawBonusQuestionArrayWrapper wrapper = JsonUtility.FromJson<RawBonusQuestionArrayWrapper>(wrappedJson);

        if (wrapper != null && wrapper.bonusQuestions != null)
        {
            // Convert to BonusQuestion format
            BonusQuestion bonusData = new BonusQuestion();
            bonusData.miniQuestions = new List<string>();

            foreach (RawBonusQuestionData raw in wrapper.bonusQuestions)
            {
                bonusData.miniQuestions.Add(raw.question);
            }

            // Shuffle the bonus questions to ensure varied playthrough order
            ShuffleUtility.Shuffle(bonusData.miniQuestions);

            int originalCount = bonusData.miniQuestions.Count;
            int desiredCount = Mathf.Clamp(bonusQuestionsPerRound, 0, originalCount);
            if (desiredCount < originalCount)
            {
                bonusData.miniQuestions = bonusData.miniQuestions.GetRange(0, desiredCount);
            }

            gameManager.bonusQuestions = bonusData;
            string logMessage = "Loaded and shuffled " + originalCount + " bonus questions";
            if (desiredCount < originalCount)
            {
                logMessage += ", trimmed to " + bonusData.miniQuestions.Count + " for this round";
            }
            Debug.Log(logMessage);
        }
        else
        {
            Debug.LogError("Failed to parse bonus questions JSON");
        }
    }
    
    List<Question> ParseQuestionArray(string json)
    {
        // The JSON is an array like: [{"QUESTION": ..., "CORRECT ANSWER": ..., "ROBOT ANSWER": ...}, ...]
        // Wrap it in an object so Unity's JsonUtility can parse it

        List<Question> questions = new List<Question>();

        try
        {
            // Wrap the array in a JSON object: {"questions": [...]}
            string wrappedJson = "{\"questions\":" + json + "}";

            // Parse using wrapper class
            RawQuestionArrayWrapper wrapper = JsonUtility.FromJson<RawQuestionArrayWrapper>(wrappedJson);

            if (wrapper != null && wrapper.questions != null)
            {
                foreach (RawQuestionData raw in wrapper.questions)
                {
                    Question q = new Question();
                    q.questionText = raw.QUESTION;
                    q.correctAnswer = raw.CORRECT_ANSWER;
                    q.robotAnswer = raw.ROBOT_ANSWER;
                    q.robotAnecdote = ""; // Not in current data structure
                    q.questionType = "standard";
                    q.imageURL = ""; // For picture questions, would be added

                    questions.Add(q);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error parsing question array: " + e.Message);
        }

        return questions;
    }

    List<Question> ParsePlayerQuestionArray(string json)
    {
        // Player questions use different field names: "Question", "Right Answer", "Robot Answer"
        // Unity's JsonUtility doesn't handle spaces in field names, so we'll replace them
        List<Question> questions = new List<Question>();

        try
        {
            // Replace field names with spaces to use underscores
            string fixedJson = json.Replace("\"Right Answer\"", "\"Right_Answer\"");
            fixedJson = fixedJson.Replace("\"Robot Answer\"", "\"Robot_Answer\"");

            // Wrap the array in a JSON object: {"questions": [...]}
            string wrappedJson = "{\"questions\":" + fixedJson + "}";

            // Parse using player question wrapper class
            RawPlayerQuestionArrayWrapper wrapper = JsonUtility.FromJson<RawPlayerQuestionArrayWrapper>(wrappedJson);

            if (wrapper != null && wrapper.questions != null)
            {
                foreach (RawPlayerQuestionData raw in wrapper.questions)
                {
                    Question q = new Question();
                    q.questionText = raw.Question;
                    q.correctAnswer = raw.Right_Answer;
                    q.robotAnswer = raw.Robot_Answer;
                    q.robotAnecdote = ""; // Not in current data structure
                    q.questionType = "player";
                    q.imageURL = "";

                    questions.Add(q);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error parsing player question array: " + e.Message);
        }

        return questions;
    }

    List<Question> ParsePictureQuestionArray(string json)
    {
        List<Question> questions = new List<Question>();

        try
        {
            string wrappedJson = "{\"questions\":" + json + "}"; // Wrap array for JsonUtility

            RawPictureQuestionArrayWrapper wrapper = JsonUtility.FromJson<RawPictureQuestionArrayWrapper>(wrappedJson);

            if (wrapper != null && wrapper.questions != null)
            {
                foreach (RawPictureQuestionData raw in wrapper.questions)
                {
                    Question q = new Question();

                    // The "question" field in picqs.json represents the image identifier (e.g. "picq1")
                    string imageIdentifier = raw.question ?? string.Empty;

                    q.questionText = imageIdentifier;
                    q.correctAnswer = raw.correctAnswer ?? string.Empty;
                    q.robotAnswer = raw.robotAnswer ?? string.Empty;
                    q.robotAnecdote = ""; // Not part of the data structure yet
                    q.questionType = "picture";

                    // Allow explicit imageURL override, otherwise fall back to the identifier itself
                    q.imageURL = string.IsNullOrEmpty(raw.imageURL) ? imageIdentifier : raw.imageURL;

                    questions.Add(q);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error parsing picture question array: " + e.Message);
        }

        return questions;
    }
}

// === JSON DATA STRUCTURES ===

[System.Serializable]
public class RawQuestionData
{
    public string QUESTION;
    public string CORRECT_ANSWER;
    public string ROBOT_ANSWER;
}

[System.Serializable]
public class RawQuestionArrayWrapper
{
    public List<RawQuestionData> questions;
}

[System.Serializable]
public class RawPlayerQuestionData
{
    public string Question;
    public string Right_Answer;
    public string Robot_Answer;
}

[System.Serializable]
public class RawPlayerQuestionArrayWrapper
{
    public List<RawPlayerQuestionData> questions;
}

[System.Serializable]
public class RawPictureQuestionData
{
    public string question;
    public string correctAnswer;
    public string robotAnswer;
    public string imageURL;
}

[System.Serializable]
public class RawPictureQuestionArrayWrapper
{
    public List<RawPictureQuestionData> questions;
}

[System.Serializable]
public class RawBonusQuestionData
{
    public int id;
    public string question;
}

[System.Serializable]
public class RawBonusQuestionArrayWrapper
{
    public List<RawBonusQuestionData> bonusQuestions;
}

[System.Serializable]
public class QuestionArrayWrapper
{
    public List<Question> questions;
}
