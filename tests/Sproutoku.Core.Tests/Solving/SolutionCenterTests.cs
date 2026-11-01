using Sproutoku.Core.Puzzles;
using Sproutoku.Core.Solving;
using Sproutoku.Core.Tests.Puzzles;

namespace Sproutoku.Core.Tests.Solving;

public class SolutionCounterTests {
    [Fact]
    public void EveryStarterGarden_HasExactlyOneSolution() {
        Assert.All(StarterGardens.All, garden => Assert.Equal(SolutionCount.One, SolutionCounter.Count(garden)));
    }

    [Fact]
    public void TheTestGarden_HasExactlyOneSolution() {
        var puzzle = Puzzle.Create(TestGardens.ValidRegions(), TestGardens.ValidSolution());

        Assert.Equal(SolutionCount.One, SolutionCounter.Count(puzzle));
    }

    [Fact]
    public void RoughGarden_HasMoreThanOneSolution() {
        // The rough random garden that "wasn't solvable by elimination": it has 9 solutions.
        var puzzle = PuzzleText.Parse(
            "B b a a a",
            "b b A c c",
            "b b c c C",
            "d D e e e",
            "d d e E e");

        Assert.Equal(SolutionCount.TwoOrMore, SolutionCounter.Count(puzzle));
    }

    [Fact]
    public void BedsThatCantAllBePlanted_HaveNoSolution() {
        // Beds 0 and 1 are single cells in the same row, so both can never have a plant.
        var regions = TestGardens.Regions(
            "0122",
            "3322",
            "3322",
            "3333");

        Assert.Equal(SolutionCount.None, SolutionCounter.Count(regions));
    }

    [Fact]
    public void ManyPossibleSolutions_StopsCounting_AtTwo() {
        // Every row is its own bed: lots of valid layouts.
        var regions = Enumerable.Range(0, 10)
            .Select(row => Enumerable.Repeat(row, 10).ToArray())
            .ToArray();

        Assert.Equal(SolutionCount.TwoOrMore, SolutionCounter.Count(regions));
    }

    [Fact]
    public void ANonSquareGrid_Throws() {
        var regions = TestGardens.Regions("012", "01");

        Assert.Throws<ArgumentException>(() => SolutionCounter.Count(regions));
    }
}
