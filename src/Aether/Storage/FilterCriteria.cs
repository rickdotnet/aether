using System.Linq.Expressions;

namespace Aether.Storage;

public abstract class FilterCriteria
{
    public static FilterCriteria<T> For<T>() => new();
    public static FilterCriteria<T> For<T>(Expression<Func<T, bool>> filter) => new(filter);
}

public sealed class FilterCriteria<T>
{
    public Expression<Func<T, bool>> Filter { get; private set; }
    public int SkipValue { get; private set; }
    public int TakeValue { get; private set; } = int.MaxValue;

    public FilterCriteria()
    {
        Filter = _ => true;
    }

    public FilterCriteria(Expression<Func<T, bool>> filter, int skip = 0, int take = int.MaxValue)
    {
        Filter = filter;
        SkipValue = skip;
        TakeValue = take;
    }

    public FilterCriteria<T> WithFilter(Expression<Func<T, bool>> filter)
    {
        Filter = filter;
        return this;
    }

    public FilterCriteria<T> Skip(int skip)
    {
        SkipValue = skip;
        return this;
    }

    public FilterCriteria<T> Take(int take)
    {
        TakeValue = take;
        return this;
    }
}