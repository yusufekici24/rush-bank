# PR: Playable Prototype Systems and Updated Documentation

## Summary

This PR expands RushBank from a Unity project skeleton into a broad script-level playable prototype foundation. It adds the main customer queue loop, mobile controls, banking transactions, specialist desk referrals, crisis events, support systems, pre-run boosters, daily quests, achievements, and updated product documentation.

The implementation is intentionally modular: most systems expose serialized fields and UnityEvents so they can be wired in the Unity Editor once the prototype scene is generated.

## Main Gameplay Loop

- Added queue-driven customer flow with `QueueManager` and `QueueCustomer`.
- Added request types, patience tracking, age-based patience multipliers, request icons, and queue relief hooks.
- Added dynamic quest spawning with day/level unlocks, weighted task pools, max queue capacity checks, thief-event spawn pause, and critical-time quick-win pacing.
- Added score, combo, run gold, target gold, and gold multiplier support.

## Player and Interaction

- Added mobile-focused controller support with joystick movement, grab/deposit/action flow, and HoldPoint item handling.
- Added chubby physics/top-down movement scripts for slightly delayed, inertia-based movement.
- Added interactable pick-up, hold, throw, and delivery-point mechanics.

## Transaction Systems

- Added quick tasks through `FastTrackActionSystem`.
- Added utility bill processing for Electricity, Water, and Telephone bill types.
- Added card block mini-game with color sequence input.
- Added wire transfer mini-game with alphanumeric code entry and Perfect Transfer gold boost.
- Added mobile banking SMS activation mini-game with 4-digit code verification and Digital Boost.
- Added banking actions for Withdraw, Deposit, and Currency Exchange.
- Added document workflow for application form, signature, manager approval, and delivery.
- Added gold exchange workflow with appraisal station and receipt delivery.
- Added cash delivery system with limited vault capacity, armored van dispatch, Super Cash Bag, and vault refill reward.

## Specialist Desk and Referral Systems

- Added `AccountOpeningSystem` for stamp-based account opening redirection to Relationship Manager.
- Added `InsuranceReferralSystem` for insurance specialist redirection and Teamwork Speed Boost.
- Added `CreditApplicationSystem` for Housing, Vehicle, and Consumer credit applications with 80/20 approval checks, denial handling, Credit Specialist routing, and credit reward scaling.
- Updated stationery support so Relationship, Insurance, and Credit desks can share redirect speed/efficiency mechanics.
- Added `RedAlertRedirectionSystem` for emergency high-anger customers, priority queue pulling, emergency redirect, Mega Cash reward, and VIP Relief.

## Crisis and Support Events

- Added `CounterIncidentManager` and `SecurityGuardAI` for counter meltdown, angry customer escort, gold penalty, and Panic Attack debuff.
- Added scammer detection with document inspection, discrepancy checks, approve/decline/security outcomes, audit failure, and Hero Employee boost.
- Added thief/police and heist-style raid systems with time freeze and stealth-style alarm interaction.
- Added phone interruption quick-reaction mechanic.
- Added staff interruption workflow for urgent office document delivery.
- Added Manager IT Support event with Blue Screen, Loose Cable, and Overheating Fan mini-games.
- Added Bank Cat Chaos with calming phase, panic phase, call security button, chase sequence, and cleanup.
- Added Tea Lady systems for caffeine boost and Tea Hospitality mode.
- Added Lazy Assistant and Assistant Manager, including snack-feeding support for extra assistant capacity at the cost of slower work.

## Meta-Game, Boosters, and Progression

- Added pre-run shop manager using PlayerPrefs for gold and booster inventory.
- Added Time Slow, Speed, and Anti-Grumpiness booster application.
- Added pre-level booster popup behavior, quick buy/equip flow, best-seller ribbon support, and 3-in-1 bundle purchase logic.
- Added branch difficulty settings for Tasra, Sehir, and Metropol branches.
- Added tutorial manager for the Soft Opening tutorial branch.
- Added `QuestAndAchievementManager` with daily quests, long-term achievements, PlayerPrefs persistence, gold rewards, notification banner feedback, and temporary/permanent passive boosts.
- Added Manager Satisfaction system with Staff Feast reward and permanent feast duration achievement integration.

## UI and Visual Direction

- Updated HUD/menu-related controller support for request icons, call-customer cooldown, timers, and feedback.
- Added chubby toon shader guide and URP-friendly stylized rendering notes.
- Updated README to reflect current architecture, gameplay loops, systems, setup flow, and PR validation notes.
- Updated product brief and backlog with the latest gameplay systems.

## Validation

- Ran `git diff --check`.
- Searched for merge conflict markers in changed gameplay/docs files.
- Checked important event/property references locally with `rg`.

## Not Tested

- Unity Editor compile test was not run in this environment.
- Play mode validation was not run in this environment.
- Android build validation was not run in this environment.

Recommended follow-up in Unity:

1. Open the project with Unity `6000.0.23f1`.
2. Run `RushBank > Setup Prototype Scenes`.
3. Open `Boot` and run Play Mode.
4. Check Console for compile issues.
5. Wire scene references for new managers and specialist desks.
6. Verify core loop with a narrow MVP pass: movement, queue, timer, passbook, cash withdraw/deposit, vault flow.
