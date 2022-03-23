
# Prototype Video

## Technologies Used

Unity - Aaron and I are both very familiar with it, its easy to complete quick prototypes, very good for 3D

Maya - Aaron used maya to create some basic models, mech, player bases and enemy bases

GitHub - version control and contains builds of the game if anyone wants to play. Will be added to the description of this video 

## Main Menu
Page displaying information about the game describes the core objectives

Final version would have an interactive tutorial 

Menu contains a seed input for selecting the randomly generated map

Selection for choosing the number of players

## Main Game

Turn based strategy game, hexagonal grid for movement 

Each player can complete one action on their turn, either move or attack

## Main Mechanics

Movement - A mech can move up to two tiles per turn, and only to tiles which are the same hight or slightly above or below. If a mech is currently in a base then it can move up to four tiles. This creates interesting dynamics as it means players can traverse the board very quickly as they gain more control.

Attacking - The player can choose to attack a mech or a base if it is on an adjacent tile. This will use dice roll mechanics to determine how much health each player should lose. The mechanic has been designed specifically to ensure that fights are fair when players have a similar health, or a dominant player will easily win the fight if they significantly outnumber their opponent.

Base capturing - When a fight ends, if a player defeats a base then they will capture it and will receive reinforcement health every three turns. 

Respawning - If a is defeated in a fight then it will be killed, and will respawn at a random base owned by that player. If the player does not control any bases then they are out of the game. 

To win, a player must be either the last player alive.

## Planned Mechanics

There are several significant mechanics that are planned for the final game but didn't make it into the prototype.

### Customisable Mechs

Players should assign skill points to various categories at the beginning of the game, to customise the behavior of their mech. 

Players would be able to choose between melee or ranged attacks, they could increase their attack strength, attack range, add an area of attack effect, they could increase their defence strength

Players could also increase the number of tiles their mech could move each turn

These mechanics would really make this game stand out from the rest as it would enable players to use different play styles, and would significantly increase the replayability of the game. 

### Online Multiplayer

Online multiplayer is planned for the final game, though this would require a significant amount of work to implement as it would require new features to work such as timed rounds, party/lobby/invite system, voice chat and/or text chat, netcode, game servers etc

### Additional Win Conditions

Additional win conditions are also planned as currently games can take a very long time and players can reach stalemates. To fix this, any player controlling all bases on the board should win. 

Additionally, secret challenges might be implemented, like in the board game Risk. This would include challenges such as knocking a certain player out of the game, controlling a certain number of bases etc 

### Graphical Changes

Graphical changes - revamp models, add more models for trees, rocks etc, add textures, different lighting for each map, lava bloom and particle effect, animations for player moving and attacking etc

Different post processing for each scene

## Feedback Implemented
Wanted more feedback for the user, was confusing what was happening in the game:
* Added a move preview and attack highlights (green and red)
* Added health +- text when fightinhg so clear who is taking damage
* Changed "strength" to "health" as is was confusing why value was decreasing during fights

Small graphical changes:
* Added more idle animation - smoke from the chimneys and flames when mechs are moving
* Made the lava material emit light






# Design Video
-	Idea behind the game (boardgame – risk esque game) conquering space
-	inspiration(civ, risk, polytopia); how it works, why is it enjoyable, main USPs
-	the main board concepts; display the paperprototypes
-	feedback on idea; go through the concept with people and see if they like it (done)
-	gloss over rules, mechanics
-	give reason to buy, sell the idea of fun to play, whats the main thing that will keep people playing
-	touch on the concepts of expansion ideas for future builds
## Design Concepts

