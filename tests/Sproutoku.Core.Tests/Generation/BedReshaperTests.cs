using Sproutoku.Core.Generation;
using Sproutoku.Core.Puzzles;
using Sproutoku.Core.Solving;
using Sproutoku.Core.Tests.Puzzles;

namespace Sproutoku.Core.Tests.Generation;

public class BedReshaperTests {
    // Two answers: these plants, and (0,1) (1,3) (2,0) (3,2).
    private static readonly Cell[] Plants = [new(0, 2), new(1, 0), new(2, 3), new(3, 1)];

    private static int[][] TwoAnswerBeds() => TestGardens.Regions(
        "1000",
        "1002",
        "1222",
        "1333"
    );

    [Fact]
    public void ATwoAnswerGarden_EndsWithOneAnswer_OverManySeeds() {
        for (ulong seed = 0; seed < 20; seed++) {
            var beds = TwoAnswerBeds();

            Assert.True(BedReshaper.TryMakeUnique(beds, Plants, new SeededRandom(seed), maxMoves: 16));
            Assert.Empty(PuzzleValidator.Validate(beds, Plants));
            Assert.Equal(SolutionCount.One, SolutionCounter.Count(beds));
        }
    }

    [Fact]
    public void OneMove_BreaksTheSecondAnswer() {
        // Only two cells can move without splitting their bed: (0,1) into bed 1, or (1,3) into bed 0.
        // Worked out with a reference script: seed 0 picks (1,3), seed 2 picks (0,1).
        var moveRight = TwoAnswerBeds();
        BedReshaper.TryMakeUnique(moveRight, Plants, new SeededRandom(0), maxMoves: 16);
        Assert.Equal(TestGardens.Regions("1000", "1000", "1222", "1333"), moveRight);

        var moveLeft = TwoAnswerBeds();
        BedReshaper.TryMakeUnique(moveLeft, Plants, new SeededRandom(2), maxMoves: 16);
        Assert.Equal(TestGardens.Regions("1100", "1002", "1222", "1333"), moveLeft);
    }

    [Fact]
    public void AOneAnswerGarden_IsLeftAlone() {
        var beds = TestGardens.ValidRegions();

        Assert.True(BedReshaper.TryMakeUnique(beds, TestGardens.ValidSolution(), new SeededRandom(1), maxMoves: 25));
        Assert.Equal(TestGardens.ValidRegions(), beds);
    }

    [Fact]
    public void ItGivesUp_WhenOutOfMoves() {
        var beds = TwoAnswerBeds();

        Assert.False(BedReshaper.TryMakeUnique(beds, Plants, new SeededRandom(1), maxMoves: 0));
        Assert.Equal(TwoAnswerBeds(), beds);
    }

    [Fact]
    public void ItGivesUp_WhenEveryMoveWouldSplitABed() {
        // Same two answers, but moving any of the second answer's cells would cut its bed in two.
        var beds = TestGardens.Regions(
            "0000",
            "1022",
            "1002",
            "1333"
        );

        Assert.False(BedReshaper.TryMakeUnique(beds, Plants, new SeededRandom(1), maxMoves: 16));
    }
}
