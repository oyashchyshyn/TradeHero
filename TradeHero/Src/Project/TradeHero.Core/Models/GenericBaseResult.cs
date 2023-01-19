using System.Diagnostics.CodeAnalysis;
using TradeHero.Core.Enums;

namespace TradeHero.Core.Models;

public class GenericBaseResult<T>
{
    [AllowNull]
    public T Data { get; }

    public ActionResult ActionResult { get; }

    public GenericBaseResult(T data)
    {
        Data = data;
        ActionResult = ActionResult.Success;
    }

    public GenericBaseResult(ActionResult actionResult)
    {
        if (actionResult == ActionResult.Success)
        {
            throw new Exception($"{ActionResult.Success} cannot be without data");
        }
        
        ActionResult = actionResult;
    }
}