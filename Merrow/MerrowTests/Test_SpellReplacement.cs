using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Merrow;
using Merrow.Util;
using System.Text;
using System.CodeDom;

namespace MerrowTests
{
    [TestClass]
    public class Test_SpellReplacement
    {
        [TestMethod]
        public void Test_SpellDataReplacementLeavesMenuAndLevelReqs()
        {
            var allSpellData = SpellDefinitions.GetAllSpellData();

            var soulSearcherOne = allSpellData.GetSpell(SpellNameEnum.SoulSearcherLv1);
            var neptyBubbleShot = allSpellData.GetSpell(SpellNameEnum.NeptyBubbleShot);

            const string EXPECTED_RESULT = "000A0001000202000000000F00C8005A00000000000000000000000040A000000003000E0000000100020000000000030000000000000000000000000000000000000000";
            var actualResult = BossSpellReplacementWorkflow.ReplaceSpellLogicData(soulSearcherOne, neptyBubbleShot);

            Assert.AreEqual(EXPECTED_RESULT, actualResult);
        }


        [TestMethod]
        public void Test_SpellReplacementWorkflowProducesCorrectEntryCount()
        {
            var workflow = new BossSpellReplacementWorkflow();
            workflow.SetReplacementLogic(SpellNameEnum.NeptyBubbleShot, SpellReplacementLogic.RandomSpell);
            workflow.SetReplacementLogic(SpellNameEnum.FargoExplosion, SpellReplacementLogic.SpecificSpell);
            workflow.SetExplicitReplacementMapping(SpellNameEnum.FargoExplosion, SpellNameEnum.HotSteamLv1);

            var operations = workflow.GetVerbatimSpellReplacementOperations();

            Assert.AreEqual(operations.Count, 2);
        }

        [TestMethod]
        public void LOG_CheckDataReplacementExamples()
        {
            this.LOG_CheckDataReplacement(SpellNameEnum.FargoLavaBall, SpellNameEnum.FireBallLv1);
        }

        private void LOG_CheckDataReplacement(SpellNameEnum spellToAdd, SpellNameEnum spellToReplace)
        {
            var allSpellData = SpellDefinitions.GetAllSpellData();

            var originalData = allSpellData.GetSpell(spellToReplace);
            var replacementData = allSpellData.GetSpell(spellToAdd);

            var replacementHex = BossSpellReplacementWorkflow.ReplaceSpellLogicData(originalData, replacementData);

            Console.WriteLine($"[Test] {spellToAdd} replacing {spellToReplace}: ");
            Console.WriteLine($"[Test]   original: {originalData.DefaultAttributeData}");
            Console.WriteLine($"[Test]   replacer: {replacementData.DefaultAttributeData}");
            Console.WriteLine($"[Test]   updated:  {replacementHex}");
        }
    }
}
