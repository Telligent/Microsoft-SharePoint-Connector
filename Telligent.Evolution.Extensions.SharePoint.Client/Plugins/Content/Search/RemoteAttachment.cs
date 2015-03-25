using System;
using System.IO;
using Telligent.Evolution.Api.Content.Search;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility.Storage.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.Components;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Plugins.Content.Search
{
    internal class RemoteAttachment : IAttachment
    {
        private readonly Stream fileStream;

        public RemoteAttachment() { }
        public RemoteAttachment(string fileName, Stream fileStream)
        {
            FileName = fileName;
            this.fileStream = fileStream;
            ContentType = Configuration.MimeTypeConfiguration.GetMimeType(FileName);
        }

        public int ApplicationContentTypeId { get; set; }
        public int ApplicationId { get; set; }
        public Telligent.Evolution.Components.ApplicationType ApplicationType { get; set; }
        public int ContentId { get; set; }
        public string ContentType { get; set; }
        public DateTime DateCreated { get; set; }
        public ICentralizedFile File { get; set; }
        public string FileName { get; set; }
        public string FriendlyFileName { get; set; }
        public bool HasDateCreated { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public bool IsImage { get; set; }
        public bool IsLegacy { get; set; }
        public bool IsRemote { get; set; }
        public AttachmentKey Key { get; set; }
        public long Length { get; set; }
        public Guid TemporaryId { get; set; }
        public string Url { get; set; }
        public int UserId { get; set; }

        public Stream OpenReadStream()
        {
            return fileStream;
        }

        public static string GetText(string webUrl, string fileName, string filePath, ICredentialsManager credentialsManager, int maxFileSizeInBytes, int bufferSizeKB = 16)
        {
            var remoteFileText = String.Empty;
            using (var remoteFile = Microsoft.SharePoint.Client.File.OpenBinaryDirect(new SPContext(webUrl, credentialsManager.Get(webUrl)), filePath))
            using (var dataStream = remoteFile.Stream)
            using (var memoryDataStream = new MemoryStream())
            {
                if (dataStream.CanRead)
                {
                    byte[] buffer = new byte[bufferSizeKB * 1024];
                    int readBytes = 0;
                    int memoryDataBytes = 0;
                    while ((readBytes = dataStream.Read(buffer, 0, buffer.Length)) > 0
                        && memoryDataBytes < maxFileSizeInBytes)
                    {
                        memoryDataStream.Write(buffer, 0, readBytes);
                        memoryDataBytes += readBytes;
                    }
                    if (memoryDataBytes < maxFileSizeInBytes)
                    {
                        memoryDataStream.Seek(0, SeekOrigin.Begin);
                        remoteFileText = SearchIndexingFormatter.GetText(new RemoteAttachment(fileName, memoryDataStream) { Length = memoryDataStream.Length });
                    }
                }
            }
            return remoteFileText;
        }
    }
}
