using DailyPoetry.Library.Models;
using SQLite;
using System.Linq.Expressions;

namespace DailyPoetry.Library.Services;

public class PoetryStorage : IPoetryStorage
{
    public const string DbName = "poetrydb.sqlite3";

    public static readonly string PoetryDbPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder
            .LocalApplicationData), DbName);

    private SQLiteAsyncConnection? _connection;
    private SQLiteAsyncConnection Connection =>
    _connection ??= new SQLiteAsyncConnection(PoetryDbPath);

    private readonly IPreferenceStorage _preferenceStorage;

    public PoetryStorage(IPreferenceStorage preferenceStorage)
    {
        _preferenceStorage = preferenceStorage;
    }

    /// <summary>
    /// 判断本地数据库是否已初始化
    /// 获取preference中记录的version 与 当前的做比对
    /// </summary>
    public bool IsInitialized =>
        _preferenceStorage.Get(PoetryStorageConstant.VersionKey, default(int)) == PoetryStorageConstant.Version;

    public async Task InitializeAsync()
    {
        // 打开本地诗词数据流
        await using var dbFileStream = new FileStream(PoetryDbPath, FileMode.OpenOrCreate);
        // 打开诗词资源数据流 使用 GetManifestResourceStream 需要为引入的poetrydb.sqlite3配置逻辑名称 在DailyPoetry.Library项目的.csproj配置文件中配置如下
        /* 
         * <EmbeddedResource Include="poetrydb.sqlite3">
         *      < LogicalName > poetrydb.sqlite3 </ LogicalName >
         * </ EmbeddedResource >
        */
        await using var dbAssetFileStream = typeof(PoetryStorage).Assembly.GetManifestResourceStream(DbName) ??
            throw new Exception($"Manifest not found: {DbName}");
        // 将dbAssetFileStream数据拷贝到dbFileStream中
        await dbAssetFileStream.CopyToAsync(dbFileStream);
        // 配置获取preference中记录的version
        _preferenceStorage.Set(PoetryStorageConstant.VersionKey, PoetryStorageConstant.Version);
    }

    public async Task<Poetry> GetPoetryAsync(int id) =>
         await Connection.Table<Poetry>().FirstOrDefaultAsync(p => p.Id == id);

    public async Task<IEnumerable<Poetry>> GetPoetriesAsync(Expression<Func<Poetry, bool>> where, int skip, int take) =>
        await Connection.Table<Poetry>().Where(where).Skip(skip).Take(take).ToListAsync();

    public async Task CloseAsync() => await Connection.CloseAsync();
}

public static class PoetryStorageConstant
{
    public const string VersionKey =
        nameof(PoetryStorageConstant) + "." + nameof(Version);

    public const int Version = 1;
}
