using Excursion_GPT.Domain.Common;
using Microsoft.Extensions.Configuration;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace Excursion_GPT.Infrastructure.Minio;

 public class MinioService : IMinioService
    {
        private readonly IMinioClient _minioClient;
        private readonly string _bucketName;

        public MinioService(IMinioClient minioClient, IConfiguration configuration)
        {
            _minioClient = minioClient;
            _bucketName = configuration["Minio:BucketName"] ?? throw new ArgumentNullException("Minio:BucketName not configured.");
        }

        public async Task CreateBucketIfNotExistAsync(string bucketName)
        {
            try
            {
                var beArgs = new BucketExistsArgs().WithBucket(bucketName);
                bool found = await _minioClient.BucketExistsAsync(beArgs).ConfigureAwait(false);
                if (!found)
                {
                    var mbArgs = new MakeBucketArgs().WithBucket(bucketName);
                    await _minioClient.MakeBucketAsync(mbArgs).ConfigureAwait(false);
                    Console.WriteLine($"Bucket '{bucketName}' created successfully.");
                }
            }
            catch (MinioException e)
            {
                Console.WriteLine($"[Minio] Error occurred when creating bucket: {e.Message}");
                throw new UploadFailedException("minio", $"Failed to initialize MinIO bucket: {e.Message}");
            }
        }

        public async Task UploadFileAsync(string objectName, Stream fileStream, string contentType)
        {
            try
            {
                bool found = await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucketName));
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(objectName)
                    .WithStreamData(fileStream)
                    .WithObjectSize(fileStream.Length)
                    .WithContentType(contentType);

                var res = await _minioClient.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
                Console.WriteLine($"Successfully uploaded {objectName} to bucket {_bucketName}");
            }
            catch (MinioException e)
            {
                Console.WriteLine($"[Minio] Error occurred when uploading file: {e.Message}");
                throw new UploadFailedException("minio", $"Failed to upload file to MinIO: {e.Message}");
            }
        }
        public async Task<(Stream Stream, string ContentType)> DownloadFileAsync(string objectName)
        {
            try
            {
                var statObjectArgs = new StatObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(objectName);
                var stat = await _minioClient.StatObjectAsync(statObjectArgs).ConfigureAwait(false);

                var stream = new MemoryStream();
                var getObjectArgs = new GetObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(objectName)
                    .WithCallbackStream((s) => s.CopyTo(stream));

                await _minioClient.GetObjectAsync(getObjectArgs).ConfigureAwait(false);
                stream.Position = 0; // Reset stream position for reading
                return (stream, stat.ContentType);
            }
            catch (MinioException e)
            {
                Console.WriteLine($"[Minio] Error occurred when downloading file: {e.Message}");
                throw new NotFoundException("minio", $"File '{objectName}' not found or inaccessible in MinIO: {e.Message}");
            }
        }

        public async Task RemoveFileAsync(string objectName)
        {
            try
            {
                var removeObjectArgs = new RemoveObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(objectName);
                await _minioClient.RemoveObjectAsync(removeObjectArgs).ConfigureAwait(false);
                Console.WriteLine($"Successfully removed {objectName} from bucket {_bucketName}");
            }
            catch (MinioException e)
            {
                Console.WriteLine((object?)$"[Minio] Error occurred when removing file: {e.Message}");
                throw new UploadFailedException("minio", $"Failed to remove file from MinIO: {e.Message}");
            }
        }
    }
