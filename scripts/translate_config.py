import os
import re

translations = {
    "Mimesis Player Enhancement": "Улучшения Mimesis",
    "Mod Toast Duration (seconds)": "Длительность уведомления (секунды)",
    "How long [PlayerEnhancements] toasts stay visible before fading. Vanilla join/leave toasts are unchanged (~2 seconds). Each player controls this locally.": "Как долго уведомления [PlayerEnhancements] остаются на экране. Ванильные уведомления не меняются (~2 сек). Настраивается индивидуально.",
    "Enable Debug Logging": "Включить отладочное логирование",
    "Emit verbose diagnostic lines to the MelonLoader console.": "Выводить подробные диагностические сообщения в консоль MelonLoader.",

    "More Players": "Больше игроков",
    "Enable More Players": "Включить увеличение числа игроков",
    "Raise the multiplayer player cap above 4.": "Увеличить лимит игроков в сессии (больше 4).",
    "Max Players": "Макс. игроков",
    "Maximum players in a session including the host (1 = solo, 2 = host + 1 client, etc.).": "Максимальное количество игроков, включая хоста (1 = соло, 2 = хост + 1 клиент и т.д.).",

    "More Voices": "Больше голосов (Mimic)",
    "Enable More Voices": "Включить расширение голосов",
    "Raise per-player voice recording limits.": "Увеличить лимиты записи голосов игроков для мимика.",
    "Max Indoor Voice Events": "Макс. записей (Внутри)",
    "Maximum stored voice events per player in indoor dungeon runs (default game limit is much lower).": "Максимум хранимых записей голоса для подземелий.",
    "Max Deathmatch Voice Events": "Макс. записей (Deathmatch)",
    "Maximum stored voice events per player in deathmatch (default game limit is much lower).": "Максимум хранимых записей голоса для детматча.",
    "Max Outdoor Voice Events": "Макс. записей (Снаружи)",
    "Maximum stored voice events per player outdoors (default game limit is much lower).": "Максимум хранимых записей голоса на улице.",

    "Persistence": "Сохранение данных",
    "Enable Persistence": "Включить сохранение данных",
    "Save and restore mimic voice recordings across save/load.": "Сохранять и восстанавливать голосовые записи мимика после перезапуска игры.",

    "Statistics": "Статистика",
    "Enable Statistics": "Включить статистику",
    "Track per-session and global player statistics per save slot.": "Отслеживать статистику игроков за сессию и за всё время (по слотам сохранения).",
    "Session Reconnect Grace (minutes)": "Окно переподключения (минуты)",
    "Reuse the previous session when a player reconnects within this many minutes.": "Использовать ту же сессию, если игрок переподключился в течение указанного времени.",
    "Show Statistics Toasts": "Показывать статистику в уведомлениях",
    "Show mod stats toasts in plain English (session intro for you, global stats on join/leave). Does not replace the game's own connect messages.": "Показывать уведомления со статистикой мода (при входе/выходе). Не заменяет сообщения игры.",

    "Player Announcements": "Игровые уведомления",
    "Enable Player Announcements": "Включить игровые уведомления",
    "Show in-game toasts for dungeon run settings, boss spawns, and your per-map stats when you die. Does not replace the game's own messages.": "Показывать уведомления о настройках, боссах и личной статистике при смерти. Не заменяет сообщения игры.",

    "Join Anytime": "Присоединение во время игры",
    "Enable Join Anytime": "Включить присоединение во время игры",
    "Allow players to join a session after it has already started.": "Позволять игрокам подключаться к сессии после того, как игра уже началась.",
    "Join Connection Grace Seconds": "Время ожидания при подключении (сек)",
    "When a player connects, block tram departure for this many seconds. Players who fail to finish loading are kicked (host is never kicked).": "При подключении игрока трамвай не отправится указанное число секунд. Застрявшие исключаются.",

    "Spawn Scaling": "Масштабирование врагов",
    "Enable Spawn Scaling": "Включить масштабирование врагов",
    "Scale dungeon monster spawn budgets by type. Host only.": "Масштабировать количество монстров по типам. Только для хоста.",
    "Auto Scale Mimic Spawns By Player Count": "Авто-масштабирование Мимиков",
    "When enabled, multiply mimic spawn budgets by player count / 4 for sessions with more than 4 players (stacks with MimicSpawnMultiplier).": "Умножает количество мимиков на (число игроков / 4) при онлайне больше 4.",
    "Mimic Spawn Multiplier": "Множитель Мимиков",
    "Total mimic spawn budget across the run, including periodic spawns (1 = vanilla, 2 = double).": "Общий бюджет появления мимиков (1 = ванилла, 2 = в два раза больше).",
    "Auto Scale Boss Spawns By Player Count": "Авто-масштабирование Боссов",
    "When enabled, multiply boss spawn budgets by player count / 4 for sessions with more than 4 players (stacks with BossSpawnMultiplier).": "Умножает количество боссов на (игроков / 4) при онлайне больше 4 человек.",
    "Boss Spawn Multiplier": "Множитель Боссов",
    "Map-placed bosses: activates unused alternate markers and schedules bonus encounters after kill (1 = vanilla, 2 = double).": "Включает альт. маркеры боссов и спавнит доп. боссов (1 = ванилла, 2 = в два раза больше).",
    "Auto Scale Jako Spawns By Player Count": "Авто-масштабирование Жако",
    "When enabled, multiply jako spawn budgets by player count / 4 for sessions with more than 4 players (stacks with JakoSpawnMultiplier).": "Умножает количество Жако на (игроков / 4) при онлайне больше 4 человек.",
    "Jako Spawn Multiplier": "Множитель Жако",
    "Total normal-monster threat budget for ambient dungeon spawns (1 = vanilla, 2 = double).": "Обычные монстры в подземелье (1 = ванилла, 2 = в два раза больше).",
    "Auto Scale Special Spawns By Player Count": "Авто-масштабирование особых врагов",
    "When enabled, multiply special spawn budgets by player count / 4 for sessions with more than 4 players (stacks with SpecialSpawnMultiplier).": "Умножает количество особых врагов на (игроков / 4) при онлайне больше 4 человек.",
    "Special Spawn Multiplier": "Множитель особых врагов",
    "Special monster budget for periodic spawns and map-placed specials (1 = vanilla, 2 = double).": "Бюджет особых монстров для периодических спавнов и точек на карте.",
    "Auto Scale Trap Spawns By Player Count": "Авто-масштабирование ловушек",
    "When enabled, multiply trap spawn counts by player count / 4 for sessions with more than 4 players (stacks with TrapSpawnMultiplier).": "Умножает количество ловушек на (игроков / 4) при онлайне больше 4 человек.",
    "Trap Spawn Multiplier": "Множитель ловушек",
    "Map-placed traps: activates unused alternate markers and schedules bonus encounters after trigger/kill (1 = vanilla, 2 = double).": "Включает альт. маркеры и спавнит доп. ловушки (1 = ванилла, 2 = в два раза больше).",
    "Map-Placed Encounter Delay Min (seconds)": "Мин. задержка спавна (сек)",
    "Shortest wait after a map-placed enemy, trap, or loot marker is cleared before the next bonus encounter from scaling can appear there.": "Мин. ожидание после зачистки точки перед спавном бонусного объекта.",
    "Map-Placed Encounter Delay Max (seconds)": "Макс. задержка спавна (сек)",
    "Longest wait for that random delay. Actual delay is picked between min and max.": "Макс. ожидание случайной задержки спавна бонусного объекта.",
    "Map-Placed Encounter Min Player Distance (m)": "Мин. дистанция спавна от игрока (м)",
    "After the delay, hold the spawn until no living players are within this radius of the marker. 0 = spawn as soon as the delay elapses.": "Не спавнить объекты, пока в этом радиусе есть игроки. 0 = спавнить мгновенно.",
    "Auto Scale Other Spawns By Player Count": "Авто-масштабирование остальных",
    "When enabled, multiply other spawn counts by player count / 4 for sessions with more than 4 players (stacks with OtherSpawnMultiplier).": "Умножает количество остальных объектов на (игроков / 4) при онлайне больше 4 человек.",
    "Other Spawn Multiplier": "Множитель остальных объектов",
    "Spawn multiplier for entities that are not mimics, bosses, jakos, specials, or traps.": "Множитель сущностей, не являющихся мимиками, боссами, жако, особыми врагами и ловушками.",

    "Loot Multiplicator": "Умножитель лута",
    "Enable Loot Multiplicator": "Включить умножитель лута",
    "Scale map loot and enemy death drops, and optionally convert mimic fake drops to real loot. Host only.": "Масштабирует лут на карте и предметы с врагов. Только для хоста.",
    "Auto Scale Map Loot By Player Count": "Авто-масштабирование лута на карте",
    "Map loot = items placed on the dungeon map (spawn markers, shelves, floors). When enabled, multiply by player count / 4 above 4 players (stacks with MapLootMultiplier).": "Умножает лут на полках и полу на (игроков / 4) при онлайне больше 4.",
    "Map Loot Multiplier": "Множитель лута на карте",
    "Multiplier for all map-placed pickup loot: fixed markers, respawn counts, and random pool budgets. 1 = vanilla, 2 = double.": "Множитель лута, который можно подобрать на карте (1 = ванилла, 2 = двойной).",
    "Auto Scale Drop Loot By Player Count": "Авто-масштабирование лута с врагов",
    "Drop loot = items from enemy death tables when killed. When enabled, multiply by player count / 4 above 4 players (stacks with DropLootMultiplier).": "Умножает лут с врагов на (игроков / 4) при онлайне больше 4.",
    "Drop Loot Multiplier": "Множитель лута с врагов",
    "Multiplier for enemy death drops: extra weighted re-rolls from drop tables and consumable stack count on spawn. 1 = vanilla, 2 = double.": "Множитель лута с мертвых врагов (1 = ванилла, 2 = двойной).",
    "Loot Item Filter Mode": "Режим фильтра лута",
    "All = every item can be scaled; AllowlistOnly = only comma-separated master IDs in LootAllowlist; BlocklistOnly = all items except LootBlocklist.": "All = все предметы; AllowlistOnly = только IDs из белого списка; BlocklistOnly = все, кроме черного списка.",
    "Loot Allowlist": "Белый список лута",
    "Comma-separated item master IDs (e.g. 12345,67890). Used when LootItemFilterMode is AllowlistOnly. See docs/LOOT_ITEM_IDS.md in the repo for the full list.": "ID предметов через запятую. Работает при режиме AllowlistOnly.",
    "Loot Blocklist": "Черный список лута",
    "Comma-separated item master IDs to exclude from scaling. Used when LootItemFilterMode is BlocklistOnly. See docs/LOOT_ITEM_IDS.md in the repo for the full list.": "ID предметов через запятую для исключения из масштабирования.",
    "Convert Fake Death Drops To Real Chance": "Шанс превращения фейк-лута",
    "Chance (0-100) that fake items dropped on enemy death (ActorDying, e.g. mimic inventory decoys) become real pickup loot. 0 = vanilla (fake items vanish on grab), 100 = always real. Monster drop-table loot is already real.": "Шанс (0-100%), что фейковые предметы из мимика превратятся в настоящий лут.",

    "Money Multiplier": "Множитель денег",
    "Enable Money Multiplier": "Включить множитель денег",
    "Scale startup money, round goal quota, scrap/sell values, shop buy prices, and reinforce costs. Host only.": "Изменяет стартовые деньги, квоту, цены продажи/покупки и стоимость улучшений.",
    "Auto Scale Startup Money By Player Count": "Авто-масштаб стартовых денег",
    "When enabled, multiply startup money by player count / 4 for sessions with more than 4 players (stacks with StartupMoneyMultiplier).": "Умножает стартовые деньги на (игроков / 4) при онлайне больше 4.",
    "Startup Money Multiplier": "Множитель стартовых денег",
    "Starting maintenance-room currency on a new save slot or session reset to vanilla initial money (1 = vanilla, 2 = double). Does not apply when loading a save game.": "Начальные деньги при старте нового сохранения (1 = ванилла, 2 = двойные).",
    "Auto Scale Round Goal Money By Player Count": "Авто-масштаб квоты этапа",
    "When enabled, multiply the stage target currency (quota) by player count / 4 for sessions with more than 4 players (stacks with RoundGoalMoneyMultiplier).": "Умножает квоту на (игроков / 4) при онлайне больше 4.",
    "Round Goal Money Multiplier": "Множитель квоты этапа",
    "Target currency required to finish a stage (1 = vanilla, 2 = double).": "Сумма, необходимая для завершения уровня (1 = ванилла, 2 = двойная).",
    "Auto Scale Scrap Sell Value By Player Count": "Авто-масштаб ценности продажи",
    "When enabled, multiply item scrap/sell values by player count / 4 for sessions with more than 4 players (stacks with ScrapSellValueMultiplier).": "Умножает ценность продаваемых предметов на (игроков / 4).",
    "Scrap Sell Value Multiplier": "Множитель цены продажи",
    "Currency earned when scrapping items and item value counted toward the tram quota (1 = vanilla, 2 = double).": "Множитель цены продаваемого лома в трамвае (1 = ванилла, 2 = в два раза дороже).",
    "Auto Scale Shop Buy Price By Player Count": "Авто-масштаб цен в магазине",
    "When enabled, multiply maintenance shop buy prices by player count / 4 for sessions with more than 4 players (stacks with ShopBuyPriceMultiplier).": "Умножает цены на предметы в магазине на (игроков / 4).",
    "Shop Buy Price Multiplier": "Множитель цен в магазине",
    "Maintenance shop and vending-machine kiosk purchase cost multiplier (1 = vanilla, 2 = double). Applied when shop items are initialized each maintenance round.": "Множитель стоимости покупки в магазине и автоматах.",
    "Shop Discount Min Percent": "Мин. скидка в магазине (%)",
    "Minimum shop discount percentage when a discount is rolled (0-100). Only used when ShopDiscountChancePercent is above 0.": "Минимальная скидка на предмет, если он получил скидку.",
    "Shop Discount Max Percent": "Макс. скидка в магазине (%)",
    "Maximum shop discount percentage when a discount is rolled (0-100). Must be >= ShopDiscountMinPercent.": "Максимальная скидка на предмет, если он получил скидку.",
    "Shop Discount Chance Percent": "Шанс скидки в магазине (%)",
    "Chance per shop item to receive a discount between min and max percent (0 = vanilla shop discounts, 100 = every item discounted).": "Шанс применения случайной скидки на предметы в магазине (0 = выкл).",
    "Auto Scale Reinforce Price By Player Count": "Авто-масштаб цены улучшений",
    "When enabled, multiply item reinforcement costs by player count / 4 for sessions with more than 4 players (stacks with ReinforcePriceMultiplier).": "Умножает стоимость улучшений на (игроков / 4) при онлайне больше 4.",
    "Reinforce Price Multiplier": "Множитель цены улучшений",
    "Maintenance item reinforcement cost multiplier (1 = vanilla, 2 = double).": "Множитель стоимости усиления предметов (1 = ванилла, 2 = в два раза дороже).",

    "Dungeon Time": "Время в подземелье",
    "Enable Dungeon Time": "Включить управление временем",
    "Extend dungeon shift length on the host when player count exceeds the baseline.": "Продлевает время прохождения подземелья, когда игроков больше стандарта.",
    "Dungeon Time Baseline Player Count": "Стандартное кол-во игроков",
    "No extra shift time at or below this player count (vanilla is 4). Minimum is 1.": "Без доп. времени при этом числе игроков и меньше (по умолчанию 4).",
    "Extra Shift Seconds Per Player Above Baseline": "Доп. время (сек) за каждого сверх лимита",
    "Real seconds added to the shift deadline for each player above the baseline. Minimum is 0.": "Сколько секунд добавляется ко времени подземелья за каждого лишнего игрока.",

    "Mimic Tuning": "Настройки Мимика",
    "Enable Mimic Tuning": "Включить настройки Мимика",
    "Tune dead-player mimic possession speak duration and cooldown on the host.": "Настраивает время речи и перезарядку при вселении мертвых игроков в мимика.",
    "Randomize Mimic Possession Duration": "Случайное время речи мимика",
    "Roll a random speak window per E-possession between min and max seconds below. Host only.": "Случайное время речи при использовании способности от мин. до макс. секунд.",
    "Mimic Possession Min Time (seconds)": "Мин. время речи Мимика (сек)",
    "Minimum rolled speak duration in seconds (vanilla is 12). Host only.": "Минимальная длительность речи (по умолчанию 12).",
    "Mimic Possession Max Time (seconds)": "Макс. время речи Мимика (сек)",
    "Maximum rolled speak duration in seconds (vanilla is 12). Host only.": "Максимальная длительность речи (по умолчанию 12).",
    "Mimic Possession Cooltime Multiplier": "Множитель перезарядки Мимика",
    "Multiplier for wait time after mimic possession before the next E-possession (1 = vanilla). Host only.": "Множитель задержки перед следующим вселением (1 = ванилла).",

    "Player Tuning": "Настройки игрока",
    "Enable Player Tuning": "Включить настройки игрока",
    "Scale player move speed, stamina, and carry weight on the host. Joining clients do not need the mod.": "Масштабирует скорость, выносливость и переносимый вес (нужно только хосту).",
    "Move Speed Multiplier": "Множитель скорости бега",
    "Scales walk and run base speed (1 = vanilla, 2 = double). Host only.": "Множитель скорости ходьбы и бега (1 = ванилла, 2 = в 2 раза быстрее).",
    "Max Stamina Multiplier": "Множитель выносливости",
    "Scales maximum stamina (1 = vanilla, 2 = double). Host only.": "Множитель максимальной выносливости (1 = ванилла, 2 = двойная).",
    "Stamina Drain Multiplier": "Множитель расхода выносливости",
    "Scales sprint stamina cost per tick (1 = vanilla, 0.5 = half drain). Host only.": "Множитель расхода выносливости при беге (1 = ванилла, 0.5 = половина).",
    "Stamina Regen Multiplier": "Множитель регена выносливости",
    "Scales stamina recovered per regen tick (1 = vanilla, 2 = double). Host only.": "Множитель скорости восстановления выносливости (1 = ванилла).",
    "Stamina Regen Delay Multiplier": "Множитель задержки регена",
    "Scales wait time before stamina regen starts after sprinting (1 = vanilla, 0.5 = regen starts sooner). Host only.": "Задержка перед восстановлением выносливости после бега.",
    "Max Carry Weight Multiplier": "Множитель переносимого веса",
    "Scales carry capacity before encumbrance slows movement (1 = vanilla, 2 = double capacity). Host only.": "Влияет на вес, при котором начинается замедление.",

    "Dungeon Randomizer": "Рандомизатор подземелий",
    "Enable Dungeon Randomizer": "Включить рандомизатор подземелий",
    "Randomize dungeon selection on the host: tram dungeon pick, layout flow, map variant, and procedural seed. Host only.": "Рандомизирует выбор подземелья, варианты карты и генерацию лабиринта.",
    "Randomize Dungeon Pick": "Случайный выбор подземелья",
    "Override which dungeon master ID is rolled on the tram.": "Переопределяет выбранное подземелье при отправке трамвая.",
    "Dungeon Pick Pool Mode": "Режим выбора подземелий",
    "WidenVanilla = keep cycle weights but allow repeats sooner; AllActiveUniform = pick uniformly from all active dungeons (ignores cycle table).": "WidenVanilla = ванильный с повторами; AllActiveUniform = полностью случайный.",
    "Dungeon Allowlist": "Белый список подземелий",
    "Comma-separated dungeon master IDs. When non-empty, only these IDs are eligible.": "ID подземелий через запятую (будут выпадать только они).",
    "Dungeon Blocklist": "Черный список подземелий",
    "Comma-separated dungeon master IDs to exclude from the pool.": "ID подземелий через запятую (никогда не выпадут).",
    "Ignore Dungeon Exclude List": "Игнорировать список исключений",
    "When using WidenVanilla, do not exclude recently played dungeons from the tram roll.": "При WidenVanilla не исключать недавно сыгранные подземелья.",
    "Randomize Layout Flow": "Случайный Layout Flow",
    "Pick DunGen layout flows uniformly from each dungeon's candidates instead of using weighted vanilla rolls.": "Случайный выбор шаблонов генерации подземелья.",
    "Randomize Map Variant": "Случайный вариант карты",
    "Pick map variants uniformly from each dungeon's MapIDs instead of vanilla selection.": "Случайный вариант макета подземелья.",
    "Randomize Dungeon Seed": "Случайный сид генерации",
    "Replace the procedural dungeon seed with a new random value when a dungeon is chosen.": "Заменяет сид генерации на случайный каждый раз.",

    "Extended Save Slots": "Расширенные слоты сохранений",
    "Enable Extended Save Slots": "Включить расширенные слоты",
    "Expands the save selection screen to support arbitrarily many save slots instead of the vanilla limit of 3.": "Расширяет экран выбора сохранений, позволяя создавать сколько угодно слотов.",

    "Web Dashboard": "Веб-панель управления",
    "Enable Web Dashboard": "Включить веб-панель",
    "Provides a web interface to configure mod settings from a browser.": "Предоставляет веб-интерфейс для настройки мода через браузер.",
    "Web Dashboard Listen Address": "IP адрес веб-панели",
    "Web Dashboard Listen Port": "Порт веб-панели",
    "The port to listen on for the web dashboard.": "Порт для подключения к веб-панели."
}

def escape_str(s):
    return s.replace('"', '\\"')

def process_file(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    changed = False
    for eng, rus in translations.items():
        q_eng = '"' + escape_str(eng) + '"'
        q_rus = '"' + escape_str(rus) + '"'
        if q_eng in content:
            content = content.replace(q_eng, q_rus)
            changed = True

    if changed:
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"Translated: {filepath}")

root_dir = "src/MimesisPlayerEnhancement"
for root, dirs, files in os.walk(root_dir):
    for file in files:
        if file.endswith(".cs"):
            process_file(os.path.join(root, file))
