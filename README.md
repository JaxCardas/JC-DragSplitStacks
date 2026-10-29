# JC-DragSplitStacks 3.4.0.2

Minecraft-style stack splitting for 7 Days to Die **V 3.4.0**.

## How to use
1. Pick up a stack as usual (left click).
2. **Hold the left mouse button** and drag over several slots → the stack is split **evenly** between them; the remainder stays in your hand. Selected slots get an orange frame while dragging.
3. **Hold the right mouse button** and drag → **one item** goes into each slot.

- Works in the backpack, toolbelt, storage/loot containers and workstations:
  - **fuel slots** (forge, chemistry station, campfire…) – only items that burn,
  - **forge smelting slots** – only materials the forge accepts (e.g. split iron scrap evenly to smelt in parallel); mods that add more smelting slots are supported (the slot count is read from the workstation itself),
  - other workstation inputs – only accepted materials.
- Slots that already hold the same item are topped up (up to the stack limit); slots with a different item, slots locked by the game (e.g. fuel while burning, crafting queue), output slots, item-combining slots, equipment and the creative menu are skipped. Slots you locked yourself for sorting behave like in vanilla (items can still be placed there).
- Releasing over a single slot behaves exactly like vanilla. Holding Shift when releasing cancels the split (vanilla quick-move). Gamepad controls are unchanged.

**Install:** unzip into the game's `Mods` folder → `Mods/JC-DragSplitStacks-3.4.0.2/`. **Delete older `JC-DragSplitStacks-*` folders first** – the game loads only one folder per mod (alphabetically first), which may be the old one.

## Installation / where to install
Everything happens in the player's own inventory screen, so this is a **per-player** mod.

| How you play | Who must install | EAC |
|---|---|---|
| Single player | you | **OFF** |
| You host (LAN / online co-op) | you (for yourself) | you: **OFF** |
| A friend hosts (LAN / online co-op) | **you** (only you get the feature) – the host doesn't need it | you: **OFF**; the host must disable EAC for the session (server setting *EAC* / `ServerEACPeerToPeer=false`), otherwise you are kicked |
| Dedicated server | each player who wants it – the server doesn't need it | those players: **OFF**; the server must have EAC disabled (`EACEnabled=false`) |

- Players without the mod are not affected and can play together with you.
- Storage and workstations: while you have them open your game owns their contents, the result is synced to the server the vanilla way – the host/server needs nothing.
- Console / crossplay players can't use it (no mods on consoles).
- Custom code (DLL): the game skips it when EasyAntiCheat is on. Start 7 Days to Die **without EAC** (launcher option).
- Safe to remove at any time – it changes no items, blocks or saves.

---

# JC-DragSplitStacks 3.4.0.2 (PL)

Dzielenie stosów jak w Minecraft: podnieś stos, **przytrzymaj LPM** i przeciągnij po kilku slotach → stos dzieli się **po równo** (reszta zostaje w ręce). **PPM** przeciągnięty → **po 1 sztuce** do każdego slotu. Działa w plecaku, pasku, skrzyniach i stacjach roboczych: sloty **paliwa** (kuźnia, stacja chemiczna, ognisko – tylko to, co się pali) i sloty **przetapiania** kuźni (tylko materiały, które kuźnia przyjmuje; także mody z większą liczbą slotów).

**Instalacja:** wypakuj do folderu `Mods` gry → `Mods/JC-DragSplitStacks-3.4.0.2/`. **Najpierw usuń starsze foldery `JC-DragSplitStacks-*`** – gra wczytuje tylko jeden folder moda (pierwszy alfabetycznie).

## Gdzie instalować
Mod działa w ekranie ekwipunku gracza, więc instaluje go **każdy gracz, który chce z niego korzystać**.

| Jak grasz | Kto musi mieć mod | EAC |
|---|---|---|
| Gra solo | ty | **wyłączony** |
| Ty hostujesz | ty (dla siebie) | u ciebie **wyłączony** |
| Hostuje znajomy (LAN / online) | **ty** – host nie potrzebuje | u ciebie **wyłączony**; host musi wyłączyć EAC w ustawieniach serwera (*EAC* / `ServerEACPeerToPeer=false`), inaczej zostaniesz wyrzucony |
| Serwer dedykowany | gracze, którzy chcą – serwer nie potrzebuje | u nich **wyłączony**; serwer z wyłączonym EAC |

Gracze bez moda nie są zmienieni. Mod nie zmienia przedmiotów, bloków ani zapisów – można go w każdej chwili usunąć.


---

## Building from source
DLL: .NET SDK 8, `dotnet build JC-DragSplitStacks` (set `SDTD_PATH` to the game folder). Release zip: `tools/release.ps1 JC-DragSplitStacks`.

Downloads: [Nexus Mods](https://next.nexusmods.com/profile/JaxCardas/mods).

## Permissions
All rights reserved. You may use the mod in your own game and on your own servers. Do not re-upload it elsewhere or publish modified versions without permission - ask on Nexus Mods.
