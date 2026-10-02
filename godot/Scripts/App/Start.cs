using System;
using BipIsland.Game;
using Godot;

namespace BipIsland.App;

/// <summary>The first scene: hands over to the coordinator, which opens "Who's playing?" or the map.</summary>
public partial class Start : Node
{
    public override void _Ready()
    {
        // A dev page or screen preview asked for with --bip-scene opens instead (Boot switches to it).
        if (Array.IndexOf(Boot.UserArgs, "--bip-scene") >= 0) return;
        Callable.From(() => GameCoordinator.Instance.Start()).CallDeferred();
    }
}
