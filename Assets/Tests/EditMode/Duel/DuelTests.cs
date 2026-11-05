using System.Collections.Generic;
using System.IO;
using KnightOrder.Battle.Timing;
using KnightOrder.Duel;
using KnightOrder.Duel.Loading;
using NUnit.Framework;
using UnityEngine;

namespace KnightOrder.Tests.Duel
{
    /// <summary>
    /// Boss duel (Story 004): per-pass judgement (window, early, no tap, shield side), the stat formulas,
    /// victory and defeat results, and the duel data files. Fixtures come from the factory functions below.
    /// </summary>
    public class DuelTests
    {
        private const float STEP_SECONDS = 1f / 60f;

        private static DuelRules MakeRules()
        {
            return new DuelRules
            {
                ApproachSeconds = 2.4f,
                GuardRevealSeconds = 1.4f,
                WindowSeconds = 0.3f,
                TurnaroundSeconds = 1.0f,
                SpeedPowerScale = 0.02f,
                SuccessMultiplier = 1.0f,
                EarlyMultiplier = 0.4f,
                GuardedMultiplier = 0.25f,
                MaxPasses = 12
            };
        }

        private static DuelistStats MakeDuelist(int attack, int riderHp, int horseSpeed, int horseHp)
        {
            return new DuelistStats { RiderAttack = attack, RiderHp = riderHp, HorseSpeed = horseSpeed, HorseHp = horseHp };
        }

        private static DuelSetup MakeSetup()
        {
            return new DuelSetup
            {
                Hero = MakeDuelist(30, 100, 20, 40),
                Boss = MakeDuelist(25, 120, 20, 40),
                BossGuardPattern = new List<LanceAim> { LanceAim.High, LanceAim.Low, LanceAim.Low, LanceAim.High },
                VictoryGrowthReward = 3
            };
        }

        private static LanceAim OpenSide(LanceAim guard) => guard == LanceAim.High ? LanceAim.Low : LanceAim.High;

        /// <summary>Advances to just past <paramref name="time"/>, so float rounding cannot stop short of it.</summary>
        private static void AdvanceTo(DuelSimulation duel, float time)
        {
            duel.Advance(time - duel.Now + 1e-4f);
        }

        /// <summary>Taps the current pass <paramref name="secondsBeforeImpact"/> before its impact, then resolves it.</summary>
        private static PassResolvedEventArgs PlayPass(DuelSimulation duel, float secondsBeforeImpact, LanceAim aim)
        {
            PassResolvedEventArgs resolved = null;
            void Handler(object s, PassResolvedEventArgs e) => resolved = e;
            duel.OnPassResolved += Handler;
            AdvanceTo(duel, duel.ImpactTime - secondsBeforeImpact);
            duel.Tap(aim);
            AdvanceTo(duel, duel.ImpactTime);
            duel.OnPassResolved -= Handler;
            return resolved;
        }

        private static void RunToEnd(DuelSimulation duel)
        {
            for (int guard = 0; !duel.IsFinished && guard < 100000; guard++)
            {
                duel.Advance(STEP_SECONDS);
            }
        }

        [Test]
        public void NoInput_WholeDuel_DefeatWithoutReward()
        {
            // Arrange
            var duel = new DuelSimulation(MakeRules(), MakeSetup());

            // Act
            RunToEnd(duel);

            // Assert
            DuelResult result = duel.GetResult();
            Assert.IsFalse(result.Victory);
            Assert.AreEqual(0, result.HeroHp);
            Assert.AreEqual(duel.BossMaxHp, result.BossHp);
            Assert.AreEqual(0, result.GrowthReward);
            Assert.IsFalse(result.ReachedPassCap);
            Assert.AreEqual((duel.HeroMaxHp + duel.BossPower - 1) / duel.BossPower, result.PassesFought);
        }

        [Test]
        public void TapInWindowAtOpenSide_FullLanceDamage_BossLanceDeflected()
        {
            // Arrange
            var duel = new DuelSimulation(MakeRules(), MakeSetup());

            // Act
            PassResolvedEventArgs pass = PlayPass(duel, 0.1f, OpenSide(duel.BossGuard));

            // Assert
            Assert.AreEqual(TimingOutcome.Success, pass.Outcome);
            Assert.IsFalse(pass.HitShield);
            Assert.AreEqual(duel.HeroPower, pass.DamageToBoss);
            Assert.AreEqual(0, pass.DamageToHero);
            Assert.AreEqual(duel.BossMaxHp - duel.HeroPower, duel.BossHp);
            Assert.AreEqual(duel.HeroMaxHp, duel.HeroHp);
        }

        [Test]
        public void TapInWindowAtShield_GuardedDamage_BossLanceDeflected()
        {
            // Arrange
            DuelRules rules = MakeRules();
            var duel = new DuelSimulation(rules, MakeSetup());

            // Act
            PassResolvedEventArgs pass = PlayPass(duel, 0.1f, duel.BossGuard);

            // Assert
            Assert.AreEqual(TimingOutcome.Success, pass.Outcome);
            Assert.IsTrue(pass.HitShield);
            Assert.AreEqual(Mathf.FloorToInt(duel.HeroPower * rules.SuccessMultiplier * rules.GuardedMultiplier), pass.DamageToBoss);
            Assert.AreEqual(0, pass.DamageToHero);
        }

        [Test]
        public void EarlyTapAtOpenSide_ReducedLanceDamage_BossLanceHits()
        {
            // Arrange
            DuelRules rules = MakeRules();
            var duel = new DuelSimulation(rules, MakeSetup());

            // Act (after the shield is shown, before the window)
            PassResolvedEventArgs pass = PlayPass(duel, 0.8f, OpenSide(duel.BossGuard));

            // Assert
            Assert.AreEqual(TimingOutcome.Early, pass.Outcome);
            Assert.AreEqual(Mathf.FloorToInt(duel.HeroPower * rules.EarlyMultiplier), pass.DamageToBoss);
            Assert.AreEqual(duel.BossPower, pass.DamageToHero);
            Assert.AreEqual(duel.HeroMaxHp - duel.BossPower, duel.HeroHp);
        }

        [Test]
        public void TapBeforeGuardReveal_IsIgnored_PassIsMiss()
        {
            // Arrange
            var duel = new DuelSimulation(MakeRules(), MakeSetup());
            AdvanceTo(duel, duel.GuardRevealTime - 0.2f);

            // Act
            bool counted = duel.Tap(OpenSide(duel.BossGuard));
            PassResolvedEventArgs pass = null;
            duel.OnPassResolved += (s, e) => pass = e;
            AdvanceTo(duel, duel.ImpactTime);

            // Assert
            Assert.IsFalse(counted);
            Assert.AreEqual(TimingOutcome.Miss, pass.Outcome);
            Assert.IsNull(pass.HeroAim);
            Assert.AreEqual(0, pass.DamageToBoss);
            Assert.AreEqual(duel.BossPower, pass.DamageToHero);
        }

        [Test]
        public void SecondTapInPass_IsIgnored_FirstTapJudged()
        {
            // Arrange
            var duel = new DuelSimulation(MakeRules(), MakeSetup());
            LanceAim open = OpenSide(duel.BossGuard);
            AdvanceTo(duel, duel.ImpactTime - 0.8f);
            duel.Tap(open);
            AdvanceTo(duel, duel.ImpactTime - 0.1f);

            // Act
            bool counted = duel.Tap(open);
            PassResolvedEventArgs pass = null;
            duel.OnPassResolved += (s, e) => pass = e;
            AdvanceTo(duel, duel.ImpactTime);

            // Assert
            Assert.IsFalse(counted);
            Assert.AreEqual(TimingOutcome.Early, pass.Outcome);
        }

        [Test]
        public void TapAfterImpact_CountsForNoPass()
        {
            // Arrange
            var duel = new DuelSimulation(MakeRules(), MakeSetup());
            AdvanceTo(duel, duel.ImpactTime + 0.1f);

            // Act
            bool counted = duel.Tap(LanceAim.High);

            // Assert
            Assert.IsTrue(duel.IsTurning);
            Assert.IsFalse(counted);
            Assert.AreEqual(0, duel.PassIndex);
        }

        [Test]
        public void PerfectPlay_Victory_ReturnsGrowthReward()
        {
            // Arrange
            DuelSetup setup = MakeSetup();
            var duel = new DuelSimulation(MakeRules(), setup);

            // Act
            while (!duel.IsFinished)
            {
                PlayPass(duel, 0.1f, OpenSide(duel.BossGuard));
                AdvanceTo(duel, duel.IsFinished ? duel.Now : duel.NextPassStartTime);
            }

            // Assert
            DuelResult result = duel.GetResult();
            Assert.IsTrue(result.Victory);
            Assert.AreEqual(0, result.BossHp);
            Assert.AreEqual(duel.HeroMaxHp, result.HeroHp);
            Assert.AreEqual(setup.VictoryGrowthReward, result.GrowthReward);
            Assert.AreEqual((duel.BossMaxHp + duel.HeroPower - 1) / duel.HeroPower, result.PassesFought);
        }

        [Test]
        public void LancePowerAndHealth_FromRiderAndHorseStats()
        {
            // Arrange
            DuelRules rules = MakeRules();
            DuelSetup setup = MakeSetup();
            DuelSetup slowHorse = MakeSetup();
            slowHorse.Hero.HorseSpeed = 0;

            // Act
            var duel = new DuelSimulation(rules, setup);
            var slowDuel = new DuelSimulation(rules, slowHorse);

            // Assert
            Assert.AreEqual(Mathf.RoundToInt(setup.Hero.RiderAttack * (1f + setup.Hero.HorseSpeed * rules.SpeedPowerScale)), duel.HeroPower);
            Assert.AreEqual(setup.Hero.RiderAttack, slowDuel.HeroPower);
            Assert.Greater(duel.HeroPower, slowDuel.HeroPower);
            Assert.AreEqual(setup.Hero.RiderHp + setup.Hero.HorseHp, duel.HeroMaxHp);
            Assert.AreEqual(setup.Boss.RiderHp + setup.Boss.HorseHp, duel.BossMaxHp);
        }

        [Test]
        public void BothFallOnSamePass_Defeat()
        {
            // Arrange: an early strike finishes the boss while the boss's lance finishes the protagonist.
            DuelSetup setup = MakeSetup();
            setup.Hero = MakeDuelist(30, 1, 20, 0);
            setup.Boss = MakeDuelist(25, 1, 20, 0);
            var duel = new DuelSimulation(MakeRules(), setup);

            // Act
            PlayPass(duel, 0.8f, OpenSide(duel.BossGuard));

            // Assert
            DuelResult result = duel.GetResult();
            Assert.AreEqual(0, result.HeroHp);
            Assert.AreEqual(0, result.BossHp);
            Assert.IsFalse(result.Victory);
            Assert.AreEqual(0, result.GrowthReward);
        }

        [Test]
        public void PassCapReached_BothUp_Defeat()
        {
            // Arrange: perfect timing into the shield deals and takes nothing.
            DuelRules rules = MakeRules();
            rules.GuardedMultiplier = 0f;
            rules.MaxPasses = 3;
            var duel = new DuelSimulation(rules, MakeSetup());

            // Act
            while (!duel.IsFinished)
            {
                PlayPass(duel, 0.1f, duel.BossGuard);
                AdvanceTo(duel, duel.IsFinished ? duel.Now : duel.NextPassStartTime);
            }

            // Assert
            DuelResult result = duel.GetResult();
            Assert.IsFalse(result.Victory);
            Assert.IsTrue(result.ReachedPassCap);
            Assert.AreEqual(rules.MaxPasses, result.PassesFought);
        }

        [Test]
        public void BossGuardPattern_RepeatsFromTheStart()
        {
            // Arrange
            DuelSetup setup = MakeSetup();
            setup.BossGuardPattern = new List<LanceAim> { LanceAim.High, LanceAim.Low, LanceAim.Low };
            DuelRules rules = MakeRules();
            rules.MaxPasses = 6;
            setup.Hero.RiderHp = 1000;
            var duel = new DuelSimulation(rules, setup);
            var guards = new List<LanceAim>();
            duel.OnPassResolved += (s, e) => guards.Add(e.BossGuard);

            // Act (no input, the protagonist outlasts the pass cap)
            RunToEnd(duel);

            // Assert
            CollectionAssert.AreEqual(new[] { LanceAim.High, LanceAim.Low, LanceAim.Low, LanceAim.High, LanceAim.Low, LanceAim.Low }, guards);
        }

        [Test]
        public void OneLargeAdvance_SameResultAsSmallSteps()
        {
            // Arrange
            var stepped = new DuelSimulation(MakeRules(), MakeSetup());
            var jumped = new DuelSimulation(MakeRules(), MakeSetup());
            int jumpedPasses = 0;
            jumped.OnPassResolved += (s, e) => jumpedPasses++;

            // Act
            RunToEnd(stepped);
            jumped.Advance(10000f);

            // Assert
            Assert.IsTrue(jumped.IsFinished);
            Assert.AreEqual(stepped.GetResult().PassesFought, jumped.GetResult().PassesFought);
            Assert.AreEqual(stepped.GetResult().HeroHp, jumped.GetResult().HeroHp);
            Assert.AreEqual(jumped.GetResult().PassesFought, jumpedPasses);
        }

        [Test]
        public void ShippedDataFiles_NoInputLoses_PerfectPlayWins()
        {
            // Arrange
            DuelRules rules = DuelDataLoader.ParseRules(ReadData("duel_rules.json"));
            DuelSetup setup = DuelDataLoader.ParseSetup(ReadData("duel_sample_setup.json"));
            var idle = new DuelSimulation(rules, setup);
            var perfect = new DuelSimulation(rules, setup);

            // Act
            RunToEnd(idle);
            while (!perfect.IsFinished)
            {
                PlayPass(perfect, rules.WindowSeconds * 0.5f, OpenSide(perfect.BossGuard));
                AdvanceTo(perfect, perfect.IsFinished ? perfect.Now : perfect.NextPassStartTime);
            }

            // Assert
            Assert.IsFalse(idle.GetResult().Victory);
            Assert.IsTrue(perfect.GetResult().Victory);
            Assert.AreEqual(setup.VictoryGrowthReward, perfect.GetResult().GrowthReward);
            Assert.Greater(setup.VictoryGrowthReward, 0);
        }

        [Test]
        public void ParseRules_WindowLongerThanGuardReveal_Throws()
        {
            const string json = "{\"approach_seconds\":2.4,\"guard_reveal_seconds\":0.2,\"window_seconds\":0.3,"
                + "\"turnaround_seconds\":1,\"speed_power_scale\":0.02,\"success_multiplier\":1,"
                + "\"early_multiplier\":0.4,\"guarded_multiplier\":0.25,\"max_passes\":12}";

            Assert.Throws<InvalidDataException>(() => DuelDataLoader.ParseRules(json));
        }

        [TestCase("[]")]
        [TestCase("[\"high\",\"middle\"]")]
        public void ParseSetup_BadGuardPattern_Throws(string pattern)
        {
            string json = "{\"hero\":{\"rider_attack\":30,\"rider_hp\":100,\"horse_speed\":20,\"horse_hp\":40},"
                + "\"boss\":{\"rider_attack\":25,\"rider_hp\":120,\"horse_speed\":20,\"horse_hp\":40},"
                + "\"boss_guard_pattern\":" + pattern + ",\"victory_growth_reward\":3}";

            Assert.Throws<InvalidDataException>(() => DuelDataLoader.ParseSetup(json));
        }

        private static string ReadData(string fileName)
        {
            return File.ReadAllText(Path.Combine(Application.dataPath, "Data", "Duel", fileName));
        }
    }
}
