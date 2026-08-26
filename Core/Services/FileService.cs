using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Deltanium.Core.Cryptography;
using Deltanium.Core.Models;
using NBitcoin;

namespace Deltanium.Core.Services
{
    /// <summary>
    /// Service for handling file operations in the Deltanium blockchain network.
    /// Implements the file processing logic described in DetailProcess.md.
    /// </summary>
    public class FileService : IFileService
    {
        public const int DEFAULT_BLOCK_SIZE = 4 * 1024 * 1024; // 4MB
        private readonly ICryptographyService _cryptographyService;
        private readonly IHashService _hashService;

        public FileService(ICryptographyService cryptographyService, IHashService hashService)
        {
            _cryptographyService = cryptographyService ?? throw new ArgumentNullException(nameof(cryptographyService));
            _hashService = hashService ?? throw new ArgumentNullException(nameof(hashService));
        }

        /// <summary>
        /// Processes a file for storage on the blockchain, splitting it into blocks,
        /// encrypting each block, and creating a merkle tree for verification.
        /// </summary>
        /// <param name="fileStream">The file data stream</param>
        /// <param name="fileName">Name of the file</param>
        /// <param name="ownerPubKey">Public key of the file owner</param>
        /// <param name="sharedWithPubKeys">Public keys of users to share the file with</param>
        /// <param name="blockSize">Size of each block</param>
        /// <returns>Processed file data ready for blockchain storage</returns>
        public async Task<ProcessedFileData> ProcessFileForBlockchainAsync(
            Stream fileStream, 
            string fileName, 
            byte[] ownerPubKey, 
            byte[][] sharedWithPubKeys = null, 
            int blockSize = DEFAULT_BLOCK_SIZE)
        {
            if (fileStream == null) throw new ArgumentNullException(nameof(fileStream));
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentException("File name cannot be null or empty", nameof(fileName));
            if (ownerPubKey == null) throw new ArgumentNullException(nameof(ownerPubKey));
            if (blockSize <= 0) throw new ArgumentException("Block size must be greater than zero", nameof(blockSize));

            // Create result object
            var result = new ProcessedFileData
            {
                FileName = fileName,
                FileSize = fileStream.Length,
                OwnerPubKey = ownerPubKey,
                CreationTime = DateTime.UtcNow,
                MimeType = GetMimeTypeFromFileName(fileName),
                FileExtension = Path.GetExtension(fileName),
                EncryptedBlocks = new List<EncryptedFileBlock>(),
                BlockContentIds = new List<string>(),
                EncryptedKeysForPubKeys = new Dictionary<string, byte[]>()
            };

            // Calculate how many blocks we need
            result.BlockCount = (int)Math.Ceiling((double)fileStream.Length / blockSize);
            
            // Reset stream position
            fileStream.Position = 0;

            // Generate a random symmetric key for this file
            byte[] symmetricKey = new byte[32]; // 256-bit key
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(symmetricKey);
            }

            // Process blocks
            byte[] buffer = new byte[blockSize];
            List<string> contentIds = new List<string>();
            List<EncryptedFileBlock> encryptedBlocks = new List<EncryptedFileBlock>();

            for (int blockIndex = 0; blockIndex < result.BlockCount; blockIndex++)
            {
                int bytesRead = await fileStream.ReadAsync(buffer, 0, blockSize);
                
                if (bytesRead < blockSize)
                {
                    // Resize buffer for last block
                    Array.Resize(ref buffer, bytesRead);
                }

                // Calculate hash for this block
                string contentId = _hashService.ComputeSha256Hash(buffer);
                contentIds.Add(contentId);

                // Encrypt block with symmetric key
                byte[] encryptedContent = await _cryptographyService.EncryptDataAsync(buffer, symmetricKey);

                // Add encrypted block
                encryptedBlocks.Add(new EncryptedFileBlock
                {
                    BlockIndex = blockIndex,
                    TotalBlocks = result.BlockCount,
                    Size = buffer.Length,
                    EncryptedSize = encryptedContent.Length,
                    EncryptedContent = encryptedContent
                });
            }

            // Add blocks to result
            result.BlockContentIds = contentIds;
            result.EncryptedBlocks = encryptedBlocks;

            // Calculate file ID (Merkle root of content IDs)
            result.FileId = CalculateMerkleRoot(contentIds);

            // Encrypt symmetric key for each user who should have access (including owner)
            var allPubKeys = new HashSet<byte[]>(sharedWithPubKeys ?? Array.Empty<byte[]>(), new ByteArrayComparer());
            allPubKeys.Add(ownerPubKey);

            foreach (byte[] pubKey in allPubKeys)
            {
                // Create PubKey from bytes
                var pubKeyObj = new NBitcoin.PubKey(pubKey);
                
                // Encrypt symmetric key with user's public key
                byte[] encryptedKey = pubKeyObj.Encrypt(symmetricKey);
                
                // Store with Base64 encoded public key as the dictionary key
                result.EncryptedKeysForPubKeys[Convert.ToBase64String(pubKey)] = encryptedKey;
            }

            return result;
        }

        /// <summary>
        /// Checks if a user has access to a file.
        /// </summary>
        /// <param name="fileData">The processed file data</param>
        /// <param name="pubKey">Public key of the user to check</param>
        /// <returns>True if the user has access to the file</returns>
        public async Task<bool> HasUserAccessAsync(ProcessedFileData fileData, byte[] pubKey)
        {
            if (fileData == null) throw new ArgumentNullException(nameof(fileData));
            if (pubKey == null) throw new ArgumentNullException(nameof(pubKey));

            string pubKeyBase64 = Convert.ToBase64String(pubKey);
            
            // Check if the public key matches the owner or is in the shared users list
            return pubKeyBase64 == Convert.ToBase64String(fileData.OwnerPubKey) || 
                   fileData.EncryptedKeysForPubKeys.ContainsKey(pubKeyBase64);
        }

        private string CalculateMerkleRoot(List<string> contentIds)
        {
            if (contentIds == null || contentIds.Count == 0)
                return string.Empty;

            if (contentIds.Count == 1)
                return contentIds[0];

            List<string> newHashes = new List<string>();
            
            for (int i = 0; i < contentIds.Count; i += 2)
            {
                if (i + 1 < contentIds.Count)
                {
                    // Concatenate and hash pairs
                    string combinedHash = _hashService.ComputeSha256Hash(
                        contentIds[i] + contentIds[i + 1]);
                    newHashes.Add(combinedHash);
                }
                else
                {
                    // Odd number of hashes, duplicate the last one
                    string combinedHash = _hashService.ComputeSha256Hash(
                        contentIds[i] + contentIds[i]);
                    newHashes.Add(combinedHash);
                }
            }

            // Recursive call with new level of hashes
            return CalculateMerkleRoot(newHashes);
        }

        private string GetMimeTypeFromFileName(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLowerInvariant();
            
            return extension switch
            {
                ".txt" => "text/plain",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".zip" => "application/zip",
                ".mp3" => "audio/mpeg",
                ".mp4" => "video/mp4",
                _ => "application/octet-stream"
            };
        }

        // Helper class for comparing byte arrays in HashSet
        private class ByteArrayComparer : IEqualityComparer<byte[]>
        {
            public bool Equals(byte[] x, byte[] y)
            {
                if (x == null || y == null)
                {
                    return x == y;
                }
                return x.SequenceEqual(y);
            }

            public int GetHashCode(byte[] obj)
            {
                if (obj == null)
                {
                    return 0;
                }
                return BitConverter.ToInt32(obj.Take(4).ToArray(), 0);
            }
        }
    }

    /// <summary>
    /// Interface for the file service.
    /// </summary>
    public interface IFileService
    {
        Task<ProcessedFileData> ProcessFileForBlockchainAsync(
            Stream fileStream,
            string fileName,
            byte[] ownerPubKey,
            byte[][] sharedWithPubKeys = null,
            int blockSize = FileService.DEFAULT_BLOCK_SIZE);

        Task<bool> HasUserAccessAsync(ProcessedFileData fileData, byte[] pubKey);
    }
} 