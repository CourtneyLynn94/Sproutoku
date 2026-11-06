using Sproutoku.Core.Puzzles;

namespace Sproutoku.Core.Solving;

/// <summary>
/// The solver's working copy of the facts. It grows as each step is applied.
/// </summary>
internal sealed class SolverGrid {
    private readonly GardenFacts _facts;
    private readonly Known[,] _known;

    public SolverGrid(GardenFacts facts) {
        _facts = facts;
        Size = facts.Size;
        _known = new Known[Size, Size];

        foreach (var cell in AllCells()) {
            _known[cell.Row, cell.Column] = facts[cell];
        }
    }

    public int Size { get; }

    public Known this[Cell cell] => _known[cell.Row, cell.Column];

    public int RegionAt(Cell cell) => _facts.RegionAt(cell);

    public bool IsSolved => Plants().Count == Size;

    public IReadOnlyList<Cell> Plants() => AllCells().Where(cell => this[cell] == Known.Plant).ToList();

    // Row by row, left to right.
    public IEnumerable<Cell> AllCells() {
        for (int row = 0; row < Size; row++) {
            for (int column = 0; column < Size; column++) {
                yield return new Cell(row, column);
            }
        }
    }

    // Beds first, then rows, then columns.
    public IEnumerable<Area> AllAreas() {
        foreach (var kind in new[] { AreaKind.Bed, AreaKind.Row, AreaKind.Column }) {
            for (int index = 0; index < Size; index++) {
                yield return new Area(kind, index);
            }
        }
    }

    public IEnumerable<Cell> CellsOf(Area area) => area.Kind switch {
        AreaKind.Bed => AllCells().Where(cell => RegionAt(cell) == area.Index),
        AreaKind.Row => Enumerable.Range(0, Size).Select(column => new Cell(area.Index, column)),
        _ => Enumerable.Range(0, Size).Select(row => new Cell(row, area.Index)),
    };

    public void Apply(LogicStep step) {
        foreach (var cell in step.Plants) {
            _known[cell.Row, cell.Column] = Known.Plant;
        }

        foreach (var cell in step.RuledOut) {
            _known[cell.Row, cell.Column] = Known.NoPlant;
        }
    }
}
