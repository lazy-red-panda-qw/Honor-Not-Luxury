# Honor, Not Luxury 2.0

[简体中文](README.md)

I'm DingDong, the author of this mod. I wanted to keep royal titles and their benefits without requiring my colonists to live in luxury. I looked for an existing mod that closely matched this idea and did not find one, so I developed Honor, Not Luxury for my own colony. I'm sharing it for players who want the same choice. It requires **RimWorld 1.6, Royalty, and Harmony**. The Oberonia Aurea (OA) mods are optional; their titles use the same vanilla `RoyalTitleDef` requirement paths covered here.

The idea, feature choices, and scope are mine; **AI wrote the mod's code**. I reviewed the code, tested it in game, and maintain the project. As of 2026-09-27, the mod loads in my **140+ mod setup**, its settings are visible, and I have verified that the bedroom, throne room, apparel style and quality, food, and expectation options can be changed independently. Setting a requirement back to **Unchanged** makes the corresponding title mood penalty reappear. I have not observed new errors attributable to this mod in that setup. Other, unrelated warnings and errors exist in the overall game log. This is not a claim of universal compatibility.

## Settings

Open **Options → Mod settings → Honor, Not Luxury**. Settings apply to every save.

| Requirement | Default | What the exemption changes |
|---|---|---|
| Bedroom | Permanent colonists | Title bedroom assignment, room quality and related mood penalties |
| Daily throne room | Permanent colonists | Daily throne and room demands and related mood penalties |
| Apparel style | Permanent colonists | Mood penalty for not wearing title-specific apparel |
| Apparel quality | Permanent colonists | Mood penalty for apparel below the title's minimum quality |
| Food | Permanent colonists | Title food refusal, new inappropriate-food memories, and the related food alert |
| Minimum expectation | Permanent colonists | The expectation floor raised by a royal title |
| Work restrictions | Unchanged | Optional removal of restrictions caused by the title |

Each row has three scopes:

- **Unchanged:** Let vanilla and other mods decide this requirement.
- **Permanent colonists:** Exempt player-faction humanlike colonists, including those travelling or having a mental break. Quest lodgers, visitors, prisoners, slaves, and subhuman mutants are excluded.
- **Everyone:** Exempt every pawn with a Royalty tracker where the original rule would otherwise apply. This does not create new demands for pawns that never had them.

The settings do not remove titles, honor, permits, psycasts, inheritance, diplomacy, or trade. **Bestowing ceremonies still require the throne room and room specifications imposed by their quests.** Wealth and ideology-role expectations, ordinary room and food rules, non-title work restrictions, decrees, noble recreation rules, and NPC apparel generation remain governed by the original game and other installed mods.

## Install, add mid-save, and remove

Place the `HonorNotLuxury` folder in RimWorld's `Mods` directory. Enable Harmony, Royalty, and this mod. Load this mod after OA and other mods that modify title requirements. You may use an existing save.

To remove it, disable the mod and restart the game before loading the save. Version 2.0 does not persist exemption flags on pawns or change the title Defs, so the original requirements participate again once its patches are absent. I have verified in game that switching a setting back to **Unchanged** restores the corresponding mood penalty. A complete disable/restart/reload cycle should still be tried on a copy of your save, especially if other royal-title mods are installed.

Settings changes refresh work and situational mood caches. Existing inappropriate-food memories expire normally. Newly enabled work may need manual priority assignment. Disabling the mod cannot reconstruct a work schedule you changed while it was enabled.

**Legacy prototype exception:** The original 1.x prototype wrote some vanilla requirement flags to saves. Version 2.0 does not guess their original values or automatically repair those saves. If you saved while 1.x was active, use a backup from before that prototype or inspect the affected pawn's save data. Saves used only with 2.0 do not receive those persistent changes.

The project archive also includes `install.ps1` for Windows. Run it from the extracted project folder with `-WhatIfOnly` for a preview; without that switch it copies the runtime mod, makes backups, and adds the package ID to `ModsConfig.xml`. It preserves an existing `About/PublishedFileId.txt` when updating a local Workshop-linked installation.

## Compatibility, issues, and maintenance

I intend to keep maintaining this mod, guided by my personal play setup and available time. Compatibility with every mod combination and immediate fixes are not guaranteed. Mods that replace title requirements, mood, work restrictions, or bestowing logic deserve particular testing.

Please report reproducible compatibility problems in [GitHub Issues](https://github.com/lazy-red-panda-qw/Honor-Not-Luxury/issues). Include the RimWorld and mod versions, relevant mod list and load order, all seven setting values, reproduction steps, and the resulting `Player.log` or HugsLib log. State whether the affected pawn is a permanent colonist, visitor, or another kind of pawn, and whether the problem remains with the relevant setting on **Unchanged**. Avoid posting private save data publicly.

Suggestions, forks, and Continued versions are welcome. Original source and documentation in this project are released under the [MIT License](LICENSE). Preserve its copyright and license notice in copies and substantial derivatives. RimWorld, Harmony, OA, and other third-party mods remain under their respective owners' terms and are not relicensed here.

## Validation and source

The [controlled QA report](Docs/QA_V2_REPORT.md) was written on 2026-09-24, before the later in-game validation described above. It covers 118 behavior checks and isolated installer tests against the installed RimWorld assemblies. It does not establish that every title, guest, OA permit, full bestowing quest, or interaction in a 140+ mod list works correctly. The Chinese README contains a fuller validation checklist and implementation details.

Source is in `HonorNotLuxury/Source`. The project targets `net472`; it references installed RimWorld and Harmony assemblies for compilation but does not bundle them. From the project root:

```powershell
dotnet build .\HonorNotLuxury\Source -c Release
```

The generated DLL is copied into `HonorNotLuxury/Assemblies`. Do not enable the original 1.x prototype DLL alongside version 2.0.

From the project root, `powershell -NoProfile -ExecutionPolicy Bypass -File .\package.ps1` creates a checked release archive. See `PUBLISHING.md` for the steps remaining before a public repository or Workshop upload.
