/* ================================================================================
   SOLANA COMBO MINTING CLIENT
   Solana SDK for .NET Integration
================================================================================ */
using Solnet.Programs;
using Solnet.Programs.Abstract;
using Solnet.Programs.Models;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Models;
using Solnet.Wallet;
using System.Text;

namespace NeuralDraft.Trading
{
    /// <summary>
    /// Solana client for combo NFT minting
    /// Note: Requires Solana SDK for .NET (Solnet) NuGet package
    /// Install: dotnet add package Solnet
    /// </summary>
    public class SolComboClient : IDisposable
    {
        private readonly IRpcClient _rpcClient;
        private readonly Wallet.Wallet _wallet;
        private readonly PublicKey _comboProgramId;
        private readonly PublicKey _comboMintAuthority;
        private readonly Connection _connection;
        private bool _disposed;

        public SolComboClient(string rpcUrl, string walletPrivateKey, string comboProgramId)
        {
            _rpcClient = ClientFactory.GetClient(rpcUrl);
            _wallet = new Wallet.Wallet(walletPrivateKey);
            _comboProgramId = new PublicKey(comboProgramId);
            _comboMintAuthority = _wallet.Account.PublicKey;
            _connection = new Connection(rpcUrl, Commitment.Confirmed);
        }

        /// <summary>
        /// Mint a combo as an NFT on Solana
        /// </summary>
        public async Task<string> MintComboAsync(ComboNFTData comboData)
        {
            byte[] comboHash = ComputeComboHash(comboData);
            PublicKey comboPDA = DeriveComboPDA(comboHash);
            var blockHash = await _rpcClient.GetLatestBlockHashAsync();

            var transaction = new TransactionBuilder()
                .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                .SetFeePayer(_wallet.Account.PublicKey)
                .AddInstruction(CreateComboInstruction(comboData, comboPDA))
                .Build(_wallet.Account);

            var result = await _rpcClient.SendTransactionAsync(transaction);
            
            if (result.WasSuccessful)
                return result.Result;
            else
                throw new Exception("Transaction failed: " + (result.Error?.Message ?? "Unknown error"));
        }

        private TransactionInstruction CreateComboInstruction(ComboNFTData comboData, PublicKey comboPDA)
        {
            var keys = new List<AccountMeta>
            {
                AccountMeta.Writable(_wallet.Account.PublicKey, isSigner: true),
                AccountMeta.ReadOnly(comboPDA, isSigner: false),
                AccountMeta.ReadOnly(_comboMintAuthority, isSigner: false),
                AccountMeta.ReadOnly(SystemProgram.ProgramIdKey, isSigner: false),
                AccountMeta.ReadOnly(Sysvars.RentKey, isSigner: false),
            };

            var data = new byte[256];
            int offset = 0;
            data[offset++] = 0; // Instruction enum

            byte[] nameBytes = Encoding.UTF8.GetBytes(comboData.ComboName.PadRight(64, '\0'));
            Buffer.BlockCopy(nameBytes, 0, data, offset, 64);
            offset += 64;

            BitConverter.TryWriteBytes(data.AsSpan(offset, 4), comboData.Damage);
            offset += 4;
            BitConverter.TryWriteBytes(data.AsSpan(offset, 4), comboData.MeterGain);
            offset += 4;
            data[offset++] = (byte)comboData.MoveCount;
            Buffer.BlockCopy(ComputeComboHash(comboData), 0, data, offset, 32);
            offset += 32;
            data[offset++] = (byte)comboData.CharacterId;

            return new TransactionInstruction
            {
                ProgramId = _comboProgramId,
                Keys = keys,
                Data = data.Take(offset).ToArray()
            };
        }

        private PublicKey DeriveComboPDA(byte[] comboHash)
        {
            byte[] seeds = new byte[40];
            Buffer.BlockCopy(comboHash, 0, seeds, 0, 32);
            Buffer.BlockCopy(_comboMintAuthority.KeyBytes, 0, seeds, 32, 8);
            return PublicKey.FindProgramAddressAddress(seeds, _comboProgramId).Key;
        }

        private byte[] ComputeComboHash(ComboNFTData comboData)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var input = new StringBuilder();
            input.Append(comboData.CharacterId);
            input.Append(comboData.ComboName);
            input.Append(comboData.Damage);
            input.Append(comboData.MeterGain);
            input.Append(comboData.MoveCount);
            foreach (var move in comboData.MoveSequence)
            {
                input.Append((int)move);
            }
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(input.ToString()));
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }

    public class ComboNFTData
    {
        public int CharacterId { get; set; }
        public string ComboName { get; set; } = "";
        public int Damage { get; set; }
        public int MeterGain { get; set; }
        public int MoveCount { get; set; }
        public JasonMoveType[] MoveSequence { get; set; } = Array.Empty<JasonMoveType>();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string ExecutingPlayer { get; set; } = "";
    }
}
