﻿using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Interfaces;
using NeoModules.Core;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;
using Switcheo.Net.Helpers;
using Switcheo.Net.Objects;
using System;
using System.Security;

namespace Switcheo.Net
{
    public class SwitcheoAuthenticationProvider : AuthenticationProvider
    {
        private static Exception privateKeyException = new Exception("Unable to extract public key and address from given private key. Please ensure that your private key is valid");

        public const string ledgerCompatiblePrefix = "010001f0";
        public const string ledgerCompatibleSuffix = "0000";

        public SecureString PrivateKeyWif;
        public BlockchainType KeyType;
        public string PublicKey;
        public string Address;

        public bool CanSign
        {
            get
            {
                return this.Credentials != null && this.Credentials.PrivateKey != null
                    && !string.IsNullOrEmpty(this.Credentials.PrivateKey.ToUnsecureString());
            }
        }

        public SwitcheoAuthenticationProvider(ApiCredentials credentials, BlockchainType keyType) : base(new ApiCredentials(EnsureHexFormat(credentials.PrivateKey)))
        {
            if (this.CanSign)
            {
                if (keyType == BlockchainType.Qtum || keyType == BlockchainType.Ethereum)
                    throw new NotImplementedException();

                try
                {
                    if (WalletsHelper.IsHex(credentials.PrivateKey.ToUnsecureString()))
                        this.PrivateKeyWif = WalletsHelper.ConvertToWif(credentials.PrivateKey);
                    else
                        this.PrivateKeyWif = credentials.PrivateKey;

                    this.KeyType = keyType;
                    var publicKeyAndAddress = WalletsHelper.GetPublicKeyAndAddress(credentials.PrivateKey, keyType);
                    if (publicKeyAndAddress.IsDefault())
                        throw privateKeyException;

                    this.PublicKey = publicKeyAndAddress.Key;
                    this.Address = publicKeyAndAddress.Value;
                }
                catch (Exception)
                {
                    throw privateKeyException;
                }
            }
        }

        public override string AddAuthenticationToUriString(string uri, bool signed)
        {
            // Cleaning query parameters, they'll be sent in request body
            return signed ? (uri.Contains("?") ? uri.Split('?')[0] : uri) : uri;
        }

        public override IRequest AddAuthenticationToRequest(IRequest request, bool signed)
        {
            return request;
        }

        /// <summary>
        /// Sign a message according to the corresponding blockchain
        /// </summary>
        /// <param name="toSign">Message to sign</param>
        /// <returns>Signed message</returns>
        public override byte[] Sign(byte[] toSign)
        {
            byte[] signedResult = null;

            switch (this.KeyType)
            {
                case BlockchainType.Neo:
                    byte[] privateKey = this.Credentials.PrivateKey.ToUnsecureString().HexToBytes();

                    X9ECParameters curve = SecNamedCurves.GetByName("secp256r1");
                    ECDomainParameters domain = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);

                    ECPrivateKeyParameters priv = new ECPrivateKeyParameters("ECDSA", (new BigInteger(1, privateKey)), domain);
                    var signer = new ECDsaSigner();
                    var fullsign = new byte[64];

                    var hash = new Sha256Digest();
                    hash.BlockUpdate(toSign, 0, toSign.Length);

                    byte[] result = new byte[32];
                    hash.DoFinal(result, 0);

                    toSign = result;

                    signer.Init(true, priv);
                    var signature = signer.GenerateSignature(toSign);

                    var r = signature[0].ToByteArray();
                    var s = signature[1].ToByteArray();
                    var rLen = r.Length;
                    var sLen = s.Length;

                    if (rLen < 32)
                        Array.Copy(r, 0, fullsign, 32 - rLen, rLen);
                    else
                        Array.Copy(r, rLen - 32, fullsign, 0, 32);

                    if (sLen < 32)
                        Array.Copy(s, 0, fullsign, 64 - sLen, sLen);
                    else
                        Array.Copy(s, sLen - 32, fullsign, 32, 32);

                    signedResult = fullsign;

                    break;

                case BlockchainType.Qtum:
                    throw new NotImplementedException();

                case BlockchainType.Ethereum:
                    throw new NotImplementedException();
            }

            return signedResult;
        }

        private static SecureString EnsureHexFormat(SecureString privateKey)
        {
            try
            {
                SecureString _privateKey = privateKey;

                if (privateKey != null && privateKey.Length > 0)
                {
                    if (!WalletsHelper.IsHex(privateKey.ToUnsecureString()))
                        _privateKey = WalletsHelper.ConvertToHex(privateKey);
                }

                return _privateKey;
            }
            catch (Exception)
            {
                throw privateKeyException;
            }
        }
    }
}
