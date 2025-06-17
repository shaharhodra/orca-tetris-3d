using UnityEngine;

public class Countdown3DDisplay : MonoBehaviour
{
    public GameObject[] digitPrefabs;         // פריפאבים של הספרות (0–9)
    public Transform digitParent;             // לאן להציב את הספרות
    public float spacing = 1.0f;              // מרחק בין ספרות

    public void ShowNumber(int number)
    {
        // מחק ספרות קודמות
        foreach (Transform child in digitParent)
            Destroy(child.gameObject);

        string numberStr = number.ToString();
        float startX = -(numberStr.Length - 1) * spacing * 0.5f;

        for (int i = 0; i < numberStr.Length; i++)
        {
            int digit = int.Parse(numberStr[i].ToString());
            GameObject digitObj = Instantiate(digitPrefabs[digit],
                                              digitParent.position + new Vector3(startX + i * spacing, 0, 0),
                                              digitParent.rotation,
                                              digitParent);
        }
    }
}
