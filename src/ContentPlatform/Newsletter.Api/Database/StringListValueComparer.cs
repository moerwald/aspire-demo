using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Newsletter.Api.Database;

public class StringListValueComparer : ValueComparer<List<string>>
{
    public StringListValueComparer() : base(
        (c1, c2) => c1.SequenceEqual(c2),
        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
        c => c.ToList())
    {
    }
}
