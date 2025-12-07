namespace PathRAG.NET.Core.Services;

public interface IOrphanEntityRepairService
{
    Task<OrphanEntityRepairResult> RepairAsync(Guid documentId, CancellationToken cancellationToken = default);
}
