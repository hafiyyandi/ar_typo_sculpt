# ar_typo_sculpt

## server.js
Server-side script. Has 3 functionalities:
1. Serve the default drawing commands of LeagueGothic.otf (/api/glyphs)
2. Check if location name is already in database. If yes, send the record. (/api/coords)
3. Save the translations of each point modified in the AR app (/api/save)

## Unity Scripts
### characterLoader.cs
1. Get the default drawing commands from server
2. Get location (lat,lng) using Unity's Location service. Feed it into Google Maps Reverse Geocoding to get location's Name / Address.
3. Check if location already exists in database. If yes, parse & store each point-translations.
4. Once plane is detected, initialize letters (drawLetters(Vector3 v)):
  1. Get default drawing commands of each character in location's Name.
  2. Only store the points (x,y of non-Z commands). Scale down the points, and translate the points by detected plane's location, and translations data from database.
  3. 
