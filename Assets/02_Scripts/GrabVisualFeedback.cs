using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>六个 XRI 事件在 Inspector 中连接到这里；颜色反映当前交互状态。</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Renderer), typeof(XRGrabInteractable))]
public sealed class GrabVisualFeedback : MonoBehaviour
{
    public Color normalColor = Color.cyan;
    public Color hoverColor = Color.yellow;
    public Color selectColor = Color.green;
    public Color activateColor = Color.magenta;

    private Renderer targetRenderer;
    private XRGrabInteractable grab;
    private MaterialPropertyBlock properties;
    private bool isActivated;
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");

    private void Awake()
    {
        targetRenderer = GetComponent<Renderer>();
        grab = GetComponent<XRGrabInteractable>();
        properties = new MaterialPropertyBlock();
        ApplyColor(normalColor);
    }

    public void OnHoverEntered() { RefreshColor(); }
    public void OnHoverExited() { RefreshColor(); }
    public void OnSelectEntered() { RefreshColor(); }

    public void OnSelectExited()
    {
        isActivated = false;
        RefreshColor();
    }

    public void OnActivated()
    {
        isActivated = true;
        RefreshColor();
    }

    public void OnDeactivated()
    {
        isActivated = false;
        RefreshColor();
    }

    private void RefreshColor()
    {
        if (grab == null) return;
        Color next = isActivated && grab.isSelected ? activateColor
            : grab.isSelected ? selectColor
            : grab.isHovered ? hoverColor : normalColor;
        ApplyColor(next);
    }

    private void ApplyColor(Color value)
    {
        if (targetRenderer == null) return;
        // 只改本物体的渲染属性，不修改共享材质或生成泄漏的材质实例。
        targetRenderer.GetPropertyBlock(properties);
        properties.SetColor(BaseColor, value);
        properties.SetColor(ColorProperty, value);
        targetRenderer.SetPropertyBlock(properties);
    }

    private void OnDisable()
    {
        isActivated = false;
        if (properties != null) ApplyColor(normalColor);
    }
}
