using Sproutoku.Core.Gardens.Generation;

namespace Sproutoku.Core.Tests.Gardens.Generation;

public class GardenLikenessTests {
    private static Puzzle Garden() => Puzzle.Create(TestGardens.ValidRegions(), TestGardens.ValidSolution());

    // The same garden with every bed's id (and so its color) changed: bed b becomes bed 4 − b.
    private static Puzzle Recolored() {
        var regions = TestGardens.ValidRegions().Select(row => row.Select(bed => 4 - bed).ToArray()).ToArray();
        var solution = Enumerable.Reverse(TestGardens.ValidSolution()).ToArray();
        return Puzzle.Create(regions, solution);
    }

    // The same garden flipped left to right.
    private static Puzzle Mirrored() {
        var regions = TestGardens.ValidRegions().Select(row => Enumerable.Reverse(row).ToArray()).ToArray();
        var solution = TestGardens.ValidSolution().Select(cell => new Cell(cell.Row, 4 - cell.Column)).ToArray();
        return Puzzle.Create(regions, solution);
    }

    [Fact]
    public void Fingerprint_RenamesBedsInReadingOrder() {
        // Rows 00111 / 11111 / 33122 / 33344 / 33444: bed 0 is read first (a), then 1 (b), 3 (c), 2 (d), 4 (e).
        Assert.Equal("aabbbbbbbbccbddccceecceee", GardenLikeness.Fingerprint(Garden()));
    }

    [Fact]
    public void Fingerprint_IsTheSame_ForARecoloredCopy() {
        Assert.Equal(GardenLikeness.Fingerprint(Garden()), GardenLikeness.Fingerprint(Recolored()));
    }

    [Fact]
    public void Fingerprint_Differs_ForAMirroredCopy() {
        Assert.NotEqual(GardenLikeness.Fingerprint(Garden()), GardenLikeness.Fingerprint(Mirrored()));
    }

    [Fact]
    public void SharedBorders_OfAGardenWithItsCopies() {
        Assert.Equal(1.0, GardenLikeness.SharedBorders(Garden(), Garden()));
        Assert.Equal(1.0, GardenLikeness.SharedBorders(Garden(), Recolored()));
        Assert.True(GardenLikeness.LookAlike(Garden(), Recolored()));

        // A mirrored copy is allowed: it shares only some borders.
        Assert.False(GardenLikeness.LookAlike(Garden(), Mirrored()));
    }

    [Fact]
    public void SharedBorders_ArePinnedForKnownGardens() {
        // Worked out with the reference script that measured levels 6 and 7.
        var garden = TestGardens.ValidRegions();
        var other5x5 = TestGardens.Regions("11200", "11203", "11223", "44443", "44433");
        var a6x6 = TestGardens.Regions("000111", "101111", "111112", "513344", "553344", "555344");

        Assert.Equal(5.0 / 26, GardenLikeness.SharedBorders(garden, other5x5), precision: 10);
        Assert.Equal(4.0 / 11, GardenLikeness.SharedBorders(garden, a6x6), precision: 10);
        Assert.Equal(4.0 / 11, GardenLikeness.SharedBorders(Garden(), Mirrored()), precision: 10);
    }

    [Fact]
    public void SharedBorders_IsTheSameEitherWayRound() {
        var garden = TestGardens.ValidRegions();
        var a6x6 = TestGardens.Regions("000111", "101111", "111112", "513344", "553344", "555344");

        Assert.Equal(GardenLikeness.SharedBorders(garden, a6x6), GardenLikeness.SharedBorders(a6x6, garden));
    }
}
