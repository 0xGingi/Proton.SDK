namespace Proton.Sdk.Cryptography;

public record struct SecretCollectionCacheItem(string EntityId, ReadOnlyMemory<byte> Bytes);
