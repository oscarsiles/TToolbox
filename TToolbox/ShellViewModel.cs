using Caliburn.Micro;
using NLog;
using NLog.Layouts;
using NLog.Targets;
using Ookii.Dialogs.Wpf;
using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Deployment.Application;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using TToolbox.Models;
using TToolbox.ViewModels;

namespace TToolbox
{
    [Export(typeof(IShell))]
    public class ShellViewModel : Screen, IShell
    {
        private IEventAggregator _eventAggregator = new EventAggregator();

        public SaveModel SaveModel { get; set; }

        public StatusBarViewModel StatusBar { get; set; }

        public AfmTipTabViewModel AfmTipTab { get; set; }

        public ForceTabViewModel ForceTab { get; set; }

        public FrictionTabViewModel FrictionTab { get; set; }

        public ModelTabViewModel ModelTab { get; set; }

        private Version _version;

        public ShellViewModel()
        {
            // get the version number
            if (ApplicationDeployment.IsNetworkDeployed)
            {
                _version = ApplicationDeployment.CurrentDeployment.CurrentVersion;

                this.DisplayName = String.Format("TToolbox - v{0}.{1}.{2}.{3} - ({4})", 
                                                    _version.Major, 
                                                    _version.Minor, 
                                                    _version.Build, 
                                                    _version.Revision, 
                                                    GetBuildDateTime(Assembly.GetExecutingAssembly()).Date.ToShortDateString());
            }
            else
                this.DisplayName = "TToolbox - Debug";

            SaveModel = new SaveModel(_eventAggregator);
            StatusBar = new StatusBarViewModel(_eventAggregator);
            AfmTipTab = new AfmTipTabViewModel(_eventAggregator);
            ForceTab = new ForceTabViewModel(_eventAggregator);
            FrictionTab = new FrictionTabViewModel(_eventAggregator);
            ModelTab = new ModelTabViewModel(_eventAggregator);
        }

        public void MenuExit()
        {
            App.Current.Shutdown();
        }

        public void MenuAbout()
        {
            using (TaskDialog dialog = new TaskDialog())
            {
                dialog.WindowTitle = "About TToolbox";
                dialog.MainInstruction = "TToolbox © 2014-2015 Oscar Siles Brügge";
                dialog.Content = 
                    "This software was written while working on a PhD at the Department of Chemistry of the University of Sheffield.\n\n" +
                    "This software includes the following third party open source software components:\n" +
                    "Caliburn.Micro, CsvReader, EPPlus, Math.NET, Ookii.Dialogs, and Oxyplot.\n" +
                    "Each of these software components have their own license.\n\n" +
                    "Code for pull-off force calculations was derived from Carpick's toolbox matlab code.";
                //dialog.ExpandedInformation = "Ookii.org's Task Dialog doesn't just provide a wrapper for the native Task Dialog API; it is designed to provide a programming interface that is natural to .Net developers.";
                dialog.Footer = "If you have any questions, please send me <a href=\"mailto:o.siles-brugge@sheffield.ac.uk?subject=TToolbox\">an email</a>.";
                dialog.FooterIcon = TaskDialogIcon.Information;
                dialog.CenterParent = true;
                dialog.EnableHyperlinks = true;
                TaskDialogButton okButton = new TaskDialogButton(ButtonType.Ok);
                TaskDialogButton licenseButton = new TaskDialogButton("View Licenses");
                dialog.Buttons.Add(licenseButton);
                dialog.Buttons.Add(okButton);
                dialog.HyperlinkClicked += new EventHandler<HyperlinkClickedEventArgs>(TaskDialog_HyperLinkClicked);
                TaskDialogButton button = dialog.ShowDialog();
                if (button == licenseButton)
                {
                    var windowManager = new WindowManager();
                    var vm = new LicencesWinViewModel();
                    windowManager.ShowDialog(vm);
                }
            }
        }

        public void MenuOpenLog()
        {
            var fileTarget = (FileTarget)NLog.LogManager.Configuration.FindTargetByName("file");
            var logEventInfo = new LogEventInfo { TimeStamp = DateTime.Now };
            var filename = fileTarget.FileName.Render(logEventInfo);
            Process.Start(filename);
        }

        public void MenuCheckForUpdates()
        {
            if (ApplicationDeployment.IsNetworkDeployed)
            {
                Boolean updateAvailable = false;
                ApplicationDeployment ad = ApplicationDeployment.CurrentDeployment;

                try
                {
                    updateAvailable = ad.CheckForUpdate();
                }
                catch (DeploymentDownloadException dde)
                {
                    // This exception occurs if a network error or disk error occurs 
                    // when downloading the deployment.
                    MessageBox.Show("The application cannt check for the existence of a new version at this time. \n\nPlease check your network connection, or try again later. Error: " + dde);
                    return;
                }
                catch (InvalidDeploymentException ide)
                {
                    MessageBox.Show("The application cannot check for an update. The ClickOnce deployment is corrupt. Please redeploy the application and try again. Error: " + ide.Message);
                    return;
                }
                catch (InvalidOperationException ioe)
                {
                    MessageBox.Show("This application cannot check for an update. This most often happens if the application is already in the process of updating. Error: " + ioe.Message);
                    return;
                }

                if (updateAvailable)
                {
                    try
                    {
                        ad.Update();
                        MessageBox.Show("The application has been upgraded, and will now restart.");
                        // from System.Windows.Forms.dll
                        System.Windows.Forms.Application.Restart();
                        Application.Current.Shutdown();
                    }
                    catch (DeploymentDownloadException dde)
                    {
                        MessageBox.Show("Cannot install the latest version of the application. Either the deployment server is unavailable, or your network connection is down. \n\nPlease check your network connection, or try again later. Error: " + dde.Message);
                    }
                    catch (TrustNotGrantedException tnge)
                    {
                        MessageBox.Show("The application cannot be updated. The system did not grant the application the appropriate level of trust. Please contact your system administrator or help desk for further troubleshooting. Error: " + tnge.Message);
                    }
                }
                else
                {
                    MessageBox.Show("You are already on the latest version.");
                }
            }
        }

        private void TaskDialog_HyperLinkClicked(object sender, HyperlinkClickedEventArgs e)
        {
            Process.Start(e.Href);
        }

        #region Gets the build date and time (by reading the COFF header)

        // http://msdn.microsoft.com/en-us/library/ms680313

        struct _IMAGE_FILE_HEADER
        {
            public ushort Machine;
            public ushort NumberOfSections;
            public uint TimeDateStamp;
            public uint PointerToSymbolTable;
            public uint NumberOfSymbols;
            public ushort SizeOfOptionalHeader;
            public ushort Characteristics;
        };

        static DateTime GetBuildDateTime(Assembly assembly)
        {
            if (File.Exists(assembly.Location))
            {
                var buffer = new byte[Math.Max(Marshal.SizeOf(typeof(_IMAGE_FILE_HEADER)), 4)];
                using (var fileStream = new FileStream(assembly.Location, FileMode.Open, FileAccess.Read))
                {
                    fileStream.Position = 0x3C;
                    fileStream.Read(buffer, 0, 4);
                    fileStream.Position = BitConverter.ToUInt32(buffer, 0); // COFF header offset
                    fileStream.Read(buffer, 0, 4); // "PE\0\0"
                    fileStream.Read(buffer, 0, buffer.Length);
                }
                var pinnedBuffer = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                try
                {
                    var coffHeader = (_IMAGE_FILE_HEADER)Marshal.PtrToStructure(pinnedBuffer.AddrOfPinnedObject(), typeof(_IMAGE_FILE_HEADER));

                    return TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1) + new TimeSpan(coffHeader.TimeDateStamp * TimeSpan.TicksPerSecond));
                }
                finally
                {
                    pinnedBuffer.Free();
                }
            }
            return new DateTime();
        }

        #endregion
    }
}