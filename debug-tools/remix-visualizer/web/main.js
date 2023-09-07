import makeVisualizer from './lib/makeVisualizer.js';
import BeatSaberClient from './lib/BeatSaberClient.js';

function setTitle(title, subtitle) {
  if (title !== undefined) document.getElementById('title').textContent = title;
  if (subtitle !== undefined) document.getElementById('subtitle').textContent = subtitle;
}

function fmtTime(totalSeconds) {
  totalSeconds |= 0;
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;

  const formattedMinutes = String(minutes).padStart(2, '0');
  const formattedSeconds = String(seconds).padStart(2, '0');

  return `${formattedMinutes}:${formattedSeconds}`;
}

async function main() {
  let visualizer = undefined;
  let songInfo = undefined;

  const beatSaberClient = new BeatSaberClient({
    onConnected: () => {
      setTitle('No song playing in Infinite Beat Saber mode', '');
    },
    onDisconnected: () => {
      setTitle('Not connected to Beat Saber', 'Refresh the page to connect');
    },
    onMessage: (cmd, args) => {
      switch (cmd) {
        case 'setSongInfo':
          if (songInfo) {
            if (songInfo.levelId !== args.levelId) {
              window.location.reload();
            }
          } else {
            songInfo = args;
            setTitle(args.songName + ' by ' + args.songAuthorName, '');
            const spotifyAnalysis = JSON.parse(args.spotifyAnalysis);
            visualizer = makeVisualizer(spotifyAnalysis, {
              isRunning: () => true,
              setNextTile: (tile) => {
                // The user clicked a beat to play in the visualization.
              },
              start: () => {
                // The user clicked a beat to play in the visualization and we're not
                // currently playing. Not providing an implementation since we don't
                // support the user clicking on beats in the visualization.
              },
            });
          }
          break;
        case 'setBeatIndex':
          visualizer && visualizer.setBeatIndex(args.beatIndex);
          setTitle(undefined, fmtTime(args.clock));
          break;
        case 'endSong':
          window.location.reload();
          break;
      }
    }
  });
  beatSaberClient.send('getSongInfo');
}

main();
