using System;
using Deltanium;
using Deltanium.RPC;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Deltanium.Core.Services;
using Deltanium.Core.Cryptography;
using Deltanium.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json.Serialization;

Console.WriteLine("Hello, world!");

// Demo encryption/decryption with NBitcoin Key
static void DemoEncryptionWithKey()
{
    Console.WriteLine("\n=== Demo Encryption/Decryption with NBitcoin Key ===");
    
    // Create a new key pair
    var myKey = new Key();
    Console.WriteLine($"Private Key (WIF): {myKey.GetWif(Network.TestNet)}");
    Console.WriteLine($"Public Key: {myKey.PubKey}");
    
    // Message to encrypt
    string originalMessage = "Hello, this is a secret message!";
    Console.WriteLine($"\nOriginal Message: {originalMessage}");
    
    // Encrypt message using public key
    byte[] messageBytes = Encoding.UTF8.GetBytes(originalMessage);
    byte[] encryptedData = myKey.PubKey.Encrypt(messageBytes);
    Console.WriteLine($"Encrypted (Base64): {Convert.ToBase64String(encryptedData)}");
    
    // Decrypt message using private key
    byte[] decryptedData = myKey.Decrypt(encryptedData);
    string decryptedMessage = Encoding.UTF8.GetString(decryptedData);
    Console.WriteLine($"Decrypted Message: {decryptedMessage}");
}

int x = 10;
int y = 20;
int sum = x + y;
Console.WriteLine($"The sum of {x} and {y} is {sum}.");

// Connect to the Bitcoin network (testnet or mainnet)
Network network = Network.TestNet; // Use TestNet for testing purposes, MainNet for real transactions

// Fetch a block by its height or hash (example for testnet)
uint256 blockHash = uint256.Parse("00000000000000000012f5d1d8ed035b2f77b28d48f2b84e2bfe4f9a9ef7b88b");
Block block = network.GetGenesis();

Console.WriteLine($"Block Hash: {block.GetHash()}");
Console.WriteLine($"Previous Block Hash: {block.Header.HashPrevBlock}");

// Function to execute a transaction
static Transaction ExecuteTransaction(Key senderKey, BitcoinAddress recipientAddress, Money amount, ICoin inputCoin, Network network)
{
    try
    {
        var transaction = new Transaction();
        
        // Add input from the provided coin
        var outpoint = new OutPoint(inputCoin.Outpoint.Hash, inputCoin.Outpoint.N);
        transaction.Inputs.Add(outpoint);

        // Add output with the specified amount
        var output = transaction.Outputs.Add(amount, recipientAddress.ScriptPubKey);

        // Create a transaction builder and sign the transaction
        var builder = new TransactionBuilder(network);
        builder.AddCoins(inputCoin);
        builder.AddKeys(senderKey);
        builder.SignTransactionInPlace(transaction);

        // Verify the signature
        bool isValid = VerifyTransactionSignature(transaction, senderKey.PubKey, inputCoin);
        if (!isValid)
        {
            throw new Exception("Transaction signature verification failed");
        }

        // Print transaction details
        Console.WriteLine("\nTransaction Details:");
        Console.WriteLine($"Transaction Hash: {transaction.GetHash()}");
        Console.WriteLine($"Input Count: {transaction.Inputs.Count}");
        Console.WriteLine($"Output Count: {transaction.Outputs.Count}");
        Console.WriteLine($"Amount Sent: {output.Value.ToUnit(MoneyUnit.BTC)} BTC");
        Console.WriteLine($"From: {senderKey.GetAddress(ScriptPubKeyType.Legacy, network)}");
        Console.WriteLine($"To: {recipientAddress}");

        Console.WriteLine("\nRaw Transaction (hex):");
        Console.WriteLine(transaction.ToHex());

        return transaction;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error executing transaction: {ex.Message}");
        throw;
    }
}

// Update DemoTransactions to use secure key storage
static void DemoTransactions()
{
    try
    {
        var network = Network.TestNet;

        // Initialize secure key storage
        string keyStorePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Deltanium",
            "Keys"
        );
        Directory.CreateDirectory(keyStorePath);
        var keyStorage = new SecureKeyStorage(keyStorePath);

        // Create key pairs for all users
        var userAKey = new Key();
        var userBKey = new Key();
        var userCKey = new Key();

        // Save keys securely
        keyStorage.SaveKey(userAKey, "userA");
        keyStorage.SaveKey(userBKey, "userB");
        keyStorage.SaveKey(userCKey, "userC");

        // Get their addresses
        var userAAddress = userAKey.GetAddress(ScriptPubKeyType.Legacy, network);
        var userBAddress = userBKey.GetAddress(ScriptPubKeyType.Legacy, network);
        var userCAddress = userCKey.GetAddress(ScriptPubKeyType.Legacy, network);

        Console.WriteLine($"\nUser A's Address: {userAAddress}");
        Console.WriteLine($"User B's Address: {userBAddress}");
        Console.WriteLine($"User C's Address: {userCAddress}");

        // Create initial coin for User A (5 BTC)
        var initialCoin = new Coin(new OutPoint(uint256.Zero, 0), new TxOut(Money.Coins(5), userAAddress.ScriptPubKey));

        Console.WriteLine("\n=== Transaction 1: A to B (3 BTC) ===");
        var transaction1 = ExecuteTransaction(
            userAKey,
            userBAddress,
            Money.Coins(3),
            initialCoin,
            network
        );

        // Create coin from first transaction output for second transaction
        var coinFromFirstTx = new Coin(new OutPoint(transaction1.GetHash(), 0), new TxOut(Money.Coins(3), userBAddress.ScriptPubKey));

        Console.WriteLine("\n=== Transaction 2: B to C (1 BTC) ===");
        var transaction2 = ExecuteTransaction(
            userBKey,
            userCAddress,
            Money.Coins(1),
            coinFromFirstTx,
            network
        );

        // Clean up keys after use
        keyStorage.DeleteKey("userA");
        keyStorage.DeleteKey("userB");
        keyStorage.DeleteKey("userC");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error in transaction demonstration: {ex.Message}");
        throw;
    }
}

static bool VerifyTransactionSignature(Transaction transaction, PubKey pubKey, ICoin coin)
{
    try
    {
        var scriptPubKey = coin.TxOut.ScriptPubKey;
        var scriptEval = new ScriptEvaluationContext
        {
            ScriptVerify = ScriptVerify.Standard
        };

        var checker = new TransactionChecker(transaction, 0, coin.TxOut);
        var result = scriptEval.VerifyScript(transaction.Inputs[0].ScriptSig, scriptPubKey, checker);

        if (!result)
        {
            Console.WriteLine($"Script verification failed: {scriptEval.Error}");
            return false;
        }

        return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error verifying signature: {ex.Message}");
        return false;
    }
}

// Demonstrate file handling with FileService using SecureKeyStorage
static async Task DemoFileHandlingAsync()
{
    Console.WriteLine("\n=== Starting File Encryption/Decryption Demo ===");
    
    // Create keys for users and store them in a dictionary
    var userKeys = new Dictionary<string, Key>
    {
        ["alice"] = new Key(),
        ["bob"] = new Key(),
        ["charlie"] = new Key(),
        ["dave"] = new Key()
    };

    // Create a dictionary of public keys for easy lookup
    var pubKeyDict = userKeys.ToDictionary(
        kvp => Convert.ToBase64String(kvp.Value.PubKey.ToBytes()),
        kvp => kvp.Value.PubKey
    );

    Console.WriteLine("Created keys for Alice, Bob, Charlie, and Dave");

    // Store keys securely (optional, just for demonstration)
    var keyStoragePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Deltanium/FileDemo"
    );
    Directory.CreateDirectory(keyStoragePath);

    var keyStorage = new SecureKeyStorage(keyStoragePath);
    foreach (var kvp in userKeys)
    {
        await keyStorage.StoreKeyAsync(kvp.Key, kvp.Value.ToBytes());
    }

    Console.WriteLine("Stored all user keys securely");

    // Create a test file with known content
    var testFilePath = "test.txt";
    string originalContent = "This is a test file for encryption and blockchain storage. We will verify if Bob and Charlie can decrypt it!";
    await File.WriteAllTextAsync(testFilePath, originalContent);
    Console.WriteLine($"\nCreated test file with content: \"{originalContent}\"");

    ProcessedFileData processedFile = null;
    var userKeyStore = new CryptoKeyAdapter(keyStorage);
    var cryptoService = new CryptographyService(userKeyStore);
    var hashService = new HashService();
    var fileService = new FileService(cryptoService, hashService);
    
    // Get public keys for shared users
    var sharedPubKeys = new[] { "bob", "charlie" }
        .Select(u => userKeys[u].PubKey.ToBytes())
        .ToArray();
    
    // Process and encrypt the file
    using (var fileStream = File.OpenRead(testFilePath))
    {
        Console.WriteLine("\nProcessing file for blockchain storage...");
        processedFile = await fileService.ProcessFileForBlockchainAsync(
            fileStream,
            Path.GetFileName(testFilePath),
            userKeys["alice"].PubKey.ToBytes(),
            sharedPubKeys,
            1024
        );

        Console.WriteLine($"File processed successfully:");
        Console.WriteLine($"File ID: {processedFile.FileId}");
        Console.WriteLine($"File Name: {processedFile.FileName}");
        Console.WriteLine($"File Size: {processedFile.FileSize} bytes");
        Console.WriteLine($"Block Count: {processedFile.BlockCount}");
        Console.WriteLine($"Shared with public keys: {string.Join(", ", processedFile.EncryptedKeysForPubKeys.Keys)}");
    }

    // Now let's verify that Bob and Charlie can decrypt the file
    var users = new[] { "bob", "charlie", "dave" };
    foreach (var user in users)
    {
        Console.WriteLine($"\n=== Testing decryption for {user} ===");
        
        try
        {
            // Get user's public key in Base64 format for lookup
            string userPubKeyBase64 = Convert.ToBase64String(userKeys[user].PubKey.ToBytes());
            
            // Check if user has access by looking up their public key
            bool canAccess = processedFile.EncryptedKeysForPubKeys.ContainsKey(userPubKeyBase64);
            Console.WriteLine($"User {user} has access to file: {canAccess}");

            if (canAccess)
            {
                // Get user's private key
                Key userKey = userKeys[user];
                
                // Get the encrypted symmetric key for this user's public key
                byte[] encryptedSymmetricKey = processedFile.EncryptedKeysForPubKeys[userPubKeyBase64];
                
                // Decrypt the symmetric key using user's private key
                byte[] symmetricKey = userKey.Decrypt(encryptedSymmetricKey);
                Console.WriteLine($"Successfully decrypted file's symmetric key");

                // Now decrypt each block using the symmetric key
                var decryptedContent = new StringBuilder();
                foreach (var block in processedFile.EncryptedBlocks)
                {
                    byte[] decryptedBlock = await cryptoService.DecryptDataAsync(block.EncryptedContent, symmetricKey);
                    decryptedContent.Append(Encoding.UTF8.GetString(decryptedBlock));
                }

                string finalContent = decryptedContent.ToString();
                Console.WriteLine($"Decrypted content: \"{finalContent}\"");
                
                // Verify the content matches
                bool contentMatches = finalContent == originalContent;
                Console.WriteLine($"Decryption verification: {(contentMatches ? "SUCCESS" : "FAILED")}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during decryption for {user}: {ex.Message}");
        }
    }

    // Clean up
    if (File.Exists(testFilePath))
    {
        File.Delete(testFilePath);
    }
    Console.WriteLine("\n=== File Encryption/Decryption Demo Completed ===");
}

// Demo encryption/decryption of symmetric key using public/private key pairs
static void DemoSymmetricKeyEncryption()
{
    Console.WriteLine("\n=== Demo Symmetric Key Encryption using Public/Private Keys ===");
    
    // Create a new key pair for Alice (sender)
    var aliceKey = new Key();
    Console.WriteLine($"Alice's Private Key (WIF): {aliceKey.GetWif(Network.TestNet)}");
    Console.WriteLine($"Alice's Public Key: {aliceKey.PubKey}");
    
    // Create a new key pair for Bob (receiver)
    var bobKey = new Key();
    Console.WriteLine($"\nBob's Private Key (WIF): {bobKey.GetWif(Network.TestNet)}");
    Console.WriteLine($"Bob's Public Key: {bobKey.PubKey}");
    
    // Generate a random symmetric key (256 bits/32 bytes for AES-256)
    byte[] symmetricKey = new byte[32];
    using (var rng = new System.Security.Cryptography.RNGCryptoServiceProvider())
    {
        rng.GetBytes(symmetricKey);
    }
    Console.WriteLine($"\nGenerated Symmetric Key (Base64): {Convert.ToBase64String(symmetricKey)}");
    
    // Encrypt symmetric key using Bob's public key
    byte[] encryptedSymmetricKey = bobKey.PubKey.Encrypt(symmetricKey);
    Console.WriteLine($"Encrypted Symmetric Key (Base64): {Convert.ToBase64String(encryptedSymmetricKey)}");
    
    // Example data to encrypt with symmetric key
    string originalMessage = "This is a secret file content that will be encrypted with the symmetric key!";
    byte[] messageBytes = Encoding.UTF8.GetBytes(originalMessage);
    Console.WriteLine($"\nOriginal Message: {originalMessage}");
    
    // Encrypt message using symmetric key
    byte[] encryptedMessage;
    using (var aes = System.Security.Cryptography.Aes.Create())
    {
        aes.Key = symmetricKey;
        aes.GenerateIV(); // Generate random IV
        
        using (var encryptor = aes.CreateEncryptor())
        {
            // Combine IV and encrypted data
            using (var msEncrypt = new System.IO.MemoryStream())
            {
                // Write IV first
                msEncrypt.Write(aes.IV, 0, aes.IV.Length);
                
                using (var csEncrypt = new System.Security.Cryptography.CryptoStream(msEncrypt, encryptor, System.Security.Cryptography.CryptoStreamMode.Write))
                using (var swEncrypt = new System.IO.StreamWriter(csEncrypt))
                {
                    swEncrypt.Write(originalMessage);
                }
                
                encryptedMessage = msEncrypt.ToArray();
            }
        }
    }
    Console.WriteLine($"Encrypted Message (Base64): {Convert.ToBase64String(encryptedMessage)}");
    
    // Now Bob decrypts the symmetric key using his private key
    byte[] decryptedSymmetricKey = bobKey.Decrypt(encryptedSymmetricKey);
    Console.WriteLine($"\nDecrypted Symmetric Key matches original: {Convert.ToBase64String(symmetricKey) == Convert.ToBase64String(decryptedSymmetricKey)}");
    
    // Decrypt the message using the decrypted symmetric key
    string decryptedMessage;
    using (var aes = System.Security.Cryptography.Aes.Create())
    {
        // Get the IV from the beginning of the encrypted message
        byte[] iv = new byte[aes.IV.Length];
        Array.Copy(encryptedMessage, 0, iv, 0, iv.Length);
        
        aes.Key = decryptedSymmetricKey;
        aes.IV = iv;
        
        using (var decryptor = aes.CreateDecryptor())
        {
            // Skip IV when decrypting
            using (var msDecrypt = new System.IO.MemoryStream(encryptedMessage, iv.Length, encryptedMessage.Length - iv.Length))
            using (var csDecrypt = new System.Security.Cryptography.CryptoStream(msDecrypt, decryptor, System.Security.Cryptography.CryptoStreamMode.Read))
            using (var srDecrypt = new System.IO.StreamReader(csDecrypt))
            {
                decryptedMessage = srDecrypt.ReadToEnd();
            }
        }
    }
    Console.WriteLine($"Decrypted Message: {decryptedMessage}");
    Console.WriteLine($"Decryption successful: {originalMessage == decryptedMessage}");
}

// Demo file handling with a real DOCX file
static async Task DemoFileHandlingWithRealFileAsync()
{
    Console.WriteLine("\n=== Starting File Encryption/Decryption Demo with DOCX ===");
    
    // Create keys for users and store them in a dictionary
    var userKeys = new Dictionary<string, Key>
    {
        ["alice"] = new Key(),
        ["bob"] = new Key(),
        ["charlie"] = new Key(),
        ["dave"] = new Key()
    };

    // Create a dictionary of public keys for easy lookup
    var pubKeyDict = userKeys.ToDictionary(
        kvp => Convert.ToBase64String(kvp.Value.PubKey.ToBytes()),
        kvp => kvp.Value.PubKey
    );

    Console.WriteLine("Created keys for Alice, Bob, Charlie, and Dave");

    // Store keys securely (optional, just for demonstration)
    var keyStoragePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Deltanium/FileDemo"
    );
    Directory.CreateDirectory(keyStoragePath);

    var keyStorage = new SecureKeyStorage(keyStoragePath);
    foreach (var kvp in userKeys)
    {
        await keyStorage.StoreKeyAsync(kvp.Key, kvp.Value.ToBytes());
    }

    Console.WriteLine("Stored all user keys securely");

    // Path to the DOCX file
    var docxFilePath = Path.Combine("sample-files", "img-test.png");
    Console.WriteLine($"\nProcessing DOCX file: {docxFilePath}");

    ProcessedFileData processedFile = null;
    var userKeyStore = new CryptoKeyAdapter(keyStorage);
    var cryptoService = new CryptographyService(userKeyStore);
    var hashService = new HashService();
    var fileService = new FileService(cryptoService, hashService);
    
    // Get public keys for shared users
    var sharedPubKeys = new[] { "bob", "charlie" }
        .Select(u => userKeys[u].PubKey.ToBytes())
        .ToArray();
    
    // Process and encrypt the file
    using (var fileStream = File.OpenRead(docxFilePath))
    {
        Console.WriteLine("\nProcessing Real file for blockchain storage...");
        processedFile = await fileService.ProcessFileForBlockchainAsync(
            fileStream,
            Path.GetFileName(docxFilePath),
            userKeys["alice"].PubKey.ToBytes(),
            sharedPubKeys,
            1024 * 1024 // Use 1MB blocks for larger files
        );

        Console.WriteLine($"File processed successfully:");
        Console.WriteLine($"File ID: {processedFile.FileId}");
        Console.WriteLine($"File Name: {processedFile.FileName}");
        Console.WriteLine($"File Size: {processedFile.FileSize:N0} bytes");
        Console.WriteLine($"Block Count: {processedFile.BlockCount}");
        Console.WriteLine($"Shared with public keys: {string.Join(", ", processedFile.EncryptedKeysForPubKeys.Keys)}");
    }

    // Now let's verify that Bob and Charlie can decrypt the file
    var users = new[] { "bob", "charlie", "dave" };
    foreach (var user in users)
    {
        Console.WriteLine($"\n=== Testing decryption for {user} ===");
        
        try
        {
            // Get user's public key in Base64 format for lookup
            string userPubKeyBase64 = Convert.ToBase64String(userKeys[user].PubKey.ToBytes());
            
            // Check if user has access by looking up their public key
            bool canAccess = processedFile.EncryptedKeysForPubKeys.ContainsKey(userPubKeyBase64);
            Console.WriteLine($"User {user} has access to file: {canAccess}");

            if (canAccess)
            {
                // Get user's private key
                Key userKey = userKeys[user];
                
                // Get the encrypted symmetric key for this user's public key
                byte[] encryptedSymmetricKey = processedFile.EncryptedKeysForPubKeys[userPubKeyBase64];
                
                // Decrypt the symmetric key using user's private key
                byte[] symmetricKey = userKey.Decrypt(encryptedSymmetricKey);
                Console.WriteLine($"Successfully decrypted file's symmetric key");

                // Create a temporary file to save the decrypted content
                string decryptedFilePath = Path.Combine(
                    Path.GetDirectoryName(docxFilePath),
                    $"decrypted_{user}_{Path.GetFileName(docxFilePath)}"
                );

                // Now decrypt each block and write to the file
                using (var outputStream = File.Create(decryptedFilePath))
                {
                    foreach (var block in processedFile.EncryptedBlocks)
                    {
                        byte[] decryptedBlock = await cryptoService.DecryptDataAsync(block.EncryptedContent, symmetricKey);
                        await outputStream.WriteAsync(decryptedBlock, 0, decryptedBlock.Length);
                    }
                }

                Console.WriteLine($"Decrypted file saved to: {decryptedFilePath}");
                
                // Verify file sizes match
                var originalFileInfo = new FileInfo(docxFilePath);
                var decryptedFileInfo = new FileInfo(decryptedFilePath);
                bool sizeMatches = originalFileInfo.Length == decryptedFileInfo.Length;
                Console.WriteLine($"File size verification: {(sizeMatches ? "SUCCESS" : "FAILED")}");
                Console.WriteLine($"Original size: {originalFileInfo.Length:N0} bytes");
                Console.WriteLine($"Decrypted size: {decryptedFileInfo.Length:N0} bytes");

                // If it's a text file, show the content
                if (Path.GetExtension(docxFilePath).Equals(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    var textContent = await File.ReadAllTextAsync(decryptedFilePath);
                    Console.WriteLine($"\nFile content:\n{textContent}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during decryption for {user}: {ex.Message}");
        }
    }

    Console.WriteLine("\n=== DOCX File Encryption/Decryption Demo Completed ===");
}

// Demo uploading file to deltanium-store
static async Task DemoUploadToDeltaniumStoreAsync()
{
    Console.WriteLine("\n=== Starting File Upload to Deltanium Store Demo ===");
    
    // Declare variables at method scope for use in both upload and download
    Dictionary<string, Key> userKeys = null;
    ProcessedFileData processedFile = null;
    ICryptographyService cryptoService = null;
    
    try
    {
        // Create keys for users
        userKeys = new Dictionary<string, Key>
        {
            ["alice"] = new Key(),
            ["bob"] = new Key(),
            ["charlie"] = new Key()
        };

        // Store keys securely
        var keyStoragePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Deltanium/FileDemo"
        );
        Directory.CreateDirectory(keyStoragePath);

        var keyStorage = new SecureKeyStorage(keyStoragePath);
        foreach (var kvp in userKeys)
        {
            await keyStorage.StoreKeyAsync(kvp.Key, kvp.Value.ToBytes());
        }

        Console.WriteLine("Created and stored keys for Alice, Bob, and Charlie");

        // Create test file
        var testFilePath = Path.Combine("sample-files", "img-test.png");
        Console.WriteLine($"\nUsing test file at: {testFilePath}");

        // Setup services for file processing
        var userKeyStore = new CryptoKeyAdapter(keyStorage);
        cryptoService = new CryptographyService(userKeyStore);
        var hashService = new HashService();
        var fileService = new FileService(cryptoService, hashService);

        // Get public keys for shared users
        var sharedPubKeys = new[] { "bob", "charlie" }
            .Select(u => userKeys[u].PubKey.ToBytes())
            .ToArray();

        // Process and encrypt the file
        using (var fileStream = File.OpenRead(testFilePath))
        {
            Console.WriteLine("\nProcessing file for upload...");
            processedFile = await fileService.ProcessFileForBlockchainAsync(
                fileStream,
                Path.GetFileName(testFilePath),
                userKeys["alice"].PubKey.ToBytes(),
                sharedPubKeys,
                1024 * 1024 // Use 1MB blocks for image files
            );

            Console.WriteLine($"File processed successfully:");
            Console.WriteLine($"File ID: {processedFile.FileId}");
            Console.WriteLine($"File Name: {processedFile.FileName}");
            Console.WriteLine($"File Size: {processedFile.FileSize:N0} bytes");
            Console.WriteLine($"Block Count: {processedFile.BlockCount}");
            Console.WriteLine($"Shared with public keys: {string.Join(", ", processedFile.EncryptedKeysForPubKeys.Keys)}");
        }

        // Upload each encrypted block to deltanium-store
        using (var httpClient = new HttpClient())
        {
            var alicePubKeyBase64 = Convert.ToBase64String(userKeys["alice"].PubKey.ToBytes());
            httpClient.DefaultRequestHeaders.Add("X-User-PubKey", alicePubKeyBase64);

            // First, upload metadata about the blocks and encryption keys
            var metadata = new
            {
                fileId = processedFile.FileId,
                fileName = processedFile.FileName,
                fileSize = processedFile.FileSize,
                blockIds = processedFile.EncryptedBlocks.Select((_, i) => $"{processedFile.FileId}_block_{i}").ToList(),
                encryptedKeysForPubKeys = processedFile.EncryptedKeysForPubKeys,
                ownerPubKey = alicePubKeyBase64,
                mimeType = processedFile.MimeType,
                fileExtension = processedFile.FileExtension,
                creationTime = DateTime.UtcNow
            };

            // Update BlockIds in processedFile to match what we're sending to the store
            processedFile.BlockIds = metadata.blockIds;

            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
            var metadataJson = System.Text.Json.JsonSerializer.Serialize(metadata, options);
            Console.WriteLine("\nUploading metadata:");
            Console.WriteLine(metadataJson);
            var metadataPath = Path.Combine(Path.GetTempPath(), $"{processedFile.FileId}_metadata.json");
            await File.WriteAllTextAsync(metadataPath, metadataJson);

            using (var formData = new MultipartFormDataContent())
            using (var fileStream = File.OpenRead(metadataPath))
            {
                var fileContent = new StreamContent(fileStream);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                formData.Add(fileContent, "file", $"{processedFile.FileId}_metadata.json");

                var response = await httpClient.PostAsync("http://localhost:5000/api/file/upload", formData);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("\nMetadata upload response:");
                    Console.WriteLine(result);
                }
                else
                {
                    Console.WriteLine($"\nError uploading metadata: {response.StatusCode}");
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error content: {errorContent}");
                    return;
                }
            }

            // Upload each block as a separate file
            for (int i = 0; i < processedFile.EncryptedBlocks.Count; i++)
            {
                var block = processedFile.EncryptedBlocks[i];
                var blockFileName = $"{processedFile.FileId}_block_{i}";
                
                // Create a temporary file for the block
                var blockFilePath = Path.Combine(Path.GetTempPath(), blockFileName);
                await File.WriteAllBytesAsync(blockFilePath, block.EncryptedContent);

                // Upload the block
                using (var formData = new MultipartFormDataContent())
                using (var fileStream = File.OpenRead(blockFilePath))
                {
                    var fileContent = new StreamContent(fileStream);
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                    formData.Add(fileContent, "file", blockFileName);

                    var response = await httpClient.PostAsync("http://localhost:5000/api/file/upload", formData);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"\nBlock {i} uploaded successfully!");
                        Console.WriteLine($"Response: {result}");
                    }
                    else
                    {
                        Console.WriteLine($"\nError uploading block {i}: {response.StatusCode}");
                        Console.WriteLine(await response.Content.ReadAsStringAsync());
                        return;
                    }

                    // Clean up block file
                    File.Delete(blockFilePath);
                }
            }

            // List all uploaded files
            var listResponse = await httpClient.GetAsync("http://localhost:5000/api/file/list");
            if (listResponse.IsSuccessStatusCode)
            {
                var files = await listResponse.Content.ReadAsStringAsync();
                Console.WriteLine("\nListing all uploaded files:");
                Console.WriteLine(files);
            }
        }

        Console.WriteLine("\n=== File Upload to Deltanium Store Demo Completed ===");

        // Now demonstrate downloading and decrypting the file
        Console.WriteLine("\n=== Starting File Download and Decryption Demo ===");

        try 
        {
            using (var httpClient = new HttpClient())
            {
                var bobPubKeyBase64 = Convert.ToBase64String(userKeys["bob"].PubKey.ToBytes());
                httpClient.DefaultRequestHeaders.Add("X-User-PubKey", bobPubKeyBase64);

                // Download metadata first
                Console.WriteLine("\nStep 1: Downloading metadata...");
                Console.WriteLine($"Attempting to download metadata for file ID: {processedFile.FileId}");
                Console.WriteLine($"Using Bob's public key: {bobPubKeyBase64}");
                
                var metadataResponse = await httpClient.GetAsync($"http://localhost:5000/api/file/download/{processedFile.FileId}");
                
                if (metadataResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new FileNotFoundException("Metadata file not found. The file might have been deleted or you don't have access.");
                }
                else if (metadataResponse.StatusCode == HttpStatusCode.Unauthorized)
                {
                    var errorContent = await metadataResponse.Content.ReadAsStringAsync();
                    throw new UnauthorizedAccessException($"You don't have permission to access this file. Server response: {errorContent}");
                }
                
                metadataResponse.EnsureSuccessStatusCode();
                
                var metadataJson = await metadataResponse.Content.ReadAsStringAsync();
                Console.WriteLine("\nReceived metadata:");
                Console.WriteLine(metadataJson);
                
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true
                };
                var metadata = System.Text.Json.JsonSerializer.Deserialize<ProcessedFileData>(metadataJson, options);

                // Validate metadata
                if (metadata == null || string.IsNullOrEmpty(metadata.FileId))
                {
                    throw new InvalidDataException("Invalid metadata format");
                }

                Console.WriteLine($"File Information:");
                Console.WriteLine($"- Name: {metadata.FileName}");
                Console.WriteLine($"- Size: {metadata.FileSize:N0} bytes");
                Console.WriteLine($"- Block IDs: {string.Join(", ", metadata.BlockIds ?? new List<string>())}");
                Console.WriteLine($"- Block Count: {metadata.BlockIds?.Count ?? 0}");
                Console.WriteLine($"- Raw metadata: {metadataJson}");

                // Step 2: Verify access rights
                if (!metadata.EncryptedKeysForPubKeys.ContainsKey(bobPubKeyBase64))
                {
                    throw new UnauthorizedAccessException("You don't have the necessary keys to decrypt this file.");
                }

                // Step 3: Create temporary directory with unique name
                var downloadDir = Path.Combine(
                    Path.GetTempPath(),
                    $"deltanium_download_{Guid.NewGuid()}"
                );
                Directory.CreateDirectory(downloadDir);
                Console.WriteLine($"\nStep 2: Created temporary directory: {downloadDir}");

                try
                {
                    // Step 4: Download and decrypt blocks
                    Console.WriteLine("\nStep 3: Downloading and decrypting blocks...");
                    var downloadedBlocks = new List<byte[]>();
                    var progress = new Progress<(int BlockNumber, int TotalBlocks)>(report =>
                    {
                        var percentage = (report.BlockNumber * 100) / report.TotalBlocks;
                        Console.WriteLine($"Progress: {percentage}% (Block {report.BlockNumber}/{report.TotalBlocks})");
                    });

                    if (metadata.BlockIds == null || metadata.BlockIds.Count == 0)
                    {
                        throw new InvalidDataException("No block IDs found in metadata");
                    }

                    for (int i = 0; i < metadata.BlockIds.Count; i++)
                    {
                        var blockId = metadata.BlockIds[i];
                        Console.WriteLine($"Downloading block {i + 1}/{metadata.BlockIds.Count}: {blockId}");
                        
                        var blockResponse = await httpClient.GetAsync(
                            $"http://localhost:5000/api/file/download/{blockId}",
                            HttpCompletionOption.ResponseHeadersRead // Stream the response
                        );

                        if (blockResponse.StatusCode == HttpStatusCode.NotFound)
                        {
                            throw new FileNotFoundException($"Block {i} not found. The file might be corrupted.");
                        }
                        
                        blockResponse.EnsureSuccessStatusCode();

                        // Download block with streaming
                        using (var stream = await blockResponse.Content.ReadAsStreamAsync())
                        using (var ms = new MemoryStream())
                        {
                            await stream.CopyToAsync(ms);
                            downloadedBlocks.Add(ms.ToArray());
                            Console.WriteLine($"Downloaded block {i + 1}: {ms.Length} bytes");
                        }

                        ((IProgress<(int, int)>)progress).Report((i + 1, metadata.BlockIds.Count));
                    }

                    // Step 5: Decrypt symmetric key
                    Console.WriteLine("\nStep 4: Decrypting file key...");
                    var encryptedSymmetricKey = metadata.EncryptedKeysForPubKeys[bobPubKeyBase64];
                    var symmetricKey = userKeys["bob"].Decrypt(encryptedSymmetricKey);

                    // Step 6: Decrypt and combine blocks
                    Console.WriteLine("\nStep 5: Combining decrypted blocks...");
                    var originalDir = Path.GetDirectoryName(testFilePath);
                    
                    // Create a dictionary to store decrypted blocks for each user
                    var userDecryptedBlocks = new Dictionary<string, List<byte[]>>();
                    
                    // First decrypt blocks for each user
                    foreach (var sharedUser in new[] { "bob", "charlie" })
                    {
                        if (metadata.EncryptedKeysForPubKeys.ContainsKey(Convert.ToBase64String(userKeys[sharedUser].PubKey.ToBytes())))
                        {
                            Console.WriteLine($"\nDecrypting blocks for {sharedUser}...");
                            var userSymmetricKey = userKeys[sharedUser].Decrypt(
                                metadata.EncryptedKeysForPubKeys[Convert.ToBase64String(userKeys[sharedUser].PubKey.ToBytes())]
                            );
                            
                            var userBlocks = new List<byte[]>();
                            for (int i = 0; i < downloadedBlocks.Count; i++)
                            {
                                var decryptedBlock = await cryptoService.DecryptDataAsync(downloadedBlocks[i], userSymmetricKey);
                                userBlocks.Add(decryptedBlock);
                                Console.WriteLine($"Decrypted block {i + 1}/{downloadedBlocks.Count} for {sharedUser}");
                            }
                            userDecryptedBlocks[sharedUser] = userBlocks;
                        }
                    }

                    // Then save decrypted files for each user
                    foreach (var kvp in userDecryptedBlocks)
                    {
                        var user = kvp.Key;
                        var userBlocks = kvp.Value;
                        
                        var decryptedFileName = $"decrypted_{user}_{metadata.FileName}";
                        var decryptedFilePath = Path.Combine(originalDir, decryptedFileName);
                        
                        using (var outputStream = File.Create(decryptedFilePath))
                        {
                            foreach (var block in userBlocks)
                            {
                                await outputStream.WriteAsync(block);
                            }
                        }
                        
                        Console.WriteLine($"\nSaved decrypted file for {user} at: {decryptedFilePath}");
                        
                        // Verify file size
                        var fileInfo = new FileInfo(decryptedFilePath);
                        if (fileInfo.Length != metadata.FileSize)
                        {
                            Console.WriteLine($"Warning: File size mismatch for {user}. Expected: {metadata.FileSize}, Got: {fileInfo.Length}");
                        }
                        else
                        {
                            Console.WriteLine($"File size verification for {user}: SUCCESS");
                        }
                    }

                    return;
                }
                finally
                {
                    // Step 8: Cleanup
                    try
                    {
                        if (Directory.Exists(downloadDir))
                        {
                            Directory.Delete(downloadDir, true);
                            Console.WriteLine("\nCleaned up temporary files");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Failed to clean up temporary directory: {ex.Message}");
                    }
                }
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"\nNetwork error: {ex.Message}");
            if (ex.StatusCode.HasValue)
            {
                Console.WriteLine($"Status code: {ex.StatusCode}");
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"\nAccess denied: {ex.Message}");
        }
        catch (FileNotFoundException ex)
        {
            Console.WriteLine($"\nFile not found: {ex.Message}");
        }
        catch (InvalidDataException ex)
        {
            Console.WriteLine($"\nData integrity error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nUnexpected error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }

        Console.WriteLine("\n=== File Download and Decryption Demo Completed ===");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\nError in upload demo: {ex.Message}");
    }
}

// Execute the demonstrations
Console.WriteLine("\nDemonstrating transactions between users...");
DemoTransactions();

// Call the demo functions
DemoEncryptionWithKey();
DemoSymmetricKeyEncryption();

Console.WriteLine("\nDemonstrating file handling...");
DemoFileHandlingAsync().Wait();

Console.WriteLine("\nDemonstrating file handling with Real file...");
DemoFileHandlingWithRealFileAsync().Wait();

Console.WriteLine("\nDemonstrating file upload to Deltanium Store...");
DemoUploadToDeltaniumStoreAsync().Wait();

namespace Deltanium
{
    // Secure key storage class
    public class SecureKeyStorage
    {
        private readonly string _keyStorePath;
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public SecureKeyStorage(string keyStorePath)
        {
            _keyStorePath = keyStorePath;
            Console.WriteLine($"Initializing SecureKeyStorage at path: {keyStorePath}");
            
            // Generate a random key and IV for AES encryption
            using (var aes = Aes.Create())
            {
                _key = aes.Key;
                _iv = aes.IV;
            }
        }

        public void SaveKey(Key key, string keyId)
        {
            try
            {
                Console.WriteLine($"Saving key for user: {keyId}");
                
                // Convert key to bytes
                byte[] keyBytes = key.ToBytes();
                Console.WriteLine($"Key bytes length: {keyBytes.Length}");
                
                // Encrypt the key using AES
                byte[] encryptedKey;
                using (var aes = Aes.Create())
                {
                    aes.Key = _key;
                    aes.IV = _iv;
                    aes.Padding = PaddingMode.PKCS7;
                    
                    using (var encryptor = aes.CreateEncryptor())
                    {
                        encryptedKey = encryptor.TransformFinalBlock(keyBytes, 0, keyBytes.Length);
                    }
                }
                Console.WriteLine($"Encrypted key length: {encryptedKey.Length}");

                // Save encrypted key to file
                string keyPath = Path.Combine(_keyStorePath, $"{keyId}.key");
                File.WriteAllBytes(keyPath, encryptedKey);
                
                // Save public key separately
                byte[] pubKeyBytes = key.PubKey.ToBytes();
                string pubKeyPath = Path.Combine(_keyStorePath, $"{keyId}.pub");
                File.WriteAllBytes(pubKeyPath, pubKeyBytes);
                
                Console.WriteLine($"Successfully saved key files for {keyId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SaveKey for {keyId}: {ex.Message}");
                throw new Exception($"Failed to save key: {ex.Message}");
            }
        }

        public Key LoadKey(string keyId)
        {
            try
            {
                Console.WriteLine($"Loading key for user: {keyId}");
                string keyPath = Path.Combine(_keyStorePath, $"{keyId}.key");
                
                if (!File.Exists(keyPath))
                {
                    Console.WriteLine($"Key file not found at: {keyPath}");
                    throw new FileNotFoundException($"Key file not found: {keyId}");
                }

                // Read encrypted key from file
                byte[] encryptedKey = File.ReadAllBytes(keyPath);
                Console.WriteLine($"Read encrypted key, length: {encryptedKey.Length}");

                // Decrypt the key using AES
                byte[] decryptedKey;
                using (var aes = Aes.Create())
                {
                    aes.Key = _key;
                    aes.IV = _iv;
                    aes.Padding = PaddingMode.PKCS7;
                    
                    using (var decryptor = aes.CreateDecryptor())
                    {
                        decryptedKey = decryptor.TransformFinalBlock(encryptedKey, 0, encryptedKey.Length);
                    }
                }
                Console.WriteLine($"Decrypted key length: {decryptedKey.Length}");

                // Convert bytes back to Key object
                var key = new Key(decryptedKey);
                Console.WriteLine($"Successfully loaded key for {keyId}");
                return key;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in LoadKey for {keyId}: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw new Exception($"Failed to load key: {ex.Message}");
            }
        }

        public void DeleteKey(string keyId)
        {
            try
            {
                string keyPath = Path.Combine(_keyStorePath, $"{keyId}.key");
                string pubKeyPath = Path.Combine(_keyStorePath, $"{keyId}.pub");
                
                if (File.Exists(keyPath))
                {
                    File.Delete(keyPath);
                }
                
                if (File.Exists(pubKeyPath))
                {
                    File.Delete(pubKeyPath);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to delete key: {ex.Message}");
            }
        }

        public byte[] LoadPublicKeyData(string userId)
        {
            try
            {
                string pubKeyPath = Path.Combine(_keyStorePath, $"{userId}.pub");
                
                if (!File.Exists(pubKeyPath))
                {
                    throw new FileNotFoundException($"Public key file not found: {userId}");
                }
                
                return File.ReadAllBytes(pubKeyPath);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load public key: {ex.Message}");
            }
        }

        public async Task StoreKeyAsync(string userId, byte[] keyBytes)
        {
            try
            {
                Console.WriteLine($"StoreKeyAsync for user: {userId}, bytes length: {keyBytes.Length}");
                
                // Create a Key object from bytes
                var key = new Key(keyBytes);
                
                // Use the synchronous SaveKey method which handles both private and public keys
                SaveKey(key, userId);
                
                Console.WriteLine($"Successfully stored key for {userId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in StoreKeyAsync for {userId}: {ex.Message}");
                throw new Exception($"Failed to store key: {ex.Message}");
            }
        }
    }

    // Adapter to use Key with IUserKeyStore interface 
    public class CryptoKeyAdapter : Deltanium.Core.Cryptography.IUserKeyStore
    {
        private readonly SecureKeyStorage _secureKeyStorage;
        private readonly Dictionary<string, Key> _keys;
        
        public CryptoKeyAdapter(SecureKeyStorage secureKeyStorage)
        {
            _secureKeyStorage = secureKeyStorage;
            _keys = new Dictionary<string, Key>();
            Console.WriteLine("Initialized CryptoKeyAdapter");
        }
        
        public async Task<byte[]> GetPublicKeyAsync(string userId)
        {
            try
            {
                Console.WriteLine($"GetPublicKeyAsync for user: {userId}");
                
                // Load the key if not already in memory
                if (!_keys.ContainsKey(userId))
                {
                    Console.WriteLine($"Key not in memory for {userId}, loading from storage");
                    var key = _secureKeyStorage.LoadKey(userId);
                    _keys[userId] = key;
                }
                else
                {
                    Console.WriteLine($"Using cached key for {userId}");
                }
                
                // Return the public key bytes
                var pubKeyBytes = _keys[userId].PubKey.ToBytes();
                Console.WriteLine($"Retrieved public key for {userId}, length: {pubKeyBytes.Length}");
                return pubKeyBytes;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetPublicKeyAsync for {userId}: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw new KeyNotFoundException($"Public key not found for user: {userId}. Error: {ex.Message}");
            }
        }
        
        public async Task<byte[]> GetPrivateKeyAsync(string userId, string password)
        {
            try
            {
                Console.WriteLine($"GetPrivateKeyAsync for user: {userId}");
                
                // Load the key if not already in memory
                if (!_keys.ContainsKey(userId))
                {
                    Console.WriteLine($"Key not in memory for {userId}, loading from storage");
                    var key = _secureKeyStorage.LoadKey(userId);
                    _keys[userId] = key;
                }
                else
                {
                    Console.WriteLine($"Using cached key for {userId}");
                }
                
                // Return the private key bytes
                var privateKeyBytes = _keys[userId].ToBytes();
                Console.WriteLine($"Retrieved private key for {userId}, length: {privateKeyBytes.Length}");
                return privateKeyBytes;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetPrivateKeyAsync for {userId}: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw new KeyNotFoundException($"Private key not found for user: {userId}. Error: {ex.Message}");
            }
        }
    }

    // Add JsonPropertyName attributes to properly map JSON response
    public class FileUploadResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("fileId")]
        public string FileId { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("fileName")]
        public string FileName { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("fileSize")]
        public long FileSize { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("mimeType")]
        public string MimeType { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("fileExtension")]
        public string FileExtension { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("creationTime")]
        public DateTime CreationTime { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("ownerPubKey")]
        public string OwnerPubKey { get; set; }
    }
}
