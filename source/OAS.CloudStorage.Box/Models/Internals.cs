using System;
using System.Collections.Generic;

namespace OAS.CloudStorage.Box.Models {
	internal class BoxItemInternal {
		public string Type { get; set; }
		public string Id { get; set; }
		public int? Sequence_Id { get; set; }
		public string ETag { get; set; }
		public string SHA1 { get; set; }
		public string Name { get; set; }
		public DateTime? Created_At { get; set; }
		public DateTime? Modified_At { get; set; }
		public DateTime? Trashed_At { get; set; }
		public DateTime? Purged_At { get; set; }
		public DateTime? Content_Created_At { get; set; }
		public DateTime? Content_Modified_At { get; set; }
		public string Description { get; set; }
		public long? Size { get; set; }
		public PathCollectionInternal Path_Collection { get; set; }
		public BoxUserInternal Created_By { get; set; }
		public BoxUserInternal Modified_By { get; set; }
		public BoxUserInternal Owned_By { get; set; }

		public BoxItemInternal Parent { get; set; }
		public string Item_Status { get; set; }
		public ItemCollectionInternal Item_Collection { get; set; }
	}

	internal class BoxFileInternal : BoxItemInternal { }

	internal class BoxFolderInternal : BoxItemInternal { }

	internal class PathCollectionInternal {
		public int Total_Count { get; set; }
		public List<BoxPathEntryInternal> Entries { get; set; }
	}

	internal class BoxPathEntryInternal {
		public string Type { get; set; }
		public int Id { get; set; }
		public int? Sequence_Id { get; set; }
		public string ETag { get; set; }
		public string Name { get; set; }
	}

	internal class ItemCollectionInternal {
		public int Total_Count { get; set; }
		public List<BoxItemInternal> Entries { get; set; }
	}

	internal class BoxUserInternal {
		public string Type { get; set; }
		public int? Id { get; set; }
		public string Name { get; set; }
		public string Login { get; set; }
	}

	internal class BoxErrrorTypeInternal {
		public string reason { get; set; }
		public string name { get; set; }
		public string message { get; set; }
	}

	internal class ContextInfoTypeInternal {
		public List<BoxErrrorTypeInternal> errors { get; set; }
	}

	internal class BoxExceptionMessageInternal {
		public string type { get; set; }
		public int status { get; set; }
		public string code { get; set; }
		public string request_id { get; set; }
		public string message { get; set; }
		public string help_url { get; set; }
		public ContextInfoTypeInternal context_info { get; set; }
	}

	internal class BoxRefreshTokenResultInternal {
		public string access_token { get; set; }
		public string refresh_token { get; set; }
		public int expires_in { get; set; }
		public string token_type { get; set; }
	}
}
