using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Deltanium.Core.Cryptography
{
    /// <summary>
    /// Service for hash operations in the Deltanium network.
    /// </summary>
    public class HashService : IHashService
    {
        private readonly HashAlgorithmName _hashAlgorithm;

        public HashService(HashAlgorithmName? hashAlgorithm = null)
        {
            _hashAlgorithm = hashAlgorithm ?? HashAlgorithmName.SHA256;
        }

        /// <summary>
        /// Computes the hash of the input data.
        /// </summary>
        /// <param name="data">Data to hash</param>
        /// <returns>Hash value</returns>
        public byte[] ComputeHash(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            using (var hashAlgorithm = GetHashAlgorithm(_hashAlgorithm))
            {
                return hashAlgorithm.ComputeHash(data);
            }
        }

        /// <summary>
        /// Computes the hash of the input data and returns it as a Base64 string.
        /// </summary>
        /// <param name="data">Data to hash</param>
        /// <returns>Base64-encoded hash value</returns>
        public string ComputeHashString(byte[] data)
        {
            return Convert.ToBase64String(ComputeHash(data));
        }

        /// <summary>
        /// Computes the hash of the input string and returns it as a Base64 string.
        /// </summary>
        /// <param name="data">String to hash</param>
        /// <returns>Base64-encoded hash value</returns>
        public string ComputeHashString(string data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(data);
            return ComputeHashString(bytes);
        }

        /// <summary>
        /// Computes the HMAC of the input data using the specified key.
        /// </summary>
        /// <param name="data">Data to compute HMAC for</param>
        /// <param name="key">Key for HMAC</param>
        /// <returns>HMAC value</returns>
        public byte[] ComputeHmac(byte[] data, byte[] key)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            using (var hmac = GetHmacAlgorithm(_hashAlgorithm, key))
            {
                return hmac.ComputeHash(data);
            }
        }

        /// <summary>
        /// Computes a SHA-256 hash of the input data.
        /// </summary>
        /// <param name="data">Data to hash</param>
        /// <returns>Hash of the data</returns>
        public byte[] ComputeHash(string input)
        {
            if (string.IsNullOrEmpty(input)) throw new ArgumentException("Input cannot be null or empty");
            
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            return ComputeHash(inputBytes);
        }

        /// <summary>
        /// Computes a SHA-256 hash of the input data and returns it as a hex string.
        /// </summary>
        /// <param name="data">Data to hash</param>
        /// <returns>Hash of the data as a hex string</returns>
        public string ComputeSha256Hash(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            
            byte[] hashBytes = ComputeHash(data);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// Computes a SHA-256 hash of the input string and returns it as a hex string.
        /// </summary>
        /// <param name="input">String to hash</param>
        /// <returns>Hash of the string as a hex string</returns>
        public string ComputeSha256Hash(string input)
        {
            if (string.IsNullOrEmpty(input)) throw new ArgumentException("Input cannot be null or empty");
            
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            return ComputeSha256Hash(inputBytes);
        }

        /// <summary>
        /// Computes a Merkle tree from leaf hashes and returns the root hash.
        /// </summary>
        /// <param name="leafHashes">List of leaf node hashes</param>
        /// <returns>Merkle tree root hash</returns>
        public byte[] ComputeMerkleRoot(byte[][] leafHashes)
        {
            if (leafHashes == null || leafHashes.Length == 0)
                throw new ArgumentException("Leaf hashes cannot be empty", nameof(leafHashes));

            // If there's only one hash, it's the root already
            if (leafHashes.Length == 1)
                return leafHashes[0];

            // Build the tree bottom-up
            byte[][] currentLevel = leafHashes;

            while (currentLevel.Length > 1)
            {
                int nextLevelSize = (currentLevel.Length + 1) / 2; // Round up for odd number of nodes
                byte[][] nextLevel = new byte[nextLevelSize][];

                for (int i = 0; i < currentLevel.Length; i += 2)
                {
                    // If this is the last node and has no pair, carry it up to the next level
                    if (i + 1 >= currentLevel.Length)
                    {
                        nextLevel[i / 2] = currentLevel[i];
                        continue;
                    }

                    // Concatenate the pair of hashes and hash them together
                    byte[] combined = new byte[currentLevel[i].Length + currentLevel[i + 1].Length];
                    Buffer.BlockCopy(currentLevel[i], 0, combined, 0, currentLevel[i].Length);
                    Buffer.BlockCopy(currentLevel[i + 1], 0, combined, currentLevel[i].Length, currentLevel[i + 1].Length);

                    nextLevel[i / 2] = ComputeHash(combined);
                }

                currentLevel = nextLevel;
            }

            // The root is the only element left
            return currentLevel[0];
        }

        /// <summary>
        /// Gets the appropriate hash algorithm based on the specified name.
        /// </summary>
        private HashAlgorithm GetHashAlgorithm(HashAlgorithmName algorithmName)
        {
            if (algorithmName == HashAlgorithmName.SHA256)
                return SHA256.Create();
            else if (algorithmName == HashAlgorithmName.SHA384)
                return SHA384.Create();
            else if (algorithmName == HashAlgorithmName.SHA512)
                return SHA512.Create();
            else if (algorithmName == HashAlgorithmName.MD5)
                return MD5.Create();
            else
                throw new ArgumentException($"Unsupported hash algorithm: {algorithmName}", nameof(algorithmName));
        }

        /// <summary>
        /// Gets the appropriate HMAC algorithm based on the specified name and key.
        /// </summary>
        private HMAC GetHmacAlgorithm(HashAlgorithmName algorithmName, byte[] key)
        {
            if (algorithmName == HashAlgorithmName.SHA256)
                return new HMACSHA256(key);
            else if (algorithmName == HashAlgorithmName.SHA384)
                return new HMACSHA384(key);
            else if (algorithmName == HashAlgorithmName.SHA512)
                return new HMACSHA512(key);
            else if (algorithmName == HashAlgorithmName.MD5)
                return new HMACMD5(key);
            else
                throw new ArgumentException($"Unsupported HMAC algorithm: {algorithmName}", nameof(algorithmName));
        }
    }

    /// <summary>
    /// Interface for hash operations.
    /// </summary>
    public interface IHashService
    {
        byte[] ComputeHash(byte[] data);
        string ComputeHashString(byte[] data);
        string ComputeHashString(string data);
        byte[] ComputeHmac(byte[] data, byte[] key);
        byte[] ComputeMerkleRoot(byte[][] leafHashes);
        byte[] ComputeHash(string input);
        string ComputeSha256Hash(byte[] data);
        string ComputeSha256Hash(string input);
    }
} 