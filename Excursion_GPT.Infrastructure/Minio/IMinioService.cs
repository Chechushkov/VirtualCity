namespace Excursion_GPT.Infrastructure.Minio;

public interface IMinioService
{
    Task CreateBucketIfNotExistAsync(string bucketName);
    Task UploadFileAsync(string objectName, Stream fileStream, string contentType);
    Task<(Stream Stream, string ContentType)> DownloadFileAsync(string objectName);
    Task RemoveFileAsync(string objectName);
}
