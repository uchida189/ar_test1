using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public class NameToPrefab
{
    public string name;
    public GameObject prefab;
}

public class ImageTrackingObjectManager : MonoBehaviour
{
    [HideInInspector]
    public ARTrackedImageManager arTrackedImageManager;

    // マーカー名とプレハブのマッピング
    [HideInInspector, SerializeField]
    public List<NameToPrefab> markerNameToPrefab = new List<NameToPrefab>();

    void OnEnable()
    {
        // トラッキングされた画像が変更された際のイベントを登録
        arTrackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    void OnDisable()
    {
        // トラッキングされた画像が変更された際のイベントを解除
        arTrackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    // トラッキングされた画像が変更された際の処理
    void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        // 追加された画像
        foreach (var trackedImage in eventArgs.added)
        {
            // マーカー名を取得
            var name = trackedImage.referenceImage.name;
            // マーカー名とプレハブのマッピングからプレハブを取得
            var prefab = markerNameToPrefab.Find(x => x.name == name)?.prefab;
            if (prefab != null)
            {
                // ARTrackedImageのTransformの位置を少し上に調整
                var pos = trackedImage.transform.position;
                pos.y += 0.02f;
                // 180度回転し調整
                var rote = trackedImage.transform.eulerAngles;
                rote.y += 180;

                var instance = Instantiate(
                    prefab,
                    pos,
                    Quaternion.Euler(rote),
                    trackedImage.transform
                );
            }
        }
        // 更新された画像
        foreach (var trackedImage in eventArgs.updated)
        {
            if (trackedImage.trackingState == TrackingState.Tracking)
            {
                trackedImage.gameObject.SetActive(true);
            }
            else if (trackedImage.trackingState == TrackingState.Limited)
            {
                trackedImage.gameObject.SetActive(false);
            }
        }
        // 削除された画像
        foreach (var trackedImage in eventArgs.removed)
        {
            trackedImage.gameObject.SetActive(false);
        }
    }

    // リファレンスライブラリに名前が存在するかチェック
    bool HasNameInReferenceLibrary(IReferenceImageLibrary library, string name)
    {
        for (int i = 0; i < library.count; i++)
        {
            if (library[i].name == name)
            {
                return true;
            }
        }
        return false;
    }
    // マーカー名とプレハブのマッピングを更新
    public void UpdateNameToPrefabMappings()
    {
        if (arTrackedImageManager == null || arTrackedImageManager.referenceLibrary == null)
        {
            return;
        }
        // マーカー名とプレハブのマッピングから存在しないマーカー名を削除
        foreach (var pair in markerNameToPrefab)
        {
            if (!HasNameInReferenceLibrary(arTrackedImageManager.referenceLibrary, pair.name))
            {
                markerNameToPrefab.Remove(pair);
            }
        }
        // ReferenceImageLibraryに登録されているすべてのマーカー名に対して、
        // マーカー名とプレハブのマッピングにマーカー名が登録されていない場合は名前を登録する
        for (int i = 0; i < arTrackedImageManager.referenceLibrary.count; i++)
        {
            var name = arTrackedImageManager.referenceLibrary[i].name;
            if (!markerNameToPrefab.Exists(x => x.name == name))
            {
                markerNameToPrefab.Add(new NameToPrefab { name = name });
            }
        }
    }
}
// カスタムエディタ
#if UNITY_EDITOR
[CustomEditor(typeof(ImageTrackingObjectManager))]
public class ImageTrackingObjectManagerEditor : Editor
{
    bool showNameToPrefabMappings = true;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var manager = (ImageTrackingObjectManager)target;
        ARTrackedImageManager newManager = (ARTrackedImageManager)
            EditorGUILayout.ObjectField(
                "AR Tracked Image Manager",
                manager.arTrackedImageManager,
                typeof(ARTrackedImageManager),
                true
            );
        if (newManager != manager.arTrackedImageManager)
        {
            manager.arTrackedImageManager = newManager;
        }
        if (manager.arTrackedImageManager == null)
        {
            EditorGUILayout.HelpBox("Tracked Image Manager is required.", MessageType.Error);
        }
        else
        {
            manager.UpdateNameToPrefabMappings();
            if (manager.markerNameToPrefab.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "There are no reference images in the Reference Image Library.",
                    MessageType.Warning
                );
            }
            else
            {
                showNameToPrefabMappings = EditorGUILayout.Foldout(
                    showNameToPrefabMappings,
                    new GUIContent("Marker To Prefab", "The mapping from marker name to prefab."),
                    true
                );
                if (showNameToPrefabMappings)
                {
                    foreach (var pair in manager.markerNameToPrefab)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField(pair.name);
                        var newPrefab = (GameObject)
                            EditorGUILayout.ObjectField(pair.prefab, typeof(GameObject), true);
                        if (newPrefab != pair.prefab)
                        {
                            pair.prefab = newPrefab;
                            EditorUtility.SetDirty(manager);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
        }
    }
}
#endif

