using System.Runtime.InteropServices;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global

namespace BTLE.Cmds
    {
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct SetConnectionSpeedRequest
        {
        public SetConnectionSpeedRequest( byte requestedSpeed ) : this()
            {
            RequestedSpeed = requestedSpeed;
            }

        public byte RequestedSpeed;
        }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct SetConnectionSpeedResponse
        {
        public ushort CurrentConnectionIntervalInMilliSecs;                     // 2 bytes

        // Specifies the **current** connection speed. If the current connection
        // speed matches the requested connection speed, then the client should
        // not expect an additional SpeedChangeCompleteResponse notification,
        // as no new connection interval negotiation will occur.     
        public byte CurrentSpeed;                                               // 1 byte
        }   
    }
