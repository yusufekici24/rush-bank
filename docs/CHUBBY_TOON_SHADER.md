# Chubby Toon URP Shader

`Assets/Shaders/ChubbyToonURP.shader` is a lightweight URP shader for the cozy RushBank character style.

## Visual Goal

- Soft warm diffuse lighting
- Smooth gradient toon banding rather than harsh anime-style cell shading
- Fresnel rim light for fluffy, rounded, toy-like silhouettes
- Single main texture support
- Vertex color tint support
- Mobile-friendly GLES3/Vulkan direction

## Material Controls

- `Base Map`: optional single texture.
- `Base Color`: global tint.
- `Vertex Color Strength`: blends mesh vertex color into the base color.
- `Warm Light Color`: pastel/yellow highlight tone.
- `Soft Shadow Color`: cool/mint-blue shadow tone.
- `Toon Smoothness`: wider values create softer diffuse rolloff.
- `Shadow Lift`: keeps dark areas warm and readable on mobile screens.
- `Rim Color`: fluffy edge highlight color.
- `Rim Power`: controls how tight the Fresnel edge is.
- `Rim Intensity`: rim brightness.
- `Rim Threshold`: trims weak rim light.

## Mobile Notes

- Uses one texture sample.
- Uses `half` precision for color math.
- Avoids normal maps, additional texture lookups, outlines, geometry passes, and screen-space effects.
- Keeps the forward pass simple for Android GLES3/Vulkan.
- Shadow caster pass is included for URP lighting, but can be removed for very low-end targets.

## URP Setup

This shader requires the Universal Render Pipeline package and a URP pipeline asset assigned in Unity. The current repo is still a lightweight Unity project shell, so install/configure URP before assigning this shader to production materials.

Recommended starting values:

```text
Base Color: warm cream or pastel character color
Warm Light Color: #FFE09E
Soft Shadow Color: #92AFC6
Toon Smoothness: 0.45
Shadow Lift: 0.32
Rim Color: mint-white
Rim Power: 3
Rim Intensity: 0.45
Rim Threshold: 0.2
```

## Art Direction

For the RushBank "tontiş" characters:

- Keep silhouettes round and readable.
- Prefer warm highlights and lifted shadows.
- Use pastel vertex color variation for cheeks, ties, glasses, folders, and money bags.
- Avoid high-frequency texture noise; clean low-poly surfaces will read better on mobile.
