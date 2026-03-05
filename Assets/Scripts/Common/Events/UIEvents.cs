using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class UIEvents {
    /// <summary>
    /// 子物体更新，通知父节点更新参数
    /// </summary>
    public delegate void LayoutChanged();
}
