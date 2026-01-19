using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    Dictionary<System.Type, UICanvas> canvasActives = new Dictionary<System.Type, UICanvas>();//dung de luu uicanvas
    Dictionary<System.Type, UICanvas> canvasPrefabs = new Dictionary<System.Type, UICanvas>();//noi chua prefab
    [SerializeField] Transform parent;
    private void Awake()
    {
        //Load UI tu resource
        UICanvas[] canvas = Resources.LoadAll<UICanvas>("UI/");
        for (int i = 0; i < canvas.Length; i++)
        {
            canvasPrefabs.Add(canvas[i].GetType(), canvas[i]);
        }
    }

    //mo canvas
    public T OpenUI<T>() where T : UICanvas
    {
        T canvas = GetUI<T>();
        canvas.SetUp();
        canvas.Open();
        return canvas;
    }

    //dong canvas sau time
    public void CloseUI<T>(float time) where T : UICanvas
    {
        if (IsLoaded<T>())
        {
            canvasActives[typeof(T)].Close(time);
        }
    }


    //dong sau 0s
    public void CloseUIDirectly<T>() where T : UICanvas
    {
        if (IsLoaded<T>())
        {
            canvasActives[typeof(T)].CloseDirectly();
        }
    }


    //kiem tra xem UI da duoc create hay ch
    public bool IsLoaded<T>() where T : UICanvas
    {
        //tra ve neu ton tai canvas cos kieu la T va no khac null
        return canvasActives.ContainsKey(typeof(T)) && canvasActives[typeof(T)] != null;
    }

    //kiem tra xem ui da duoc active hay chua
    public bool IsOpen<T>() where T : UICanvas
    {
        return IsLoaded<T>() && canvasActives[typeof(T)].gameObject.activeSelf;
    }


    // lay UI
    public T GetUI<T>() where T : UICanvas
    {
        if (!IsLoaded<T>())
        {
            T prefab = GetUIPrefab<T>();
            T canvas = Instantiate(prefab, parent);
            canvasActives[typeof(T)] = canvas;
        }

        return canvasActives[typeof(T)] as T;
    }

    private T GetUIPrefab<T>() where T : UICanvas
    {

        return canvasPrefabs[typeof(T)] as T;

    }
    // dong tat ca cac ui
    public void CloseAll()
    {
        foreach (var canvas in canvasActives)
        {
            if (canvas.Value != null && canvas.Value.gameObject.activeSelf == true)
            {
                canvas.Value.Close(0);
            }
        }
    }
}