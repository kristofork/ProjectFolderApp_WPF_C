using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using static ProjectFolderApp.Utils;

namespace ProjectFolderApp
{
    /// <summary>
    /// Interaction logic for UpdateProjWindow.xaml
    /// </summary>
    /// 
    public partial class UpdateProjWindow : Window
    {
        private string rootPath = GlobalVars.rootdir;
        List<Group> groups = new List<Group>();
        List<IdentityReference> accessGroups = new List<IdentityReference>();
        public string SelectedProject { get; set; } = string.Empty;
        List<string> currentGroupList = new List<string>();
        List<string> addGroupList = new List<string>();
        List<string> removeGroupList = new List<string>();
        BackgroundWorker bgw;


        public UpdateProjWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            groups = (List<Group>)GetGroups();
            bgw = new BackgroundWorker();
            bgw.DoWork += BackgroundWorker_DoWork;
            bgw.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
            bgw.WorkerReportsProgress = true;
        }
        private void Update_Permissions(string prjDir, List<string> addGrps, List<string> removeGrps)
        {
            // Get the directory access information for currently assigned groups
            DirectoryInfo directoryInfo = new DirectoryInfo(prjDir);
            DirectorySecurity directorySecurity = directoryInfo.GetAccessControl();
            AuthorizationRuleCollection accessRules = directorySecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
            // loop through all groups that were selected and pass as an argument. Add them as an AccessRule
            foreach (var gr in addGrps)
            {
                string identityTemp = @"Contoso\" + gr.ToString();
                IdentityReference identityReferenceTemp = new NTAccount(identityTemp);
                FileSystemAccessRule accessRuleTemp = new FileSystemAccessRule(identityReferenceTemp, FileSystemRights.ReadAndExecute, InheritanceFlags.None, PropagationFlags.None, AccessControlType.Allow);
                directorySecurity.AddAccessRule(accessRuleTemp);
            }
            // loop through all remove groups and remove them.
            foreach (var gr in removeGrps)
            {
                string identityTempRemove = @"Contoso\" + gr.ToString();
                foreach (FileSystemAccessRule rule in accessRules)
                {
                    if (rule.IdentityReference.Value.Equals(identityTempRemove, StringComparison.OrdinalIgnoreCase))
                    {
                        directorySecurity.RemoveAccessRule(rule);
                    }
                }
            }
            directoryInfo.SetAccessControl(directorySecurity);
        }
        private void UpdateProjWindow1_Loaded(object sender, RoutedEventArgs e)
        {
            // update the textbox on the form
            UpdateProjTextBox.Text = SelectedProject;
            // Get the access groups for the selected project
            accessGroups = (List<IdentityReference>)GetDirectorySecurity(SelectedProject);
            // Setup the listview with all groups
            lvGroups.ItemsSource = groups;
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(lvGroups.ItemsSource);
            // Loop through all groups available
            foreach (Group item in lvGroups.Items)
            {   // loop through each group the selected project has access to.
                foreach (var groupAccess in accessGroups)
                {   // if group is CREATOR OWNER skip to avoid application crash. Some early projects may have this group assign which causes the app to crash
                    if (groupAccess.ToString() == "CREATOR OWNER")
                    {
                        continue;
                    }
                    // if the group mataches the listliew group check the checkbox
                    if (groupAccess.Value.Split('\\')[1] == item.Name.ToString())
                    {
                        item.isChecked = true;
                        currentGroupList.Add(item.Name.ToString());
                    }
                }
            }
        }

        private void UpdPrj_CheckBoxGroups_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox Item = (CheckBox)sender;
            MessageBox.Show(Item.Content.ToString());

            if (Item.IsChecked == true)
            {
                if (currentGroupList.Contains(Item.Content.ToString()))
                {
                    removeGroupList.Remove(Item.Content.ToString());
                }
                else
                {
                    addGroupList.Add(Item.Content.ToString());
                }
            }
        }

        private void UpdPrj_CheckBoxGroups_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox Item = (CheckBox)sender;
            MessageBox.Show(Item.Content.ToString());
            if (Item.IsChecked == false)
            {
                if (currentGroupList.Contains(Item.Content.ToString()))
                {
                    removeGroupList.Add(Item.Content.ToString());
                }
                else
                { addGroupList.Remove(Item.Content.ToString()); }
            }
        }

        private void UpdateProject_Click(object sender, RoutedEventArgs e)
        {
            // Show the progress bar
            lvProgressBar.Visibility = Visibility.Visible;
            // Disable all inputs
            Cancel.IsEnabled = false;
            lvGroups.IsEnabled = false;
            StartBackgroundWork();
        }

        // Arguments for the background worker
        private class WorkerArguments
        {
            public string Path { get; set; }
            public List<string> AddGroups { get; set; }
            public List<string> RemoveGroups { get; set; }
        }
        private void StartBackgroundWork()
        {
            // Create an instance of WorkerArguments and populate it with values
            WorkerArguments arguments = new WorkerArguments
            {
                Path = System.IO.Path.Combine(rootPath, SelectedProject),
                AddGroups = groups.Where(g => g.isChecked).Select(g => g.Name).ToList(),
                RemoveGroups = removeGroupList
            };
            // Start the background worker with the argument
            bgw.RunWorkerAsync(arguments);
        }
        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //Hide the progress bar
            lvProgressBar.Visibility = Visibility.Collapsed;
            // Close the window
            this.DialogResult = true;
        }
        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            // Retrieve the arguments from the Argument property
            WorkerArguments arguments = (WorkerArguments)e.Argument;

            // Access individual arguments
            string prjPath = arguments.Path;
            List<string> grpAdd = arguments.AddGroups;
            List<string> grpRemove = arguments.RemoveGroups;
            try
            {
                Update_Permissions(prjPath, grpAdd, grpRemove);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                throw;
            }
        }
    }
}