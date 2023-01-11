namespace TradeHero.Core.Extensions;

public static class LinqExtensions
{
    public static IEnumerable<TEntity> WhereIf<TEntity>(this IEnumerable<TEntity> source, bool condition, Func<TEntity, bool> func)
    {
        return condition ? source.Where(func) : source;
    }
}