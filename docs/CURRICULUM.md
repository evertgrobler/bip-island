# Bip Island — Curriculum guide

How every Bip Island mini-game lines up with the **Cambridge** curriculum for ages 4–8, and how the content data in `Content/` is organised. Read this, `docs/GAMES.md` and `CLAUDE.md` before building or changing any game.

## Which Cambridge frameworks, at which ages

| Game band | Child's age | Cambridge programme | What the band works on |
| --- | --- | --- | --- |
| `foundation` | 4–5 | Cambridge Early Years (EY2–EY3) | First sounds, counting to 10, patterns, putting steps in order. Works towards the Stage 1 objectives. |
| `stage1` | 5–6 | Cambridge Primary Stage 1 | All single-letter sounds and the common digraphs, numbers to 20, first programs for Bip |
| `stage2` | 6–7 | Cambridge Primary Stage 2 | Split digraphs and alternative spellings, numbers to 100, repeat blocks, debugging |
| `stage3` | 7–8 | Cambridge Primary Stage 3 | Homophones and contractions, money with change, growing patterns, inputs and outputs |

The frameworks used:

- **English (0058)**, Reading and Writing *word structure* strands (codes `Rw`, `Ww`). Phonics runs through Stages 1–4; spelling through every stage.
- **Mathematics (0096)**, mainly the Number strand: counting and sequences `Nc`, integers `Ni`, money `Nm`, place value `Np`, plus position `Gp`, statistics `Ss` and probability `Sp`.
- **Computing (0059)**: computational thinking `CT` and programming `P`.
- **Art Island** adds Mathematics geometry (`Gg`: 2D shapes, turning, symmetry) and halves (`Nf`), plus colour mixing from **Art & Design (0067)**, Making strand.

Cambridge Early Years learning statements are only available to registered schools, so the `foundation` band uses the earliest Stage 1 objectives as its targets. If the family ever gets the EY statements, add them to `Content/curriculum/objectives.json` and link them to the foundation skills.

Objective codes are Cambridge's own. The one-line summaries in the data are short paraphrases written for this project. Do not paste Cambridge's wording into the game or the repo.

## How Cambridge expects children to learn, and what that means for the game

1. **Sounds before names, blending from day one.** Stage 1 English starts with the most common sound for each letter and with blending decodable words. So: teach pure sounds (s = "sss"); use only words whose graphemes are already unlocked (`decodableFromGroup`); add letter names only after phonics group 5.
2. **Reading and spelling move together.** Every Reading `Rw` objective has a Writing `Ww` partner. Each phonics group therefore unlocks both a reading game (Sound Buttons) and a spelling game (Word Builder, Missing Letter).
3. **Concrete before abstract in maths.** Stage 1 maths counts real objects, sees small amounts at a glance, and makes and breaks numbers before symbols. Number games show objects first, then the numeral, then the sum.
4. **Thinking and Working Mathematically.** Cambridge names eight habits: specialising, generalising, conjecturing, convincing, characterising, classifying, critiquing and improving. Games build them in with "what comes next?" (Pattern Party, Number Train), "is there a quicker way?" (Repeat Robot) and "spot the mistake" (Fix-It, the pattern `fix` rule).
5. **Unplugged before plugged in computing.** Stage 1 computing starts with algorithms for everyday tasks, then directional instructions, then simple programs. Morning Order comes before Bip's Path, and arrow blocks come before turn blocks.
6. **Mistakes are part of programming.** Stages 1–3 all expect children to predict, test and debug. Fix-It is a full game, and Bip's Path lets children run a half-built program to see what happens.

## Phonics progression (`Content/phonics/graphemes.json`)

The order follows the UK *Letters and Sounds* synthetic phonics sequence, which matches Cambridge Stage 1–2 phonics:

| Group | Sounds | Band | Cambridge |
| --- | --- | --- | --- |
| 1 | s a t p i n | foundation | 1Rw.01, 1Ww.01 |
| 2 | m d g o c k | foundation | 1Rw.01, 1Ww.01 |
| 3 | ck e u r | foundation | 1Rw.01, 1Rw.02 |
| 4 | h b f l ff ll ss | foundation | 1Rw.01, 1Rw.02 |
| 5 | j v w x y z zz qu | stage1 | 1Rw.01, 1Rw.02 |
| 6 | ch sh th ng | stage1 | 1Rw.02, 1Ww.02 |
| 7 | ai ee igh oa oo (moon) oo (book) | stage1 | 1Rw.02 |
| 8 | ar or ur ow oi er, plus tt mm dd nn | stage1 | 1Rw.02 |
| 9 | a-e i-e o-e u-e, ay ea ie ow (snow) ou ir aw | stage2 | 2Rw.01, 2Rw.02, 2Ww.01, 2Ww.02 |

Each grapheme carries its sound (IPA), its kind (`stretchy`, `bouncy`, `vowel` or `digraph`), a narrator hint for clean audio, a picture word, and a handwriting family for Letter Trace. **Bouncy (stop) sounds must be clipped with no "uh"**; the narrator hint says how.

## Every game against the curriculum

| Game | Ages | Cambridge objectives |
| --- | --- | --- |
| Meet the Sound | 4–6 | 1Rw.01, 1Rw.02, 1Ww.01, 1Ww.02, 2Rw.01, 2Rw.02, 2Ww.01, 2Ww.02 |
| Sound Hunt | 4–6 | 1Rw.01, 1Rw.02, 1Ww.01, 1Ww.02 |
| Bubble Pop | 4–7 | 1Rw.01, 1Rw.02, 1Ww.01, 1Ww.02, 2Rw.01, 2Rw.02, 2Ww.01, 2Ww.02 |
| Letter Trace | 4–6 | 1Ww.01 |
| Feed the Monster | 4–7 | 1Rw.01, 1Rw.02, 1Ww.01, 1Ww.02 |
| Letter Fishing | 5–8 | 1Rw.01, 1Rw.02, 1Ww.02, 2Rw.01, 2Rw.02, 2Ww.01, 2Ww.02 |
| Sound Detective | 5–8 | 1Rw.01, 1Rw.02, 1Rw.03, 1Rw.05, 1Ww.02 |
| Count & Tap | 4–5 | 1Nc.01, 1Ni.01, 1Np.01 |
| Quick Look | 4–6 | 1Nc.02, 1Nc.03, 2Nc.02 |
| Feed the Monster: Numbers | 4–6 | 1Nc.01, 1Ni.01, 1Ni.02, 1Ni.03, 1Ni.05, 1Np.01, 3Nc.04 |
| Number Train | 5–7 | 1Nc.04, 1Nc.05, 1Np.03, 1Np.04, 2Nc.01, 2Nc.04, 2Nc.05, 2Nc.06, 2Ni.01, 2Ni.07, 2Np.01–04, 3Nc.05 |
| More or Less | 4–7 | 1Ni.02, 1Ni.03, 1Ni.05, 1Np.03, 1Ss.03, 2Ni.02, 2Ni.04 |
| Ten-Frame Garden | 6–8 | 1Ni.04, 1Ni.06, 1Np.02, 2Ni.03 |
| Corner Shop | 6–8 | 1Nm.01, 2Nm.01, 2Nm.02, 3Nm.01, 3Nm.02 |
| Sound Buttons | 4–6 | 1Rw.03, 1Rw.05, 1Rw.06, 2Rw.05, 2Rw.06 |
| Word Builder | 5–7 | 1Rw.04, 1Ww.02, 1Ww.04, 1Ww.05, 2Ww.05 |
| Missing Letter | 5–7 | 1Ww.02, 1Ww.05 |
| Rhyme Time | 4–7 | 1Ww.03, 2Ww.03 |
| Tricky Word Memory | 6–8 | 1Rw.07, 1Ww.06, 2Rw.04, 2Rw.07, 2Ww.09, 3Rw.01, 3Rw.02 |
| Silly Sentences | 6–8 | 1Rw.05, 1Rw.07 |
| Word Rocket | 7–8 | 1Rw.04, 1Ww.04, 1Ww.06, 2Rw.04, 2Rw.07, 2Ww.05, 2Ww.08, 2Ww.09, 3Rw.01, 3Ww.04 |
| Pattern Party | 4–5 | 1Nc.06, 2Sp.01, 3Nc.06 |
| Morning Order | 4–5 | 1CT.01, 1CT.05, 1CT.06 |
| Bip's Path | 4–6 | 1CT.03, 1CT.04, 1Gp.01, 1P.02, 1P.03, 1P.05, 2CT.05, 2CT.06, 2Gp.01, 2P.08 |
| Fix-It | 5–7 | 1CT.02, 1P.04, 1P.06, 1P.07, 2CT.02, 2P.07, 3P.09 |
| Repeat Robot | 6–8 | 2P.03, 3CT.02, 3CT.03, 3P.01 |
| Puddle Rules | 7–8 | 3CT.05, 3CT.07, 3CT.08 (if-then goes slightly beyond Stage 3: offer it only to confident 7–8 year olds) |
| Bip's Dance Party | 5–8 | 1CT.07, 2P.03, 2P.04, 3CT.02, 3CT.03, 3P.01, 3P.03, 3P.04 |
| Shape Builder (Art) | 4–8 | 1Gg.01, 1Gg.07, 2Gg.01, 2Gg.10, 3Gg.01 |
| Paint Pots (Art) | 4–7 | AD1.colour, AD2.colour (Art & Design 0067, codes to be confirmed) |
| Mirror Magic (Art) | 4–8 | 1Nf.01, 2Gg.09, 3Gg.09 |

The authoritative mapping is `Content/curriculum/games.json`, generated from the skills each game practises. If this table and the JSON ever differ, the JSON wins.

**Gaps (objectives listed but not yet covered by a game):** 2Rw.03 (prefixes and suffixes), 3Gp.01 (compass directions), 3Rw.03 and 3Ww.05 (strategies for unfamiliar words). They are good candidates for future games.

## Levels, mastery and review (`Content/curriculum/skills.json`)

- **Starting level** comes from the child's age (4–5 foundation, 6 stage1, 7 stage2, 8 stage3). It then adjusts by performance, never by age alone.
- **Skills** form a map: each has a band, Cambridge objectives and prerequisites. A skill unlocks when its prerequisites are mastered. The island map's "new areas" follow this map.
- **Within a game:** 3 correct in a row moves up a level, and 2 misses in a row drops back a step.
- **Mastered** means 80% correct over the last 10 attempts, on at least 2 different days, so one lucky session doesn't count.
- **Review:** mastered skills come back after 2, 5 and 14 days. Bip's recommendations prefer skills due for review.
- The game reads these numbers from `masteryRules` in `skills.json` (the `mastery` text next to it is the same rules in words). Change both together. Questions per visit to a game come from `session.roundsPerSession` in `games.json`.

## The content files

All game content is data. Games never hard-code words, numbers or levels. The game bundles the whole `Content/` folder (copied in by `scripts/godot/prepare_assets.py`), and `ContentLibrary` (in `godot/BipCore/Content/`) decodes every file into C# models at start-up. A unit test decodes each file and fails if a model would drop any key, so a new field in the JSON must also be added to the C# content models.

| File | What it holds | Used by |
| --- | --- | --- |
| `curriculum/objectives.json` | The 103 Cambridge objective codes used, with stage, strand and a short paraphrase | Everything (reference) |
| `curriculum/skills.json` | 55 skills: band, objectives, prerequisites, plus the mastery and review rules | Bip's recommendations, island unlocks, parent progress view |
| `curriculum/games.json` | 28 games: island, ages, skills, objectives, content source, build phase | Game registry |
| `phonics/graphemes.json` | 63 graphemes in teaching order, in 9 groups | Letters island |
| `words/words.json` | 221 decodable words (158 with pictures) with grapheme splits, phonics group, first sound and rhyme family | Letters and Words islands |
| `words/tricky_words.json` | Stage 1 and Stage 2 common exception words | Tricky Word Memory, Silly Sentences, Word Rocket |
| `words/sentences.json` | 16 silly sentences, each fully decodable at its group, with a right and a wrong picture brief | Silly Sentences |
| `words/endings.json` | Plurals (-s, -es) and verb endings (-s, -ed, -ing) with no change to the root | Word Builder, Word Rocket |
| `words/homophones.json` | 8 homophone sets with fill-the-gap sentences | Word Rocket (stage 3) |
| `words/contractions.json` | 10 contraction pairs (do not / don't) | Tricky Word Memory (stage 3) |
| `numbers/numbers.json` | Ranges per band, counting objects, rand coins and notes, shop items | Numbers island |
| `coding/sequences.json` | 8 picture-card sequences for Morning Order | Coding island |
| `coding/patterns.json` | Pattern rules from AB up to growing patterns | Pattern Party |
| `coding/levels.json` | 52 grid levels (easy arrows, arrows, turns, Fix-It, repeat) plus 5 Puddle Rules levels, each checked solvable with a shortest solution | Bip's Path, Fix-It, Repeat Robot, Puddle Rules |
| `art/shapes.json` | 12 flat shapes (outline, sides, curved, regular, how often they look the same in a turn) and 14 pictures built from them | Shape Builder |
| `art/paints.json` | Paint colours, the pots (each with a label picture) and every mix, following real paint | Paint Pots |
| `art/mirror.json` | 8 x 8 peg pictures, each folding along exactly one mirror line, and the peg board | Mirror Magic |
| `asset_manifest.json` | Generated: every voice clip (660, with the text to speak) and picture (256, with a brief) the content needs | Audio and art production |

## Rules for changing content

1. **Edit the JSON, then run `python3 scripts/validate_content.py`.** It must pass before anything is merged. Add it as a CI step on every pull request.
2. The validator checks that:
   - every word's graphemes spell the word;
   - every word sits in the right phonics group;
   - every sentence is decodable at its level;
   - every objective code exists;
   - the skill map has no loops;
   - every grid level is solvable;
   - its stored solution really is a shortest route;
   - every Fix-It bug really breaks the program.
3. **A new word** needs its grapheme split. If it is picturable, it also needs a `pic_<word>` artwork. Every word needs a `word_<word>` voice clip.
4. **New objectives:** add the code to `objectives.json` with your own short paraphrase, then link it from a skill.
5. **Voice clips** come from the asset manifest and are recorded only in the official narrator voice ("Bip Island Narrator" in ElevenLabs). Cloud sessions can't reach ElevenLabs, so list the missing clips in the pull request and the owner has them generated.
6. Language rule: proper South African English, UK spelling, no slang.

## Sources

- [Cambridge Primary English curriculum framework 0058 (2020)](https://resources.pastpapersacademy.com/resources/curriculum/cambridge/0058-English-Curriculum-Framework.pdf)
- [Cambridge Primary Mathematics curriculum framework 0096 (2020)](https://cmapspublic2.ihmc.us/rid=1X1FVKBN9-26QY3DL-426X/Cambridge%20Primary%20Mathematics%20Curriculum%20Framework%200096_tcm142-592530.pdf)
- [Cambridge Primary Computing curriculum framework 0059 (2021)](https://pdfcoffee.com/0059-primary-computing-curriculum-framework-2021-tcm142-635600-pdf-free.html)
- [Cambridge Early Years to Primary transition document (EY stage ages)](https://www.cambridgeinternational.org/Images/729772-cambridge-early-years-to-primary-transition-support-document.pdf)
- [Cambridge Primary Art & Design (0067)](https://www.cambridgeinternational.org/programmes-and-qualifications/cambridge-primary/curriculum/art-and-design/)
- [Cambridge Early Years handbook 2026](https://www.cambridgeinternational.org/Images/745285-cambridge-early-years-handbook-2026.pdf)

## Art Island (added 3 October 2026)

Three games on a fifth island, in the Godot version only.

- **Shape Builder**: find a named shape, then fill the gap in a picture made of shapes (foundation). From stage 1, sort shapes by straight sides and curves. From stage 2, pentagons and hexagons, shapes in any position, which shape still looks the same after a quarter turn, and how many times a shape looks the same in one full turn. At stage 3, tell regular shapes (all sides equal) from irregular ones.
- **Paint Pots**: mix two pots to make a colour, or predict what two pots make and then mix to check (the "predict and check" habit from Thinking and Working Mathematically). White makes colours lighter (stage 1), black makes them darker (stage 2). Mixes follow real paint, never screen light. Every pot carries a label picture and Bip says every colour name, for children who find some colours hard to tell apart.
- **Mirror Magic**: which picture is the same on both sides (foundation), which half finishes a picture with the mirror down or across (stage 1), mirror the pegs on a peg board (stage 2), and where the mirror line goes (stage 3).

**To confirm:** 1Gg.07 and 3Gg.09 were checked against summaries of the 0096 framework, not the document itself. The Art & Design 0067 framework is only open to registered schools, so `AD1.colour` and `AD2.colour` are placeholder codes for its Making strand: swap in the real codes once someone has the framework.

The validator checks the art content too: each shape's sides, regular flag and turn count against its outline; that every pair of pots a level offers has exactly one, distinct mix; and that every mirror picture folds along its own line and no other.
