using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deltanium
{
	public interface ITransactionSignature
	{
		byte[] ToBytes();
	}
}
