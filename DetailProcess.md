Deltanium is a blockchain network that stores transactions, uses D coin as its internal currency, and stores files, media, and content such as posts, images, videos, and livestreams (collectively called content or files) on the Deltanium social network. The network includes the following node types:

Block creator nodes process transactions, add them to the blockchain, and have their own public/private key pair.

User nodes have their own public/private key pair and send requests to the blockchain network, such as uploading files, creating content, performing actions on content (like, love, donate), or transferring coins to other users.

Content store nodes store content, files, images, videos, and similar data. They also have a key pair used to authenticate that a file returned to a user was stored on that node.

Inspector nodes audit and re-check blocks. If a block is violating or invalid, it is immediately penalized in coin and marked invalid on the blockchain.

Router nodes control file send and download routing: which content store node will store a file, and which node a file will be downloaded from.

Operation:

Users can upload files, create content, and submit it to the blockchain. On creation they sign the content to prove ownership.

Users can share content with specific people or a group, or make it public so the whole network can view it.

Only the owner may edit and update a file or content. On update they re-sign the content, and the blockchain automatically increments the content version.

Files submitted to the blockchain are distributed across several content store nodes. When a user requests to view a file, the content is signed and returned. Because multiple nodes store the same file, the system may choose a node at random, designate a specific node (for example a primary node), or pick the node that is most available and can return the file fastest.

When a user submits content, besides paying a transaction fee to block creator nodes, they pay most of the fee to content store nodes.

When a user likes, loves, or donates to content, the content owner receives a fee and any donation from that user. Block creator nodes also receive a block-creation fee.

Users can share files or content with selected users or groups. Files stored on content nodes must be encrypted so that the storing node itself cannot read them. Only the owner and users who were granted access can view the content.

The owner can update or delete content on the blockchain. Each update increments the version. The owner can also revoke access for other users.

Consensus:

A central node manages and designates other nodes, accessed via deltanium.com. A node may create blocks or store content only after this central node has authenticated it and signed that authorization onto the blockchain.

When a transaction occurs (transfer, like, love, donate, and so on), it is distributed to all block creator nodes and also sent to the center. The center decides which node may create the next block and signs that decision. The selected node creates the block and publishes it. Block creator nodes must stake a deposit; if no new block is created within a time limit, that stake is forfeited.

Newly created blocks are validated by all other block creator nodes. First, the block must have been created by the node the center authorized. Second, the block must be valid: every transaction in it must be valid. If so, the validating node adds the block to its local chain and signals acceptance to the network. Otherwise it reports rejection to the center and to inspector nodes.

Inspector nodes also receive the new block. They wait for all block creator confirmations. If some replies mark the block invalid, the inspector re-runs validation. If invalid, it sends a signed cancellation request. If valid, it does nothing. Periodically it also randomly audits blocks even when every block creator reported the block as valid.

If the selected block creator is slower than the time allowed after the center issued the create-block request, the center cancels that order, sends a new one to a different node, and penalizes the late node a set amount of coin.

Content store node selection:

When a file storage request reaches the network, the user node splits the file into chunks and encrypts those chunks with a random key (encrypted content). That random key is then encrypted to each public key of the users or groups the file is shared with. The node also hashes each chunk from version, creator, creation time, encrypted content, previous-chunk hash, and next-chunk hash to produce a unique content id. A file id is the Merkle-tree hash of all unique content ids. The package is sent to one or more router nodes.

The user node signs an upload request. Content store nodes that can accept it sign the request and return it. If the user agrees, they record that agreement and sign again, because they may refuse some content nodes. The user then sends the signed, hashed chunks to those content nodes, either directly or via shipper nodes. Shipper nodes confirm that chunks are valid, that they were received from the user, and that they were delivered to the content node.

For downloads, the user node sends a signed download request. Content nodes that can serve the file sign the request. If the user agrees to download from that node, they record agreement and sign. The user then receives chunks from the content node directly or via a shipper.

Shipper nodes must log send and receive activity for two months, which inspectors may need during investigations.

To speed transfers, P2P direct transfer with content store nodes may be used. Depending on bandwidth and transfer speed, the receiving user node chooses which content node to fetch each file or chunk from.

To prove the user received the content, the content node re-encrypts the chunk with a random key (re-encrypted content) and sends both that key and the re-encrypted content to the inspector. The inspector hashes the re-encrypted content together with the random key. The content node sends only the re-encrypted content to the user first. After the user agrees, signs, and publishes that on the blockchain, the content node sends the random key to the user. Keys sent to the inspector or the user are encrypted to that node's public key first, so only the intended recipient can decrypt them. All of these transactions are recorded on the blockchain for transparency.
