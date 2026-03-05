using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public static class LifeCycleEvents {
    /// <summary>
    /// 通知订阅者取消订阅
    /// </summary>
    public delegate void Destroyed();
    public delegate void DestroyedWithTarget<T>(T target); // 支持带参版
}
