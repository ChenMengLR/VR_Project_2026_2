using UnityEngine;

/// <summary>第 2 周旋转练习；进入抓取阶段后关闭此组件。</summary>
public sealed class RotateObject : MonoBehaviour
{
    [Tooltip("每秒绕本地 Y 轴旋转的角度。")]
    public float rotationSpeed = 45f;

    private void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
    }
}
