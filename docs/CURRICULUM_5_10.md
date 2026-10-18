# Bip Island for ages 5 to 10, in English and Afrikaans: the plan

Written 18 October 2026 (SAST). **Approved by the owner the same day** ("I'm happy with every update and
recommendations"), with the recommended answers below. Built step by step, each its own pull request.

## The short version

- **Today** the game covers ages 4 to 8: Cambridge Early Years to Stage 3, in English. Afrikaans is being
  added for the same ages.
- **The goal:** ages 5 to 10 (Grade R to Grade 4) in both languages, following **Cambridge** for the English
  side and **CAPS** (the South African curriculum) for both: CAPS English Home Language, **CAPS Afrikaans
  Huistaal** and CAPS Mathematics (Wiskunde). Where the two agree (most of maths and computing) one game
  serves both; where they differ (reading and spelling in Afrikaans), Afrikaans gets its own levels.
- **What's missing for 9 and 10-year-olds:** reading for meaning (short passages and questions), grammar,
  longer spelling patterns, times tables to 10 × 10, fractions and decimals, time and measuring, graphs,
  and coding with loops, choices ("if") and variables. In Afrikaans also: verkleinwoorde, meervoude,
  verlede tyd and the double "nie".
- **How:** two new bands (ages 8–9 and 9–10), about 8 new games and new levels in 9 existing ones, built in
  five steps over a few weeks, each its own pull request. The older children also get Bip's "Big" voice
  (already built) so it never sounds babyish.
- **Recording:** roughly 2,000 more clips across both languages (mostly passages, questions and words), so
  another 2 to 3 weeks of free daily recording after the current queue. Nothing needs paying for.

## 1. Ages, grades and stages side by side

The child's **age** sets where they start; their play moves them up or down from there (mastery tracking,
as today). South African children are in Grade R in the year they turn 6.

| Age | SA grade (CAPS) | Cambridge | Game band | Bip's voice |
| --- | --- | --- | --- | --- |
| 5 | Grade R | Early Years → Stage 1 | `foundation` (today) | Little |
| 6 | Grade 1 | Stage 1 | `stage1` (today) | Little |
| 7 | Grade 2 | Stage 2 | `stage2` (today) | Big |
| 8 | Grade 3 | Stage 3 | `stage3` (today, thin) | Big |
| 9 | Grade 4 | Stage 4 | `stage4` **new** | Big |
| 10 | Grade 4 to 5 | Stage 5 | `stage5` **new** | Big |

CAPS phases: Grade R to 3 is the **Foundation Phase**, Grade 4 starts the **Intermediate Phase**, where
reading for meaning, grammar and longer writing take over from phonics, and Mathematics moves to bigger
numbers, fractions and measurement.

## 2. What each island teaches, age by age

### Letters & Sounds, then Reading (English: Cambridge English 0058 + CAPS English Home Language; Afrikaans: CAPS Afrikaans Huistaal)

| Age | English | Afrikaans | Games |
| --- | --- | --- | --- |
| 5 | Letter sounds groups 1–5, blending three-letter words | Klanke groups 1–3 (a s m t p e…), klank-groepe saamvoeg | Meet the Sound, Sound Hunt, Bubble Pop, Letter Trace (all built); Bip's Stories level 1 |
| 6 | Digraphs (sh, ch, th, ng), letter names, first sight words | Groups 4–6, klinkerverbindings (ie, oe, ou, ui), hoëfrekwensiewoorde | + Sound Buttons, Word Builder, Feed the Monster (built); Bip's Stories level 2 |
| 7 | Long vowels and alternative spellings, reading short sentences | Groups 6–7 (ei, aa, ee, oo, uu, eu), tweeklanke, sinne lees | + Silly Sentences (planned), Word Rocket (built) |
| 8 | Prefixes and suffixes, contractions, homophones, reading a short paragraph | Verkleinwoorde (-tjie, -pie, -etjie, -kie), meervoude (-e, -s, -ers), lidwoorde, voorsetsels | **New:** Bip's Stories (read and answer), **new Afrikaans:** Klein Maak (verkleinwoorde), Meer Maak (meervoude) |
| 9 | Reading for meaning: who, where, why; word families; spelling rules (doubling, dropping e) | Leesbegrip, verlede tyd ("het ge-"), sinsbou, die dubbele "nie" | Bip's Stories level 5, **new:** Sentence Builder / Sinsbouer (drag words into order), Spelling Rules |
| 10 | Grammar: nouns, verbs, adjectives, tenses, punctuation; reading a page | Woordsoorte, leestekens, trappe van vergelyking (groot, groter, grootste), samestellings | **New:** Word Detective / Woordspeurder (find the noun, the verb…), Bip's Stories level 5 |

Bip's Stories starts at age 5, just easier (owner, 18 October 2026): two scenes with one short, mostly decodable sentence each and one picture question at 5; three scenes and two questions at 6; longer stories with "why" and "how did they feel" questions from 7 to 10. It's the big one for 8 to 10: a short illustrated story read aloud by the narrator (the child
can tap any sentence to hear it again), then three spoken questions with picture or word answers. No
typing, no time pressure. Stories are written separately in each language (not translated), set in South
African life, with boys and girls, animals, sport, cooking, building and nature mixed through them.

### Numbers (Cambridge Mathematics 0096 + CAPS Mathematics / Wiskunde; one set of games, both languages)

| Age | Cambridge | CAPS | Games |
| --- | --- | --- | --- |
| 5 | Count to 10, at a glance to 5 | Grade R: count and order to 10, early adding | Count & Tap, Quick Look, Block Towers (built) |
| 6 | To 20, adding and taking away, doubles | Grade 1: count to 100, calculate within 20 to 34 | + Bip's Balance (built), Number Train (planned) |
| 7 | To 100, tens and ones, 2/5/10 times | Grade 2: count to 200, calculate to 99, early multiplication | + Picnic Plates (sharing), Number Machine (planned) |
| 8 | To 1,000, tables 2–5 and 10, halves and quarters, money with change | Grade 3: to 1,000, tables, fractions, rand and cents | + Corner Shop (planned), **new:** Fraction Pizza |
| 9 | To 10,000, all tables to 10 × 10, fractions of amounts, telling time, measuring | Grade 4: whole numbers to 4 digits, multiplication, fractions, time, length, mass, capacity | **New:** Times Table Garden, Clock Shop (time), Measure It (rulers, scales, jugs) |
| 10 | Decimals (tenths), place value to 100,000, area and perimeter, graphs | Grade 4–5: common fractions, decimals start, perimeter and area, data handling | **New:** Decimal Darts, Bip's Graphs (read a bar graph and answer) |

Specific CAPS number ranges in this table are from memory and must be checked against the CAPS
Mathematics documents before the levels are built.

### Coding (Cambridge Computing 0059 + the new CAPS Coding and Robotics subject)

CAPS has a new Coding and Robotics subject for Grade R to 3 (and later grades). The Department of Basic
Education piloted it in schools from 2021; the latest public statement found (a September 2024 reply in
Parliament) says it wasn't yet fully rolled out. The plan follows Cambridge Computing and maps to the CAPS
Coding and Robotics outlines where they're published, to be confirmed.

| Age | What | Games |
| --- | --- | --- |
| 5–6 | Steps in order, moving Bip | Morning Order, Bip's Path (built) |
| 7 | Repeat (loops), fixing bugs | Repeat Robot, Fix-It (planned) |
| 8 | If-then rules, inputs and outputs | Puddle Rules (planned), Dance Party (planned) |
| 9 | Loops inside loops, choices with "if…else", sorting data | **New:** Bip's Factory (sort parcels with rules) |
| 10 | Variables (counting things), events, debugging longer programs | **New:** Bip's Garden Robot (a variable for water, a loop for each row) |

### Art Island

Shapes, colour mixing and symmetry already run to Stage 3. For 9 and 10: 3D shapes and their nets, angles
(right angle, more, less), tessellation patterns and colour wheels (warm and cool). **New levels** in Shape
Builder and Mirror Magic rather than new games.

### Language Island (CAPS First Additional Language)

Today: 124 everyday words. For older children:
- **Ages 7–8:** short phrases ("Ek is honger", "Can I play?") and simple questions with picture answers.
- **Ages 9–10:** little conversations: Bip and a friend talk in the new language, then the child answers
  one question; and a "Say it" game where the child picks the right sentence for a picture. Still
  listening and pointing only: no microphone, ever.

### Life Skills (CAPS Beginning Knowledge), woven in

CAPS Foundation Phase Life Skills (my body, seasons, safety, healthy food, South African animals and
places) isn't a separate island: it feeds the stories, Bip's Day scenes and Language Island themes, so the
content stays South African.

## 3. Same voice, older children

Everything for ages 7 and up uses Bip's Big voice lines (already built). Stories and questions for 8 to 10
are recorded in the "clear and friendly" style, read at a natural pace. One narrator for boys and girls.

## 3b. Art for ages 5 to 10 (owner, 18 October 2026: "Add to the plan")

- **One hand-drawn style for every age** (wobbly ink outlines, paper texture, warm bright colours; a
  locked decision). No second "older" style.
- **Age shows in the content, not the style:** young children (5–6) get simple single objects and
  friendly faces on things like the sun; older children (7–10) get richer scenes with more detail and fewer
  faces on objects, and "grown-up" pictures drawn from the content (clocks, rulers, scales, maps, graphs,
  fraction pizzas), like the Language Island's colour splashes and counting dots.
- **Boys and girls:** no pink/blue split and no princess/superhero sets. Every picture set mixes sport,
  animals, building, cooking, space and nature, with boys and girls of different South African looks
  doing all of them. Bip stays the robot everyone relates to.
- **South African life everywhere:** our animals, food, houses, weather, schools and money; never snow
  days or squirrels (the same rule as the words).
- **Done once:** every picture is hand-drawn since PR #52 (the emoji stand-ins replaced). The biggest new
  job is Bip's Stories: about 20 stories with 2 to 6 scenes each, shared by both languages, drawn in one
  pass when the stories are written, not piece by piece.

## 4. What needs building

| Step | What | New content | Rough clips (EN + AF) |
| --- | --- | --- | --- |
| 1 | Two new bands (`stage4`, `stage5`), age 9–10 starting points, CAPS references beside the Cambridge codes | skills and objectives | none |
| 2 | Bip's Stories: reader, questions, 20 stories per language over five levels (ages 5–10) | stories, questions | ~1,200 |
| 3 | Numbers for 9–10: Times Table Garden, Clock Shop, Measure It, Fraction Pizza | tables, times, measures | ~350 |
| 4 | Afrikaans grammar games (Klein Maak, Meer Maak, verlede tyd) and English Sentence Builder, Word Detective | word lists, sentences | ~500 |
| 5 | Coding for 9–10 (Bip's Factory, Garden Robot), Art levels, Language Island phrases and conversations | levels, phrases | ~300 |

Every game follows the existing template: one game file, content from data lists, random rounds checked by
tests, and both languages checked by the validator (South African English and AWS Afrikaans spelling).

CAPS has no short codes like Cambridge's (1Rw.01), so each skill gets a CAPS reference written as subject,
grade and topic (for example "CAPS Wiskunde Gr 3: Breuke"), checked against the CAPS documents.

## 5. Decisions (owner, 18 October 2026)

1. **Ages:** the course is planned for 5 to 10; the games that start at 4 stay, so a 4-year-old sibling
   can still play (profiles keep ages 3 to 10).
2. **First:** reading (Bip's Stories) before the maths for 9 and 10: it's the biggest gain for 8 to 10.
3. **Look:** one map for every age; older children get Bip's Big voice lines, not a different look.
4. **Checking:** a fluent Afrikaans reader (the owner, or a Grade 3–4 teacher) checks the Afrikaans
   stories and grammar games before they're recorded, through the Afrikaans Word Check page.
5. **Coding:** follow Cambridge Computing and map to CAPS Coding and Robotics once its documents are final.
6. **CAPS numbers:** the number ranges in section 2 are checked against the CAPS documents before each
   level is built.

## Sources

- Cambridge Primary covers Stages 1 to 6 for ages about 5 to 11:
  [Cambridge help centre](https://help.cambridgeinternational.org/hc/en-gb/articles/360000055178-Which-age-group-of-learners-is-the-Cambridge-Primary-Checkpoint-for-and-when-is-the-transition-to-Cambridge-Lower-Secondary),
  [Cambridge Primary brochure](https://WWW.CAMBRIDGEINTERNATIONAL.ORG/Images/599345-cambridge-primary-brochure.pdf).
- CAPS Coding and Robotics pilot and status:
  [DBE pilot page](https://www.education.gov.za/CodingRoboticsPilot.aspx),
  [Parliamentary question NW451](https://pmg.org.za/committee-question/25946/),
  [BusinessTech on the new subjects](https://businesstech.co.za/news/government/686269/government-finalising-new-subjects-for-schools-in-south-africa/).
- The game's current Cambridge mapping: `docs/CURRICULUM.md`; Afrikaans: `docs/LANGUAGES.md`.
