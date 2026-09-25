# Online Mode Flow

## Connection

Hosting or joining creates a persistent session and returns both players to the main menu. Leaving a submenu does not disconnect. Only **Disconnect Session** tears the session down.

The connected main menu exposes two game intents:

- **Co-op Campaign** — two human players share a team against campaign enemies.
- **Duel — 8 SP** — two players fight each other under symmetrical loadout rules. The current playable baseline uses the same 8 SP preset for both players; the interactive draft below is the planned replacement.

The host selects the mode. The guest remains on the same main menu, automatically acknowledges the request, and enters the selected mode after host confirmation.

## Duel draft

The draft is separated from combat rules and synchronized as its own network state.

1. Each player has an 8 SP budget.
2. Each player secretly bans one skill.
3. Bans are revealed simultaneously and apply to both players.
4. Players alternate picks; the host does not receive first pick automatically. A session seed determines order.
5. `God`, `Immortal`, and `Emperor` cost 3 SP. `Dragon` and `Transform` cost 5 SP because of their high ceiling.
   `Super God`, `Super Immortal`, and `Super Emperor` cost 6 SP because each rewrites both numbers.
6. Conflicting replacements cannot coexist in one loadout.
7. Both loadouts are locked and exchanged before combat initialization.

The playable implementation now uses simultaneous hidden bans followed by independent picks. A loadout must spend exactly 8 SP before it can be locked. The host validates both loadouts and broadcasts the canonical result before combat is created.

The protocol carries `DraftBan`, `DraftLock`, and a final canonical `DraftStart`. A future alternating-pick variant can add `DraftPick` without changing combat initialization.

## Campaign Co-op (mode 2)

Both players share the campaign map but each owns an independent route:

- Presence is broadcast continuously as `CampaignPresence(PlayerId, Map|Loadout|Battle, NodeId, Ready, Skills, SessionId)`; the peer's node shows a label on the map and in the loadout screen.
- Entering the same node's loadout screen together requires both players to Ready before the cooperative battle starts (host-authoritative `CampaignCoopStart`). Either player may also start the same node solo.
- If a peer is already fighting a node, the other player can pick that node and Join: the battle owner captures a turn-boundary `BattleSnapshot` and the joiner rebuilds the battle from it.
- Battles are independent unless explicitly shared (`IsCampaignSharedBattle`); actions from another route are dropped.

All tuning constants live in `Network/OnlineModeRules.cs` so UI, encounter generation, and tests can share one source of truth.
