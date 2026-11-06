using Sproutoku.Core.Gardens.Generation;

namespace Sproutoku.Core.Tests.Gardens.Generation;

public class BedGrowerTests {
    public static TheoryData<int> AllSizes => [4, 5, 6, 7, 8, 9, 10];

    // Plants then beds from one random sequence, the same way the generator does.
    private static string[] Grow(int size, ulong seed, BedStyle style = BedStyle.Sprawling) {
        var random = new SeededRandom(seed);
        var beds = BedGrower.Grow(PlantPlacer.Place(size, random), random, style);
        return beds.Select(row => string.Concat(row)).ToArray();
    }

    [Theory]
    [MemberData(nameof(AllSizes))]
    public void EveryGarden_IsAValidPuzzle_OverManySeeds(int size) {
        for (ulong seed = 0; seed < 200; seed++) {
            var random = new SeededRandom(seed);
            var plants = PlantPlacer.Place(size, random);
            var beds = BedGrower.Grow(plants, random);

            // The validator checks every cell has a bed, each bed is connected, and each holds its own plant.
            var errors = PuzzleValidator.Validate(beds, plants);
            Assert.True(errors.Count == 0, $"Seed {seed}: {string.Join("; ", errors.Select(error => error.Message))}");
        }
    }

    [Fact]
    public void TheSameSeed_GivesTheSameBeds() {
        Assert.Equal(Grow(8, seed: 123), Grow(8, seed: 123));
    }

    [Fact]
    public void Beds_ArePinnedForKnownSeeds() {
        // Worked out with a reference script. If these change, every generated garden changes.
        Assert.Equal(["20011", "22001", "22111", "43333", "43333"], Grow(5, seed: 1));
        Assert.Equal(["000111", "101111", "111112", "513344", "553344", "555344"], Grow(6, seed: 1));
    }

    [Fact]
    public void DifferentSeeds_GiveDifferentGardens() {
        var gardens = Enumerable.Range(0, 100)
            .Select(seed => string.Join("/", Grow(5, (ulong)seed)))
            .ToHashSet();

        Assert.Equal(100, gardens.Count);
    }

    [Fact]
    public void TooFewPlants_Throw() {
        Assert.Throws<ArgumentException>(() => BedGrower.Grow([new(0, 0), new(1, 2), new(2, 4)], new SeededRandom(1)));
    }

    [Fact]
    public void APlantOutsideTheGarden_Throws() {
        Assert.Throws<ArgumentException>(() => BedGrower.Grow([new(0, 1), new(1, 3), new(2, 0), new(3, 4)], new SeededRandom(1)));
    }

    [Fact]
    public void TwoPlantsOnOneCell_Throw() {
        Assert.Throws<ArgumentException>(() => BedGrower.Grow([new(0, 1), new(0, 1), new(2, 0), new(3, 2)], new SeededRandom(1)));
    }

    [Theory]
    [InlineData(BedStyle.Sprawling)]
    [InlineData(BedStyle.Winding)]
    [InlineData(BedStyle.Even)]
    public void EveryStyle_GivesAValidPuzzle_AtEverySize(BedStyle style) {
        for (int size = 4; size <= 10; size++) {
            for (ulong seed = 0; seed < 50; seed++) {
                var random = new SeededRandom(seed);
                var plants = PlantPlacer.Place(size, random);
                var beds = BedGrower.Grow(plants, random, style);

                var errors = PuzzleValidator.Validate(beds, plants);
                Assert.True(errors.Count == 0, $"{style} {size}×{size} seed {seed}: {string.Join("; ", errors.Select(error => error.Message))}");
            }
        }
    }

    [Fact]
    public void Styles_ArePinnedForKnownSeeds() {
        // Worked out with a reference script. Sprawling's 6×6 pin is in Beds_ArePinnedForKnownSeeds.
        Assert.Equal(["011111", "001111", "301322", "333332", "555544", "555444"], Grow(6, seed: 1, BedStyle.Winding));
        Assert.Equal(["001112", "011112", "331122", "333144", "554444", "554444"], Grow(6, seed: 1, BedStyle.Even));
    }

    [Fact]
    public void EvenBeds_StaySmaller_ThanSprawlingOnes() {
        // Over 100 seeds the biggest 7×7 bed averages about 10.2 cells in Even and 12.7 in Sprawling.
        double BiggestBed(BedStyle style) => Enumerable.Range(0, 100)
            .Select(seed => Grow(7, (ulong)seed, style).SelectMany(row => row).GroupBy(bed => bed).Max(group => group.Count()))
            .Average();

        Assert.True(BiggestBed(BedStyle.Even) < BiggestBed(BedStyle.Sprawling));
    }

    [Fact]
    public void PickStyle_UsesEveryStyle_WithSprawlingMostOften() {
        var counts = Enumerable.Range(0, 100)
            .GroupBy(seed => BedGrower.PickStyle(new SeededRandom((ulong)seed)))
            .ToDictionary(group => group.Key, group => group.Count());

        Assert.Equal(3, counts.Count);
        Assert.True(counts[BedStyle.Sprawling] > counts[BedStyle.Winding]);
        Assert.True(counts[BedStyle.Sprawling] > counts[BedStyle.Even]);
    }
}
