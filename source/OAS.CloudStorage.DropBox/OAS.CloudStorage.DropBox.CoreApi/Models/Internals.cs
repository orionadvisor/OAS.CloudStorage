#region Using Statements

using System;
using System.Collections.Generic;

#endregion

namespace OAS.CloudStorage.DropBox.CoreApi.Models {
	internal class DeltaPageInternal {
		public string Cursor { get; set; }
		public bool Has_More { get; set; }
		public bool Reset { get; set; }
		public List<List<object>> Entries { get; set; }
	}

	internal class DropBoxMetaDataInternal {
		public string Hash { get; set; }
		public bool Thumb_Exists { get; set; }
		public long Bytes { get; set; }
		public DateTime? Modified { get; set; }
		public string Path { get; set; }
		public bool Is_Dir { get; set; }
		public bool Is_Deleted { get; set; }
		public string Size { get; set; }
		public string Root { get; set; }
		public string Icon { get; set; }
		public int Revision { get; set; }
		public string Rev { get; set; }
		public List<DropBoxMetaDataInternal> Contents { get; set; }
	}

	internal class OAuth2AccessToken {
		/// <summary>
		/// </summary>
		public string Access_Token { get; set; }

		public string Token_Type { get; set; }
		public string Uid { get; set; }
	}
}