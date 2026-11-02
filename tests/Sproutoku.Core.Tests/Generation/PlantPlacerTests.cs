using Sproutoku.Core.Generation;

namespace Sproutoku.Core.Tests.Generation;

public class PlantPlacerTests {
    public static TheoryData<int> AllSizes => [4, 5, 6, 7, 8, 9, 10];

    [Theory]
    [MemberData(nameof(AllSizes))]
    public void EveryLayout_FollowsTheRules_OverManySeeds(int size) {
        for (ulong seed = 0; seed < 200; seed++) {
            var plants = PlantPlacer.Place(size, new SeededRandom(seed));

            Assert.Equal(size, plants.Count);
            Assert.Equal(Enumerable.Range(0, size), plants.Select(plant => plant.Row));
            Assert.Equal(size, plants.Select(plant => plant.Column).Distinct().Count());

            for (int a = 0; a < size; a++) {
                for (int b = a + 1; b < size; b++) {
                    Assert.False(plants[a].Touches(plants[b]), $"Seed {seed}: plants {plants[a]} and {plants[b]} touch.");
                }
            }
        }
    }

    [Fact]
    public void TheSameSeed_GivesTheSameLayout() {
        var first = PlantPlacer.Place(8, new SeededRandom(123));
        var second = PlantPlacer.Place(8, new SeededRandom(123));

        Assert.Equal(first, second);
    }

    [Fact]
    public void Layouts_ArePinnedForKnownSeeds() {
        // Worked out with a reference script. If these change, every generated garden changes.
        Assert.Equal([0, 3, 5, 2, 4, 1], PlantPlacer.Place(6, new SeededRandom(1)).Select(plant => plant.Column));
        Assert.Equal([8, 3, 1, 6, 9, 4, 0, 2, 5, 7], PlantPlacer.Place(10, new SeededRandom(7)).Select(plant => plant.Column));
    }

    [Fact]
    public void Every5x5Layout_CanAppear() {
        // There are exactly 14 valid 5×5 layouts; 500 seeds reach all of them, so none is left out.
        var seen = Enumerable.Range(0, 500)
            .Select(seed => string.Join(",", PlantPlacer.Place(5, new SeededRandom((ulong)seed)).Select(plant => plant.Column)))
            .ToHashSet();

        Assert.Equal(14, seen.Count);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(11)]
    public void SizesOutsideTheGame_Throw(int size) {
        Assert.Throws<ArgumentOutOfRangeException>(() => PlantPlacer.Place(size, new SeededRandom(1)));
    }
}
