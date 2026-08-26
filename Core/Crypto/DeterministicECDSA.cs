#if !HAS_SPAN
using Deltanium.BouncyCastle.Crypto;
using Deltanium.BouncyCastle.Crypto.Digests;
using Deltanium.BouncyCastle.Crypto.Parameters;
using Deltanium.BouncyCastle.Crypto.Signers;
using Deltanium.BouncyCastle.Security;
using System;
using System.Linq;

namespace Deltanium.Crypto
{
	static class DeterministicDSAExtensions
	{
		public static void Update(this IMac hmac, byte[] input)
		{
			hmac.BlockUpdate(input, 0, input.Length);
		}
		public static byte[] DoFinal(this IMac hmac)
		{
			byte[] result = new byte[hmac.GetMacSize()];
			hmac.DoFinal(result, 0);
			return result;
		}

		public static void Update(this IDigest digest, byte[] input)
		{
			digest.BlockUpdate(input, 0, input.Length);
		}
		public static void Update(this IDigest digest, byte[] input, int offset, int length)
		{
			digest.BlockUpdate(input, offset, length);
		}
		public static byte[] Digest(this IDigest digest)
		{
			byte[] result = new byte[digest.GetDigestSize()];
			digest.DoFinal(result, 0);
			return result;
		}
	}
	internal class DeterministicECDSA : ECDsaSigner
	{
		private byte[] _buffer = new byte[0];
		private readonly IDigest _digest;

		public DeterministicECDSA(bool forceLowR = true)
			: base(new HMacDsaKCalculator(new Sha256Digest()), forceLowR)

		{
			_digest = new Sha256Digest();
			this.forceLowR = forceLowR;
		}
		public DeterministicECDSA(Func<IDigest> digest, bool forceLowR = true)
			: base(new HMacDsaKCalculator(digest()), forceLowR)
		{
			_digest = digest();
		}


		public void setPrivateKey(ECPrivateKeyParameters ecKey)
		{
			base.Init(true, ecKey);
		}

		public void update(byte[] buf)
		{
			_buffer = _buffer.Concat(buf).ToArray();
		}

		public byte[] sign()
		{
			var hash = new byte[_digest.GetDigestSize()];
			_digest.BlockUpdate(_buffer, 0, _buffer.Length);
			_digest.DoFinal(hash, 0);
			_digest.Reset();
			return signHash(hash);
		}

		public byte[] signHash(byte[] hash)
		{
#pragma warning disable 618
			return new ECDSASignature(GenerateSignature(hash)).ToDER();
#pragma warning restore 618
		}
	}
}
#endif
