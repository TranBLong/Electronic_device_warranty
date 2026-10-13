namespace Warranty.Dtos;

public sealed record RepairRequestAttachmentResponseDto(
    int Id,
    int RepairRequestId,
    string FileName,
    string ContentType,
    long FileSize,
    int UploadedById,
    string UploadedByName,
    DateTime UploadedAt);

public sealed record AttachmentDownloadDto(string FilePath, string FileName, string ContentType);
