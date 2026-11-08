using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.API.Services;

public sealed record BlobUploadResult(string BlobName, string ContentType);

public interface IBlobStorageService
{
    Task<BlobUploadResult> UploadImageAsync(Stream content, long length, string contentType, string folder, CancellationToken cancellationToken = default);
    Task DeleteAsync(string? blobName, CancellationToken cancellationToken = default);
    Task<(Stream Content, string ContentType)?> OpenReadAsync(string blobName, string contentType, CancellationToken cancellationToken = default);
}

public sealed class BlobStorageService(IConfiguration configuration) : IBlobStorageService
{
    private const long MaxFileSize = 5 * 1024 * 1024;
    private static readonly IReadOnlyDictionary<string, byte[]> Signatures = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = [0xFF, 0xD8, 0xFF],
        ["image/png"] = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
        ["image/webp"] = [0x52, 0x49, 0x46, 0x46]
    };

    private BlobContainerClient Container => CreateContainer();

    public async Task<BlobUploadResult> UploadImageAsync(Stream content, long length, string contentType, string folder, CancellationToken cancellationToken = default)
    {
        if (length <= 0 || length > MaxFileSize) throw new InvalidDataException("Image must be between 1 byte and 5 MB.");
        if (!Signatures.ContainsKey(contentType)) throw new InvalidDataException("Only JPEG, PNG, and WebP images are supported.");
        if (!await HasValidSignatureAsync(content, contentType, cancellationToken)) throw new InvalidDataException("The uploaded file is not a valid image.");

        var blobName = $"{folder.Trim('/')}/{Guid.NewGuid():N}{ExtensionFor(contentType)}";
        var blob = Container.GetBlobClient(blobName);
        if (content.CanSeek) content.Position = 0;
        await blob.UploadAsync(content, new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } }, cancellationToken);
        return new BlobUploadResult(blobName, contentType);
    }

    public async Task DeleteAsync(string? blobName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(blobName)) return;
        await Container.DeleteBlobIfExistsAsync(blobName, Azure.Storage.Blobs.Models.DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken);
    }

    public async Task<(Stream Content, string ContentType)?> OpenReadAsync(string blobName, string contentType, CancellationToken cancellationToken = default)
    {
        var blob = Container.GetBlobClient(blobName);
        if (!await blob.ExistsAsync(cancellationToken)) return null;
        var response = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
        return (response.Value.Content, contentType);
    }

    private BlobContainerClient CreateContainer()
    {
        var connectionString = configuration["AzureBlobStorage:ConnectionString"];
        var containerName = configuration["AzureBlobStorage:ContainerName"] ?? "raceday-images";
        if (string.IsNullOrWhiteSpace(connectionString)) throw new InvalidOperationException("Azure Blob Storage is not configured.");
        return new BlobServiceClient(connectionString).GetBlobContainerClient(containerName);
    }

    private static async Task<bool> HasValidSignatureAsync(Stream content, string contentType, CancellationToken cancellationToken)
    {
        var signature = Signatures[contentType];
        if (!content.CanSeek) return false;
        content.Position = 0;
        var header = new byte[signature.Length];
        var read = await content.ReadAsync(header, cancellationToken);
        content.Position = 0;
        if (read < signature.Length) return false;
        if (contentType == "image/webp" && !header.AsSpan().SequenceEqual(new byte[] { 0x52, 0x49, 0x46, 0x46 })) return false;
        return header.AsSpan().SequenceEqual(signature);
    }

    private static string ExtensionFor(string contentType) => contentType switch
    {
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/webp" => ".webp",
        _ => ".img"
    };
}