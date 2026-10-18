# The narrator voice ("Bip Island Narrator")

Every spoken clip in the game uses this one voice (CLAUDE.md, "Art and voice"). It was made with
ElevenLabs **Voice Design**, so it lives in the ElevenLabs account that designed it and doesn't move
to another account by itself. This page keeps everything needed to use or rebuild it elsewhere
(saved 11 October 2026, before the owner switched the ElevenLabs connector to another account).

## The voice

| | |
| --- | --- |
| Name | Bip Island Narrator |
| Voice ID | `Mq5hYfc3xyDzuW3pPMck` in the original account; `P7kHKZAtnEEhLBZBK8pP` in the second (see below) |
| Type | Designed voice (ElevenLabs category "generated") |
| Description | Official narrator for Bip Island: warm, friendly female South African English voice, like a kind primary school teacher for children aged 4–8. Clear, gentle, patient pace. |
| Labels | use_case: narrator · accent: South African English · gender: female · project: Bip Island |
| Model for clips | `eleven_v3` (default voice settings) |
| Clip format | Trimmed silence, 44.1 kHz mono AAC `.m4a` in `Resources/Audio/` |

No voice samples are kept in the repository: it's public, and a sample is all anyone would need to
clone the children's narrator. (A cloning sample and the old preview were committed briefly on
11 October and removed again; they remain in the git history of that day's merge.)

## How new clips are made (from 18 October 2026)

With **Chatterbox** (Resemble AI, open source, MIT licence), which copies her voice from two of her own
game clips, `vo_welcome` and `vo_bip_charging` (about 16 seconds). `scripts/speak_clips.py` speaks each
clip's text from the manifest or `audio/script.csv` with a fixed seed and the default settings
(exaggeration 0.5, cfg 0.5), trims it and levels it to -14.5 LUFS like the rest. The **Narrator** GitHub
Actions workflow runs it: list the clips (names or patterns) in `voice-lab/request.txt` on a `claude/`
branch and push; it records the missing ones and commits them to that branch. No ElevenLabs account,
credits or voice ID is involved. The owner chose this by ear on 18 October (listening test on the
`claude/voice-lab` branch). Pure phonics sounds (`snd_`) stay ElevenLabs recordings.

## In the owner's second ElevenLabs account (11–17 October 2026, no longer used)

The voice was designed again there from the description above (`eleven_ttv_v3`, three previews; the
owner picked one) and saved as **Bip Island Narrator**, voice ID `P7kHKZAtnEEhLBZBK8pP`. It's close to
the original but not identical. No clips in the game use it: the ones recorded with it were replaced by Chatterbox
clips of the original voice.

## Using it from another ElevenLabs account

Pick one:

1. **Share it (keeps the exact voice).** In the original account: ElevenLabs → Voices → Bip Island
   Narrator → Share, and copy the share link. In the new account, open the link and add the voice to
   "My voices". It gets a new voice ID there: put that ID in CLAUDE.md, PLAN.md and STATUS.md.
2. **Clone it (very close, any account on a paid plan).** In the new account: Voices → Add a new
   voice → Instant Voice Clone, upload about 3 minutes of her `vo_`, `praise_` and `hint_` clips from
   `Resources/Audio/` joined into one file (made locally, never committed), name it "Bip Island Narrator",
   and paste the description above. Ear-check a few phonics sounds (`snd_s`, `snd_t`, `snd_h`)
   against the old clips before recording anything new, and note the new voice ID in the docs.

Background music (`Resources/Music/`) doesn't use the voice, so any account can make it.
