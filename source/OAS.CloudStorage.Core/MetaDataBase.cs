using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OAS.CloudStorage.Core {
	public abstract class MetaDataBase {
		public string Path { get; set; }
		public string Name { get; set; }
		public bool IsFolder { get; set; }
	}

	public abstract class FolderMetaDataBase : MetaDataBase {
		public ICollection<FolderMetaDataBase> Folders { get; set; }
		public ICollection<FileMetaDataBase> Files { get; set; }
	}

	public abstract class FileVersionMetaDataBase : MetaDataBase { }

	public abstract class FileMetaDataBase : MetaDataBase {
		public ICollection<FileVersionMetaDataBase> Versions { get; set; }
	}
}
