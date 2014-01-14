using System;
using System.Collections.Generic;
using OAS.CloudStorage.Core;

namespace OAS.CloudStorage.Box.Models {
	public class BoxFolderMetaData : FolderMetaDataBase {
		public string Id { get; private set; }
		public string Sha1 { get; set; }
		public long Size { get; set; }
		public DateTime? ModifiedDate { get; set; }
		public bool IsDeleted { get; set; }
		public string Root { get; set; }

		public string Modified {
			get {
				return ( ( this.ModifiedDate != null ) ? this.ModifiedDate.Value.ToString( "yyyy-MM-dd" ) : string.Empty );
			}

			set {
				DateTime modifiedDate;

				if( !string.IsNullOrWhiteSpace( value ) && ( DateTime.TryParse( value, out modifiedDate ) ) ) {
					this.ModifiedDate = modifiedDate;
				} else {
					this.ModifiedDate = null;
				}
			}
		}

		public string Extension {
			get {
				int extensionStartPosition = this.Name.LastIndexOf( '.' );

				return ( ( ( extensionStartPosition >= 0 ) && ( ++extensionStartPosition < this.Name.Length ) ) ? this.Name.Substring( extensionStartPosition ) : string.Empty );
			}
		}

		public string SizeString {
			get {
				return ExtensionMethods.GetFileSizeInHumanReadableFormat( this.Size );
			}
		}

		internal BoxFolderMetaData( BoxItemInternal folderInfo ) {
			this.Id = folderInfo.Id;
			this.Name = ( this.Id != "0" ) ? folderInfo.Name : string.Empty;
			this.Size = folderInfo.Size ?? 0;
			this.Path = Utilities.GetPath( folderInfo );
			this.IsFolder = true;
			this.IsDeleted = Utilities.IsItemDeleted( folderInfo );
			this.Root = "0";

			string concat = this.Path != null && this.Path.EndsWith( "/" ) ? "{0}{1}" : "{0}/{1}";
			if( ( folderInfo.Item_Collection != null ) && ( folderInfo.Item_Collection.Entries != null ) ) {
				var thisFiles = new List<BoxFileMetaData>( );
				var thisFolders = new List<BoxFolderMetaData>( );

				foreach( var item in folderInfo.Item_Collection.Entries ) {
					if( item.Type.ToLowerInvariant( ) == "file" ) {
						var file = new BoxFileMetaData( item );
						file.Path = string.Format( concat, this.Path, file.Name );
						thisFiles.Add( file );
					} else {
						var folder = new BoxFolderMetaData( item );
						folder.Path = string.Format( concat, this.Path, folder.Name );
						thisFolders.Add( folder );
					}
				}

				this.Files = thisFiles.ConvertAll( x => (FileMetaDataBase) x );
				this.Folders = thisFolders.ConvertAll( x => (FolderMetaDataBase) x );
			} else {
				this.Folders = null;
				this.Files = null;
			}
		}
	}
}
