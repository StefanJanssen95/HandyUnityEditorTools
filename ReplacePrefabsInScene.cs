using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ReplacePrefabsInScene : EditorWindow
{
    [SerializeField] private Dictionary<string, GameObject> prefabReplacements = new Dictionary<string, GameObject>();

    [MenuItem("Tools/Replace With Prefab")]
    static void CreateReplaceWithPrefab()
    {
        GetWindow<ReplacePrefabsInScene>();
    }

    private void OnGUI()
    {
        var sourceGameObjects = Editor.FindObjectsOfType<GameObject>()
            .Where(go => go.name.ToLower().StartsWith("sm_")).ToList();

        var sourcePrefabs = sourceGameObjects
            .Select(PrefabUtility.GetCorrespondingObjectFromOriginalSource)
            .Distinct()
            .OrderBy(go => go.name)
            .ToList();

        EditorGUIUtility.labelWidth = 250f;
        EditorGUIUtility.fieldWidth = 5f;
        foreach (var src in sourcePrefabs)
        {
            prefabReplacements[src.name] = (GameObject) EditorGUILayout.ObjectField(src.name,
                prefabReplacements.ContainsKey(src.name) ? prefabReplacements[src.name] : null, typeof(GameObject),
                false);
        }

        if (GUILayout.Button("Replace"))
        {
            int replaced = 0;
            var selection = sourceGameObjects;
            for (var i = selection.Count - 1; i >= 0; --i)
            {
                var selected = selection[i];
                var selectedOriginPrefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(selected);
                if (!prefabReplacements.ContainsKey(selectedOriginPrefab.name) ||
                    prefabReplacements[selectedOriginPrefab.name] == null)
                    continue;

                var prefab = prefabReplacements[selectedOriginPrefab.name];
                var prefabType = PrefabUtility.GetPrefabAssetType(prefab);
                GameObject newObject;
                if (prefabType == PrefabAssetType.Model)
                {
                    newObject = (GameObject) PrefabUtility.InstantiatePrefab(prefab);
                }
                else
                {
                    newObject = Instantiate(prefab);
                    newObject.name = prefab.name;
                }

                if (newObject == null)
                {
                    Debug.LogError("Error instantiating prefab");
                    break;
                }

                Undo.RegisterCreatedObjectUndo(newObject, "Replace With Prefabs");
                newObject.transform.parent = selected.transform.parent;
                newObject.transform.localPosition = selected.transform.localPosition;
                newObject.transform.localRotation = selected.transform.localRotation;
                newObject.transform.localScale = selected.transform.localScale;
                newObject.transform.SetSiblingIndex(selected.transform.GetSiblingIndex());
                Undo.DestroyObjectImmediate(selected);
                replaced++;
            }

            Debug.Log($"Replaced {replaced} GameObjects in scene.");
        }

        GUI.enabled = false;
        EditorGUILayout.LabelField("Number of GameObjects for potential replacement: " + sourceGameObjects.Count);
        EditorGUILayout.LabelField("Number of prefabs used: " + sourcePrefabs.Count);
    }
}
