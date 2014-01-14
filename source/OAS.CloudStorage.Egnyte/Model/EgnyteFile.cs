#region Using Statements

using System;
using System.Collections.Generic;
using OAS.CloudStorage.Core;

#endregion

namespace OAS.CloudStorage.Egnyte.Models {
	public class EgnyteFile : FileMetaDataBase {
		public string EntryId { get; set; }
		public string Checksum { get; set; }
		public DateTime LastModified { get; set; }
		public string UploadedBy { get; set; }
		public long NumberOfBytes { get; set; }
	}

	public class EgnyteFileVersion : FileVersionMetaDataBase {
		public string EntryId { get; set; }
		public string Checksum { get; set; }
		public DateTime LastModified { get; set; }
		public string UploadedBy { get; set; }
		public long NumberOfBytes { get; set; }
	}
}