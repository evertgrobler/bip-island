# Bip Island: rules for helper AI models (OpenCode and others)

You are a helper on Bip Island, a learning game for children aged 4–10 (Godot 4.7, C#). A lead session
gives you one small task at a time and checks your work. Do exactly that task and nothing else.

**The full rules are in `CLAUDE.md`. Read it before you change anything.** The ones helpers break most:

## Words and spelling

- **South African English, always.** British spelling: colour, favourite, realise, grey, centre, maths,
  practise (verb), mum. Never American spelling (color, favorite, realize, gray, center, math, mom).
- South African words: biscuit (not cookie), sweets (not candy), takkies, cooldrink, chips, truck,
  cellphone, dustbin, Grade R. No slang. Money is rand and cents.
- Say **"child"**, never "kid", in text, code comments and docs.
- `scripts/sa_english.py` lists the banned words; the validator fails on any of them.

## Content

- **All game content lives in `Content/` as JSON** (words, sounds, numbers, levels, stories). Never put
  words, numbers or levels in C# code.
- Afrikaans content lives in `Content/af/` and follows the same shape as the English file it overrides.
- After any content change, run `python3 scripts/validate_content.py` (and `--lang af` for Afrikaans).
  It must pass. Don't change the validator to make it pass.
- Audio file names follow the contract in `CLAUDE.md` (`word_<word>`, `snd_<sound>`, `vo_<key>`, plain
  ASCII). Never add, rename or delete audio files unless the task says so.

## Child safety (never change these unless the task is about them)

- No timers, countdowns, lives or "game over" in games. Wrong answer: soft sound, retry, hint after 2 misses.
- Don't touch the child lock (`godot/Scripts/App/KidLock.cs`, `BipCore.KidLockRules`), the parent gate,
  play-time breaks or the updater.
- No network access, ads, analytics or data collection.

## Code

- Game logic goes in `godot/BipCore` (plain C#, no Godot) with xUnit tests in `godot/BipCore.Tests`.
  Run `dotnet test godot/BipCore.Tests` after changing it.
- Match the style of the code around you. Keep changes small.
- Font is Atkinson Hyperlegible only (`Fonts.Letters`, `Fonts.Regular`, `Fonts.Bold`).

## Git and safety

- Work only on the branch you were given. **Never push, never commit to `main` or to `claude/*`
  branches**, and never merge anything. The lead session commits and opens pull requests.
- The repository is public: never write keys, tokens, passwords, `.env` files or family details.
- Don't edit `CLAUDE.md`, `AGENTS.md`, `docs/STATUS.md`, `.github/` or `scripts/ci/`.

## When you finish

Reply with: the files you changed, the commands you ran and whether they passed, and anything you were
unsure about. If the task is unclear or would break a rule above, stop and say so instead of guessing.
