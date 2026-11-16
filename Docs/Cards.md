# Myth Mayhem — Card Set v1 (Rework)

Companion to [GDD.md](GDD.md). Rules terms (Alay, Strike, keywords, Domain
clash) are defined in the GDD; this file is the content list.

- Date: 2026-11-16 · Status: **draft for co-dev review** · Lore pass: verified against sources (see §11)
- Source: co-dev initial rework list, tailored to the locked GDD decisions.
- All numbers are **[tune]** until the AI-vs-AI simulation (GDD §8.5).

## 0. Locked decisions this list follows

| Decision | Choice |
|---|---|
| Rarity model | **Fixed stats per card.** One card = one rarity = one stat line. Rarity only sets pull odds. |
| Duplicates | Copies beyond the deck max convert to **Abo** (crafting). |
| Stat scale | **Inheritor 100 HP.** All combat numbers (ATK, HP, damage, healing, Shield, ATK changes) are ×3 compared with the original 30-HP design. SP, costs, Charges and card draw are **not** scaled. |
| Gods | **Patron Gods** replace the Inheritor Power. Not deck cards, never on the board. Each has a **Passive + charged Active**. |
| God access | 3 free starter Gods; the rest from the Tawag banner or crafted with Abo at a high cost. |
| Domains | **One shared Domain slot**, resolved by the **affinity triangle**. |
| Starter | Two themed 20-card starter decks. |
| Other card access | Craft with Abo, Ginto shop rotation, story/quest rewards, direct buy with Mutya. |
| Pull system | One gacha (**Tawag**), Gods are the rarest tier. |

## 1. Categories

| Category (co-dev name) | Was in GDD v1 | Count | On board? |
|---|---|---|---|
| **Creature** | Creature | 24 | Creature slot |
| **Ritual** (Ritual Support) | Invocation | 8 | No — resolves |
| **Domain** | — (new) | 9 | Shared Domain slot |
| **Building** | Structure | 3 | Building slot |
| **God** (Patron) | Inheritor Power | 11 | No — patron, chosen pre-match |

Deck cards: **44**. With Gods: **55** collectibles.

## 2. Rarity ladder

| Tier | Symbol | Who | Deck copies max |
|---|---|---|---|
| Common | ★3 | Creatures, Rituals, Domains, Buildings | 2 |
| Rare | ★4 | 〃 | 2 |
| Legendary | ★5 | 〃 | 1 |
| **Divine** | ✦ | **Gods only** — the rarest tier | 1 patron per deck |

Rarity = complexity and identity, **not** stat efficiency.
Stat budget: ATK + HP ≈ 6 × cost + 6; each keyword ≈ 3 points; abilities 3–6 points.
All combat numbers are multiples of 3, so the ×3 scale stays clean.
Commons have at most one keyword and no unique ability (an Alay effect is allowed).

---

## 3. Creatures (24)

Changes from co-dev list: rescaled stats; costs mostly kept (Bakunawa 5→6,
Manananggal 2→3, Bungisngis stays 4); every card got a role.

| # | Card | Rarity | Cost | ATK/HP | Keywords / Ability | Alay effect | Folklore basis |
|---|---|---|---|---|---|---|---|
| 1 | **Aswang** | ★3 | 1 | 6/3 | Drain | — | Shapeshifting flesh-eater |
| 2 | **Tiktik** | ★3 | 1 | 3/6 | Flight | Draw a card | Aswang in bird form; its "tik-tik" call grows fainter the closer it gets |
| 3 | **Diwata** | ★3 | 1 | 3/6 | Guard | Restore 9 HP to your Inheritor | Benevolent nature spirit |
| 4 | **Duwende** | ★3 | 1 | 3/9 | — | Gain +1 extra SP (this Alay gives +2) | Dwarf spirit bringing fortune or misfortune |
| 5 | **Tiyanak** | ★3 | 2 | 6/9 | Swift | — | Demon disguised as an infant; strikes when approached |
| 6 | **Shokoy** *(Siyokoy)* | ★3 | 2 | 9/6 | Pierce | — | Scaled, web-limbed sea humanoid that drags swimmers to the deep. Not a male Sirena. |
| 7 | **Wakwak** | ★3 | 2 | 9/6 | Flight | — | Vampiric bird, named after its wing sound |
| 8 | **Santelmo** | ★3 | 2 | 6/9 | — | Deal 3 damage to an enemy creature | Floating fireball spirit |
| 9 | **Pah** | ★3 | 2 | 9/9 | — | — | Maguindanao giant bird whose wings blot out the sun (Cole, *Philippine Folk Tales*, 1916) |
| 10 | **Kaperosa** *(White Lady)* | ★3 | 2 | 6/9 | Elusive | Enemy creature −3 ATK this turn | Woman ghost in white; "White Lady" is the American-era name |
| 11 | **Bungisngis** | ★3 | 4 | 15/12 | Pierce | — | Laughing one-eyed giant (Tagalog/Cebuano tales) |
| 12 | **Tikbalang** | ★4 | 2 | 6/9 | **Mislead (OnSummon):** swap the lanes of two enemy creatures | — | Horse-headed trickster who leads travelers astray |
| 13 | **Sirena** | ★4 | 2 | 6/9 | **Siren Song (OnStrike):** the creature it strikes loses 3 ATK permanently | — | Mermaid whose song lures sailors |
| 14 | **Nuno sa Punso** | ★4 | 2 | 3/9 | Guard. **Curse of the Mound (OnDeath):** deal 6 damage to the creature that killed it | Deal 6 damage to an enemy creature | Anthill spirit that curses trespassers |
| 15 | **Batibat** | ★4 | 2 | 3/12 | **Nightmare (OnStrike):** the struck creature can't strike on its owner's next turn | Enemy creature −6 ATK this turn | Ilocano sleep demon, an obese hag that sits on sleepers' chests (linked to *bangungot*) |
| 16 | **Berberoka** | ★4 | 3 | 6/15 | **Drink the River (OnSummon):** destroy the active Domain | — | Ilocano/Cordillera ogre that sucks up swamp water to trap fishermen, then drowns them |
| 17 | **Kataw** | ★4 | 3 | 6/12 | **Royal Command (Passive):** friendly creatures in adjacent lanes +3 ATK | — | Visayan merfolk said to rule the waters |
| 18 | **Amalanhig** | ★4 | 3 | 9/9 | **Undying (OnDeath):** the first time it dies, return it to its lane with 3 HP | — | Visayan: a failed aswang that rises from its grave |
| 19 | **Kapre** | ★4 | 4 | 9/15 | Guard. **Smoke (Passive):** the enemy creature in its lane −3 ATK | If offered from hand: +3 SP instead of +1 | Tree giant smoking a cigar |
| 20 | **Manananggal** | ★5 | 3 | 9/6 | Flight. **Severed (OnDeath):** leave a 0/6 *Lower Body* token (can't strike) in its lane | — | Self-segmenting flier that leaves its lower half behind |
| 21 | **Sarimanok** | ★5 | 3 | 6/15 | **Good Omen (Passive):** the first time you perform Alay each turn, draw a card | — | Maranao legendary bird and symbol of good fortune |
| 22 | **Manaul** *(replaces Garuda)* | ★5 | 4 | 9/15 | **Splitting the Bamboo (OnSummon):** summon a 3/3 *Malakas* and a 3/3 *Maganda* token in empty lanes on your side | — | Visayan king of birds who pecked open the bamboo that held the first man and woman |
| 23 | **Minokawa** | ★5 | 5 | 12/21 | Flight. **Devour the Moon (OnSummon):** the enemy's patron God loses all Charges | — | Bagobo giant bird that swallows the moon (Maranao: *Arimaonga*) |
| 24 | **Bakunawa** | ★5 | 6 | 18/24 | **Eclipse (OnSummon):** destroy the enemy Building in its lane and deal 6 damage to all enemy creatures | — | Visayan sea serpent that devours the moons |

Notes:
- Legendary (★5) creatures can't be offered from the board (GDD §3.5); they
  can be offered from hand.
- An Alay effect on a creature triggers whether it is offered from hand or board.
- **White Lady → Kaperosa:** this keeps the card's name Filipino.
- **Garuda → Manaul:** Garuda comes from Hindu/Buddhist myth. Its Kapampangan
  counterpart, Galura, appears in only one blog source, so it was dropped.
  Manaul is a well-documented Visayan bird, and its creation myth suits a
  Legendary.

Rarity split: 11 Common · 8 Rare · 5 Legendary.
Cost curve: 1-cost ×4 · 2 ×10 · 3 ×5 · 4 ×3 · 5 ×1 · 6 ×1.

---

## 4. Rituals (8)

In the co-dev list, Kulam and Sumpa ng Tiyanak were identical, and so were
Bulong and Lakas ng Diwata. Each card now has its own effect, and the two you disliked are replaced. "For N turns" = N of
the caster's turns, counting the current turn.

| # | Card | Rarity | Cost | Effect | Alay effect |
|---|---|---|---|---|---|
| 1 | **Kulam** | ★3 | 1 | Deal 6 damage to an enemy creature | — |
| 2 | **Barang** *(replaces Sumpa ng Tiyanak)* | ★3 | 1 | Enemy creature −6 ATK for 2 turns | Deal 3 damage to an enemy creature |
| 3 | **Usog** *(Buyag / Balis)* | ★3 | 1 | Enemy creature −3 ATK permanently and takes 3 damage | Enemy creature −3 ATK this turn |
| 4 | **Pangayaw** *(replaces Lakas ng Diwata)* | ★3 | 1 | Friendly creature +6 ATK for 2 turns | — |
| 5 | **Bulong** | ★3 | 1 | Draw a card. If it's a creature, it costs 1 less this turn | Draw a card |
| 6 | **Hilom** | ★3 | 1 | Restore 12 HP to a friendly creature | Restore 6 HP to your Inheritor |
| 7 | **Agimat** | ★4 | 1 | Friendly creature gains Shield 9 | Friendly creature gains Shield 3 |
| 8 | **Orasyon** | ★4 | 2 | Friendly creature +6 ATK permanently and restore 9 HP | — |

**Folklore basis:**
- **Kulam:** Tagalog witchcraft (*mangkukulam*), often worked through a doll.
- **Barang:** Visayan sorcery in which the *mambabarang* sends swarms of
  insects or beetles. It replaces the generic "curse" card: the swarm saps
  the target's strength.
- **Usog:** a folk illness caused by a stranger's greeting or admiring gaze,
  mostly affecting infants. "Usog" (Tagalog) is the name on the card. The
  flavor text uses the verified regional names: *buyag* (Visayan) and
  *balis* / *nabati* ("was greeted," southern Tagalog), plus the folk cure
  *"Puera usog!"*. *Gahoy* was dropped because no source confirms it.
- **Pangayaw:** the pre-colonial Visayan raid, which warriors prepared for
  with rites and omens. It replaces "Lakas ng Diwata": the card is the
  battle-blessing before a raid.
- **Bulong:** whispered healing or warding incantations used by folk healers.
- **Hilom:** Tagalog for "to heal." Flavor: an *albularyo*'s mending.
- **Agimat:** an amulet (*anting-anting*) said to grant invulnerability.
- **Orasyon:** Latin-derived prayer formulas used to power agimat and
  folk healing.
- Orasyon's cost drops from 3 to 2 because its buff is now permanent.

---

## 5. Domains (9)

### 5.1 Domain rules
- **One shared Domain slot** for the whole battlefield. The Domain belongs to
  whoever played it: "friendly" means the owner's creatures, "enemy" means the
  opponent's.
- **Duration:** N of the owner's turns, counting the turn it's played. When it
  ends or is replaced, all its effects stop immediately.
- **Replacing your own Domain:** always allowed.
- **Affinity triangle:** applies when the active Domain is the opponent's.

```
              Nature
           ▲            │
   beats   │            │ beats
           │            ▼
       Spirit ◄── Human
                  beats
```
| Matchup vs. active enemy Domain | Result |
|---|---|
| Yours **beats** theirs | Replaces it |
| **Same** affinity | Replaces it, but costs +1 SP |
| Yours **loses** to theirs | Can't be played until theirs ends |

**Names:** cards show the English names (Nature, Human, Spirit). The Filipino
words *Kalikasan*, *Bayan* and *Kaluluwa* appear only as flavor.

**In the lore:** nature reclaims human places, human faith and walls ward off
spirits, and spirits haunt the wild.

### 5.2 Domain list

| # | Card | Affinity | Rarity | Cost | Duration | Effect | Alay effect |
|---|---|---|---|---|---|---|---|
| 1 | **Cursed Village** | Human | ★3 | 3 | 2 | At the start of the opponent's turn, deal 3 damage to each enemy creature | — |
| 2 | **Intramuros** | Human | ★3 | 2 | 2 | Friendly creatures take 3 less damage from strikes | Friendly Building +6 HP |
| 3 | **Baybayin** | Human | ★3 | 3 | 2 | On play: friendly creatures gain Shield 6 | — |
| 4 | **Enchanted Balete** | Nature | ★3 | 3 | 3 | At the start of your turn, restore 3 HP to each friendly creature | Restore 6 HP to a friendly creature |
| 5 | **Moonlit Forest** | Nature | ★3 | 3 | 2 | Friendly creatures +3 ATK | — |
| 6 | **Sagradong Ilog** | Nature | ★4 | 2 | 2 | On play: restore 6 HP to all friendly creatures. While active: friendly creatures ignore enemy ATK reductions | — |
| 7 | **Sulad** *(replaces Bloodied Terraces)* | Spirit | ★4 | 3 | 2 | Friendly creatures +6 ATK; they lose 3 HP at the start of your turn | — |
| 8 | **Sementeryo** | Spirit | ★4 | 3 | 2 | Enemy creatures −3 ATK and −3 max HP. Warning on the card: any enemy at 3 HP or less dies, including tokens (Malakas, Maganda) and a revived Amalanhig | — |
| 9 | **Biringan** *(was Realm of Engkanto)* | Spirit | ★5 | 4 | 2 | Friendly creatures +3 ATK and +3 max HP | — |

- Sagradong Ilog was "Immediate" in the co-dev list. A Domain has to occupy
  the slot, so it now lasts 2 turns.

**Folklore basis and changes:**
- **Enchanted Balete:** the balete tree, home of kapre, tikbalang and
  engkanto, and Philippine folklore's classic haunted tree.
- **Baybayin:** the pre-colonial script. It moves to **Human** (it's a human
  craft), and its shield reads as protective writing on agimat.
- **Biringan** *(renamed from Realm of Engkanto)*: the invisible engkanto
  city of Samar, which is the folklore name for the engkanto realm.
- **Sulad** *(replaces Bloodied Terraces)*: the Visayan underworld where
  souls suffer before moving on, which suits a "power at the cost of HP" effect.
  The rice terraces are a living Ifugao heritage site, so a "bloodied"
  version risked offending the community.
- **Sementeryo, Intramuros, Cursed Village, Moonlit Forest, Sagradong Ilog:**
  generic or historical places. They fit the setting as they are.
- Each affinity has exactly 3 Domains, and each starter deck can reach at
  least one affinity.

---

## 6. Buildings (3)

Buildings have HP but no ATK, sit in a lane's Building slot, can't be offered, and **expire after their Duration** (GDD §3.7a). Duration counts the owner's turns, including the turn it is played.

| # | Card | Rarity | Cost | HP | Duration | Effect |
|---|---|---|---|---|---|---|
| 1 | **Bantayog** | ★3 | 2 | 15 | 3 | Intercepts every enemy strike in its lane, Flight included: the strike hits Bantayog, and your creature there takes no damage and doesn't retaliate. Bakunawa's Eclipse and direct damage can still destroy it. |
| 2 | **Torogan** *(replaces Sagrado)* | ★4 | 2 | 12 | 2 | All friendly creatures +3 ATK while it stands |
| 3 | **Dambana** *(new)* | ★4 | 2 | 12 | 3 | Your first Alay each turn gives +1 extra SP (one Alay bonus at a time; see GDD §3.5) |

**Folklore basis:**
- **Bantayog:** a raised marker or monument. Flavor: a guardian post that
  takes blows meant for others.
- **Torogan:** the Maranao royal house of a datu or sultan, carved with
  *panolong* and *okir* serpents. Its rallying aura replaces the generic
  "Sagrado."
- **Dambana:** a shrine or altar for offerings, and the Building that pays
  off Alay. Together the three Buildings cover defense (Bantayog), offense
  (Torogan) and Alay (Dambana).

---

## 7. Patron Gods (11) — Divine ✦

The co-dev list gave the Gods cost, HP and ATK. As patrons they never enter
the board, so those stats are dropped.

**Rules:**
- Pick 1 patron per deck.
- **Charges:** +1 at the start of each of your turns, +1 from your first Alay each turn, max 3.
- **Active:** costs 3 Charges, usable once per turn.
- **Passive:** always on.
- Path cosmetics (Separation/Control) change only VFX and voice lines.

| # | God | Origin (verified) | Role | Passive | Active (3 Charges) | Access |
|---|---|---|---|---|---|---|
| 1 | **Adlaw** | Visayan sun god, "worshiped by the good" | Offense | Creatures with Swift get +3 ATK on the turn they're summoned | **Sinag** (Ray): deal 9 damage to an enemy creature | **Starter** |
| 2 | **Aman Sinaya** | Tagalog patron of fishermen and inventor of fishing gear (Jocano later made her the primordial sea goddess) | Defense | Your creatures with Guard have +3 HP | **Lambat** (Net): an enemy creature can't strike on its next turn | **Starter** |
| 3 | **Bulan** | Moon god: the merry, mischievous moon of the Pangasinan pantheon (Visayan *bulan* = moon) | Utility | At the start of your turn, if you hold 2 or fewer cards, draw 1 | Draw 2 cards | **Starter** |
| 4 | **Bathala** | Tagalog supreme being and creator | Board | The first creature you summon each turn gets +3/+3 (up to 3 times per match) | All friendly creatures +3 ATK permanently | Tawag / craft |
| 5 | **Sidapa** *(replaces Hukluban)* | Visayan god of death on Mt. Madjaas, who decides each person's lifespan | Control | Whenever you offer a creature via Alay, deal 3 damage to an enemy creature | Destroy an enemy creature with 6 or less HP | Tawag / craft |
| 6 | **Akasi** | Sambal god of health and sickness; offerings were made to him for the sick | Sustain | Whenever you restore HP, restore 3 more | Restore 15 HP to your Inheritor | Tawag / craft |
| 7 | **Anitun Tabu** | Goddess of wind and rain: Tagalog (Jocano), and in the Sambal pantheon as *Anitun Tauo*, demoted by Malayari for her conceit | Tempo | Your creatures with Flight +3 ATK | Swap the lanes of two creatures on the same side | Tawag / craft |
| 8 | **Apung Malyari** | Kapampangan moon god of Mt. Pinatubo, ruler of the eight rivers | Domain | Your Domains cost 1 less | Destroy the active Domain; if it was the opponent's, draw a card | Tawag / craft |
| 9 | **Amanikable** | Tagalog patron of hunters (1613 record); in Jocano, the ill-tempered ruler of the sea | Punish | When an enemy creature strikes one of yours, deal 3 damage back to it | **Pangangaso** (The Hunt): deal 6 damage to an enemy creature; if it dies, draw a card | Tawag / craft |
| 10 | **Agueo** | Pangasinan sun god: morose, taciturn and obedient to his father Ama | Alay engine | Creatures you offer from the board give +1 extra SP (one Alay bonus at a time; the +3 per turn cap still applies) | Your next card this turn costs 2 less | Tawag / craft |
| 11 | **Lalahon** *(replaces Agui)* | Visayan goddess of fire, volcanoes and the harvest, dwelling in Mt. Kanlaon | Burn | Alay effects you trigger that deal damage deal +3 | **Pagsabog** (Eruption): deal 6 damage to the enemy creature and Building in one lane | Tawag / craft |

**Verification and changes:**
- **Agueo is confirmed:** he's the Pangasinan sun god, brother of Bulan, so
  the two form a sun-and-moon pair. His obedience to Ama inspires the
  disciplined Alay-engine role.
- **Agui → Lalahon:** the only source for Agui is a fan site, and its own text
  admits there are few early records. Lalahon is the well-attested Visayan
  fire goddess and keeps the fire role.
- **Hukluban → Sidapa:** the early sources (Plasencia, 1589) describe Hukluban
  as a kind of witch or priest, one of Sitan's agents, not a god. Sidapa is a
  documented death god. Hukluban could come back later as a Legendary
  creature card.
- **Aman Sinaya and Amanikable:** their descriptions were corrected to
  separate the earliest records from Jocano's 20th-century pantheon. Both
  versions are allowed in flavor text.
- **Pantheons represented:** Tagalog, Visayan, Pangasinan, Kapampangan and
  Sambal.
- **Starter balance:** the three free Gods must win about as often as the
  pulled ones in simulation (target within ±3%).
- **Test together:** Sidapa, Agueo, Sarimanok and Dambana all reward Alay.

---

## 8. Starter collection

Every new account gets both decks below, plus the 3 starter Gods.

### Deck A — *Mga Mangangaso ng Gabi* (Night Hunters) · aggro · patron **Adlaw**
| Card | × |
|---|---|
| Aswang | 2 |
| Tiktik | 2 |
| Tiyanak | 2 |
| Wakwak | 2 |
| Santelmo | 2 |
| Shokoy | 2 |
| Bungisngis | 1 |
| Batibat ★4 | 1 |
| Amalanhig ★4 | 1 |
| Kulam | 2 |
| Pangayaw | 2 |
| Moonlit Forest | 1 |
| **Total** | **20** |

### Deck B — *Mga Bantay ng Gubat* (Forest Guardians) · defense · patron **Aman Sinaya**
| Card | × |
|---|---|
| Diwata | 2 |
| Duwende | 2 |
| Kaperosa | 2 |
| Pah | 2 |
| Bungisngis | 1 |
| Nuno sa Punso ★4 | 1 |
| Kapre ★4 | 1 |
| Hilom | 2 |
| Bulong | 2 |
| Bantayog | 2 |
| Enchanted Balete | 1 |
| Intramuros | 1 |
| Baybayin | 1 |
| **Total** | **20** |

Bulan (the utility God) fits either deck.

**What starters don't include** (obtained through pulls, the shop, crafting or story):
- Commons: Barang, Usog, Cursed Village
- All other Rares, and every Legendary
- 8 of the 11 Gods

---

## 9. How cards are obtained

| Source | Commons ★3 | Rares ★4 | Legendaries ★5 | Gods ✦ |
|---|---|---|---|---|
| Starter decks | Most | 4 | — | 3 |
| **Tawag** banner pulls | ✓ | ✓ | ✓ | ✓ (rarest) |
| Craft with Abo | ✓ | ✓ | ✓ | ✓ (high cost) |
| Ginto shop rotation (daily) | ✓ | ✓ | Weekly slot | ✗ |
| Story and quest rewards | ✓ | ✓ | Select cards | Story milestone (optional) |
| Direct buy with Mutya | ✓ | ✓ | ✓ | ✗ |

Gods can't be bought directly, which keeps them the rarest tier. They come only
from pulls, crafting, or story milestones.

### 9.1 Tawag (gacha) **[tune]**
- **One pull = one item** (a card or a God).
- **Banners:**
  - *Featured banner:* rotates every ~3 weeks and headlines one God at boosted odds.
  - *Standard banner:* permanent, with all cards and all released Gods.
- **Base rates:**

| Tier | Rate |
|---|---|
| Divine ✦ (God) | 0.6% |
| Legendary ★5 | 3.4% |
| Rare ★4 | 16% |
| Common ★3 | 80% |

- **Pity:**
  - Every 10 pulls guarantees a Rare or better.
  - Every 30 pulls guarantees a Legendary or better.
  - Gods: soft pity starts at pull 70, and pull 90 is a guaranteed God.
  - Featured banner: if the God you get isn't the featured one, the next God
    is guaranteed to be featured.
  - Pity counters carry over between banners of the same type.
- **Cosmetic variants:** any card pull has a 5% chance to come as an
  alternate-art or animated variant. Stats are identical.
- **Price:** 100 Mutya per pull; a 10-pull costs 1,000 Mutya.

### 9.2 Abo (duplicates and crafting) **[tune]**
| Tier | Abo from a duplicate | Craft cost |
|---|---|---|
| Common | 10 | 40 |
| Rare | 40 | 100 |
| Legendary | 150 | 400 |
| God | 500 | 2,000 |

- A duplicate is any copy beyond the deck max: 3rd+ Common or Rare, 2nd+
  Legendary, 2nd+ God.
- A duplicate variant keeps the variant unlocked; the copy still converts to Abo.

---

## 10. Open items for co-devs

No open items. The card set is final for v1.

## 11. Lore verification sources

Philippine mythology has many regional versions. Where sources disagree, the
card notes the earliest record and the later (Jocano) version. Most online
sources are secondary, so check against F. Landa Jocano's *Philippine
Mythology* and Spanish-era records (Plasencia 1589, Buenaventura 1613) before
final card text.

- [List of Philippine mythological figures (Wikipedia)](https://en.wikipedia.org/wiki/List_of_Philippine_mythological_figures): Agueo, Bulan, Adlaw, Bathala, Amansinaya, Amanicable, Anitun Tauo, Akasi, Hukluban
- [List of Philippine mythological creatures (Wikipedia)](https://www.wikipedia.org/wiki/Philippine_mythical_creatures): Amalanhig, Batibat, Siyokoy, Minokawa
- [The Aswang Project: Amanikable](https://aswangproject.com/amanikable) · [Sambal mythology](https://aswangproject.com/sambal-mythology) · [Kapampangan mythology](https://aswangproject.com/kapampangan-mythology)
- [Pah (Philippine Spirits)](https://phspirits.com/pah/) · [Cole, *Philippine Folk Tales* (sacred-texts)](https://sacred-texts.com/asia/pft/pft47.htm)
- [Kaperosa (UP folklore page)](https://sites.google.com/up.edu.ph/kaperosa-fresnillo-folklore)
- [Witchcraft in the Philippines (Kulam, Barang)](https://Www.wikipedia.org/wiki/Kulam) · [Agimat](https://en.wikipedia.com/wiki/Agimat)
- [Usog (Wikipedia)](https://en.wikipedia.com/wiki/Usog) · [Bales / nabati (StuartXchange)](https://stuartxchange.com/Bales): Usog, buyag, balis
- Unverified, which is why these were replaced or dropped: [Agui (mythlok)](https://mythlok.com/agui), the single-blog mention of Galura, and *gahoy* (no source found)

## 12. Balance pass 1 (2026-11-16)

| Change | Why |
|---|---|
| Alay gives at most +3 SP per turn; only one Alay bonus applies per offering (GDD §3.5) | Stopped Duwende/Dambana/Agueo/Kapre combos from playing 6-cost cards on turn 2 |
| Only the first Alay each turn gives a God Charge | Keeps God Actives at 3–4 uses per match |
| Flight capped at 4 cards: Sarimanok (gained +3 HP) and Manaul lost Flight | Flight was getting around every defense |
| Bantayog intercepts all strikes in its lane, Flight included | Gives defensive decks a real answer to fliers |
| Lalahon's Active changed to single-lane damage to a creature and a Building | The old version swept every low-HP creature and wiped out the aggro starter deck |
| Bathala's Passive limited to 3 times per match | It was the strongest Passive, which risked a pay-to-win perception |
| Batibat's Alay effect changed to an ATK reduction this turn | A full silence was too strong for a single offered card |
| Sementeryo gets a warning about dying low-HP enemies | Players shouldn't lose tokens or a revived Amalanhig by surprise |
| Abo from duplicates raised (10 / 40 / 150 / 500) | Free players needed a realistic path to crafting Gods |
| **Scale ×3: Inheritor 30 → 100 HP** (pass 2) | Bigger numbers make hits feel more impactful. Every combat number (ATK, HP, damage, healing, Shield, ATK changes) was multiplied by 3; SP, costs, Charges and draw are unchanged. Inheritor HP is ×3.3, so matches run about 10% longer, which Boundary Collapse absorbs. |

## 13. Balance pass 3 (2026-11-16)

| Change | Why |
|---|---|
| Buildings get a Duration: Bantayog 3, Torogan 2, Dambana 3 | Buildings couldn't be removed while a creature stood in front of them. Torogan's +3 ATK to all creatures now lasts at most 2 turns. |
| You can play a new Building over your own | Lets you refresh a Building or swap it for another |
| Moonlit Forest loses "creatures summoned gain Swift" | Stops turn-2 bursts like Moonlit + Bungisngis (15 Pierce damage on arrival) |
| Damage-reduction cap of −3 per hit (GDD §3.11) | Guard + Intramuros made 3-ATK creatures deal 0 |
| Boundary Collapse hits both Inheritors at once at the start of each round | The first player was always hit first |
| Pierce doesn't carry past Buildings | Keeps Buildings meaningful as protection |
| Tokens can be offered for 0 SP | Tokens were free Alay fuel through the "min 1 SP" rule |
| Bulong's Alay effect changed to "Draw a card" | Its +1 Charge broke the 2-Charges-per-turn limit |
