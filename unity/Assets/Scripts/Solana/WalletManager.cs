using UnityEngine;
using Solana.Unity.Rpc;
using Solana.Unity.Wallet;
using Solana.Unity.Programs;
using System.Threading.Tasks;

namespace NeuralDraft.Unity {
    public class WalletManager : MonoBehaviour {
        public static WalletManager Instance { get; private set; }
        
        [Header("Solana Settings")]
        public string rpcUrl = "https://api.devnet.solana.com";
        public string programId = "YOUR_PROGRAM_ID_HERE";
        
        [Header("Wallet")]
        public bool isConnected;
        public string walletAddress;
        public double balance;
        
        private IRpcClient _rpcClient;
        private Wallet.Wallet _wallet;
        
        void Awake() {
            Instance = this;
            _rpcClient = ClientFactory.GetClient(rpcUrl);
        }
        
        public async Task ConnectWallet(string privateKey) {
            try {
                _wallet = new Wallet.Wallet(privateKey);
                walletAddress = _wallet.Account.PublicKey.ToString();
                
                var balance = await _rpcClient.GetBalanceAsync(_wallet.Account.PublicKey);
                this.balance = balance.Result.Value / 1000000000.0;
                
                isConnected = true;
                Debug.Log($"[WalletManager] Connected: {walletAddress.Substring(0, 8)}... ({(float)this.balance:F2} SOL)");
            } catch (System.Exception e) {
                Debug.LogError($"[WalletManager] Connection failed: {e.Message}");
            }
        }
        
        public async Task Disconnect() {
            _wallet = null;
            walletAddress = null;
            balance = 0;
            isConnected = false;
            Debug.Log("[WalletManager] Disconnected");
        }
        
        public async Task<double> GetBalance() {
            if (!isConnected || _wallet == null) return 0;
            
            try {
                var result = await _rpcClient.GetBalanceAsync(_wallet.Account.PublicKey);
                balance = result.Result.Value / 1000000000.0;
                return balance;
            } catch {
                return 0;
            }
        }
        
        public async Task<string> MintComboNFT(ComboData combo) {
            if (!isConnected || _wallet == null) {
                Debug.LogError("[WalletManager] Wallet not connected");
                return null;
            }
            
            try {
                // This would call the Anchor program
                Debug.Log($"[WalletManager] Minting combo: {combo.name}");
                
                // Simulate minting delay
                await Task.Delay(2000);
                
                string comboMintId = System.Guid.NewGuid().ToString().Substring(0, 8);
                Debug.Log($"[WalletManager] Combo NFT minted: {comboMintId}");
                
                return comboMintId;
            } catch (System.Exception e) {
                Debug.LogError($"[WalletManager] Mint failed: {e.Message}");
                return null;
            }
        }
        
        public async Task<bool> PurchaseSkin(int skinId, double price) {
            if (!isConnected || _wallet == null) return false;
            
            if (balance < price) {
                Debug.LogWarning("[WalletManager] Insufficient balance");
                return false;
            }
            
            try {
                // Simulate purchase
                await Task.Delay(1000);
                balance -= price;
                Debug.Log($"[WalletManager] Purchased skin {skinId} for {price} SOL");
                return true;
            } catch (System.Exception e) {
                Debug.LogError($"[WalletManager] Purchase failed: {e.Message}");
                return false;
            }
        }
    }
    
    [System.Serializable]
    public class ComboData {
        public string name;
        public int characterId;
        public int damage;
        public int hits;
        public string[] moves;
    }
    
    [System.Serializable]
    public class SkinData {
        public int id;
        public string name;
        public int characterId;
        public double price;
        public string previewUrl;
        public bool owned;
    }
}
