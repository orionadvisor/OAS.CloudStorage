
namespace OAS.CloudStorage.Box.Models {
	internal static class Utilities {
		public static bool IsItemDeleted( BoxItemInternal folderInfo ) {
			return ( !string.IsNullOrWhiteSpace( folderInfo.Item_Status ) ) && ( folderInfo.Item_Status.ToLowerInvariant( ) == "deleted" );
		}

		public static string GetPath( BoxItemInternal folderInfo ) {
			if( folderInfo.Id == "0" )
				return "/";

			if( ( folderInfo.Path_Collection == null ) || ( folderInfo.Path_Collection.Entries == null ) )
				return null;

			string result = "/";

			for( int i = 1; i < folderInfo.Path_Collection.Entries.Count; ++i ) {
				result += folderInfo.Path_Collection.Entries[ i ].Name + "/";
			}

			return result + folderInfo.Name;
		}
	}
}
