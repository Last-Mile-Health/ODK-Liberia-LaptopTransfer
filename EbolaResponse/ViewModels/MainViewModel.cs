using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using LastMileHealth.Helpers;
using LastMileHealth.Models;
using GalaSoft.MvvmLight.Command;
using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using Ionic.Zip;
using Microsoft.Win32;
using System.Management;
using System.Xml;

namespace LastMileHealth.ViewModels
{
    public class MainViewModel : GalaSoft.MvvmLight.ViewModelBase
    {
        #region Fields

        private readonly Guid _serviceClassId;

        private string status;

        private ObservableCollection<Zip> zips;

        private ObservableCollection<Device> devices;
        private ObservableCollection<string> statusList = new ObservableCollection<string>();

        private int selectedTabIndex;

        private Zip currentForm;

        private bool canSendLMU = true;

        private bool isBluetoothListView;

        private Device selectedDevice;
        private string lastZipPath;
        private int selectedDeviceIndex = -1;
        private bool isScanning;
        private BluetoothDeviceInfo[] realDevices;

        private readonly ReceiverBluetoothService _receiverBluetoothService;
        private bool canStartListen = true;



        private bool connectionIntervalFinished;
        private bool wasCanceled;
        private bool isZipsShown;
        private bool sendingFromRecent;
        private string versionString;
        //private DispatcherTimer dispatcherTimer;

        #endregion Fields

        public MainViewModel()
        {
            if (MainViewModel.Instance == null)
            {
                MainViewModel.Instance = this;
                //this.InitialiseViewModel();
                this.InitializeFileManager();
                this.SendZipCommand = new RelayCommand<Zip>(this.SendZipExecute);
                this.SelectAllCommand = new RelayCommand(this.SelectAllExecute);

                this.DeleteZipCommand = new RelayCommand<Zip>(this.DeleteZipExecute);
                this.DeleteZipListCommand = new RelayCommand(this.DeleteZipListExecute);

                this.CancelSendLMUCommand = new RelayCommand(this.CancelSendLMUExecute);
                this.RefreshCommand = new RelayCommand(this.RefreshExecute, () => !this.IsScanning);

                this.SendLMUCommand = new RelayCommand(this.SendLMUExecute, () => this.canSendLMU);
                this.StartOrStopListenCommand = new RelayCommand(this.StartOrStopListenExecute);


                _serviceClassId = new Guid("fa87c0d0-afac-11de-8a39-0800200c9a66");


                var listener = new ReceiverBluetoothService();
                _receiverBluetoothService = listener;
                _receiverBluetoothService.PropertyChanged += ReceiverBluetoothService_PropertyChanged;
                _receiverBluetoothService.ListnerStopped += OnReceiverBluetoothServiceListnerStopped;
            }
        }

        void OnReceiverBluetoothServiceListnerStopped(object sender, ListenerStoppedStatus e)
        {

            switch (e.StatusCode)
            {
                case 0: // nothing
                    break;
                case 200: // some error
                    break;
                case 10004: ///A blocking System.Net.Sockets.Socket call was canceled.

                    break;

                default:
                    break;
            }

            if (e.StatusCode != 10004)
            {
                Logger.LogEvent(false, "ListenerStoppedStatus = {0}", e.StatusCode);
            }

            this.UpdsateButtonsState();
        }

        private void ReceiverBluetoothService_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "WasStarted")
            {
                this.CanStartListen = false;
            }
            else
            {
                this.CanStartListen = true;

            }
        }

        public string GetSystemInformation()
        {
            StringBuilder StringBuilderPCInfo = new StringBuilder("System info: \n");
            try
            {
                StringBuilderPCInfo.AppendFormat("Operation System:  {0}", Environment.OSVersion);
                if (Environment.Is64BitOperatingSystem)
                {
                    StringBuilderPCInfo.AppendFormat(", 64 Bit Operating System\n");
                }
                else
                {
                    StringBuilderPCInfo.AppendFormat(", 32 Bit Operating System\n");
                }
                StringBuilderPCInfo.AppendFormat("Cores Count:  {0}\n", Environment.ProcessorCount);

                StringBuilderPCInfo.AppendFormat("Version:  {0}", Environment.Version);

                ManagementClass managementClass = new ManagementClass("Win32_ComputerSystem");
                //collection to store all management objects
                ManagementObjectCollection managementCollection = managementClass.GetInstances();
                if (managementCollection.Count != 0)
                {
                    foreach (ManagementObject managmentObject in managementClass.GetInstances())
                    {
                        // display general system information
                        StringBuilderPCInfo.AppendFormat("\nMachine Make: {0}", managmentObject["Manufacturer"].ToString());
                    }
                }
            }
            catch
            {
            }
            return StringBuilderPCInfo.ToString();
        }

        public string GetBluetoothDeviceInfo()
        {
            StringBuilder bluetoothInfoStrBuilder = new StringBuilder("Bluetooth device info: \n");

            if(BluetoothRadio.PrimaryRadio != null)
            {
                bluetoothInfoStrBuilder.AppendFormat("Name: {0}\n", BluetoothRadio.PrimaryRadio.Name);
                bluetoothInfoStrBuilder.AppendFormat("HardwareStatus: {0}\n", BluetoothRadio.PrimaryRadio.HardwareStatus);
                bluetoothInfoStrBuilder.AppendFormat("HciRevision: {0}\n", BluetoothRadio.PrimaryRadio.HciRevision);
                bluetoothInfoStrBuilder.AppendFormat("HciVersion: {0}\n", BluetoothRadio.PrimaryRadio.HciVersion);
                bluetoothInfoStrBuilder.AppendFormat("LmpVersion: {0}\n", BluetoothRadio.PrimaryRadio.LmpVersion);
                bluetoothInfoStrBuilder.AppendFormat("LmpSubversion: {0}\n", BluetoothRadio.PrimaryRadio.LmpSubversion);
                bluetoothInfoStrBuilder.AppendFormat("Manufacturer: {0}\n", BluetoothRadio.PrimaryRadio.Manufacturer);
                bluetoothInfoStrBuilder.AppendFormat("SoftwareManufacturer: {0}\n", BluetoothRadio.PrimaryRadio.SoftwareManufacturer);
                bluetoothInfoStrBuilder.AppendFormat("Mode: {0}\n", BluetoothRadio.PrimaryRadio.Mode);
                bluetoothInfoStrBuilder.AppendFormat("ClassOfDevice: {0}\n", BluetoothRadio.PrimaryRadio.ClassOfDevice);
                bluetoothInfoStrBuilder.AppendFormat("ClassOfDevice.Device: {0}\n", BluetoothRadio.PrimaryRadio.ClassOfDevice.Device);
                bluetoothInfoStrBuilder.AppendFormat("ClassOfDevice.MajorDevice: {0}\n", BluetoothRadio.PrimaryRadio.ClassOfDevice.MajorDevice);
                bluetoothInfoStrBuilder.AppendFormat("ClassOfDevice.Service: {0}\n", BluetoothRadio.PrimaryRadio.ClassOfDevice.Service);
                bluetoothInfoStrBuilder.AppendFormat("ClassOfDevice.ValueAsInt32: {0}\n", BluetoothRadio.PrimaryRadio.ClassOfDevice.ValueAsInt32);
                bluetoothInfoStrBuilder.AppendFormat("LocalAddress: {0}\n", BluetoothRadio.PrimaryRadio.LocalAddress);
                bluetoothInfoStrBuilder.AppendFormat("LocalAddress.Nap: {0}\n", BluetoothRadio.PrimaryRadio.LocalAddress.Nap);
                bluetoothInfoStrBuilder.AppendFormat("LocalAddress.Sap: {0}", BluetoothRadio.PrimaryRadio.LocalAddress.Sap);

            }

            return bluetoothInfoStrBuilder.ToString();
        }

        #region Commands

        public static MainViewModel Instance { get; private set; }

        public RelayCommand<Zip> DeleteZipCommand { get; set; }
        public RelayCommand DeleteZipListCommand { get; set; }

        public RelayCommand<Zip> SendZipCommand { get; set; }
        public RelayCommand SelectAllCommand { get; set; }

        public RelayCommand SendLMUCommand { get; set; }
        public RelayCommand CancelSendLMUCommand { get; set; }
        public RelayCommand RefreshCommand { get; set; }
        public RelayCommand StartOrStopListenCommand { get; set; }

        #endregion Commands

        #region Properties

        public ObservableCollection<Zip> Zips
        {
            get
            {
                return this.zips;
            }

            set
            {
                this.zips = value;
                this.RaisePropertyChanged("Zips");
            }
        }

        public ObservableCollection<Device> Devices
        {
            get
            {
                return this.devices;
            }

            set
            {
                this.devices = value;
                this.RaisePropertyChanged("Devices");
            }
        }

        public ObservableCollection<string> StatusList
        {
            get
            {
                //return this.statusList;
                return Logger.GetLogWithoutExceptions;
            }

            //set
            //{

            //    this.statusList = value;
            //    this.RaisePropertyChanged("StatusList");
            //}
        }

        public int SelectedDeviceIndex
        {
            get
            {
                return this.selectedDeviceIndex;
            }
            set
            {
                this.selectedDeviceIndex = value;
                this.RaisePropertyChanged("SelectedDeviceIndex");
                if (this.selectedDeviceIndex != -1)
                {
                    //this.IsBluetoothListView = false;
                    this.selectedDevice = this.Devices[this.selectedDeviceIndex];
                    this.SendZip();
                }
                else
                {
                    this.selectedDevice = null;
                }
            }
        }


        public int SelectedTabIndex
        {
            get
            {
                return this.selectedTabIndex;
            }
            set
            {
                this.selectedTabIndex = value;
                this.RaisePropertyChanged("SelectedTabIndex");

            }
        }
        public Zip CurrentForm
        {
            get
            {
                return this.currentForm;
            }

            set
            {
                this.currentForm = value;
                this.RaisePropertyChanged("CurrentForm");
            }
        }

        public string Status
        {
            get
            {
                return this.status;
            }

            set
            {
                this.status = value;
                this.RaisePropertyChanged("Status");
                Logger.LogEvent(false, "{0}", status);
                if (!string.IsNullOrEmpty(this.status))
                {
                    //this.StatusList.Add(string.Format("{0}: {1}", DateTime.Now, this.status));
                }
            }
        }

        public bool IsBluetoothListView
        {
            get
            {
                return this.isBluetoothListView;
            }

            set
            {
                this.isBluetoothListView = value;
                this.RaisePropertyChanged("IsBluetoothListView");
            }
        }

        public bool IsScanning
        {
            get
            {
                return this.isScanning;
            }

            set
            {
                this.isScanning = value;
                this.RaisePropertyChanged("IsScanning");
                this.RefreshCommand.RaiseCanExecuteChanged();
            }
        }

        public bool CanStartListen
        {
            get
            {
                return this.canStartListen;
            }

            set
            {
                this.canStartListen = value;
                this.RaisePropertyChanged("CanStartListen");
            }
        }

        public bool IsZipsShown
        {
            get
            {
                return this.isZipsShown;
            }

            set
            {
                this.isZipsShown = value;
                this.RaisePropertyChanged("IsZipsShown");
            }
        }

        public string VersionString
        {
            get
            {
                if (this.versionString == null)
                {
                    var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                    this.versionString = "ODK Bluetooth Transfer " + version;
                }
                return this.versionString;
            }
        }

        #endregion Properties

        private void SendZipExecute(Zip zip)
        {
            this.sendingFromRecent = true;
            this.PrepareToSendZip(zip.FullPath);
        }

        private void SelectAllExecute()
        {
            if (this.Zips.Any(x => !x.IsSelected))
            {
                this.Zips.ToList().ForEach(x => x.IsSelected = true);
            }
            else
            {
                this.Zips.ToList().ForEach(x => x.IsSelected = false);
            }
        }

        private void DeleteZipExecute(Zip zip)
        {
            if (File.Exists(zip.FullPath))
            {
                File.Delete(zip.FullPath);
            }
            //await DatabaseProvider.Instance.DeleteZip(zip);
            this.Zips.Remove(zip);
            this.UpdateIsZipsShown();
        }

        private void DeleteZipListExecute()
        {
            //var idsForDelete = this.Zips.Where(x => x.IsSelected).Select(x => x.Id);
            //await DatabaseProvider.Instance.DeleteZips(idsForDelete);

            for (int i = this.Zips.Count - 1; i >= 0; i--)
            {
                if (this.Zips[i].IsSelected)
                {
                    if (File.Exists(this.Zips[i].FullPath))
                    {
                        File.Delete(this.Zips[i].FullPath);
                    }
                    this.Zips.Remove(this.Zips[i]);
                }
            }

            this.UpdateIsZipsShown();
        }

        private void UpdateIsZipsShown()
        {
            if (this.Zips != null && this.Zips.Count > 0)
            {
                this.IsZipsShown = true;
            }
            else
            {
                this.IsZipsShown = false;
            }
        }

        //private async void InitialiseViewModel()
        //{
        //    await DatabaseProvider.Instance.InsertZip(new Zip
        //    {
        //        CreationDate = DateTime.Now.AddDays(DateTime.Now.Ticks % 10),
        //        IsSelected = true,
        //        Name = Guid.NewGuid().ToString().Substring(0, 8) + ".xml"
        //    }); //TODO: delete this test data

        //    var zipList = await DatabaseProvider.Instance.GetZips();
        //    //this.Forms = new ObservableCollection<Form>(formList);
        //    //this.CurrentForm = formList.OrderBy(x => x.CreationDate).FirstOrDefault();
        //    //this.CurrentForm.IsEditingNow = true;
        //}

        private void InitializeFileManager()
        {
            if (Directory.Exists(Constants.ODKPath))
            {
                var filesPathes = Directory.GetFiles(Constants.ODKPath, "*.zip", SearchOption.TopDirectoryOnly);// SearchOption.AllDirectories);
                var zipList = new List<Zip>();

                foreach (var path in filesPathes)
                {
                    var fileInfo = new FileInfo(path);
                    zipList.Add(new Zip
                    {
                        CreationDate = fileInfo.CreationTime,
                        FolderPath = fileInfo.DirectoryName,
                        FullPath = fileInfo.FullName,
                        DisplayName = fileInfo.Name.Replace(".zip", ".lmu")
                    });
                }
                this.Zips = new ObservableCollection<Zip>(zipList.OrderByDescending(x => x.CreationDate));

                this.UpdateIsZipsShown();
            }
        }

        #region transfer part

        private void CancelSendLMUExecute()
        {
            if (!this.sendingFromRecent)
            {
                this.DeleteLastZip();
            }
            this.wasCanceled = true;
            this.ClearAfterSendOrCancel();
            this.Status = string.Empty;
        }

        private void DeleteLastZip()
        {
            if (this.lastZipPath != null)
            {
                File.Delete(lastZipPath);
                this.Zips.RemoveAt(this.Zips.Count - 1);
                this.UpdateIsZipsShown();
            }
        }

        private void SendLMUExecute()
        {
            this.canSendLMU = false;
            this.SendLMUCommand.RaiseCanExecuteChanged();
            this.Status = "Selecting lmu file..";

            OpenFileDialog dialog = new OpenFileDialog();

            // Set filter for file extension and default file extension 
            dialog.DefaultExt = ".lmu";
            dialog.Filter = "LMU Files (*.lmu)|*.lmu";

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dialog.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result.HasValue && result == true)
            {
                try
                {
                    this.Status = "Creating archive upload..";

                    string text = null;

                    using (var lmuStream = dialog.OpenFile())
                    {
                        StreamReader reader = new StreamReader(lmuStream, Encoding.UTF8);
                        text = reader.ReadToEnd();
                    }

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        string[] separators = new string[1] { "<LMD_delimiter>" };
                        var xmlFiles = text.Split(separators, StringSplitOptions.RemoveEmptyEntries);

                        for (int i = 0; i < xmlFiles.Length; i++)
                        {
                            xmlFiles[i] = xmlFiles[i].Trim();
                        }

                        if (!Directory.Exists(Constants.FormsPath))
                        {
                            Directory.CreateDirectory(Constants.FormsPath);
                        }

                        var directoriesToZip = new List<string>();

                        foreach (var xmlFile in xmlFiles)
                        {
                            var openTag = "<h:title>";
                            var closeTag = "</h:title>";
                            var startIndex = xmlFile.IndexOf(openTag) + openTag.Length;
                            var name = xmlFile.Substring(startIndex, xmlFile.IndexOf(closeTag) - startIndex);

                            var invalidChars = Path.GetInvalidFileNameChars();

                            name = new string(name.Where(x => !invalidChars.Contains(x)).ToArray());
                            var directory = Directory.CreateDirectory(Path.Combine(Constants.FormsPath, name));

                            var xmlContent = xmlFile;

                            if (name == Constants.RolesSection)
                            {
                                var bodyOpenTag = "<h:body>";
                                var bodyCloseTag = "</h:body>";

                                var bodyStartIndex = xmlFile.IndexOf(bodyOpenTag) + bodyOpenTag.Length;
                                var content = xmlFile.Substring(bodyStartIndex, xmlFile.IndexOf(bodyCloseTag) - bodyStartIndex);

                                xmlContent = "<roles>" + content + "</roles>";
                            }

                            System.IO.File.WriteAllText(Path.Combine(directory.FullName, name + ".xml"), xmlContent);
                            directoriesToZip.Add(directory.FullName);
                        }

                        //if (!File.Exists(Path.Combine(Constants.ODKPath, "forms.txt")))
                        //{
                        //    File.Create(Path.Combine(Constants.ODKPath, "forms.txt"));
                        //}

                        string fullZipPath = Path.Combine(Constants.ODKPath, dialog.SafeFileName.Replace(".lmu", ".zip"));

                        if (File.Exists(fullZipPath))
                        {
                            File.Delete(fullZipPath);
                            if (this.Zips.Any(x => x.FullPath == fullZipPath)) // double check
                            {
                                this.Zips.Remove(this.Zips.First(x => x.FullPath == fullZipPath));
                            }
                        }

                        using (ZipFile zip = new ZipFile())
                        {
                            zip.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
                            foreach (var path in directoriesToZip)
                            {
                                zip.AddDirectory(path);
                            }
                            //zip.AddFile(Path.Combine(Constants.ODKPath, "forms.txt"));
                            zip.AddEntry("forms.txt", string.Empty);
                            zip.Save(fullZipPath);
                        }

                        this.Zips.Insert(0, new Zip
                        {
                            CreationDate = DateTime.Now,
                            FolderPath = Path.GetDirectoryName(fullZipPath),
                            FullPath = fullZipPath,
                            DisplayName = Path.GetFileName(fullZipPath)
                        });

                        this.Status = "Archive created.";
                        this.UpdateIsZipsShown();
                        this.PrepareToSendZip(fullZipPath);
                    }
                }
                catch (Exception ex)
                {
                    this.AddExceptionToLog(ex);
                    this.Status = "There were some problems with parsing LMU file. Try again please.";
                }
                finally
                {
                    this.canSendLMU = true;
                    this.SendLMUCommand.RaiseCanExecuteChanged();
                }
            }
            else
            {
                this.Status = string.Empty;
                this.canSendLMU = true;
                this.SendLMUCommand.RaiseCanExecuteChanged();
            }
        }

        private void PrepareToSendZip(string fullZipPath, bool fromRecents = false)
        {
            if (File.Exists(fullZipPath))
            {
                this.lastZipPath = fullZipPath;
                this.ScanForDevices();
            }
            else
            {
                if (fromRecents)
                {
                    this.Status = "Application can not find appropriate lmu file. Try to send lmu form from main screen again.";
                }
                else
                {
                    this.Status = "Application can not find appropriate lmu file. Try to send form again.";
                }
            }
        }

        private void RefreshExecute()
        {
            this.ScanForDevices();
        }

        private void ScanForDevices()
        {
            if (!BluetoothRadio.IsSupported)
            {
                this.Status = string.Empty;
                MessageBox.Show("No Bluetooth device detected.");
                return;
            }

            this.Devices = null;
            this.Status = "Scanning for bluetooth devices..";
            this.IsBluetoothListView = true;
            this.SelectedDeviceIndex = -1;
            this.IsScanning = true;

            if (BluetoothRadio.PrimaryRadio.Mode == RadioMode.PowerOff)
            {
                BluetoothRadio.PrimaryRadio.Mode = RadioMode.Connectable;
            }

            //await Task.Delay(200);
            //Utils.Delay(5000);
            //await TaskEx.Delay(200);

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            timer.Start();
            timer.Tick += (sender, args) =>
            {
                timer.Stop();

                if (this.IsScanning)
                {
                    BluetoothClient bc = new BluetoothClient();
                    bool realDevicesAreFound = false;
                    Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            this.realDevices = bc.DiscoverDevices(200, true, true, true, true);
                            realDevicesAreFound = this.realDevices != null && this.realDevices.Length > 0;
                        }
                        catch (Exception e)
                        {
                            this.AddExceptionToLog(e);
                        }

                    }).ContinueWith(taskState =>
                    {
                        if (this.IsScanning)
                        {
                            this.IsScanning = false;

                            if (realDevicesAreFound)
                            {
                                this.Status = "Choose device to transfer file.";

                                var devicesList = new List<Device>();

                                foreach (var realDevice in this.realDevices)
                                {
                                    devicesList.Add(new Device { DeviceAddressSap = realDevice.DeviceAddress.Sap, Name = realDevice.DeviceName });
                                }

                                this.Devices = new ObservableCollection<Device>(devicesList);
                            }
                            else
                            {
                                this.Status = "Devices were not found. Press \"App Update\" button on other device and press refresh here.";

                                //     MessageBox.Show("Selected bluetooth device is not available. Please press \"App Update\" button on an appropriate device before sending forms.", "Destination device is not reachable.",
                                //MessageBoxButton.OK, MessageBoxImage.Warning);
                                this.Devices = null;
                            }
                        }
                    }, TaskScheduler.FromCurrentSynchronizationContext());
                }
            };

        }

        private void SendZip()
        {
            Logger.LogEvent(true, "MainViewModel.SendZip 200");

            this.wasCanceled = false;
            this.IsScanning = true;
            this.Status = "Trying to establish connection and transfer file..";
            var success = false;
            Exception lastEx = null;

            if (BluetoothRadio.PrimaryRadio.Mode == RadioMode.PowerOff)
            {
                BluetoothRadio.PrimaryRadio.Mode = RadioMode.Connectable;
            }

            Logger.LogEvent(true, "MainViewModel.SendZip 201");

            Task.Factory.StartNew(() =>
            {
                if (this.selectedDevice != null)
                {
                    var bluetoothClient = new BluetoothClient();
                    NetworkStream bluetoothStream = null;

                    this.connectionIntervalFinished = false;

                    System.Timers.Timer aTimer = new System.Timers.Timer();
                    aTimer.Elapsed += this.OnTimedEvent;
                    // Set the Interval to 20 second.
                    aTimer.Interval = 20000;
                    aTimer.Start();

                    var device = this.realDevices.FirstOrDefault(x => x.DeviceAddress.Sap == this.selectedDevice.DeviceAddressSap);

                    Logger.LogEvent(true, "MainViewModel.SendZip 201.5 device.DeviceName: {0}", device.DeviceName);

                    var ep = new BluetoothEndPoint(device.DeviceAddress, _serviceClassId);

                    Logger.LogEvent(true, "MainViewModel.SendZip 201.75 ep.Address: {0}", ep.Address);

                    while (!this.connectionIntervalFinished && !success && !this.wasCanceled)
                    {
                        try
                        {
                            Logger.LogEvent(true, "MainViewModel.SendZip 201.8 connectionIntervalFinished: {0} , \n success: {1} , \n wasCanceled: {2}",
                                connectionIntervalFinished, success, wasCanceled);

                            // connecting
                            bluetoothClient.Connect(ep);

                            Logger.LogEvent(true, "MainViewModel.SendZip 201.9");

                            // get stream for send the data
                            bluetoothStream = bluetoothClient.GetStream();

                            Logger.LogEvent(true, "MainViewModel.SendZip 201.95");

                            // if all is ok to send
                            if (bluetoothClient.Connected && bluetoothStream != null)
                            {
                                this.Status = "Connection is established and ready to send.";
                                // write the data in the stream

                                var fileInfo = new FileInfo(this.lastZipPath);

                                Logger.LogEvent(true, "MainViewModel.SendZip 202");

                                //var buffer = System.Text.Encoding.UTF8.GetBytes();
                                int fileLength;
                                if (int.TryParse(fileInfo.Length.ToString(), out fileLength))
                                {
                                    var sizeBuffer = LastMileHealth.Helpers.Utils.IntToByteArray(fileLength);
                                    //var sizeBuffer = BitConverter.GetBytes(fileLength);

                                    Logger.LogEvent(true, "MainViewModel.SendZip 203 sizeBuffer: {0}", sizeBuffer.Length);

                                    //TODO: CHUNKS! 4096 //BinaryWriter bw = new BinaryWriter(bluetoothStream);
                                    var fileBuffer = System.IO.File.ReadAllBytes(this.lastZipPath);

                                    var buffer = LastMileHealth.Helpers.Utils.Combine(sizeBuffer, fileBuffer);

                                    Logger.LogEvent(true, "MainViewModel.SendZip 204 sizeBuffer: {0}", buffer.Length);

                                    bluetoothStream.Write(buffer, 0, buffer.Length);
                                    //bluetoothStream.Flush();

                                    // TODO: async method StartListen for responce
                                    byte[] receivedIntBytes = new byte[4];
                                    var tst = bluetoothStream.Read(receivedIntBytes, 0, 4);
                                    var receivedInt = LastMileHealth.Helpers.Utils.ByteArrayToInt(receivedIntBytes);

                                    Logger.LogEvent(true, "MainViewModel.SendZip 205 sizeBuffer: {0}", receivedInt);

                                    if (receivedInt == fileLength)
                                    {
                                        success = true;
                                        Logger.LogEvent(true, "MainViewModel.SendZip 206");
                                    }
                                }
                                else
                                {
                                    this.Status = "Too big file!";
                                    MessageBox.Show("Too big file!");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogEvent(true, "MainViewModel.SendZip 207 Exception = {0} , \nStack = {1}", ex.Message, ex.StackTrace);
                            lastEx = ex;
                        }
                        finally
                        {
                            //if (bluetoothClient != null)
                            //{
                            //    //bluetoothStream.Flush();
                            //    //bluetoothStream.Close();

                            //    bluetoothClient.Close();
                            //    bluetoothClient.Dispose();
                            //}
                        }
                    }

                    Logger.LogEvent(true, "MainViewModel.SendZip 208");

                    this.StopTimer(aTimer);
                    //if (bluetoothStream != null)
                    //{
                    //    bluetoothStream.Flush();
                    //    bluetoothStream.Close();
                    //}

                    if (bluetoothClient != null)
                    {
                        Logger.LogEvent(true, "MainViewModel.SendZip 209");

                        bluetoothClient.Close();
                        bluetoothClient.Dispose();
                        bluetoothClient = null;
                    }

                    Logger.LogEvent(true, "MainViewModel.SendZip 210");
                }
            }).ContinueWith(taskState =>
            {
                Logger.LogEvent(true, "MainViewModel.SendZip 211");

                this.IsScanning = false;

                if (success)
                {
                    this.Status = string.Format("File \"{0}\" has been transferred to {1}!", Path.GetFileNameWithoutExtension(this.lastZipPath) + ".lmu", this.selectedDevice.Name);
                    this.ClearAfterSendOrCancel();
                }
                else if (!this.wasCanceled)
                {
                    this.Status = "Form transfering failed.";

                    Logger.LogEvent(true, "MainViewModel.SendZip 212 lastEx: {0}", lastEx == null ? "IsNULL" : "Not NULL");

                    if (lastEx != null)
                    {
                        if (lastEx is SocketException)
                        {
                            var socketEx = (SocketException)lastEx;

                            BluetoothRadio.PrimaryRadio.Mode = RadioMode.PowerOff;

                            if (socketEx.SocketErrorCode == SocketError.AddressNotAvailable || socketEx.SocketErrorCode == SocketError.TimedOut)
                            {
                                MessageBox.Show("Selected bluetooth device is not available. Please press \"App Update\" button on an appropriate device before sending forms.", "Destination device is not reachable.",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                            else if (socketEx.SocketErrorCode == SocketError.InvalidArgument || socketEx.SocketErrorCode == SocketError.Shutdown)
                            {
                                MessageBox.Show("Try one of next solutions:\n- Try to restart the application\n- Unpair devices in settings (for both devices) and pair them again.\n",
                                    "Bluetooth connection problem.", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                            else// if (lastEx.Message.Contains("invalid argument was supplied")) // (lastEx as SocketException).SocketErrorCode == SocketError.Shutdown 
                            {
                                MessageBox.Show("Try one of next solutions:\n- Try to restart the application;\n- Unpair devices in settings (for both devices) and pair them again;\n- Go to Start > Type services.msc > Services Local > Then scroll down the list till you see 'Bluetooth Support Service' > Right click on it and then Stop the process and then start it up again;\n- Update bluetooth drivers;",
                                    "Bluetooth connection problem.", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }

                        Logger.LogEvent(true, "MainViewModel.SendZip 213"); 

                        this.AddExceptionToLog(lastEx);
                    }

                    this.SelectedDeviceIndex = -1;
                }

                Logger.LogEvent(true, "MainViewModel.SendZip 214"); 

            }, TaskScheduler.FromCurrentSynchronizationContext());

            Logger.LogEvent(true, "MainViewModel.SendZip 215"); 
        }

        public void AddExceptionToLog(Exception lastEx)
        {
            if (lastEx is SocketException)
            {
                var socketEx = lastEx as SocketException;
                var errorString = string.Format("AppException Error= {0}\n\nStackTrace: {1}\n\n ErrorCode: {2}\n\n NativeErrorCode: {3}\n\n SocketErrorCode: {4}",
                                                   socketEx.Message, socketEx.StackTrace, socketEx.ErrorCode, socketEx.NativeErrorCode, socketEx.SocketErrorCode);
                Logger.LogEvent(false, errorString);
                //this.StatusList.Add(string.Format("{0}: {1}", DateTime.Now, errorString));
            }
            else
            {
                var errorString = string.Format("AppException Error= {0} , \nStackTrace= {1}", lastEx.Message, lastEx.StackTrace);
                Logger.LogEvent(false, errorString);
                //this.StatusList.Add(string.Format("{0}: {1}", DateTime.Now, errorString));
            }
        }

        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            var timer = sender as Timer;

            this.StopTimer(timer);
        }

        private void StopTimer(Timer timer)
        {
            timer.Stop();
            timer.Elapsed -= OnTimedEvent;
            timer = null;
            this.connectionIntervalFinished = true;
        }

        private void ClearAfterSendOrCancel()
        {
            this.canSendLMU = true;
            this.SendLMUCommand.RaiseCanExecuteChanged();
            this.IsBluetoothListView = false;
            this.IsScanning = false;
            this.Devices = null;
            this.sendingFromRecent = false;
        }

        #endregion transfer part

        private void UpdsateButtonsState()
        {
            //this.CanStartListen = !_receiverBluetoothService.WasStarted;
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                if (_receiverBluetoothService.WasStarted)
                {
                    this.CanStartListen = false;
                }
                else
                {
                    this.CanStartListen = true;
                }
                //_receiverBluetoothService.Stop();
            }));
        }

        private void StartOrStopListenExecute()
        {
            try
            {
                if (this.CanStartListen)
                {
                    if (!BluetoothRadio.IsSupported)
                    {
                        MessageBox.Show("No Bluetooth device detected.");
                        return;
                    }

                    this.CanStartListen = !this.CanStartListen;
                    this.Status = "Start listen to instances..";

                    BluetoothRadio.PrimaryRadio.Mode = RadioMode.Discoverable;

                    _receiverBluetoothService.Start(SetReceivedZip);
                }
                else
                {
                    _receiverBluetoothService.Stop();
                    BluetoothRadio.PrimaryRadio.Mode = RadioMode.Connectable;
                    //this.CanStartListen = !this.CanStartListen;

                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        this.Status = string.Empty;
                        Logger.LogEvent(false, "Listening was stopped");
                        //this.StatusList.Add(string.Format("{0}: {1}", DateTime.Now, "Listening was stopped."));
                    }));
                }
            }
            catch (Exception ex)
            {
                this.AddExceptionToLog(ex);
            }
        }

        /// <summary>
        /// The set data received.
        /// </summary>
        /// <param name="data">
        /// The data.
        /// </param>
        public void SetReceivedZip(ZipFile zip)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                if (!zip.ContainsEntry("forms.txt"))
                {
                    string backupFolder = Path.Combine(Constants.ODKPath, "LMD Backups");
                    string nameOfLMD = string.Format("data_{0:yyyy-MM-dd}.lmd", DateTime.Now.Date);

                    if (!Directory.Exists(backupFolder))
                    {
                        Directory.CreateDirectory(backupFolder);
                    }

                    int counter = 2;
                    while (true)
                    {
                        if (File.Exists(Path.Combine(backupFolder, nameOfLMD)))
                        {
                            nameOfLMD = string.Format("data_{0:yyyy-MM-dd} ({1}).lmd", DateTime.Now.Date, counter);
                            counter++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    try
                    {
                        var filePathLMD = Path.Combine(backupFolder, nameOfLMD);
                        var fileStream = File.Create(filePathLMD);
                        fileStream.Close();

                        bool firstZip = true;

                        if (!Directory.Exists(Path.Combine(Constants.ODKPath, "Temp")))
                        {
                            Directory.CreateDirectory(Path.Combine(Constants.ODKPath, "Temp"));
                        }

                        var zipFilePath = Path.Combine(Constants.ODKPath, "Temp", nameOfLMD.Replace(".lmd", ".zip"));
                        zip.Save(zipFilePath);
                        var unzippedFolder = zipFilePath.Replace(".zip", string.Empty);
                        zip.ExtractAll(unzippedFolder);

                        // bool isForm = true;
                        using (StreamWriter writetext = new StreamWriter(filePathLMD))
                        {
                            var unzippedFiles = Directory.GetFiles(unzippedFolder, "*.xml", SearchOption.AllDirectories);
                            foreach (var filePathFromZip in unzippedFiles)
                            {
                                if (!firstZip)
                                {
                                    writetext.Write(Environment.NewLine + Environment.NewLine + "<LMD_delimiter>" + Environment.NewLine + Environment.NewLine);
                                }
                                else
                                {
                                    firstZip = false;
                                }

                                var fileString = File.ReadAllText(filePathFromZip);

                                writetext.Write(fileString);
                            }
                        }

                        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                        File.Copy(filePathLMD, Path.Combine(desktop, nameOfLMD));
                        //this.Status = String.Format("Forms are stored on the desktop as {0}.", nameOfLMD);
                        this.Status = String.Format("File \"{0}\" has been received from {1} and stored on the desktop!", nameOfLMD, _receiverBluetoothService.lastClientName);

                        File.Delete(zipFilePath);

                        string[] files = Directory.GetFiles(unzippedFolder, "*.*", SearchOption.AllDirectories);

                        // Display all the files.
                        foreach (string file in files)
                        {
                            File.Delete(file);
                        }

                        Directory.Delete(unzippedFolder, true);
                    }
                    catch (Exception ex)
                    {
                        this.AddExceptionToLog(ex);
                    }
                }
                else
                {
                    this.Status = "Laptop can only receive instances! Form transfer was rejected.";
                    zip.Dispose();
                    MessageBox.Show(this.Status, string.Empty, MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }));
        }

        public void ClearXMLsOnClose()
        {
            if (Directory.Exists((Constants.FormsPath)))
            {
                string[] files = Directory.GetFiles(Constants.FormsPath, "*.*", SearchOption.AllDirectories);

                // Display all the files.
                foreach (string file in files)
                {
                    File.Delete(file);
                }

                Directory.Delete(Constants.FormsPath, true);
            }
        }
    }
}
