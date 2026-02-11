using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Excursion_GPT.Domain.Entities;

[Table("model_files")]
public class ModelFile
{
    [Column("id")]
    public Guid Id { get; set; }

    [Column("minio_object_name")]
    public string MinioObjectName { get; set; } = string.Empty;

    [Column("original_file_name")]
    public string OriginalFileName { get; set; } = string.Empty;

    [Column("content_type")]
    public string ContentType { get; set; } = string.Empty;

    [Column("file_size")]
    public long FileSize { get; set; }

    [Column("uploaded_at")]
    public DateTime UploadedAt { get; set; }
}
