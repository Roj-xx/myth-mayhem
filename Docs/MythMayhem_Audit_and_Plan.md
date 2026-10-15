# Myth Mayhem — Systems Audit & TDD Implementation Plan

Audit only. No gameplay code, scenes, assets, or project settings were modified.
All findings below are from **source inspection** unless explicitly marked
`[runtime]` (needs Unity Play Mode confirmation) or `[offline]` (proven by a
compiler/editor check we ran).

- Project: `C:\Dev\Myth Mayhem` — Unity `6000.3.26f1`, URP `17.3.0`, uGUI (no TMP),
  Input System only (`activeInputHandler: 1`), text-mode serialization
  (`m_SerializationMode: 2`), domain reload enabled.
- No `.asmdef` anywhere → all runtime code is in `Assembly-CSharp`, all editor
  tooling in `Assembly-CSharp-Editor`. No test assembly exists.
- Test Framework `com.unity.test-framework 1.6.0` is installed but unused.

---

## Verification performed for this audit

| Check | Command | Result |
|---|---|---|
| Runtime assembly | `dotnet build Assembly-CSharp.csproj` (Unity-generated) | **0 warn / 0 err** `[offline]` |
| Editor assembly | `dotnet build Assembly-CSharp-Editor.csproj` (Unity-generated) | **0 warn / 0 err** `[offline]` |
| Hand-rolled runtime check | `%TEMP%\opencode\uicheck` (all 21 runtime files, engine refs) | **0 warn / 0 err** `[offline]` |
| Hand-rolled editor check | `%TEMP%\opencode\uiedit` (all editor tools + InputSystem ref) | **0 warn / 0 err** `[offline]` |
| Deck wiring data | scene `cardPool` GUIDs vs `.asset.meta` GUIDs | **match** (all 20 entries) `[offline]` |
| Script GUID integrity | every `.cs.meta` guid is 32 hex | **valid** `[offline]` |
| Play Mode behavior | — | **NOT verified** |

Offline compiles prove the C# compiles and the assets are referenced correctly.
They do **not** prove Unity scene/gameplay correctness (per working rule #2).

---

## A. Current-State Architecture Report

### A.1 Runtime classes and responsibilities

| Class (namespace) | Responsibility | Notes |
|---|---|---|
| `BoardManager` (`MythMayhem.Board`) | 3 lanes × 2 sides; `PlayerSpawns[3]`, `OpponentSpawns[3]`; `GetPlayerSpawn/GetOpponentSpawn` | `LaneCount = 3` const. On `Battlefield3D`. |
| `CreatureSide` enum | Player/Opponent | in `CreatureInstance.cs` |
| `CardType` enum | Creature/Support/Building | in `DeckCard.cs` |
| `CreatureDefinition` (SO) | name, prefab, maxHealth, attack, cost | immutable at runtime |
| `SupportCardDefinition` (SO) | name, cost, effectType, magnitude, durationTurns, description | effects: FriendlyAttackBuff, FriendlyHeal, EnemyAttackDebuff |
| `BuildingDefinition` (SO) | name, cost, maxHealth, effectType, magnitude, description | effects: LaneDefense, FriendlyAttackBonus |
| `DeckCard` (serializable) | type + 3 optional definition refs; `Type`, defs, `HasValidDefinition`, `DefinitionName`, `Cost` | no stable id |
| `DeckController` (MB) | `cardPool`, `initialHandSize=5`, `maxHandSize=10`; `drawPile`/`hand`; `Initialize`, `ShuffleDrawPile`, `DealInitialHand`, `DrawToHand`, `AddToHand`, `GetHandCard`, `RemoveFromHand` | **no discard, no reshuffle, no pile reset** |
| `CreatureInstance` (MB) | runtime combatant: `MaxHealth`, `CurrentHealth`, `Attack = BaseAttack + attackModifier`, `attackModifier`, `attackModifierTurns`; `Initialize`, `AddAttackModifier`, `Heal`, `OnOwnerTurnBegan`, `TakeDamage`, `SetHealthBar` | destroys its health bar on death |
| `CreatureFactory` (MB) | instantiate creature prefab *or* capsule placeholder; lane tint; add `CreatureInstance`; spawn health bar; `FallbackMaterial` | placeholder path is the intended render path |
| `CreatureHealthBar` (MB) | world-space canvas bar + `current/max` label; `LateUpdate` follows target | no building equivalent |
| `BuildingInstance` (MB) | cube placeholder; side tint; `MaxHealth`, `CurrentHealth`, `IsAlive`; `TakeDamage` (destroys at 0); logs to console only | no health bar, no color feedback change |
| `CardPlayController` (MB) | authoritative-ish for **buildings**, **support application**, **global attack bonus**, **modifier ticking** on `TurnBegan` | holds `playerBuildings[3]`/`opponentBuildings[3]` |
| `SummonController` (MB) | player pointer input (tap lane / enemy lane); validates & performs **player** plays; owns `laneOccupants[3]` for player creatures | also does creature summon + support + building |
| `OpponentController` (MB) | AI: weighted-random one play/turn; owns `laneOccupants[3]` for opponent creatures; coroutine turn routine | separate summon implementation from player |
| `CombatController` (MB) | per-lane resolution; manual Space key; optional auto interval | reads both controllers + `cardPlay` |
| `TurnManager` (MB) | turn alternation, resources, initial deals, per-turn draw, raises `TurnBegan` | no win/lose, no restart |
| `CardHandController` (UI) | builds up to `MaxHandSize` slots, labels (`name\nstats\nCost n`), selection, `ConsumeSelected` | rebuilds layout every frame in `Update` |
| `UITestController` (UI) | sets an instruction label in `Awake` | legacy/unused-ish demo |
| `MainMenuController`, `SplashController`, `SafeAreaFitter`, `ReturnToMenuButton` (UI) | menu/splash scene flow | `ReturnToMenuButton` is **not referenced by any scene** |

### A.2 Editor tooling

| Tool | Purpose |
|---|---|
| `CardCollectionSetup` | `Setup Card Collection` (ensure defs, rebuild both 10-card pools, add+wire `CardPlayController`), `Validate Card Collection` | 
| `SceneDiagnostics` | validate each build scene has exactly one EventSystem + AudioListener and correct input module |
| `SceneSetupTools` | `Build All Scenes`, `ConfigureBuildSettings`, and an `[InitializeOnLoadMethod]` auto-rebuild guard |
| `UiLayoutValidator` | menu/splash layout overlap/out-of-bounds checks |
| `MainMenuSceneBuilder`, `SplashSceneBuilder`, `MythMayhemUi` | programmatic construction of menu/splash scenes + generated sprites |

### A.3 Verified / Partial / Missing / Unsafe / Decision-needed

- **Verified by source inspection:** lane topology (3×2); resource math; turn
  alternation; combat occurs at end of opponent turns; deck/hand/draw basics;
  card-pool wiring; definitions immutable at runtime; both Unity assemblies compile.
- **Reported/implemented, needs runtime confirmation `[runtime]`:** AI plays
  exactly one valid card per turn; support/building placement through the tap flow;
  buildings intercept only when the lane has no owner creature; modifier expiry;
  weighted randomness; splash/menu scene auto-build.
- **Missing:** hero/player HP; victory/defeat/draw; post-game input rejection;
  discard pile & reshuffle; draw exhaustion handling; restart/clean re-init;
  activated abilities/exhaustion; landscape cards; stable card/entity IDs;
  building health UI; any automated test.
- **Partially implemented:** shared validation/resolution (player and AI have two
  separate summon implementations); effect duration rules undocumented;
  building-damage overflow undefined; repeat-resolution protection absent.
- **Contradictory / unsafe:** (1) `EditorBuildSettings` splash GUID is all-zeros →
  `SceneSetupTools.AutoSetupTick` re-runs `BuildAll()` on **every** domain reload
  (see A.5). (2) `CombatController` Space key calls `ResolveRound` with no debounce,
  so combat can be re-triggered anytime including during an opponent turn (double
  resolution). (3) `CardHandController.Update` rebuilds UI every frame.
- **Requires a design decision:** hero HP value & direct-attack model; stacking vs
  overwrite for buffs; building damage overflow; draw-pile exhaustion rule;
  whether landscape/abilities are in scope.

### A.4 Turn / phase / resource / deck behavior (verified)

- `Start()`: `activeSide=Player`, `openingTurn=true`, both sides set to 5 points,
  both decks `DealInitialHand()` (5 cards), raises `TurnBegan`.
- `EndTurn()` (0.5s debounce): if `!openingTurn && activeSide==Opponent` → combat.
  Then flip side, award +2 to the new side **if it has already started a turn**,
  refresh labels, refresh hand, draw one card for the new side, raise `TurnBegan`.
- Resource flow: `SetPoints/GainPoints` clamp ≥0; `CanAfford(side,cost)`; `Spend`
  requires `side==activeSide`. Unspent points carry over (Gain adds to current).
- Effective points: side starts at 5; its **second** and later turns +2 → 7,9,…
- Draw: `DrawToHand` skips (warn) when hand ≥10 or pile empty. No reshuffle.

### A.5 Known runtime risks (flagged)

1. **Splash GUID loop** — `ProjectSettings/EditorBuildSettings.asset` lists
   `Assets/Scenes/SplashScreen.unity` with `guid: 0000…0000`, while the file's
   `.meta` GUID is `29c7718f5352d1f4e8fb4aab38c02038`. `HasValidSceneGuid()` returns
   false, so `AutoSetupTick` calls `BuildAll()` on every reload. This is wasteful
   and indicates `ConfigureBuildSettings()` is not persisting a real splash GUID.
   `[runtime]`
2. **Combat re-entrancy** — Space key → `ResolveRound(true)` bypasses the turn
   gate and the end-turn debounce. `[runtime]`
3. **No terminal state** — a match cannot be won/lost; only quitting ends it.

---

## B. Requirements Traceability Matrix

Status legend: **V** verified (source) · **R** implemented, needs runtime confirm ·
**P** partial · **M** missing · **U** unsafe/contradictory · **D** decision needed.

| ID | Requirement | Current implementation | Status | Missing / gap | Depends on | Proposed location | Acceptance test |
|---|---|---|---|---|---|---|---|
| BRD-1 | 3 lanes per side | `BoardManager` 3×2, scene spawns | V | — | — | — | spawn arrays length 3 |
| TRN-1 | Alternating turns | `TurnManager.EndTurn` flips `activeSide` | V | — | — | — | two ends return to Player |
| RES-1 | Both start at 5 | `Start()` `SetPoints(...,5)` | V | — | — | — | points==5 at start |
| RES-2 | +2 from 2nd turn | `AwardTurnStartPoints` | V | — | — | — | 5→5(opp1)→7→7→9 |
| RES-3 | Unspent carry over | `GainPoints` adds | V | — | — | — | spend 3 then +2 → 6? verify |
| RES-4 | Cards cost points, spent on play | `Spend` after success | V | — | — | — | cost deducted only on success |
| HAND-1 | Initial hand 5 | `DealInitialHand` | V | — | — | — | hand==5 after Start |
| HAND-2 | Max hand 10 | `maxHandSize=10`; `DrawToHand` guard | V | draw skip silently | — | — | 11th draw skipped |
| DEC-1 | Separate 10-card decks | scene pools rebuilt by tooling | V | — | — | — | each pool==10 `[offline]` |
| CRD-1 | Creature cards | `CreatureDefinition` | V | — | — | — | pool type 0 + def |
| CRD-2 | Support cards | `SupportCardDefinition` | R | runtime effect | — | CardPlayController | apply buff/heal/debuff |
| CRD-3 | Building cards | `BuildingDefinition` | R | runtime effect | — | CardPlayController | place/intercept |
| CRD-4 | Card type validation | `HasValidDefinition` | P | no cross-type guard; no id | — | DeckCard/ActionValidator | null/def-mismatch rejected |
| OCC-1 | 1 creature / side / lane | lane arrays + checks | V | duplicated logic (2 impls) | — | unify in ActionValidator | second summon rejected |
| OCC-2 | Buildings occupy lane | `CardPlayController` arrays | R | no UI feedback | — | — | place/deny |
| OCC-3 | Creature↔building exclusivity | checks in both play paths | R | — | — | ActionValidator | mutual exclusion |
| SUP-1 | Support targeting | immediate on validated target | R | target rules per effect | — | CardPlayController | buff/heal own; debuff enemy |
| SUP-2 | Effect duration timing | decrement at owner `TurnBegan`, overwrite | R/D | rule undocumented; no stack model | — | EffectManager | expires after N owner turns |
| BLD-1 | Building damage/interception | absorb only if lane has no owner creature | R | overflow undefined; D | — | CombatController | building takes damage |
| BLD-2 | Building destruction | destroys at 0 | R | no UI | — | — | removed at 0 |
| BLD-3 | Passive effect | `FriendlyAttackBonus` global | R | stacking across buildings additive | D | CardPlayController | +N per bonus building |
| CBT-1 | Combat at end of opponent turn | `EndTurn` gate | V | Space key bypasses | U | CombatController | one resolve per round |
| CBT-2 | Simultaneous damage | both computed pre-apply | V | — | — | — | both die if lethal |
| CBT-3 | No repeated resolution | — | M | no idempotency guard | — | Reduce/guard | resolve once |
| AI-1 | ≤1 card/turn | `playedThisTurn` | R | — | — | — | 1 play then end |
| AI-2 | Weighted afford+valid | `ChooseWeighted` (creature 2, else 1) | R | — | — | — | distribution fuzz |
| AI-3 | Shared validation pipeline | support/building share `cardPlay`; **summon duplicated** | P | two summon impls | — | ActionValidator | same legality both sides |
| SYS-1 | No consume on failure | validate→commit ordering | V | — | — | — | failed play keeps card+points |
| SYS-2 | No mutation of definitions | runtime mutates instances only | V | — | — | — | SO unchanged after play |
| SYS-3 | Stable IDs | none | M | add `CardId` | — | DeckCard | id stable across play |
| SYS-4 | Null/empty/full/dead/dup/terminate safety | partial guards | P | no termination; dup events | — | MatchController | fuzz + edge cases |
| WIN-1 | Victory/defeat/draw | none | M | whole subsystem | D | MatchController | HP≤0 ends match |
| WIN-2 | Hero HP & direct attacks | none | M | model + UI | D | HeroState/Combat | unbeaten lane hits hero |
| WIN-3 | Post-game action rejection | none | M | gate all actions | WIN-1 | MatchController/ActionValidator | no plays after end |
| DEC-2 | Draw-pile exhaustion/reshuffle | skip draw | M | reshuffle rule | D | DeckController | reshuffle or fatigue |
| RST-1 | Restart/clean init | none | M | reinit + unsubscribe | WIN-1 | MatchController | new match resets state |
| EFF-1 | Trigger ordering/re-entrancy | implicit | P/U | deterministic order | — | EffectManager | defined order |
| ABL-1 | Activated abilities/exhaustion | none | M/D | scope decision | D | Abilities | defer or implement |
| LND-1 | Landscape cards | none | M/D | scope decision | D | — | exclude initial |

---

## C. Consolidated Technical Design (TDD)

Design leans on **existing** components; new types are proposed only where a
responsibility is genuinely unowned. Nothing here is written yet.

### C.1 Target responsibility map (reuse first)

| Responsibility | Reuse | Add / change |
|---|---|---|
| Match lifecycle & terminal state | — | **new** `MatchController` (or minimally extend `TurnManager`) |
| Authoritative match state | scattered | consolidate board+resources+life into a `MatchState` view |
| Turn/phase management | `TurnManager` | add phases (Start/Draw/Main/Resolve/End) |
| Shared action validation | — | **new** `ActionValidator` (pure) |
| Action resolution | `CardPlayController` | move commit here; command objects |
| Board & lane occupancy | `BoardManager` + lane arrays | single `BoardState` (one source of truth) |
| Deck/hand/discard | `DeckController` | add discard + reshuffle |
| Card-play orchestration | `SummonController`/`OpponentController` | thin via `ActionValidator`+`ActionResolver` |
| Combat & damage | `CombatController` | extract pure `CombatResolver`; add hero hits + idempotency |
| Temp/permanent effects | `CreatureInstance` modifier | **new** `EffectManager` (stacking/duration) |
| Abilities/exhaustion | — | defer (ABL-1) |
| AI decision | `OpponentController` | enumerate legal actions via `ActionValidator` |
| Victory | — | inside `MatchController` |
| UI | `CardHandController` | add hero HP, feedback, building health |
| Data validation/gameplay tests | editor validators | **new** test asmdef + EditMode/PlayMode tests |

### C.2 Match state machine

```
NotStarted --Start()-> InProgress --(life<=0 | deck-out rule)-> Finished(Outcome)
Finished: all actions rejected; input disabled; Restart allowed.
Outcome: PlayerWin | OpponentWin | Draw
```
Recommended outcome rules (D): a side wins when the opposing **hero life ≤ 0**
(**confirmed: 30 HP**); simultaneous lethal → **Draw**. Deck-out uses the
exhaustion rule (C.7).

### C.3 Turn state machine

```
BeginTurn -> Award -> Draw -> Main(actions) -> EndTurn -> [ResolveCombat if this
turn is the opponent's] -> flip -> BeginTurn
```
Formalize `openingTurn` as a `TurnPhase`/first-turn flag rather than an implicit bool.

### C.4 Action flow (single pipeline — satisfies rule #10)

```
ActionRequest { actor, kind(Creature|Support|Building), cardId, lane, target }
1. Guardian: match InProgress? actor==activeSide? phase==Main?   -> reject
2. Validate: costs affordable; card in hand & HasValidDefinition;
             lane open / target legal (per kind)                  -> reject (no mutation)
3. Resolve:  apply effect (spawn creature / place building / apply support)
4. Commit:   remove card from hand; spend points; raise event
5. Present:  status text / VFX
Reject path performs no state change (rule #9).
```

### C.5 Card data & runtime entities

- Keep SO definitions immutable (rule #11). Add a stable `CardId` (GUID or
  `definition.name`) to `DeckCard` for logging/replay/tests (rule #12).
- Runtime: `CreatureInstance`, `BuildingInstance` (exist). Add `HeroState`
  (`side`, `maxLife`, `life`) — either as a component on GameDirector or plain data
  in `MatchState`.

### C.6 Board topology

`lanes = 3`; per `(side, lane)` cell holds at most **one** `Occupant` of kind
Creature **or** Building. Keep one shared occupancy store (currently split across
`SummonController.laneOccupants`, `OpponentController.laneOccupants`,
`CardPlayController.*Buildings`). Proposed: a single `BoardState` with
`GetOccupant(side,lane)` returning a discriminated occupant; controllers query it.

### C.7 Resources & deck economy (proposed defaults)

- Resources: unchanged (5 start, +2 from 2nd turn, carry over).
- Draw: on empty pile, **reshuffle discard into draw pile** (confirmed); if both
  empty, no draw (fatigue is out of scope unless later requested).
- Discard: played/destroyed cards go to a discard pile (currently discarded
  entirely). Needed for reshuffle and for “card recycling” effects.

### C.8 Targeting & effect resolution

- Targeting descriptor: `{kind, relativeSide(Self/Enemy), requiresAlive,
  laneConstraint(Any/SameSourceLane)}`. `FriendlyAttackBuff`/`FriendlyHeal` =
  Self+alive; `EnemyAttackDebuff` = Enemy+alive.
- Effect resolution: propose an `EffectManager` that stores timed modifiers as a
  **list**, but with **confirmed stacking policy = overwrite** (a new effect
  replaces the existing magnitude/duration, matching current behaviour). Duration
  counted in **owner turn starts** (matches current). Deterministic trigger order:
  `OnApply → OnOwnerTurnStart → OnExpire` (EFF-1).

### C.9 Combat (proposed)

- Inputs: both sides' occupants, bonuses, buildings, hero life.
- Per lane: both creatures alive → mutual simultaneous damage; one alive & enemy
  creature absent → damage enemy **building** if present else enemy **hero**
  (WIN-2); nothing when both empty.
- Idempotency: combat may run only once per opponent-turn end; expose
  `ResolveCombatRound()` behind a guard/turn-phase check and remove the raw Space
  shortcut (or gate it to debug-only) (CBT-3).
- Overflow (confirmed): **no overflow** — a building absorbs up to its remaining
  life; if destroyed, no spill to hero.

### C.10 AI (proposed)

Enumerate legal `ActionRequest`s via the **same** `ActionValidator`, then choose
weighted-random (creature 2, support/building 1). This removes the duplicated
summon path (AI-3) and guarantees player/AI legality parity.

### C.11 Victory

`MatchController` checks life after each combat resolution and after any direct
effect; on terminal state disables input, announces, allows Restart.

### C.12 Error handling & edge cases (rule #12)

null refs, empty deck, full hand, destroyed entities (Unity fake-null),
duplicate `TurnBegan`/listener subscriptions (ensure symmetric subscribe/unsubscribe
on enable/disable), match termination gating, card with invalid/def-mismatched type.

---

## D. Prioritized Implementation Plan (dependency-ordered)

> Every phase reuses the editor-automation + validator pattern already in the
> project (programmatic setup, no manual Inspector steps) and ends with offline +
> validator checks.

### Phase 0 — Stabilize (low risk, no new design)
- **Scope:** fix splash GUID loop; gate/debounce manual combat; move hand UI
  refresh off per-frame; remove dead `UITestController`/dangling refs.
- **Files:** `SceneSetupTools.cs`, `CombatController.cs`, `CardHandController.cs`,
  `ProjectSettings/EditorBuildSettings.asset` (via tooling).
- **Acceptance:** build settings splash GUID non-zero after one `Build All Scenes`;
  Space cannot double-resolve; hand rebuilds only on change.
- **Regression risk:** touching editor auto-setup could over-trigger; keep behind
  the existing guards.
- **Automated:** csproj builds; `Validate Card Collection`; `Validate Scenes`.
- **Runtime `[runtime]`:** confirm no error loop on reload; one combat/round.

### Phase 1 — Shared ActionValidator/Resolver (enables everything else)
- **Scope:** extract pure legality + resolution; route player (Summon) and AI
  (Opponent) through it; single board occupancy.
- **Files:** new `ActionValidator.cs`, `ActionResolver.cs`, `BoardState.cs`;
  edit `SummonController.cs`, `OpponentController.cs`, `CardPlayController.cs`.
- **Acceptance:** identical legality both sides; existing behaviour preserved.
- **Risk:** behaviour drift in AI; mitigate with golden tests (Phase 7 earlier if
  desired).
- **Automated:** EditMode tests for validator truth table.
- **Runtime `[runtime]`:** summon/support/building still work for both sides.

### Phase 2 — Match lifecycle & victory
- **Scope:** `MatchController`, `HeroState`, direct attacks, terminal gating.
- **Files:** new `MatchController.cs`; edit `CombatController.cs`,
  `TurnManager.cs`, `SummonController.cs`, UI.
- **Acceptance:** life to 0 ends match; no actions after end; Restart resets.
- **Risk:** changes combat/flow — highest regression surface; keep additive.
- **Automated:** EditMode resolution tests; PlayMode smoke.
- **Runtime `[runtime]`:** win/lose/draw reachable; input locked at end.

### Phase 3 — Deck economy
- **Scope:** discard pile, reshuffle, exhaustion rule.
- **Files:** `DeckController.cs`, `DeckCard.cs` (id), commit paths.
- **Acceptance:** empty pile reshuffles; hand cap respected.
- **Risk:** low. **Automated:** EditMode deck tests. **Runtime `[runtime]`:** long match.

### Phase 4 — Effects/triggers formalization
- **Scope:** `EffectManager`, stacking decision, interception/overflow decision,
  deterministic trigger order.
- **Files:** new `EffectManager.cs`; edit `CreatureInstance.cs`, `CardPlayController.cs`.
- **Acceptance:** documented duration/stack/overflow behaviour under test.
- **Risk:** medium. **Automated:** EditMode effect tests. **Runtime `[runtime]`.**

### Phase 5 — UI/UX
- **Scope:** hero HP display, building health, card affordance/feedback,
  selection clarity.
- **Files:** `CardHandController.cs`, scene builders/`MythMayhemUi`, new HUD script.
- **Acceptance:** HP + building health visible; feedback on illegal play.
- **Risk:** low. **Automated:** `Validate UI Layout`. **Runtime `[runtime]`.**

### Phase 6 — Optional systems (decide scope)
- Abilities/exhaustion (ABL-1), landscape cards (LND-1), tutorial, PvP.
- **Decision required** before scheduling.

### Phase 7 — Test harness & CI (schedule early to enable TDD)
- **Scope:** `Tests/` asmdefs (EditMode + PlayMode), deterministic tests, CI hook.
- **Files:** new `Assets/Tests/**`, `.asmdef`s.
- **Acceptance:** suite green in Unity Test Runner.
- **Risk:** low. **Automated:** Test Runner. **Runtime:** run in editor.

---

## E. Test Plan

Goal: deterministic tests independent of rendering/timing. Requires extracting
pure logic (Phase 1/2/4) so it can run under EditMode without a scene.

### E.1 EditMode (pure logic)
- **Resources:** 5 start; +2 from 2nd turn; carry-over; `Spend` only active side;
  `CanAfford` boundaries.
- **Card validation:** null def, wrong-type def, cost boundaries, `HasValidDefinition`.
- **Consumption:** failed action leaves hand+points intact; success removes exactly
  one card and spends exact cost.
- **Targeting:** buff/heal require own alive creature; debuff requires enemy alive;
  building requires empty own lane w/o creature or building.
- **Combat:** simultaneous mutual damage; lethal-both; single-alive→building; →hero
  (after Phase 2); building destroyed at 0; no overflow (per decision).
- **Status expiration:** buff/debuff applied turn N lasts exactly N owner-turns;
  overwrite/non-stack per decision.
- **Building destruction:** removal, occupancy freed, passive bonus removed.
- **AI legality:** never chooses unaffordable/illegal; ≤1 play/turn; distribution
  over many seeds.
- **Victory:** life≤0 → correct outcome; simultaneous → draw; post-game rejection.
- **Restart:** full clean re-init; no leaked subscriptions.

### E.2 PlayMode (scene-level smoke)
- Load `SampleScene`; execute opening deals; perform one legal play per card type
  for each side; run combat; assert no exceptions, expected status text.
- Event subscription symmetry across scene reload (domain reload enabled).

### E.3 Existing editor validators (keep as gates)
- `Validate Card Collection`, `Validate Scenes`, `Validate UI Layout`.

---

## F. Execution Strategy

### F.1 Safe to automate (no design decision needed)
- Phase 0 stabilization; code extraction (Phase 1, 4) preserving behaviour;
  Phase 3 deck economy; Phase 7 test harness; all editor setup/validation additions.
- All scene/asset mutation continues through editor menus (`CardCollectionSetup`,
  scene builders) — no manual Inspector steps.

### F.2 Design decisions — CONFIRMED (2026-10-12)
1. **Buff stacking** = **overwrite** (current behavior; new effect replaces old
   magnitude/duration).
2. **Victory** = **hero HP 30**, direct attacks from an unblocked lane, and
   **simultaneous lethal = draw**.
3. **Building damage overflow** = **absorb only, no spill to hero**.
4. **Draw-pile exhaustion** = **reshuffle discard into draw pile** (requires
   adding a discard pile).
5. **Abilities / exhaust / landscape** = **deferred** to a later phase.

These unblock Phases 2 and 4; Phase 6 (optional systems) stays deferred.

### F.3 Guardrails honored
- No wholesale rewrite; extend existing classes (rule #4). Any change to a working
  system is called out with its regression risk in Phase sections (rule #5).
- No duplicate managers: `MatchController`/`ActionValidator`/`EffectManager` are
  net-new **only** because no existing class owns those responsibilities (rule #3).
- Preserve GUIDs/scene refs; all asset/scene edits via tooling (working rules).
- Offline compiles are treated as necessary-but-insufficient (rule #2).

---

## Implementation Log

### Phase 0 — Stabilize (in progress)
- **`SceneSetupTools.cs`** — separated "scene asset missing" (full `BuildAll`) from
  "stale build-settings GUID" (cheap `ConfigureBuildSettings` repair); GUID now
  resolved from the `.meta` file as a fallback so a freshly saved scene is
  referenced before AssetDatabase import completes. Intended to stop the
  every-reload `BuildAll()` loop. **`[runtime]` verify** GUID persists non-zero.
- **`CombatController.cs`** — added `resolving` re-entrancy guard and
  `enableManualCombat` / `manualCombatCooldown` (0.5s) debounce around the Space
  key so it cannot double-resolve alongside end-turn combat. **`[runtime]` verify.**
- **`CardHandController.cs`** — `Update()` now compares a cheap signature
  (hand count, selection, active side, panel width, per-card identity) and only
  rebuilds when it changes (was rebuilding every frame).
- **Offline:** `Assembly-CSharp.csproj` and `Assembly-CSharp-Editor.csproj` both
  build **0 warning / 0 error** after the changes.

### Phase 2 — Match lifecycle & victory (implemented)
- **`MatchController.cs`** (new, on GameDirector) — hero HP (default **30** each),
  `ApplyHeroDamage` / `ApplyHeroDamageBoth`, outcome detection
  (PlayerWin / OpponentWin / **Draw** on simultaneous lethal), `IsMatchOver`,
  events `HeroHealthChanged` / `MatchEnded`, and `Restart()` (reloads the active
  scene).
- **`HeroHud.cs`** (new, built at runtime under the scene Canvas — no manual
  editor steps) — player/enemy hero HP labels and a game-over overlay with a
  RESTART button.
- **`CombatController.cs`** — when a lane has an attacker but no opposing
  creature and no opposing building, it now deals its attack (plus global bonus)
  to the enemy **hero** (direct attacks). Both sides' direct damage is applied in
  one batch so simultaneous lethal resolves as a Draw.
- **Gating** — `TurnManager.IsMatchOver`; `EndTurn` aborts when the match is over
  (and re-checks right after combat); `SummonController` rejects plays; the AI
  routine and debug key stop. Post-game input is therefore rejected.

### Phase 3 — Deck economy (implemented)
- **`DeckController.cs`** — added a **discard pile**; `RemoveFromHand` now moves
  the played card to the discard pile; when the draw pile empties,
  `ReshuffleDiscardIntoDraw()` recycles the discard back into the draw pile and
  shuffles. New `DiscardPileSize` / `Discard(card)` API.

### Wiring
- **`CardCollectionSetup.cs`** — `WireController` now also adds a `MatchController`
  to the GameDirector and wires `CombatController.matchController` and
  `TurnManager.matchController`; `ValidateWiring` asserts both.

### Phase 1 — Shared action pipeline (implemented)
- **`CardPlayController.cs`** — now the single authority for lane legality and
  summoning: added `IsLaneOpen(side, lane)`, `FindOpenLane(side)`, `GetSpawn`,
  `SetLaneOccupant(side, lane, occupant)` and `TrySummonCreature(side, def, lane,
  out failure)` (validate → spawn via factory → register occupant).
- **`SummonController.cs`** — player summon/building placement now validate and
  spawn through `CardPlayController`; added `SetLaneOccupant`; dropped its now
  redundant `creatureFactory` field.
- **`OpponentController.cs`** — AI uses the same `FindOpenLane` /
  `TrySummonCreature` path; added `SetLaneOccupant`; dropped its now redundant
  `creatureFactory` field.
- Result: player and AI share one lane-validation + summon pipeline.

### Phase 5 — Building health UI (implemented)
- **`IHealthTarget.cs`** (new) — abstraction over anything that shows a health
  bar (`MaxHealth`, `CurrentHealth`, `HealthBar`).
- **`CreatureHealthBar.cs`** — now targets `IHealthTarget` (works for creatures
  **and** buildings); null-checks the target via its `MonoBehaviour` to avoid the
  Unity fake-null pitfall.
- **`CreatureInstance.cs` / `BuildingInstance.cs`** — implement `IHealthTarget`;
  both destroy their bar on death.
- **`CardPlayController.cs`** — spawns a world-space health bar for every placed
  building (serialized scale/size/height config; on by default).

### Phase 7 — Test assembly (implemented)
- **`Assets/Scripts/MythMayhem.asmdef`** (new) — moves all runtime code into a
  `MythMayhem` assembly (references `UnityEngine.UI`, `Unity.InputSystem`;
  `autoReferenced` so editor scripts keep working).
- **`Assets/Scripts/Gameplay/MatchRules.cs`** (new) — pure `EvaluateOutcome`
  extracted from `MatchController`, which now delegates to it.
- **`Assets/Tests/EditMode/MythMayhem.EditModeTests.asmdef`** (new) — Editor-only
  test assembly (`overrideReferences` + `nunit.framework.dll`,
  `UNITY_INCLUDE_TESTS`).
- **`MatchRulesTests.cs` / `DeckControllerTests.cs`** (new) — EditMode tests for
  win/draw/none outcomes and deck discard/reshuffle economy.

### Buildings + creatures coexist in one lane (implemented)
- **Rule split** — `CardPlayController.IsLaneOpen` was replaced by
  `IsLaneOpenForCreature` (no living creature) and `IsLaneOpenForBuilding` (no
  building), plus `FindOpenLaneForCreature` / `FindOpenLaneForBuilding`. A lane can
  now hold one creature **and** one building on the same side.
- **Dedicated spawn points** — `BoardManager` gained `playerBuildingSpawns` /
  `opponentBuildingSpawns`; the editor menu `Myth Mayhem/Board/Arrange Lane Spawns
  (Creatures Front, Buildings Back)` creates a visible marker per lane and arranges
  the layout: **creature spawns move toward the board centre**, each **building spawn
  takes the creature's former spot**. `CardPlayController` places buildings on those
  spawns, falling back to a runtime back-offset when none are assigned.
- **Placement** — buildings are seated *behind* the lane's creature via
  `GetForwardDirection` (own→enemy spawn) × `buildingBackOffset` (default 1.3);
  `BuildingInstance.Create` now takes a `positionOffset`.
- **Player + AI** — `SummonController` and `OpponentController` use the split rules,
  so both sides can stack a building behind a creature.
- **Lane-scoped buff** — `GetAttackBonus` (side-wide total) became
  `GetLaneAttackBonus(side, lane)`; `CombatController` now applies a building's
  `FriendlyAttackBonus` only to the creature in its own lane.

### Still open
- Phase 7 tests must be run from Unity's Test Runner (EditMode) to confirm green;
  the assembly split itself also needs a Unity recompile to confirm.

- **Offline re-verified:** runtime + editor check projects build **0/0** with the
  new `MatchController.cs` / `HeroHud.cs` included.

### Balance pass (implemented 2026-10-15)
- **Problem:** hero HP 30 vs attacks up to 30 meant one unblocked lane could kill in
  1–2 rounds, while creature HP (45–180) was 3–16× attack so the board never cleared —
  binary "first open lane wins". Cost curve was also non-monotonic (2-drops beat
  4/5-drops).
- **Framework (aligned to CCG standards):** hero HP ≈ 5–10× average creature attack;
  creature HP ≈ 1.5–2.5× its attack (trades resolve in 2–3 rounds); stats scale with
  cost; resource ramps with a cap.
- **Applied:** hero HP 30 → **100** (`MatchController.maxHeroHealth` default + scene).
  Added `TurnManager.maxSummonPoints = 8` and clamped `SetPoints`/`GainPoints`
  (start 5 / +2 per turn / cap 8, scene updated).
- **Final card values** (assets + `CardCollectionSetup` specs synced):

  | Card | Cost | ATK | HP |
  |---|---|---|---|
  | Diwata | 1 | 3 | 12 |
  | Aswang | 1 | 5 | 8 |
  | Tiyanak | 2 | 8 | 16 |
  | Manananggal | 2 | 11 | 11 |
  | Tikbalang | 2 | 6 | 22 |
  | Kapre | 4 | 14 | 34 |
  | Bakunawa | 5 | 19 | 46 |

  | Support | Cost | Effect |
  |---|---|---|
  | Lakas ng Diwata | 1 | +4 ATK, 2 turns |
  | Hilom | 1 | Heal 15 |
  | Sumpa ni Tiyanak | 1 | −4 ATK, 2 turns |

  | Building | Cost | HP | Effect |
  |---|---|---|---|
  | Bantayog | 2 | 30 | absorbs lane damage |
  | Sagrado | 2 | 30 | +2 ATK to lane |

- `CardCollectionSetup.CreatureSpecs` was stale (4 creatures); now lists all 7 so
  re-running Setup applies the new values.

### Bugfix — Sumpa ni Tiyanak could not target enemy creatures (2026-10-15)
- **Cause:** `SummonController.Update` checked `FindPlayerLane` first with a 200px
  `tapRadius`. After the spawn rows moved to z=±1.5 they are only ~96 px apart on
  screen (camera at (0,10,−14), FOV 60), so tapping an enemy creature also fell
  within 200 px of the player spawn → routed as a player-lane play → the debuff was
  rejected (`target.Side == Player`).
- **Fix:** branch on the selected card — a debuff (`CanTargetEnemyLane()`) resolves
  enemy lanes only and never falls through to the player row; all other cards resolve
  player lanes only.

---

## Appendix — Facts pinned during audit
- Scene `GameDirector` component fileIDs: Summon `…382`, Factory `…383`,
  Opponent `…384`, Combat `…385`, TurnManager `…386`, CardPlay `…387`.
- `PlayerDeck` fileID `139456257`, `OpponentDeck` `460774189`; both 10 entries and
  each entry's definition GUID matches the corresponding `.asset.meta` `[offline]`.
- `CardHandController` scene value `maxHandSlots: 5` is inert because `playerDeck`
  is wired (`MaxSlotCount` → `playerDeck.MaxHandSize = 10`).
- `activeInputHandler: 1` (Input System only) — legacy `Input` API unavailable.
- No `.asmdef`; no `Tests`; the only file named `*Test*.cs` is `UITestController`.
- `ReturnToMenuButton` is defined but unused by any scene.
