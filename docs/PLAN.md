# Kids Learning Game for Mac — Game Plan

Plan agreed 1 October 2026. Live, editable copy: https://claude.ai/code/artifact/0dad7f6b-06c0-4575-b3bd-0afec094b6c0

A full-screen native macOS game for ages 4–8 where kids explore four islands — Letters & Sounds, Numbers, Words & Spelling, and Coding — guided by a friendly character who speaks every instruction, so no reading is needed to play.

## The game at a glance

The child picks their profile, lands on an island map, and a companion character (working idea: a small robot called Bip) suggests where to go next. Each island unlocks new spots as skills are mastered, and every win adds a sticker to a sticker book.

| Island | Core skill | Ages 4–5 | Ages 6–8 |
| --- | --- | --- | --- |
| Letters & Sounds | Phonics: letter shapes and their sounds | First 12 sounds (s a t p i n m d g o c k), tracing, sound hunts | All 26 letters, then digraphs (sh, ch, th, ck, ee, oo) |
| Numbers | Counting, number sense, early sums | Count to 10, match quantity to numeral, more / less | Count to 100, number bonds to 10 and 20, add and subtract with objects |
| Words & Spelling | Blending and segmenting | Blend 3-sound words (c-a-t), picture-word match | Build words from letter tiles, tricky words, short sentences |
| Coding | Sequencing and logic | Put 3–4 steps in order, arrow paths | Loops, simple if-then rules, debugging broken paths |

A session runs about 15–20 minutes: a warm-up the child already knows, two or three new activities, one free-play reward, then a gentle “see you tomorrow”. No timers, no lives, no losing: a wrong answer gets a soft sound, a second try, then a spoken hint.

## Who it's for

The game starts each child at a level set by age, then moves them by mastery: three correct in a row moves a skill up, two misses in a row drops back a step. A 4-year-old and an 8-year-old can share the Mac with separate profiles.

- **Ages 4–5:** everything spoken, no text needed to play. Big click targets (at least 120 pt), drag-and-drop instead of typing, short activities (2–3 minutes each).
- **Ages 6–8:** reading starts to replace pictures, keyboard typing for spelling, longer puzzles, and the first real coding concepts.
- **Small hands on a Mac:** a trackpad is hard for 4-year-olds. Design for a plain USB or Bluetooth mouse, make every action a single click or a forgiving drag, and let any key on the keyboard trigger “play that sound again”.
- **Letter tracing** works with a mouse only if the tolerance is wide; treat it as an optional activity, not a gate.

## Letters & Sounds

Letters are taught by their sound first, never by name or song: the letter s is “sss”, not “ess”. The order follows synthetic phonics (the Jolly Phonics groups used in many UK and South African schools), so kids can blend real words after the first six sounds.

| Group | Sounds | First words it unlocks |
| --- | --- | --- |
| 1 | s a t p i n | sat, tap, pin, nap, sit |
| 2 | m d g o c k | dog, cat, mop, kid |
| 3 | ck e u r | duck, red, sun, run |
| 4 | h b f l | hat, bed, fun, leg |
| 5 | j v w x y z qu | jam, van, web, box |
| 6 (ages 6–8) | sh ch th ng ee oo ai | ship, chip, moon, rain |

Activities for each sound:

1. **Meet the sound:** the letter bounces in, Bip says the pure sound three times, then a picture word (s → sun).
2. **Sound hunt:** “Find the one that starts with mmm” — click the right picture out of three.
3. **Pop the letter:** bubbles with letters float up; pop the one that says the sound you hear.
4. **Trace it:** follow the letter's path with the mouse; sparkles follow the cursor.
5. **Letter names (later):** once all 26 sounds are known, a separate activity adds “this letter is called ess, and it says sss”.

## Voice & phonics audio

Use ElevenLabs for every spoken word, instruction and praise line, and test the 44 pure phonemes separately because text-to-speech tends to say letter names (“tee”) or add an “uh” (“tuh”) to short sounds. A first test clip (Alice, a British educator voice, with the v3 model) is in your ElevenLabs Flows from 1 October; listen to whether the t comes out clean.

**Voice:** Alice (British educator, works on your current ElevenLabs plan). The kid-specific library voices need the Creator tier. A custom voice designed in ElevenLabs could give a South African English accent if Alice sounds too foreign.

**How to write sounds so the voice says them right:**

| Sound type | Letters | Write it as | Avoid |
| --- | --- | --- | --- |
| Stretchy (can be held) | s m f n l r v z | “sssss”, “mmmmm”, “fffff” | “ess”, “em” |
| Short vowels | a e i o u | “a, as in apple”, then trim to the vowel | “ay”, “ee” (letter names) |
| Bouncy / stop | t p k c b d g | Generate the word (tap, pig) and trim to the first \~100 ms | “tuh”, “puh” |
| Digraphs | sh ch th | “shhhh”, “ch, as in chip” | “ess aitch” |

**Fallback:** if the stop sounds still come out wrong after trimming, record those 10–12 clips yourself or with a voice artist, and keep ElevenLabs for everything else. Kids will hear each sound hundreds of times, so it has to be right.

**Audio file list** (about 450 short clips; one .m4a per line):

- `snd_s`, `snd_a` … — 44 pure sounds
- `word_sun`, `word_cat` … — about 150 picture and spelling words
- `num_1` … `num_20`, plus tens to 100
- `vo_find_the_sound`, `vo_drag_the_letter`, `vo_help_bip_get_home` … — about 80 instructions
- `praise_01` … `praise_30` and `hint_01` … `hint_20` — varied so it never gets repetitive

Keep the script as a spreadsheet (file name, text to speak, notes) so all clips can be generated, checked and re-done in batches.

## Numbers, Words & Spelling

Both islands use real objects on screen before symbols: count the apples, then see the 3.

**Numbers Island**

| Activity | Ages 4–5 | Ages 6–8 |
| --- | --- | --- |
| Count & tap | Tap each of up to 10 ducks as Bip counts aloud | Count by 2s, 5s and 10s to 100 |
| Quick look | Dot patterns on dice flash for 2 seconds: how many? | Ten-frames up to 20 |
| Match it | Drag the numeral onto the matching group | Match a sum (3 + 4) to its answer |
| More or less | Which basket has more? | Number line jumps: 8 take away 3 |
| Bonds | — | Fill the ten-frame: 6 and how many make 10? |

**Words & Spelling Island**

| Activity | Ages 4–5 | Ages 6–8 |
| --- | --- | --- |
| Sound buttons | Click each letter in c-a-t to hear its sound, then the whole word | Same with 4–5 sound words (frog, ship) |
| Build a word | Drag 3 letter tiles into slots to match the picture | Type the word on the keyboard after hearing it |
| Tricky words | — | Words that don't sound out (the, said, was, you) as a memory game |
| Silly sentences | — | Read “the cat sat on a hat” and pick the matching picture |

Spelling follows UK / South African English (colour, mum, favourite).

## Coding Island

Kids help Bip get home across a grid by snapping picture blocks into a command strip and pressing Go. No words are needed: arrows, a repeat block with a number, and picture conditions. Each tier has about 10 levels.

| Tier | Concept | Example puzzle | Ages |
| --- | --- | --- | --- |
| 1. Order | Sequencing | Put “wake up, brush teeth, eat breakfast” cards in order | 4–5 |
| 2. Paths | Commands | Drag 3–6 arrows to walk Bip to the battery | 4–6 |
| 3. Fix it | Debugging | Bip's path is wrong and crashes into a rock: swap one arrow | 5–7 |
| 4. Repeat | Loops | Walk 6 steps using “repeat 3 × (→ →)” in fewer blocks | 6–8 |
| 5. If-then | Conditions | “If puddle, jump” so the same strip works on two maps | 7–8 |
| 6. Build | Free play | Program Bip to draw a shape or dance to music | 6–8 |

Pressing Go plays the program one step at a time with a highlight on the current block, so kids see exactly where a mistake happens. Fewer blocks earns a gold star, but any working solution passes.

## Native Mac build

Build it in Swift: SwiftUI for menus and the parent area, SpriteKit for the game scenes, and everything stored on the Mac with no internet, ads or data collection.

| Part | Choice | Why |
| --- | --- | --- |
| Language | Swift, Xcode | Apple's native stack, smallest app, smoothest animation |
| Game scenes | SpriteKit | 2D sprites, physics, particle sparkles, built into macOS |
| Menus, parent area | SwiftUI | Fast to build standard screens |
| Audio | AVFoundation, bundled .m4a files | Instant playback, works offline |
| Progress & profiles | SwiftData | Local, up to 4 child profiles |
| Minimum macOS | 14 Sonoma | Covers most Macs from 2018 on |

**Full screen with a kid lock:** the app opens straight into full screen and uses macOS presentation options to hide the Dock and menu bar and block Cmd-Tab and Cmd-Q, so a child can't wander into other apps.

**Parent gate:** hold the Esc key for 3 seconds, then answer an adult question (e.g. “type 7 × 8”) to reach settings or quit. Settings cover profiles, a daily play limit, volume and voice speed, and a simple progress view per skill.

**Distribution:** either the Mac App Store, or a signed and notarised download from your own site (needs a paid Apple Developer account, US$99 a year, in both cases).

## Build phases

Prove the hardest part first: if the phonics audio and the core click-and-hear loop feel right with real kids, the rest is content.

| Phase | What gets built | Done when |
| --- | --- | --- |
| 0. Sound test | Script for group 1 sounds (s a t p i n) generated in ElevenLabs, trimmed, checked | Every sound is clean, no letter names or “uh” |
| 1. Prototype | Full-screen app, Bip, Letters Island with group 1 (meet, hunt, pop) | A 4-year-old plays 10 minutes without help |
| 2. All islands | Groups 2–5, Numbers, Words, Coding tiers 1–3, profiles, progress | Each island has 15+ activities and mastery tracking |
| 3. Polish | Art, music, sticker book, parent gate and dashboard, ages 6–8 content, Coding tiers 4–6 | Kids ask to play it again |
| 4. Release | Signing, notarisation or App Store review, website page | Installs cleanly on a fresh Mac |

## Decisions

- [x] Language: English only for now.
- [x] Audience: your own family first, to see how it plays and feels; no App Store needed yet, so free Xcode signing is enough.
- [x] Build: entirely with Claude Code, in a new GitHub repository.
- [x] Art style: hand-drawn.
- [x] Voice: a female South African English voice, designed in ElevenLabs (replaces Alice).
- [x] Companion: a robot (Bip, working name).
- [x] Repository: private, named bip-island.
- [x] Workflow: cloud-driven. Claude Code cloud sessions write the code, GitHub Actions builds it on a Mac runner, and the app updates itself on your Mac via Sparkle. No Xcode needed on your Mac.
