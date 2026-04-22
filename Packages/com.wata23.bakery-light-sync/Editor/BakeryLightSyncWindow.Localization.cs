
//License: PolyForm Noncommercial 1.0.0

//Required Notice: Copyright(c) 2026 Wata23

#if UNITY_EDITOR

namespace Wata23.BakeryLightSync.Editor
{

    // Localized UI strings and status messages.
    public partial class BakeryLightSyncWindow
    {
        private void SetMessage(string key, params object[] args)
        {
            string text = T(key);
            message = args != null && args.Length > 0 ? string.Format(text, args) : text;
            Repaint();
        }

        private string T(string key)
        {
            bool ja = language == UiLanguage.Japanese;

            switch (key)
            {
                case "Ready":
                    return ja ? "準備完了。" : "Ready.";

                case "Language":
                    return ja ? "言語" : "Language";

                case "LanguageChanged":
                    return ja ? "表示言語を変更しました。" : "Language changed.";

                case "Refresh":
                    return ja ? "更新" : "Refresh";

                case "Settings":
                    return ja ? "設定" : "Settings";

                case "RealtimeHandlingMenuRoot":
                    return ja ? "Bakeryコンポーネント追加時のライトモード" : "Light Mode When Adding Bakery Components";

                case "RealtimeSettingKeep":
                    return ja ? "Realtime のまま追加" : "Keep Realtime";

                case "RealtimeSettingAutoMixed":
                    return ja ? "自動で Mixed に変更" : "Change to Mixed";

                case "RealtimeSettingAutoBaked":
                    return ja ? "自動で Baked に変更" : "Change to Baked";

                case "RealtimeSettingAskOnAddAll":
                    return ja ? "まとめて追加時のみ確認" : "Ask only on Add All";

                case "RealtimeHandlingChanged":
                    return ja
                        ? "Realtimeライト追加時の扱いを変更しました: {0}"
                        : "Changed Realtime light handling: {0}";

                case "AddAll":
                    return ja ? "すべてにBakeryコンポーネントを追加..." : "Add Bakery Components To All...";

                case "RemoveAll":
                    return ja ? "すべてのBakeryコンポーネントを削除..." : "Remove Bakery Components From All...";

                case "Object":
                    return ja ? "オブジェクト" : "Object";

                case "Component":
                    return ja ? "コンポーネント" : "Component";

                case "LightType":
                    return ja ? "ライト種別" : "Light Type";

                case "LightMode":
                    return ja ? "ライトモード" : "Light Mode";

                case "LightModeRealtime":
                    return "Realtime";

                case "LightModeMixed":
                    return "Mixed";

                case "LightModeBaked":
                    return "Baked";

                case "Enabled":
                    return ja ? "有効" : "Enabled";

                case "Color":
                    return ja ? "色" : "Color";

                case "Intensity":
                    return ja ? "強度" : "Intensity";

                case "Range":
                    return ja ? "範囲/Cutoff" : "Range/Cutoff";

                case "SpotAngle":
                    return ja ? "スポット角度" : "Spot Angle";

                case "AreaSize":
                    return ja ? "Areaサイズ" : "Area Size";

                case "Cookie":
                    return "Cookie";

                case "NoLights":
                    return ja
                        ? "Scene内にLightコンポーネントが見つかりません。"
                        : "No Light components were found in the open scenes.";

                case "BakeryNotInstalled":
                    return ja
                        ? "プロジェクトにBakeryがインポートされていません。インポートしてください。"
                        : "Bakery is not imported in this project. Please import it.";

                case "UnsupportedOrMissingBakery":
                    return ja
                        ? "未対応、またはBakeryクラスが見つかりません。"
                        : "Unsupported light type or missing Bakery class.";

                case "Refreshed":
                    return ja
                        ? "Light一覧を更新しました: {0}件。"
                        : "Refreshed light list: {0} item(s).";

                case "AddAllDialog":
                    return ja
                        ? "Scene内のすべてのUnity LightにBakeryコンポーネントを追加しますか？\n既に存在するBakeryコンポーネントは保持し、設定を更新します。\nArea LightではMeshFilter、MeshRenderer、Material、Transform Scaleが変更される可能性があります。"
                        : "Add Bakery components to all Unity Light objects in the open scenes?\nExisting Bakery components will be kept and updated.\nArea Lights may modify MeshFilter, MeshRenderer, material, or transform scale.";

                case "RemoveAllDialog":
                    return ja
                        ? "Scene内のUnity Lightと同じGameObject上にある対応Bakeryライトコンポーネントを削除しますか？\nこの操作はUndoに対応しています。"
                        : "Remove matching Bakery light components on the same GameObjects as Unity Light components in the open scenes?\nThis operation supports Undo.";

                case "RealtimeBulkPrompt":
                    return ja
                        ? "Realtime の Unity Light が {0} 件あります。\nBakery コンポーネントを追加する前に、Realtime ライトをどう処理するか選んでください。"
                        : "There are {0} Realtime Unity Light(s).\nChoose how to handle those Realtime lights before adding Bakery components.";

                case "RealtimeBulkPromptMore":
                    return ja
                        ? "Realtime の Unity Light が {0} 件あります。\nMixed に変更してから追加するか、戻るか、キャンセルするか選んでください。"
                        : "There are {0} Realtime Unity Light(s).\nChoose whether to change them to Mixed before adding, go back, or cancel.";

                case "RealtimeKeep":
                    return ja ? "Realtime のまま追加" : "Add and keep Realtime";

                case "RealtimeMixed":
                    return ja ? "Mixed に変更して追加" : "Add and change to Mixed";

                case "RealtimeBaked":
                    return ja ? "Baked に変更して追加" : "Add and change to Baked";

                case "More":
                    return ja ? "その他..." : "More...";

                case "Back":
                    return ja ? "戻る" : "Back";

                case "OK":
                    return "OK";

                case "Cancel":
                    return ja ? "キャンセル" : "Cancel";

                case "AddAllResult":
                    return ja
                        ? "一括追加完了。追加: {0}件、更新: {1}件、失敗/未対応: {2}件。"
                        : "Add all completed. Added: {0}, updated: {1}, failed/unsupported: {2}.";

                case "RemoveAllResult":
                    return ja
                        ? "一括削除完了。削除: {0}件。"
                        : "Remove all completed. Removed: {0}.";

                case "BulkAddCanceledRealtime":
                    return ja
                        ? "Realtime ライトの処理選択がキャンセルされたため、一括追加を中止しました。対象: {0}件。"
                        : "Bulk add was canceled because the Realtime light action selection was canceled. Target count: {0}.";

                case "FailedInvalidEntry":
                    return ja
                        ? "対象が無効です。Light一覧を更新してください。"
                        : "Invalid target. Please refresh the light list.";

                case "BakeryTypeMissing":
                    return ja
                        ? "{0}: 対応するBakeryクラスが見つかりません。Bakeryがインポートされているか確認してください。"
                        : "{0}: Matching Bakery class was not found. Make sure Bakery is imported.";

                case "LightModeChanged":
                    return ja
                        ? "{0}: ライトモードを {1} に変更しました。"
                        : "{0}: Changed Light Mode to {1}.";

                case "RealtimeAddCanceled":
                    return ja
                        ? "{0}: Realtime ライトへの Bakery コンポーネント追加をキャンセルしました。"
                        : "{0}: Canceled adding a Bakery component to the Realtime light.";

                case "AddedOne":
                    return ja
                        ? "{0}: Bakeryコンポーネントを追加しました。"
                        : "{0}: Added Bakery component.";

                case "UpdatedOne":
                    return ja
                        ? "{0}: 既存のBakeryコンポーネントを更新しました。"
                        : "{0}: Updated existing Bakery component.";

                case "AddedAreaMatchPrompt":
                    return ja
                        ? "{0}: Area Light用のBakeryコンポーネントを追加しました。CutOff を更新するため、Bakery の「Match lightmapped to area light」を押してください。"
                        : "{0}: Added Bakery component for an Area Light. To update CutOff, please press Bakery's 'Match lightmapped to area light'.";

                case "UpdatedAreaMatchPrompt":
                    return ja
                        ? "{0}: Area Light用のBakeryコンポーネントを更新しました。CutOff を更新するため、Bakery の「Match lightmapped to area light」を押してください。"
                        : "{0}: Updated Bakery component for an Area Light. To update CutOff, please press Bakery's 'Match lightmapped to area light'.";

                case "FailedAdd":
                    return ja
                        ? "{0}: Bakeryコンポーネントの追加/更新に失敗しました。理由: {1}"
                        : "{0}: Failed to add/update Bakery component. Reason: {1}";

                case "NoBakeryToRemove":
                    return ja
                        ? "{0}: 削除対象のBakeryコンポーネントがありません。"
                        : "{0}: No Bakery component to remove.";

                case "RemovedOne":
                    return ja
                        ? "{0}: Bakeryコンポーネントを削除しました。"
                        : "{0}: Removed Bakery component.";

                case "FailedRemove":
                    return ja
                        ? "{0}: Bakeryコンポーネントの削除に失敗しました。理由: {1}"
                        : "{0}: Failed to remove Bakery component. Reason: {1}";

                default:
                    return key;
            }
        }
    }

}

#endif