using System;
using System.Collections.Generic;

namespace OAS.CloudStorage.Egnyte.Models {
	internal class EgnyteMetaDataInternal {
		public string Name { get; set; }
		public bool Is_Folder { get; set; }
		public string Folder_Id { get; set; }
		public string Entry_Id { get; set; }
		public string Checksum { get; set; }
		public DateTime Last_Modified { get; set; }
		public string Uploaded_By { get; set; }
		public int Size { get; set; }

		public ICollection<EgnyteFileVersionInternal> Versions { get; set; }
		public ICollection<EgnyteFolderInternal> Folders { get; set; }
		public ICollection<EgnyteFileInternal> Files { get; set; }
	}
	
	internal class EgnyteFileInternal {
		public string Name { get; set; }
		public bool Is_Folder { get; set; }
		public string Entry_Id { get; set; }
		public string Checksum { get; set; }
		public DateTime Last_Modified { get; set; }
		public string Uploaded_By { get; set; }
		public int Size { get; set; }
	}

	internal class EgnyteFileVersionInternal {
		public bool Is_Folder { get; set; }
		public string Entry_Id { get; set; }
		public string Checksum { get; set; }
		public DateTime Last_Modified { get; set; }
		public string Uploaded_By { get; set; }
		public int Size { get; set; }
	}

	internal class EgnyteFolderInternal {
		public string Name { get; set; }
		public bool Is_Folder { get; set; }
		public string Folder_Id { get; set; }
	}
}