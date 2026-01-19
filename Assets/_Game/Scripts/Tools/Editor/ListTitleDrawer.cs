using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(ListTitleAttribute))]
public class ListTitleDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // 1. Lấy tham số tên biến truyền vào
        ListTitleAttribute attr = attribute as ListTitleAttribute;

        // 2. Tìm thuộc tính con có tên tương ứng trong class
        SerializedProperty titleProp = property.FindPropertyRelative(attr.VarName);

        string newLabel = label.text;

        // 3. Nếu tìm thấy, lấy giá trị của nó làm tên hiển thị
        if (titleProp != null)
        {
            try
            {
                // Xử lý các kiểu dữ liệu khác nhau
                switch (titleProp.propertyType)
                {
                    case SerializedPropertyType.Enum:
                        newLabel = titleProp.enumDisplayNames[titleProp.enumValueIndex];
                        break;
                    case SerializedPropertyType.Integer:
                        newLabel = titleProp.intValue.ToString();
                        break;
                    case SerializedPropertyType.Float:
                        newLabel = titleProp.floatValue.ToString();
                        break;
                    case SerializedPropertyType.String:
                        newLabel = titleProp.stringValue;
                        break;
                    default:
                        newLabel = "(" + titleProp.propertyType + ")";
                        break;
                }
            }
            catch
            {
                newLabel = "Error";
            }
        }

        // 4. Vẽ lại Property với tên mới
        EditorGUI.PropertyField(position, property, new GUIContent(newLabel), true);
    }
}