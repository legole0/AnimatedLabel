using Godot;
using Godot.Collections;

namespace AnimatedLabel;

#if TOOLS
[Tool]
#endif
[GlobalClass] public partial class AnimatedFont : Resource
{
    [Export] public SpriteFrames SpriteFrames;
    [ExportGroup("Letter Options"), Export] private Dictionary<string,string> _characterAliases = new();
    [Export] public Dictionary<string,Vector2> Offset = new();
    [Export] public Dictionary<string,Vector2> Advance = new();
}