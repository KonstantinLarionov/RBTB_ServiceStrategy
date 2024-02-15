namespace RBTB_ServiceStrategy.Domain.States;

public class LevelStrategyState
{
    private decimal priceNow;
    private decimal volumeNow;
    private string? levels;
    private string? levelsUse;
    private string? positionNow;
    private bool isUpTrend = false;

    public bool IsUpTrend { get => isUpTrend; set { isUpTrend = value; } }
    public decimal PriceNow { get => priceNow; set { priceNow = value;  } }
    public decimal VolumeNow { get => volumeNow; set { volumeNow = value;  } }
    public string Levels { get => levels!; set { levels = value;  } }
    public string LevelsUse { get => levelsUse!; set { levelsUse = value; } }
    public string PositionNow { get => positionNow!; set { positionNow = value;  } }
    
    public decimal PriceLevel { get; set; }
    public bool IsPositionStay { get; set; } = false;
    public long IdTakeProfit { get; set; } = 0;
    public decimal TakeProfitVolume { get; set; } = 0;
    public decimal TakeProfitPrice { get; set; } = 0;
    public decimal PositionStartPrice { get; set; }
    public decimal PositionVolume { get; set; }
    public decimal PositionOutPrice { get; set; }
}