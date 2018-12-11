# ar_typo_sculpt

## How the app works
0. Setup: a server that serves default drawing commands from LeagueGothic-Regular.otf
  * Based on (Allison's modify every glyph example) (https://editor.p5js.org/allison.parrish/sketches/SJwZn0wpQ)
1. App is initialized
2. Get device's location name, detect plane, check database
3. Draw location name on top of detected plane using spheres & lines.
4. User can select a sphere and move it around the physical space via moving the device.
5. The app auto-saves each sphere new coordinate, specific to the location name. When the user or another user opens the app in the same space, they will see the previously morphed letterforms.
6. VIEW MODE: the letters are drawn little by little using neon colored lights.

## Server (server.js)
Server-side script (http://68.183.20.22:8080). Has 3 functionalities:
1. Serve the default drawing commands of LeagueGothic-Regular.otf (/api/glyphs)
2. Check if location name is already in database. If yes, send the record. (/api/coords)
3. Save the translations of each point modified in the AR app (/api/save)

## Unity Scripts
### characterLoader.cs
1. Get the default drawing commands from server.
2. Get location (lat,lng) using Unity's Location service. Feed it into Google Maps Reverse Geocoding to get location's Name / Address.
3. Check if location already exists in database. If yes, parse & store each point-translations.
4. Once plane is detected, initialize letters (drawLetters(Vector3 v)):
  1. Get default drawing commands of each character in location's Name.
  2. Only store the points (x,y of non-Z commands). Scale down the points, and translate the points by (a) detected plane's location, (b) index of character and line, (c) translation data from database
  3. Initialize sphere (sphereController.cs)
  4. For each sphere, use the coordinate as points for white LineRenderer (lRend)
5. If in EDIT mode, render sphere and white line
6. If in VIEW mode, render animated line (animLines):
  1. Initialize: hide all spheres and white line, reset all animLines' lists.
  2. Store spheres' positions as point-pairs: starting & destination.
  3. Instantiate LineRenderer for each point-pair.
  4. For each point-pair, get points in between using Lerp. Cycle through the points in between.
  
### sphereHit.cs
1. Raycast from point of touch
2. If the ray hit an object tagged "sphere", call the function GetHit of the hit sphere.

### sphereController.cs
1. Once initialized, sphere stores:
  1. detected plane's location
  2. the index of the character
  3. default coordinates from default drawing command
2. If sphere is hit by Raycast, lock sphere to a child of the Camera object so that sphere always follow the camera.
3. Once hit sphere is hit again, release the sphere.
4. Parse the new coordinate, get translations for x,y,x to be stored in database. Formula : translations = current position - plane's position - default coordinate
5. Upload translations to server (/api/save) along with location name & index of character within name string.

### planeChecker.cs & planeCount.cs
Basically just makes sure that the letters are only drawn on top of the first plane detected.
