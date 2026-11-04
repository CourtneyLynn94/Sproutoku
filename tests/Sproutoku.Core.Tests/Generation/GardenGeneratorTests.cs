using Sproutoku.Core.Generation;
using Sproutoku.Core.Puzzles;
using Sproutoku.Core.Solving;

namespace Sproutoku.Core.Tests.Generation;

public class GardenGeneratorTests {
    public static TheoryData<int> AllSizes => [4, 5, 6, 7, 8, 9, 10];

    private static string[] Beds(Puzzle puzzle) => Enumerable.Range(0, puzzle.Size)
        .Select(row => string.Concat(Enumerable.Range(0, puzzle.Size).Select(column => puzzle.RegionAt(new Cell(row, column)))))
        .ToArray();

    [Fact]
    public void TheSameSeed_GivesTheSameGarden() {
        var first = GardenGenerator.Generate(8, 123).Puzzle;
        var second = GardenGenerator.Generate(8, 123).Puzzle;

        Assert.Equal(Beds(first), Beds(second));
        Assert.Equal(first.Solution, second.Solution);
    }

    [Fact]
    public void Gardens_ArePinnedForKnownSeeds() {
        // Worked out with a reference script. If these change, every generated level changes.
        // 4×4: one reshaping move.
        // 5×5: one answer straight away (the same beds as BedGrowerTests).
        // 6×6: the first attempt gets stuck and the second one succeeds.
        Assert.Equal(["1000", "1000", "1222", "1333"], Beds(GardenGenerator.Generate(4, 1).Puzzle));
        Assert.Equal(["20011", "22001", "22111", "43333", "43333"], Beds(GardenGenerator.Generate(5, 1).Puzzle));
        Assert.Equal(["022211", "222211", "222211", "222213", "444455", "444555"], Beds(GardenGenerator.Generate(6, 1).Puzzle));
    }

    [Theory]
    [InlineData(3)]
    [InlineData(11)]
    public void SizesOutsideTheGame_Throw(int size) {
        Assert.Throws<ArgumentOutOfRangeException>(() => GardenGenerator.Generate(size, 1));
    }

    [Theory]
    [MemberData(nameof(AllSizes))]
    public void EveryGarden_HasOneAnswer_ThatLogicAloneReaches(int size) {
        for (ulong seed = 0; seed < 100; seed++) {
            var puzzle = GardenGenerator.Generate(size, seed).Puzzle;
            var result = LogicSolver.Solve(GardenFacts.Blank(puzzle));

            Assert.Equal(size, puzzle.Size);
            Assert.True(SolutionCounter.Count(puzzle) == SolutionCount.One, $"Seed {seed} has more than one answer.");
            Assert.True(result.IsSolved, $"Seed {seed} needs a guess.");
            Assert.Equal(puzzle.Solution.ToHashSet(), result.Plants.ToHashSet());
        }
    }

    [Fact]
    public void Attempts_AreCounted() {
        Assert.Equal(1, GardenGenerator.Generate(5, 1).Attempts);
        Assert.Equal(2, GardenGenerator.Generate(6, 1).Attempts); // The first attempt gets stuck reshaping.
    }

    [Theory]
    [InlineData(DifficultyGrade.Gentle)]
    [InlineData(DifficultyGrade.Easy)]
    [InlineData(DifficultyGrade.Medium)]
    [InlineData(DifficultyGrade.Tricky)]
    public void AskingForAGrade_GivesThatGrade(DifficultyGrade grade) {
        for (ulong seed = 0; seed < 20; seed++) {
            var garden = GardenGenerator.Generate(7, seed, easiest: grade, hardest: grade);

            Assert.Equal(grade, garden.Grade);
            Assert.Equal(grade, DifficultyGrader.Grade(LogicSolver.Solve(GardenFacts.Blank(garden.Puzzle))));
        }
    }

    [Fact]
    public void AnExpertGarden_CanBeAskedForAt10x10() {
        Assert.Equal(DifficultyGrade.Expert, GardenGenerator.Generate(10, 1, easiest: DifficultyGrade.Expert).Grade);
    }

    [Fact]
    public void AnEmptyGradeRange_Throws() {
        Assert.Throws<ArgumentException>(() => GardenGenerator.Generate(5, 1, easiest: DifficultyGrade.Medium, hardest: DifficultyGrade.Easy));
    }
}
