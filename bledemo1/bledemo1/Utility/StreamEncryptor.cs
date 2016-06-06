using bledemo1.Type;
using System.Diagnostics;

namespace BTLE.Utils
{
    public class StreamEncryptor
    {
        private readonly EncryptedStreamGenerator _encyptedRxStream;
        private readonly EncryptedStreamGenerator _encyptedTxStream;

        public StreamEncryptor(byte[] key, byte[] deviceSeed, byte[] phoneSeed)
        {
            Debug.Assert(key != null && key.Length == BtleLinkTypes.ENCRYPTED_BLOCK_SIZE);
            Debug.Assert(deviceSeed != null && deviceSeed.Length > 0);
            Debug.Assert(phoneSeed != null && phoneSeed.Length > 0);

            // For use to send data, Encrypt
            _encyptedRxStream = new EncryptedStreamGenerator(key, deviceSeed);

            // For use to receive data, Decrypt 
            _encyptedTxStream = new EncryptedStreamGenerator(key, phoneSeed);

            IsEnabled = true;
        }

        public bool IsEnabled
        {
            get; set;
        }

        public byte[] EncryptAsync(byte[] data)
        {
            return Xor(data, 0, data.Length, _encyptedTxStream);
        }

        public void DecryptAsync(byte[] data, int index, int len)
        {
            byte[] result = Xor(data, index, len, _encyptedRxStream);

            for (int i = 0; i < len; i++)
            {
                data[index + i] = result[i];
            }
        }

        private byte[] Xor(byte[] data, int index, int len, EncryptedStreamGenerator encryptor)
        {
            Debug.Assert(data != null && data.Length > 0);
            Debug.Assert(index >= 0 && len > 0);
            Debug.Assert(index + len <= data.Length);

            Debug.Assert(encryptor != null, "Encryptor is NULL");

            byte[] result = new byte[len];
            byte[] encryptedBlock = encryptor.Generate(len);

            for (int i = 0; i < len; i++)
            {
                result[i] = (byte)(data[index + i] ^ encryptedBlock[i]);

                // Eitan, Just for debug: 0x84 - New Battery Reading
                //if ( i == 0 && result[ 0 ] == 0x84 )
                //    {
                //    // Just to detect the Battery record
                //    Debug.WriteLine( "    Found it: result[ 0 ] == 0x84" );
                //    }
            }
            return result;
        }
    }
}
