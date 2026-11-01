using Sproutoku.Core.Generation;

namespace Sproutoku.Core.Tests.Generation;

public class SeededRandomTests {
    [Fact]
    public void SeedZero_GivesTheReferenceSplitMix64Outputs() {
        // The published SplitMix64 outputs for seed 0. If these change, every generated garden changes.
        var random = new SeededRandom(0);

        Assert.Equal(0xE220A8397B1DCDAFUL, random.NextUInt64());
        Assert.Equal(0x6E789E6AA1B965F4UL, random.NextUInt64());
        Assert.Equal(0x06C45D188009454FUL, random.NextUInt64());
    }

    [Fact]
    public void Next_IsPinnedForAKnownSeed() {
        var random = new SeededRandom(42);

        int[] values = [.. Enumerable.Range(0, 10).Select(_ => random.Next(10))];

        Assert.Equal([3, 1, 8, 4, 0, 2, 5, 8, 5, 4], values);
    }

    [Fact]
    public void SeedFrom_IsPinned_AndOrderMatters() {
        Assert.Equal(0x910A2DEC89025CC1UL, SeededRandom.SeedFrom(1));
        Assert.Equal(0xBCD9DBB49673066BUL, SeededRandom.SeedFrom(1, 2));
        Assert.Equal(0xE06DD043328BD285UL, SeededRandom.SeedFrom(2, 1));
    }

    [Fact]
    public void SameSeed_GivesTheSameSequence() {
        var first = new SeededRandom(1234);
        var second = new SeededRandom(1234);

        for (int i = 0; i < 100; i++) {
            Assert.Equal(first.NextUInt64(), second.NextUInt64());
        }
    }

    [Fact]
    public void DifferentSeeds_GiveDifferentSequences() {
        Assert.NotEqual(new SeededRandom(1).NextUInt64(), new SeededRandom(2).NextUInt64());
    }

    [Fact]
    public void Next_StaysInRange_AndReachesEveryValue() {
        var random = new SeededRandom(7);
        var seen = new HashSet<int>();

        for (int i = 0; i < 1000; i++) {
            int value = random.Next(6);
            Assert.InRange(value, 0, 5);
            seen.Add(value);
        }

        Assert.Equal(6, seen.Count);
    }

    [Fact]
    public void NextWithMinimum_StaysInRange() {
        var random = new SeededRandom(7);

        for (int i = 0; i < 1000; i++) {
            Assert.InRange(random.Next(5, 10), 5, 9);
        }
    }

    [Fact]
    public void NextWithMinimum_HandlesTheWidestRange() {
        var random = new SeededRandom(7);

        for (int i = 0; i < 100; i++) {
            random.Next(int.MinValue, int.MaxValue); //must not throw
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Next_WithNoValuesToPickFrom_Throws(int maxExclusive) {
        var random = new SeededRandom(7);

        Assert.Throws<ArgumentOutOfRangeException>(() => random.Next(maxExclusive));
    }

    [Fact]
    public void NextDouble_IsFromZeroUpToOne() {
        var random = new SeededRandom(7);

        for (int i = 0; i < 1000; i++) {
            double value = random.NextDouble();
            Assert.True(value is >= 0.0 and < 1.0);
        }
    }

    [Fact]
    public void Shuffle_IsRepeatable_AndKeepsEveryItem() {
        int[] first = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];
        int[] second = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];

        new SeededRandom(99).Shuffle(first);
        new SeededRandom(99).Shuffle(second);

        Assert.Equal(first, second);
        Assert.Equal(Enumerable.Range(0, 10), first.Order());
        Assert.NotEqual(Enumerable.Range(0, 10), first);
    }
}
