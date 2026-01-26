using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    // Trỏ vào biến _name (biến giả chúng ta tạo bên dưới)
    [ListTitle("_name")]
    [SerializeField] private PoolAmount[] poolAmounts;

    private void Awake()
    {
        for (int i = 0; i < poolAmounts.Length; i++)
        {
            if (poolAmounts[i].prefab != null)
                SimplePool.PreLoad(poolAmounts[i].prefab, poolAmounts[i].amount, poolAmounts[i].parent);
        }
    }

    // Hàm này tự chạy mỗi khi bạn chỉnh sửa gì đó trong Inspector
    private void OnValidate()
    {
        if (poolAmounts == null) return;

        foreach (var item in poolAmounts)
        {
            // Gọi hàm cập nhật tên cho từng phần tử
            item.UpdateName();
        }
    }
}

[System.Serializable]
public class PoolAmount
{
    // Biến này để lưu tên hiển thị (ẩn đi để đỡ rối)
    [HideInInspector] public string _name;

    public int amount;
    public GameUnit prefab;
    public Transform parent;

    // Hàm cập nhật tên
    public void UpdateName()
    {
        if (prefab != null)
        {
            // Lấy PoolType từ prefab và chuyển thành chữ để hiển thị
            _name = prefab.poolType.ToString();
        }
        else
        {
            _name = "Chưa gán Prefab";
        }
    }
}
public enum PoolType
{
    None,
    Audio_Sources,
    Weapon_Axe,
    Bullet,
}