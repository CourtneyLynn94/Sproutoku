using Sproutoku.Core.Puzzles;

namespace Sproutoku.Core.Tests.Puzzles;

public class PuzzleTextTests {
    [Fact]
    public void Parse_ReadsRegionsAndSolution() {
        // The same garden as TestGardens.ValidRegions, written with letters.
        var puzzle = PuzzleText.Parse(
            "A a b b b",
            "b b B b b",
            "d d b c C",
            "d D d e e",
            "d d e E e");

        var expected = TestGardens.ValidRegions();
        for (int row = 0; row < 5; row++) {
            for (int column = 0; column < 5; column++) {
                Assert.Equal(expected[row][column], puzzle.RegionAt(new Cell(row, column)));
            }
        }

        Assert.Equal(TestGardens.ValidSolution(), puzzle.Solution.ToArray());
    }

    [Fact]
    public void Parse_GardenMissingAPlant_Throws() {
        // Region b has no uppercase letter.
        Assert.Throws<InvalidPuzzleException>(() => PuzzleText.Parse(
            "A a b b b",
            "b b b b b",
            "d d b c C",
            "d D d e e",
            "d d e E e"));
    }

    [Fact]
    public void Parse_NonLetter_Throws() {
        Assert.Throws<FormatException>(() => PuzzleText.Parse(
            "A a b b 1",
            "b b B b b",
            "d d b c C",
            "d D d e e",
            "d d e E e"));
    }
}
