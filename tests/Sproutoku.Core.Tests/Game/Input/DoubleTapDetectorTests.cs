using Sproutoku.Core.Input;
using Sproutoku.Core.Puzzles;

namespace Sproutoku.Core.Tests.Input;

public class DoubleTapDetectorTests {
    private static readonly Cell A = new(1, 1);
    private static readonly Cell B = new(1, 2);

    [Fact]
    public void FirstTap_IsNotADoubleTap() {
        var detector = new DoubleTapDetector();

        Assert.False(detector.RegisterTap(A, 1000));
    }

    [Fact]
    public void SecondTapOnSameCell_WithinTheGap_IsADoubleTap() {
        var detector = new DoubleTapDetector();
        detector.RegisterTap(A, 1000);

        Assert.True(detector.RegisterTap(A, 1250));
    }

    [Fact]
    public void SecondTap_AfterTheGap_IsNotADoubleTap() {
        var detector = new DoubleTapDetector();
        detector.RegisterTap(A, 1000);

        Assert.False(detector.RegisterTap(A, 1251));
    }

    [Fact]
    public void SecondTap_OnAnotherCell_IsNotADoubleTap() {
        var detector = new DoubleTapDetector();
        detector.RegisterTap(A, 1000);

        Assert.False(detector.RegisterTap(B, 1100));
    }

    [Fact]
    public void ThirdQuickTap_StartsOver() {
        var detector = new DoubleTapDetector();
        detector.RegisterTap(A, 1000);
        detector.RegisterTap(A, 1100);

        Assert.False(detector.RegisterTap(A, 1200));
    }

    [Fact]
    public void Reset_ForgetsTheLastTap() {
        var detector = new DoubleTapDetector();
        detector.RegisterTap(A, 1000);
        detector.Reset();

        Assert.False(detector.RegisterTap(A, 1100));
    }
}
