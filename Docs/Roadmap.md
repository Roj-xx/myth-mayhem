# Myth Mayhem — Development Roadmap (White Paper)

*A turn-based card battler built on Philippine mythology*

| | |
|---|---|
| Version | 1.0 |
| Date | 2026-11-16 |
| Engine | Unity 6 (URP) |
| Platforms | Android, iOS, Windows (cross-play) |
| Companion documents | [GDD.md](GDD.md) · [Cards.md](Cards.md) · [UserManual.md](UserManual.md) |

---

## 1. Executive summary

**Myth Mayhem** is a 1v1 turn-based card battler. Two Inheritors summon the
beings of Philippine mythology (Aswang, Tikbalang, Bakunawa and others) and
fight across three lanes on the Boundary between the human and spirit worlds.

Its signature mechanic is **Alay** (offering): players can sacrifice cards
and creatures for power. That gives every turn a "what am I willing to give
up?" decision that standard card battlers don't have.

The project already has a working v0.2.0 build: 3-lane combat, AI and
online PvP, mobile polish. This paper lays out the rework from that build
into a full, lore-faithful, fair free-to-play game, in **six milestones**,
with a **competition build** that shows the core experience first.

---

## 2. Why this game

| Opportunity | How Myth Mayhem answers it |
|---|---|
| Philippine mythology is rich but underused in games | A whole setting built from it: 24 creatures, 11 deities from five pantheons, and real rituals, places and artifacts, each checked against folklore sources |
| Card battlers tend to feel the same | Alay adds a sacrifice layer on top of familiar summoning, and a Domain affinity triangle adds a fight over the battlefield itself |
| Gacha games are seen as pay-to-win | Every card has one fixed stat line, everything can be crafted, and Gods can't be bought directly. Paying speeds up collecting; it doesn't buy power. |
| Mobile sessions are short | Matches take 6–10 minutes, with 30-second turns and a turn limit built in |
| Players want to switch devices | One account on phone and PC, with cross-play |

---

## 3. The game at a glance

- **Goal:** reduce the enemy Inheritor from **100 HP to 0**.
- **Board:** 3 lanes. Each side has a creature slot and a Building slot per
  lane, plus one shared Domain slot.
- **Turn:**
  1. Gain summon points (SP) and draw a card.
  2. Play cards and optionally offer Alay.
  3. Your creatures strike automatically, lane by lane.
- **Card types:**
  - **Creatures:** summoned beings that fight in the lanes.
  - **Rituals:** spells.
  - **Domains:** battlefield-wide effects with a Nature > Human > Spirit triangle.
  - **Buildings:** temporary lane structures.
  - **Patron Gods:** one per deck, with a passive and a charged power.
- **First player:** decided by **Kara y Krus**, a fair agimat coin toss.
- **Modes:** Vs AI, Casual PvP (quick match and lobby codes), Ranked, Story.
- **Collection:** the Tawag gacha (one pull = one card or God; Gods are the
  rarest, guaranteed by pull 90), Abo crafting, a Ginto shop and story rewards.

Full rules: [GDD.md](GDD.md). Player-facing guide: [UserManual.md](UserManual.md).

---

## 4. Where we are today (v0.2.0)

| Already built | Changes in the rework |
|---|---|
| 3 lanes × 2 sides, summon points, deck and hand | Kept; the numbers are retuned |
| Combat at the end of the opponent's turn | Replaced: your creatures strike at the end of your turn, with retaliation |
| 11 cards (creatures, supports, buildings) | Expanded to 44 cards plus 11 patron Gods |
| AI opponent (one random play per turn) | Rebuilt to understand Alay, with 3 difficulty levels |
| Online PvP (Netcode for GameObjects, Unity Relay, lobby codes) | Kept for casual play; matchmaking added |
| Tutorial slideshow, intro cinematic, mobile polish | The tutorial becomes interactive; the cinematic is reused |
| Meshy-generated 3D models | Placeholder art, to be replaced in M6 |

**Engine decision:** we evaluated Godot and chose to **stay on Unity**. Godot
wouldn't fix the visuals on its own, since they come from the assets and
animation, not the engine. Switching would also mean rewriting working
networking, tooling and the mobile pipeline. Effort goes into art and
animation instead.

---

## 5. Milestones

Each milestone has a single goal and a clear exit test. Target dates are left
for the team to set based on team size and the competition calendar.

```
 M1 Rules core ─► M2 Vertical slice ─► M3 Online casual ─► M4 Collection & accounts ─► M5 Ranked & story ─► M6 Content & polish
 └───────── Competition build (M1 + M2 + the lobby part of M3) ─────────┘
```

### M1: Rules core
| | |
|---|---|
| **Goal** | Every rule in the GDD works in code, without any graphics. |
| **Deliverables** | A plain C# rules engine (game state, turns, Alay, strikes, keywords, abilities, Domains, Buildings, Gods), unit tests for every rule, and an AI-vs-AI simulation harness |
| **Exit criteria** | All rules are unit-tested, and 10,000 simulated matches run and report match length, first-player win rate and card win rates |
| **Why first** | Local play, the AI and the online server all share this one rules engine, so it has to be right first |
| **Target date** | ____ |

### M2: Playable vertical slice
| | |
|---|---|
| **Goal** | A complete, good-looking match a stranger can pick up and play. |
| **Deliverables** | New HUD, the Alay altar UX, Kara y Krus, the opponent-turn view, both starter decks, 3 starter Gods, a few showcase Legendaries, Vs AI (3 difficulties), and the interactive tutorial |
| **Exit criteria** | A 10-minute match plays from start to finish on PC and Android, and a new player finishes the tutorial without help |
| **Target date** | ____ |

### M3: Online casual
| | |
|---|---|
| **Goal** | Two players on different devices play a fair match. |
| **Deliverables** | Netcode on top of the rules engine, the lobby code flow (already working), Quick Match matchmaking, and reconnects |
| **Exit criteria** | 100 consecutive two-device matches finish without a desync |
| **Target date** | ____ |

### M4: Collection, economy and accounts
| | |
|---|---|
| **Goal** | Players own cards and accounts that persist across devices. |
| **Deliverables** | Guest-first accounts with platform sign-in and cross-progression, XP and levels, deck builder, collection screen, the three currencies (Ginto, Mutya, Abo), Tawag gacha with pity, crafting, the Ginto shop, IAP (in sandbox), age gate, and account deletion |
| **Exit criteria** | Pulls are resolved by the server, the odds screen is published, and a purchase made on one device shows up on the other |
| **Target date** | ____ |

### M5: Ranked and story
| | |
|---|---|
| **Goal** | Competitive play and the narrative layer. |
| **Deliverables** | An authoritative match server, ranked seasons (Baguhan to Maalamat), new-player protection, and story Chapter 1 for both the Separation and Control paths |
| **Exit criteria** | The ranked queue passes a test using bot players, and both story chapters are playable from start to finish |
| **Target date** | ____ |

### M6: Content and polish
| | |
|---|---|
| **Goal** | Launch quality. |
| **Deliverables** | The full Set v1 (44 cards and 11 Gods), new art and animation in a consistent style, the Alay VFX, kulintang-based audio, and a balance pass |
| **Exit criteria** | Simulation and playtests meet every balance target in §7 |
| **Target date** | ____ |

---

## 6. Competition build

For the game development competition we show the **core identity of the
game**, not its business systems.

| Shown | Deferred (and why) |
|---|---|
| The full new rules: Alay, lane strikes, keywords, Domains, Buildings, Gods | **Accounts and levels:** backend-heavy, and invisible on stage |
| Vs AI and the interactive tutorial | **Gacha, currencies, IAP:** need a server and compliance work |
| Kara y Krus and the opponent-turn view | **Ranked and matchmaking:** need a player base and a server |
| Both starter decks, the 3 starter Gods and showcase Legendaries | **Story campaign:** content-heavy; the intro cinematic covers the setting |
| Two-player PvP through the existing lobby codes | **Server saves:** a local profile (name and settings) is enough |

**Demo script (about 5 minutes):**
1. The intro cinematic sets up the lore.
2. Kara y Krus decides who goes first.
3. Turns 1–3 show summoning and lane strikes.
4. An Alay turn: sacrifice a creature, then play a big Legendary.
5. A Domain clash shows the affinity triangle.
6. A God power swings the board.
7. The finishing blow and win screen.

---

## 7. How we measure success

| Area | Target |
|---|---|
| Match length | 6–10 minutes on average |
| Fairness | First-player win rate between 48% and 52% |
| Card health | No card above a 40% play rate; every card's win rate within ±5% of average |
| Alay adoption | 2–5 offerings per player per match |
| Onboarding | At least 80% of new players finish the tutorial |
| Stability | No desyncs across 100 consecutive online matches |

---

## 8. Risks and how we handle them

| Risk | Impact | Mitigation |
|---|---|---|
| Balance problems such as first-player advantage or Alay combos | High | AI-vs-AI simulation from M1, values marked [tune], and three balance passes already applied |
| Online desync or cheating | High | One shared rules engine, a server-authoritative design, and snapshot hash checks |
| Pay-to-win perception | High | Fixed stats, everything craftable, Gods not sold directly |
| Gacha regulations (odds disclosure, regional restrictions, minors) | Medium | Published odds and pity, an age gate, spending caps, and a per-region legal review before launch |
| Server costs for ranked | Medium | Ranked deferred to M5; casual keeps using Relay |
| Art quality (placeholder AI models) | Medium | Gameplay is built first; art and animation get a dedicated milestone (M6) |
| Misrepresenting folklore | Medium | Every card is checked against sources (Cards.md §11), and doubtful figures were replaced |
| Scope creep | Medium | The out-of-scope list (GDD §12) and the competition scope (§6 above) |

---

## 9. Beyond launch

- **Story expansions:** each one can reveal a little more of the five
  protected mysteries (the Relic, the Awakening, why only two Inheritors exist,
  the original separation, and the factions' true motives) without resolving
  them.
- **New card sets:** expansion folklore candidates include Sigbin, Amomongo,
  Busaw, Mambabarang, Engkanto, Bal-Bal and Hukluban (as a creature).
- **Live operations:** seasonal ranked, featured God banners, and the Landas
  season pass.
- **Cultural partnerships:** working with folklorists and educators on lore
  accuracy and in-game folklore notes.
