using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Warranty.Dtos;
using Warranty.Enums;
using Warranty.Exceptions;
using Warranty.Models;
using Warranty.Repositories.Interfaces.Complete;
using Warranty.Repositories.Interfaces.Important;
using Warranty.Services.Interfaces.Complete;

namespace Warranty.Services.Implementations.Complete;

public sealed class RepairRequestAttachmentService(
    IRepairRequestAttachmentRepository attachmentRepository,
    IRepairRequestRepository repairRequestRepository,
    IConfiguration configuration,
    IWebHostEnvironment environment) : IRepairRequestAttachmentService
{
    private const long DefaultUploadLimit = 5 * 1024 * 1024;

    public async Task<RepairRequestAttachmentResponseDto> UploadAsync(
        int repairRequestId,
        IFormFile file,
        int userId,
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        var repairRequest = await repairRequestRepository.GetByIdAsync(repairRequestId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException("Repair request was not found.");
        EnsureCanAccess(repairRequest, userId, role, upload: true);
        var uploadConfig = GetUploadConfig();
        ValidateFile(file, uploadConfig);

        var originalName = Path.GetFileName(file.FileName.Replace('\\', '/'));
        if (originalName.Length > 255)
        {
            throw new BusinessRuleException("File name must not exceed 255 characters.");
        }

        var storedName = $"{Guid.NewGuid():N}{Path.GetExtension(originalName).ToLowerInvariant()}";
        var relativePath = storedName;
        var directory = GetUploadDirectory(uploadConfig.RootPath);
        Directory.CreateDirectory(directory);
        var fullPath = Path.Combine(directory, storedName);
        var attachment = new RepairRequestAttachment
        {
            RepairRequestId = repairRequestId,
            UploadedById = userId,
            FileName = originalName,
            FilePath = relativePath,
            ContentType = GetCanonicalContentType(Path.GetExtension(originalName)),
            FileSize = file.Length,
            UploadedAt = DateTime.UtcNow
        };

        try
        {
            await using (var destination = new FileStream(
                fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous))
            {
                await file.CopyToAsync(destination, cancellationToken);
            }

            await attachmentRepository.AddAsync(attachment, cancellationToken);
            await attachmentRepository.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            File.Delete(fullPath);
            throw;
        }

        var savedAttachment = await attachmentRepository.GetByIdAsync(attachment.Id, cancellationToken)
            ?? throw new InvalidOperationException("Saved attachment could not be loaded.");
        return ToResponse(savedAttachment, savedAttachment.UploadedBy.FullName);
    }

    public async Task<IReadOnlyList<RepairRequestAttachmentResponseDto>> GetByRepairRequestIdAsync(
        int repairRequestId,
        int userId,
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        var repairRequest = await repairRequestRepository.GetByIdAsync(repairRequestId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException("Repair request was not found.");
        EnsureCanAccess(repairRequest, userId, role, upload: false);
        return (await attachmentRepository.GetByRepairRequestIdAsync(repairRequestId, cancellationToken))
            .Select(attachment => ToResponse(attachment, attachment.UploadedBy.FullName))
            .ToList();
    }

    public async Task<AttachmentDownloadDto> GetDownloadAsync(
        int id,
        int userId,
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        var attachment = await attachmentRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Attachment was not found.");
        EnsureCanAccess(attachment.RepairRequest, userId, role, upload: false);
        var rootPath = GetUploadConfig().RootPath;
        var fullPath = Path.GetFullPath(Path.Combine(GetUploadDirectory(rootPath), attachment.FilePath));
        var uploadDirectory = GetUploadDirectory(rootPath) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(uploadDirectory, StringComparison.OrdinalIgnoreCase) || !File.Exists(fullPath))
        {
            throw new NotFoundException("Attachment file was not found.");
        }

        return new AttachmentDownloadDto(fullPath, attachment.FileName, attachment.ContentType);
    }

    private void ValidateFile(IFormFile file, UploadConfig config)
    {
        if (file is null || file.Length <= 0)
        {
            throw new BusinessRuleException("A non-empty attachment file is required.");
        }

        if (file.Length > config.MaxFileSizeBytes)
        {
            throw new BusinessRuleException($"Attachment exceeds the {config.MaxFileSizeBytes}-byte file size limit.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!config.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new BusinessRuleException("Only JPG, JPEG, PNG and PDF attachments are allowed.");
        }

        var expectedContentType = GetCanonicalContentType(extension);
        if (!string.Equals(file.ContentType, expectedContentType, StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessRuleException("Attachment content type does not match its file extension.");
        }

        using var stream = file.OpenReadStream();
        Span<byte> signature = stackalloc byte[8];
        var read = 0;
        while (read < signature.Length)
        {
            var bytesRead = stream.Read(signature[read..]);
            if (bytesRead == 0)
            {
                break;
            }

            read += bytesRead;
        }
        var validSignature = extension switch
        {
            ".jpg" or ".jpeg" => read >= 3 && signature[0] == 0xFF && signature[1] == 0xD8 && signature[2] == 0xFF,
            ".png" => read >= 8 && signature.SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            ".pdf" => read >= 5 && signature[..5].SequenceEqual("%PDF-"u8),
            _ => false
        };
        if (!validSignature)
        {
            throw new BusinessRuleException("Attachment file signature does not match its declared format.");
        }
    }

    private UploadConfig GetUploadConfig()
    {
        var maxFileSizeBytes = configuration.GetValue<long?>("Upload:MaxFileSizeBytes")
            ?? throw new InvalidOperationException("Upload:MaxFileSizeBytes must be configured.");
        var extensions = configuration.GetSection("Upload:AllowedExtensions").Get<string[]>()
            ?? throw new InvalidOperationException("Upload:AllowedExtensions must be configured.");
        var rootPath = configuration["Upload:RootPath"];
        if (maxFileSizeBytes <= 0 || extensions.Length == 0 || string.IsNullOrWhiteSpace(rootPath))
        {
            throw new InvalidOperationException("Upload settings are invalid.");
        }

        return new UploadConfig(maxFileSizeBytes, extensions, rootPath);
    }

    private string GetUploadDirectory(string configuredRoot)
    {
        var path = Path.IsPathRooted(configuredRoot)
            ? configuredRoot
            : Path.Combine(environment.ContentRootPath, configuredRoot);
        return Path.GetFullPath(path);
    }

    private static string GetCanonicalContentType(string extension) => extension.ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".pdf" => "application/pdf",
        _ => string.Empty
    };

    private static void EnsureCanAccess(RepairRequest repairRequest, int userId, UserRole role, bool upload)
    {
        if (upload)
        {
            if (role == UserRole.Receptionist ||
                (role == UserRole.Customer && repairRequest.WarrantyCard.CustomerId == userId) ||
                (role == UserRole.Technician && repairRequest.TechnicianId == userId))
            {
                return;
            }
        }
        else if (role is UserRole.Admin or UserRole.Manager or UserRole.Receptionist ||
                 (role == UserRole.Customer && repairRequest.WarrantyCard.CustomerId == userId) ||
                 (role == UserRole.Technician && repairRequest.TechnicianId == userId))
        {
            return;
        }

        throw new ForbiddenException("You are not allowed to access attachments for this repair request.");
    }

    private static RepairRequestAttachmentResponseDto ToResponse(RepairRequestAttachment attachment, string uploadedByName) => new(
        attachment.Id,
        attachment.RepairRequestId,
        attachment.FileName,
        attachment.ContentType,
        attachment.FileSize,
        attachment.UploadedById,
        uploadedByName,
        attachment.UploadedAt);

    private sealed record UploadConfig(long MaxFileSizeBytes, string[] AllowedExtensions, string RootPath);
}
