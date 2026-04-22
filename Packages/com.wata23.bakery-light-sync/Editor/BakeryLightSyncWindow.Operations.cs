
//License: PolyForm Noncommercial 1.0.0

//Required Notice: Copyright(c) 2026 Wata23

#if UNITY_EDITOR

namespace Wata23.BakeryLightSync.Editor
{

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;

    // Add, update, remove, and realtime handling logic.
    public partial class BakeryLightSyncWindow
    {
        // Add Bakery components to all supported lights.
        private void AddBakeryComponentsToAllWithDialog()
        {
            if (!EditorUtility.DisplayDialog(
                    WindowTitle,
                    T("AddAllDialog"),
                    T("OK"),
                    T("Cancel")))
            {
                return;
            }

            RefreshLightList(false);

            if (!isBakeryAvailable)
            {
                SetMessage("BakeryNotInstalled");
                return;
            }

            RealtimeResolvedAction bulkRealtimeAction = RealtimeResolvedAction.KeepRealtime;

            if (realtimeAddSetting == RealtimeAddSetting.AskOnAddAll)
            {
                int realtimeCount = entries.Count(e =>
                    e.IsSupported &&
                    e.UnityLight != null &&
                    e.BakeryComponent == null &&
                    e.UnityLight.lightmapBakeType == LightmapBakeType.Realtime);

                if (realtimeCount > 0)
                {
                    bulkRealtimeAction = PromptRealtimeActionForBulk(realtimeCount);
                    if (bulkRealtimeAction == RealtimeResolvedAction.Cancel)
                    {
                        SetMessage("BulkAddCanceledRealtime", realtimeCount);
                        return;
                    }
                }
            }
            else
            {
                bulkRealtimeAction = GetRealtimeResolvedActionForSingleAdd();
            }

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Add Bakery Light Components To All");

            int added = 0;
            int updated = 0;
            int failed = 0;

            foreach (var entry in entries)
            {
                if (!entry.IsSupported)
                {
                    failed++;
                    continue;
                }

                bool hadComponent = entry.BakeryComponent != null;
                bool ok = AddOrUpdateBakeryForEntry(entry, false, bulkRealtimeAction);
                if (!ok)
                {
                    failed++;
                    continue;
                }

                if (hadComponent)
                {
                    updated++;
                }
                else
                {
                    added++;
                }
            }

            Undo.CollapseUndoOperations(undoGroup);
            RefreshLightList(false);
            SetMessage("AddAllResult", added, updated, failed);
        }

        // Ask how to handle Realtime lights for bulk add.
        private RealtimeResolvedAction PromptRealtimeActionForBulk(int count)
        {
            while (true)
            {
                int first = EditorUtility.DisplayDialogComplex(
                    WindowTitle,
                    string.Format(T("RealtimeBulkPrompt"), count),
                    T("RealtimeKeep"),
                    T("RealtimeBaked"),
                    T("More"));

                if (first == 0)
                {
                    return RealtimeResolvedAction.KeepRealtime;
                }

                if (first == 1)
                {
                    return RealtimeResolvedAction.ChangeToBaked;
                }

                int second = EditorUtility.DisplayDialogComplex(
                    WindowTitle,
                    string.Format(T("RealtimeBulkPromptMore"), count),
                    T("RealtimeMixed"),
                    T("Back"),
                    T("Cancel"));

                if (second == 0)
                {
                    return RealtimeResolvedAction.ChangeToMixed;
                }

                if (second == 2)
                {
                    return RealtimeResolvedAction.Cancel;
                }
            }
        }

        // Remove Bakery components from all matching lights.
        private void RemoveBakeryComponentsFromAllWithDialog()
        {
            if (!EditorUtility.DisplayDialog(
                    WindowTitle,
                    T("RemoveAllDialog"),
                    T("OK"),
                    T("Cancel")))
            {
                return;
            }

            RefreshLightList(false);

            if (!isBakeryAvailable)
            {
                SetMessage("BakeryNotInstalled");
                return;
            }

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Remove Bakery Light Components From All");

            var targets = new HashSet<Component>();
            foreach (var entry in entries)
            {
                // Remove only the matching Bakery component on the same GameObject.
                if (entry.IsSupported && entry.BakeryComponent != null)
                {
                    targets.Add(entry.BakeryComponent);
                }
            }

            int removed = 0;
            foreach (var comp in targets)
            {
                if (comp == null)
                {
                    continue;
                }

                Undo.DestroyObjectImmediate(comp);
                removed++;
            }

            Undo.CollapseUndoOperations(undoGroup);
            RefreshLightList(false);
            SetMessage("RemoveAllResult", removed);
        }

        // Apply the resolved action to a Realtime Unity Light.
        private bool ApplyRealtimeResolvedAction(Light unityLight, RealtimeResolvedAction action)
        {
            if (unityLight == null)
            {
                return false;
            }

            switch (action)
            {
                case RealtimeResolvedAction.ChangeToMixed:
                    Undo.RecordObject(unityLight, "Change Light Mode To Mixed");
                    unityLight.lightmapBakeType = LightmapBakeType.Mixed;
                    EditorUtility.SetDirty(unityLight);
                    EditorSceneManager.MarkSceneDirty(unityLight.gameObject.scene);
                    return true;

                case RealtimeResolvedAction.ChangeToBaked:
                    Undo.RecordObject(unityLight, "Change Light Mode To Baked");
                    unityLight.lightmapBakeType = LightmapBakeType.Baked;
                    EditorUtility.SetDirty(unityLight);
                    EditorSceneManager.MarkSceneDirty(unityLight.gameObject.scene);
                    return true;

                case RealtimeResolvedAction.KeepRealtime:
                default:
                    return true;
            }
        }

        // Add or update the Bakery component for one entry.
        private bool AddOrUpdateBakeryForEntry(
            LightEntry entry,
            bool showMessage,
            RealtimeResolvedAction realtimeAction = RealtimeResolvedAction.KeepRealtime)
        {
            if (entry == null || entry.UnityLight == null || entry.GameObject == null)
            {
                if (showMessage)
                {
                    SetMessage("FailedInvalidEntry");
                }

                return false;
            }

            if (!entry.IsSupported || entry.BakeryType == null)
            {
                if (showMessage)
                {
                    SetMessage("BakeryTypeMissing", entry.GameObject.name);
                }

                return false;
            }

            try
            {
                Component bakery = entry.GameObject.GetComponent(entry.BakeryType);
                bool created = false;

                if (bakery == null)
                {
                    if (entry.UnityLight.lightmapBakeType == LightmapBakeType.Realtime)
                    {
                        bool actionOk = ApplyRealtimeResolvedAction(entry.UnityLight, realtimeAction);
                        if (!actionOk)
                        {
                            if (showMessage)
                            {
                                SetMessage("RealtimeAddCanceled", entry.GameObject.name);
                            }

                            return false;
                        }
                    }

                    bakery = Undo.AddComponent(entry.GameObject, entry.BakeryType);
                    created = true;
                }
                else
                {
                    Undo.RecordObject(bakery, "Update Bakery Light Component");
                }

                ManualCopyFromUnityLight(entry.UnityLight, bakery);

                if (entry.UnityLight.type == LightType.Area)
                {
                    EnsureAreaLightMeshSetup(entry.UnityLight, bakery);
                    TryEnsureAreaLightCutoff(bakery, 100f);
                }

                EditorUtility.SetDirty(bakery);
                EditorUtility.SetDirty(entry.GameObject);
                EditorSceneManager.MarkSceneDirty(entry.GameObject.scene);

                if (showMessage)
                {
                    if (entry.UnityLight.type == LightType.Area)
                    {
                        SetMessage(created ? "AddedAreaMatchPrompt" : "UpdatedAreaMatchPrompt", entry.GameObject.name);
                    }
                    else
                    {
                        SetMessage(created ? "AddedOne" : "UpdatedOne", entry.GameObject.name);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                if (showMessage)
                {
                    SetMessage("FailedAdd", entry.GameObject.name, ex.Message);
                }

                Debug.LogException(ex);
                return false;
            }
        }

        // Remove the Bakery component for one entry.
        private bool RemoveBakeryForEntry(LightEntry entry, bool showMessage)
        {
            if (entry == null || entry.GameObject == null || entry.BakeryType == null)
            {
                if (showMessage)
                {
                    SetMessage("FailedInvalidEntry");
                }

                return false;
            }

            try
            {
                Component comp = entry.GameObject.GetComponent(entry.BakeryType);
                if (comp == null)
                {
                    if (showMessage)
                    {
                        SetMessage("NoBakeryToRemove", entry.GameObject.name);
                    }

                    return true;
                }

                Undo.DestroyObjectImmediate(comp);
                EditorSceneManager.MarkSceneDirty(entry.GameObject.scene);

                if (showMessage)
                {
                    SetMessage("RemovedOne", entry.GameObject.name);
                }

                return true;
            }
            catch (Exception ex)
            {
                if (showMessage)
                {
                    SetMessage("FailedRemove", entry.GameObject.name, ex.Message);
                }

                Debug.LogException(ex);
                return false;
            }
        }
    }

}

#endif