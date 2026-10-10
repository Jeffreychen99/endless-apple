using Foundation;
using Security;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Endless.Apple.iOS;

/// <summary>One opt-in account, restricted to this device's unlocked Keychain.</summary>
internal static class RememberedAccount
{
    internal sealed record Credentials(string Username, string Password);
    private const string DefaultService = "dev.endlessapple.client.remembered-login";
    private static SecRecord Query(string service) => new(SecKind.GenericPassword)
    {
        Service = service,
        Account = "official-server"
    };

    public static Credentials? Read(string service = DefaultService)
    {
        using var query = Query(service);
        using var record = SecKeyChain.QueryAsRecord(query, out var result);
        if (result != SecStatusCode.Success || record?.ValueData == null) return null;
        var bytes = record.ValueData.ToArray();
        try
        {
            var saved = JsonSerializer.Deserialize(bytes, AccountJsonContext.Default.Credentials);
            return saved != null && Valid(saved.Username) && Valid(saved.Password) ? saved : null;
        }
        catch (JsonException) { return null; }
        finally { Array.Clear(bytes); }
    }

    private static bool Valid(string? value) => value is { Length: >= 1 and <= 16 }
        && value.All(c => c >= 32 && c <= 126);

    public static bool Save(string username, string password, string service = DefaultService)
    {
        if (!Valid(username) || !Valid(password)) return false;
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new Credentials(username, password), AccountJsonContext.Default.Credentials);
        try
        {
            using var data = NSData.FromArray(bytes);
            using var query = Query(service);
            using var attributes = new SecRecord { ValueData = data, Accessible = SecAccessible.WhenUnlockedThisDeviceOnly };
            var result = SecKeyChain.Update(query, attributes);
            if (result != SecStatusCode.ItemNotFound) return result == SecStatusCode.Success;
            query.ValueData = data;
            query.Accessible = SecAccessible.WhenUnlockedThisDeviceOnly;
            return SecKeyChain.Add(query) == SecStatusCode.Success;
        }
        finally { Array.Clear(bytes); }
    }

#if DEBUG
    internal static bool SelfTest()
    {
        const string service = "dev.endlessapple.client.keychain-self-test";
        try
        {
            if (!Forget(service) || Read(service) != null) return false;
            if (Save("", "test-only", service) || Read(service) != null) return false;
            if (!Save("synthetic", "test-only", service)) return false;
            if (Read(service) != new Credentials("synthetic", "test-only")) return false;
            if (!Save("replacement", "changed", service)) return false;
            if (Read(service) != new Credentials("replacement", "changed")) return false;
            return Forget(service) && Read(service) == null && Forget(service);
        }
        finally { Forget(service); }
    }
#endif

    public static bool Forget(string service = DefaultService)
    {
        using var query = Query(service);
        return SecKeyChain.Remove(query) is SecStatusCode.Success or SecStatusCode.ItemNotFound;
    }
}

[JsonSerializable(typeof(RememberedAccount.Credentials))]
internal partial class AccountJsonContext : JsonSerializerContext { }
