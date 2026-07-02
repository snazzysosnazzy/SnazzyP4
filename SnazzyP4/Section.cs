using System;
using System.Numerics;

namespace SnazzyP4;

/// <summary>
/// A repositionable UI block that the hosting window draws at a given pixel scale.
/// </summary>
/// <param name="Id">The stable identifier used for configuration keys and window ids.</param>
/// <param name="Name">The display name shown in settings and as the detached window title.</param>
/// <param name="DefaultOffset">The default windowed offset, also used as the detached position fallback.</param>
/// <param name="Draw">The delegate that emits the block's ImGui widgets at the given pixel scale.</param>
/// <param name="HasButtons">Whether the block contains interactive buttons rather than pure text.</param>
public sealed record SectionDef(
    string Id,
    string Name,
    Vector2 DefaultOffset,
    Action<float> Draw,
    bool HasButtons = false);
