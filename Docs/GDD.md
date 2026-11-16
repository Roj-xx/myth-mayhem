# Myth Mayhem — Game Design Document (Rework)

| | |
|---|---|
| Version | **2.0, final** (concept, rules, card set, balance passes 1–3, accounts) |
| Date | 2026-11-16 |
| Engine | Unity 6 (6000.3), URP |
| Platforms | Mobile (Android/iOS) and PC (Windows) — equal priority |
| Genre | Turn-based 1v1 collectible card battler |
| Match length | 6–10 minutes |
| Status | **Final.** Rules and content are locked; numbers marked **[tune]** are starting values to confirm through simulation and playtests |

### Document set
| Document | For | Contents |
|---|---|---|
| **GDD.md** (this file) | Developers | Full rules, systems, tech and scope |
| [Cards.md](Cards.md) | Developers, co-devs | The Set v1 card list, starter decks, gacha and crafting numbers, lore sources, balance changelog |
| [Roadmap.md](Roadmap.md) | Judges, partners, team | White paper: vision, milestones, competition scope, risks |
| [UserManual.md](UserManual.md) | Players | A simple walkthrough of how to play |

If a number differs between documents, **Cards.md wins for card numbers**
and **GDD.md wins for rules**.

---

## 1. Vision

### 1.1 Pitch
Two Inheritors with the forgotten power to summon the beings of Philippine
mythology clash on the Boundary between the human and supernatural worlds.
Summon Aswang, Diwata and Bakunawa, invoke ancient forces, raise sacred
structures — and when the battle demands it, give something up. **Alay**, the
act of offering, turns your own cards and creatures into power, at a price.

### 1.2 Design pillars
1. **Lore first.** Every mechanic, ability and name comes from Philippine
   folklore or the Myth Mayhem canon. If a rule can't be explained through the
   lore, it doesn't ship.
2. **Simple to read, deep to master.** Three lanes, one resource, automatic
   combat. Depth comes from *where* and *when*, and *what you're willing to
   sacrifice* — not from rule volume.
3. **Every sacrifice is a choice.** Alay is optional but always tempting.
   A good turn often means giving up a card you'd like to keep.
4. **Fair duels.** Collection grows through play and gacha, but rarity adds
   complexity and identity, not raw power. Ranked must be winnable with
   craftable decks.

### 1.3 Unique selling points
- Alay: an optional sacrifice layer on top of a familiar resource, with
  cards that have a second life when offered.
- Filipino mythology as a full setting (not reskinned fantasy), with Tagalog
  naming throughout.
- A canon built around open mysteries, designed for long-running story expansions.

### 1.4 Target audience
- Mobile and PC card-game players (13+; see §7.5 for monetization age gates).
- Filipino players and the diaspora; mythology and folklore fans globally.

---

## 2. World & Narrative

### 2.1 Canon summary
- **The Forgotten World.** The human world and a supernatural realm once
  existed side by side. Conflict and fear drove them apart; humanity forgot,
  and the supernatural faded into legend.
- **The Awakening.** A forgotten source of power stirs, awakening mythical
  beings and reigniting ancient rivalries. Its cause is unknown.
- **The Two Inheritors.** Only two Inheritors can exist at a time. Neither knew
  their heritage; both can summon mythical beings and channel supernatural
  forces. They are not destined enemies — their choices oppose them.
  - **Path of Separation:** seal the worlds apart forever to protect humanity.
  - **Path of Control:** rule both worlds under one will to end disorder.
  - Neither is unquestionably right.
- **The Relic.** An unidentified ancient relic believed to alter the boundary
  between worlds. Both Inheritors seek it. Its cost is unknown.
- **Factions.** Mythical beings belong to independent factions with their own
  motives, leaders and rivalries — not a single army, not good vs. evil.
- **The Battles.** Matches are manifestations of the larger conflict over the
  future of both worlds, fought on a bounded battlefield.

**Central question:** Is peace worth the loss of connection, and can order
justify the loss of freedom?

### 2.2 Protected mysteries
These must **not** be answered in any shipped content, card text, or tutorial.
Hints are allowed; resolutions are reserved for future expansions.
1. The relic's true identity and origin.
2. The deeper cause of the Awakening.
3. Why exactly two Inheritors can exist.
4. What really happened at the original separation.
5. The true motives and histories of the factions.

### 2.3 How lore shows up in gameplay
| Lore element | In gameplay |
|---|---|
| Inheritors | The player's hero; 100 HP; a patron God (Passive + Active) |
| Paths (Separation / Control) | **Story only** — campaign chapters, cosmetics, voice lines. No rule differences. |
| Summoning beings | Creature cards |
| Supernatural forces | Ritual cards |
| Manifested structures | Building cards |
| Sacred and cursed places | Domain cards (shared battlefield effect) |
| The old gods | Patron Gods (one per deck, never on the board) |
| The Boundary | The battlefield itself (3 lanes) |
| Offerings to the spirits | Alay |
| Factions | Card art, flavor text, collection grouping, story. **No gameplay rules.** |

---

## 3. Core Gameplay

### 3.1 Match overview
1. Both players bring a 20-card deck and choose one patron God.
2. **Kara y Krus**, a coin toss, decides who goes first (§3.1a).
3. Players take turns. On your turn you gain summon points, draw, play cards,
   optionally perform Alay, and your creatures strike at end of turn.
4. Reduce the enemy Inheritor from 100 HP to 0 to win.

### 3.1a Kara y Krus (who goes first)
*Kara y Krus* is the traditional Filipino coin toss: *kara* means "face" and
*krus* means "cross." In Myth Mayhem the coin is an **agimat**, with a
carved face on the Kara side and a cross-like sigil on the Krus side.

1. **Both players call at the same time.** Each chooses Kara or Krus within
   **5 s [tune]**. Calls stay hidden until both are locked in. There's no
   race, so connection speed and device type (tap vs. click) don't matter.
2. **The server flips the agimat.** The result is generated and broadcast by
   the server, never by a client, and is recorded in the match log for ranked
   disputes.
3. **Who goes first:**

| Calls | Result |
|---|---|
| Different calls | The player whose call matches the coin goes first |
| Same call | The coin still flips for show, and the server picks one of the two players at random |
| A player doesn't call | Their call is assigned at random |

Every case is exactly 50/50. The call is for excitement only.
4. **Presentation:** about 2 s of agimat spin and the result reveal, with
   "Ikaw ang mauuna!" ("You go first!") or the second-player +1 SP notice.
5. **Vs AI and Story:** the same toss happens, and the AI calls at random.
   Scripted story battles can fix who goes first.

### 3.2 Win, lose, draw
- **Win:** enemy Inheritor reaches 0 HP.
- **Draw:** both reach 0 in the same resolution.
- **Concede:** counts as a loss (existing `ConcedeServerRpc`).
- **Disconnect:** 60 s reconnect window, then loss **[tune]**.
- **Turn limit:** see Boundary Collapse (§3.9).

### 3.3 Board
```
              OPPONENT INHERITOR (100)
   ┌──────────┬──────────┬──────────┐
   │ Building │ Building │ Building │   opponent row
   │ Creature │ Creature │ Creature │
   ├──────────┼──────────┼──────────┤ ← the Boundary
   │ Creature │ Creature │ Creature │
   │ Building │ Building │ Building │   player row
   └──────────┴──────────┴──────────┘
     Lane 1      Lane 2      Lane 3
               PLAYER INHERITOR (100)
```
- 3 lanes per side (keeps `BoardManager.LaneCount = 3`).
- Each lane slot holds **1 creature** and **1 Building** per side; the whole battlefield shares **1 Domain slot**.
- A creature can only be played into an empty creature slot. To replace one,
  offer it via Alay first (this makes Alay a board-management tool too).
- Lanes are **adjacent** left–right (1↔2, 2↔3); some abilities use adjacency.

### 3.4 Summon points
Summon points (SP) remain the primary resource.

| Rule | Value |
|---|---|
| Starting SP — first player | 3 **[tune]** |
| Starting SP — second player | 4 **[tune]** (first-turn compensation) |
| Income per turn (from your 2nd turn) | +2 **[tune]** |
| Unspent SP | Carries over |
| SP cap | 10 **[tune]** — excess is lost |

Change from current build: starting SP 5 → 3/4 and a cap of 10 to stop
turn-1 high-cost plays and runaway banking.

### 3.5 Alay (Offering)
The signature mechanic. During your main phase you may offer:

| Offering | SP gained | Notes |
|---|---|---|
| A card from your hand | **+1 SP** | Card goes to your discard pile |
| A creature on your board | **+⌈cost ÷ 2⌉ SP** (min 1) | Creature is removed (does *not* count as dying) |
| A token on your board (Lower Body, Malakas, Maganda) | **+0 SP** | It frees the lane and still counts as "offering a creature" (Sidapa, Sarimanok, the God Charge) |

Rules:
- **Max 2 Alay per turn [tune].**
- Alay SP is added immediately and **may exceed the SP cap this turn only**
  (excess above cap is lost at end of turn) — so Alay can enable a big
  play but not long-term banking.
- **Alay SP cap: at most +3 SP from Alay per turn [tune]**, bonuses included.
  This stops early ramp into 6-cost cards.
- **Bonus SP doesn't stack:** each Alay applies at most **one** bonus
  (Duwende, Kapre, Dambana or Agueo). If several apply, the highest one counts.
- If the offered card has an **Alay effect**, it triggers (see §3.6).
- Your **first Alay each turn** gives **+1 God Charge** (§3.8); a second Alay
  that turn gives no Charge.
- A card you can't play right now (such as a Domain that loses the affinity
  matchup, §3.8a) can still be offered from hand.
- Buildings cannot be offered. Rituals and Domains can be offered from hand.
- Legendary creatures **cannot be offered from the board** (they may still be
  offered from hand).
- Alay is never mandatory; no card cost requires it.

Design intent: Alay trades card advantage for tempo. Players ask each turn:
*Do I offer a weak card to land a big summon now? Do I sacrifice a damaged
creature before it dies, to free its lane and power my next play?*

### 3.6 Alay effects
Only some cards have an Alay effect (target: ~35–40% of the card pool).
It is printed on the card as a separate line:

> **Alay:** Restore 9 HP to your Inheritor.

- Triggers only when the card is offered — not when it dies, is discarded,
  or is played.
- Alay effects are usually modest; they reward offering the *right* card.
- Cards with strong play effects usually have weak or no Alay effects, and
  vice versa — this creates "play it or offer it?" decisions.

### 3.7 Card types
| Type | Description | On board? |
|---|---|---|
| **Creature** | Summoned mythical being with ATK/HP, keywords, optional ability and Alay effect | Creature slot |
| **Ritual** | Supernatural effect: instant or timed (1–3 turns) | No (resolves) |
| **Domain** | Battlefield-wide effect lasting N turns; owned by whoever played it | Shared Domain slot |
| **Building** | Manifested building; has HP, no ATK; passive effect; **lasts a limited number of turns** (§3.7a) | Building slot |
| **Patron God** | Not a deck card; chosen pre-match; Passive + charged Active | Hero UI |

"For N turns" (Rituals, Domains and Buildings) means N of the owner's turns,
counting the turn it is played.

### 3.7a Buildings are temporary
A manifestation can't hold forever on the Boundary, so every Building has a
**Duration** as well as HP.
- A Building is removed at the end of its owner's last turn, or earlier if
  its HP reaches 0 (from strikes into an empty lane, Bakunawa's Eclipse,
  Lalahon's Pagsabog or Cursed Village).
- Your Building card shows a countdown pip for each turn it has left.
- You can play a new Building over your own in the same lane, and the old one
  is removed. An enemy Building can't be replaced.
- Why: Buildings no longer depend on a lane being empty to be removed, and a
  strong aura like Torogan has a built-in end.

### 3.8 Patron Gods
Patron Gods replace the Inheritor Power. The full list is in
[Cards.md §7](Cards.md).
- Choose **1** patron God per deck. They are the rarest collectibles (Divine ✦).
- **Passive:** always on.
- **Charges:** +1 at the start of each of your turns, +1 from your first Alay
  each turn. Max 3.
- **Active:** costs **3 Charges** and 0 SP. Once per turn.
- 3 starter Gods (Adlaw, Aman Sinaya, Bulan); the rest come from the Tawag
  banner or are crafted with Abo.
- Path cosmetics (Separation/Control) change only VFX and voice lines.

### 3.8a Domain clash (affinity triangle)
There is one shared Domain slot. Each Domain has an affinity:
**Nature** beats **Human**, Human beats **Spirit**, and Spirit beats Nature.
Cards show the English names. The Filipino words *Kalikasan* (Nature), *Bayan*
(Human) and *Kaluluwa* (Spirit) appear only as flavor, such as the affinity
icon tooltip and card lore text.
- You can always replace your own active Domain.
- Against an active enemy Domain: if yours **beats** it, yours replaces it.
  With the **same** affinity, it replaces it for +1 SP. If yours **loses**,
  you can't play it until theirs ends, but you can still offer it with Alay.
- A replaced or expired Domain's effects end immediately.
- Turns alternate, so two Domains are never played at the same moment.

### 3.9 Turn structure
```
START PHASE
  1. Gain income SP (not on your first turn), apply cap
  2. +1 God Charge
  3. Draw 1 card (hand max 10; overdraw is discarded)
  4. Start-of-turn triggers; your timed effects tick down
MAIN PHASE (any order, any number of times)
  • Play cards (pay SP)
  • Alay (max 2)
  • Use patron God Active (once)
  • End Turn
STRIKE PHASE (automatic, lanes 1 → 3, ~0.45 s each)
  For each lane, your creature strikes if it was not summoned this turn
  (or has Swift):
    – enemy Bantayog in lane  → Bantayog takes ATK (it intercepts all strikes,
                                Flight included; nobody retaliates)
    – else enemy creature     → both deal ATK to each other (retaliation)
    – else enemy Building     → Building takes ATK (Flight skips this step)
    – else                    → enemy Inheritor takes ATK
END PHASE
  End-of-turn triggers; excess SP above cap is lost; pass turn
```

**Change from current build:** combat moves from "end of the opponent's turn,
both sides clash" to "end of *your* turn, *your* creatures strike." Each
player's turn now ends with their own payoff, and retaliation keeps defending
creatures relevant.

**Boundary Collapse (turn limit):** from round 10 **[tune]**, at the start
of each round (before the first player's turn), **both Inheritors take
damage at the same time**, equal to 3 × (round − 9). If both reach 0, the
match is a draw. Hitting both at once means the first player is no longer
always hit first. With the
turn timer, this keeps matches within ~10 minutes (30 s × 2 players × 10
rounds = 10 min worst case).

**First-player fairness:** the second player starts with +1 SP. If simulation
shows the first player winning more than 52% of matches, the next lever is
giving the second player a free 0-cost *Bulong* card in their opening hand **[tune]**.

**Turn-1 damage:** the first player can hit the enemy Inheritor on turn 1
only with a Swift creature (currently just Tiyanak): 6 damage, 9 with Adlaw,
and 15 with Adlaw + Pangayaw. That's accepted. The simulation watches the
Tiyanak + Pangayaw opening, and if it wins too often, the fix is "Pangayaw
can't target a creature summoned this turn" **[tune]**.

**No responses during a turn:** only the active player acts. There are no
counter-spells or interrupts, which keeps turns fast and mobile-friendly.

**Turn timer (PvP):** 30 s per turn + 20 s one-time reserve per match **[tune]**.
On timeout the turn ends (strike still resolves). Three consecutive timeouts = loss.

### 3.9a The opponent's turn (what the waiting player sees)
While waiting, you watch the opponent's turn live. The server broadcasts the
board after every action.

| Visible to you | Hidden from you |
|---|---|
| Each card they play, shown face-up center-screen for ~1 s before it resolves | The cards in their hand |
| Each Alay: the offered card is revealed, along with its SP gain and Alay effect | Cards they drag, hover over or consider but don't play |
| God Active use, Domain changes, and the strike animations | Their deck order |
| Their hand size, deck count, SP, Charges and discard pile | — |
| Their turn timer | — |

**What you can do while waiting:**
- Inspect any card (your hand, the board, either discard pile).
- Plan your next move.
- Send an emote (rate-limited, and players can mute them).
- You can't play, offer, or use your God.

Not showing their hesitation is deliberate: it stops players from reading
each other's indecision and keeps the waiting player's screen calm. The
existing `BoardSnapshot` replication covers this. Each snapshot just needs a
"last action" field so clients can play the reveal animation.

### 3.10 Deck rules
| Rule | Value |
|---|---|
| Deck size | 20 cards **[tune]** |
| Copies per card | Max 2 (Legendary: max 1) |
| Opening hand | 5 cards, one free mulligan (replace any number once) |
| Hand max | 10 |
| Draw per turn | 1 |
| Empty deck | **Hollow draw:** each draw from an empty deck deals 3, then 6, then 9… damage to your Inheritor |
| Discard pile | Visible; some abilities interact with it |

### 3.11 Keywords
Kept small. Keywords appear on any rarity; unique abilities are Rare+.

| Keyword | Rule |
|---|---|
| **Swift** | Can strike the turn it is summoned |
| **Shield N** | Absorbs the next N damage, then is removed (doesn't stack; the higher value wins) |
| **Drain** | Damage it deals heals your Inheritor by the same amount, up to max HP (no overheal) |
| **Pierce** | Excess damage over a **creature's** HP hits the Inheritor behind it. It doesn't carry past Buildings or Bantayog. |
| **Flight** | If its lane has no enemy creature, it skips the enemy Building and hits the Inheritor. It can't skip Bantayog, and it still fights an enemy creature normally. Cap: at most 4 Flight cards in the set. |
| **Elusive** | Cannot be targeted by enemy Rituals or God Actives |
| **Guard** | Adjacent friendly creatures take 3 less damage |

**Damage-reduction cap:** "take N less damage" effects (Guard, Intramuros)
don't stack beyond **−3 per hit**. Shield is separate: it absorbs damage
after the reduction.

### 3.12 Ability triggers (for the ability system)
`OnSummon`, `OnStrike`, `OnDamaged`, `OnDeath`, `OnAlay`, `OnTurnStart`,
`OnTurnEnd`, `Passive` (continuous while on board).
Targeting: `Self`, `LaneEnemy`, `AdjacentFriendly`, `AnyFriendly`,
`AnyEnemy`, `AllEnemies`, `Inheritor`, `EnemyInheritor`.

---

## 4. Card Content

**The full card list is in [Cards.md](Cards.md)**: 44 deck cards and
11 patron Gods, with starter decks and how each card is obtained. This
section holds only the rules for that content.

### 4.1 Rarity
**Fixed stats per card:** a card has exactly one rarity and one stat line.
Rarity sets pull odds, not power.

| Rarity | Symbol | Design role | Applies to |
|---|---|---|---|
| Common | ★3 | Vanilla stats + ≤1 keyword (an Alay effect is allowed) | Deck cards |
| Rare | ★4 | Keyword(s) and/or simple ability | Deck cards |
| Legendary | ★5 | Unique folklore ability, build-around | Deck cards |
| **Divine** | ✦ | Patron Gods only, the rarest tier | Gods |

Rarity scales **complexity and identity, not stat efficiency**. A Common at
cost N should be stat-competitive with a Legendary at cost N.

### 4.2 Stat scale
**Inheritor 100 HP.** Every combat number (ATK, HP, damage, healing, Shield,
ATK changes) is a multiple of 3; SP, costs, Charges and draw stay small.
Budget guide **[tune]**: ATK + HP ≈ 6 × cost + 6; a keyword ≈ 3 points; abilities ≈ 3–6 points.

### 4.3 Launch set
- **Set v1 (Cards.md):** 24 Creatures, 8 Rituals, 9 Domains, 3 Buildings
  (44 deck cards) and 11 patron Gods.
- Expansion candidates: Sigbin, Amomongo, Busaw, Mambabarang, Engkanto,
  Mangkukulam, Bal-Bal.
- Every card gets folklore research notes (origin region, myth summary) in the
  card data so abilities stay faithful (Pillar 1).
- Factions are a collection/art grouping only.

### 4.5 Card data model (ScriptableObject, extends current definitions)
```
CardDefinition (base)
  id (stable string, e.g. "MM-CORE-001")   ← required for net/gacha/save
  displayName, flavorText, folkloreNotes, artwork, prefab
  type: Creature | Ritual | Domain | Building
  rarity: Common | Rare | Legendary
  faction (cosmetic), set
  cost
  abilities: List<AbilityDefinition>        ← trigger + target + effect + magnitude
  alayEffect: AbilityDefinition (nullable)
  canOfferFromBoard: bool
CreatureDefinition : CardDefinition  → attack, health, keywords (flags)
BuildingDefinition : CardDefinition → health, duration, effect
RitualDefinition : CardDefinition → duration
DomainDefinition : CardDefinition → affinity (Nature|Human|Spirit), duration, effects
PatronGodDefinition → id, rarity = Divine, passive, active (chargeCost = 3),
                      craftCost, vfxBySeparation/Control
```

---

## 5. Game Modes

### 5.1 Vs AI
- Skirmish against the AI with preset or player decks.
- Difficulty: **Baguhan** (Easy), **Mandirigma** (Normal), **Bagani** (Hard).
- AI must evaluate Alay (offer when it enables a stronger play or saves a
  dying creature's value). Hard AI uses a 1-ply lookahead on the strike phase.
- Rewards: reduced vs. PvP; daily caps to prevent farming.

### 5.2 PvP Casual
- **Quick Match:** matchmaking on hidden MMR, no rank shown.
- **Lobby:** create/join a private match with a 6-character code (current
  Relay flow), invite friends, rematch button. Accessible from the main menu
  in one tap.
- No rank change; normal rewards.
- **New-player protection:** for an account's first 10 casual matches, Quick
  Match pairs it only with other new accounts, or with an AI opponent labeled
  as "Practice" if no match is found within 20 s **[tune]**. Lobby matches
  with friends are unaffected.

### 5.3 PvP Ranked
- Season length: 6 weeks **[tune]**.
- Tiers: **Baguhan → Mandirigma → Bagani → Datu → Maalamat**, each with
  3 divisions except Maalamat (leaderboard by rating).
- Rating: Glicko-2 hidden MMR; visible tier progresses with stars.
- No demotion out of a tier floor in the first two tiers.
- End-of-season rewards: card back, frame, Mutya.
- Requires: account auth, server-validated match results (see §8.3).
- **Unlocks at account level 5 [tune]**, so players reach ranked with at
  least one tuned deck and know the rules.
- **Testing without players:** Unity Multiplayer Play Mode (local multi-client),
  queue bots flagged `isBot`, seeded fake MMR pools, and a dev panel to
  force pairings and simulate rating updates.

### 5.4 Story Campaign
- Structure: alternating chapters following each Inheritor's path.
  Chapter 1 per Path at launch (5–6 battles each).
- Battles are scripted AI matches with **special rules** (e.g., pre-placed
  Buildings, a creature that starts on the board, a lane sealed for 3 turns).
- Story told through 2D illustrated panels + dialogue (reuse the tutorial
  slideshow system).
- Rewards: starter cards, Mutya, cosmetics; completion unlocks a Path-themed
  Inheritor skin.
- **Writing rule:** respect §2.2 — hints only, no resolutions.

### 5.5 Tutorial
- Rework the existing slideshow into an **interactive first match** (scripted
  AI).
  - **Match 1:** play a creature → strike → Ritual → Building.
  - **Match 2:** Alay → patron God → Domain (with the affinity triangle).
  - Splitting it this way keeps the first match from overloading new players.

---

## 6. Progression

> **Competition build:** this whole section (accounts, XP, levels) is
> **post-competition** scope (M4). The competition build uses a local profile
> only (§11.1).

### 6.1 Account creation ("play first, sign in later")
1. **First launch:** a guest account is created silently. The player enters
   a display name and birth date, then goes straight into Kara y Krus and the
   tutorial match.
2. **Prompt to link:** after the tutorial or at level 3. Linking is required
   before the first purchase and before ranked.
3. **Sign-in options:**

| Platform | Sign-in |
|---|---|
| Android | Google Play Games |
| iOS | Sign in with Apple, Game Center |
| PC | Steam |
| Any platform | Myth Mayhem account by email (Unity Player Accounts) |

- **Cross-progression:** link two sign-ins and it's the same account on phone
  and PC: same cards, Gods, level and currencies (check each store's policy on
  sharing purchased currency).
- **No passwords stored by us:** the platform handles login. A lost device
  just means signing in again.
- **Guest risk:** an unlinked guest account is lost on uninstall, so the game
  must show a clear warning.
- **Display name:** not unique; a tag is added (`Dalisay#4821`). Names pass a
  profanity filter. The first rename is free, later ones cost Ginto.

### 6.2 Account level and XP
| Source | XP |
|---|---|
| PvP win / draw / loss | 100 / 75 / 50 |
| Vs AI match | Half of PvP; XP from AI is capped at 10 matches a day |
| First win of the day | +200 |
| Daily quest | 100–300 each (3 per day) |
| Story chapter cleared | 500 (first time only) |

- **Anti-farming:** a match conceded before round 3 gives no XP to either
  player.
- **Curve:** level *n* needs 400 + 100 × *n* XP **[tune]**. Launch cap:
  **level 50**. After that, each further level-up gives Ginto and Abo instead
  of a new level.

| Level | Unlocks |
|---|---|
| 1 | Both starter decks and the 3 starter Gods |
| 2 | Deck builder |
| 3 | Ginto shop and daily quests, plus the prompt to link a sign-in |
| 4 | Crafting with Abo |
| 5 | **Ranked** and a free 10-pull |
| 10, 20, 30… | Extra deck slots, from 3 up to 9 |
| Every level | Ginto; some levels also give Abo, Mutya or cosmetics (frames, card backs, titles) |

### 6.3 Profile
- **Look:** an Inheritor portrait with Separation or Control themed skins, a
  title, a card back and a frame.
- **Stats:** level, ranked tier, win rate, favorite God, the last 20 matches,
  and collection completion %.
- **Friends:** add by name tag, and invite into lobby codes.

### 6.4 Safety and compliance
- **Age gate:** the birth date decides spending limits for minors and gacha
  eligibility by region (check before launch).
- **Parental controls:** a monthly spending cap, and an option to hide chat
  and emotes.
- **Account deletion:** an in-app request (Apple and Google require one),
  with a 30-day grace period before server data is wiped.
- **Privacy:** follows the Philippine Data Privacy Act (2012) and GDPR, with
  a data download option.
- **Reports and bans:** post-match reporting, backed by server match logs
  (including the Kara y Krus result).

### 6.5 Tech
- Unity Authentication (already in the project) handles guest accounts, Apple,
  Google, Steam and account linking.
- Level, XP, collection and currencies live on the server (Cloud Save or a
  custom backend). Cloud Code grants XP and rewards, and the client is never
  trusted.

### 6.6 Other progression
- **Daily quests:** 3 per day (e.g., "Perform 5 Alay," "Win with Diwata in deck").
- **Starter collection:** two themed 20-card decks (Night Hunters and Forest
  Guardians) and 3 patron Gods (Adlaw, Aman Sinaya, Bulan), enough to play
  ranked. See [Cards.md §8](Cards.md).
- **Collection screen:** filter by type, rarity, cost, faction; card detail
  shows folklore notes.
- **Deck builder:** 20-card validation, mana-curve view, patron God pick,
  up to 9 deck slots.

---

## 7. Economy & Monetization (F2P + IAP, gacha)

### 7.1 Currencies
| Currency | Name | Source | Use |
|---|---|---|---|
| Soft | **Ginto** (gold) | Matches, quests, levels | Standard packs, crafting top-up |
| Premium | **Mutya** (charm-pearl) | IAP, seasons, story, events | Banner pulls, cosmetics, pass |
| Crafting | **Abo** (ash) | Duplicate cards beyond the deck max | Craft any card or God |

### 7.2 Gacha — "Tawag" (The Calling)
The full rates and pity tables are in [Cards.md §9](Cards.md).
- **A banner, not packs:** each pull gives one item (a card or a God).
- **Featured banner:** rotates about every 3 weeks and headlines one God at
  boosted odds. **Standard banner:** permanent, with all cards and released Gods.
- **Base rates [tune]:** God ✦ 0.6%, ★5 3.4%, ★4 16%, ★3 80%.
- **Pity [tune]:**
  - Every 10 pulls guarantees ★4 or better.
  - Every 30 pulls guarantees ★5 or better.
  - God soft pity starts at pull 70, and pull 90 is a guaranteed God.
  - If the God you get isn't the featured one, the next God is guaranteed featured.
- Pity counters carry over between banners of the same type.
- **Cosmetic variants:** a 5% chance per card pull for alternate or animated
  art. Stats are identical.
- **Duplicate protection:** copies beyond a card's deck max convert to Abo.

### 7.3 Other ways to get cards
- Craft with Abo: every card and every God (Gods at a high cost).
- Ginto shop: a daily rotation of Commons and Rares, plus a weekly Legendary slot.
- Story and quest rewards: specific cards.
- Direct buy with Mutya: any card. **Gods can't be bought directly**, which
  keeps them the rarest tier.

### 7.3a Ranked fairness
- Fixed stats: a pulled card is never stronger than a crafted one.
- Every card and God can be crafted, so nothing is permanently paywalled.
- Rarity does not mean higher stat efficiency (§4.1).

### 7.4 Other monetization
- **Season pass "Landas"** (Path): free + premium tracks, cosmetics and currency.
- **Cosmetics:** Inheritor skins (Separation/Control themes), card frames,
  animated card art, board skins, emotes.
- **Starter bundle:** one-time, low price.

### 7.5 Compliance requirements (not legal advice — verify before launch)
- Publish pull odds in-game (Apple App Store and Google Play require it).
- Keep and show pity counters and pull history.
- Regional restrictions: paid loot boxes are restricted in Belgium (and may need
  changes in other regions); check region-by-region before release.
- Age rating, minor spending limits, and parental-control support per store
  and region.
- All purchases and pulls resolved server-side.

---

## 8. Technical Design

### 8.1 Architecture direction
- **Extract a pure C# rules core** (no MonoBehaviours): `GameState`,
  `TurnRules`, `AlayRules`, `CombatResolver`, `AbilitySystem`. It must be
  deterministic and unit-testable, and shared by local play, AI and the
  networked server.
- Unity layer (MonoBehaviours) only renders state and sends player intents.
- Extends the existing intent pattern (`IMatchIntentSink`, `MatchSession`
  ServerRpcs) with `OfferCard`, `OfferCreature`, `UseInheritorPower`, `Mulligan`.
- `BoardSnapshot` gains: Buildings, the Domain slot (owner, card, turns left), keywords/status, Charges, Alay count,
  discard pile, turn timer.

### 8.2 Mapping current code to the rework
| Current | Rework action |
|---|---|
| `TurnManager` | Keep SP logic; new start values, cap, Charges; phases per §3.9 |
| `DeckController` | Add discard pile, mulligan, hollow draw |
| `CombatController` | Strike on active player's turn end; retaliation; keywords |
| `SummonController` / `OpponentController` | Merge into one side-agnostic play pipeline (fixes duplicated summon logic) |
| `CardPlayController` | Becomes the effect/ability resolver |
| `CreatureDefinition` etc. | Migrate to the §4.5 data model with stable IDs |
| `MatchRules` | Expand to full outcome rules (collapse, concede, timeout) |
| `MatchSession` / `RelayLobbyConnector` | Keep for casual lobby; add matchmaking |
| `TutorialSceneController` | Reuse for story panels; new interactive tutorial |
| — (new) | `AlayController`, `InheritorPower`, `AbilitySystem`, `KeywordSet`, Collection/Gacha/Economy services |

### 8.3 Online services
| Need | Proposed service |
|---|---|
| Auth | Unity Authentication (existing) + platform sign-in |
| Casual lobby | Unity Lobby + Relay (Relay existing) |
| Matchmaking | Unity Matchmaker |
| Ranked integrity | Authoritative server (Unity Multiplay Hosting or equivalent). **Player-hosted matches are not acceptable for ranked** — the host could cheat. |
| Collection, currencies, gacha | Server-side (Unity Cloud Save + Cloud Code, or a custom backend). Never resolve pulls on the client. |
| IAP | Unity IAP with server receipt validation |
| Analytics | Unity Analytics (match length, Alay usage, card win rates, funnel) |

### 8.4 Platform & UX
- **Landscape only**, on both mobile and PC. There's no portrait mode: the
  3-lane board needs the width. The UI scales from ~5.5" phones to 27" monitors.
- Input: tap/drag on mobile, click/drag + hover tooltips on PC; full keyboard
  shortcut for End Turn.
- Alay UX: drag a card or creature onto an **Alay altar** zone; show the SP gain
  and Alay effect preview before release. Alay is final once dropped (no undo),
  so the preview must be clear.
- Readability: large cost/ATK/HP numerals; keyword icons with long-press/hover text.

### 8.5 Testing
- EditMode tests on the rules core (extend existing `MatchRulesTests`,
  `DeckControllerTests`): every keyword, every Alay rule, strike order,
  collapse, hollow draw.
- Simulation harness: AI vs AI ×10,000 matches to report average match length,
  first-player win rate (target 48–52%), card win rates.
- Netcode: Multiplayer Play Mode for 2-client sessions; desync check by
  comparing snapshot hashes.

---

## 9. Art, Audio & Feel (rework scope)

Assets will be replaced later; this section sets direction only.
- **Style:** stylized, readable silhouettes rooted in Filipino art traditions
  (baybayin motifs, textile patterns, sacred-grove palettes).
- **Animation per creature:** idle, summon, strike, hit, death; Alay has its own
  dissolve-into-offering VFX — this moment must feel ritual and weighty.
- **Board:** the Boundary is visually split — human-world side vs.
  supernatural side — with lane markers.
- **Audio:** kulintang/gong-based motifs, per-creature summon cries, a distinct
  Alay sound.

---

## 10. Balance Targets

| Metric | Target |
|---|---|
| Average match length | 6–10 min (≈ 8–10 rounds) |
| First-player win rate | 48–52% |
| Alay used per match (per player) | 2–5 average; 0-Alay decks viable but weaker |
| Any single card's play rate in ranked | < 40% |
| Any card's win-rate delta vs. average | within ±5% |
| God Active usage | 3–4 times per match (Charge gain ≤ 2 per turn → about every 2 turns with Alay, every 3 without) |

---

## 11. Roadmap

| Phase | Scope | Exit criteria |
|---|---|---|
| **M1 — Rules core** | Pure C# rules, Alay, new strike, keywords, abilities; tests | All rules unit-tested; AI-vs-AI sim runs |
| **M2 — Playable vertical slice** | Both starter decks, 3 starter Gods, showcase Legendaries, Vs AI, new HUD/Alay UX, Kara y Krus, opponent-turn view, tutorial | 10-minute playable match on PC and Android |
| **M3 — Online casual** | Netcode on rules core, lobby + quick match | Two devices complete a match without desync |
| **M4 — Collection & economy** | Deck builder, collection, currencies, gacha, IAP (sandbox) | Server-validated pulls; odds screen |
| **M5 — Ranked & story** | Authoritative server, ranked seasons, story chapter 1 ×2 Paths | Bots-in-queue ranked test passes |
| **M6 — Content & polish** | Full Set v1 (44 cards + 11 Gods), new assets/animations, audio | Balance targets met in sim + playtests |

Accounts, XP and levels (§6.1–6.5) belong to **M4**.

### 11.1 Competition build (presentation scope)
The competition build only needs to show the game's core identity. Judges
look at gameplay, theme and polish, not account systems.

| In | Out (post-competition) |
|---|---|
| M1 rules core, plus the M2 vertical slice (Vs AI, Alay, the new HUD, the tutorial) | Accounts, sign-in, XP and levels (§6) |
| Kara y Krus coin toss and the opponent-turn view | Gacha, currencies, IAP, crafting (§7) |
| Both starter decks, the 3 starter Gods, and a few showcase Legendaries | Ranked, matchmaking, new-player protection |
| PvP through the **existing lobby code** (Relay) | Story campaign (a short intro cinematic is enough) |
| A **local profile**: display name and settings saved on the device | Server-side saves and the authoritative server |

A local profile means a name and settings saved on the device: no login and no
server. It's enough for the lobby to show player names.

---

## 12. Out of Scope (for this rework)
- Path-based gameplay differences.
- Faction loyalty, defection or faction-synergy rules.
- Win conditions other than Inheritor HP.
- 2v2, draft or arena modes.
- Trading cards between players.

## 13. Open Questions
None of these block the competition build.
1. Exact SP numbers (start 3/4, +2, cap 10), to be confirmed by simulation.
2. Hosting provider and budget for the ranked authoritative server (M5).
3. Launch regions, which affect gacha compliance and the server region (M4).

## 14. Glossary
| Term | Meaning |
|---|---|
| Alay | Offering — sacrificing a hand card or board creature for SP (+ effect) |
| Inheritor | The player's hero |
| SP | Summon points |
| Charge | Patron God Active resource (max 3) |
| Strike | End-of-turn automatic attack by your creatures |
| Boundary | The battlefield; also the line between the two worlds |
| Boundary Collapse | Turn-limit damage from round 10 |
| Kara y Krus | The pre-match agimat coin toss that decides who goes first |
| Hollow draw | Damage from drawing on an empty deck |
| Tawag | The gacha ("The Calling") |
| Ginto / Mutya / Abo | Soft / premium / crafting currencies |
| Landas | Season pass |
