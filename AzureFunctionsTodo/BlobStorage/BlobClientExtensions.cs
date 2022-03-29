using Azure.Storage.Blobs;
using System;
using System.Threading.Tasks;

namespace AzureFunctionsTodo.BlobStorage;

internal static class BlobClientExtensions
{
    public static Task UploadTextAsync(this BlobClient client, string text)
    {
        var content = new BinaryData(text);
        return client.UploadAsync(content, true); // support overwrite as we use this to update blobs
    }

    public async static Task<string> DownloadTextAsync(this BlobClient client)
    {
        var res = await client.DownloadContentAsync();
        return res.Value.Content.ToString();
    }
}
