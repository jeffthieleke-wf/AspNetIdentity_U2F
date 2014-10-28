﻿using System;
using System.Security.Cryptography;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Security;
using X509Certificate = System.Security.Cryptography.X509Certificates.X509Certificate;

namespace U2F.Server.Impl
{
	public class BouncyCastleCrypto : ICrypto
	{

		public bool VerifySignature(X509Certificate attestationCertificate, byte[] signedBytes,
			byte[] signature)
		{
			return VerifySignature(attestationCertificate.GetPublicKey(), signedBytes, signature);
		}

		public bool VerifySignature(AsymmetricKeyParameter publicKey, byte[] signedBytes, byte[] signature)
		{
			try
			{
				var signer = SignerUtilities.GetSigner("SHA-256withECDSA");
				signer.Init(false, publicKey);
				signer.BlockUpdate(signedBytes, 0, signedBytes.Length);
				return signer.VerifySignature(signature);
			}
			catch (Exception e)
			{
				throw new U2FException("Error when verifying signature", e);
			}
		}


		public bool VerifySignature(CngKey publicKey, byte[] signedBytes, byte[] signature)
		{
			try
			{

				var ecdsaSignature = new ECDsaCng(publicKey)
				{
					HashAlgorithm = CngAlgorithm.Sha256
				};

				return ecdsaSignature.VerifyData(signedBytes, signature);

			}
			catch (ArgumentException e)
			{
				throw new U2FException("Error when verifying signature", e);
			}
			catch (PlatformNotSupportedException e)
			{
				throw new U2FException("Error when verifying signature", e);
			}
		}

		public bool VerifySignature(byte[] publicKey, byte[] signedBytes, byte[] signature)
		{
			return VerifySignature(CngKey.Import(publicKey, CngKeyBlobFormat.EccPublicBlob), signedBytes, signature);
		}


		public AsymmetricKeyParameter DecodePublicKey(byte[] encodedPublicKey)
		{
			try
			{

				var curve = X962NamedCurves.GetByName("secp256r1");
				ECPoint point;
				try
				{
					point = curve.Curve.DecodePoint(encodedPublicKey);
				}
				catch (Exception e)
				{
					throw new U2FException("Couldn't parse user public key", e);
				}


				var g = new ECKeyPairGenerator("ECDSA");

				var ecP = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);

				g.Init(new ECKeyGenerationParameters(ecP, new SecureRandom()));

				var aKeys = g.GenerateKeyPair();
				return aKeys.Public;
			}
			catch (Exception e)
			{
				throw new U2FException("Error when decoding public key", e);
			}
		}


		public byte[] ComputeSha256(byte[] bytes)
		{
			try
			{
				var mySHA256 = SHA256Cng.Create();
				var hash = mySHA256.ComputeHash(bytes);
				return hash;
			}
			catch (Exception e)
			{
				throw new U2FException("Error when computing SHA-256", e);
			}
		}
	}
}