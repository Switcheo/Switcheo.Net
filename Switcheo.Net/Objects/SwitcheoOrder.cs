﻿using CryptoExchange.Net.Converters;
using NeoModules.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Switcheo.Net.Converters;
using System;
using System.Collections.Generic;

namespace Switcheo.Net.Objects
{
    /// <summary>
    /// Information about an order
    /// Orders are instructions to buy or sell assets on Switcheo Exchange
    /// </summary>
    public class SwitcheoOrder
    {
        /// <summary>
        /// The order id generated by Switcheo
        /// </summary>
        [JsonProperty("id")]
        public Guid Id { get; set; }

        /// <summary>
        /// The blockchain that the order exists on
        /// </summary>
        [JsonProperty("blockchain")]
        [JsonConverter(typeof(BlockchainTypeConverter))]
        public BlockchainType Blockchain { get; set; }

        /// <summary>
        /// Contract hash that the order is on
        /// </summary>
        [JsonProperty("contract_hash")]
        public string ContractHash { get; set; }

        /// <summary>
        /// Wallet Address of the order maker
        /// </summary>
        [JsonProperty("address")]
        public string Address { get; set; }

        /// <summary>
        /// Whether the order maker is buying or selling
        /// </summary>
        [JsonProperty("side")]
        [JsonConverter(typeof(OrderSideConverter))]
        public OrderSide Side { get; set; }

        /// <summary>
        /// Asset of the token that the order maker is offering
        /// </summary>
        [JsonProperty("offer_asset_id")]
        [JsonConverter(typeof(TokenConverter), true)]
        public SwitcheoToken OfferAsset { get; set; }

        /// <summary>
        /// Asset of the token that the order maker wants
        /// </summary>
        [JsonProperty("want_asset_id")]
        [JsonConverter(typeof(TokenConverter), true)]
        public SwitcheoToken WantAsset { get; set; }

        [JsonProperty("offer_amount")]
        private string _OfferAmount { get; set; }

        /// <summary>
        /// Total amount of the token that the order maker is offering
        /// </summary>
        [JsonIgnore]
        public decimal OfferAmount
        {
            get
            {
                return SwitcheoHelpers.FromAssetAmount(this._OfferAmount, this.OfferAsset?.Precision);
            }
            set
            {
                this._OfferAmount = SwitcheoHelpers.ToAssetAmount(value);
            }
        }

        [JsonProperty("want_amount")]
        private string _WantAmount { get; set; }

        /// <summary>
        /// Total amount of the token that the order maker wants
        /// </summary>
        [JsonIgnore]
        public decimal WantAmount
        {
            get
            {
                return SwitcheoHelpers.FromAssetAmount(this._WantAmount, this.WantAsset?.Precision);
            }
            set
            {
                this._WantAmount = SwitcheoHelpers.ToAssetAmount(value);
            }
        }

        [JsonProperty("transfer_amount")]
        private string _TransferAmount { get; set; }

        /// <summary>
        /// Amount (out of the OfferAmount) that was deposited into the contract in order to create the order
        /// </summary>
        [JsonIgnore]
        public decimal TransferAmount
        {
            get
            {
                return SwitcheoHelpers.FromAssetAmount(this._TransferAmount, this.OfferAsset?.Precision);
            }
        }

        [JsonProperty("priority_gas_amount")]
        private string _PriorityGasAmount { get; set; }

        /// <summary>
        /// Amount of gas paid by the order maker as priority
        /// </summary>
        [JsonIgnore]
        public decimal PriorityGasAmount
        {
            get
            {
                return SwitcheoHelpers.FromAssetAmount(this._PriorityGasAmount);
            }
        }

        /// <summary>
        /// Whether SWTH tokens was used by the order maker to pay taker fees
        /// </summary>
        [JsonProperty("use_native_token")]
        public bool UseNativeToken { get; set; }

        [JsonProperty("native_fee_transfer_amount")]
        private string _NativeFeeTransferAmount { get; set; }

        /// <summary>
        /// Amount of SWTH that was deposited into the contract in order to pay the taker fees of the order
        /// </summary>
        [JsonIgnore]
        public decimal NativeFeeTransferAmount
        {
            get
            {
                return SwitcheoHelpers.FromAssetAmount(this._NativeFeeTransferAmount);
            }
        }

        /// <summary>
        /// Transaction that was used for deposits related to the order creation
        /// </summary>
        [JsonProperty("deposit_txn")]
        public SwitcheoTransaction DepositTransaction { get; set; }

        /// <summary>
        /// Time when the order was created
        /// </summary>
        [JsonProperty("created_at")]
        [JsonConverter(typeof(UTCDateTimeConverter))]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Status of the order
        /// </summary>
        [JsonProperty("status")]
        [JsonConverter(typeof(OrderStatusConverter))]
        public OrderStatus Status { get; set; }

        /// <summary>
        /// Fills of the order
        /// </summary>
        [JsonProperty("fills")]
        public SwitcheoFill[] Fills { get; set; }

        /// <summary>
        /// Makes of the order
        /// </summary>
        [JsonProperty("makes")]
        public SwitcheoMake[] Makes { get; set; }

        public JObject GetSignatures(SwitcheoAuthenticationProvider authProvider)
        {
            var signatures = new JObject();

            Dictionary<string, string> fillsSignatures = new Dictionary<string, string>();
            foreach (var fill in this.Fills)
            {
                var signedTransaction = fill.Transaction.ToSignedTransaction();

                var serializedTransaction = signedTransaction.Serialize(false);
                var signature = authProvider.Sign(serializedTransaction).ToHexString();

                fillsSignatures.Add(fill.Id.ToString(), signature);
            }
            signatures["fills"] = JToken.FromObject(fillsSignatures);

            Dictionary<string, string> makesSignatures = new Dictionary<string, string>();
            foreach (var make in this.Makes)
            {
                var signedTransaction = make.Transaction.ToSignedTransaction();

                var serializedTransaction = signedTransaction.Serialize(false);
                var signature = authProvider.Sign(serializedTransaction).ToHexString();

                makesSignatures.Add(make.Id.ToString(), signature);
            }
            signatures["makes"] = JToken.FromObject(makesSignatures);

            return signatures;
        }

        public override string ToString()
        {
            return string.Format("{{ Id : {0}, Blockchain : {1}, ContractHash : {2}, Address : {3}, Side : {4}, OfferAsset : {5}, WantAsset : {6}, OfferAmount : {7}, WantAmount : {8}, TransferAmount : {9}, PriorityGasAmount : {10}, UseNativeToken : {11}, NativeFeeTransferAmount : {12}, DepositTransaction : {13}, CreatedAt : {14}, Status : {15}, Fills : {16}, Makes : {17} }}",
                this.Id.ToString(), this.Blockchain.ToString(), this.ContractHash, this.Address, this.Side.ToString(), this.OfferAsset.ToString(),
                this.WantAsset.ToString(), this.OfferAmount, this.WantAmount, this.TransferAmount, this.PriorityGasAmount, this.UseNativeToken,
                this.NativeFeeTransferAmount,
                this.DepositTransaction != null ? this.DepositTransaction.ToString() : "null",
                this.CreatedAt.ToString(), this.Status.ToString(),
                this.Fills != null ? $"(Length : {this.Fills.Length})" : "null",
                this.Makes != null ? $"(Length : {this.Makes.Length})" : "null");
        }
    }
}
