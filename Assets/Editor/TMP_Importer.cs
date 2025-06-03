using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using System.Collections.Generic;

public class TMP_Importer : Editor
{
    private static ListRequest Request;

    [InitializeOnLoadMethod]
    static void ImportTMP()
    {
        Request = Client.List();
        EditorApplication.update += Progress;
    }

    static void Progress()
    {
        if (Request.IsCompleted)
        {
            if (Request.Status == StatusCode.Success)
            {
                bool tmpInstalled = false;
                foreach (var package in Request.Result)
                {
                    if (package.name == "com.unity.textmeshpro")
                    {
                        tmpInstalled = true;
                        break;
                    }
                }

                if (!tmpInstalled)
                {
                    Debug.Log("Importing TextMeshPro...");
                    Client.Add("com.unity.textmeshpro");
                }
            }
            else if (Request.Status == StatusCode.Failure)
            {
                Debug.LogError("Failed to list packages: " + Request.Error.message);
            }

            EditorApplication.update -= Progress;
        }
    }
}
