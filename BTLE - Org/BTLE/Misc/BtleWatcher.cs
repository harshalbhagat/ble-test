using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using BTLE.Types;
using BTLE.Utils;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ClassWithVirtualMembersNeverInherited.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable MemberCanBePrivate.Global

namespace BTLE.Misc
    {
    public class BtleWatcher
        {
        private readonly App _app = Application.Current as App;

        public MainPage RootPage
            {
            get;
            }

        public string Name
            {
            get;
            }

        public DeviceWatcher Watcher
            {
            get; set;
            }

        private TypedEventHandler<DeviceWatcher, DeviceInformation> _handlerAdded;
        private TypedEventHandler<DeviceWatcher, DeviceInformationUpdate> _handlerUpdated;
        private TypedEventHandler<DeviceWatcher, DeviceInformationUpdate> _handlerRemoved;
        private TypedEventHandler<DeviceWatcher, object> _handlerEnumCompleted;
        private TypedEventHandler<DeviceWatcher, object> _handlerStopped;

        public bool EnumCompleted
            {
            get; set;
            }

        public ObservableCollection<DeviceInformationDisplay> ResultCollection
            {
            get;
            }

        public BtleWatcher( MainPage rootPage, string name )
            {
            RootPage = rootPage;
            Name = name;
            ResultCollection = new ObservableCollection<DeviceInformationDisplay>();
            }

        public void InitializeBtleWatcher( string aqsFilter )
            {
            if ( Watcher != null )
                {
                return;
                }

            ResultCollection.Clear();

            Debug.WriteLine( "BTLEWatcher aqsFilter: " + aqsFilter );


            ////////////////////////////////////////////////////////////////
            // Create the Watcher for the BTLE - GATT_STREAM_SERVICE_UUID //
            ////////////////////////////////////////////////////////////////
            // List of additional properties 
            // https://msdn.microsoft.com/en-us/windows/uwp/devices-sensors/device-information-properties
            Watcher = DeviceInformation.CreateWatcher( aqsFilter );     // An AQS string that filters the DeviceInformation objects to enumerate


            /////////////////////////////////////////////////////////////////////////
            // Hook up handlers for the watcher events before starting the watcher //
            /////////////////////////////////////////////////////////////////////////

            ///////////
            // Added //
            ///////////
            _handlerAdded = async ( watcher, deviceInfo ) =>
                {
                    // Since we have the collection databound to a UI element, we need to update the collection on the UI thread.
                    await RootPage.Dispatcher.RunAsync( CoreDispatcherPriority.Low, () =>
                    {
                        // Check for duplicate entry
                        var isDuplicate = ResultCollection.Any( bleInfoDisp => deviceInfo.Id == bleInfoDisp.Id );
                        if ( isDuplicate )
                            {
                            return;
                            }

                        // DeviceID W4 - \\?\BTHLEDevice#{151c0000-4580-4111-9ca1-5056f3454fbc}_f7a30813de8a#6&2e59d2a&f9&000c#{6e3bb679-4372-40c8-9eaa-4509df260cd8}
                        Debug.WriteLine( Name + " - " + deviceInfo.Id );
                        ResultCollection.Add( new DeviceInformationDisplay( deviceInfo ) );

                        //MainPage.Debug_DisplayDeviceParams( deviceInfo, ResultCollection.Count );

                        RootPage.NotifyUser( string.Format( Name + "-{0} devices found.", ResultCollection.Count ), NotifyType.StatusMessage );

                        // Fire Event
                        var args = new BtleWatcherEventsArgs
                            {
                            Event = WatcherEvents.Added,
                            TimeReached = DateTime.Now
                            };
                        OnWatcherEventReached( args );
                    } );
                };
            Watcher.Added += _handlerAdded;


            /////////////
            // Updated //
            /////////////
            _handlerUpdated = async ( watcher, deviceInfoUpdate ) =>
                {
                    // Since we have the collection databound to a UI element, we need to update the collection on the UI thread.
                    await RootPage.Dispatcher.RunAsync( CoreDispatcherPriority.Low, () =>
                    {
                        // Find the corresponding updated DeviceInformation in the collection and pass the update object
                        // to the Update method of the existing DeviceInformation. This automatically updates the object for us.
                        foreach ( DeviceInformationDisplay deviceInfoDisp in ResultCollection )
                            {
                            if ( deviceInfoDisp.Id != deviceInfoUpdate.Id )
                                {
                                continue;
                                }

                            deviceInfoDisp.Update( deviceInfoUpdate, Name );
                            break;
                            }

                        // Fire Event
                        var args = new BtleWatcherEventsArgs
                            {
                            Event = WatcherEvents.Updated,
                            TimeReached = DateTime.Now
                            };
                        OnWatcherEventReached( args );
                    } );
                };
            Watcher.Updated += _handlerUpdated;


            /////////////
            // Removed //
            /////////////
            _handlerRemoved = async ( watcher, deviceInfoUpdate ) =>
                {
                    // Since we have the collection databound to a UI element, we need to update the collection on the UI thread.
                    await RootPage.Dispatcher.RunAsync( CoreDispatcherPriority.Low, () =>
                        {
                            // Find the corresponding DeviceInformation in the collection and remove it
                            foreach ( DeviceInformationDisplay deviceInfoDisp in ResultCollection )
                                {
                                if ( deviceInfoDisp.Id != deviceInfoUpdate.Id )
                                    {
                                    continue;
                                    }

                                if ( ResultCollection.Contains( deviceInfoDisp ) )
                                    {
                                    ResultCollection.Remove( deviceInfoDisp );
                                    }
                                break;
                                }

                            RootPage.NotifyUser( string.Format( Name + "-{0} devices found.", ResultCollection.Count ), NotifyType.StatusMessage );

                            // Fire Event
                            var args = new BtleWatcherEventsArgs
                                {
                                Event = WatcherEvents.Removed,
                                TimeReached = DateTime.Now
                                };
                            OnWatcherEventReached( args );
                        } );
                };
            Watcher.Removed += _handlerRemoved;


            //////////////////////////
            // EnumerationCompleted //
            //////////////////////////
            _handlerEnumCompleted = async ( watcher, obj ) =>
                {
                    await RootPage.Dispatcher.RunAsync( CoreDispatcherPriority.Low, () =>
                    {
                        EnumCompleted = true;
                        Debug.WriteLine( Name + " Enumeration completed: {0} devices found .... Watching for updates...", ResultCollection.Count );
                        RootPage.NotifyUser( string.Format( Name + "-{0} devices found.\n{1}-Enumeration completed.\n{1}-Watching for updates...", ResultCollection.Count, Name ), NotifyType.StatusMessage );
                        Utility.VibratePhone();

                        // Fire Event
                        var args = new BtleWatcherEventsArgs
                            {
                            Event = WatcherEvents.EnumerationCompleted,
                            TimeReached = DateTime.Now
                            };
                        OnWatcherEventReached( args );
                    } );
                };
            Watcher.EnumerationCompleted += _handlerEnumCompleted;


            /////////////
            // Stopped //
            /////////////
            _handlerStopped = async ( watcher, obj ) =>
                {
                    await RootPage.Dispatcher.RunAsync( CoreDispatcherPriority.Low, () =>
                    {
                        RootPage.NotifyUser( string.Format( Name + "-{0} devices found. Watcher {1}.",
                                             ResultCollection.Count, DeviceWatcherStatus.Aborted == watcher.Status ? "aborted" : "stopped" ),
                                             NotifyType.StatusMessage );

                        // Fire Event
                        var args = new BtleWatcherEventsArgs
                            {
                            Event = WatcherEvents.Stopped,
                            TimeReached = DateTime.Now
                            };
                        OnWatcherEventReached( args );
                    } );
                };
            Watcher.Stopped += _handlerStopped;
            }

        public void StopDeviceWatcher()
            {
            if ( Watcher != null )
                {
                // First unhook all event handlers except the stopped handler. This ensures our
                // event handlers don't get called after stop, as stop won't block for any "in flight" 
                // event handler calls.  We leave the stopped handler as it's message guaranteed to only be called
                // once and we'll use it to know when the query is completely stopped. 
                Watcher.Added -= _handlerAdded;
                Watcher.Updated -= _handlerUpdated;
                Watcher.Removed -= _handlerRemoved;
                Watcher.EnumerationCompleted -= _handlerEnumCompleted;

                if ( DeviceWatcherStatus.Started == Watcher.Status || DeviceWatcherStatus.EnumerationCompleted == Watcher.Status )
                    {
                    Watcher.Stop();
                    }

                Watcher = null;
                }
            }

        public event EventHandler<BtleWatcherEventsArgs> WacherEvent;

        protected virtual void OnWatcherEventReached( BtleWatcherEventsArgs e )
            {
            // Fire the event
            EventHandler<BtleWatcherEventsArgs> handler = WacherEvent;
            handler?.Invoke( this, e );
            }
        }



    public class BtleWatcherEventsArgs : EventArgs
        {
        public WatcherEvents Event
            {
            get; set;
            }

        public DateTime TimeReached
            {
            get; set;
            }
        }
    }
