# Deltanium Blockchain Network

## Production

| Service | URL |
|---------|-----|
| Landing | [https://deltanium.com](https://deltanium.com) |
| App | [https://app.deltanium.com](https://app.deltanium.com) |
| Central API | [https://api.deltanium.com](https://api.deltanium.com) |
| Store nodes | [store-1](https://store-1.deltanium.com) · [store-2](https://store-2.deltanium.com) |
| Block creator | [https://blocker-1.deltanium.com](https://blocker-1.deltanium.com) |

This library is the shared crypto / PRE core used by those services; it has no public HTTP endpoint of its own.

## Overview
Deltanium is a blockchain network that facilitates transactions using D coin as its native currency. The network enables storage of various types of content including files, media, articles, images, videos, and livestreams on a decentralized social network infrastructure.

## Network Architecture

### Node Types
The Deltanium network consists of five specialized node types:

1. **Block Creator Nodes**
   - Process transactions and add them to the blockchain
   - Maintain their own unique public/private key pair
   - Validate blocks created by other nodes

2. **User Nodes**
   - Each user has a unique public/private key pair
   - Send requests to the blockchain network (upload files, create content)
   - Perform actions on content (like, love, donate)
   - Transfer D coins to other users

3. **Content Store Nodes**
   - Store content, files, images, videos, etc.
   - Maintain their own key pair for authenticating file delivery
   - Encrypt content for secure storage

4. **Inspector Nodes**
   - Audit and verify blocks
   - Mark invalid blocks and impose penalties
   - Randomly check blocks for compliance

5. **Router Nodes**
   - Determine which Content Store node will store uploaded files
   - Direct file retrieval requests to appropriate Content Store nodes
   - Optimize content delivery based on availability and performance

## Core Functionality

### Content Management
- Users can upload files and create content on the blockchain
- Content is signed by creators to establish ownership
- Content can be shared with specific users, groups, or made public
- Only the original owner can modify or update content
- Each update automatically increases the content version

### Content Distribution
- Files uploaded to the blockchain are distributed across multiple Content Store nodes
- When users request content, it is signed and delivered from a Content Store node
- The system can choose nodes randomly or based on availability and performance
- Node selection may also follow specific rules (primary node designation)

### Transaction Fees
- Users pay transaction fees to Block Creator nodes
- Additional fees are paid to Content Store nodes for file storage
- When users interact with content (like, love, donate), the content owner receives a portion
- Block creators receive fees for processing these transactions

### Privacy and Security
- Content stored on nodes is encrypted
- Even the storing node cannot read the content
- Only the owner and authorized users can access the content
- Owners can update access permissions or delete content

## Consensus Mechanism

### Central Coordination
- A central node (accessible via deltanium.com) validates and authorizes other nodes
- Nodes must be authenticated by the central node to create blocks or store content

### Transaction Processing
- Transactions (transfers, likes, donations) are broadcast to all Block Creator nodes
- The central node selects which Block Creator will create the next block
- The chosen node must create a block within a specified time limit
- Failure to create a block in time results in penalties

### Block Validation
- New blocks must be created by authorized nodes
- All transactions in the block must be valid
- Block Creator nodes validate new blocks and signal acceptance
- If invalid, nodes report to the central node and Inspector nodes

### Inspection Process
- Inspector nodes receive new blocks
- They wait for Block Creator validations
- If reports indicate issues, they re-validate the block
- They can request block cancellation if invalid
- They periodically perform random block audits

## Content Storage and Retrieval

### Upload Process
1. User nodes split files into chunks and encrypt them
2. A unique content ID is generated for each chunk
3. A file ID is created using a Merkle tree of all chunk IDs
4. The request is sent to Router nodes
5. User signs the upload request
6. Content Store nodes sign to accept storage responsibility
7. User approves specific Content Store nodes
8. Content chunks are transmitted to approved nodes

### Download Process
1. User sends a signed download request
2. Content Store nodes sign to indicate availability
3. User approves specific Content Store nodes
4. Content is delivered directly or via shipper nodes
5. Re-encrypted content verification ensures delivery integrity

### Transfer Optimization
- P2P direct transfer capabilities for speed
- Shipper nodes maintain logs for 2 months for auditing
- Multi-source downloading based on network conditions
- Encrypted verification process ensures content integrity

## Security Features
- All communications are signed
- Content is double-encrypted for transmission
- Inspector nodes verify content transfers
- Public/private key encryption ensures only authorized access
- All transactions are recorded on the blockchain for transparency

## Implementation
This core implementation provides the foundation for the Deltanium blockchain network, with support for the consensus mechanism, transaction processing, and node communication protocols.

### Proxy Re-Encryption (PRE) Support

This repository provides Option 2 follower sharing using Proxy Re-Encryption (PRE) with Umbral.

- Library and interop:
  - Native C-ABI shim: `native/umbral_pre/` (Rust)
  - Build script: `scripts/build_umbral_pre.sh`
  - .NET interop: `Core/Cryptography/Pre/UmbralPreInterop.cs` (P/Invoke)
- Policy tags: use epoch-based tags like `followers:YYYY` (e.g., `followers:2025`).
- Flow: encapsulate symmetric key K to author with tag → generate rekey A→B → re-encrypt capsule client-side → recipient decapsulates and decrypts content with K.

#### Build the native Umbral PRE library

Prerequisites:
- Rust toolchain (cargo/rustc)
- git

Note about sources: Umbral is vendored under `.vendor/rust-umbral` (tracked as a submodule). Either initialize the submodule, or let the build script clone/update it on demand.

Option A (recommended, submodule):
```bash
cd deltanium-core
git submodule update --init --recursive
chmod +x scripts/build_umbral_pre.sh
# Copy artifact next to any host that loads the native PRE library
scripts/build_umbral_pre.sh Release ./artifacts
```

Option B (script-managed vendor clone):
```bash
cd deltanium-core
chmod +x scripts/build_umbral_pre.sh
scripts/build_umbral_pre.sh Release ./artifacts
```

Artifacts (per platform):
- macOS: `libumbral_pre.dylib`
- Linux: `libumbral_pre.so`
- Windows: `umbral_pre.dll`

## System Architecture

### Layered Architecture
- **Blockchain Core Layer**: Core blockchain functionality, consensus, transaction management
- **Network Layer**: P2P infrastructure, inter-node communication
- **Content Storage Layer**: Distributed content storage
- **Security Layer**: Encryption, authentication, and access control
- **API Layer**: Interfaces for application communication
- **Application Layer**: End-user applications

### Deployment Model
- **Central Coordinator Cluster**: Central server cluster (deltanium.com)
- **Distributed Node Network**: Network of distributed nodes
- **CDN Integration**: Integration with CDNs for content distribution
- **Client Applications**: Applications for end users

## Required Services and Applications

### Core Services

#### Central Coordination Service
- Authentication Authority
- Node Registry
- Block Creation Scheduler
- Health Monitoring

#### Blockchain Core Services
- Consensus Engine
- Transaction Processor
- Block Validator
- Chain Manager
- Merkle Tree Generator

#### Content Management Services
- Content Chunking Service
- Content Indexing Service
- Version Control Service
- Content Routing Service

### Node Software

#### Block Creator Node
- Transaction Pool Manager
- Block Assembly Service
- Validation Engine
- Node Communication Protocol

#### Content Store Node
- Distributed Storage Engine
- Content Encryption Service
- Content Delivery Service
- Storage Capacity Manager

#### Inspector Node
- Blockchain Auditor
- Validation Inspector
- Penalty Manager
- Audit Logging System

#### Router Node
- Load Balancing Service
- Route Optimization Engine
- Network Topology Manager
- Transfer Protocol Selector

#### User Node
- Wallet Service
- Content Creation Tool
- Access Control Manager
- File Encryption Client

### Security Infrastructure

#### Cryptography Services
- Key Management System
- Signature Verification Service
- Encryption Standards Implementation
- Zero-Knowledge Proof System

#### Authentication & Authorization
- Identity Provider
- Access Control Service
- Permission Management
- Multi-factor Authentication

### User-Facing Applications

#### Deltanium Wallet
- Web & mobile wallet
- Coin, transaction, and content management
- Social features integration

#### Deltanium Creator Platform
- Content creation and management platform
- Publishing and sharing tools
- Creative tools integration

#### Deltanium Browser
- Content browser for the Deltanium network
- Social and content discovery features
- Wallet and payment tools integration

#### Admin Dashboard
- Control panel for node operators
- Monitoring and management tools
- Performance reporting and analytics

### Monitoring & Management

#### Network Monitoring System
- Node Health Monitor
- Performance Analytics
- Alert System

#### Logging Infrastructure
- Distributed Logging System
- Log Analysis Tools
- Audit Trail System

#### Developer Tools
- Blockchain Explorer
- API Testing Suite
- Smart Contract Development Kit

## Implementation Phases

### Phase 1: Core Infrastructure
- Deploy Central Coordination Service
- Develop basic Block Creator Node
- Build basic consensus mechanism
- Create simple wallet

### Phase 2: Content Management
- Deploy Content Store Node
- Develop content encryption and storage system
- Build file transfer protocols

### Phase 3: Verification & Security
- Deploy Inspector Node
- Complete verification and checking system
- Enhance security layers

### Phase 4: Performance & Scalability
- Deploy Router Node
- Optimize network performance
- Expand storage capabilities

### Phase 5: User Applications
- Develop end-user applications
- Integrate social features
- Complete ecosystem

## License

MIT — see [LICENSE](LICENSE). 