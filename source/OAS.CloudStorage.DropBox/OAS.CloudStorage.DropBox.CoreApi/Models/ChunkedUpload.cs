#region Using Statements

using System;

#endregion

namespace OAS.CloudStorage.DropBox.CoreApi.Models {
	public class ChunkedUpload {
		public string Upload_Id { get; set; }
		public long Offset { get; set; }
		public string Expires { get; set; }

		public DateTime ExpiresDate {
			get { return this.Expires == null ? DateTime.MinValue : DateTime.Parse( this.Expires ); }
		}
	}
}