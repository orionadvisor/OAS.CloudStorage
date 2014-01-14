#region Using Statements

using System;
using System.Collections.Generic;
using OAS.CloudStorage.Core;

#endregion

namespace OAS.CloudStorage.DropBox.CoreApi.Models {
	public class DropBoxFolder : FolderMetaDataBase {
		public string Checksum { get; set; }
		public DateTime? LastModified { get; set; }
		public long NumberOfBytes { get; set; }
		public bool ThumbExists { get; set; }
		public bool IsDeleted { get; set; }
		public string Size { get; set; }
		public string Root { get; set; }
		public string Icon { get; set; }
		public int Revision { get; set; }
	}
}