using TradeHero.Core.Enums;

namespace TradeHero.Contracts.Repositories.Models;

public class StrategyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public TradeLogicType TradeLogicType { get; set; }
    public InstanceType InstanceType { get; set; }
    public string TradeLogicJson { get; set; }  = string.Empty;
    public string InstanceJson { get; set; }  = string.Empty;
    public bool IsActive { get; set; }
}