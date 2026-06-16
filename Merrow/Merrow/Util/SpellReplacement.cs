using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Merrow.Util
{
    public enum SpellReplacementLogic
    {
        None = 0, 
        RandomSpell = 1,
        RandomBuff = 2,
        RandomDebuff = 3,
        RandomStatusSpell = 4,
        RandomDamageSpell = 5,
        SpecificSpell = 6,
    }

    public enum SpellReplacementPreset
    {
        None = 0,
        Recommended = 1,
        ReplaceSimilar = 2,
        Custom = 3,
    }

    public struct SpellReplacementEntry
    {
        public SpellNameEnum spellBeingAdded;
        public SpellReplacementLogic logic;
        public SpellNameEnum spellBeingReplaced;

        public int GetSortPriority()
        {
            switch (this.logic)
            {
                case SpellReplacementLogic.SpecificSpell: return 0;
                case SpellReplacementLogic.RandomBuff: return 1;
                case SpellReplacementLogic.RandomDebuff: return 2;
                case SpellReplacementLogic.RandomStatusSpell: return 3;
                case SpellReplacementLogic.RandomDamageSpell: return 4;
                case SpellReplacementLogic.RandomSpell: return 5;
                
                default: 
                    return int.MinValue;
            };
        }
    }

    public class BossSpellReplacementWorkflow
    {
        public event Action<BossSpellReplacementWorkflow, SpellReplacementPreset> onPresetApplied;

        private Dictionary<SpellNameEnum, SpellReplacementLogic> logicMapping;
        private Dictionary<SpellNameEnum, SpellNameEnum> explicitReplacementMapping;
        private Dictionary<SpellNameEnum, bool> enabledMapping;

        private List<SpellNameEnum> availableReplacementCache;
        private List<SpellReplacementEntry> entryResultCache;

        private const int BOSS_SPELL_INDEX_START = 60;
        private const int BOSS_SPELL_INDEX_END = 76;

        public BossSpellReplacementWorkflow()
        {
            this.logicMapping = new Dictionary<SpellNameEnum, SpellReplacementLogic>();
            this.explicitReplacementMapping = new Dictionary<SpellNameEnum, SpellNameEnum>();
            this.enabledMapping = new Dictionary<SpellNameEnum, bool>();

            this.availableReplacementCache = new List<SpellNameEnum>();
            this.entryResultCache = new List<SpellReplacementEntry>();
        }

        public bool IsBeingAdded(SpellNameEnum spellName)
        {
            if (this.enabledMapping.ContainsKey(spellName))
            {
                return true;
            }
            return false;
        }

        public void SetSpellUsageActive(SpellNameEnum spellName, bool isActive)
        {
            if (this.enabledMapping.ContainsKey(spellName))
            {
                this.enabledMapping[spellName] = isActive;
                return;
            }

            this.enabledMapping.Add(spellName, isActive);
        }

        public List<SpellNameEnum> GetSpellsAvailableAsReplacements()
        {
            var availableSpells = this.GetAllPlayerSpellEnums();

            for (int k=0; k<availableSpells.Count; k++)
            {
                var spell = availableSpells[k];
                var alreadyMapped = this.explicitReplacementMapping.ContainsValue(spell);
                if (alreadyMapped)
                {
                    availableSpells.RemoveAt(k--);
                    continue;
                }
            }

            return availableSpells;
        }

        private List<SpellNameEnum> GetAllPlayerSpellEnums()
        {
            this.availableReplacementCache.Clear();
            for (int k=0; k<BOSS_SPELL_INDEX_START; k++)
            {
                this.availableReplacementCache.Add((SpellNameEnum)k);
            }
            return this.availableReplacementCache;
        }

        public List<SpellReplacementEntry> GetPlannedSpellReplacementData()
        {
            this.entryResultCache.Clear();

            for (int k = BOSS_SPELL_INDEX_START; k <= BOSS_SPELL_INDEX_END; k++)
            {
                var spellEnum = (SpellNameEnum)k;
                
                if (this.logicMapping.ContainsKey(spellEnum) == false)
                {
                    this.entryResultCache.Add(new SpellReplacementEntry
                    {
                        logic = SpellReplacementLogic.None,
                        spellBeingAdded = spellEnum
                    });
                    continue;
                }

                var logic = this.logicMapping[spellEnum];
                var replacementData = new SpellReplacementEntry
                {
                    spellBeingAdded = spellEnum,
                    logic = logic,
                };

                var hasExplicitReplacement = this.explicitReplacementMapping.ContainsKey(spellEnum);
                if (hasExplicitReplacement)
                    replacementData.spellBeingReplaced = this.explicitReplacementMapping[spellEnum];

                this.entryResultCache.Add(replacementData);
            }

            return this.entryResultCache;
        }

        public List<SpellReplacementEntry> GetVerbatimSpellReplacementOperations()
        {
            this.entryResultCache.Clear();

            var availableReplacementSpells = this.GetAllPlayerSpellEnums();
            var selectedReplacements = this.logicMapping.Values;

            foreach (var replacementPair in this.logicMapping)
            {
                var spellToAdd = replacementPair.Key;
                var replacementLogic = replacementPair.Value;

                if (replacementLogic == SpellReplacementLogic.None)
                    continue;

                

                this.entryResultCache.Add(new SpellReplacementEntry
                {
                    spellBeingAdded = spellToAdd,
                    logic = replacementLogic
                });
            }

            // Sort things by priority, this will help ensure that narrower rules 
            this.entryResultCache.Sort((a, b) =>
            {
                var weightA = a.GetSortPriority();
                var weightB = b.GetSortPriority();

                return weightA.CompareTo(weightB);
            });

            for (int k=0; k<this.entryResultCache.Count; k++)
            {
                var entry = this.entryResultCache[k];

                var foundReplacement = this.TryGetReplacementSpell(entry.spellBeingAdded, entry.logic, availableReplacementSpells, out var replacementSpell);
                if (foundReplacement)
                {
                    entry.spellBeingReplaced = replacementSpell;

                    Console.WriteLine($"[Boss Spells] Replacing {replacementSpell} with {entry.spellBeingAdded}");
                }
                else
                {
                    throw (new Exception(($"ERROR -- Could not find replacement spell with current setup:{entry.spellBeingReplaced} -> {entry.logic}")));
                }

                this.entryResultCache[k] = entry;
            }

            return this.entryResultCache;
        }

        private bool TryGetReplacementSpell(SpellNameEnum spellBeingAdded, SpellReplacementLogic logic, List<SpellNameEnum> avilableReplacements, out SpellNameEnum chosenReplacement)
        {
            switch (logic)
            {
                default:
                case SpellReplacementLogic.RandomSpell:
                    return this.InternalTryGetReplacementSpell(avilableReplacements, SpellDefinitions.AllBaseSpells, out chosenReplacement);

                case SpellReplacementLogic.RandomDamageSpell:
                    return this.InternalTryGetReplacementSpell(avilableReplacements, SpellDefinitions.OffenseSpells, out chosenReplacement);

                case SpellReplacementLogic.RandomStatusSpell:
                    return this.InternalTryGetReplacementSpell(avilableReplacements, SpellDefinitions.StatusSpells, out chosenReplacement);

                case SpellReplacementLogic.RandomBuff:
                    return this.InternalTryGetReplacementSpell(avilableReplacements, SpellDefinitions.BuffSpells, out chosenReplacement);

                case SpellReplacementLogic.RandomDebuff:
                    return this.InternalTryGetReplacementSpell(avilableReplacements, SpellDefinitions.DebuffSpells, out chosenReplacement);

                case SpellReplacementLogic.SpecificSpell:
                    return this.TryGetExplicitPairing(spellBeingAdded, avilableReplacements, out chosenReplacement);
            }
        }

        private bool TryGetExplicitPairing(SpellNameEnum toReplace, List<SpellNameEnum> availableReplacements, out SpellNameEnum chosenReplacement)
        {
            if (this.explicitReplacementMapping.ContainsKey(toReplace))
            {
                chosenReplacement = this.explicitReplacementMapping[toReplace];
                return true;
            }

            chosenReplacement = default;
            return false;
        }

        private List<SpellNameEnum> randomCache = new List<SpellNameEnum>();
        private bool InternalTryGetReplacementSpell(List<SpellNameEnum> availableReplacements, SpellNameEnum[] selectionPool, out SpellNameEnum chosenReplacement, bool isFallback = false)
        {
            this.randomCache.Clear();
            this.randomCache.AddRange(selectionPool);
            this.randomCache.Shuffle();

            for (int k = 0; k < this.randomCache.Count; k++)
            {
                var possibleReplacement = this.randomCache[k];
                var isAvailable = availableReplacements.Contains(possibleReplacement);
                if (isAvailable)
                {
                    chosenReplacement = possibleReplacement;
                    availableReplacements.Remove(possibleReplacement);
                    return true;
                }

            }

            if (isFallback == false)
            {
                return this.InternalTryGetReplacementSpell(availableReplacements, SpellDefinitions.OffenseSpells, out chosenReplacement, isFallback: true);
            }

            chosenReplacement = default;
            return false;
        }

        public void ClearReplacementLogic(SpellNameEnum spellToClear)
        {
            if (this.logicMapping.ContainsKey(spellToClear))
                this.logicMapping.Remove(spellToClear);

            if (this.explicitReplacementMapping.ContainsKey(spellToClear))
                this.explicitReplacementMapping.Remove(spellToClear);
        }

        public void SetReplacementLogic(SpellNameEnum spell, SpellReplacementLogic logic)
        {
            if (this.logicMapping.ContainsKey(spell) == false)
            {
                this.logicMapping.Add(spell, logic);
                return;
            }

            this.logicMapping[spell] = logic;
        }

        public void SetExplicitReplacementMapping(SpellNameEnum spellToAdd, SpellNameEnum spellToReplace)
        {
            this.SetReplacementLogic(spellToAdd, SpellReplacementLogic.SpecificSpell);

            if (this.explicitReplacementMapping.ContainsKey(spellToAdd) == false)
                this.explicitReplacementMapping.Add(spellToAdd, spellToReplace);
            else
                this.explicitReplacementMapping[spellToAdd] = spellToReplace;
        }

        public void ApplyPreset(SpellReplacementPreset replacementPreset)
        {
            switch (replacementPreset)
            {
                default:
                case SpellReplacementPreset.None:
                    this.ApplyNotUsedPreset();
                    break;

                case SpellReplacementPreset.Recommended:
                    this.ApplyRecommendedPreset();
                    break;

                case SpellReplacementPreset.ReplaceSimilar:
                    this.ApplyReplaceSimilarPreset();
                    break;

                case SpellReplacementPreset.Custom:
                    this.ApplyCustomPreset();
                    break;
            }

            this.onPresetApplied?.Invoke(this, replacementPreset);

            Console.WriteLine($"Applied Replacement Preset: {replacementPreset}");
        }

        private void ApplyCustomPreset()
        {

        }

        private void ApplyRecommendedPreset()
        {
            this.explicitReplacementMapping.Clear();
            this.logicMapping.Clear();
            this.enabledMapping.Clear();

            for (int k=0; k<this.recommendedReplacements.Count; k++)
            {
                var recommendedEntry = this.recommendedReplacements[k];

                var logic = recommendedEntry.logic;
                var spellToAdd = recommendedEntry.spellBeingAdded;
                var spellToReplace = recommendedEntry.spellBeingReplaced;

                this.logicMapping.Add(spellToAdd, logic);

                if (logic == SpellReplacementLogic.SpecificSpell)
                {
                    this.explicitReplacementMapping.Add(spellToAdd, spellToReplace);
                }

                this.enabledMapping.Add(spellToAdd, true);
            }
        }

        private void ApplyReplaceSimilarPreset()
        {
            this.explicitReplacementMapping.Clear();
            this.logicMapping.Clear();
            this.enabledMapping.Clear();

            foreach (var similarPairing in this.similarSpellReplacements)
            {
                var spellToAdd = similarPairing.Key;
                var spellToReplace = similarPairing.Value;

                this.logicMapping.Add(spellToAdd, SpellReplacementLogic.SpecificSpell);
                this.explicitReplacementMapping.Add(spellToAdd, spellToReplace);

                this.enabledMapping.Add(spellToAdd, true);
            }
        }

        private void ApplyNotUsedPreset()
        {
            this.explicitReplacementMapping.Clear();
            this.logicMapping.Clear();
            this.enabledMapping.Clear();
            

        }

        private readonly List<SpellReplacementEntry> recommendedReplacements = new List<SpellReplacementEntry>
        {
            new SpellReplacementEntry{ spellBeingAdded = SpellNameEnum.ZelseWindRazor, logic = SpellReplacementLogic.RandomDebuff, spellBeingReplaced = SpellNameEnum.HomingArrowLv1 },
            new SpellReplacementEntry{ spellBeingAdded = SpellNameEnum.ZelseWindZipper, logic = SpellReplacementLogic.RandomDamageSpell },

            new SpellReplacementEntry{ spellBeingAdded = SpellNameEnum.NeptyBubbleShot, logic = SpellReplacementLogic.SpecificSpell, spellBeingReplaced = SpellNameEnum.SoulSearcherLv1 },
            new SpellReplacementEntry{ spellBeingAdded = SpellNameEnum.ShilfDoveRazor, logic = SpellReplacementLogic.RandomDamageSpell },

            new SpellReplacementEntry{ spellBeingAdded = SpellNameEnum.FargoLavaBall, logic = SpellReplacementLogic.RandomStatusSpell },
            new SpellReplacementEntry{ spellBeingAdded = SpellNameEnum.FargoExplosion, logic = SpellReplacementLogic.RandomDamageSpell },

            new SpellReplacementEntry{ spellBeingAdded = SpellNameEnum.BeigisSpiritSword, logic = SpellReplacementLogic.RandomSpell },
            new SpellReplacementEntry{ spellBeingAdded = SpellNameEnum.MammonFlameWaves, logic = SpellReplacementLogic.RandomBuff },
            new SpellReplacementEntry{ spellBeingAdded = SpellNameEnum.MammonFireArrows, logic = SpellReplacementLogic.RandomSpell },
        };

        private readonly Dictionary<SpellNameEnum, SpellNameEnum> similarSpellReplacements = new Dictionary<SpellNameEnum, SpellNameEnum>
        {
            { SpellNameEnum.ZelseWindRazor, SpellNameEnum.HomingArrowLv1 },
            { SpellNameEnum.ZelseWindZipper, SpellNameEnum.RollingRockLv1 },
            { SpellNameEnum.NeptyBubbleShot, SpellNameEnum.SoulSearcherLv1 },
            { SpellNameEnum.ShilfDoveRazor, SpellNameEnum.WindCutterLv2 },
            { SpellNameEnum.FargoLavaBall, SpellNameEnum.FireBallLv1 },
            { SpellNameEnum.FargoExplosion, SpellNameEnum.FireBomb },
            { SpellNameEnum.BeigisSpiritSword, SpellNameEnum.Cyclone},
            { SpellNameEnum.MammonFlameWaves, SpellNameEnum.MagmaBall},
            { SpellNameEnum.MammonFireArrows, SpellNameEnum.HomingArrowLv2 },
        };
    }

    public static class ReplacementOperations
    {
        public static string ReplaceSpellLogicData(SpellData original, SpellData replacement)
        {
            var originalAttributes = original.GetAttributeData();
            var replacementAttributes = replacement.GetAttributeData();

            // Grab the spell rule and menu path of the original spell,
            // but everything else from the replacement spell.
            //
            const int MENU_LEVEL_REQ_OFFSET = 0x0;
            const int MENU_LEVEL_REQ_LENGTH = 0x2;
            const int MENU_MEM_OFFSET = 0x4;
            const int MENU_MEM_LENGTH = 0x6;

            const int STR_LEVEL_START = 2 * MENU_LEVEL_REQ_OFFSET;
            const int STR_LEVEL_LENGTH = 2 * MENU_LEVEL_REQ_LENGTH;
            const int STR_MENU_START = 2 * MENU_MEM_OFFSET;
            const int STR_MENU_LENGTH = 2 * MENU_MEM_LENGTH;

            var originalLevelReq = originalAttributes.Substring(STR_LEVEL_START, STR_LEVEL_LENGTH);
            var originalMenuData = originalAttributes.Substring(STR_MENU_START, STR_MENU_LENGTH);

            replacementAttributes = replacementAttributes
                .ReplaceAt(STR_LEVEL_START, originalLevelReq)
                .ReplaceAt(STR_MENU_START, originalMenuData);

            return replacementAttributes;
        }

        private static string ReplaceSpellLogicDataRaw(string originalData, string replacementData)
        {
            // Grab the spell rule and menu path of the original spell,
            // but everything else from the replacement spell.
            //
            const int MENU_LEVEL_REQ_OFFSET = 0x0;
            const int MENU_LEVEL_REQ_LENGTH = 0x2;
            const int MENU_MEM_OFFSET = 0x4;
            const int MENU_MEM_LENGTH = 0x6;

            const int STR_LEVEL_START = 2 * MENU_LEVEL_REQ_OFFSET;
            const int STR_LEVEL_LENGTH = 2 * MENU_LEVEL_REQ_LENGTH;
            const int STR_MENU_START = 2 * MENU_MEM_OFFSET;
            const int STR_MENU_LENGTH = 2 * MENU_MEM_LENGTH;

            var originalLevelReq = originalData.Substring(STR_LEVEL_START, STR_LEVEL_LENGTH);
            var originalMenuData = originalData.Substring(STR_MENU_START, STR_MENU_LENGTH);

            replacementData = replacementData
                .ReplaceAt(STR_LEVEL_START, originalLevelReq)
                .ReplaceAt(STR_MENU_START, originalMenuData);

            return replacementData;
        }

        public static MerrowPatchOperation GetSpellDataReplacementOperation(string originalRomAddress, string originalDataRaw, string replacementDataRaw)
        {
            var replacedDataString = ReplaceSpellLogicDataRaw(originalDataRaw, replacementDataRaw);

            return new MerrowPatchOperation
            {
                romAddress = originalRomAddress,
                patchContents = replacedDataString
            };
        }

        public static MerrowPatchOperation GetSpellDataReplacementOperation(SpellData originalData, SpellData replacementData)
        {
            var replacedDataString = ReplaceSpellLogicData(originalData, replacementData);

            return new MerrowPatchOperation
            {
                romAddress = originalData.RomAddress,
                patchContents = replacedDataString
            };
        }

        public static MerrowPatchOperation GetSpellAnimReplacementOperation(SpellAnimationData originalAnim, SpellAnimationData replacementAnim)
        {
            return new MerrowPatchOperation
            {
                romAddress = originalAnim.RomAddress,
                patchContents = replacementAnim.AnimationData
            };
        }
    }
}

