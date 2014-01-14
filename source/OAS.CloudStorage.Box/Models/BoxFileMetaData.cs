using System;
using OAS.CloudStorage.Core;

namespace OAS.CloudStorage.Box.Models {
	public class BoxFileMetaData : FileMetaDataBase  {
		public string Id { get; private set; }
		public string Sha1 { get; set; }
		public long Size { get; set; }
		public DateTime? ModifiedDate { get; set; }
		public bool IsDeleted { get; set; }
		public string Root { get; set; }
		public string Version { get; set; }

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

		internal BoxFileMetaData( BoxItemInternal fileInfo, ItemCollectionInternal versions ) :this(fileInfo ) {
			if( versions != null ) {
				//TODO: convert the version info to the external objects here
				//this.Versions = versions.Entries.Select(  );	
			}
		}

		internal BoxFileMetaData( BoxItemInternal fileInfo ) {
			this.Id = fileInfo.Id;
			this.Name = fileInfo.Name;
			this.Size = fileInfo.Size ?? 0;
			this.ModifiedDate = fileInfo.Modified_At;
			this.Path = Utilities.GetPath( fileInfo );
			this.IsFolder = false;
			this.IsDeleted = Utilities.IsItemDeleted( fileInfo );
			this.Root = "0";
			this.Version = fileInfo.ETag;
		}
	}
}
