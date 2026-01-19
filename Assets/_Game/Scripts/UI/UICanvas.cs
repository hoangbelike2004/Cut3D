using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UICanvas : MonoBehaviour
{
    [SerializeField] bool isdestroyOnClose = false;
    // goi truoc khi canvas duoc active
    private void Awake()//xu li man hinh tai tho
    {
        //RectTransform rectTransform = GetComponent<RectTransform>();
        //float radio = (float)Screen.width / (float)Screen.height;
        //if (radio > 2.1f)
        //{
        //    Vector2 leftbottom = rectTransform.offsetMin;
        //    Vector2 righttop = rectTransform.offsetMax;

        //    leftbottom.y = 0f;
        //    righttop.y = -100f;

        //    rectTransform.offsetMin = leftbottom;
        //    rectTransform.offsetMax = righttop;
        //}
    }

    public virtual void SetUp()
    {

    }

    //goi sau khi duoc active
    public virtual void Open()
    {
        CancelInvoke();
        gameObject.SetActive(true);
    }

    //dong ui sau time(s)
    public virtual void Close(float time)
    {
        Invoke(nameof(CloseDirectly), time);
    }

    //dong ui sau time = 0
    public virtual void CloseDirectly()
    {
        if (isdestroyOnClose)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }

    }
}