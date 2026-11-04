using Sproutoku.Core.Puzzles;
using Sproutoku.Core.Solving;
using Sproutoku.Core.Tests.Puzzles;

namespace Sproutoku.Core.Tests.Solving;

public class DifficultyGraderTests {
    // A step with the given technique and number of areas; the rest doesn't affect the grade.
    private static LogicStep Step(Technique technique, int areas = 1) =>
        new(technique, Enumerable.Repeat(new Area(AreaKind.Bed, 0), areas).ToList(), [], [], []);

    private static DifficultyGrade GradeOf(Puzzle puzzle) => DifficultyGrader.Grade(LogicSolver.Solve(GardenFacts.Blank(puzzle)));

    [Theory]
    [InlineData(Technique.PlantRulesOut, 0, DifficultyGrade.Gentle)]
    [InlineData(Technique.OnlyCellLeft, 1, DifficultyGrade.Gentle)]
    [InlineData(Technique.BedsFillLines, 2, DifficultyGrade.Easy)]
    [InlineData(Technique.LinesFillBeds, 2, DifficultyGrade.Easy)]
    [InlineData(Technique.WouldStarve, 1, DifficultyGrade.Medium)]
    [InlineData(Technique.BedsFillLines, 4, DifficultyGrade.Tricky)]
    [InlineData(Technique.LinesFillBeds, 4, DifficultyGrade.Tricky)]
    [InlineData(Technique.BedsFillLines, 6, DifficultyGrade.Expert)]
    public void EachStep_HasAGrade(Technique technique, int areas, DifficultyGrade grade) {
        Assert.Equal(grade, DifficultyGrader.GradeOf(Step(technique, areas)));
    }

    [Fact]
    public void AGarden_IsGradedByItsHardestStep() {
        var result = new SolveResult(true, [Step(Technique.OnlyCellLeft), Step(Technique.WouldStarve), Step(Technique.BedsFillLines, 2)], []);

        Assert.Equal(DifficultyGrade.Medium, DifficultyGrader.Grade(result));
    }

    [Fact]
    public void AnUnsolvedGarden_HasNoGrade() {
        var result = new SolveResult(false, [Step(Technique.OnlyCellLeft)], []);

        Assert.Throws<ArgumentException>(() => DifficultyGrader.Grade(result));
    }

    [Fact]
    public void KnownGardens_HaveTheirGrades() {
        // Worked out with a reference copy of the solver.
        Assert.Equal(DifficultyGrade.Easy, GradeOf(StarterGardens.All[0]));
        Assert.Equal(DifficultyGrade.Gentle, GradeOf(StarterGardens.All[1]));
        Assert.Equal(DifficultyGrade.Medium, GradeOf(StarterGardens.All[2]));
        Assert.Equal(DifficultyGrade.Medium, GradeOf(Puzzle.Create(TestGardens.ValidRegions(), TestGardens.ValidSolution())));
    }
}
