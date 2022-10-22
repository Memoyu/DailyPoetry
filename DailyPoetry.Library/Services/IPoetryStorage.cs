using DailyPoetry.Library.Models;
using System.Linq.Expressions;

namespace DailyPoetry.Library.Services;

public interface IPoetryStorage
{
    bool IsInitialized { get; }

    Task InitializeAsync();

    Task<Poetry> GetPoetryAsync(int id);

    Task<IEnumerable<Poetry>> GetPoetriesAsync(
        Expression<Func<Poetry, bool>> where, int skip, int take);
}
