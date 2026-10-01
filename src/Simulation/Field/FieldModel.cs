using DesktopFarmIdle.Core.Config;

namespace DesktopFarmIdle.Simulation.Field;

public class FieldCell
{
    public int Index { get; set; }
    public SoilState State { get; set; } = SoilState.Stubble;
    public CropType CurrentCrop { get; set; } = CropType.Wheat;
    public float GrowthProgress { get; set; } = 0f; // 0.0 to 1.0
    public float Moisture { get; set; } = 0.6f;     // 0.0 to 1.0
    public float Nutrients { get; set; } = 0.8f;    // 0.0 to 1.0
    public float Health { get; set; } = 1.0f;       // 0.0 to 1.0
    public float Weeds { get; set; } = 0.0f;        // 0.0 to 1.0
}

public class FieldModel
{
    public int Id { get; set; } = 1;
    public string Name { get; set; } = "Main North Field";
    public int CellCount { get; set; } = GameBalanceConfig.DefaultFieldCellCount;
    public FieldCell[] Cells { get; }

    public FieldModel(int id = 1, int cellCount = GameBalanceConfig.DefaultFieldCellCount)
    {
        Id = id;
        CellCount = cellCount;
        Cells = new FieldCell[cellCount];
        for (int i = 0; i < cellCount; i++)
        {
            Cells[i] = new FieldCell
            {
                Index = i,
                State = SoilState.Stubble,
                Moisture = 0.6f,
                Nutrients = 0.8f,
                Health = 1.0f
            };
        }
    }
}
