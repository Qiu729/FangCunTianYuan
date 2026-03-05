using UnityEngine;

public class Slot : MonoBehaviour {
    /// <summary>
    /// 通过检查是否有 DraggableItem 组件来判断槽位是否为空。
    /// 因为 Slot Prefab 本身可能包含背景图等子对象，不能只判断 childCount。
    /// </summary>
    public bool IsEmpty {
        get { return GetComponentInChildren<DraggableItem>() == null; }
    }
}
