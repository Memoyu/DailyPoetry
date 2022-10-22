using DailyPoetry.Library.Models;
using Moq;
using System.Linq.Expressions;

namespace DailyPoetry.UnitTest.Services;

public class PoetryStorageTest : IDisposable
{
    public PoetryStorageTest()
    {
        // 测试开始前进行数据库文件清理
        RemoveDatabaseFile();
    }

    /// <summary>
    /// Dispose 时进行测试创建的数据文件销毁（为了还原测试环境）
    /// </summary>
    public void Dispose() => RemoveDatabaseFile();

    /// <summary>
    /// 销毁数据库文件
    /// </summary>
    private void RemoveDatabaseFile() => File.Delete(PoetryStorage.PoetryDbPath);

    [Fact]
    public void IsInitialzed_Default()
    {
        // mock preferenceStorage 实例
        var preferenceStorageMock = new Mock<IPreferenceStorage>();
        // Setup preferenceStorage Get()的实现，并始终返回当前的版本号
        preferenceStorageMock
            .Setup(p => p.Get(PoetryStorageConstant.VersionKey, default(int)))
            .Returns(PoetryStorageConstant.Version);
        // 获得preferenceStorage实例
        var mockPreferenceStorage = preferenceStorageMock.Object;

        // 构建poetryStorage
        var poetryStorage = new PoetryStorage(mockPreferenceStorage);
        Assert.True(poetryStorage.IsInitialized);

        // 验证mock preferenceStorage 中的 Get 是否只被调用过一次
        preferenceStorageMock.Verify(p => p.Get(PoetryStorageConstant.VersionKey, default(int)), Times.Once);
    }

    [Fact]
    public async Task InitialzeAsync_Default()
    {
        var preferenceStorageMock = new Mock<IPreferenceStorage>();
        var mockPreferenceStorage = preferenceStorageMock.Object;
        var poetryStorage = new PoetryStorage(mockPreferenceStorage);

        // 确定数据库文件不存在
        Assert.False(File.Exists(PoetryStorage.PoetryDbPath));
        await poetryStorage.InitializeAsync();
        Assert.True(File.Exists(PoetryStorage.PoetryDbPath));

        // 验证mock preferenceStorage 中的 Set 是否只被调用过一次
        preferenceStorageMock.Verify(p => p.Set(PoetryStorageConstant.VersionKey, PoetryStorageConstant.Version), Times.Once);
    }

    [Fact]
    public async Task GetPoetryAsync_Default()
    {
        var poetryStorage = await GetInitializedPoetryStorage();
        var poetry = await poetryStorage.GetPoetryAsync(10001);
        Assert.Equal("临江仙 · 夜归临皋", poetry.Name);
        // 关闭数据连接（不关闭会导致删除数据库时提示文件占用异常）
        await poetryStorage.CloseAsync();
    }

    [Fact]
    public async Task GetPoetriesAsync_Default()
    {
        var poetryStorage = await GetInitializedPoetryStorage();
        var poetries = await poetryStorage.GetPoetriesAsync(
                    Expression.Lambda<Func<Poetry, bool>>(Expression.Constant(true),
                        Expression.Parameter(typeof(Poetry), "p")), 0, int.MaxValue);
        Assert.Equal(30, poetries.Count());
        await poetryStorage.CloseAsync();
    }

    /// <summary>
    /// 获取初始化后的PoetryStorage
    /// </summary>
    /// <returns></returns>
    private async Task<PoetryStorage> GetInitializedPoetryStorage()
    {
        var preferenceStorageMock = new Mock<IPreferenceStorage>();
        var mockPreferenceStorage = preferenceStorageMock.Object;
        var poetryStorage = new PoetryStorage(mockPreferenceStorage);
        await poetryStorage.InitializeAsync();
        return poetryStorage;
    }
}
