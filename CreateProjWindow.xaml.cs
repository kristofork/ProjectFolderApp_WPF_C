using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using static ProjectFolderApp.Utils;

namespace ProjectFolderApp
{
    /// <summary>
    /// Interaction logic for CreateProjWindow.xaml
    /// </summary>
    /// 
    // Validation logic. Validation template is in the App.xaml
    public class NameValidator : ValidationRule
    {
        public override ValidationResult Validate
          (object value, System.Globalization.CultureInfo cultureInfo)
        {
            if (string.IsNullOrEmpty(value.ToString()))
                return new ValidationResult(false, "value cannot be empty.");
            else if (value.ToString().Length > 50)
            {
                return new ValidationResult
                (false, "Name cannot be more than 50 characters long.");
            }
            return ValidationResult.ValidResult;
        }
    }

    public partial class CreateProjWindow : Window
    {

        private string rootPath = GlobalVars.rootdir;
        private string adminDir = GlobalVars.adminDir;
        private string mgmtDir = GlobalVars.projMgmtDir;
        private string[] subDirs = GlobalVars.subDir;
        private string[] subDirsAdmin = GlobalVars.subDirAdmin;
        private string[] subDirsProjMgmt = GlobalVars.subDirProjMgmt;
        string fullPath;
        List<Group> groups = new List<Group>();
        BackgroundWorker bgw;


        private bool Create_Directory(string prjName)
        {
            // Combine the root directory and directory name to get the full path
            string fullPath = System.IO.Path.Combine(rootPath, prjName);
            try
            {
                if (Directory.Exists(fullPath))
                {
                    MessageBox.Show("Directory Already Exists", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return (false);
                }
                bgw.ReportProgress(0, "Creating directory");
                // Create the directory
                Directory.CreateDirectory(fullPath);

                // Create sub-directories
                foreach (string subDir in subDirs)
                {
                    string subDirfullPath = System.IO.Path.Combine(fullPath, subDir);
                    Directory.CreateDirectory(subDirfullPath);
                    bgw.ReportProgress(0, $"Creating directory: {subDir}");
                }
                foreach (string subDir in subDirsAdmin)
                {
                    string subDirAdminfullPath = System.IO.Path.Combine(fullPath, adminDir, subDir);
                    Directory.CreateDirectory(subDirAdminfullPath);
                    bgw.ReportProgress(0, $"Creating directory: {subDir}");
                }
                foreach (string subDir in subDirsProjMgmt)
                {
                    string subDirMgmtfullPath = System.IO.Path.Combine(fullPath, mgmtDir, subDir);
                    Directory.CreateDirectory(subDirMgmtfullPath);
                    bgw.ReportProgress(0, $"Creating directory: {subDir}");
                }
                bgw.ReportProgress(0, $"Directory '{prjName}' created successfully at '{fullPath}'.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating directory: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return (true);
        }
        private void Create_Permissions(string rootPath, string prjName, Array groups)
        {
            bgw.ReportProgress(0, "Setting permissions...");
            string directoryPath = System.IO.Path.Combine(rootPath, prjName);
            string adminPath = System.IO.Path.Combine(directoryPath, "Admin");
            string prjmgmtPath = System.IO.Path.Combine(directoryPath, "Proj Mgmt");
            string techPath = System.IO.Path.Combine(directoryPath, "Tech");

            string identityPrjAccAll = @"Contoso\Group"; 
            string identityITAdminsFull = @"Contoso\Group";
            string identityIT = @"Contoso\IT"; 
            string identityAdministrator = @"Contoso\Administrator"; 
            string identityBuiltInAdministrators = @"BUILTIN\ADMINISTRATORS";
            string identityDomainUsers = @"Contoso\Domain Users";
            string identityProjMgmtFolder = @"Contoso\Group";

            // Specify the file system rights
            FileSystemRights rightsRead = FileSystemRights.ReadAndExecute;
            FileSystemRights rightsFull = FileSystemRights.FullControl;
            FileSystemRights rightsWrite = FileSystemRights.Write;
            FileSystemRights rightsDeleteSubDirAndFiles = FileSystemRights.DeleteSubdirectoriesAndFiles;
            FileSystemRights rightsModifywithDelete = FileSystemRights.ReadAndExecute | FileSystemRights.DeleteSubdirectoriesAndFiles | FileSystemRights.Write;

            // Specify the inheritance and propagation flags
            InheritanceFlags inheritanceFlagsNone = InheritanceFlags.None;
            PropagationFlags propagationFlagsNone = PropagationFlags.None;
            InheritanceFlags inheritanceFlags = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;

            // Specify the access control type (Allow or Deny)
            AccessControlType controlType = AccessControlType.Allow;

            // Group Variable setup
            IdentityReference identityReferencePrjAccAll = new NTAccount(identityPrjAccAll);
            IdentityReference identityReferenceITAdminsFull = new NTAccount(identityITAdminsFull);
            IdentityReference identityReferenceIT = new NTAccount(identityIT);
            IdentityReference identityReferenceAdministrator = new NTAccount(identityAdministrator);
            IdentityReference identityReferenceBuiltInAdministrators = new NTAccount(identityBuiltInAdministrators);
            IdentityReference identityReferenceDomainUsers = new NTAccount(identityDomainUsers);

            // Create the file system access rule Root folder
            FileSystemAccessRule accessRulePrjAll = new FileSystemAccessRule(identityReferencePrjAccAll, rightsRead, inheritanceFlagsNone, propagationFlagsNone, controlType);
            FileSystemAccessRule accessRuleITAdminsFull = new FileSystemAccessRule(identityReferenceITAdminsFull, rightsFull, inheritanceFlags, propagationFlagsNone, controlType);
            FileSystemAccessRule accessRuleIT = new FileSystemAccessRule(identityReferenceIT, rightsModifywithDelete, inheritanceFlags, propagationFlagsNone, controlType);
            FileSystemAccessRule accessRuleAdministrator = new FileSystemAccessRule(identityReferenceAdministrator, rightsFull, inheritanceFlags, propagationFlagsNone, controlType);
            FileSystemAccessRule accessRuleBuiltInAdministrators = new FileSystemAccessRule(identityReferenceBuiltInAdministrators, rightsFull, inheritanceFlags, propagationFlagsNone, controlType);

            // Create file system access rule Admin folder
            FileSystemAccessRule accessRuleDomainUsers = new FileSystemAccessRule(identityReferenceDomainUsers, rightsModifywithDelete, inheritanceFlags, propagationFlagsNone, controlType);
            FileSystemAccessRule accessRuleDomainUsers_folderOnly = new FileSystemAccessRule(identityReferenceDomainUsers, rightsRead, inheritanceFlagsNone, propagationFlagsNone, controlType);

            // Create file system access rule ProjectMgmt folder
            FileSystemAccessRule accessRuleProjMgmtFolder = new FileSystemAccessRule(identityProjMgmtFolder, rightsModifywithDelete, inheritanceFlags, propagationFlagsNone, controlType);
            FileSystemAccessRule accessRuleProjMgmt_folderOnly = new FileSystemAccessRule(identityProjMgmtFolder, rightsRead, inheritanceFlagsNone, propagationFlagsNone, controlType);

            // Root Directory
            // Get the directory info
            DirectoryInfo directoryInfo = new DirectoryInfo(directoryPath);
            DirectorySecurity directorySecurity = directoryInfo.GetAccessControl();
            // Add the access rule to the directory security
            directorySecurity.AddAccessRule(accessRulePrjAll);
            directorySecurity.AddAccessRule(accessRuleITAdminsFull);
            directorySecurity.AddAccessRule(accessRuleIT);
            directorySecurity.AddAccessRule(accessRuleAdministrator);
            directorySecurity.AddAccessRule(accessRuleBuiltInAdministrators);
            foreach (var group in groups)
            {
                string identityTemp = @"Contoso\" + group.ToString();
                bgw.ReportProgress(0, $"Adding group'{identityTemp}' ...");
                IdentityReference identityReferenceTemp = new NTAccount(identityTemp);
                FileSystemAccessRule accessRuleTemp = new FileSystemAccessRule(identityTemp, rightsRead, inheritanceFlagsNone, propagationFlagsNone, controlType);
                directorySecurity.AddAccessRule(accessRuleTemp);
                directoryInfo.SetAccessControl(directorySecurity);
            }
            directorySecurity.SetAccessRuleProtection(true, false);
            // Apply the modified directory security back to the directory
            directoryInfo.SetAccessControl(directorySecurity);

            //Admin Directory
            DirectoryInfo adminInfo = new DirectoryInfo(adminPath);
            DirectorySecurity admindirSecurity = adminInfo.GetAccessControl();
            admindirSecurity.SetAccessRuleProtection(true, true);
            admindirSecurity.AddAccessRule(accessRuleDomainUsers_folderOnly);
            adminInfo.SetAccessControl(admindirSecurity);

            foreach (var subdir in subDirsAdmin)
            {
                bgw.ReportProgress(0, $"Applying permissions to '{subdir}' ...");
                string subAdminPath = System.IO.Path.Combine(adminPath, subdir);
                DirectoryInfo subAdminInfo = new DirectoryInfo(subAdminPath);
                DirectorySecurity subAdminDirSecurity = subAdminInfo.GetAccessControl();
                subAdminDirSecurity.AddAccessRule(accessRuleDomainUsers);
                subAdminDirSecurity.SetAccessRuleProtection(true, true);
                subAdminInfo.SetAccessControl(subAdminDirSecurity);
            }

            // Project Management
            DirectoryInfo prjmgmtInfo = new DirectoryInfo(prjmgmtPath);
            DirectorySecurity prjmgmtdirSecurity = prjmgmtInfo.GetAccessControl();
            prjmgmtdirSecurity.AddAccessRule(accessRuleProjMgmt_folderOnly);
            prjmgmtdirSecurity.SetAccessRuleProtection(true, true);
            prjmgmtInfo.SetAccessControl(prjmgmtdirSecurity);

            foreach (var subdir in subDirsProjMgmt)
            {
                bgw.ReportProgress(0, $"Applying permissions to '{subdir}' ...");
                string subPrjmgmtPath = System.IO.Path.Combine(prjmgmtPath, subdir);
                DirectoryInfo subPrjmgmtInfo = new DirectoryInfo(subPrjmgmtPath);
                DirectorySecurity subPrjmgmtDirSecurity = subPrjmgmtInfo.GetAccessControl();
                subPrjmgmtDirSecurity.AddAccessRule(accessRuleProjMgmtFolder);
                subPrjmgmtDirSecurity.SetAccessRuleProtection(true, true);
                subPrjmgmtInfo.SetAccessControl(subPrjmgmtDirSecurity);
            }

            // Tech
            DirectoryInfo techInfo = new DirectoryInfo(techPath);
            DirectorySecurity techdirSecurity = techInfo.GetAccessControl();
            techdirSecurity.AddAccessRule(accessRuleDomainUsers);
            techdirSecurity.SetAccessRuleProtection(true, true);
            techInfo.SetAccessControl(techdirSecurity);
            bgw.ReportProgress(0, "Applying permissions to tech ...");
        }

        public CreateProjWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            groups = (List<Group>)GetGroups();
            lvGroups.ItemsSource = groups;
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(lvGroups.ItemsSource);
            bgw = new BackgroundWorker();
            bgw.DoWork += BackgroundWorker_DoWork;
            bgw.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
            bgw.WorkerReportsProgress = true;
            bgw.ProgressChanged += BackgroundWorker_ProgressChanged;
        }
        // routedeventargs change
        private async void CreateProject_Click(object sender, RoutedEventArgs e)
        {
            // Show the progress bar
            lvProgressBar.Visibility = Visibility.Visible;
            statusTextBox.Text = "Processing...";
            // Disable all inputs
            CreateProject.IsEnabled = false;
            Cancel.IsEnabled = false;
            lvGroups.IsEnabled = false;
            NewProjectTextBox.IsEnabled = false;

            string pName = NewProjectTextBox.Text;
            string[] groupsList = groups.Where(s => s.isChecked).Select(s => s.Name).ToArray();
            string arrayString = string.Join(", \n-", groupsList);
            MessageBoxResult result = MessageBox.Show($"Confirm creating project folder:\n {pName}\n Groups:\n-{arrayString}", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Start the background worker
                StartBackgroundWork();
            }
            else if (result == MessageBoxResult.No)
            {
                lvProgressBar.Visibility = Visibility.Collapsed;
                // Disable all inputs
                CreateProject.IsEnabled = true;
                Cancel.IsEnabled = true;
                lvGroups.IsEnabled = true;
                NewProjectTextBox.IsEnabled = true;
            }
        }

        // Custom class to encapsulate multiple arguments
        private class WorkerArguments
        {
            public string ProjectName { get; set; }
            public Array GroupList { get; set; }
        }

        private void StartBackgroundWork()
        {
            // Create an instance of WorkerArguments and populate it with values
            WorkerArguments arguments = new WorkerArguments
            {
                ProjectName = NewProjectTextBox.Text.Trim(),
                GroupList = groups.Where(s => s.isChecked).Select(s => s.Name).ToArray()
            };

            // Start the background worker with the argument
            bgw.RunWorkerAsync(arguments);
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //finishing up stuff, perhaps hide the bar or something?
            lvProgressBar.Visibility = Visibility.Collapsed;
            // Close the window
            this.DialogResult = true;
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            // Retrieve the arguments from the Argument property
            WorkerArguments arguments = (WorkerArguments)e.Argument;

            // Access individual arguments
            string prjName = arguments.ProjectName;
            Array grpList = arguments.GroupList;
            // Simulate progress
            bgw.ReportProgress(0, "Checking if directory exists...");
            // Run function1
            bool condition = Create_Directory(prjName);
            // If function1 completes without error, run function2
            if (condition)
            {
                Create_Permissions(rootPath, prjName, grpList);
                bgw.ReportProgress(0, "Finished!");
                System.Threading.Thread.Sleep(1000);
            }
            else
            {
                bgw.ReportProgress(0, "Error! Directory Not Created.");
                System.Threading.Thread.Sleep(1000);
                return;
            }
        }
        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            string progressMessage = e.UserState as string;
            // This method runs on the UI thread and can be used to update UI components with progress
            statusTextBox.Text = progressMessage;
        }
    }
}

