using Sproutoku.Core.Levels;
using Sproutoku.Core.Play;
using Sproutoku.Core.Tests.TestSupport;

namespace Sproutoku.Core.Tests.Levels;

public class LevelPreparerTests {
    // A real garden to copy, so fake growers can return something valid quickly.
    private static readonly LevelGarden Template = GardenLevels.ForLevel(1);

    // A pretend grower that counts how many gardens it grows.
    private sealed class CountingGrower {
        private int _calls;

        public int Calls => _calls;

        public LevelGarden Grow(int level) {
            Interlocked.Increment(ref _calls);
            return Template with { Level = level };
        }
    }

    [Fact]
    public async Task ALevel_IsGrownOnce_HoweverOftenItsAskedFor() {
        var grower = new CountingGrower();
        var preparer = new LevelPreparer(grower.Grow);

        preparer.Prepare(5);
        var first = await preparer.GetAsync(5);
        var second = await preparer.GetAsync(5);

        Assert.Equal(1, grower.Calls);
        Assert.Same(first, second);
        Assert.Equal(5, first.Level);
    }

    [Fact]
    public async Task IsReady_OnlyOnceTheGardenHasGrown() {
        using var release = new ManualResetEventSlim();
        var preparer = new LevelPreparer(level => {
            release.Wait(); // Holds the garden "growing" until the test lets go.
            return Template with { Level = level };
        });

        Assert.False(preparer.IsReady(2)); // Not asked for yet.

        var growing = preparer.GetAsync(2);
        Assert.False(preparer.IsReady(2)); // Still growing.

        release.Set();
        await growing;
        Assert.True(preparer.IsReady(2));
    }

    [Fact]
    public async Task ForgottenLevels_AreGrownAgain_IfAskedForLater() {
        var grower = new CountingGrower();
        var preparer = new LevelPreparer(grower.Grow);
        await preparer.GetAsync(3);
        await preparer.GetAsync(4);

        preparer.ForgetBelow(4);

        Assert.False(preparer.IsReady(3));
        Assert.True(preparer.IsReady(4));

        await preparer.GetAsync(3);
        Assert.Equal(3, grower.Calls);
    }

    [Fact]
    public async Task ByDefault_ItGrowsTheRealLevels() {
        var garden = await new LevelPreparer().GetAsync(3);

        Assert.Equal(TestGardens.BedRows(GardenLevels.ForLevel(3).Puzzle), TestGardens.BedRows(garden.Puzzle));
    }

    [Fact]
    public async Task LevelsBelowOne_Throw() {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => new LevelPreparer().GetAsync(0));
    }
}
