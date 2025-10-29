using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PictureQuestionLoader : MonoBehaviour
{
    public static PictureQuestionLoader Instance;
    
    [Header("Picture Settings")]
    public string picturePath = "sprites/picture questions/";
    public int totalPictures = 20;
    
    [Header("Loaded Pictures")]
    private Dictionary<string, Sprite> pictureSprites = new Dictionary<string, Sprite>();
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        
        LoadAllPictures();
    }
    
    void LoadAllPictures()
    {
        pictureSprites.Clear();
        
        // Load all 20 picture question images
        for (int i = 1; i <= totalPictures; i++)
        {
            string pictureName = "picq" + i;
            string fullPath = picturePath + pictureName;
            
            Sprite pictureSprite = Resources.Load<Sprite>(fullPath);
            
            if (pictureSprite != null)
            {
                pictureSprites.Add(pictureName, pictureSprite);
            }
            else
            {
                Debug.LogWarning("Could not load picture: " + fullPath);
            }
        }
        
        Debug.Log("Loaded " + pictureSprites.Count + " picture question images");
    }
    
    // === PUBLIC METHODS ===
    
    public Sprite GetPicture(string pictureName)
    {
        if (pictureSprites.ContainsKey(pictureName))
        {
            return pictureSprites[pictureName];
        }
        
        Debug.LogWarning("Picture not found: " + pictureName);
        return null;
    }
    
    public Sprite GetPictureByIndex(int index)
    {
        if (index < 1 || index > totalPictures)
        {
            Debug.LogWarning("Picture index out of range: " + index);
            return null;
        }
        
        string pictureName = "picq" + index;
        return GetPicture(pictureName);
    }
    
    public Sprite GetPictureByImageURL(string imageURL)
    {
        // imageURL from Question object should match picture name
        // e.g., "picq1" or just "1"
        
        if (string.IsNullOrEmpty(imageURL))
        {
            Debug.LogWarning("Empty imageURL provided");
            return null;
        }
        
        // If imageURL is just a number, convert to "picqN" format
        if (int.TryParse(imageURL, out int index))
        {
            return GetPictureByIndex(index);
        }
        
        // Otherwise, use as-is
        return GetPicture(imageURL);
    }
    
    public void SetPictureToImage(Image imageComponent, string imageURL)
    {
        if (imageComponent == null)
        {
            Debug.LogWarning("Image component is null");
            return;
        }
        
        Sprite picture = GetPictureByImageURL(imageURL);
        
        if (picture != null)
        {
            imageComponent.sprite = picture;
        }
        else
        {
            Debug.LogWarning("Could not set picture for imageURL: " + imageURL);
        }
    }
    
    public List<Sprite> GetAllPictures()
    {
        return new List<Sprite>(pictureSprites.Values);
    }
}
