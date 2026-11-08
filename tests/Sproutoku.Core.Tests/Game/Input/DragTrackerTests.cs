using Sproutoku.Core.Game.Input;

namespace Sproutoku.Core.Tests.Game.Input;

public class DragTrackerTests {
    private const int Size = 5;

    [Fact]
    public void ASmallWobble_StaysAPress() {
        var drag = new DragTracker(0.5, 0.5, Size);

        Assert.Empty(drag.MoveTo(0.7, 0.6));
        Assert.False(drag.IsDragging);
    }

    [Fact]
    public void MovingAThirdOfACell_StartsTheDrag_OnItsOwnCell() {
        var drag = new DragTracker(0.5, 0.5, Size);

        var cells = drag.MoveTo(0.9, 0.5);

        Assert.True(drag.IsDragging);
        Assert.Equal([new Cell(0, 0)], cells);
    }

    [Fact]
    public void ARowDrag_IgnoresWobbleIntoTheNextRow() {
        var drag = new DragTracker(0.5, 0.5, Size);

        var cells = drag.MoveTo(1.0, 0.55)  // Clearly sideways: locked to row 0.
            .Concat(drag.MoveTo(2.5, 1.2))  // The finger drifts into row 1…
            .Concat(drag.MoveTo(3.5, 1.4))  // …but not past its middle.
            .ToList();

        Assert.Equal(DragLock.Row, drag.Lock);
        Assert.All(cells, cell => Assert.Equal(0, cell.Row));
        Assert.Contains(new Cell(0, 3), cells);
    }

    [Fact]
    public void GoingPastTheMiddleOfTheNextRow_LetsGo() {
        var drag = new DragTracker(0.5, 0.5, Size);
        drag.MoveTo(1.0, 0.55);

        var cells = drag.MoveTo(1.5, 1.6);

        Assert.Equal(DragLock.None, drag.Lock);
        Assert.Contains(new Cell(1, 1), cells);
    }

    [Fact]
    public void AColumnDrag_StaysInItsColumn() {
        var drag = new DragTracker(2.5, 0.5, Size);

        var cells = drag.MoveTo(2.55, 1.0)
            .Concat(drag.MoveTo(3.2, 3.5))
            .ToList();

        Assert.Equal(DragLock.Column, drag.Lock);
        Assert.All(cells, cell => Assert.Equal(2, cell.Column));
        Assert.Contains(new Cell(3, 2), cells);
    }

    [Fact]
    public void ADiagonalStart_FollowsTheFinger() {
        var drag = new DragTracker(0.5, 0.5, Size);
        drag.MoveTo(0.8, 0.8);

        var cells = drag.MoveTo(2.5, 2.5);

        Assert.Equal(DragLock.None, drag.Lock);
        Assert.Equal([new Cell(0, 0), new Cell(1, 1), new Cell(2, 2)], cells);
    }

    [Fact]
    public void SlidingOffTheBoard_StaysAPress() {
        var drag = new DragTracker(0.5, 0.5, Size); // The top-left cell.

        Assert.Empty(drag.MoveTo(0.5, -0.5)); // Up, off the board.
        Assert.False(drag.IsDragging);
    }

    [Fact]
    public void LeavingTheBoardThroughAnotherCell_IsADrag() {
        var drag = new DragTracker(0.5, 0.5, Size);

        var cells = drag.MoveTo(1.6, -0.2); // Through (0,1) on the way off the top.

        Assert.True(drag.IsDragging);
        Assert.Contains(new Cell(0, 0), cells);
        Assert.Contains(new Cell(0, 1), cells);
    }

    [Fact]
    public void ComingBackOntoTheBoard_StartsTheDrag() {
        var drag = new DragTracker(0.5, 0.5, Size);
        drag.MoveTo(0.5, -0.5);

        var cells = drag.MoveTo(0.9, 0.5);

        Assert.True(drag.IsDragging);
        Assert.Equal([new Cell(0, 0)], cells);
    }
}
