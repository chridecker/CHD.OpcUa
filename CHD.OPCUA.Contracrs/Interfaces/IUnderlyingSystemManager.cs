using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace chd.OpcUa.Contracts.Interfaces
{
    public interface IUnderlyingSystemManager<TSegment, TBlock, TMethod>
    where TSegment : class, ISystemElement
    where TBlock : class, ISystemElement
    where TMethod : class, ISystemElement

    {
        ValueTask<List<TSegment>> GetMainSegmentsAsync(CancellationToken cancellationToken);

        List<TSegment> FindSegmentsForBlock(string blockIdentifier);

        ValueTask<TSegment> FindSegmentByIdentifier(string identifier, CancellationToken cancellationToken);

        ValueTask<TBlock> FindBlockByIdentifier(string identifier, CancellationToken cancellationToken);

        ValueTask<TMethod> FindMethodByIdentifier(string identifier, CancellationToken cancellationToken);

        ValueTask InitializeAsync(CancellationToken cancellationToken);

        List<TBlock> FindBlocksForSegment(TSegment segment);
    }
}
