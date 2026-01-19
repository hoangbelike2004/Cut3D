using UnityEngine;

public class ListTitleAttribute : PropertyAttribute
{
    public string VarName;
    public string ChildName; // Tham số mới: Tên biến con bên trong (nếu có)

    // Constructor hỗ trợ 2 tham số
    public ListTitleAttribute(string varName, string childName = null)
    {
        VarName = varName;
        ChildName = childName;
    }
}