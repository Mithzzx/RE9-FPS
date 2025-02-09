using UnityEngine;
using UnityEditor;

public class CopyTransformsEditor : EditorWindow
{
    private GameObject sourceCharacter;
    private GameObject targetCharacter;

    [MenuItem("Tools/Copy Transforms")]
    public static void ShowWindow()
    {
        GetWindow<CopyTransformsEditor>("Copy Transforms");
    }

    private void OnGUI()
    {
        GUILayout.Label("Copy Transforms", EditorStyles.boldLabel);

        sourceCharacter = (GameObject)EditorGUILayout.ObjectField("Source Character", sourceCharacter, typeof(GameObject), true);
        targetCharacter = (GameObject)EditorGUILayout.ObjectField("Target Character", targetCharacter, typeof(GameObject), true);

        if (GUILayout.Button("Copy Transforms"))
        {
            if (sourceCharacter == null || targetCharacter == null)
            {
                Debug.LogError("Source or target character is not assigned.");
                return;
            }

            CopyTransformsRecursive(sourceCharacter.transform, targetCharacter.transform);
            Debug.Log("Transforms copied successfully.");
        }
    }

    private void CopyTransformsRecursive(Transform source, Transform target)
    {
        if (source == null || target == null)
        {
            Debug.LogError("Source or target transform is null.");
            return;
        }

        target.localPosition = source.localPosition;
        target.localRotation = source.localRotation;
        target.localScale = source.localScale;

        for (int i = 0; i < source.childCount; i++)
        {
            if (i < target.childCount)
            {
                CopyTransformsRecursive(source.GetChild(i), target.GetChild(i));
            }
            else
            {
                Debug.LogWarning("Target character has fewer children than source character.");
                break;
            }
        }
    }
}
