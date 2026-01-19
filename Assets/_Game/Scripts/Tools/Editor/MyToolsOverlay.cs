using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars; // Cần namespace này cho Unity 6
using UnityEngine;

// 1. Khai báo một Overlay mới
// id: Định danh duy nhất
// displayName: Tên hiển thị trong menu "Overlays"
[Overlay(typeof(SceneView), "My Game Tools", true)]
public class MyToolsOverlay : ToolbarOverlay
{
    // Constructor: Add các nút vào thanh công cụ
    MyToolsOverlay() : base(
        OpenVSCodeBtn.id,
        ClearPrefsBtn.id,
        QuickAccessDropdown.id // Thêm nút Dropdown
    )
    { }
}

// --- NÚT 1: OPEN VS CODE ---
[EditorToolbarElement(id, typeof(SceneView))]
class OpenVSCodeBtn : EditorToolbarButton
{
    public const string id = "MyGameTools/OpenVSCode";

    public OpenVSCodeBtn()
    {
        text = "VS Code";
        icon = EditorGUIUtility.IconContent("d_editicon.sml").image as Texture2D;
        tooltip = "Mở Visual Studio Code";

        // Sự kiện click
        clicked += () => EditorApplication.ExecuteMenuItem("Assets/Open C# Project");
    }
}

// --- NÚT 2: CLEAR PREFS ---
[EditorToolbarElement(id, typeof(SceneView))]
class ClearPrefsBtn : EditorToolbarButton
{
    public const string id = "MyGameTools/ClearPrefs";

    public ClearPrefsBtn()
    {
        text = "Clear Prefs";
        icon = EditorGUIUtility.IconContent("d_TreeEditor.Trash").image as Texture2D;
        tooltip = "Xoá toàn bộ PlayerPrefs";

        clicked += () =>
        {
            if (EditorUtility.DisplayDialog("Xác nhận", "Xoá sạch PlayerPrefs?", "Xoá", "Thôi"))
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                Debug.Log("Đã xoá PlayerPrefs!");
            }
        };
    }
}

// --- NÚT 3: QUICK ACCESS (Dạng Dropdown) ---
[EditorToolbarElement(id, typeof(SceneView))]
class QuickAccessDropdown : EditorToolbarDropdown
{
    public const string id = "MyGameTools/QuickAccess";

    public QuickAccessDropdown()
    {
        text = "Quick Access";
        tooltip = "Lối tắt đến file";
        icon = EditorGUIUtility.IconContent("d_ScriptableObject Icon").image as Texture2D;
        // Sự kiện khi bấm vào nút dropdown thì hiển thị menu gì
        clicked += ShowMenu;
    }

    void ShowMenu()
    {
        GenericMenu menu = new GenericMenu();
        menu.AddItem(new GUIContent("Data/Game Setting"), false, () => SelectAsset("Assets/Resources/Data/GameSetting.asset"));
        menu.AddItem(new GUIContent("Data/Game Level"), false, () => SelectAsset("Assets/Resources/Data/GameLevel.asset"));
        menu.AddItem(new GUIContent("Data/Game Support"), false, () => SelectAsset("Assets/Resources/Data/GameSupport.asset"));
        menu.AddItem(new GUIContent("Prefabs"), false, () => SelectAsset("Assets/_Game/Prefabs"));

        // Hiển thị menu
        menu.ShowAsContext();
    }

    static void SelectAsset(string path)
    {
        Object obj = AssetDatabase.LoadAssetAtPath<Object>(path);
        if (obj != null) { EditorGUIUtility.PingObject(obj); Selection.activeObject = obj; }
        else { Debug.LogError("Không tìm thấy: " + path); }
    }
}