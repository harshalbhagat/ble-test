using System;

namespace bledemo1
{
    public class EnumberationCompletedEventHandler
    {
        public event EventHandler<EnumberationCompletedEventArgs> StopEventHandle;

        public void EnumberationCompleted(EnumberationCompletedEventArgs args)
        {
            OnEnumberationCompleted(args);
        }
        void OnEnumberationCompleted(EnumberationCompletedEventArgs args)
        {
            if (StopEventHandle != null)
            {
                StopEventHandle(this, args);
            }
        }
    }
}
