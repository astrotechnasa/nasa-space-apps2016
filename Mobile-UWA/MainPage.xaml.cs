using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Networking.Sockets;
using System.Collections.ObjectModel;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using System.Threading.Tasks;
using System.Threading;
using System.Text;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace NasaSpaceApp
{
    /// <summary>
    /// The Main Page for the app
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Windows.Devices.Bluetooth.Rfcomm.RfcommDeviceService _service;
        private StreamSocket _socket;
        private DataWriter dataWriterObject;
        private DataReader dataReaderObject;
        ObservableCollection<PairedDeviceInfo> _pairedDevices;
        private CancellationTokenSource ReadCancellationTokenSource;
        bool ServerStarted = false;
        StreamSocketListener listener;
        int port = 8000;
        string recvdtxt = null;
        string Temp1 = "0", Humi1 = "0", AirQ1 = "0", Press1 = "0";

        public MainPage()
        {
            this.InitializeComponent();
            InitializeRfcommDeviceService();
        }


        async void InitializeRfcommDeviceService()
        {
            try
            {
                DeviceInformationCollection DeviceInfoCollection = await DeviceInformation.FindAllAsync(RfcommDeviceService.GetDeviceSelector(RfcommServiceId.SerialPort));


                var numDevices = DeviceInfoCollection.Count();

                // By clearing the backing data, we are effectively clearing the ListBox
                _pairedDevices = new ObservableCollection<PairedDeviceInfo>();
                _pairedDevices.Clear();

                if (numDevices == 0)
                {
                    //MessageDialog md = new MessageDialog("No paired devices found", "Title");
                    //await md.ShowAsync();
                    System.Diagnostics.Debug.WriteLine("InitializeRfcommDeviceService: No paired devices found.");
                }
                else
                {
                    // Found paired devices.
                    foreach (var deviceInfo in DeviceInfoCollection)
                    {
                        _pairedDevices.Add(new PairedDeviceInfo(deviceInfo));
                    }
                }
                PairedDevices.Source = _pairedDevices;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("InitializeRfcommDeviceService: " + ex.Message);
            }
        }

        private void ConnectDevices_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            PairedDeviceInfo pairedDevice = (PairedDeviceInfo)ConnectDevices.SelectedItem;
            ConnectDevice_Click(sender, e);
        }

        async private void ConnectDevice_Click(object sender, RoutedEventArgs e)
        {
            DeviceInformation DeviceInfo;
            PairedDeviceInfo pairedDevice = (PairedDeviceInfo)ConnectDevices.SelectedItem;
            DeviceInfo = pairedDevice.DeviceInfo;

            bool success = true;
            try
            {
                _service = await RfcommDeviceService.FromIdAsync(DeviceInfo.Id);

                if (_socket != null)
                {
                    // Disposing the socket with close it and release all resources associated with the socket
                    _socket.Dispose();
                }

                _socket = new StreamSocket();
                try { 
                    // Note: If either parameter is null or empty, the call will throw an exception
                    await _socket.ConnectAsync(_service.ConnectionHostName, _service.ConnectionServiceName);
                }
                catch (Exception ex)
                {
                        success = false;
                        System.Diagnostics.Debug.WriteLine("Connect:" + ex.Message);
                }
                // If the connection was successful, the RemoteAddress field will be populated
                if (success)
                {
                    this.buttonDisconnect.IsEnabled = true;
                    Listen();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Overall Connect: " +ex.Message);
                _socket.Dispose();
                _socket = null;
            }
        }
        
        private void buttonDisconnect_Click(object sender, RoutedEventArgs e)
        {
            CancelReadTask();
            _socket.Dispose();
            _socket = null;
            this.buttonDisconnect.IsEnabled = false;
        }

        private async void SOS_Click(object sender, RoutedEventArgs e)
        {
            if (!ServerStarted)
            {
                listener = new StreamSocketListener();
                await listener.BindServiceNameAsync(port.ToString());
                ServerStarted = true;
            }
            //this.textBlock.Text = "Started Server";
            listener.ConnectionReceived += async (s, f) =>
            {
                //await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                //{
                //    textBlock.Text = "Got Connected";
                //});
                using (IInputStream input = f.Socket.InputStream)
                {
                    var buffer = new Windows.Storage.Streams.Buffer(2);
                    await input.ReadAsync(buffer, buffer.Capacity, InputStreamOptions.Partial);
                }

                using (IOutputStream output = f.Socket.OutputStream)
                {
                    using (Stream resp = output.AsStreamForWrite())
                    {
                        string header = "HTTP/1.1 200 OK\r\n" +
                                        "Content-Length: {0}\r\n" +
                                        "Content-Type: application/json\r\n" +
                                        "\r\n\r\n" +
                                        "S.O.S." +
                                        "\r\n";
                        string headerOut = String.Format(header, header.Length);
                        byte[] headerArray = Encoding.UTF8.GetBytes(headerOut);
                        await resp.WriteAsync(headerArray, 0, headerArray.Length);

                        await resp.FlushAsync();
                    }
                }
            };
        }

        #region Methods
        private async void SendServerMessage()
        {
            if (!ServerStarted)
            {
                listener = new StreamSocketListener();
                await listener.BindServiceNameAsync(port.ToString());
                ServerStarted = true;
            }
            //this.textBlock.Text = "Started Server";
            listener.ConnectionReceived += async (s, e) =>
            {
                //await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                //{
                //    textBlock.Text = "Got Connected";
                //});
                using (IInputStream input = e.Socket.InputStream)
                {
                    var buffer = new Windows.Storage.Streams.Buffer(2);
                    await input.ReadAsync(buffer, buffer.Capacity, InputStreamOptions.Partial);
                }

                using (IOutputStream output = e.Socket.OutputStream)
                {
                    using (Stream resp = output.AsStreamForWrite())
                    {
                        string header = "HTTP/1.1 200 OK\r\n" +
                                        "Content-Length: {0}\r\n" +
                                        "Content-Type: application/json\r\n" +
                                        "\r\n\r\n" +
                                         
                                        "\r\n";
                        string headerOut = String.Format(header, header.Length);
                        byte[] headerArray = Encoding.UTF8.GetBytes(headerOut);
                        await resp.WriteAsync(headerArray, 0, headerArray.Length);

                        await resp.FlushAsync();
                    }
                }
            };
        }

        public async void Send(string msg)
        {
            try
            {
                if (_socket.OutputStream != null)
                {
                    // Create the DataWriter object and attach to OutputStream
                    dataWriterObject = new DataWriter(_socket.OutputStream);

                    //Launch the WriteAsync task to perform the write
                    await WriteAsync(msg);
                }
                else
                {
                    //status.Text = "Select a device and connect";
                }
            }
            catch (Exception ex)
            {
                //status.Text = "Send(): " + ex.Message;
                System.Diagnostics.Debug.WriteLine("Send(): " + ex.Message);
            }
            finally
            {
                // Cleanup once complete
                if (dataWriterObject != null)
                {
                    dataWriterObject.DetachStream();
                    dataWriterObject = null;
                }
            }
        }

        private async Task WriteAsync(string msg)
        {
            Task<UInt32> storeAsyncTask;

            if (msg == "")
                msg = "none";// sendText.Text;
            if (msg.Length != 0)
            //if (msg.sendText.Text.Length != 0)
            {
                // Load the text from the sendText input text box to the dataWriter object
                dataWriterObject.WriteString(msg);
                
                // Launch an async task to complete the write operation
                storeAsyncTask = dataWriterObject.StoreAsync().AsTask();

                UInt32 bytesWritten = await storeAsyncTask;
                if (bytesWritten > 0)
                {
                    string status_Text = msg + ", ";
                    status_Text += bytesWritten.ToString();
                    status_Text += " bytes written successfully!";
                    System.Diagnostics.Debug.WriteLine(status_Text);
                }
            }
            else
            {
                string status_Text2 = "Enter the text you want to write and then click on 'WRITE'";
                System.Diagnostics.Debug.WriteLine(status_Text2);
            }
        }

        private async void Listen()
        {
            try
            {
                ReadCancellationTokenSource = new CancellationTokenSource();
                if (_socket.InputStream != null)
                {
                    dataReaderObject = new DataReader(_socket.InputStream);
                    while (true)
                    {                 
                        await ReadAsync(ReadCancellationTokenSource.Token);
                    }
                }
            }
            catch (Exception ex)
            {
                this.buttonDisconnect.IsEnabled = false;
                if (ex.GetType().Name == "TaskCanceledException")
                {
                    System.Diagnostics.Debug.WriteLine( "Listen: Reading task was cancelled, closing device and cleaning up");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Listen: " +ex.Message);
                }
            }
            finally
            {
                // Cleanup once complete
                if (dataReaderObject != null)
                {
                    dataReaderObject.DetachStream();
                    dataReaderObject = null;
                }
            }
        }

        private async Task ReadAsync(CancellationToken cancellationToken)
        {
            Task<UInt32> loadAsyncTask;

            uint ReadBufferLength = 1024;

            // If task cancellation was requested, comply
            cancellationToken.ThrowIfCancellationRequested();

            // Set InputStreamOptions to complete the asynchronous read operation when one or more bytes is available
            dataReaderObject.InputStreamOptions = InputStreamOptions.Partial;

            // Create a task object to wait for data on the serialPort.InputStream
            loadAsyncTask = dataReaderObject.LoadAsync(ReadBufferLength).AsTask(cancellationToken);

            // Launch the task and wait
            UInt32 bytesRead = await loadAsyncTask;
            if (bytesRead > 0)
            {
                try
                {
                    string TempVal, HumiVal, PressVal, AirQVal;
                    recvdtxt = dataReaderObject.ReadString(bytesRead);
                    string[] newString = recvdtxt.Split('\n');
                    string StringParsed = newString[newString.Length - 2];
                    //TODO: Add Code to be excuted upon receiving a message
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () => {
                        try
                        {
                            TempVal = StringParsed.Substring(StringParsed.IndexOf("T") + 1, StringParsed.IndexOf("t", StringParsed.IndexOf("T")) - StringParsed.IndexOf("T") - 1);
                            Temp1 = TempVal;
                        }
                        catch
                        {
                            TempVal = Temp1;
                        }
                        try
                        {
                            HumiVal = StringParsed.Substring(StringParsed.IndexOf("H") + 1, StringParsed.IndexOf("h", StringParsed.IndexOf("H")) - StringParsed.IndexOf("H") - 1);
                            Humi1 = HumiVal;
                        }
                        catch
                        {
                            HumiVal = Humi1;
                        }
                        try
                        {
                            PressVal = StringParsed.Substring(StringParsed.IndexOf("P") + 1, StringParsed.IndexOf("p", StringParsed.IndexOf("P")) - StringParsed.IndexOf("P") - 1);
                            Press1 = PressVal;
                        }
                        catch
                        {
                            PressVal = Press1;
                        }
                        try
                        {
                            AirQVal = StringParsed.Substring(StringParsed.IndexOf("A") + 1, StringParsed.IndexOf("a", StringParsed.IndexOf("A")) - StringParsed.IndexOf("A") - 1);
                            AirQ1 = AirQVal;
                        }
                        catch
                        {
                            AirQVal = AirQ1;
                        }
                        Temp.Value = double.Parse(TempVal);
                        Humidity.Value = double.Parse(HumiVal);
                        Pressure.Value = double.Parse(PressVal)/1000;
                        AirQuality.Value = double.Parse(AirQVal);
                        Random Rand = new Random();
                        HeartBeat.Text = Rand.Next(70, 80).ToString();
                        BloodPreassure.Text = Rand.Next(115, 125).ToString() + "//" + Rand.Next(75, 85).ToString();
                    });
                    
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("ReadAsync: " + ex.Message);
                }
                
            }
        }

        private void CancelReadTask()
        {
            if (ReadCancellationTokenSource != null)
            {
                if (!ReadCancellationTokenSource.IsCancellationRequested)
                {
                    ReadCancellationTokenSource.Cancel();
                }
            }
        }

        public class PairedDeviceInfo
        {
            internal PairedDeviceInfo(DeviceInformation deviceInfo)
            {
                this.DeviceInfo = deviceInfo;
                this.ID = this.DeviceInfo.Id;
                this.Name = this.DeviceInfo.Name;
            }

            public string Name { get; private set; }
            public string ID { get; private set; }
            public DeviceInformation DeviceInfo { get; private set; }
        }

        #endregion

        
    }
}
