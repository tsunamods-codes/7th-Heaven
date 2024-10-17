//TODO: Add support for .NET 6.0

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.ServiceModel;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows;

///// <summary>
///// Implementation reference: https://www.codeproject.com/Articles/1186738/A-Robust-and-Elegant-Single-Instance-WPF-Applicati
///// </summary>
//namespace AppUI.Classes.WCF
//{
//    /// <summary> The WCF interface for passing the startup parameters </summary>
//    [ServiceContract (ProtectionLevel = System.Net.Security.ProtectionLevel.None)]
//    public interface ISingleInstance
//    {
//        /// <summary>
//        /// Notifies the first instance that another instance of the application attempted to start.
//        /// </summary>
//        /// <param name="args">The other instance's command-line arguments.</param>
//        [OperationContract]
//        void PassStartupArgs(string[] args);
//    }

//    /// <summary>
//    /// Event declaration for the startup of a second instance
//    /// </summary>
//    public class SecondInstanceStartedEventArgs : EventArgs
//    {
//        /// <summary>
//        /// The event method declaration for the startup of a second instance
//        /// </summary>
//        /// <param name="args">The other instance's command-line arguments.</param>
//        public SecondInstanceStartedEventArgs(string[] args)
//        { Args = args; }

//        /// <summary>
//        /// Property containing the second instance's command-line arguments
//        /// </summary>
//        public string[] Args { get; set; }
//    }

//    /// <summary>
//    /// A class to allow for a single-instance of an application.
//    /// </summary>
//    public class SingleInstance : ISingleInstance
//    {
//        /// <summary>
//        /// Is raised when another instance attempts to start up.
//        /// </summary>
//        public static event EventHandler<SecondInstanceStartedEventArgs> OnSecondInstanceStarted;

//        private static ServiceHost NamedPipeHost = null;

//        /// <summary>
//        /// Interface: Notifies the first instance that another instance of the application attempted to start.
//        /// </summary>
//        /// <param name="args">The other instance's command-line arguments.</param>
//        public void PassStartupArgs(string[] args)
//        {
//            //check if an event is registered for when a second instance is started
//            OnSecondInstanceStarted?.Invoke(this, new SecondInstanceStartedEventArgs(args));
//        }

//        /// <summary>
//        /// Checks to see if this instance is the first instance of this application on the local machine.  If it is not, this method will
//        /// send the first instance this instance's command-line arguments.
//        /// </summary>
//        /// <param name="uid">The application's unique identifier.</param>
//        /// <param name="activatewindow">Should the main window become active on a second instance launch</param>
//        /// <returns>True if this instance is the first instance.</returns>
//        public static bool IsFirstInstance(string uid, bool activatewindow)
//        {
//            //attempt to open the service, should succeed if this is the first instance
//            if (OpenServiceHost(uid))
//            {
//                if (activatewindow)
//                    OnSecondInstanceStarted += ActivateMainWindow;
//                return true;
//            }

//            return false;
//        }

//        /// <summary>
//        /// Attempts to create the named pipe service.
//        /// </summary>
//        /// <param name="uid">The application's unique identifier.</param>
//        /// <returns>True if the service was published successfully.</returns>
//        private static bool OpenServiceHost(string uid)
//        {
//            try
//            {
//                //hook the application exit event to avoid race condition when messages flow while the application is disposing of the channel during shutdown
//                Application.Current.Exit += new ExitEventHandler(OnAppExit);

//                //for any unhandled exception we need to ensure NamedPipeHost is disposed
//                AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(OnUnhandledException);


//                //setup the WCF service using a NamedPipe
//                NamedPipeHost = new ServiceHost(typeof(SingleInstance), new Uri("net.pipe://localhost/7thHeavenApp"));
//                NamedPipeHost.AddServiceEndpoint(typeof(ISingleInstance), new NetNamedPipeBinding(), uid);

//                //if the service is already open (i.e. another instance is already running) this will cause an exception
//                NamedPipeHost.Open();

//                //success and we are first instance
//                return true;
//            }
//            catch (AddressAlreadyInUseException)
//            {
//                //failed to open the service so must be a second instance
//                NamedPipeHost.Abort();
//                NamedPipeHost = null;
//                return false;
//            }
//            catch (CommunicationObjectFaultedException)
//            {
//                //failed to open the service so must be a second instance
//                NamedPipeHost.Abort();
//                NamedPipeHost = null;
//                return false;
//            }
//            catch (Exception)
//            {
//                // an uknown error occurred maybe invalid endpoint?
//                return false;
//            }
//        }

//        /// <summary>
//        /// Ensures that the named pipe service host is closed on the application exit
//        /// </summary>
//        private static void OnAppExit(object sender, EventArgs e)
//        {
//            if (NamedPipeHost != null)
//            {
//                NamedPipeHost.Close();
//                NamedPipeHost = null;
//            }
//        }

//        /// <summary>
//        /// ensure host is disposed of if there is an unhandled exception
//        /// </summary>
//        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
//        {
//            if (NamedPipeHost != null)
//            {
//                if (NamedPipeHost.State == CommunicationState.Faulted)
//                    NamedPipeHost.Abort();
//                else
//                    NamedPipeHost.Close();
//                NamedPipeHost = null;
//            }
//        }

//        /// <summary>
//        /// Notifies the main instance that this instance is attempting to start up.
//        /// </summary>
//        /// <param name="uid">The application's unique identifier.</param>
//        public static void NotifyFirstInstance(string uid)
//        {
//            //create channel with first instance interface
//            using (ChannelFactory<ISingleInstance> factory = new ChannelFactory<ISingleInstance>(
//                new NetNamedPipeBinding(), new EndpointAddress("net.pipe://localhost/7thHeavenApp/" + uid)))
//            {
//                ISingleInstance singleInstanceInterface = factory.CreateChannel();
//                //pass the command-line args to the first interface
//                singleInstanceInterface.PassStartupArgs(Environment.GetCommandLineArgs());
//            }
//        }

//        /// <summary>
//        /// Activate the first instance's main window
//        /// </summary>
//        private static void ActivateMainWindow(object sender, SecondInstanceStartedEventArgs e)
//        {
//            //activate first instance window
//            Application.Current.Dispatcher.Invoke(new Action(() =>
//            {
//                //check if window state is minimized then restore
//                if (Application.Current.MainWindow.WindowState == WindowState.Minimized)
//                    Application.Current.MainWindow.WindowState = WindowState.Normal; //despite saying Normal this actually will restore
//                Application.Current.MainWindow.Activate(); //now activate window
//            }));
//        }
//    }
//}
