using System.Threading;
using System.Threading.Tasks;

namespace Deltanium
{
	public interface IBlockRepository
	{
		Task<Block> GetBlockAsync(uint256 blockId, CancellationToken cancellationToken);
	}
}
