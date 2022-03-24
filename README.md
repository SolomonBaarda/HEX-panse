# HEX-panse
HEX-panse is a player vs player turn based strategy game that works as both a board game and video game. It uses a hexagonal grid for movement combined with a low-poly, isometric graphical style, and features a randomly generated game board. The game is designed for 2-6 players and they must fight using dice roll mechanics, to gain control of the wide expanse of land on an unfamiliar alien planet. The game board is generated randomly using procedural generation and features several colour schemes. 

## Team Name
Mediocre Entertainment™

## Team Members
Aaron Molesbury (am375)

Solomon Baarda (sb169)

## Inspiration 
Civilization, Risk, Catan and Polytopia.

## Videos
The design video can be found here: https://www.youtube.com/watch?v=OnS8N6_Rv7g

The prototype demo video can be found here: https://www.youtube.com/watch?v=xz-BvMVKj-o

## Game Mechanics
### Game Setup
* The game board is a hexagon of size 6x6, featuring walkable land tiles and unwalkable tiles
* Players start at their own base, with a health of 10
* Enemies spawn randomly around the board at the start, with health 0 to 3

### Turn Mechanics
* A player turn consists of one action - movement or fighting
* After each player has had 3 turns, then a "game turn" occurs
* During a game turn, each base on the board that is owned by a player or is an enemy base will receive 1 health

### Movement Mechanics
* When in a base, player can move up to 4 tiles per turn
* When in the mech, player can move up to 2 tiles per turn
* A player can only move to tiles of the same height, or above or below by one height
* When a player leaves a base, they leave 1 health at the base and take the rest
* If the player leaves a base and has only 1 health, then the base will become unclaimed

### Fighting Mechanics
* To fight, players/bases must be on adjacent tiles
* The attacker can roll up to 10 dice, and the defender can roll up to 7 dice (limited by the number of health they each have)
* Then respective dice are ordered from highest to lowest, and compared sequentially to determine who wins
* e.g. if defender=5 3 1 and attacker=6 2 1 1, then the defender would lose 1 (since 5<6) and the attacker would lose 2 (since 2<3 and 1=1 => defender always wins draws)  

### Respawning Mechanics
* If a player mech reaches a health level of zero, then the mech should respawn at a random base owned by that player
* If the player doesn't own any bases, then that player is out of the game if their mech dies 

### Base Claiming Mechanics
* To capture an enemy base, it must have zero health and the player must move onto that tile

### Win & Lose Conditions
* If a mech reaches health 0, then it should respawn at the original player base if that exists, or any other base owned by that player if not. If there are no bases then the player is out of the game
* Last player alive wins