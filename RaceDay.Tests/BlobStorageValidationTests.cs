using RaceDay.API.Services;
using Xunit;

namespace RaceDay.Tests;

public class BlobStorageValidationTests
{
    [Fact]
    public void BlobService_IsRegisteredByInterface()
    {
        Assert.True(typeof(IBlobStorageService).IsAssignableFrom(typeof(BlobStorageService)));
    }

    [Fact]
    public void ImageUploadResult_StoresBlobMetadata()
    {
        var result = new BlobUploadResult("events/1/banner.png", "image/png");
        Assert.Equal("events/1/banner.png", result.BlobName);
        Assert.Equal("image/png", result.ContentType);
    }
}
