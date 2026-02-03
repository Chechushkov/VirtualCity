using Microsoft.AspNetCore.Http;

namespace Excursion_GPT.Application.DTOs;

// Interface for file uploads
public interface IFormFile
{
    string ContentType { get; }
    string ContentDisposition { get; }
    IHeaderDictionary Headers { get; }
    long Length { get; }
    string Name { get; }
    string FileName { get; }

    void CopyTo(Stream target);
    Task CopyToAsync(Stream target, CancellationToken cancellationToken = default);
    Stream OpenReadStream();
}
