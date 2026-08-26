using System;
using System.Collections.Generic;

namespace Deltanium.Core.Models
{
    /// <summary>
    /// Represents an encrypted block of a file.
    /// </summary>
    public class EncryptedFileBlock
    {
        /// <summary>
        /// Index of this block in the file
        /// </summary>
        public int BlockIndex { get; set; }

        /// <summary>
        /// Total number of blocks in the file
        /// </summary>
        public int TotalBlocks { get; set; }

        /// <summary>
        /// Original size of the block before encryption
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// Size of the block after encryption
        /// </summary>
        public int EncryptedSize { get; set; }

        /// <summary>
        /// Encrypted content of the block
        /// </summary>
        public byte[] EncryptedContent { get; set; }
    }

    /// <summary>
    /// Represents fully processed file data ready for blockchain storage.
    /// </summary>
    public class ProcessedFileData
    {
        /// <summary>
        /// Unique identifier for the file (Merkle root of block hashes)
        /// </summary>
        public string FileId { get; set; }

        /// <summary>
        /// Original file name
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Size of the original file in bytes
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// MIME type of the file
        /// </summary>
        public string MimeType { get; set; }

        /// <summary>
        /// File extension
        /// </summary>
        public string FileExtension { get; set; }

        /// <summary>
        /// When the file was processed
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// Owner's public key
        /// </summary>
        public byte[] OwnerPubKey { get; set; }

        /// <summary>
        /// Version of the file processing format
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// Number of blocks the file is split into
        /// </summary>
        public int BlockCount { get; set; }

        /// <summary>
        /// List of encrypted blocks
        /// </summary>
        public List<EncryptedFileBlock> EncryptedBlocks { get; set; } = new List<EncryptedFileBlock>();

        /// <summary>
        /// List of content IDs for each block (SHA-256 hashes)
        /// </summary>
        public List<string> BlockContentIds { get; set; } = new List<string>();

        /// <summary>
        /// List of block IDs used by the store
        /// </summary>
        public List<string> BlockIds { get; set; } = new List<string>();

        /// <summary>
        /// Dictionary of encrypted keys for users who have access.
        /// Key: Public key bytes (Base64 encoded), Value: Symmetric key encrypted with that public key
        /// </summary>
        public Dictionary<string, byte[]> EncryptedKeysForPubKeys { get; set; } = new Dictionary<string, byte[]>();
    }
} 