# How to Add Additional Levels (Incomplete)

**Note:** This document is incomplete. It's more of a brain dump of the techniques I use when trying to add a song to Infinite Beat Saber.

If you want to add a song and feel adventurous, you can try out the techniques in this doc. Another option is to request the song by opening a GitHub issue.

## Aligning the Beat Saber & Spotify Songs

The main task in adding support for an additional level is aligning the Beat Saber song with Spotify's version of the song. That way the Infinite Jukebox alogrithm can use the data from [Spotify's audio analysis web API](https://developer.spotify.com/documentation/web-api/reference/get-audio-analysis) to create an infinitely long remix of the Beat Saber song.

Note that so far the songs I've added didn't need any adjustments: the Beat Saber and Spotify songs were already aligned. Consequently, I don't know of good techniques for finding the offset between the Beat Saber and Spotify songs. But below I'll describe the kind of analysis I've used so far.

The output of these steps should be:
- `spotifyAnalysis.json`: A JSON file from Spotify's audio analysis API.
- `spotifyShiftTimestampsSeconds`: The number of seconds by which the Spotify song should be shifted so that it's aligned with the Beat Saber song.

Here are the steps:
1. Decide which Beat Saber *custom level* you'd like to add support for (built-in levels aren't currently supported). For this guide I'll use [Gangnam Style by PSY (mapped by GreatYazer)](https://bsaber.com/songs/141/) as an example.
1. Install the song in Beat Saber if you haven't already.
1. Create a folder called "Gangnam Style Files" in your Documents folder or in your preferred location. This folder will be used to collect various files as you work.
1. Copy the Beat Saber audio for the level into the "Gangnam Style Files" folder. I found the audio file located at `C:\Program Files (x86)\Steam\steamapps\common\Beat Saber\Beat Saber_Data\CustomLevels\141 (Gangnam Style - greatyazer)\song.egg`.
1. Next to the song file should be a file called `Info.dat`. It contains various information about the level. Copy that file into "Gangnam Style Files".
1. Open `Info.dat` and note the field labeled `_songTimeOffset`. When the level begins, this represents how many seconds of the audio file should be skipped.
1. Find the same song on Spotify and then download its audio analysis file. See [here](https://github.com/rigdern/InfiniteJukeboxAlgorithm/tree/main/tools/spotifyAudioAnalysisClient) for a tool and detailed instructions on how to do that. When I downloaded the audio analysis file, I saved it as `Gangname Style Files\spotifyAnalysis.json`.
1. Use the [spotifyBeatMetronome tool](https://github.com/rigdern/InfiniteJukeboxAlgorithm/tree/main/tools/spotifyBeatMetronome) to generate an audio file which plays a tick at each beat identified by Spotify's audio analysis. This will help us to understand where the beats are in Spotify's version of the song. I saved the resulting audio file at `Gangnam Style Files\spotifyAnalysisBeats.wav`.
1. Use [ArrowVortex](https://arrowvortex.ddrnl.com/) or your preferred technique for determining the song's tempo/BPM. You can check the result against the `_beatsPerMinute` in `Info.dat`. See [this video](https://youtu.be/Z49UKFefu5c) for an ArrowVortex tutorial.
1. Follow these steps in [Audacity](https://www.audacityteam.org/) (an audio editor).
    1. Create a new Audacity project.
    1. Add these files as tracks of the Audacity project:
        - `Gangnam Style Files\song.egg` (or whatever the name of the Beat Saber audio file may be). In Audiacity, name the track "Song" by right-clicking on its title and selecting "Name" from the context menu.
        - `Gangnam Style Files\spotifyAnalysisBeats.wav`. This track represents the beats of the Spotify song. In Audacity, name the track "Spotify Beats".
    1. Delete the first `_songTimeOffset` seconds of the "Song" track.
    1. Use the BPM identified by ArrowVortex to add a click track in Audacity. See the "Add a Click Track" section of [this page](https://bsmg.wiki/mapping/basic-audio.html#add-a-click-track) for a tutorial. This track represents the beats of the Beat Saber song. In Audacity, name the track "BPM". You should now have 3 tracks in Audacity:
        - "Song", the audio for the song.
        - "Spotify Beats", the timestamps of the beats in Spotify's version of the song.
        - "BPM", the timestamps of the beats in Beat Saber's version of the song.
    1. In Audacity, mute the "Spotify Beats" track by clicking its "Mute" button.
    1. In Audacity, hit the play button to listen to the "Song" and "BPM" tracks. Does it sound like the BPM track is in sync with the song's beats? If not, maybe the BPM value is wrong or the BPM track needs to be shifted by some fraction of a second. When the "BPM" track sounds aligned to the song's beats, proceed to the next step.
    1. Look at the "BPM" and "Spotify Beats" tracks. The "BPM" track represents the timestamps of the Beat Saber song's beats and the "Spotify Beats" track represents the timestamps of the Spotify song's beats. Are the "BPM" track's beats visually aligned with those of the "Spotify Beats" track? If not, shift the "Spotify Beats" track to the left or right until the beats line up. There's more work to do: the Beat Saber & Spotify songs may now be on beat but they may be on different beats. Another technique is to listen to the Beat Saber and Spotify songs. By listening, try to find the first timestamp where the Beat Saber and Spotify songs are aligned (sound the same). Then use this information to find the right shift for the "Spotify Beats" track in Audacity. When you feel happy with your result, note how many seconds you've shifted the "Spotify Beats" track by &mdash; this is your value for `spotifyShiftTimestampsSeconds`. (Sorry these instructions are vague and incomplete. I hope to come up with better techniques and tools in the future.)
    1. Try playing the level in Infinite Beat Saber mode and see if it feels right. To do this take `spotifyAnalysis.json`, `spotifyShiftTimestampsSeconds`, and head to the [Updating the Code](#updating-the-code) section.

## Updating the Code

1. Add an entry for your song to the [`_levelIdToSpotifyAnalysisInfo` dictionary in RemixableSongs.cs](https://github.com/rigdern/InfiniteBeatSaber/blob/ba5ed9e98a7f683f703973f116b92ee5206bf44c/InfiniteBeatSaber/RemixableSongs.cs#L10). There's a comment there that describes the meaning of the data. You'll need to find the level's ID. One option for doing that is to uncomment [these lines in `InfiniteBeatSaberMenuUI.OnDidChangeDifficultyBeatmap`](https://github.com/rigdern/InfiniteBeatSaber/blob/ba5ed9e98a7f683f703973f116b92ee5206bf44c/InfiniteBeatSaber/InfiniteBeatSaberMenuUI.cs#L46-L47). This will generate a log line like this each time you tap a level in the level selection menu:
    - `InfiniteBeatSaberMenuUI.OnDidChangeDifficultyBeatmap: Gangnam Style by PSY, GreatYazer (ID: custom_level_8E7E553099436AF31564ADF1977A5EC42A61CFFF)`

    This tells us that the level ID of Gangnam Style is `custom_level_8E7E553099436AF31564ADF1977A5EC42A61CFFF`.
1. Add your Spotify analysis file to the "SpotifyAnalyses" folder.
1. Now you can try out your change by recompiling the mod and launching Beat Saber.

## Unsupported Beatmap Features

Not all features that a beatmap might contain are currently supported by Infinite Beat Saber.

When you play a level in Infinite Beat Saber mode, there will either be a log line like this indicating that all of the level's features are supported:

```
BeatmapRemixer.FilterBeatmapDataItems: All beatmap item types are supported.
```

Or a log line like this indicating the list of unsupported features used by the level:

```
BeatmapRemixer.FilterBeatmapDataItems: Ignoring unsupported beatmap item types:
  ExampleBeatmapItemType1
  ExampleBeatmapItemType2
  etc.
```

If you see the latter, you can either investigate adding support for the feature to [BeatmapRemixer.cs](../InfiniteBeatSaber/BeatmapRemixer.cs) or open a GitHub issue about it.
