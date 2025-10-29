using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Utility class for shuffling collections using the Fisher-Yates algorithm.
/// This is the industry-standard shuffle method that ensures uniform distribution
/// and prevents patterns that simpler shuffle methods might produce.
/// </summary>
public static class ShuffleUtility
{
    /// <summary>
    /// Shuffles a list in-place using the Fisher-Yates algorithm (also known as Knuth shuffle).
    /// This algorithm guarantees that every permutation is equally likely.
    /// Time complexity: O(n), Space complexity: O(1)
    /// </summary>
    /// <typeparam name="T">The type of elements in the list</typeparam>
    /// <param name="list">The list to shuffle</param>
    public static void Shuffle<T>(List<T> list)
    {
        if (list == null || list.Count <= 1)
        {
            return;
        }

        // Fisher-Yates shuffle algorithm
        // Start from the last element and swap it with a random element before it
        for (int i = list.Count - 1; i > 0; i--)
        {
            // Generate a random index from 0 to i (inclusive)
            int randomIndex = Random.Range(0, i + 1);

            // Swap elements at i and randomIndex
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    /// <summary>
    /// Creates a shuffled copy of a list without modifying the original.
    /// Uses the Fisher-Yates algorithm.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list</typeparam>
    /// <param name="list">The list to copy and shuffle</param>
    /// <returns>A new shuffled list</returns>
    public static List<T> ShuffledCopy<T>(List<T> list)
    {
        if (list == null)
        {
            return null;
        }

        List<T> copy = new List<T>(list);
        Shuffle(copy);
        return copy;
    }

    /// <summary>
    /// Seeds Unity's random number generator with a specific value.
    /// Useful for reproducible shuffles (e.g., for testing or replay features).
    /// </summary>
    /// <param name="seed">The seed value</param>
    public static void SeedRandom(int seed)
    {
        Random.InitState(seed);
    }

    /// <summary>
    /// Seeds Unity's random number generator with the current system time.
    /// This ensures truly random shuffles across different game sessions.
    /// </summary>
    public static void SeedRandomWithTime()
    {
        Random.InitState(System.DateTime.Now.Millisecond +
                        System.DateTime.Now.Second * 1000 +
                        System.DateTime.Now.Minute * 60000);
    }

    /// <summary>
    /// Tests the shuffle implementation by running multiple shuffles and verifying:
    /// 1. All original elements are preserved (no loss or duplication)
    /// 2. The order changes between shuffles
    /// 3. Different shuffles produce different results
    /// </summary>
    public static void TestShuffle()
    {
        Debug.Log("=== Testing Fisher-Yates Shuffle Implementation ===");

        // Test 1: Verify all elements are preserved
        List<int> original = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        List<int> shuffled = new List<int>(original);
        Shuffle(shuffled);

        // Check count
        if (shuffled.Count != original.Count)
        {
            Debug.LogError($"FAIL: Count mismatch. Original: {original.Count}, Shuffled: {shuffled.Count}");
            return;
        }

        // Check all elements exist
        foreach (int num in original)
        {
            if (!shuffled.Contains(num))
            {
                Debug.LogError($"FAIL: Element {num} missing after shuffle");
                return;
            }
        }
        Debug.Log("PASS: All elements preserved after shuffle");

        // Test 2: Verify order changes (with high probability)
        SeedRandomWithTime();
        List<string> test = new List<string> { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J" };
        List<string> before = new List<string>(test);
        Shuffle(test);

        bool orderChanged = false;
        for (int i = 0; i < test.Count; i++)
        {
            if (test[i] != before[i])
            {
                orderChanged = true;
                break;
            }
        }

        if (orderChanged)
        {
            Debug.Log("PASS: Order changed after shuffle");
            Debug.Log($"Before: {string.Join(", ", before)}");
            Debug.Log($"After:  {string.Join(", ", test)}");
        }
        else
        {
            Debug.LogWarning("WARNING: Order did not change (very unlikely but possible with small lists)");
        }

        // Test 3: Multiple shuffles produce different results
        SeedRandomWithTime();
        List<int> shuffle1 = new List<int>(original);
        Shuffle(shuffle1);

        System.Threading.Thread.Sleep(10); // Small delay to ensure different seed
        SeedRandomWithTime();
        List<int> shuffle2 = new List<int>(original);
        Shuffle(shuffle2);

        bool differentResults = false;
        for (int i = 0; i < shuffle1.Count; i++)
        {
            if (shuffle1[i] != shuffle2[i])
            {
                differentResults = true;
                break;
            }
        }

        if (differentResults)
        {
            Debug.Log("PASS: Multiple shuffles produce different results");
        }
        else
        {
            Debug.LogWarning("WARNING: Two shuffles produced identical results (very unlikely)");
        }

        Debug.Log("=== Shuffle Test Complete ===");
    }
}
