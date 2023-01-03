using TradeHero.Contracts.Base.Enums;

namespace TradeHero.Database.Entities;

internal class Strategy
{
    public Strategy()
    {
        Id = Guid.NewGuid();
    }
    
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public TradeLogicType TradeLogicType { get; set; }
    public InstanceType InstanceType { get; set; }
    public string TradeLogicJson { get; set; }  = string.Empty;
    public string InstanceJson { get; set; }  = string.Empty;
    public bool IsActive { get; set; }
}