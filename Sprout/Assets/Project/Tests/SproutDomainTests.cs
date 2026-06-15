using NUnit.Framework;
using Sprout.Domain.Flowers;
using Sprout.Domain.Narrative;
using Sprout.Domain.Creativity;
using Sprout.Domain.DayCycle;
using Sprout.Domain.Endings;
using Sprout.Domain.Gossip;

namespace Sprout.Tests
{
    public class BouquetResolverTests
    {
        [Test] public void Peace_IsOrderIndependent()
        {
            Assert.AreEqual(BouquetKind.Peace, BouquetResolver.Resolve(FlowerKind.Sol, FlowerKind.Acuariana));
            Assert.AreEqual(BouquetKind.Peace, BouquetResolver.Resolve(FlowerKind.Acuariana, FlowerKind.Sol));
        }

        [Test] public void AllEightRecipesResolve()
        {
            Assert.AreEqual(BouquetKind.HiddenDesire, BouquetResolver.Resolve(FlowerKind.Brasa, FlowerKind.Crisalida));
            Assert.AreEqual(BouquetKind.Comfort,      BouquetResolver.Resolve(FlowerKind.Velada, FlowerKind.Acuariana));
            Assert.AreEqual(BouquetKind.Obsession,    BouquetResolver.Resolve(FlowerKind.Brasa, FlowerKind.Inquieta));
            Assert.AreEqual(BouquetKind.Promise,      BouquetResolver.Resolve(FlowerKind.Sol, FlowerKind.Anima));
            Assert.AreEqual(BouquetKind.Confession,   BouquetResolver.Resolve(FlowerKind.Crisalida, FlowerKind.Anima));
            Assert.AreEqual(BouquetKind.Farewell,     BouquetResolver.Resolve(FlowerKind.Velada, FlowerKind.Brasa));
            Assert.AreEqual(BouquetKind.Suspicion,    BouquetResolver.Resolve(FlowerKind.Inquieta, FlowerKind.Crisalida));
        }

        [Test] public void UnknownPair_ReturnsNone()
            => Assert.AreEqual(BouquetKind.None, BouquetResolver.Resolve(FlowerKind.Sol, FlowerKind.Sol));
    }

    public class FlowerInventoryTests
    {
        [Test] public void Craft_ConsumesFlowers_AndProducesBouquet()
        {
            var inv = new FlowerInventory();
            inv.AddFlower(FlowerKind.Sol);
            inv.AddFlower(FlowerKind.Acuariana);
            var result = inv.Craft(FlowerKind.Sol, FlowerKind.Acuariana);
            Assert.AreEqual(BouquetKind.Peace, result);
            Assert.AreEqual(0, inv.CountOf(FlowerKind.Sol));
            Assert.AreEqual(1, inv.CountOf(BouquetKind.Peace));
        }

        [Test] public void Craft_FailsWithoutFlowers()
        {
            var inv = new FlowerInventory();
            inv.AddFlower(FlowerKind.Sol);
            Assert.AreEqual(BouquetKind.None, inv.Craft(FlowerKind.Sol, FlowerKind.Acuariana));
        }

        [Test] public void GiveBouquet_RemovesOne()
        {
            var inv = new FlowerInventory();
            inv.AddFlower(FlowerKind.Sol); inv.AddFlower(FlowerKind.Anima);
            inv.Craft(FlowerKind.Sol, FlowerKind.Anima);
            Assert.IsTrue(inv.GiveBouquet(BouquetKind.Promise));
            Assert.AreEqual(0, inv.CountOf(BouquetKind.Promise));
            Assert.IsFalse(inv.GiveBouquet(BouquetKind.Promise));
        }
    }

    public class NarrativeFlagStoreTests
    {
        [Test] public void Counter_Increments()
        {
            var s = new NarrativeFlagStore();
            Assert.AreEqual(1, s.IncrementCounter("mochi_ideas_count"));
            Assert.AreEqual(3, s.IncrementCounter("mochi_ideas_count", 2));
        }

        [Test] public void Flag_RoundTrips()
        {
            var s = new NarrativeFlagStore();
            s.SetFlag("aster_met");
            Assert.IsTrue(s.GetFlag("aster_met"));
            Assert.IsFalse(s.GetFlag("never_set"));
        }
    }

    public class DayCycleTests
    {
        [Test] public void Advances_ThroughPhases_AndDays()
        {
            var d = new DayCycleState(2);
            Assert.AreEqual(DayPhase.Morning, d.Phase);
            d.Advance(); d.Advance(); d.Advance(); // Aft, Eve, Night
            Assert.AreEqual(DayPhase.Night, d.Phase);
            d.Advance(); // roll to day 2
            Assert.AreEqual(2, d.Day);
            Assert.AreEqual(DayPhase.Morning, d.Phase);
        }

        [Test] public void Finishes_AfterTotalDays()
        {
            var d = new DayCycleState(1);
            for (int i = 0; i < 4; i++) d.Advance();
            Assert.IsTrue(d.IsFinished);
        }
    }

    public class EndingResolverTests
    {
        [Test] public void Harm_LeadsTo_TangledRoots()
        {
            var f = new NarrativeFlagStore();
            f.SetFlag(NarrativeFlagKeys.HelpedMothLie);
            f.SetFlag(NarrativeFlagKeys.RixHatesPlayer);
            Assert.AreEqual(EndingKind.TangledRoots,
                EndingResolver.Resolve(f, CreativityScores.Zero));
        }

        [Test] public void HighCreativity_Honest_NoHarm_Blooms()
        {
            var f = new NarrativeFlagStore();
            f.SetFlag(NarrativeFlagKeys.PlayerWasHonest);
            var c = new CreativityScores { Fluency = 6, Originality = 0.7f };
            Assert.AreEqual(EndingKind.BloomingVillage, EndingResolver.Resolve(f, c));
        }

        [Test] public void RixTrust_Curiosity_IsSecret()
        {
            var f = new NarrativeFlagStore();
            f.SetFlag(NarrativeFlagKeys.RixTrustsPlayer);
            f.SetFlag(NarrativeFlagKeys.RixCuriosity);
            Assert.AreEqual(EndingKind.SecretEnding, EndingResolver.Resolve(f, CreativityScores.Zero));
        }

        [Test] public void Default_IsPrettyButHollow()
            => Assert.AreEqual(EndingKind.PrettyButHollow,
                EndingResolver.Resolve(new NarrativeFlagStore(), CreativityScores.Zero));
    }

    public class GossipEngineTests
    {
        [Test] public void GossipAboutAster_MakesAsterAngry()
        {
            var f = new NarrativeFlagStore();
            f.SetFlag(NarrativeFlagKeys.GossipToMochiAboutAster);
            var results = GossipRuleEngine.RunNight(f);
            // Apply changes as the service would.
            foreach (var r in results)
                foreach (var (flag, val) in r.FlagChanges) f.SetFlag(flag, val);
            Assert.IsTrue(f.GetFlag(NarrativeFlagKeys.AsterAngry));
        }
    }
}
