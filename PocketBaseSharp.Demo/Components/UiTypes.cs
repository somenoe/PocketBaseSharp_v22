using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Demo.Components;

public enum BadgeColor
{
    Gray,
    Primary,
    Info,
    Success,
    Warning,
    Pink,
}

public enum ButtonColor
{
    Primary,
    Gray,
    Dark,
    Red,
}

public enum ButtonVariant
{
    Default,
    Outline,
}

public enum ButtonSize
{
    Default,
    Small,
}

public enum AlertColor
{
    Info,
    Warning,
    Success,
    Failure,
}

public enum TabVariant
{
    Default,
    Pills,
}

public enum InputBehavior
{
    OnChange,
    OnInput,
}

public enum TooltipPlacement
{
    Top,
    Right,
    Bottom,
    Left,
}

public enum DropdownPlacement
{
    Bottom,
}

public enum DrawerPosition
{
    Left,
    Right,
}

public abstract class IconBase : ComponentBase
{
    [Parameter]
    public string? Class { get; set; }

    protected virtual string ViewBox => "0 0 24 24";

    protected virtual string[] Paths => [];

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "svg");
        builder.AddAttribute(1, "xmlns", "http://www.w3.org/2000/svg");
        builder.AddAttribute(2, "viewBox", ViewBox);
        builder.AddAttribute(3, "fill", "none");
        builder.AddAttribute(4, "stroke", "currentColor");
        builder.AddAttribute(5, "stroke-width", "1.8");
        builder.AddAttribute(6, "stroke-linecap", "round");
        builder.AddAttribute(7, "stroke-linejoin", "round");
        builder.AddAttribute(8, "aria-hidden", "true");
        builder.AddAttribute(9, "class", string.IsNullOrWhiteSpace(Class) ? "h-5 w-5" : Class);

        var seq = 10;
        foreach (var path in Paths)
        {
            builder.OpenElement(seq++, "path");
            builder.AddAttribute(seq++, "d", path);
            builder.CloseElement();
        }

        builder.CloseElement();
    }
}

public sealed class BarsIcon : IconBase
{
    protected override string[] Paths => ["M4 7h16", "M4 12h16", "M4 17h16"];
}

public sealed class HomeIcon : IconBase
{
    protected override string[] Paths => ["m3 10.5 9-7 9 7", "M5.25 9.75V20h13.5V9.75", "M9.75 20v-5.25h4.5V20"];
}

public sealed class UserIcon : IconBase
{
    protected override string[] Paths => ["M15.75 7.5a3.75 3.75 0 1 1-7.5 0 3.75 3.75 0 0 1 7.5 0Z", "M4.5 20.118a7.5 7.5 0 0 1 15 0"];
}

public sealed class HeartIcon : IconBase
{
    protected override string[] Paths => ["m12 20.25-.32-.29C6.2 14.97 3 12.04 3 8.44 3 5.74 5.1 3.75 7.67 3.75c1.45 0 2.84.67 3.73 1.8.89-1.13 2.28-1.8 3.73-1.8C17.9 3.75 20 5.74 20 8.44c0 3.6-3.2 6.53-8.68 11.52L12 20.25Z"];
}

public sealed class GearIcon : IconBase
{
    protected override string[] Paths => ["M10.325 4.317a1.724 1.724 0 0 1 3.35 0l.113.4a1.724 1.724 0 0 0 2.591.996l.36-.21a1.724 1.724 0 0 1 2.288.633 1.724 1.724 0 0 1-.633 2.288l-.36.21a1.724 1.724 0 0 0-.83 1.494c0 .538.304 1.03.83 1.334l.36.21a1.724 1.724 0 1 1-1.655 3.025l-.36-.21a1.724 1.724 0 0 0-2.59.997l-.114.399a1.724 1.724 0 1 1-3.35 0l-.113-.4a1.724 1.724 0 0 0-2.591-.996l-.36.21a1.724 1.724 0 1 1-1.655-3.025l.36-.21a1.724 1.724 0 0 0 .83-1.494 1.724 1.724 0 0 0-.83-1.494l-.36-.21A1.724 1.724 0 0 1 6.6 5.503l.36.21a1.724 1.724 0 0 0 2.59-.997l.114-.399Z", "M15 12a3 3 0 1 1-6 0 3 3 0 0 1 6 0Z"];
}

public sealed class ArrowRightToBracketIcon : IconBase
{
    protected override string[] Paths => ["M10.5 17.25 15.75 12 10.5 6.75", "M15.75 12H3", "M12 3.75h4.5A2.25 2.25 0 0 1 18.75 6v12a2.25 2.25 0 0 1-2.25 2.25H12"];
}

public sealed class FileIcon : IconBase
{
    protected override string[] Paths => ["M14.25 3H6.75A2.25 2.25 0 0 0 4.5 5.25v13.5A2.25 2.25 0 0 0 6.75 21h10.5a2.25 2.25 0 0 0 2.25-2.25V8.25L14.25 3Z", "M14.25 3v5.25h5.25", "M8.25 12h7.5", "M8.25 15.75h7.5"];
}

public sealed class UsersIcon : IconBase
{
    protected override string[] Paths => ["M15 19.128a9.38 9.38 0 0 0-6 0", "M12 15.75a3.75 3.75 0 1 0 0-7.5 3.75 3.75 0 0 0 0 7.5Z", "M18.75 18.66a9.712 9.712 0 0 0-3.67-2.2", "M16.5 7.5a3 3 0 1 1 0 6", "M5.25 18.66a9.712 9.712 0 0 1 3.67-2.2", "M7.5 7.5a3 3 0 1 0 0 6"];
}

public sealed class GridIcon : IconBase
{
    protected override string[] Paths => ["M4.5 4.5h6v6h-6z", "M13.5 4.5h6v6h-6z", "M4.5 13.5h6v6h-6z", "M13.5 13.5h6v6h-6z"];
}

public sealed class DatabaseIcon : IconBase
{
    protected override string[] Paths => ["M12 4.5c4.556 0 8.25 1.511 8.25 3.375S16.556 11.25 12 11.25 3.75 9.739 3.75 7.875 7.444 4.5 12 4.5Z", "M3.75 7.875v8.25C3.75 17.989 7.444 19.5 12 19.5s8.25-1.511 8.25-3.375v-8.25", "M3.75 12c0 1.864 3.694 3.375 8.25 3.375S20.25 13.864 20.25 12"];
}

public sealed class BellIcon : IconBase
{
    protected override string[] Paths => ["M14.857 17.082a23.848 23.848 0 0 1-5.714 0", "M18 8.25a6 6 0 1 0-12 0c0 6.372-2.25 7.5-2.25 7.5h16.5S18 14.622 18 8.25", "M13.73 21a2.25 2.25 0 0 1-3.46 0"];
}

public sealed class ExclamationTriangleIcon : IconBase
{
    protected override string[] Paths => ["M12 9v4.5", "M12 17.25h.008v.008H12v-.008Z", "M10.29 3.86 1.82 18a2.25 2.25 0 0 0 1.93 3.375h16.5A2.25 2.25 0 0 0 22.18 18L13.71 3.86a2.25 2.25 0 0 0-3.42 0Z"];
}

public sealed class InfoCircleIcon : IconBase
{
    protected override string[] Paths => ["M12 16.5v-4.5", "M12 8.25h.008v.008H12V8.25Z", "M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z"];
}

public sealed class CheckCircleIcon : IconBase
{
    protected override string[] Paths => ["m9 12 2.25 2.25L15 10.5", "M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z"];
}

public sealed class ClipboardArrowIcon : IconBase
{
    protected override string[] Paths => ["M15.75 3.75H8.25A2.25 2.25 0 0 0 6 6v12a2.25 2.25 0 0 0 2.25 2.25h7.5A2.25 2.25 0 0 0 18 18V6a2.25 2.25 0 0 0-2.25-2.25Z", "M9.75 3.75h4.5A1.5 1.5 0 0 1 15.75 5.25v0A1.5 1.5 0 0 1 14.25 6.75h-4.5a1.5 1.5 0 0 1-1.5-1.5v0a1.5 1.5 0 0 1 1.5-1.5Z", "M12 10.5v5.25", "m9.75 13.5 2.25 2.25 2.25-2.25"];
}

public sealed class RocketIcon : IconBase
{
    protected override string[] Paths => ["M15.59 4.41c2.61 2.61 2.61 6.84 0 9.45L11.25 18.2l-5.45 1.05 1.05-5.45 4.34-4.34c2.61-2.61 6.84-2.61 9.45 0Z", "M13.5 6.75 17.25 10.5", "M6.75 17.25c-.75 0-2.25 0-3 2.25 2.25-.75 2.25-2.25 2.25-3"];
}

public sealed class LockIcon : IconBase
{
    protected override string[] Paths => ["M16.5 10.5V7.875a4.5 4.5 0 1 0-9 0V10.5", "M6.75 10.5h10.5A2.25 2.25 0 0 1 19.5 12.75v6A2.25 2.25 0 0 1 17.25 21H6.75A2.25 2.25 0 0 1 4.5 18.75v-6A2.25 2.25 0 0 1 6.75 10.5Z"];
}

public sealed class SearchIcon : IconBase
{
    protected override string[] Paths => ["m21 21-4.35-4.35", "M10.5 18a7.5 7.5 0 1 1 0-15 7.5 7.5 0 0 1 0 15Z"];
}
