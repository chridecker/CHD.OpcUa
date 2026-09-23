using chd.OpcUa.Server.UnderlyingSystem;
using System;
using System.Collections.Generic;
using System.Text;

namespace chd.OpcUa.Server.Interfaces
{
    public interface IUnderlyingSystemManager
    {
        ValueTask<List<UnderlyingSystemSegment>> GetMainSegmentsAsync(CancellationToken cancellationToken);

        List<UnderlyingSystemSegment> FindSegmentsForBlock(string blockIdentifier);

        ValueTask<UnderlyingSystemSegment> FindSegmentByIdentifier(string identifier, CancellationToken cancellationToken);

        ValueTask<UnderlyingSystemBlock> FindBlockByIdentifier(string identifier, CancellationToken cancellationToken);

        ValueTask InitializeAsync(CancellationToken cancellationToken);
    }
}
