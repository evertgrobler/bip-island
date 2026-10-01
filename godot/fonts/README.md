# Godot-only fonts

Noto Color Emoji (`NotoColorEmoji.ttf`, the "noflags" build from googlefonts/noto-emoji), by Google,
under the SIL Open Font License 1.1 (`OFL.txt`). The Godot game uses it for the stand-in emoji
pictures, so they look the same on Mac and Windows. Kept out of `Resources/Fonts` so the Swift app
(which registers that whole folder) is unchanged.

Atkinson Hyperlegible is copied in from `Resources/Fonts` by `scripts/godot/prepare_assets.py`.
