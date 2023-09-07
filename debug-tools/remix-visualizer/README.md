# Remix Visualizer

**This tool only works with debug builds of the Infinite Beat Saber mod.** The relevant code is intentionally disabled on release builds of the mod.

When playing Infinite Beat Saber mode, this tool renders a visualization in your web browser that shows you which beat of the song is playing. It also visually shows when the algorithm chooses to seek the song to another beat rather than allowing it to continue sequentially. A seek is represented by a green line in the visualizer.

## Setup
- Run `npm install` to install the example's dependencies.

## Usage
Do all of these steps on the same computer:

- `node server.js` to start the web app.
- Open http://localhost:2020/ in your web browser.
- Launch Beat Saber and start playing a song in Infinite Beat Saber mode.
- (Refresh the web browser if necessary so it connects to Beat Saber).

## Third-party licenses
All code in the [`./web/third-party/`](./web/third-party/) folder uses the MIT license:

- [`jquery-ui.css`](./web/third-party/jquery-ui.css)
- [`raphael-min.js`](./web/third-party/raphael-min.js)
- [`three-dots.css`](./web/third-party/three-dots.css)

The code in the [`./web/algorithm/`](./web/algorithm/) folder uses the MIT license.
