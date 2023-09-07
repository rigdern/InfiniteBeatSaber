const express = require('express');
const path = require('path');

const app = express();

const port = 2020;
const webRoot = path.resolve(__dirname, 'web');

app.use(express.static(webRoot));

app.listen(port, () => {
    console.log(`Server running on http://localhost:${port}`);
});
