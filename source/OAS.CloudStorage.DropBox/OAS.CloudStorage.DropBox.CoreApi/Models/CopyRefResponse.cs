#region Using Statements

using System;

#endregion

namespace OAS.CloudStorage.DropBox.CoreApi.Models {
	public class CopyRefResponse {
		public string Copy_Ref { get; set; }
		public string Expires { get; set; }

		public DateTime ExpiresDate {
			get { return DateTime.Parse( this.Expires ); }
		}
	}
}