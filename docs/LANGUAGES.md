# Afrikaans, Learn Afrikaans and Learn English — the plan

Owner request, 18 October 2026: "add the Afrikaans side. Also add a learn Afrikaans and learn English.
First plan it." This replaces the "English only" rule once the owner approves the plan.

## What the family gets

1. **Each child has a home language: English or Afrikaans.** A grown-up sets it per child (setup guide
   and Children tab). The whole game then speaks, shows and teaches in that language: Bip's
   instructions, praise and hints, the letters and sounds, the words, the numbers and the stories.
   Two siblings can play in different languages on the same computer.
2. **A sixth island: the Language Island** ("Learn Afrikaans" for an English child, "Leer Engels" for
   an Afrikaans child). Bip explains in the child's home language and teaches the other language:
   everyday words first, then short phrases and simple sentences. It is a second-language island, not
   a second copy of the game: no phonics, no spelling tests, lots of listening and pointing.
3. **One narrator for everything:** Aoede (Gemini), sparkling for instructions and praise, calm and
   clean for letter sounds, in both languages.

## The three parts and what each needs

### A. Afrikaans as a home language (the big part)

The game's structure stays the same; the content gets an Afrikaans version.

| Island | What changes for Afrikaans |
| --- | --- |
| Letters & Sounds | New phonics order for Afrikaans (Grade R and Grade 1 klanke, the CAPS Afrikaans Huistaal sequence: single sounds, then klinkerverbindings like ie, oe, ou, ui, ei, aa, ee, oo, uu). New picture words per sound (a for appel, s for son…). Letter names: a, bee, see… |
| Numbers | Number words and the Block Towers / Bip's Balance sentences in Afrikaans; money already works (rand and sent). |
| Words & Spelling | Afrikaans decodable words per sound group, high-frequency words (Afrikaans "tricky words": die, en, is, het, nie…), sentences with right and wrong pictures, Word Rocket typing. Contractions and English homophones have no Afrikaans version; those levels are English only. |
| Coding | Same puzzles; only Bip's spoken instructions are translated. |
| Art | Same games; only the spoken instructions and colour and shape words are translated. |

Curriculum: Cambridge has no Afrikaans; the Afrikaans side follows **CAPS Afrikaans Huistaal**
(Grade R and Foundation Phase), mapped in `docs/CURRICULUM.md` like the Cambridge codes.

**Progress per language (decided 18 October 2026):** each language keeps its own sound and word
mastery, phonics groups and game levels (`ChildProgress.OtherLanguages`, by language folder; English's
stays where older saves have it, so nothing is migrated). Stars, stickers, the play clock and recent
games are shared. A saved language this build doesn't have (no `Content/af/language.json`) plays in
English (`Languages.Playable`).

Content lives beside the English: `Content/af/…` with the same file shapes, so every game, test and
the validator work unchanged. Spelling follows the AWS (Afrikaanse Woordelys en Spelreëls), checked
by a new `scripts/af_spelling.py` the way `sa_english.py` checks English. No slang, everyday South
African life, the same rule as English.

Rough size: about 90 instruction lines, about 250 picture words, 25 letter names, about 60 sounds,
16 sentence pairs, numbers 0–100: about 1,100 clips, like English.

### B. The Language Island (Learn Afrikaans / Learn English)

Three games, built from data lists like every other game, each in both directions:

1. **Wys my / Show me** — Bip says a word in the new language; the child taps the picture (3, then 4,
   then 6 pictures). Hint after two misses: Bip says it in the home language too.
2. **Wat is dit? / What is it?** — a picture appears; the child picks the right spoken word from two
   or three (tap each to hear it). No microphone: the child never records anything (kid-safety rule).
3. **Bip se dag / Bip's day** — a short scene (the Morning Order pictures) with a simple sentence in the
   new language ("Bip eet ontbyt", "Bip is eating breakfast"); the child finds the matching picture.

Themes, in order: greetings and family, colours, numbers to 10, animals, food, clothes, the body,
home and school, weather and seasons, then short phrases ("Ek is honger", "Dankie", "Waar is my skoen?").
About 150 words and 40 phrases per direction, reusing the 340 hand-drawn pictures.

Curriculum: CAPS First Additional Language (Afrikaans EAT for English children, English FAL for
Afrikaans children), Grade R and 1 listening and speaking outcomes.

### C. Voice

- **English:** being re-voiced with Aoede (owner decision, 18 October 2026). The recorder,
  `scripts/gemini_narrator.py` (daily free runs), is on branch `claude/aoede-narrator` and reaches `main`
  through its own pull request. Until a clip is re-recorded, the current clip plays (Chatterbox, and
  ElevenLabs for the letter sounds); CLAUDE.md says the same.
- **Afrikaans:** the same recorder with an Afrikaans style line and an Afrikaans speech check
  (Whisper's multilingual model, which knows Afrikaans), on the same free daily runs.
- **Everything is re-recorded with Aoede, letter sounds included, in both languages** (owner, 18 Oct:
  "a re-recording of everything"). Letter sounds are a calm "pure sound" take, cut at the pauses;
  Whisper can't check a pure sound, so they wait in `voice-lab/aoede_phonics/` until the owner has
  listened, and only the approved ones replace the ElevenLabs sounds (stop sounds must stay clipped).
- Cost: free on Gemini's free tier (a few weeks of daily runs for both languages); about R20 in
  total if the owner ever chooses to pay to finish faster.

## Build order (each step is its own pull request, reviewed and green before the next)

1. **English with Aoede** (running now on branch `claude/aoede-narrator`, its own pull request): every
   English clip, letter sounds included.
2. **Language plumbing:** home language per child (save file, setup guide, Children tab), content
   and audio folders per language, the game loading by language, on-screen text in both languages
   (about a dozen strings plus the parent area), tests. English plays exactly as before.
3. **Afrikaans content, part 1:** Letters & Sounds (klanke, picture words), Numbers, the instruction
   lines; Afrikaans spelling check; CAPS mapping.
4. **Afrikaans content, part 2:** Words & Spelling, sentences, Coding and Art lines.
5. **Afrikaans voice:** record everything with Aoede on the daily runs; owner listens to the letter
   sounds and a sample of each island.
6. **Language Island:** the three games, both directions, word and phrase lists, voice.
7. **Letter sounds with Aoede** (both languages): recorded on the daily runs with everything else;
   the owner approves them by ear before they go into the game.
8. Family test on both computers.

## What the owner decides or checks

- **A fluent Afrikaans reader checks the Afrikaans lists** (words, sentences, instructions) before
  they are recorded: the owner or a family member. Claude writes them; a person signs them off.
- Island name and where it sits on the map (a sixth island near the middle, unlocked from the start).
- Whether a child can switch home language, or only a grown-up (suggested: only behind the parent gate).
- The listening tests: Afrikaans letter sounds, a sample per island, the Language Island words.

## What stays the same

All kid-safety rules (no microphone, no network except updates, no timers), the art style and
font, the play-time breaks, free choice, rewards, and the South African life rule in both languages.
