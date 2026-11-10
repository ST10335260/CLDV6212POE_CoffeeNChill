namespace CoffeeNChillGateway.Dtos;

/// <summary>A staff document stored in the staff-docs blob container.</summary>
public class DocumentInfoResponse
{
    public string FileName { get; set; } = string.Empty;
    public long SizeInBytes { get; set; }
    public DateTimeOffset? LastModified { get; set; }
}

/// <summary>Confirmation returned after a document upload.</summary>
public class UploadDocumentResponse
{
    public string Message { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long SizeInBytes { get; set; }
}
