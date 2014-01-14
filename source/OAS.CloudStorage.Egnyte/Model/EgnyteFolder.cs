#region Using Statements

using System;
using System.Collections.Generic;
using OAS.CloudStorage.Core;

#endregion

namespace OAS.CloudStorage.Egnyte.Models {
	public class EgnyteFolder : FolderMetaDataBase {
		public string FolderId { get; set; }
	}
}