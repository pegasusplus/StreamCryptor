﻿using ProtoBuf;
using Sodium;
using StreamCryptor.Helper;
using System.Linq;

namespace StreamCryptor.Model
{
    /// <summary>
    /// EncryptedFileFooter for every file.
    /// </summary>
    [ProtoContract]
    public class EncryptedFileFooter
    {
        /// <summary>
        /// The chunk count of this file.
        /// </summary>
        [ProtoMember(1)]
        public byte[] ChunkCount { get; set; }
        /// <summary>
        /// The length of all chunks.
        /// </summary>
        [ProtoMember(2)]
        public byte[] OverallChunkLength { get; set; }
        /// <summary>
        /// The nonce to encrypt and decrypt the OverallChunkLength.
        /// </summary>
        [ProtoMember(3)]
        private byte[] FooterNonceLength { get; set; }
        /// <summary>
        /// The nonce to encrypt and decrypt the ChunkCount.
        /// </summary>
        [ProtoMember(4)]
        private byte[] FooterNonceCount { get; set; }
        /// <summary>
        /// The footer checksum to validate this footer.
        /// </summary>
        [ProtoMember(5)]
        private byte[] FooterChecksum { get; set; }

        public EncryptedFileFooter()
        {
            //do nothing
        }

        public EncryptedFileFooter(int nonceLength)
        {
            this.FooterNonceLength = SodiumCore.GetRandomBytes(nonceLength);
            this.FooterNonceCount = SodiumCore.GetRandomBytes(nonceLength);
        }

        /// <summary>
        /// Sets the footer checksum.
        /// </summary>
        /// <param name="ephemeralKey">A 32 byte key.</param>
        /// <param name="footerChecksumLength">The length of the checksum.</param>
        public void SetFooterChecksum(byte[] ephemeralKey, int footerChecksumLength)
        {
            //protect the ChunkCount
            this.ChunkCount = SecretBox.Create(this.ChunkCount, this.FooterNonceCount, ephemeralKey);
            //protect the OverallChunkLength
            this.OverallChunkLength = SecretBox.Create(this.OverallChunkLength, this.FooterNonceLength, ephemeralKey);
            //generate and set the Footerchecksum
            this.FooterChecksum = Sodium.GenericHash.Hash(ArrayHelpers.ConcatArrays(this.ChunkCount, this.OverallChunkLength), ephemeralKey, footerChecksumLength);
        }

        /// <summary>
        /// Validates the footer checksum.
        /// </summary>
        /// <param name="chunkCount">Number of chunks in the file.</param>
        /// <param name="chunkOverallLength">Length of all chunks in the file.</param>
        /// <param name="ephemeralKey">A 32 byte key.</param>
        /// <param name="footerChecksumLength">The length of the checksum.</param>
        /// <exception cref="BadFileFooterException"></exception>
        public void ValidateFooterChecksum(byte[] chunkCount, byte[] chunkOverallLength, byte[] ephemeralKey, int footerChecksumLength)
        {
            byte[] footerChecksum = Sodium.GenericHash.Hash(
                ArrayHelpers.ConcatArrays(SecretBox.Create(chunkCount, this.FooterNonceCount, ephemeralKey),
                SecretBox.Create(chunkOverallLength, this.FooterNonceLength, ephemeralKey)), 
                ephemeralKey, 
                footerChecksumLength);
            //check the file footer
            if (!footerChecksum.SequenceEqual(this.FooterChecksum))
            {
                throw new BadFileFooterException("Malformed file footer: file could be damaged or manipulated!");
            }
        }
    }
}
