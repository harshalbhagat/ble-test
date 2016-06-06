using System.Runtime.InteropServices;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global

namespace BTLE.Cmds
    {
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct AlertCommand
        {
        public AlertCommand( byte alertType ) : this()
            {
            AlertType = alertType;
            }

        public byte AlertType;
        }
    }
