# Voice lab

A listening test for making narrator clips with an open-source model (Chatterbox, MIT licence) instead of
ElevenLabs. The model copies the voice of the reference clips (the original narrator's own game lines, so no
new voice sample is stored here) and speaks each line in `lines.tsv`.

Run: push to the `claude/voice-lab` branch (or start "Voice lab" by hand in GitHub Actions). The job runs on a
GitHub Linux runner (CPU), then commits the results to `voice-lab/out/` on the same branch as MP3s.
Nothing here ships in the game.
