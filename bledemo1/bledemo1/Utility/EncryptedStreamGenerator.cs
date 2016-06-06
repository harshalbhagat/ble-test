using bledemo1.Type;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;
using Buffer = System.Buffer;

namespace BTLE.Utils
{
    public class EncryptedStreamGenerator
    {
        private int _offset;
        private readonly byte[] _key;
        private readonly byte[] _prevSeed;

        // The seed is either the deviceSeed or the phoneSeed, 16 bytes long
        public EncryptedStreamGenerator(byte[] key, byte[] seed)
        {
            Debug.Assert(key != null && key.Length == BtleLinkTypes.ENCRYPTED_BLOCK_SIZE);
            Debug.Assert(seed != null && seed.Length == BtleLinkTypes.ENCRYPTED_BLOCK_SIZE);

            _key = new byte[BtleLinkTypes.ENCRYPTED_BLOCK_SIZE];
            Buffer.BlockCopy(key, 0, _key, 0, BtleLinkTypes.ENCRYPTED_BLOCK_SIZE);

            _prevSeed = new byte[BtleLinkTypes.ENCRYPTED_BLOCK_SIZE];
            Buffer.BlockCopy(seed, 0, _prevSeed, 0, BtleLinkTypes.ENCRYPTED_BLOCK_SIZE);

            _offset = 0;
        }

        //convert to Task if taking time
        public byte[] Generate(int length)
        {
            Debug.Assert(length > 0);
            Debug.Assert(_key != null);
            Debug.Assert(_prevSeed != null);

            byte[] result = new byte[length];

            // Creates an instance of the SymmetricKeyAlgorithmProvider class and opens the specified algorithm for use
            // SymmetricAlgorithmNames.AesEcb - Retrieves a string that contains "AES_ECB".
            SymmetricKeyAlgorithmProvider aesProvider = SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithmNames.AesEcb);

            // Creates a buffer from an input byte array
            IBuffer keyBuffer = CryptographicBuffer.CreateFromByteArray(_key);

            // Creates a symmetric key
            // https://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k(Windows.Security.Cryptography.Core.SymmetricKeyAlgorithmProvider.CreateSymmetricKey);k(TargetFrameworkMoniker-.NETCore,Version%3Dv5.0);k(DevLang-csharp)&rd=true 
            // https://msdn.microsoft.com/en-us/library/windows/apps/xaml/br241541(v=win.10).aspx?appid=dev14idef1&l=en-us&k=k(windows.security.cryptography.core.symmetrickeyalgorithmprovider.createsymmetrickey)%3bk(targetframeworkmoniker-.netcore,version%3dv5.0)%3bk(devlang-csharp)&rd=true
            CryptographicKey cryptographicKey = aesProvider.CreateSymmetricKey(keyBuffer);

            int remainingLength = length;
            int index = 0;

            while (remainingLength > 0)
            {
                int chunkLength = BtleLinkTypes.ENCRYPTED_BLOCK_SIZE - _offset;
                chunkLength = remainingLength < chunkLength ? remainingLength : chunkLength;

                if (_offset == 0)
                {
                    //byte[ ] initializationVector = new byte[ BtleLinkTypes.ENCRYPTED_BLOCK_SIZE ];

                    // Creates a buffer from an input byte array
                    //IBuffer ivBuffer = CryptographicBuffer.CreateFromByteArray( initializationVector );

                    // Creates a buffer from an input byte array
                    IBuffer seedBuffer = CryptographicBuffer.CreateFromByteArray(_prevSeed);

                    // Encrypt the data
                    IBuffer encryptedSeed = CryptographicEngine.Encrypt(cryptographicKey, seedBuffer, null);
                    Debug.Assert(encryptedSeed.Length >= BtleLinkTypes.ENCRYPTED_BLOCK_SIZE);

                    Buffer.BlockCopy(encryptedSeed.ToArray(), 0, _prevSeed, 0, BtleLinkTypes.ENCRYPTED_BLOCK_SIZE);
                }

                Buffer.BlockCopy(_prevSeed, _offset, result, index, chunkLength);
                index += chunkLength;
                remainingLength -= chunkLength;

                Debug.Assert(_offset + chunkLength <= BtleLinkTypes.ENCRYPTED_BLOCK_SIZE);

                _offset = (_offset + chunkLength) % BtleLinkTypes.ENCRYPTED_BLOCK_SIZE;
            }

            Debug.Assert(result.Length == length);

            return result;
        }
    }
}