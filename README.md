# HEX-panse
HEX-panse is a turn based strategy game that works as both a board game and video game. It uses a hexagonal grid for movement combined with a low-poly, isometric graphical style, and features a randomly generated game board. The game is designed for 2-6 players and they must fight using dice roll mechanics, to gain control of the wide expanse of land on an unfamilliar alien planet.

## Team Name
Mediocre Entertainment™

## Team Members
Aaron Molesbury (am375)

Solomon Baarda (sb169)

## Core concept
Turn based strategy game that works as both a board game and video game. Uses a hexagonal grid for movement and a low-poly, isometric graphical style. 

Features a randomly generated game board and dice roll mechanics. 2-6 players must fight it out for control of the island.

## Inspiration 
Civilization, Catan and Polytopia.

## Game Mechanics
### Game Setup
* The game board is a hexagon of size 6x6, featuring walkable land tiles and unwalkable tiles
* TODO IMAGE OF SETUP
* Players start at their own base, with a strength of 10
* Enemies spawn around the board at the start, with strength 0 to 3

### Turn Mechanics
* A player turn consists of one action - movement or fighting
* After each player has had 3 turns, then a "game turn" occurs
* During a game turn, each base on the board that is owned by a player or is an enemy base will receive 1 strength

### Movement Mechanics
* When in a base, player can move up to 4 tiles per turn
* When in the mech, player can move up to 2 tiles per turn
* A player can only move to tiles of the same height, or above or below by one height
* When a player leaves a base, they leave 1 strength at the base and take the rest
* If the player leaves a base and has only 1 strength, then the base will become unclaimed

### Fighting Mechanics
* To fight, players/bases must be on adjacent tiles
* The attacker can roll up to 10 dice, and the defender can roll up to 7 dice (limited by the number of strength they each have)
* Then respective dice are ordered from highest to lowest, and compared sequentially to determine who wins
* e.g. if defender=5 3 1 and attacker=6 2 1 1, then the defender would lose 1 (since 5<6) and the attacker would lose 2 (since 2<3 and 1=1 => defender always wins draws)  

### Base Claiming Mechanics
* To capture an enemy base, it must have strength 0 and the player must move onto that tile

### Win & Lose Conditions
* If a mech reaches strength 0, then it should respawn at the original player base if that exists, or any other base owned by that player if not. If there are no bases then the player is out of the game
* Last player alive wins