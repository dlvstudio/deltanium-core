using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using NBitcoin;

namespace Deltanium.Core.Cryptography
{
    /// <summary>
    /// Service for cryptographic operations in the Deltanium network.
    /// </summary>
    public class CryptographyService : ICryptographyService
    {
        private readonly IUserKeyStore _userKeyStore;

        public CryptographyService(IUserKeyStore userKeyStore)
        {
            _userKeyStore = userKeyStore ?? throw new ArgumentNullException(nameof(userKeyStore));
        }

        /// <summary>
        /// Encrypts data using AES with the specified key.
        /// </summary>
        /// <param name="data">Data to encrypt</param>
        /// <param name="key">Encryption key</param>
        /// <returns>Encrypted data</returns>
        public async Task<byte[]> EncryptDataAsync(byte[] data, byte[] key)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (key == null) throw new ArgumentNullException(nameof(key));
            
            // Generate a random IV
            byte[] iv = new byte[16]; // AES block size is 16 bytes
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(iv);
            }
            
            // Encrypt the data
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                
                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                {
                    // Write IV to the output
                    await ms.WriteAsync(iv, 0, iv.Length);
                    
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        await cs.WriteAsync(data, 0, data.Length);
                        cs.FlushFinalBlock();
                    }
                    
                    return ms.ToArray();
                }
            }
        }

        /// <summary>
        /// Decrypts data using AES with the specified key.
        /// </summary>
        /// <param name="encryptedData">Encrypted data (including IV)</param>
        /// <param name="key">Decryption key</param>
        /// <returns>Decrypted data</returns>
        public async Task<byte[]> DecryptDataAsync(byte[] encryptedData, byte[] key)
        {
            if (encryptedData == null) throw new ArgumentNullException(nameof(encryptedData));
            if (key == null) throw new ArgumentNullException(nameof(key));
            
            // Extract IV from the beginning of the encrypted data
            byte[] iv = new byte[16]; // AES block size
            Array.Copy(encryptedData, iv, iv.Length);
            
            // Decrypt the data
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                
                using (var decryptor = aes.CreateDecryptor())
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
                    {
                        // Skip the IV and decrypt the rest
                        await cs.WriteAsync(encryptedData, iv.Length, encryptedData.Length - iv.Length);
                        cs.FlushFinalBlock();
                    }
                    
                    return ms.ToArray();
                }
            }
        }

        /// <summary>
        /// Encrypts data with a user's public key.
        /// </summary>
        /// <param name="data">Data to encrypt</param>
        /// <param name="publicKey">Public key to encrypt with</param>
        /// <returns>Encrypted data</returns>
        public async Task<byte[]> EncryptWithPublicKeyAsync(byte[] data, byte[] publicKey)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (publicKey == null) throw new ArgumentNullException(nameof(publicKey));
            
            try
            {
                // Create a PubKey from the public key bytes
                var pubKey = new NBitcoin.PubKey(publicKey);
                
                // Encrypt the data using NBitcoin's PubKey.Encrypt
                byte[] encryptedData = pubKey.Encrypt(data);
                
                return encryptedData;
            }
            catch (Exception ex)
            {
                throw new CryptographicException($"Failed to encrypt data: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Decrypts data with a user's private key.
        /// </summary>
        /// <param name="encryptedData">Data encrypted with public key</param>
        /// <param name="privateKey">Private key to decrypt with</param>
        /// <returns>Decrypted data</returns>
        public async Task<byte[]> DecryptWithPrivateKeyAsync(byte[] encryptedData, byte[] privateKey)
        {
            if (encryptedData == null) throw new ArgumentNullException(nameof(encryptedData));
            if (privateKey == null) throw new ArgumentNullException(nameof(privateKey));
            
            try
            {
                // Create a Key from the private key bytes
                var key = new NBitcoin.Key(privateKey);
                
                // Decrypt the data using NBitcoin's Key.Decrypt
                byte[] decryptedData = key.Decrypt(encryptedData);
                
                return decryptedData;
            }
            catch (Exception ex)
            {
                throw new CryptographicException($"Failed to decrypt data: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets a user's public key from the key store.
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>User's public key</returns>
        public async Task<byte[]> GetUserPublicKeyAsync(string userId)
        {
            return await _userKeyStore.GetPublicKeyAsync(userId);
        }

        /// <summary>
        /// Signs data with a user's private key.
        /// </summary>
        /// <param name="data">Data to sign</param>
        /// <param name="privateKey">Private key to sign with</param>
        /// <returns>Signature</returns>
        public byte[] SignData(byte[] data, byte[] privateKey)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (privateKey == null) throw new ArgumentNullException(nameof(privateKey));
            
            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                rsa.ImportPkcs8PrivateKey(privateKey, out _);
                return rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
        }

        /// <summary>
        /// Verifies a signature with a user's public key.
        /// </summary>
        /// <param name="data">Original data</param>
        /// <param name="signature">Signature to verify</param>
        /// <param name="publicKey">Public key to verify with</param>
        /// <returns>True if signature is valid</returns>
        public bool VerifySignature(byte[] data, byte[] signature, byte[] publicKey)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (signature == null) throw new ArgumentNullException(nameof(signature));
            if (publicKey == null) throw new ArgumentNullException(nameof(publicKey));
            
            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                rsa.ImportSubjectPublicKeyInfo(publicKey, out _);
                return rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
        }

        /// <summary>
        /// Gets a user's public key from the key store.
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>User's public key</returns>
        public async Task<byte[]> GetPublicKeyAsync(string userId)
        {
            return await _userKeyStore.GetPublicKeyAsync(userId);
        }

        /// <summary>
        /// Gets a user's private key from the key store.
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="password">Password for decryption</param>
        /// <returns>User's private key</returns>
        public async Task<byte[]> GetPrivateKeyAsync(string userId, string password = null)
        {
            return await _userKeyStore.GetPrivateKeyAsync(userId, password);
        }
    }

    /// <summary>
    /// Interface for the cryptography service.
    /// </summary>
    public interface ICryptographyService
    {
        Task<byte[]> EncryptDataAsync(byte[] data, byte[] key);
        Task<byte[]> DecryptDataAsync(byte[] encryptedData, byte[] key);
        Task<byte[]> EncryptWithPublicKeyAsync(byte[] data, byte[] publicKey);
        Task<byte[]> DecryptWithPrivateKeyAsync(byte[] encryptedData, byte[] privateKey);
        Task<byte[]> GetUserPublicKeyAsync(string userId);
        Task<byte[]> GetPublicKeyAsync(string userId);
        Task<byte[]> GetPrivateKeyAsync(string userId, string password = null);
        byte[] SignData(byte[] data, byte[] privateKey);
        bool VerifySignature(byte[] data, byte[] signature, byte[] publicKey);
    }

    /// <summary>
    /// Interface for storing and retrieving user keys.
    /// </summary>
    public interface IUserKeyStore
    {
        Task<byte[]> GetPublicKeyAsync(string userId);
        Task<byte[]> GetPrivateKeyAsync(string userId, string password);
    }
} 