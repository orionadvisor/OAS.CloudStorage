using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OAS.CloudStorage.Core {
	public struct Range<T> {
		public T End;
		public T Start;
	}
}
