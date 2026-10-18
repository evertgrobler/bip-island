# Bip Island voice plan: one narrator for a 5-year-old and a 10-year-old

> **Approved by the owner, 18 October 2026** ("Voice plan is good"), with the recommended answers: the Big
> voice from age 7, one narrator for boys and girls, no per-child override yet. Built in `BipCore.VoiceBands`;
> the Afrikaans Big lines wait for a fluent reader's check before they're recorded.

Written 18 October 2026 (SAST). A plan only: nothing has been recorded, changed or committed.

## The short version (for the owner)

- **Keep one narrator, Aoede, for everyone, boys and girls, English and Afrikaans.** What makes a voice
  feel "babyish" or "cool" is *how* she speaks and *what* she says, not whether she's a man or a woman.
- **Most of the game stays one recording for everyone.** Letter sounds, words, numbers, sentences and
  game instructions are the same for a 5-year-old and a 10-year-old. That's about 2,200 clips, and
  they're already being recorded.
- **Only Bip's "personality" lines get a second, older version:** praise ("Well done!"), hints,
  island welcomes, "level up", Bip charging and "see you tomorrow". That's **30 extra lines per
  language, 60 in total: under one day of free recording.**
- **Two voice bands, picked by the child's age** (set in their profile): **Little (ages 3 to 6)**
  hears today's bubbly, sparkling Aoede; **Big (ages 7 to 10)** hears the same Aoede, upbeat and
  friendly but relaxed, quicker and never sing-song. A 9-year-old who is still learning to read gets
  the Big voice with the easy content, so they never feel talked down to.
- **One cheap change for everyone:** game instructions and sentences are recorded "friendly and clear"
  rather than "bubbly", so they suit every age. Praise and welcomes for the little ones keep the full
  sparkle you liked.
- **Boys and girls:** no separate versions. Praise is about thinking, listening and trying, with a
  mix of themes (space, animals, building, sport, cooking, nature), never "princess", "champ",
  "good girl" or "good boy". The profile has no boy/girl field and doesn't need one.
- **A male voice isn't worth it now:** it would double the recording (about two more weeks of free
  runs) and break the "one narrator" rule, and it wouldn't fix what older children actually notice.
  If you're curious, one free request per voice lets you hear a test.
- **Order:** praise, hints and instructions first (heard every minute), then numbers and words, then
  stories, then the long list of number sentences last (the game already joins those from pieces).
  The whole game, both languages plus the Big lines, is about **two weeks** of daily free runs.

Questions for you are at the end (section 8).

---

## 1. Inventory: what there is to record

Counted with a script from `Content/asset_manifest.json`, `Content/af/asset_manifest.json`,
`audio/script.csv` and `audio/af/script.csv` on the `claude/language-island` branch, and
`voice-lab/aoede.json` on `claude/aoede-narrator` for what's done.

| Category | Clip names | English | Done (EN) | Afrikaans | Heard how often |
| --- | --- | ---: | ---: | ---: | --- |
| Letter sounds | `snd_*` | 63 | 0 * | 33 | Constantly on Letters Island |
| Letter names | `name_*` | 25 | 0 | 22 | Rare (after phonics group 5) |
| Picture and reading words | `word_*` (incl. 10 `word_contraction_*`) | 418 | 0 | 214 | Often, spread thin |
| Numbers | `num_0`–`num_100` | 101 | 0 | 101 | Very often (0–20 most) |
| Numbers mid-sentence | `num_0_mid`–`num_20_mid` | 21 | 17 | 21 | Very often (Block Towers, Balance) |
| Money | `money_*` | 9 | 0 | 9 | Corner Shop only |
| Language Island words | `learn_*` | 124 | 0 | 124 | Language Island (heard in the *other* language) |
| Game instructions | `vo_*` in script.csv (prompts and sentence pieces) | 60 | 15 | 60 | Every game, every round |
| Personality lines | welcome, 6 island intros, island asleep, who's playing, level up, practise again, round done, Bip charging, day done | 14 | 3 | 14 | Every visit |
| Praise | `praise_01`–`praise_10` | 10 | 10 | 10 | After nearly every right answer |
| Hints | `hint_01`–`hint_04` | 4 | 3 | 4 | After two misses |
| Morning Order / Bip's Day steps | `vo_<story>_N` (32 stories) | 123 | 46 | 123 | Morning Order, Bip's Day |
| Reading sentences | `vo_sent_*` (16), `vo_hom_*` (17, English only) | 33 | 0 | 16 | Words Island |
| Number sentences | `vo_say_*` (towers, joins, take away, doubles, odd/even, make ten) | 233 | 0 | 233 | Numbers Island; **the game joins pieces when missing** |
| **Total** | | **1,238** | **94** | **984** | |

**2,222 clips in all** (the earlier ~1,964 figure was before the 248 Language Island words were
added). 94 English clips are done; letter sounds (*) are recorded into `voice-lab/aoede_phonics/` and
only count once you've approved them by ear.

How the recorder groups them today (`group()` in `scripts/gemini_narrator.py`):

| Style | English | Afrikaans | Batch | First-pass requests |
| --- | ---: | ---: | ---: | ---: |
| sparkle (everything not below) | 622 | 605 | 18 | 35 + 34 |
| words (`word_`, `num_`, `money_`, `name_`) | 553 | 346 | 36 | 16 + 10 |
| phonics (`snd_`) | 63 | 33 | 8 | 8 + 5 |
| **Total** | | | | **108** |

Two things to fix in passing (no extra cost, saves requests):
- `learn_*` words fall into the *sparkle* group because `group()` doesn't list the `learn_` prefix, so
  248 single words would be read bubbly in batches of 18 (14 requests) instead of as words in batches
  of 36 (7 requests). Add `learn_` to the words group.
- `docs/LANGUAGES.md` says the 124 Language Island words wait for the owner's check before recording,
  but the recorder would record them now. Hold them back (a skip list, or record them last) until
  they're signed off.

## 2. Which clips stay the same for everyone, and which get an older version

**Rule: content is neutral; personality is banded.** A word, a number or a sentence is the thing the
child is learning, and it must sound the same every time and for every child (and sentences are
*read* by the child, so the recording is a model of reading, not a performance). Bip's personality
lines are what a 10-year-old hears as "this is for babies", so those are the ones worth a second take.

| Stays one recording (neutral) | Why |
| --- | --- |
| Letter sounds, letter names | Must be identical and pure; approved by ear |
| Words, Language Island words, contractions | The learning content |
| Numbers, mid-sentence numbers, money | Joined into sums; must match each other |
| Number sentences (`vo_say_*`), reading sentences, story steps | Content the child reads or follows |
| Game instructions and sentence pieces (`vo_find_the_sound`, `vo_plus`, `vo_build_tower`…) | Joined to other clips, so the tone must match them; recorded "friendly and clear" so they suit all ages (see section 6) |

| Gets a Big (7–10) version | English | Afrikaans |
| --- | ---: | ---: |
| Praise: a separate Big pool of 12 lines (not translations of the Little ones) | 12 | 12 |
| Hints: one Big version of each, same job (three lead into a sound, `hint_02` stands alone) | 4 | 4 |
| Feedback: level up, practise again, round done | 3 | 3 |
| Greetings: welcome, 6 island intros, island asleep, who's playing | 9 | 9 |
| Rest: Bip charging, day done | 2 | 2 |
| **Extra clips** | **30** | **30** |

Not banded, on purpose: the Language Island prompts (`vo_show_me`, `vo_what_is_it`, `vo_bips_day`,
`vo_in_our_language`) and all game prompts. They're already plain instructions; recording them
"friendly and clear" for everyone is enough. If you later feel one specific prompt sounds young
(for example "Sparkles will follow you!" in Letter Trace), give just that one a Big version: the
system below allows any `vo_` line to have one.

## 3. Age bands

The game has four *learning* bands (foundation 4–5, stage1 6, stage2 7, stage3 8+, from
`Content/curriculum/skills.json`) and profiles allow ages 3 to 10. The *voice* should follow the
child's **age**, not their learning band: a 9-year-old catching up on phonics plays foundation games
but must not be spoken to like a 4-year-old.

**Recommendation: two voice bands now, room for a third later.**

| Voice band | Ages | Who | How Aoede sounds |
| --- | --- | --- | --- |
| **Little** | 3–6 (and no age set) | Grade R, Grade 1 | Today's recordings: bubbly, sparkling, warm, slower, a little sing-song on the happy words, playful wonder, short pauses |
| **Big** | 7–10 | Grade 2 and up, older siblings | Same warm Aoede, upbeat and friendly but relaxed; normal talking speed, less sing-song, fewer exclamations, respectful, a bit of "you've got this"; never babyish, never sarcastic |
| *(later) Explorer* | 9–10 | Only if older siblings play a lot | Calmer still, a touch of humour; another 30 lines per language. Not worth it until it's clear they play |

Why not four voice bands: a 5- and 6-year-old don't need different praise, and every band adds 30
lines per language to record and check. Why 7: it's where Cambridge Stage 2 starts, children are
reading by themselves, and Grade 2 is when "that's for babies" begins.

Writing tips that carry into the recording (Gemini follows the punctuation):
- Little lines: exclamation marks, short words, "we" and "let's", Bip as a friend.
- Big lines: mostly full stops, an exclamation only where it's earned, "you" (the child did it),
  specific praise ("Smart thinking."), shorter, no "Wow!" openers.

### Sample lines

Little lines are what's in the scripts today (owner signed off the Afrikaans on 18 October 2026).
Big lines are new drafts; the Afrikaans needs a fluent reader's sign-off before recording, as usual.

**Praise**

| Little (existing) EN | Little (existing) AF | Big (new) EN | Big (new) AF |
| --- | --- | --- | --- |
| Well done! | Mooi so! | Nice one. | Mooi. |
| Brilliant! | Briljant! | Spot on. | Presies reg. |
| You got it! | Jy het dit! | That's it. Well done. | Dis dit. Goed gedoen. |
| Super listening! | Super geluister! | Smart thinking! | Slim gedink! |
| Excellent! | Uitstekend! | You worked that out. | Jy het dit self uitgewerk. |
| Fantastic! | Fantasties! | Exactly right. | Heeltemal reg. |
| Yes! That's the one! | Ja! Dis die regte een! | Great focus. | Goeie fokus. |
| Bip is so proud of you! | Bip is so trots op jou! | Bip is impressed! | Bip is beïndruk! |
| Amazing work! | Wonderlike werk! | You're getting really good at this. | Jy word regtig goed hiermee. |
| Great job! | Knap gedoen! | Excellent thinking. | Uitstekende denkwerk. |
| | | Correct. Keep going! | Reg. Hou so aan! |
| | | Good plan. It worked! | Goeie plan. Dit het gewerk! |

**Hints** (same job as the Little ones: 01, 03 and 04 lead straight into a sound; 02 stands alone)

| Clip | Little EN | Big EN | Little AF | Big AF |
| --- | --- | --- | --- | --- |
| hint_01 | Listen carefully. It sounds like this: | Here's the sound again: | Luister mooi. Dit klink so: | Hier is die klank weer: |
| hint_02 | Here's a clue. Look for the one that's wiggling! | Here's a clue: check the one that's wiggling. | Hier is 'n leidraad. Soek die een wat wikkel! | Hier is 'n leidraad: kyk na die een wat wikkel. |
| hint_03 | Let's listen again, together: | Let's hear it once more: | Kom ons luister weer saam: | Kom ons hoor dit nog een keer: |
| hint_04 | Nearly! Have another listen: | Close. Listen again: | Amper! Luister nog 'n keer: | Amper daar. Luister weer: |

**Feedback, greetings and rest**

| Clip | Big EN | Big AF |
| --- | --- | --- |
| vo_level_up | You've really got that one now. On to the next level! | Jy ken dié een nou regtig goed. Op na die volgende vlak! |
| vo_lets_practise_again | Let's give that one a bit more practice. | Kom ons oefen dié een nog 'n bietjie. |
| vo_round_done | Good round. Let's head back to the island. | Goeie rondte. Kom ons gaan terug na die eiland. |
| vo_welcome | Hello again! Bip's ready when you are. Pick an island to explore. | Hallo weer! Bip is gereed as jy is. Kies 'n eiland om te verken. |
| vo_letters_island | Letters Island. Click Bip when you're ready. | Letter-eiland. Klik op Bip as jy gereed is. |
| vo_numbers_island | Numbers Island. Click Bip to start counting. | Getalle-eiland. Klik op Bip om te begin tel. |
| vo_words_island | Words Island. Click Bip to start reading. | Woorde-eiland. Klik op Bip om te begin lees. |
| vo_coding_island | Coding Island. Click Bip to start programming. | Kodeer-eiland. Klik op Bip om te begin programmeer. |
| vo_art_island | Art Island: shapes, paint and mirrors. Click Bip to start. | Kuns-eiland: vorms, verf en spieëls. Klik op Bip om te begin. |
| vo_language_island (EN child) | Language Island. Let's learn some Afrikaans. | — |
| vo_language_island (AF child) | — | Taal-eiland. Kom ons leer 'n bietjie Engels. |
| vo_island_sleeping | That island isn't ready yet. Try Letters Island! | Daardie eiland is nog nie gereed nie. Probeer Letter-eiland! |
| vo_who_is_playing | Who's playing? Click your picture. | Wie speel? Klik op jou prentjie. |
| vo_bip_charging | Bip's battery is low, so he's recharging. Time for a break. We'll play again when he's full. | Bip se battery is pap, so hy laai op. Tyd vir 'n blaaskans. Ons speel weer wanneer hy vol is. |
| vo_day_done | That's it for today. Good work. See you tomorrow! | Dis al vir vandag. Goeie werk. Sien jou môre! |

All lines pass the house rules: South African English and British spelling, AWS Afrikaans, no slang
("lekker", "sharp", "sorted", "nailed it" left out on purpose), no time pressure, nothing that
sounds like a punishment.

## 4. Boys and girls

**Recommendation: one voice and one set of words for both, written so neither feels left out. No
boy/girl setting.**

What makes it work for both:
- **Praise the doing, not the child's looks or "type":** listening, thinking, trying, working it
  out. Never "good girl/good boy", "princess", "champ", "buddy", "my hero", "pretty", "strong".
- **Mix themes inside every pool**, not split by gender: the stories already do this (pizza, rocket,
  carwash, butterfly, birthday, building a house, sandcastle). Keep praise and greetings free of
  themes, or spread them evenly: space, animals, building, sport, cooking, music, nature.
- **Bip stays the hero both relate to:** a small robot friend, curious and a bit clumsy. Bip is "he"
  in today's lines; that's fine and doesn't need changing.
- **Let the child choose, not the parent's guess:** if you want more "this is mine", give each
  child a choice of Bip's cheer *sound* (a rocket whoosh, a drum roll, a chime, an animal call)
  played with praise. That's a robot sound effect, not narrator speech, so it costs no recording
  requests. Optional, later.
- **Don't add a gender field to the profile.** The game doesn't need it, and a public, child-safe app
  is better without it.

**A second (male) narrator: not now.**

| | Keep Aoede only (recommended) | Add a male voice |
| --- | --- | --- |
| One-narrator rule | Kept | Broken, or two "narrators" a child has to get used to |
| Free-tier cost | None | Re-record what he'd say: all 2,222 clips = about 110 more requests, two more weeks; or only the personality lines (28 per language), which then clash with Aoede doing the instructions |
| Fixes what older children dislike | Yes, the Big band does that | No: a sing-song man is just as babyish |
| Boys | Most South African Grade R to 3 teachers are women; children are used to it | Some boys may like it; so may some girls |
| Letter sounds | Already approved for Aoede | Would need new ear-checks |

If you want to hear the option anyway, the Gemini voices worth a one-request test are **Achird**
(friendly), **Puck** (upbeat), **Sadachbia** (lively) and **Iapetus** (clear). Each test costs one
request of the daily allowance (one voice per request), e.g. the 12 Big praise lines. Only if one
of them is clearly better for the Big band would it be worth a rethink, and even then only for a
single band, never mixed within one band.

## 5. How the game picks the right version

**Picking:** when a child is chosen, the game sets the voice band from their age (3–6 or unset →
Little; 7–10 → Big), next to the language it already sets (`VoicePlayer.Language`). Optionally a
grown-up can override it per child in the Children tab ("Bip's voice: Little / Big / By age"); see
question 2.

**File names** (fit the existing contract; the base name stays the Little version):

| Kind | Little (today) | Big |
| --- | --- | --- |
| Instruction / personality | `vo_welcome.m4a` | `vo_welcome_big.m4a` |
| Hint (1:1) | `hint_02.m4a` | `hint_02_big.m4a` |
| Praise (own pool) | `praise_01`…`praise_10` | `praise_01_big`…`praise_12_big` |
| Afrikaans | `af/vo_welcome.m4a` | `af/vo_welcome_big.m4a` |

**Fallbacks (the game never goes silent because a Big line is missing):**
- A `vo_` or `hint_` clip: play `<clip>_big` if it's in the build, else `<clip>`.
- Praise: pick from the Big pool's recorded clips; if fewer than 4 are recorded yet, pick from the
  Little pool instead (so a half-recorded Big pool doesn't repeat one line).
- Afrikaans: same rules inside `af/`; the existing "missing clip plays a short silence" stays the
  last resort.

**What changes in code and content (brief; the main session builds it):**
- `BipCore`: a `VoiceBand { Little, Big }` with `VoiceBandRules.For(int? age)` (and the optional
  override on `SavedChild`); tests for the age edges (3, 6, 7, 10, none).
- `AudioCatalogue`: `BandClip(clip, band)`, `PraiseClips(band)` (10 Little, 12 Big),
  `HintClips(band)`, `ScriptedClips` including the Big rows; the naming regex accepts an optional
  `_big` suffix on `praise_NN` and `hint_NN` (`vo_` already allows it); `LeadsOn` treats
  `hint_NN_big` like `hint_NN`.
- `VoicePlayer`: a `Band` property; when resolving a clip, try the `_big` file first for a Big child.
  `GameCoordinator.RandomPraise()` / `RandomHint()` pick from the band's pool.
- `audio/script.csv` and `audio/af/script.csv`: add the 30 Big rows each (same columns; the `notes`
  column says "Big band (ages 7–10)").
- `scripts/validate_content.py`: every `_big` row has a Little row of the same name (praise pool
  excepted), and `sa_english.py` / `af_spelling.py` already cover the script files.
- `scripts/gemini_narrator.py`: `group()` returns `"big"` for clips ending `_big` and a new
  `"clear"` style for instructions and sentences (section 6); `learn_` joins the words group; a
  priority order replaces plain alphabetical (section 7).
- `docs/LANGUAGES.md`, `audio/NARRATOR_VOICE.md`: a short "voice bands" note. No change to the
  kid-safety rules.

## 6. Director's notes for Gemini

The recorder already sends `### DIRECTOR'S NOTES (never read these out)` then
`#### TRANSCRIPT (read only this)`. Proposed styles, each followed by the existing pause line
("After every line, stay silent for two full seconds before the next one. Never read anything
twice."). Notes stay in English for both languages (that works today); the Afrikaans ones add the
existing Afrikaans accent sentence.

| Style | Used for | English | Afrikaans |
| --- | --- | --- | --- |
| `sparkle` (Little) | praise, hints, feedback, greetings, rest (base files) | A | A + AF |
| `big` | the `_big` lines | B | B + AF |
| `clear` (new) | game instructions, sentence pieces, story steps, reading and number sentences | C | C + AF |
| `words` | words, numbers, money, names, Language Island words | D | D + AF |
| `phonics` | letter sounds | unchanged | unchanged |

**A. Little, sparkling (ages 3–6)**
> Read these lines as a bubbly, sparkling, warm South African English teacher of four- to
> six-year-olds who is delighted to see the child: a big smile in the voice, playful and full of
> wonder, a little sing-song on the happy words, unhurried and very clear, with tiny pauses so a young
> child can follow. Friendly, never shrill, squeaky or baby-talk. Natural South African English
> accent, not British and not American.

**B. Big, upbeat (ages 7–10)**
> Read these lines as the same warm South African English teacher, now talking to a seven- to
> ten-year-old she respects: friendly, upbeat and genuinely pleased, with a real smile in the voice,
> but relaxed and natural, like a favourite teacher or an older cousin chatting, not performing for a
> toddler. Normal talking speed, a little quicker than for little ones; no sing-song, no exaggerated
> excitement, no baby talk, never sarcastic or bored. Confident and encouraging. Natural South
> African English accent, not British and not American.

**C. Clear and friendly (all ages)**
> Read these lines as a warm, friendly South African English primary school teacher giving clear
> instructions to children aged four to ten: a smile in the voice and a bright, positive tone, but
> natural and not sing-song, so it suits a five-year-old and doesn't sound babyish to a ten-year-old.
> Unhurried and very clear; say each line once, exactly as written. A line that ends without a full
> stop, or with "…", leads straight into another recording: keep the voice up and unfinished, as if
> the sentence carries on. Natural South African English accent, not British and not American.

**D. Words (all ages)**
> Read these words as a warm, friendly South African English teacher showing a child each word:
> clear, bright and natural, each word once, exactly as written, all at the same even pitch and
> speed, so the first and last words sound alike and none sounds like the end of a list. Natural
> South African English accent, not British and not American.

**AF (added to A–D for Afrikaans, replacing "South African English" with "Afrikaans" in the text):**
> Everything is in Afrikaans: say it with natural, fluent South African Afrikaans pronunciation, as a
> first-language Afrikaans speaker would, never with an English or Dutch accent.

For the Language Island words (`learn_*`) the notes add one sentence: "These are first words for a
child learning this language: say each one slowly and very clearly." (An English child hears
`af/learn_*` and vice versa, so the clearer the better.)

Note on the 64 instruction and story clips already done in the old `sparkle` style: listen to a
handful first. If they sound fine for an older child, keep them (no cost). If not, re-recording them
in `clear` is 4 requests.

## 7. Budget and recording order

**Extra cost of this plan**

| Item | Clips | Requests (18 per batch) |
| --- | ---: | ---: |
| Big lines, English | 30 | 2 |
| Big lines, Afrikaans | 30 | 2 |
| Retries (small batches of 4) | ~10 | ~2 |
| **Total extra** | **60** | **~6 = under one day** |
| Saved by moving `learn_` to the words style | | −7 |

So the age bands cost **less than they save**. The new `clear` style costs nothing extra (it only
changes the notes on clips that still need recording).

**The whole job.** Left to record: 1,144 English + 984 Afrikaans + 60 Big = about 2,190 clips, about
104 first-pass requests with the new grouping (the table below), plus about 15% retries: **~120
requests, about 12 to 17 days at 7 to 10 requests a day.** If the number sentences (`vo_say_*`, 466
clips, 26 requests) wait until last, everything else is done in **about 9 to 13 days**, and the game keeps joining those sentences
from pieces until then.

Tip: Gemini's free daily allowance resets at midnight US Pacific time, which is **09:00 SAST** until
1 November and **10:00 SAST** after the US clocks change. Starting the daily run soon after that gets
the whole day's allowance.

**Order: most-heard first, English and Afrikaans taking turns batch by batch (as the recorder does
now):**

| Step | What | EN clips | AF clips | Requests (EN + AF) |
| --- | --- | ---: | ---: | ---: |
| 1 | Praise, hints, feedback, greetings, rest (Little) | 12 | 28 | 1 + 2 |
| 2 | Game instructions and sentence pieces (`clear`) | 45 | 60 | 3 + 4 |
| 3 | Big lines (both languages) | 30 | 30 | 2 + 2 |
| 4 | Numbers 0–20, mid-sentence numbers, money | 34 | 51 | 2 + 2 |
| 5 | Letter sounds, for your ear check (parallel, any day) | 63 | 33 | 8 + 5 |
| 6 | Words, in phonics-group order (groups 1–3 first), then numbers 21–100, letter names | 523 | 316 | 15 + 9 |
| 7 | Morning Order / Bip's Day steps, reading sentences | 110 | 139 | 7 + 8 |
| 8 | Language Island words, once you've signed them off | 124 | 124 | 4 + 4 |
| 9 | Number sentences (`vo_say_*`) | 233 | 233 | 13 + 13 |

(Day 1 covers steps 1 to 3 in both languages, so the next build already has every praise and hint
in both bands.) The recorder needs a small `priority(clip)` function in place of `sorted()` to follow
this order; the batches stay one style each.

## 8. Questions for you

1. **Is 7 the right age to switch to the Big voice, or would you rather 8?**
2. **Should a grown-up be able to choose per child** ("Little", "Big" or "by age") in the Children
   tab, for a 6-year-old who wants the big-kid voice or a 7-year-old who loves the sparkly one?
3. **Happy with one narrator (Aoede) for boys and girls?** Or would you like a one-request test of a
   male voice (Achird or Puck) for the Big praise, just to hear it?
4. **The Big lines:** do the English drafts above sound right to you, and can someone fluent check
   the Afrikaans before they're recorded?
5. **Instructions "friendly and clear" for everyone, full sparkle kept for praise and greetings for
   the little ones:** OK? And should the 64 instruction and story clips already done stay as they
   are, or be re-done in the new style (4 requests)?
6. **OK to record the long list of number sentences last?** The game already joins them from pieces
   ("seven… plus three") until they're done.
7. **Later, if older siblings play a lot:** would you want a third band for ages 9–10 (30 more lines
   per language, under a day)?
8. **Optional:** would you like each child to pick Bip's cheer sound (rocket, drum, chime, animal)?
   It's a sound effect, so it costs no recording time.
