using Godot;

namespace SwitchTheSpire;

internal sealed class NoTooltipButton : Button
{
    public override string _GetTooltip(Vector2 atPosition) => string.Empty;
}
