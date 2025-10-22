![screenshot](./Img/game_screen_1.png)
# Game of Life
О чём: Game of Life с двумя режимами: 
 * **Casual**: есть возможность попробовать разный neighborhood для клеток и всемозможно поиграться с правилами. Есть множество функций (обозначено в управлении ниже)
 * **PvP**: локальный режим ПвП (hot seat), два игрока имеют по 128 клеток и поле 128x128, необходимо расставить доступные формы так, чтобы клетки вашего цвета порождали больше новых клеток (правила обозначены ниже), но чем больше форма - тем больше клеток она стоит, так что (какой-никакой) стратегический аспект сохранён.

## Legend (Casual):
Esc = Quit

**Simulation:**
R = Fill random cells
C = Clear cells
Scroll wheel = Zoom In/Out
Space = Pause
WASD = Move simulation
+/- = Regulates game speed

**Custom Neighborhood:**
Toggle = Neighborhood on/off
Clear = Clear
Square = Draw/Erase (LMB/RMB) square of increasing size
Circle = analogichno, but circle
Random = Random neighborhood fill
Scale = Changes grid size

Buttons S/B = Assign segments in which cells are stable or born (ex. Default = Conway Ruleset)
You can LMB/RMB to increase the counter or DRAG it up or down while LMB pressed

You can also draw/erase (LMB/RMB) on neighborhood field itself 

## Legend (PvP):
Esc = Quit

Well... that's it.

**Rules:**
30s match, highest score wins
Controls: LMB = Red, RMB = Blue
One shared shape selector
Match starts when (presumably) both players toggle "Ready"
Each birth gives +1/-1 (Red/Blue) to its owner

After 30s: score > 0: Red wins
 else Blue wins
(yeah, it's sligtly unfair)