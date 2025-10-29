using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CreditsLoader : MonoBehaviour
{
    [Header("Credits File")]
    public string creditsFilePath = "credits";
    
    [Header("UI Elements")]
    public Transform creditsScrollZone;
    public GameObject creditTextPrefab;
    public float lineSpacing = 30f;
    
    [Header("Scroll Settings")]
    public float scrollSpeed = 20f;
    public bool autoScroll = true;
    
    void Start()
    {
        LoadAndDisplayCredits();
    }
    
    void Update()
    {
        if (autoScroll && creditsScrollZone != null)
        {
            RectTransform scrollRect = creditsScrollZone.GetComponent<RectTransform>();
            if (scrollRect != null)
            {
                Vector2 pos = scrollRect.anchoredPosition;
                pos.y += scrollSpeed * Time.deltaTime;
                scrollRect.anchoredPosition = pos;
            }
        }
    }
    
    void LoadAndDisplayCredits()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(creditsFilePath);

        if (jsonFile == null)
        {
            Debug.LogError("Could not find credits.json at Resources/" + creditsFilePath);
            return;
        }

        // Wrap the array in an object: {"credits": [...]}
        string wrappedJson = "{\"credits\":" + jsonFile.text + "}";

        CreditsList creditsData = JsonUtility.FromJson<CreditsList>(wrappedJson);

        if (creditsData != null && creditsData.credits != null)
        {
            DisplayCredits(creditsData.credits);
            Debug.Log("Loaded " + creditsData.credits.Count + " credit entries");
        }
        else
        {
            Debug.LogError("Failed to parse credits JSON");
        }
    }
    
    void DisplayCredits(List<CreditEntry> credits)
    {
        if (creditsScrollZone == null)
        {
            Debug.LogError("Credits scroll zone not assigned");
            return;
        }
        
        float yPosition = 0f;
        
        foreach (CreditEntry entry in credits)
        {
            // Create role text (e.g., "Game Design")
            if (!string.IsNullOrEmpty(entry.role))
            {
                GameObject roleObj = CreateCreditText(entry.role, true);
                roleObj.transform.SetParent(creditsScrollZone);
                PositionCreditText(roleObj, yPosition);
                yPosition -= lineSpacing * 1.5f; // Extra space after role
            }
            
            // Create name text (e.g., "John Smith")
            if (!string.IsNullOrEmpty(entry.name))
            {
                GameObject nameObj = CreateCreditText(entry.name, false);
                nameObj.transform.SetParent(creditsScrollZone);
                PositionCreditText(nameObj, yPosition);
                yPosition -= lineSpacing;
            }
            
            // Extra space between entries
            yPosition -= lineSpacing * 0.5f;
        }
    }
    
    GameObject CreateCreditText(string text, bool isRole)
    {
        GameObject textObj;
        
        if (creditTextPrefab != null)
        {
            textObj = Instantiate(creditTextPrefab);
        }
        else
        {
            // Create text programmatically
            textObj = new GameObject("CreditText");
            textObj.AddComponent<RectTransform>();
            textObj.AddComponent<TextMeshProUGUI>();
        }
        
        TextMeshProUGUI textComponent = textObj.GetComponent<TextMeshProUGUI>();
        if (textComponent != null)
        {
            textComponent.text = text;
            textComponent.alignment = TextAlignmentOptions.Center;
            
            if (isRole)
            {
                // Role styling (bold, larger)
                textComponent.fontStyle = FontStyles.Bold;
                textComponent.fontSize = 24;
            }
            else
            {
                // Name styling (normal, smaller)
                textComponent.fontStyle = FontStyles.Normal;
                textComponent.fontSize = 18;
            }
        }
        
        return textObj;
    }
    
    void PositionCreditText(GameObject textObj, float yPosition)
    {
        RectTransform rectTransform = textObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = new Vector2(0, yPosition);
            rectTransform.sizeDelta = new Vector2(400, 50);
        }
    }
}

// === JSON DATA STRUCTURES ===

[System.Serializable]
public class CreditsList
{
    public List<CreditEntry> credits;
}

[System.Serializable]
public class CreditEntry
{
    public string name;
    public string job;

    // Property to provide backwards compatibility
    public string role
    {
        get { return job; }
        set { job = value; }
    }
}
