using Sproutoku.Core.Play;
using Sproutoku.Core.Puzzles;

namespace Sproutoku.Core.Tests.Play;

public class GardenLevelsTests {
    [Fact]
    public void Levels_PlayTheStarterGardensInOrder() {
        for (int i = 0; i < StarterGardens.All.Count; i++) {
            Assert.Same(StarterGardens.All[i], GardenLevels.ForLevel(i + 1));
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void LevelsBelowOne_HaveNoGarden(int level) {
        Assert.Null(GardenLevels.ForLevel(level));
    }

    [Fact]
    public void AfterTheLastStarter_ThereIsNoGardenYet() {
        Assert.Null(GardenLevels.ForLevel(StarterGardens.All.Count + 1));
    }
}
