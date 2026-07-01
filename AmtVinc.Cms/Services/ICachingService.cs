namespace AmtVinc.Cms.Services;

/// <summary>Basit, önek bazlı temizlenebilen bellek cache sözleşmesi.</summary>
public interface ICachingService
{
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? duration = null);
    void Remove(string key);
    void RemoveByPrefix(string prefix);
}
