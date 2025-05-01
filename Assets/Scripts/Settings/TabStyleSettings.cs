using UnityEngine;

[CreateAssetMenu(fileName = "TabStyleSettings", menuName = "ClockApp/Tab Style Settings", order = 1)]
public class TabStyleSettings : ScriptableObject
{
    // a very simple settings for styling the tabs

    [Header("Font Size")]
    public float SelectedFontSize = 36f;
    public float DeselectedFontSize = 30f;

    [Header("Color")]
    public Color SelectedColor = Color.white;
    public Color DeselectedColor = Color.gray;

}